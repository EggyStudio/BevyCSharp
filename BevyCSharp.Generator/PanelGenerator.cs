using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bevy.Generator;

/// <summary>
/// Turns <c>[EditorPanel]</c> classes into the wiring between a document and its bindings.
/// </summary>
/// <remarks>
/// <para>
/// A panel is three files: structure in HTML, appearance in CSS, behavior in C#. This generator
/// is what joins them, so that a panel author writes fields and methods with attributes on them
/// and never writes a lookup, a comparison or a dispatch.
/// </para>
/// <para>
/// It cannot check that an element id exists, because the document is an asset the compiler
/// never sees, and deliberately so: the document is meant to be edited without a rebuild. What
/// it does check is everything on this side, so the failures that remain are ones a person can
/// see by looking at the file they just edited.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class PanelGenerator : IIncrementalGenerator
{
    private const string PanelAttribute = "BevyCSharp.Editor.Framework.EditorPanelAttribute";
    private const string BindAttribute = "BevyCSharp.Editor.Framework.BindAttribute";
    private const string CommandAttribute = "BevyCSharp.Editor.Framework.CommandAttribute";
    private const string ChangeAttribute = "BevyCSharp.Editor.Framework.OnChangeAttribute";
    private const string ShowAttribute = "BevyCSharp.Editor.Framework.ShowAttribute";
    private const string RefreshAttribute = "BevyCSharp.Editor.Framework.OnRefreshAttribute";
    private const string ContextAttribute = "BevyCSharp.Editor.Framework.ContextAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var panels = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                PanelAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, token) => Extract(ctx, token))
            .Where(static result => result is not null)
            .Collect();

        context.RegisterSourceOutput(panels, static (spc, results) =>
        {
            var models = new List<PanelModel>();

            foreach (var result in results)
            {
                if (result is null) continue;

                foreach (var diagnostic in result.Diagnostics)
                    spc.ReportDiagnostic(diagnostic);

                if (result.Model is { } model) models.Add(model);
            }

            foreach (var model in models)
                spc.AddSource($"{model.UniqueKey}.Panel.g.cs", PanelEmitter.Emit(model));

            if (models.Count > 0)
                spc.AddSource("PanelCatalogue.g.cs", PanelEmitter.EmitCatalogue(models));
        });
    }

    /// <summary>The model for one class, plus anything wrong with it.</summary>
    private sealed record ExtractResult(PanelModel? Model, ImmutableArray<Diagnostic> Diagnostics);

    /// <summary>Reads one <c>[EditorPanel]</c> class into a model, validating as it goes.</summary>
    private static ExtractResult? Extract(
        GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol type) return null;
        if (context.TargetNode is not ClassDeclarationSyntax declaration) return null;

        token.ThrowIfCancellationRequested();

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (!declaration.Modifiers.Any(m => m.ValueText == "partial"))
        {
            diagnostics.Add(Diagnostic.Create(
                PanelDiagnostics.NotPartial, declaration.Identifier.GetLocation(), type.Name));
        }

        var panelAttribute = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == PanelAttribute);

        var document = panelAttribute?.ConstructorArguments.FirstOrDefault().Value as string;

        if (string.IsNullOrEmpty(document)) return null;

        var chrome = ChromeOf(panelAttribute!);

        var bindings = new List<PanelBindingModel>();
        var commands = new List<PanelCommandModel>();
        var contexts = new List<PanelCommandModel>();
        var changed = new List<string>();
        var refreshed = new List<string>();
        var claimed = new Dictionary<string, string>();

        foreach (var member in type.GetMembers())
        {
            token.ThrowIfCancellationRequested();

            switch (member)
            {
                case IFieldSymbol field when Element(field, BindAttribute) is { } element:
                    AddBinding(field, field.Type, field.IsReadOnly, element, BindAttribute);
                    break;

                case IPropertySymbol property when Element(property, BindAttribute) is { } element:
                    AddBinding(property, property.Type, property.SetMethod is null, element, BindAttribute);
                    break;

                case IFieldSymbol field when Elements(field, ShowAttribute) is { Count: > 0 } shown:
                    foreach (var element in shown)
                        AddBinding(field, field.Type, true, element, ShowAttribute);
                    break;

                case IPropertySymbol property
                    when Elements(property, ShowAttribute) is { Count: > 0 } shown:
                    foreach (var element in shown)
                        AddBinding(property, property.Type, true, element, ShowAttribute);
                    break;

                case IMethodSymbol method when Marked(method, RefreshAttribute):
                    if (method.Parameters.Length > 0)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            PanelDiagnostics.UnsupportedCommand, Where(method), method.Name));
                        break;
                    }

                    refreshed.Add(method.Name);
                    break;

                case IMethodSymbol method when Marked(method, ChangeAttribute):
                    if (method.Parameters.Length > 0)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            PanelDiagnostics.UnsupportedCommand, Where(method), method.Name));
                        break;
                    }

                    changed.Add(method.Name);
                    break;

                case IMethodSymbol method when Element(method, ContextAttribute) is { } element:
                {
                    var repeat = CountOf(method, ContextAttribute);

                    var wanted = repeat > 0 ? 1 : 0;
                    if (method.Parameters.Length != wanted
                        || (wanted == 1
                            && method.Parameters[0].Type.SpecialType != SpecialType.System_Int32))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            PanelDiagnostics.UnsupportedCommand, Where(method), method.Name));
                        break;
                    }

                    contexts.Add(new PanelCommandModel(element, method.Name, repeat));
                    break;
                }

                case IMethodSymbol method when Element(method, CommandAttribute) is { } element:
                {
                    var repeat = CountOf(method, CommandAttribute);

                    // A repeated command is told which of its elements was clicked, and an
                    // ordinary one is told nothing, because the attribute already named it.
                    var wanted = repeat > 0 ? 1 : 0;
                    if (method.Parameters.Length != wanted
                        || (wanted == 1
                            && method.Parameters[0].Type.SpecialType != SpecialType.System_Int32))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            PanelDiagnostics.UnsupportedCommand, Where(method), method.Name));
                        break;
                    }

                    commands.Add(new PanelCommandModel(element, method.Name, repeat));
                    break;
                }
            }
        }

        if (bindings.Count == 0 && commands.Count == 0 && contexts.Count == 0 && refreshed.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                PanelDiagnostics.NothingBound, declaration.Identifier.GetLocation(), type.Name));
        }

        var model = new PanelModel(
            type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            document!,
            chrome,
            bindings,
            commands,
            contexts,
            changed,
            refreshed);

        return new ExtractResult(model, diagnostics.ToImmutable());

        void AddBinding(
            ISymbol member, ITypeSymbol memberType, bool readOnly, string element, string attribute)
        {
            var count = CountOf(member, attribute);

            // A repeated binding stands for many elements, so it needs a value for each. The
            // element type is then what has to be one a widget carries, not the array itself.
            if (count > 0)
            {
                if (memberType is not IArrayTypeSymbol array)
                {
                    diagnostics.Add(Diagnostic.Create(
                        PanelDiagnostics.RepeatedNeedsArray, Where(member),
                        member.Name, element, count));
                    return;
                }

                memberType = array.ElementType;
                readOnly = false;
            }

            var kind = attribute == ShowAttribute
                ? (memberType.SpecialType == SpecialType.System_Boolean ? BindKind.Visible : null)
                : KindOf(memberType);

            if (kind is not { } bound)
            {
                diagnostics.Add(Diagnostic.Create(
                    PanelDiagnostics.UnsupportedBindType, Where(member),
                    member.Name, element, memberType.ToDisplayString()));
                return;
            }

            // Whether an element is on screen is a different channel from what it holds, so a
            // row can have its text bound and its visibility bound without those being a clash.
            var channel = bound == BindKind.Visible ? "visible:" + element : element;

            if (claimed.TryGetValue(channel, out var first))
            {
                diagnostics.Add(Diagnostic.Create(
                    PanelDiagnostics.DuplicateElement, Where(member), first, member.Name, element));
                return;
            }

            claimed[channel] = member.Name;

            // A member with nowhere to put an edit is one way whatever the attribute asked for,
            // and so is anything bound to whether an element is on screen at all.
            var twoWay = !readOnly && bound != BindKind.Visible && ModeOf(member) != 1;

            bindings.Add(new PanelBindingModel(
                element, member.Name, bound, twoWay,
                bound == BindKind.Number ? memberType.ToDisplayString() : null,
                count));
        }
    }

    /// <summary>
    /// Reads what the panel attribute says about where the panel sits.
    /// </summary>
    /// <remarks>
    /// Every one of these is optional, and a panel that names none of them is placed by its own
    /// stylesheet exactly as before, which is what keeps a panel that does not care about layout
    /// from having to say so.
    /// </remarks>
    private static PanelChromeModel ChromeOf(AttributeData attribute)
    {
        var named = attribute.NamedArguments
            .ToDictionary(pair => pair.Key, pair => pair.Value.Value);

        return new PanelChromeModel(
            Id("Root"),
            Id("Handle"),
            Number("Dock") is { } dock ? (int)dock : 0,
            Number("X") is { } x ? (float)x : 0f,
            Number("Y") is { } y ? (float)y : 0f,
            Number("Width") is { } width ? (float)width : float.NaN,
            Number("Height") is { } height ? (float)height : float.NaN,
            Number("Order") is { } order ? (int)order : 0,
            Number("Dismiss") is { } dismiss ? (int)dismiss : 0,
            Number("Layer") is { } layer ? (int)layer : 0);

        // Written either way, the same as a binding's element: a hash is how the id reads in a
        // stylesheet, and leaving it out is how it reads in the document.
        string? Id(string key) =>
            named.TryGetValue(key, out var value) && value is string text && text.Length > 0
                ? text.TrimStart('#')
                : null;

        double? Number(string key) =>
            named.TryGetValue(key, out var value) && value is not null
                ? System.Convert.ToDouble(value)
                : null;

    }

    /// <summary>Whether a member carries an attribute that takes no arguments.</summary>
    private static bool Marked(ISymbol member, string attribute) => member.GetAttributes()
        .Any(a => a.AttributeClass?.ToDisplayString() == attribute);

    /// <summary>The element a member is bound to, without its leading hash, or null.</summary>
    private static string? Element(ISymbol member, string attribute)
    {
        var data = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attribute);

        if (data?.ConstructorArguments.FirstOrDefault().Value is not string element) return null;

        // Written either way. A hash is how the id reads in a stylesheet, and leaving it out is
        // how it reads in the document, so both arrive here.
        return element.TrimStart('#');
    }

    /// <summary>Every element a member is bound to through one kind of attribute.</summary>
    /// <remarks>
    /// A member may carry several of the same attribute, which is how one array shows and hides
    /// three elements at once.
    /// </remarks>
    private static List<string> Elements(ISymbol member, string attribute)
    {
        var elements = new List<string>();

        foreach (var data in member.GetAttributes())
        {
            if (data.AttributeClass?.ToDisplayString() != attribute) continue;
            if (data.ConstructorArguments.FirstOrDefault().Value is not string element) continue;

            elements.Add(element.TrimStart('#'));
        }

        return elements;
    }

    /// <summary>How many elements an attribute's id stands for, or 0 for one.</summary>
    private static int CountOf(ISymbol member, string attribute) => member.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == attribute)
        ?.NamedArguments.FirstOrDefault(n => n.Key == "Count").Value.Value as int? ?? 0;

    /// <summary>The <c>Mode</c> a binding asked for, or 0 for the default.</summary>
    private static int ModeOf(ISymbol member) => member.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == BindAttribute)
        ?.NamedArguments.FirstOrDefault(n => n.Key == "Mode").Value.Value as int? ?? 0;

    /// <summary>Which of an element's values a type maps onto, or null when none does.</summary>
    private static BindKind? KindOf(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Boolean => BindKind.Flag,
        SpecialType.System_Single or SpecialType.System_Double => BindKind.Number,
        SpecialType.System_Int32 or SpecialType.System_Int64 => BindKind.Number,
        SpecialType.System_String => BindKind.Text,
        _ => null,
    };

    /// <summary>Where to point a diagnostic at a member.</summary>
    private static Location Where(ISymbol member) =>
        member.Locations.FirstOrDefault() ?? Location.None;
}

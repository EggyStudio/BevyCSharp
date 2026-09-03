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

        var document = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == PanelAttribute)
            ?.ConstructorArguments.FirstOrDefault().Value as string;

        if (string.IsNullOrEmpty(document)) return null;

        var bindings = new List<PanelBindingModel>();
        var commands = new List<PanelCommandModel>();
        var changed = new List<string>();
        var claimed = new Dictionary<string, string>();

        foreach (var member in type.GetMembers())
        {
            token.ThrowIfCancellationRequested();

            switch (member)
            {
                case IFieldSymbol field when Element(field, BindAttribute) is { } element:
                    AddBinding(field, field.Type, field.IsReadOnly, element);
                    break;

                case IPropertySymbol property when Element(property, BindAttribute) is { } element:
                    AddBinding(property, property.Type, property.SetMethod is null, element);
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

                case IMethodSymbol method when Element(method, CommandAttribute) is { } element:
                    if (method.Parameters.Length > 0)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            PanelDiagnostics.UnsupportedCommand, Where(method), method.Name));
                        break;
                    }

                    commands.Add(new PanelCommandModel(element, method.Name));
                    break;
            }
        }

        if (bindings.Count == 0 && commands.Count == 0)
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
            bindings,
            commands,
            changed);

        return new ExtractResult(model, diagnostics.ToImmutable());

        void AddBinding(ISymbol member, ITypeSymbol memberType, bool readOnly, string element)
        {
            if (KindOf(memberType) is not { } kind)
            {
                diagnostics.Add(Diagnostic.Create(
                    PanelDiagnostics.UnsupportedBindType, Where(member),
                    member.Name, element, memberType.ToDisplayString()));
                return;
            }

            if (claimed.TryGetValue(element, out var first))
            {
                diagnostics.Add(Diagnostic.Create(
                    PanelDiagnostics.DuplicateElement, Where(member), first, member.Name, element));
                return;
            }

            claimed[element] = member.Name;

            // A member with nowhere to put an edit is one way whatever the attribute asked for.
            var twoWay = !readOnly && ModeOf(member) != 1;

            bindings.Add(new PanelBindingModel(
                element, member.Name, kind, twoWay,
                kind == BindKind.Number ? memberType.ToDisplayString() : null));
        }
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

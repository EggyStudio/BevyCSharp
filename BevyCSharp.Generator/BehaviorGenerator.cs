using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bevy.Generator;

/// <summary>
/// Turns <c>[Behavior]</c> structs into Bevy systems.
/// </summary>
/// <remarks>
/// <para>
/// For each behavior the generator emits a runner class with one system method per stage, and
/// a <c>Register(App)</c> that adds them to the schedule. It then emits a single per-assembly
/// entry point tagged <c>[GeneratedBehaviorRegistration]</c>, which
/// <c>BehaviorsPlugin</c> finds reflectively at startup. That is the whole reason a consuming
/// project needs no registration code, because the wiring is generated and then discovered.
/// </para>
/// <para>
/// The emitted code stays deliberately thin. Iteration, filtering and parallel partitioning
/// all live in <c>BehaviorRunners</c> in the runtime library, so the generated file is a
/// handful of readable, verifiable lines that a consumer can step through, and so fixing the
/// iteration strategy does not mean regenerating anyone's code.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class BehaviorGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = RecognizedAttributes.Namespace;
    private const string BehaviorAttribute = RecognizedAttributes.Namespace + "." + RecognizedAttributes.Behavior;
    private const string DataAssetAttribute = RecognizedAttributes.Namespace + "." + RecognizedAttributes.DataAsset;
    private const string DataVersionAttribute = RecognizedAttributes.Namespace + "." + RecognizedAttributes.DataVersion;

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var behaviors = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BehaviorAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, token) => Extract(ctx, token))
            .Where(static result => result is not null)
            .Collect();

        context.RegisterSourceOutput(behaviors, static (spc, results) =>
        {
            var models = new List<BehaviorModel>();
            var described = new List<BehaviorModel>();

            foreach (var result in results)
            {
                if (result is null) continue;

                foreach (var diagnostic in result.Diagnostics)
                    spc.ReportDiagnostic(diagnostic);

                if (result.Model is not { } model) continue;

                if (model.Methods.Count > 0) models.Add(model);

                // A behavior with fields and no methods is a plain data component, and a tool
                // showing one has exactly as much to say about it as about any other, so the
                // field table is collected independently of whether there are systems to run.
                described.Add(model);
            }

            foreach (var model in models)
                spc.AddSource($"{model.UniqueKey}.Behavior.g.cs", BehaviorEmitter.Emit(model));

            if (models.Count > 0)
                spc.AddSource("BehaviorRegistration.g.cs", BehaviorEmitter.EmitRegistration(models));

            if (SchemaEmitter.Emit(described) is { } schemas)
                spc.AddSource("ComponentSchemaRegistration.g.cs", schemas);
        });

        // Data assets are described the same way, with strings, lists and dictionaries counted as
        // fields, since they live on the managed side, and registered so a file can be read as the
        // type it names.
        var assets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataAssetAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol type
                    ? new BehaviorModel
                    {
                        Namespace = type.ContainingNamespace.IsGlobalNamespace
                            ? null
                            : type.ContainingNamespace.ToDisplayString(),
                        Name = type.Name,
                        QualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Fields = ReadFields(type, managed: true),
                        FormerNames = FormerNamesOf(type),
                        Version = VersionOf(type),
                        Migrates = Migration(type) is not null,
                    }
                    : null)
            .Where(static model => model is not null)
            .Collect();

        context.RegisterSourceOutput(assets, static (spc, found) =>
        {
            if (SchemaEmitter.EmitDataAssets([.. found.OfType<BehaviorModel>()]) is { } registration)
                spc.AddSource("DataAssetRegistration.g.cs", registration);
        });

        // A version past the first with no Migrate method reads an older file as if it were the new
        // shape, which is the mistake the version was there to prevent, so it is said where the
        // attribute is rather than found in a save that loaded wrong. Its own pass, because a
        // component and a data asset both carry the attribute.
        var versions = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataVersionAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, _) =>
                    ctx.TargetSymbol is INamedTypeSymbol type
                    && VersionOf(type) is var version and > 1
                    && Migration(type) is null
                        ? Diagnostic.Create(
                            BehaviorDiagnostics.VersionWithoutMigrate,
                            ctx.TargetNode is TypeDeclarationSyntax declaration
                                ? declaration.Identifier.GetLocation()
                                : ctx.TargetNode.GetLocation(),
                            type.Name,
                            version)
                        : null)
            .Where(static diagnostic => diagnostic is not null);

        context.RegisterSourceOutput(versions, static (spc, diagnostic) => spc.ReportDiagnostic(diagnostic!));
    }

    /// <summary>
    /// Reads what a behavior shows: its instance fields and its readable properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Declaration order matters, because it is the order the runtime lays the struct out in, and
    /// the order a tool shows the fields in. Static and constant members are left out, since they
    /// belong to the type rather than to any entity carrying it, and so are private ones, which are
    /// the behavior's own working state and not reachable from the generated schema anyway.
    /// </para>
    /// <para>
    /// A property is described too, and read and written through itself rather than through
    /// whatever it is made of. Something worked out from two fields, something clamped on the way
    /// in, something stored in one unit and shown in another. All of those are the property's own
    /// business, and a tool that went round it would show a number nothing else in the program
    /// agrees with. One without a setter is described as read only, which is exactly what it is.
    /// </para>
    /// </remarks>
    /// <param name="type">The type whose members are described.</param>
    /// <param name="managed">
    /// Whether the type lives on the managed side, as a data asset does, so a string, a
    /// <c>List</c> and a <c>Dictionary</c> are fields a tool can edit rather than ones it cannot.
    /// </param>
    internal static IReadOnlyList<BehaviorField> ReadFields(INamedTypeSymbol type, bool managed = false) =>
    [
        .. type.GetMembers()
            .Where(member => member is IFieldSymbol or IPropertySymbol)
            .SelectMany(member => Expand(member, string.Empty, [], null, 0, managed)),
    ];

    /// <summary>
    /// One member as the fields a tool sees, which for a value of its own is more than one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A component's field may itself be a struct with fields: a spring with a stiffness and a
    /// damping, a slot with a name and a weight. Described as one opaque row it says nothing, and
    /// there is nothing a tool could do with it. So it is taken apart, and the parts are ordinary
    /// fields whose names carry the path they came from.
    /// </para>
    /// <para>
    /// The parts land in a fold named after the field they came from, so a struct inside a
    /// component reads as one thing that opens and shuts rather than as a run of unrelated rows.
    /// Nesting goes as deep as the types do, up to a limit, since a struct cannot contain itself
    /// and the recursion ends on its own.
    /// </para>
    /// <para>
    /// Fields only. A property returns a copy of what it holds, so writing one part of what a
    /// property answered with writes to the copy and nothing happens.
    /// </para>
    /// <para>
    /// A <c>[FormerName]</c> on a struct field renames every path under it, so each part carries
    /// every whole path it had before, made of its own names under every name each struct above it
    /// had. A reader then looks each one up as it is, without knowing which level was renamed.
    /// </para>
    /// </remarks>
    private static IEnumerable<BehaviorField> Expand(
        ISymbol member, string prefix, IReadOnlyList<string> formerPrefixes, string? fold, int depth, bool managed)
    {
        if (Described(member, managed) is not { } described) yield break;

        var named = prefix + described.Name;
        var inside = Inside(fold, described.Hints.Foldout);

        string[] names = [described.Name, .. described.Hints.FormerNames.Items];
        var former = new[] { prefix }.Concat(formerPrefixes)
            .SelectMany(above => names.Select(name => above + name))
            .Where(path => path != named)
            .Distinct()
            .ToArray();

        if (member is IFieldSymbol field
            && depth < Depth
            && !described.Hints.Hidden
            && Parts(field.Type) is { Count: > 0 } parts)
        {
            var under = Inside(inside, described.Name);

            foreach (var part in parts)
            {
                var formerAbove = former.Select(path => path + ".").ToArray();
                foreach (var expanded in Expand(part, named + ".", formerAbove, under, depth + 1, managed))
                {
                    yield return expanded;
                }
            }

            yield break;
        }

        yield return described with
        {
            Name = named,
            Hints = described.Hints with
            {
                // What it is called is the last part of the path. The rest of the path is said by
                // the fold it sits in, and repeating it in every row inside would be reading the
                // same word four times down a column.
                Label = described.Hints.Label ?? described.Name,
                Foldout = inside,
                FormerNames = new EquatableArray<string>(former),
            },
        };
    }

    /// <summary>How many structs deep a field is taken apart.</summary>
    /// <remarks>
    /// Three. A struct cannot contain itself so this cannot run away, but a component whose fields
    /// are four levels of struct is one whose inspector nobody can read however it is drawn.
    /// </remarks>
    private const int Depth = 3;

    /// <summary>One fold path inside another.</summary>
    private static string? Inside(string? outer, string? inner) => (outer, inner) switch
    {
        (null or "", null or "") => null,
        (null or "", var only) => only,
        (var only, null or "") => only,
        var (above, below) => above + "/" + below,
    };

    /// <summary>
    /// The members of a value that is worth taking apart, or nothing when it is not.
    /// </summary>
    /// <remarks>
    /// A struct with fields of its own, that is not one of the shapes a tool already draws. A
    /// vector is three numbers and is drawn as a vector; taking it apart would replace one row
    /// somebody understands with three that say the same thing worse.
    /// </remarks>
    private static IReadOnlyList<ISymbol>? Parts(ITypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Struct) return null;
        if (type.SpecialType != SpecialType.None) return null;
        if (KindOf(type) != FieldKind.Opaque) return null;

        var parts = type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field =>
                !field.IsStatic
                && !field.IsConst
                && !field.IsImplicitlyDeclared
                && field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .ToList();

        return parts.Count > 0 ? parts : null;
    }

    /// <summary>How one member is described, or nothing when it is not shown at all.</summary>
    private static BehaviorField? Described(ISymbol member, bool managed) => member switch
    {
        IFieldSymbol field
            when !field.IsStatic
                && !field.IsConst
                && !field.IsImplicitlyDeclared
                && field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
            => Field(field.Name, field.Type, HintsOf(field), managed),

        IPropertySymbol property
            when !property.IsStatic
                && !property.IsIndexer
                && !property.IsImplicitlyDeclared
                && property.GetMethod is not null
                && property.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                && KindOf(property.Type) != FieldKind.Opaque
            => new BehaviorField(
                property.Name,
                KindOf(property.Type),
                property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                OptionsOf(property.Type),
                Settable(property) ? HintsOf(property) : HintsOf(property) with { ReadOnly = true },
                IsProperty: true),

        _ => null,
    };

    /// <summary>One field, described by its type, with a list's or a map's item kinds.</summary>
    private static BehaviorField Field(string name, ITypeSymbol type, FieldHintModel hints, bool managed)
    {
        var (key, item, held) = Collection(type, managed);
        var kind = KindOf(type, managed);

        // Items with fields of their own are described as a data asset is, a few levels deep at
        // most, since a class can hold a list of itself and the description would never end.
        var itemKind = item is null ? FieldKind.Opaque : ItemKindOf(item);
        var itemFields = EquatableArray<BehaviorField>.Empty;
        if (itemKind == FieldKind.Struct && item is INamedTypeSymbol named)
        {
            if (_itemDepth >= 3)
            {
                itemKind = FieldKind.Opaque;
            }
            else
            {
                _itemDepth++;
                try
                {
                    itemFields = new EquatableArray<BehaviorField>([.. ReadFields(named, managed: true)]);
                }
                finally
                {
                    _itemDepth--;
                }
            }
        }

        // A data reference offers the files of its own type, which the hint carries to the drawer.
        if (kind == FieldKind.Data && DataTarget(type) is { } target)
            hints = hints with { Asset = target.ToDisplayString() };

        return new BehaviorField(
            name,
            kind,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            OptionsOf(item ?? type),
            hints,
            ElementKind: itemKind,
            ElementType: item?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsHandle: IsStored(type),
            KeyKind: key is null ? FieldKind.Opaque : ItemKindOf(key),
            KeyType: key?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Collection: held,
            ItemFields: itemFields,
            ItemIsClass: item?.IsReferenceType == true,
            ItemName: item?.Name);
    }

    /// <summary>How many item types deep the description of a field's items has gone, on this thread.</summary>
    [ThreadStatic]
    private static int _itemDepth;

    /// <summary>
    /// Whether a type is a struct or a class made of fields of its own, which a list can hold as
    /// items with an editor and a JSON form.
    /// </summary>
    /// <remarks>
    /// A class needs a constructor with no parameters, since a new item is made at its defaults,
    /// and a field or a property to set, since one with nothing to set has nothing to show.
    /// </remarks>
    private static bool IsItemRecord(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || type.SpecialType != SpecialType.None) return false;
        if (KindOf(type) != FieldKind.Opaque) return false;

        if (type.TypeKind == TypeKind.Struct) return Parts(type) is not null;
        if (type.TypeKind != TypeKind.Class || named.IsAbstract || named.IsGenericType) return false;

        var constructed = named.InstanceConstructors.Any(constructor =>
            constructor.Parameters.Length == 0
            && constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal);

        return constructed && named.GetMembers().Any(member => member switch
        {
            IFieldSymbol field => !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared
                && !field.IsReadOnly && field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal,
            IPropertySymbol property => !property.IsStatic && property.SetMethod is { IsInitOnly: false }
                && property.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal,
            _ => false,
        });
    }

    /// <summary>
    /// The key and item types of a list or a map, and how it is held, or nothing for any other
    /// type.
    /// </summary>
    private static (ITypeSymbol? Key, ITypeSymbol? Item, string? Held) Collection(ITypeSymbol type, bool managed)
    {
        if (ListItem(type) is { } item) return (null, item, IsStored(type) ? "stored" : "inline");
        if (MapTypes(type) is { } map) return (map.Key, map.Value, "stored");

        if (managed && type is INamedTypeSymbol { IsGenericType: true } named
            && named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
        {
            if (named.Name == "List" && named.TypeArguments.Length == 1)
                return (null, named.TypeArguments[0], "managed");
            if (named.Name == "Dictionary" && named.TypeArguments.Length == 2)
                return (named.TypeArguments[0], named.TypeArguments[1], "managed");
        }

        return (null, null, null);
    }

    /// <summary>The data asset type a <c>DataRef</c> refers to, or nothing for any other type.</summary>
    private static ITypeSymbol? DataTarget(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "DataRef", TypeArguments.Length: 1 } named
        && named.ContainingNamespace?.ToDisplayString() == "Bevy"
            ? named.TypeArguments[0]
            : null;

    /// <summary>
    /// How a tool reads a field of this type, counting a string, a list and a dictionary on a type
    /// that lives on the managed side.
    /// </summary>
    private static FieldKind KindOf(ITypeSymbol type, bool managed)
    {
        if (managed && type.SpecialType == SpecialType.System_String) return FieldKind.String;

        var (key, _, held) = Collection(type, managed);
        if (held == "managed") return key is null ? FieldKind.List : FieldKind.Map;

        return KindOf(type);
    }

    /// <summary>Whether a property can be written from outside the type.</summary>
    private static bool Settable(IPropertySymbol property) =>
        property.SetMethod is { } setter
        && setter.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;

    /// <summary>What a member's attributes asked for, whichever kind of member it is.</summary>
    private static FieldHintModel HintsOf(IPropertySymbol property) => HintsOf(property.GetAttributes());

    /// <summary>
    /// What a field's attributes asked for.
    /// </summary>
    /// <remarks>
    /// Read here, at compile time, rather than reflected over at runtime. The attributes are
    /// matched by name so that a game can declare its own with the same names if it would rather
    /// not reference this assembly, and an attribute nothing recognizes is left alone.
    /// </remarks>
    private static FieldHintModel HintsOf(IFieldSymbol field) => HintsOf(field.GetAttributes());

    /// <inheritdoc cref="HintsOf(IFieldSymbol)"/>
    private static FieldHintModel HintsOf(ImmutableArray<AttributeData> attributes)
    {
        var hints = FieldHintModel.None;

        // Conditions and change notifications gather rather than replace, because several of either
        // can sit on one field, and the last one written is not the only one meant.
        var conditions = new List<ConditionModel>();
        var changed = new List<string>();
        var former = new List<string>();

        foreach (var attribute in attributes)
        {
            var name = attribute.AttributeClass?.Name;
            if (name is null) continue;

            switch (name)
            {
                case RecognizedAttributes.ShowIf:
                    conditions.Add(new ConditionModel(
                        Text(attribute, 0) ?? string.Empty,
                        Written(attribute, 1),
                        Flag(attribute, "Not")));
                    continue;

                case RecognizedAttributes.HideIf:
                    conditions.Add(new ConditionModel(
                        Text(attribute, 0) ?? string.Empty, Written(attribute, 1), true));
                    continue;

                case RecognizedAttributes.OnValueChanged:
                    foreach (var method in Names(attribute)) changed.Add(method);
                    continue;

                case RecognizedAttributes.FormerName:
                    if (Text(attribute, 0) is { Length: > 0 } was) former.Add(was);
                    continue;
            }

            hints = name switch
            {
                RecognizedAttributes.Label => hints with { Label = Text(attribute, 0) },
                RecognizedAttributes.Tooltip => hints with { Tooltip = Text(attribute, 0) },
                RecognizedAttributes.Header => hints with { Header = Text(attribute, 0) },
                RecognizedAttributes.Unit => hints with { Unit = Text(attribute, 0) },
                RecognizedAttributes.Range => hints with
                {
                    Minimum = Number(attribute, 0),
                    Maximum = Number(attribute, 1),
                    Readout = Chosen(attribute, "Readout"),
                },
                RecognizedAttributes.Step => hints with { Step = Number(attribute, 0) },
                RecognizedAttributes.ReadOnly => hints with { ReadOnly = true },
                RecognizedAttributes.Hidden => hints with { Hidden = true },
                RecognizedAttributes.Space => hints with { Space = true },
                RecognizedAttributes.Separator => hints with { Separator = true },
                RecognizedAttributes.Color => hints with { Color = true },
                RecognizedAttributes.Wide => hints with { Wide = true },
                RecognizedAttributes.Inline => hints with { Inline = true },
                RecognizedAttributes.Foldout => hints with
                {
                    Foldout = Text(attribute, 0),
                    FoldoutShut = attribute.NamedArguments.Any(
                        pair => pair.Key == "Open" && pair.Value.Value is false),
                },
                RecognizedAttributes.Info => hints with
                {
                    Note = Text(attribute, 0),
                    NoteKind = Chosen(attribute, "Kind"),
                },
                RecognizedAttributes.Order => hints with { Order = (int)(Number(attribute, 0) ?? 0d) },
                RecognizedAttributes.Asset => hints with
                {
                    Asset = Text(attribute, 0),
                    Extensions = Named(attribute, "Extensions"),
                },
                _ => hints,
            };
        }

        if (conditions.Count > 0)
            hints = hints with { Conditions = new EquatableArray<ConditionModel>([.. conditions]) };

        if (changed.Count > 0)
            hints = hints with { Changed = new EquatableArray<string>([.. changed]) };

        if (former.Count > 0)
            hints = hints with { FormerNames = new EquatableArray<string>([.. former]) };

        return hints;
    }

    /// <summary>
    /// One of an attribute's positional arguments, written as the source wrote it.
    /// </summary>
    /// <remarks>
    /// What a condition compares against, which can be a flag, a number, a word or one of an enum's
    /// names. An enum argument arrives as the number behind the name, so the name is taken from the
    /// type rather than from the value, because a condition written against a name has to be
    /// checked against one, since the number is not what the field reads as at runtime.
    /// </remarks>
    private static string? Written(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index) return null;

        var argument = attribute.ConstructorArguments[index];

        // A boxed object argument arrives wrapped, so the declared type is object and the value it
        // holds is the one written.
        if (argument.Kind == TypedConstantKind.Type) return null;
        if (argument.Value is null) return null;

        if (argument.Type is { TypeKind: TypeKind.Enum } enumeration)
        {
            foreach (var member in enumeration.GetMembers())
            {
                if (member is not IFieldSymbol { HasConstantValue: true } constant) continue;
                if (!Equals(constant.ConstantValue, argument.Value)) continue;

                return constant.Name;
            }
        }

        return argument.Value switch
        {
            bool flag => flag ? "true" : "false",
            string text => text,
            var other => System.Convert.ToString(
                other, System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    /// <summary>The name of an enum passed as a named argument, or nothing.</summary>
    private static string? Chosen(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != name) continue;
            if (argument.Value.Type is not { TypeKind: TypeKind.Enum } enumeration) continue;

            foreach (var member in enumeration.GetMembers())
            {
                if (member is not IFieldSymbol { HasConstantValue: true } constant) continue;
                if (!Equals(constant.ConstantValue, argument.Value.Value)) continue;

                return constant.Name;
            }
        }

        return null;
    }

    /// <summary>Every string in an attribute's one array argument.</summary>
    private static IEnumerable<string> Names(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0) yield break;

        foreach (var value in attribute.ConstructorArguments[0].Values)
        {
            if (value.Value is string text and { Length: > 0 }) yield return text;
        }
    }

    /// <summary>One of an attribute's named arguments, as text.</summary>
    private static string? Named(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is string value) return value;
        }

        return null;
    }

    /// <summary>What a method's attributes asked for.</summary>
    private static MethodHintModel MethodHintsOf(IMethodSymbol method)
    {
        var hints = MethodHintModel.None;

        foreach (var attribute in method.GetAttributes())
        {
            var name = attribute.AttributeClass?.Name;
            if (name is null) continue;

            hints = name switch
            {
                RecognizedAttributes.Button => hints with
                {
                    Label = Text(attribute, 0),
                    Line = Chosen(attribute, "Line"),
                    Weight = Weight(attribute),
                },
                RecognizedAttributes.Label => hints with { Label = Text(attribute, 0) },
                RecognizedAttributes.Tooltip => hints with { Tooltip = Text(attribute, 0) },
                RecognizedAttributes.Hidden => hints with { Hidden = true },
                RecognizedAttributes.Space => hints with { Space = true },
                RecognizedAttributes.Separator => hints with { Separator = true },
                RecognizedAttributes.Header => hints with { Header = Text(attribute, 0) },
                RecognizedAttributes.Foldout => hints with
                {
                    Foldout = Text(attribute, 0),
                    FoldoutShut = attribute.NamedArguments.Any(
                        pair => pair.Key == "Open" && pair.Value.Value is false),
                },
                RecognizedAttributes.Info => hints with
                {
                    Note = Text(attribute, 0),
                    NoteKind = Chosen(attribute, "Kind"),
                },
                RecognizedAttributes.Order => hints with { Order = (int)(Number(attribute, 0) ?? 0d) },
                _ => hints,
            };
        }

        return hints;
    }

    /// <summary>How wide a button asked to be, which is one unless it said otherwise.</summary>
    private static double Weight(AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != "Weight") continue;

            return argument.Value.Value switch
            {
                double value => value,
                float value => value,
                int value => value,
                _ => 1d,
            };
        }

        return 1d;
    }

    /// <summary>One of an attribute's positional arguments, as text.</summary>
    private static string? Text(AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as string
            : null;

    /// <summary>One of an attribute's positional arguments, as a number.</summary>
    private static double? Number(AttributeData attribute, int index)
    {
        if (attribute.ConstructorArguments.Length <= index) return null;

        return attribute.ConstructorArguments[index].Value switch
        {
            double value => value,
            float value => value,
            int value => value,
            long value => value,
            _ => null,
        };
    }

    /// <summary>One of an attribute's named arguments, as a flag.</summary>
    private static bool Flag(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is bool value) return value;
        }

        return false;
    }

    /// <summary>
    /// The behavior's own methods that take nothing and return nothing.
    /// </summary>
    /// <remarks>
    /// Instance methods only, and only the ordinary ones, because a property's getter is a method
    /// too, and a static method is not a thing an entity can be told to do. Anything carrying a
    /// stage attribute is a system, which runs on its own schedule rather than when somebody asks.
    /// </remarks>
    private static IReadOnlyList<BehaviorInvokable> ReadInvokables(INamedTypeSymbol type) =>
    [
        .. type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method =>
                !method.IsStatic
                && method.MethodKind == MethodKind.Ordinary
                && method.DeclaredAccessibility == Accessibility.Public
                && method.Parameters.Length == 0
                && method.ReturnsVoid
                && GetStages(method).Count == 0
                && GetStateEdge(method) is null)
            .Select(method => new BehaviorInvokable(method.Name, MethodHintsOf(method))),
    ];

    /// <summary>Whether an enum's values are bits rather than a choice of one.</summary>
    private static bool IsFlags(ITypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == RecognizedAttributes.Flags) return true;
        }

        return false;
    }

    /// <summary>The names an enum field can take, in declaration order, or nothing.</summary>
    private static EquatableArray<string> OptionsOf(ITypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Enum) return EquatableArray<string>.Empty;

        // A flags enum's zero is not one of the things that can be on. It is the name for none of
        // them, and a row offering to turn it on would be a row that turns the others off.
        var bits = IsFlags(type);

        return new EquatableArray<string>([
            .. type.GetMembers().OfType<IFieldSymbol>()
                .Where(member => member.IsConst && !(bits && IsZero(member)))
                .Select(member => member.Name),
        ]);
    }

    /// <summary>Whether an enum member stands for no bits at all.</summary>
    private static bool IsZero(IFieldSymbol member) => member.ConstantValue switch
    {
        int value => value == 0,
        long value => value == 0L,
        uint value => value == 0u,
        ulong value => value == 0ul,
        short value => value == 0,
        ushort value => value == 0,
        byte value => value == 0,
        sbyte value => value == 0,
        _ => false,
    };

    /// <summary>
    /// The item type of one of the library's lists, inline or stored, or nothing for any other
    /// type.
    /// </summary>
    /// <remarks>
    /// Matched by name and namespace, as the attributes are, and every inline capacity at once,
    /// since the capacity is part of the type's name rather than an argument of it.
    /// </remarks>
    private static ITypeSymbol? ListItem(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } named
        && (named.Name.StartsWith("InlineList", StringComparison.Ordinal) || named.Name == "EcsList")
        && named.ContainingNamespace?.ToDisplayString() == "Bevy"
            ? named.TypeArguments[0]
            : null;

    /// <summary>The key and value types of the library's stored map, or nothing for any other type.</summary>
    private static (ITypeSymbol Key, ITypeSymbol Value)? MapTypes(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "EcsMap", TypeArguments.Length: 2 } named
        && named.ContainingNamespace?.ToDisplayString() == "Bevy"
            ? (named.TypeArguments[0], named.TypeArguments[1])
            : null;

    /// <summary>
    /// Whether a type is a handle into the managed store, which has to be freed when the component
    /// holding it goes.
    /// </summary>
    private static bool IsStored(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "EcsList" or "EcsMap" } named
        && named.ContainingNamespace?.ToDisplayString() == "Bevy";

    /// <summary>
    /// The kind of a list's items, which may be text, as no field of a component can be, since a
    /// stored list lives on the managed side.
    /// </summary>
    private static FieldKind ItemKindOf(ITypeSymbol item) =>
        item.SpecialType == SpecialType.System_String ? FieldKind.String
        : IsItemRecord(item) ? FieldKind.Struct
        : KindOf(item);

    /// <summary>How a tool should read and draw a field of this type.</summary>
    private static FieldKind KindOf(ITypeSymbol type) => ListItem(type) is not null
        ? FieldKind.List
        : MapTypes(type) is not null
        ? FieldKind.Map
        : DataTarget(type) is not null
        ? FieldKind.Data
        : type.TypeKind == TypeKind.Enum
        ? IsFlags(type) ? FieldKind.Flags : FieldKind.Enum
        : type.SpecialType switch
    {
        SpecialType.System_Boolean => FieldKind.Bool,
        SpecialType.System_Single => FieldKind.Float,
        SpecialType.System_Double => FieldKind.Double,
        // Every width, as the kind says. A field a tool cannot edit because nobody listed its
        // width is a field somebody has to write a drawer for, and there is nothing to write,
        // since it is a whole number.
        SpecialType.System_Int32 or SpecialType.System_Int16 or SpecialType.System_SByte
            or SpecialType.System_UInt32 or SpecialType.System_UInt16 or SpecialType.System_Byte
            or SpecialType.System_Int64 or SpecialType.System_UInt64
            => FieldKind.Int,
        _ => type.ToDisplayString() switch
        {
            "Bevy.Vec2" => FieldKind.Vec2,
            "Bevy.Vec3" => FieldKind.Vec3,
            "Bevy.Vec4" => FieldKind.Vec4,
            "Bevy.Color" => FieldKind.Color,
            "Bevy.Quat" => FieldKind.Quat,
            "Bevy.Entity" => FieldKind.Entity,
            "Bevy.AssetHandle" => FieldKind.Asset,
            _ => FieldKind.Opaque,
        },
    };

    /// <summary>The model for one struct, plus anything wrong with it.</summary>
    private sealed record ExtractResult(BehaviorModel? Model, ImmutableArray<Diagnostic> Diagnostics);

    /// <summary>Reads one <c>[Behavior]</c> struct into a model, validating as it goes.</summary>
    private static ExtractResult? Extract(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol type) return null;
        if (context.TargetNode is not StructDeclarationSyntax declaration) return null;

        token.ThrowIfCancellationRequested();

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var isPartial = declaration.Modifiers.Any(m => m.ValueText == "partial");
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                BehaviorDiagnostics.NotPartial, declaration.Identifier.GetLocation(), type.Name));
        }

        var methods = new List<StageMethod>();
        var hasInstanceMethod = false;

        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
        {
            token.ThrowIfCancellationRequested();

            var stages = GetStages(member);
            var edge = GetStateEdge(member);

            if (stages.Count == 0 && edge is null) continue;

            // A transition is not a stage, so asking for both says two different things about
            // when the method runs.
            if (stages.Count > 1 || (stages.Count > 0 && edge is not null))
            {
                diagnostics.Add(Diagnostic.Create(
                    BehaviorDiagnostics.MultipleStages, member.Locations.FirstOrDefault(), member.Name));
                continue;
            }

            if (!HasSystemSignature(member))
            {
                diagnostics.Add(Diagnostic.Create(
                    BehaviorDiagnostics.BadSignature, member.Locations.FirstOrDefault(), member.Name));
                continue;
            }

            if (!member.IsStatic) hasInstanceMethod = true;

            methods.Add(new StageMethod
            {
                Stage = stages.Count > 0 ? stages[0] : BehaviorStage.Startup,
                Edge = edge,
                Name = member.Name,
                IsStatic = member.IsStatic,
                Filters = GetFilters(member, diagnostics),
                Condition = GetCondition(member, type, diagnostics),
                Toggle = GetToggle(member),
                InState = GetInState(member),
            });
        }

        var fields = ReadFields(type);

        // A behavior with fields and no methods is a plain data component, and one with neither is
        // a marker, such as a wall a game's script finds by it. Both are described for the editor
        // and a scene, so neither is warned about.

        // Only instance methods force the struct into Bevy's storage, so only they require it
        // to be blittable. A behavior with static methods alone is a system holder.
        if (hasInstanceMethod && !type.IsUnmanagedType)
        {
            diagnostics.Add(Diagnostic.Create(
                BehaviorDiagnostics.BehaviorNotUnmanaged,
                declaration.Identifier.GetLocation(),
                type.Name));
        }

        var model = new BehaviorModel
        {
            Namespace = type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString(),
            Name = type.Name,
            QualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Methods = methods,
            Fields = fields,
            FormerNames = FormerNamesOf(type),
            Version = VersionOf(type),
            Migrates = Migration(type) is not null,
            Persisted = type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == RecognizedAttributes.Persist),
            Invokables = ReadInvokables(type),
        };

        return new ExtractResult(model, diagnostics.ToImmutable());
    }

    /// <summary>The version a type's <c>[DataVersion]</c> gives it, or zero for none.</summary>
    private static int VersionOf(INamedTypeSymbol type) =>
        type.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.Name == RecognizedAttributes.DataVersion)
            .Select(attribute => attribute.ConstructorArguments.FirstOrDefault().Value)
            .OfType<int>()
            .FirstOrDefault();

    /// <summary>
    /// A type's <c>static JsonObject Migrate(int from, JsonObject value)</c>, or nothing when it has
    /// none of that shape.
    /// </summary>
    /// <remarks>
    /// Public or internal, because the schema that calls it is registered from a class of the
    /// generator's own, outside the type.
    /// </remarks>
    private static IMethodSymbol? Migration(INamedTypeSymbol type) =>
        type.GetMembers("Migrate").OfType<IMethodSymbol>().FirstOrDefault(method =>
            method.IsStatic
            && method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
            && method.Parameters.Length == 2
            && method.Parameters[0].Type.SpecialType == SpecialType.System_Int32
            && IsJsonObject(method.Parameters[1].Type)
            && IsJsonObject(method.ReturnType));

    /// <summary>Whether a type is <c>System.Text.Json.Nodes.JsonObject</c>.</summary>
    private static bool IsJsonObject(ITypeSymbol type) =>
        type.Name == "JsonObject" && type.ContainingNamespace?.ToDisplayString() == "System.Text.Json.Nodes";

    /// <summary>The names a type's <c>[FormerName]</c> attributes say it had.</summary>
    private static IReadOnlyList<string> FormerNamesOf(INamedTypeSymbol type) =>
    [
        .. type.GetAttributes()
            .Where(attribute => attribute.AttributeClass?.Name == RecognizedAttributes.FormerName)
            .Select(attribute => Text(attribute, 0))
            .OfType<string>(),
    ];

    /// <summary>The stages named by a method's attributes.</summary>
    private static List<BehaviorStage> GetStages(IMethodSymbol method)
    {
        var stages = new List<BehaviorStage>();

        foreach (var attribute in method.GetAttributes())
        {
            var stage = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.{RecognizedAttributes.OnStartup}" => BehaviorStage.Startup,
                $"{AttributeNamespace}.{RecognizedAttributes.OnFirst}" => BehaviorStage.First,
                $"{AttributeNamespace}.{RecognizedAttributes.OnPreUpdate}" => BehaviorStage.PreUpdate,
                $"{AttributeNamespace}.{RecognizedAttributes.OnFixedUpdate}" => BehaviorStage.FixedUpdate,
                $"{AttributeNamespace}.{RecognizedAttributes.OnUpdate}" => BehaviorStage.Update,
                $"{AttributeNamespace}.{RecognizedAttributes.OnPostUpdate}" => BehaviorStage.PostUpdate,
                $"{AttributeNamespace}.{RecognizedAttributes.OnRender}" => BehaviorStage.Render,
                $"{AttributeNamespace}.{RecognizedAttributes.OnLast}" => BehaviorStage.Last,
                $"{AttributeNamespace}.{RecognizedAttributes.OnCleanup}" => BehaviorStage.Cleanup,
                _ => (BehaviorStage?)null,
            };

            if (stage is { } value) stages.Add(value);
        }

        return stages;
    }

    /// <summary>True when the method looks like <c>void M(BehaviorContext ctx)</c>.</summary>
    private static bool HasSystemSignature(IMethodSymbol method) =>
        method.ReturnsVoid
        && method.Parameters.Length == 1
        && method.Parameters[0].Type.ToDisplayString() == $"{AttributeNamespace}.BehaviorContext";

    /// <summary>Reads the With/Without/Changed filters off a method.</summary>
    private static BehaviorFilters GetFilters(
        IMethodSymbol method,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var with = new List<string>();
        var without = new List<string>();
        var changed = new List<string>();

        foreach (var attribute in method.GetAttributes())
        {
            var bucket = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.{RecognizedAttributes.With}" => with,
                $"{AttributeNamespace}.{RecognizedAttributes.Without}" => without,
                $"{AttributeNamespace}.{RecognizedAttributes.Changed}" => changed,
                _ => null,
            };

            if (bucket is null || attribute.ConstructorArguments.Length == 0) continue;

            foreach (var value in attribute.ConstructorArguments[0].Values)
            {
                if (value.Value is not ITypeSymbol typeSymbol) continue;

                if (!typeSymbol.IsUnmanagedType)
                {
                    diagnostics.Add(Diagnostic.Create(
                        BehaviorDiagnostics.FilterNotUnmanaged,
                        method.Locations.FirstOrDefault(),
                        typeSymbol.Name));
                    continue;
                }

                bucket.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return with.Count == 0 && without.Count == 0 && changed.Count == 0
            ? BehaviorFilters.None
            : new BehaviorFilters(with, without, changed);
    }

    /// <summary>Reads an <c>[OnEnter]</c> or <c>[OnExit]</c> attribute.</summary>
    private static StateEdgeInfo? GetStateEdge(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var entering = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.{RecognizedAttributes.OnEnter}" => true,
                $"{AttributeNamespace}.{RecognizedAttributes.OnExit}" => false,
                _ => (bool?)null,
            };

            if (entering is not { } edge || attribute.ConstructorArguments.Length == 0) continue;

            var argument = attribute.ConstructorArguments[0];
            if (argument.Type is not INamedTypeSymbol { EnumUnderlyingType: not null } enumType)
                continue;
            if (argument.Value is null) continue;

            return new StateEdgeInfo(
                enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                argument.Value.ToString(),
                edge);
        }

        return null;
    }

    /// <summary>Reads an <c>[InState]</c> attribute into the enum type and value it names.</summary>
    /// <remarks>
    /// The argument is typed as <c>object</c> so any enum can be passed, which means the enum type
    /// arrives on the constant rather than on the parameter. Emitting a cast back to that type lets
    /// the condition infer its type parameter.
    /// </remarks>
    private static InStateInfo? GetInState(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.{RecognizedAttributes.InState}")
                continue;

            if (attribute.ConstructorArguments.Length == 0) continue;

            var argument = attribute.ConstructorArguments[0];
            if (argument.Type is not INamedTypeSymbol { EnumUnderlyingType: not null } enumType)
                continue;
            if (argument.Value is null) continue;

            return new InStateInfo(
                enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                argument.Value.ToString());
        }

        return null;
    }

    /// <summary>Resolves a <c>[RunIf]</c> attribute against the behavior's own members.</summary>
    private static ConditionInfo? GetCondition(
        IMethodSymbol method,
        INamedTypeSymbol type,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? name = null;

        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.{RecognizedAttributes.RunIf}")
                continue;
            if (attribute.ConstructorArguments.Length == 0) continue;
            if (attribute.ConstructorArguments[0].Value is string value) name = value;
            break;
        }

        if (name is null) return null;

        foreach (var member in type.GetMembers(name))
        {
            if (!member.IsStatic) continue;

            switch (member)
            {
                case IMethodSymbol: return new ConditionInfo(name, ConditionKind.Method);
                case IPropertySymbol: return new ConditionInfo(name, ConditionKind.Property);
                case IFieldSymbol: return new ConditionInfo(name, ConditionKind.Field);
            }
        }

        diagnostics.Add(Diagnostic.Create(
            BehaviorDiagnostics.UnknownCondition,
            method.Locations.FirstOrDefault(),
            name,
            method.Name));

        return null;
    }

    /// <summary>Reads a <c>[ToggleKey]</c> attribute off a method.</summary>
    /// <remarks>
    /// The modifier argument is a flags enum, so a combination such as
    /// <c>KeyModifier.Ctrl | KeyModifier.Shift</c> arrives as a single folded constant. There is
    /// nothing extra to do here to support several modifiers at once.
    /// </remarks>
    private static ToggleKeyInfo? GetToggle(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.{RecognizedAttributes.ToggleKey}")
                continue;

            var arguments = attribute.ConstructorArguments;
            var key = arguments.Length > 0 && arguments[0].Value is int k ? k : 0;
            var modifiers = arguments.Length > 1 && arguments[1].Value is int m ? m : 0;

            var defaultEnabled = true;
            foreach (var named in attribute.NamedArguments)
                if (named.Key == "DefaultEnabled" && named.Value.Value is bool enabled)
                    defaultEnabled = enabled;

            return new ToggleKeyInfo(key, modifiers, defaultEnabled);
        }

        return null;
    }
}

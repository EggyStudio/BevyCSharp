using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bevy.Generator;

/// <summary>
/// Turns the checked-in description of Bevy's components into a typed wrapper per component.
/// </summary>
/// <remarks>
/// <para>
/// A reflected component is reached by string paths, which fail when they run rather than when
/// they compile. A wrapper such as <c>PointLightRef</c> has a property per field instead, so a field
/// Bevy renames is a compile error in the code that used it once the description is regenerated.
/// The properties still go through Bevy's reflection, so a wrapper costs what a string path costs.
/// </para>
/// <para>
/// The description is <c>bevy-components.tsv</c>, written by the <c>schema.dump</c> command from a
/// running app and named as an additional file by the library alone, so a game's own compilation
/// never sees it and emits nothing here. It is tab-separated lines rather than JSON because this
/// runs inside the compiler on netstandard2.0, where a JSON reader would have to be shipped with
/// the analyzer.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed partial class ReflectedGenerator : IIncrementalGenerator
{
    private const string FileName = "bevy-components.tsv";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var components = context.AdditionalTextsProvider
            .Where(static file =>
                file.Path.EndsWith("/" + FileName, StringComparison.Ordinal)
                || file.Path.EndsWith("\\" + FileName, StringComparison.Ordinal)
                || file.Path == FileName)
            .Select(static (file, token) => Parse(file.GetText(token)?.ToString() ?? string.Empty));

        // The names the library already gives types in the Bevy namespace, which a union of the
        // same name would make ambiguous in code that uses both namespaces.
        var taken = context.CompilationProvider.Select(static (compilation, _) =>
        {
            var bevy = compilation.GlobalNamespace.GetNamespaceMembers().FirstOrDefault(n => n.Name == "Bevy");
            var names = bevy is null ? new string[0] : bevy.GetTypeMembers().Select(t => t.Name).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToArray();
            return new EquatableArray<string>(names);
        });

        context.RegisterSourceOutput(components.Combine(taken), static (spc, pair) =>
        {
            // The wrappers' names are taken as well, since an enum inside a variant can be called
            // what a wrapper is, as Bevy's WindowRef and the wrapper of its Window are.
            var named = Named(pair.Left.Items.Where(c => !c.Item).ToList()).ToList();
            var items = pair.Left.Items.Where(c => c.Item).ToDictionary(c => c.Path, StringComparer.Ordinal);
            var unions = new Unions(new HashSet<string>(pair.Right.Items.Concat(named.Select(c => c.Type)), StringComparer.Ordinal));
            foreach (var component in named)
                spc.AddSource($"Reflected.{component.Type}.g.cs", Emit(component, items, unions));
            foreach (var union in unions.All)
                spc.AddSource($"Reflected.Union.{union.Name}.g.cs", EmitUnion(union));
        });
    }

    /// <summary>One field a wrapper can type.</summary>
    /// <param name="Name">The row's name, Rust names joined by dots.</param>
    /// <param name="Reflect">Bevy's reflect path.</param>
    /// <param name="Kind">The <c>FieldKind</c> it is read as.</param>
    /// <param name="Rust">The Rust type, which decides the width of a whole number.</param>
    /// <param name="Extra">An enum's variants, comma-separated, or a handle's asset kind.</param>
    private readonly record struct FieldModel(
        string Name, string Reflect, string Kind, string Rust, string Extra);

    /// <summary>One field of an enum's variant, there while that variant is held.</summary>
    /// <param name="Enum">The enum field's row name, as its <see cref="FieldModel.Name"/>.</param>
    /// <param name="Variant">The variant it belongs to.</param>
    /// <param name="Field">The part of the row's name after the variant's, empty for a variant wrapping one value.</param>
    /// <param name="Reflect">Bevy's reflect path, as its <see cref="FieldModel.Reflect"/>.</param>
    /// <param name="Kind">The <c>FieldKind</c> it is read as.</param>
    /// <param name="Rust">The Rust type.</param>
    /// <param name="Extra">An enum's variants or a handle's asset kind, as its <see cref="FieldModel.Extra"/>.</param>
    private readonly record struct VariantFieldModel(
        string Enum, string Variant, string Field, string Reflect, string Kind, string Rust, string Extra)
    {
        public FieldModel AsField => new(Field, Reflect, Kind, Rust, Extra);
    }

    /// <summary>One component, or one type a list holds as its items, as the description lists it.</summary>
    /// <param name="Path">Its Rust type path.</param>
    /// <param name="Short">Its short name.</param>
    /// <param name="Fields">Its rows.</param>
    /// <param name="Variants">The rows of its enums' variants.</param>
    /// <param name="Item">Whether it is a type a list holds as its items, which has no wrapper of its own.</param>
    private sealed record ComponentModel(
        string Path, string Short, EquatableArray<FieldModel> Fields, EquatableArray<VariantFieldModel> Variants, bool Item = false)
    {
        /// <summary>The wrapper's type name, settled once every component is known.</summary>
        public string Type { get; init; } = string.Empty;
    }

    /// <summary>Reads the description's lines into components, skipping comments.</summary>
    private static EquatableArray<ComponentModel> Parse(string text)
    {
        var components = new List<ComponentModel>();
        string? path = null;
        string? shortName = null;
        var item = false;
        var fields = new List<FieldModel>();
        var variants = new List<VariantFieldModel>();

        void Close()
        {
            if (path is not null)
            {
                components.Add(new ComponentModel(path, shortName!,
                    new EquatableArray<FieldModel>(fields.ToArray()),
                    new EquatableArray<VariantFieldModel>(variants.ToArray()),
                    item));
            }
            fields.Clear();
            variants.Clear();
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;

            var words = line.Split('\t');
            // A type a list holds as its items is described as a component is, after them.
            if (words[0] is "component" or "item" && words.Length >= 3)
            {
                Close();
                path = words[1];
                shortName = words[2];
                item = words[0] == "item";
            }
            else if (words[0] == "field" && words.Length >= 7 && words[1] == path)
            {
                fields.Add(new FieldModel(words[2], words[3], words[4], words[5], words[6]));
            }
            else if (words[0] == "variant" && words.Length >= 9 && words[1] == path)
            {
                variants.Add(new VariantFieldModel(words[2], words[3], words[4], words[5], words[6], words[7], words[8]));
            }
        }

        Close();
        return new EquatableArray<ComponentModel>(components.ToArray());
    }

    /// <summary>
    /// Gives every component a wrapper name, lengthening the ones two components would share.
    /// </summary>
    private static IEnumerable<ComponentModel> Named(IReadOnlyList<ComponentModel> components)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in components)
        {
            var name = Identifier(component.Short.Contains("::")
                ? Crate(component.Path) + "_" + Last(component.Path)
                : component.Short) + "Ref";

            var unique = name;
            for (var n = 2; !taken.Add(unique); n++) unique = name + n;

            yield return component with { Type = unique };
        }
    }

    /// <summary>The crate a type path starts with.</summary>
    private static string Crate(string path)
    {
        var end = path.IndexOf("::", StringComparison.Ordinal);
        return end < 0 ? path : path.Substring(0, end);
    }

    /// <summary>The last segment of a type path, generic arguments included.</summary>
    private static string Last(string path)
    {
        var open = path.IndexOf('<');
        var head = open < 0 ? path : path.Substring(0, open);
        var cut = head.LastIndexOf("::", StringComparison.Ordinal);
        return path.Substring(cut < 0 ? 0 : cut + 2);
    }

    /// <summary>
    /// A C# identifier in Pascal case from a Rust name: <c>shadow_depth_bias</c> becomes
    /// <c>ShadowDepthBias</c>, <c>MeshMaterial3d&lt;StandardMaterial&gt;</c> becomes
    /// <c>MeshMaterial3dStandardMaterial</c>.
    /// </summary>
    private static string Identifier(string name)
    {
        var text = new StringBuilder(name.Length);
        var upper = true;

        foreach (var c in name)
        {
            if (!char.IsLetterOrDigit(c))
            {
                upper = true;
                continue;
            }

            text.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }

        if (text.Length == 0 || char.IsDigit(text[0])) text.Insert(0, "Item");
        return text.ToString();
    }

    /// <summary>The Rust type Bevy reflects as one value with no fields, read and written whole as its two ends.</summary>
    private const string Range = "Range<f32>";

    /// <summary>The C# type a field's property has, or nothing for a kind a wrapper does not type.</summary>
    private static string? TypeOf(FieldModel field) => field.Rust == Range ? "global::Bevy.FloatRange" : field.Kind switch
    {
        "Float" => "float",
        "Double" => "double",
        "Bool" => "bool",
        "String" => "string",
        "Vec2" => "global::Bevy.Vec2",
        "Vec3" => "global::Bevy.Vec3",
        "Vec4" => "global::Bevy.Vec4",
        "Color" => "global::Bevy.Color",
        "Quat" => "global::Bevy.Quat",
        "Entity" => "global::Bevy.Entity",
        "Asset" => "global::Bevy.AssetHandle",
        "Int" => field.Rust switch
        {
            "u8" => "byte",
            "i8" => "sbyte",
            "u16" => "ushort",
            "i16" => "short",
            "u32" => "uint",
            "u64" or "usize" => "ulong",
            "i64" or "isize" => "long",
            _ => "int",
        },
        _ => null,
    };

    /// <summary>Text as it can stand inside an XML documentation comment.</summary>
    private static string Xml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>A name C# accepts as written, with <c>@</c> before one that is a keyword.</summary>
    private static string Escaped(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    /// <summary>The C# type a variant's field is held as, or nothing when a wrapper cannot type it.</summary>
    private static string? Typed(FieldModel field) => field.Kind switch
    {
        "Enum" => null,
        _ => TypeOf(field),
    };

    /// <summary>The expression reading a field a wrapper or a variant names, at a path given as a C# expression.</summary>
    private static string Reading(FieldModel field, string path) => field.Kind switch
    {
        _ when field.Rust == Range => $"global::Bevy.ReflectedValue.Range(_world, Entity, TypePath, {path})",
        "Color" => $"global::Bevy.ReflectedValue.Color(_world, Entity, TypePath, {path})",
        "Asset" => $"global::Bevy.ReflectedValue.Asset(_world, Entity, TypePath, {path})",
        _ => $"global::Bevy.ReflectedValue.Get<{TypeOf(field)}>(_world, Entity, TypePath, {path}, global::Bevy.FieldKind.{field.Kind})",
    };

    /// <summary>The statement writing a field a wrapper or a variant names, at a path given as a C# expression.</summary>
    private static string Writing(FieldModel field, string path, string value) => field.Kind switch
    {
        _ when field.Rust == Range => $"global::Bevy.ReflectedValue.SetRange(_world, Entity, TypePath, {path}, {value});",
        "Color" => $"_world.SetReflectedColor(Entity, TypePath, {path}, {value});",
        "Asset" => $"_world.SetReflectedAsset(Entity, TypePath, {path}, {value});",
        _ => $"global::Bevy.ReflectedValue.Set(_world, Entity, TypePath, {path}, global::Bevy.FieldKind.{field.Kind}, {value});",
    };

    /// <summary>A reflect path as it stands inside a C# string.</summary>
    private static string Quoted(string path) => path.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>Writes one wrapper.</summary>
    private static string Emit(ComponentModel component, IReadOnlyDictionary<string, ComponentModel> items, Unions unions)
    {
        var type = component.Type;
        var text = new StringBuilder();

        text.Append($$"""
            // <auto-generated/>
            #nullable enable

            namespace Bevy.Reflected;

            /// <summary>
            /// Bevy's <c>{{Xml(component.Short)}}</c>, read and written through Bevy's reflection.
            /// </summary>
            /// <remarks>
            /// Generated from <c>bevy-components.tsv</c>. Each property reads or writes one field of the
            /// component on <see cref="Entity"/> as it is now, and throws when the entity no longer
            /// carries it. Get one with <c>EcsWorld.Get</c> or <c>EcsWorld.Insert</c>.
            /// </remarks>
            public readonly struct {{type}} : global::Bevy.IReflectedComponent<{{type}}>, global::Bevy.IReflectedWrapper<{{type}}>
            {
                /// <summary>The component's full Rust type path.</summary>
                public const string TypePath = "{{component.Path}}";

                private readonly global::Bevy.EcsWorld _world;

                private {{type}}(global::Bevy.EcsWorld world, global::Bevy.Entity entity)
                {
                    _world = world;
                    Entity = entity;
                }

                /// <summary>The entity whose component this reads and writes.</summary>
                public global::Bevy.Entity Entity { get; }

                static string global::Bevy.IReflectedComponent<{{type}}>.TypePath => TypePath;

                static {{type}} global::Bevy.IReflectedComponent<{{type}}>.Create(
                    global::Bevy.EcsWorld world, global::Bevy.Entity entity) => new(world, entity);

                string global::Bevy.IReflectedWrapper<{{type}}>.ComponentPath => TypePath;

                {{type}} global::Bevy.IReflectedWrapper<{{type}}>.Over(
                    global::Bevy.EcsWorld world, global::Bevy.Entity entity) => new(world, entity);

                /// <summary>Takes the component off the entity, reporting whether it was there.</summary>
                public bool Remove() => _world.RemoveReflected(Entity, TypePath);

            """);

        var used = new HashSet<string>(StringComparer.Ordinal) { type, "Entity", "Remove", "TypePath" };
        var scope = new Scope(component.Variants.Items, items, unions);

        foreach (var field in component.Fields.Items)
        {
            var property = TypeOf(field);

            // A list of records is read and written by the methods of the record its items are.
            var list = field.Kind == "List" ? ListOf(field, "Value", scope) : null;
            if (property is null && field.Kind != "Enum" && list is null) continue;

            // A newtype's one field is called 0 by Rust, which says nothing as a property name.
            var name = field.Name == "0" ? "Value" : Identifier(field.Name);
            var unique = name;
            for (var n = 2; !used.Add(unique); n++) unique = name + n;

            var path = field.Reflect.Replace("\\", "\\\\").Replace("\"", "\\\"");
            text.Append($$"""

                    /// <summary>Bevy's <c>{{field.Name}}</c>.</summary>

                """);

            if (list is not null)
            {
                text.Append($$"""
                        public {{list.Type}} {{unique}}
                        {
                            get => {{Read(list, null)}};
                            set => {{Write(list, "value", 1, null)}}
                        }

                    """);
                continue;
            }

            var parts = component.Variants.Items.Where(p => p.Enum == field.Name).ToList();
            if (field.Kind == "Enum" && parts.Count > 0 && EmitHolding(text, field, path, unique, parts, scope))
                continue;

            switch (field.Kind)
            {
                case "Enum":
                    // Each with its line of documentation, since the library treats a public member
                    // without one as an error (NORM.md, N 2.2) and these are the library's.
                    var variants = field.Extra.Split(',').Where(v => v.Length > 0)
                        .Select(v => $"/// <summary>The variant <c>{Xml(Last(field.Rust))}::{Xml(v)}</c>.</summary>\n        {Escaped(v)},");
                    var named = unique + "Variant";
                    used.Add(named);
                    text.Append($$"""
                            public {{named}} {{unique}}
                            {
                                get => global::System.Enum.Parse<{{named}}>(
                                    global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}"));
                                set => _world.SetVariant(Entity, TypePath, "{{path}}", value.ToString());
                            }

                            /// <summary>The variants <see cref="{{unique}}"/> can hold.</summary>
                            public enum {{named}}
                            {
                                {{string.Join("\n\n        ", variants)}}
                            }

                        """);
                    break;

                case "Vec2" when field.Rust == Range:
                    text.Append($$"""
                            public global::Bevy.FloatRange {{unique}}
                            {
                                get => {{Reading(field, $"\"{path}\"")}};
                                set => {{Writing(field, $"\"{path}\"", "value")}}
                            }

                        """);
                    break;

                case "Color":
                    text.Append($$"""
                            public global::Bevy.Color {{unique}}
                            {
                                get => global::Bevy.ReflectedValue.Color(_world, Entity, TypePath, "{{path}}");
                                set => _world.SetReflectedColor(Entity, TypePath, "{{path}}", value);
                            }

                        """);
                    break;

                case "Asset":
                    text.Append($$"""
                            public global::Bevy.AssetHandle {{unique}}
                            {
                                get => global::Bevy.ReflectedValue.Asset(_world, Entity, TypePath, "{{path}}");
                                set => _world.SetReflectedAsset(Entity, TypePath, "{{path}}", value);
                            }

                        """);
                    break;

                default:
                    text.Append($$"""
                            public {{property}} {{unique}}
                            {
                                get => global::Bevy.ReflectedValue.Get<{{property}}>(
                                    _world, Entity, TypePath, "{{path}}", global::Bevy.FieldKind.{{field.Kind}});
                                set => global::Bevy.ReflectedValue.Set(
                                    _world, Entity, TypePath, "{{path}}", global::Bevy.FieldKind.{{field.Kind}}, value);
                            }

                        """);
                    break;
            }
        }

        text.Append("}\n");
        return text.ToString();
    }
}

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
public sealed class ReflectedGenerator : IIncrementalGenerator
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
            var unions = new Unions(new HashSet<string>(pair.Right.Items, StringComparer.Ordinal));
            foreach (var component in Named(pair.Left.Items))
                spc.AddSource($"Reflected.{component.Type}.g.cs", Emit(component, unions));
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
    private readonly record struct VariantFieldModel(
        string Enum, string Variant, string Field, string Reflect, string Kind, string Rust, string Extra)
    {
        public FieldModel AsField => new(Field, Reflect, Kind, Rust, Extra);
    }

    /// <summary>One component, as the description lists it.</summary>
    private sealed record ComponentModel(
        string Path, string Short, EquatableArray<FieldModel> Fields, EquatableArray<VariantFieldModel> Variants)
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
        var fields = new List<FieldModel>();
        var variants = new List<VariantFieldModel>();

        void Close()
        {
            if (path is not null)
            {
                components.Add(new ComponentModel(path, shortName!,
                    new EquatableArray<FieldModel>(fields.ToArray()),
                    new EquatableArray<VariantFieldModel>(variants.ToArray())));
            }
            fields.Clear();
            variants.Clear();
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') continue;

            var words = line.Split('\t');
            if (words[0] == "component" && words.Length >= 3)
            {
                Close();
                path = words[1];
                shortName = words[2];
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

    /// <summary>The C# type a field's property has, or nothing for a kind a wrapper does not type.</summary>
    private static string? TypeOf(FieldModel field) => field.Kind switch
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

    /// <summary>The expression reading a field a wrapper or a variant names.</summary>
    private static string Reading(FieldModel field, string path) => field.Kind switch
    {
        "Color" => $"global::Bevy.ReflectedValue.Color(_world, Entity, TypePath, \"{path}\")",
        "Asset" => $"global::Bevy.ReflectedValue.Asset(_world, Entity, TypePath, \"{path}\")",
        _ => $"global::Bevy.ReflectedValue.Get<{TypeOf(field)}>(_world, Entity, TypePath, \"{path}\", global::Bevy.FieldKind.{field.Kind})",
    };

    /// <summary>The statement writing a field a wrapper or a variant names.</summary>
    private static string Writing(FieldModel field, string path, string value) => field.Kind switch
    {
        "Color" => $"_world.SetReflectedColor(Entity, TypePath, \"{path}\", {value});",
        "Asset" => $"_world.SetReflectedAsset(Entity, TypePath, \"{path}\", {value});",
        _ => $"global::Bevy.ReflectedValue.Set(_world, Entity, TypePath, \"{path}\", global::Bevy.FieldKind.{field.Kind}, {value});",
    };

    /// <summary>A reflect path as it stands inside a C# string.</summary>
    private static string Quoted(string path) => path.Replace("\\", "\\\\").Replace("\"", "\\\"");

    /// <summary>One value of a variant's record, or of a struct's.</summary>
    private sealed record Part(string Name, string Type, FieldModel Field);

    /// <summary>One variant of a union, the record it is and what that record holds.</summary>
    private sealed record Case(string Variant, string Record, List<Part> Parts);

    /// <summary>
    /// A record the generator emits for Bevy's types, the abstract one an enum whose variants hold
    /// values becomes, or the one a struct inside an <c>Option</c> becomes.
    /// </summary>
    /// <param name="Name">Its C# name.</param>
    /// <param name="Rust">Bevy's name for the type, which its documentation gives.</param>
    /// <param name="Cases">Its variants, or for a struct the one case holding its fields.</param>
    /// <param name="Struct">Whether it is a struct's record rather than a union.</param>
    private sealed record UnionModel(string Name, string Rust, List<Case> Cases, bool Struct)
    {
        /// <summary>What two uses of the same Rust type have to agree on to share one record.</summary>
        public string Signature => string.Join(";", Cases.Select(c => c.Variant + "(" + string.Join(",", c.Parts.Select(p => p.Name + ":" + p.Type)) + ")"));
    }

    /// <summary>
    /// The records emitted so far, one a Rust type, so a type two components hold is one record.
    /// </summary>
    private sealed class Unions(HashSet<string> taken)
    {
        private readonly Dictionary<string, UnionModel> _byName = new(StringComparer.Ordinal);

        public IEnumerable<UnionModel> All => _byName.Values;

        /// <summary>The record for a Rust type, the one already made where it agrees.</summary>
        public UnionModel Add(string rust, List<Case> cases, bool isStruct)
        {
            var name = Identifier(Last(rust));
            if (taken.Contains(name)) name += "Value";

            var made = new UnionModel(name, rust, cases, isStruct);
            for (var n = 2; ; n++)
            {
                if (!_byName.TryGetValue(made.Name, out var existing))
                {
                    _byName[made.Name] = made;
                    return made;
                }
                if (existing.Signature == made.Signature) return existing;
                made = made with { Name = name + n };
            }
        }
    }

    /// <summary>A record's parameter name for a variant's field, which is never its own type's name.</summary>
    private static string PartName(string field, string record)
    {
        var name = field.Length == 0 ? "Value" : Identifier(field);
        return name == record ? name + "Value" : name;
    }

    /// <summary>
    /// The variants of an enum field as records, or nothing when a variant holds a value a wrapper
    /// cannot type, which leaves the field the plain enum of variant names it was.
    /// </summary>
    private static List<Case>? CasesOf(FieldModel field, IEnumerable<VariantFieldModel> parts)
    {
        var cases = new List<Case>();
        foreach (var variant in field.Extra.Split(',').Where(v => v.Length > 0))
        {
            var record = Identifier(variant);
            var held = new List<Part>();
            foreach (var part in parts.Where(p => p.Variant == variant))
            {
                if (Typed(part.AsField) is not { } type) return null;
                held.Add(new Part(PartName(part.Field, record), type, part.AsField));
            }
            cases.Add(new Case(variant, record, held));
        }
        return cases;
    }

    /// <summary>Writes one of the records <see cref="Unions"/> collected.</summary>
    private static string EmitUnion(UnionModel union)
    {
        var text = new StringBuilder();
        text.Append($$"""
            // <auto-generated/>
            #nullable enable

            namespace Bevy.Reflected;


            """);

        string Parameters(Case c) => string.Join(", ", c.Parts.Select(p => $"{p.Type} {p.Name}"));

        if (union.Struct)
        {
            var only = union.Cases[0];
            text.Append($$"""
                /// <summary>Bevy's <c>{{Xml(union.Rust)}}</c>, as a value a typed wrapper reads and writes whole.</summary>
                public sealed record {{union.Name}}({{Parameters(only)}});

                """);
            return text.ToString();
        }

        text.Append($$"""
            /// <summary>
            /// Bevy's <c>{{Xml(union.Rust)}}</c>, one record a variant, holding that variant's values.
            /// </summary>
            /// <remarks>
            /// Generated from <c>bevy-components.tsv</c>. A typed wrapper reads the variant a component
            /// holds as one of these, so reading is a <c>switch</c> on its type, and writing one sets the
            /// variant and its values together.
            /// </remarks>
            public abstract record {{union.Name}}
            {
                private {{union.Name}}() { }

            """);

        foreach (var c in union.Cases)
        {
            text.Append($$"""

                    /// <summary>Bevy's <c>{{Xml(union.Rust)}}::{{Xml(c.Variant)}}</c>.</summary>
                    public sealed record {{c.Record}}({{Parameters(c)}}) : {{union.Name}};

                """);
        }

        text.Append("}\n");
        return text.ToString();
    }

    /// <summary>Writes one wrapper.</summary>
    private static string Emit(ComponentModel component, Unions unions)
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
            public readonly struct {{type}} : global::Bevy.IReflectedComponent<{{type}}>
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

                /// <summary>Takes the component off the entity, reporting whether it was there.</summary>
                public bool Remove() => _world.RemoveReflected(Entity, TypePath);

            """);

        var used = new HashSet<string>(StringComparer.Ordinal) { type, "Entity", "Remove", "TypePath" };

        foreach (var field in component.Fields.Items)
        {
            var property = TypeOf(field);
            if (property is null && field.Kind != "Enum") continue;

            // A newtype's one field is called 0 by Rust, which says nothing as a property name.
            var name = field.Name == "0" ? "Value" : Identifier(field.Name);
            var unique = name;
            for (var n = 2; !used.Add(unique); n++) unique = name + n;

            var path = field.Reflect.Replace("\\", "\\\\").Replace("\"", "\\\"");
            text.Append($$"""

                    /// <summary>Bevy's <c>{{field.Name}}</c>.</summary>

                """);

            var parts = component.Variants.Items.Where(p => p.Enum == field.Name).ToList();
            if (field.Kind == "Enum" && parts.Count > 0 && EmitHolding(text, field, path, unique, parts, unions))
                continue;

            switch (field.Kind)
            {
                case "Enum":
                    var variants = field.Extra.Split(',').Where(v => v.Length > 0).Select(Escaped);
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
                                {{string.Join(",\n        ", variants)}},
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

    /// <summary>
    /// Writes an enum field whose variants hold values as a property of the record that holds
    /// them, or as a nullable value for an <c>Option</c>, and reports whether it could.
    /// </summary>
    private static bool EmitHolding(
        StringBuilder text, FieldModel field, string path, string unique, List<VariantFieldModel> parts, Unions unions)
    {
        var cases = CasesOf(field, parts);
        if (cases is null) return false;

        // Rust's Option is a value that may be absent, which C# writes as a nullable. Only a field
        // that is None is switched to Some before the value is written, since switching makes the
        // variant again with every field at its default, and a field the record does not hold, such
        // as a texture atlas's layout, would be lost each time the record is written.
        var options = field.Extra.Split(',').Where(v => v.Length > 0).ToArray();
        if (options.Length == 2 && options[0] == "None" && options[1] == "Some")
        {
            var some = cases[1];
            if (some.Parts.Count == 0) return false;

            if (some.Parts.Count == 1 && some.Parts[0].Field.Name.Length == 0)
            {
                var part = some.Parts[0];
                var at = Quoted(part.Field.Reflect);
                text.Append($$"""
                        public {{part.Type}}? {{unique}}
                        {
                            get => global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") == "None"
                                ? null
                                : {{Reading(part.Field, at)}};
                            set
                            {
                                if (value is not { } held)
                                {
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                    return;
                                }

                                if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                                {{Writing(part.Field, at, "held")}}
                            }
                        }

                    """);
                return true;
            }

            var inner = field.Rust.IndexOf('<') is var open and >= 0 && field.Rust.EndsWith(">", StringComparison.Ordinal)
                ? field.Rust.Substring(open + 1, field.Rust.Length - open - 2)
                : field.Rust;
            var record = unions.Add(inner, [some], isStruct: true);
            text.Append($$"""
                    public {{record.Name}}? {{unique}}
                    {
                        get => global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") == "None"
                            ? null
                            : new {{record.Name}}({{string.Join(", ", some.Parts.Select(p => Reading(p.Field, Quoted(p.Field.Reflect))))}});
                        set
                        {
                            if (value is null)
                            {
                                _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                return;
                            }

                            if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                            {{string.Join("\n                ", some.Parts.Select(p => Writing(p.Field, Quoted(p.Field.Reflect), "value." + p.Name)))}}
                        }
                    }

                """);
            return true;
        }

        // The record is shared with every other field of the same Rust type, so its paths are this
        // field's own cases' rather than the record's.
        var union = unions.Add(field.Rust, cases, isStruct: false);
        var reads = string.Join("\n", cases.Select(c =>
            $"            \"{c.Variant}\" => new global::Bevy.Reflected.{union.Name}.{c.Record}({string.Join(", ", c.Parts.Select(p => Reading(p.Field, Quoted(p.Field.Reflect))))}),"));
        var writes = string.Join("\n", cases.Select(c =>
        {
            var body = new StringBuilder();
            body.Append($"                case global::Bevy.Reflected.{union.Name}.{c.Record} held:\n");
            body.Append($"                    _world.SetVariant(Entity, TypePath, \"{path}\", \"{c.Variant}\");\n");
            foreach (var p in c.Parts)
                body.Append($"                    {Writing(p.Field, Quoted(p.Field.Reflect), "held." + p.Name)}\n");
            body.Append("                    break;");
            return body.ToString();
        }));

        text.Append($$"""
                public global::Bevy.Reflected.{{union.Name}} {{unique}}
                {
                    get => global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") switch
                    {
            {{reads}}
                        var other => throw new global::System.InvalidOperationException(
                            $"'{{path}}' of {TypePath} holds {other}, which this wrapper was not generated with."),
                    };
                    set
                    {
                        switch (value)
                        {
            {{writes}}
                            default:
                                throw new global::System.ArgumentNullException(nameof(value));
                        }
                    }
                }

            """);
        return true;
    }
}

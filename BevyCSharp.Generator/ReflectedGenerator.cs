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

        context.RegisterSourceOutput(components, static (spc, parsed) =>
        {
            foreach (var component in Named(parsed.Items))
                spc.AddSource($"Reflected.{component.Type}.g.cs", Emit(component));
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

    /// <summary>One component, as the description lists it.</summary>
    private sealed record ComponentModel(
        string Path, string Short, EquatableArray<FieldModel> Fields)
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

        void Close()
        {
            if (path is not null)
                components.Add(new ComponentModel(path, shortName!, new EquatableArray<FieldModel>(fields.ToArray())));
            fields.Clear();
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
        "Vec3" => "global::Bevy.Vec3",
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

    /// <summary>Writes one wrapper.</summary>
    private static string Emit(ComponentModel component)
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

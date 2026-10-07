using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bevy.Generator;

public sealed partial class ReflectedGenerator
{
    /// <summary>One value of a variant's record, or of a struct's.</summary>
    /// <param name="Name">Its parameter's name.</param>
    /// <param name="Type">Its C# type.</param>
    /// <param name="Field">The row it is read from.</param>
    /// <param name="Union">For an enum inside the variant, the record or enum it is held as.</param>
    /// <param name="Cases">For such an enum, its variants with their own values and paths.</param>
    /// <param name="Some">For an <c>Option</c> inside the variant, the one value its <c>Some</c> holds, the part a nullable.</param>
    private sealed record Part(
        string Name, string Type, FieldModel Field, UnionModel? Union = null, List<Case>? Cases = null, Part? Some = null);

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
    /// <param name="Plain">Whether it is an enum none of whose variants holds a value, written as a C# enum.</param>
    private sealed record UnionModel(string Name, string Rust, List<Case> Cases, bool Struct, bool Plain = false)
    {
        /// <summary>What two uses of the same Rust type have to agree on to share one record.</summary>
        public string Signature => (Plain ? "enum:" : string.Empty)
            + string.Join(";", Cases.Select(c => c.Variant + "(" + string.Join(",", c.Parts.Select(p => p.Name + ":" + p.Type)) + ")"));
    }

    /// <summary>
    /// The records emitted so far, one a Rust type, so a type two components hold is one record.
    /// </summary>
    private sealed class Unions(HashSet<string> taken)
    {
        private readonly Dictionary<string, UnionModel> _byName = new(StringComparer.Ordinal);

        public IEnumerable<UnionModel> All => _byName.Values;

        /// <summary>The record for a Rust type, the one already made where it agrees.</summary>
        public UnionModel Add(string rust, List<Case> cases, bool isStruct, bool plain = false)
        {
            var name = Identifier(Last(rust));
            if (taken.Contains(name)) name += "Value";

            var made = new UnionModel(name, rust, cases, isStruct, plain);
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
    /// <remarks>
    /// An enum inside a variant, as an orthographic projection's scaling mode or a sprite slicer's
    /// scale modes, is a record of its own inside the variant's, its variants found by its row's
    /// name among <paramref name="all"/>, or a plain C# enum where none of its variants holds a value.
    /// </remarks>
    private static List<Case>? CasesOf(
        FieldModel field, IEnumerable<VariantFieldModel> parts, IReadOnlyList<VariantFieldModel> all, Unions unions)
    {
        var cases = new List<Case>();
        foreach (var variant in field.Extra.Split(',').Where(v => v.Length > 0))
        {
            var record = Identifier(variant);
            var held = new List<Part>();
            foreach (var part in parts.Where(p => p.Variant == variant))
            {
                var name = PartName(part.Field, record);
                if (Typed(part.AsField) is { } type)
                {
                    held.Add(new Part(name, type, part.AsField));
                    continue;
                }

                if (part.Kind != "Enum") return null;

                // The inner enum's row is named under the variant, as its values' rows are.
                var row = part.Field.Length == 0 ? field.Name + "." + variant : field.Name + "." + variant + "." + part.Field;
                var inner = CasesOf(part.AsField with { Name = row }, all.Where(p => p.Enum == row), all, unions);
                if (inner is null) return null;

                // An Option holding one value is a nullable, as one a component holds is.
                if (inner.Count == 2
                    && inner[0] is { Variant: "None", Parts.Count: 0 }
                    && inner[1] is { Variant: "Some", Parts.Count: 1 } some
                    && some.Parts[0].Field.Name.Length == 0)
                {
                    held.Add(new Part(name, some.Parts[0].Type + "?", part.AsField, Some: some.Parts[0]));
                    continue;
                }

                var union = unions.Add(part.Rust, inner, isStruct: false, plain: inner.All(c => c.Parts.Count == 0));
                held.Add(new Part(name, "global::Bevy.Reflected." + union.Name, part.AsField, union, inner));
            }
            cases.Add(new Case(variant, record, held));
        }
        return cases;
    }

    /// <summary>The expression reading one value of a record, an enum inside it by its variant.</summary>
    private static string Read(Part part)
    {
        var path = Quoted(part.Field.Reflect);
        var variant = $"global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, \"{path}\")";
        if (part.Some is { } some) return $"({variant} == \"None\" ? null : ({part.Type}){Read(some)})";
        if (part.Union is not { } union) return Reading(part.Field, path);
        if (union.Plain) return $"global::System.Enum.Parse<global::Bevy.Reflected.{union.Name}>({variant})";

        var arms = part.Cases!.Select(c =>
            $"\"{c.Variant}\" => new global::Bevy.Reflected.{union.Name}.{c.Record}({string.Join(", ", c.Parts.Select(Read))}),");
        return $"({variant} switch {{ {string.Join(" ", arms)} var other => throw new global::System.InvalidOperationException("
            + "$\"'" + path + "' of {TypePath} holds {other}, which this wrapper was not generated with.\") })";
    }

    /// <summary>
    /// The statements writing one value of a record, an enum inside it by choosing its variant and
    /// then writing that variant's values.
    /// </summary>
    /// <param name="part">The value.</param>
    /// <param name="value">The expression holding it.</param>
    /// <param name="depth">How many enums deep it is, which names the variable each level matches.</param>
    private static string Write(Part part, string value, int depth)
    {
        var path = Quoted(part.Field.Reflect);
        if (part.Some is { } some)
        {
            // Switched to Some only from None, since switching makes the variant again at its default.
            var present = "some" + depth;
            return $"if ({value} is not {{ }} {present}) _world.SetVariant(Entity, TypePath, \"{path}\", \"None\"); "
                + $"else {{ if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, \"{path}\") != \"Some\") _world.SetVariant(Entity, TypePath, \"{path}\", \"Some\"); "
                + Write(some, present, depth + 1) + " }";
        }

        if (part.Union is not { } union) return Writing(part.Field, path, value);
        if (union.Plain) return $"_world.SetVariant(Entity, TypePath, \"{path}\", {value}.ToString());";

        var held = "inner" + depth;
        var text = new StringBuilder($"switch ({value}) {{ ");
        foreach (var c in part.Cases!)
        {
            text.Append($"case global::Bevy.Reflected.{union.Name}.{c.Record} {held}: ");
            text.Append($"_world.SetVariant(Entity, TypePath, \"{path}\", \"{c.Variant}\"); ");
            foreach (var p in c.Parts) text.Append(Write(p, held + "." + p.Name, depth + 1)).Append(' ');
            text.Append("break; ");
        }

        return text.Append("default: throw new global::System.ArgumentNullException(nameof(value)); }").ToString();
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

        if (union.Plain)
        {
            var members = union.Cases.Select(c =>
                $"    /// <summary>Bevy's <c>{Xml(union.Rust)}::{Xml(c.Variant)}</c>.</summary>\n    {Escaped(c.Variant)},");
            text.Append($$"""
                /// <summary>Bevy's <c>{{Xml(union.Rust)}}</c>, a member a variant, none of which holds a value.</summary>
                public enum {{union.Name}}
                {
                {{string.Join("\n\n", members)}}
                }

                """);
            return text.ToString();
        }

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

    /// <summary>
    /// Writes an enum field whose variants hold values as a property of the record that holds
    /// them, or as a nullable value for an <c>Option</c>, and reports whether it could.
    /// </summary>
    private static bool EmitHolding(
        StringBuilder text,
        FieldModel field,
        string path,
        string unique,
        List<VariantFieldModel> parts,
        IReadOnlyList<VariantFieldModel> all,
        Unions unions)
    {
        var cases = CasesOf(field, parts, all, unions);
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
                text.Append($$"""
                        public {{part.Type}}? {{unique}}
                        {
                            get => global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") == "None"
                                ? null
                                : {{Read(part)}};
                            set
                            {
                                if (value is not { } held)
                                {
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                    return;
                                }

                                if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                                {{Write(part, "held", 1)}}
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
                            : new {{record.Name}}({{string.Join(", ", some.Parts.Select(Read))}});
                        set
                        {
                            if (value is null)
                            {
                                _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                return;
                            }

                            if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                            {{string.Join("\n                ", some.Parts.Select(p => Write(p, "value." + p.Name, 1)))}}
                        }
                    }

                """);
            return true;
        }

        // The record is shared with every other field of the same Rust type, so its paths are this
        // field's own cases' rather than the record's.
        var union = unions.Add(field.Rust, cases, isStruct: false);
        var reads = string.Join("\n", cases.Select(c =>
            $"            \"{c.Variant}\" => new global::Bevy.Reflected.{union.Name}.{c.Record}({string.Join(", ", c.Parts.Select(Read))}),"));
        var writes = string.Join("\n", cases.Select(c =>
        {
            var body = new StringBuilder();
            body.Append($"                case global::Bevy.Reflected.{union.Name}.{c.Record} held:\n");
            body.Append($"                    _world.SetVariant(Entity, TypePath, \"{path}\", \"{c.Variant}\");\n");
            foreach (var p in c.Parts)
                body.Append($"                    {Write(p, "held." + p.Name, 1)}\n");
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

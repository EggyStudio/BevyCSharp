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
    /// <param name="List">For a list of records, the record its items are, which reads and writes them.</param>
    /// <param name="Element">For a list of plain values, what one item is, read and written as its kind is.</param>
    private sealed record Part(
        string Name,
        string Type,
        FieldModel Field,
        UnionModel? Union = null,
        List<Case>? Cases = null,
        Part? Some = null,
        UnionModel? List = null,
        FieldModel? Element = null);

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
        /// <summary>
        /// Whether a list holds these as its items, which gives the record the methods reading and
        /// writing such a list.
        /// </summary>
        public bool Listed { get; set; }

        /// <summary>For a union a list holds, the value its items are read as, the union's row.</summary>
        public Part? Item { get; set; }

        /// <summary>
        /// What two uses of the same Rust type have to agree on to share one record, a struct's
        /// values whether it came as an option's or a list's.
        /// </summary>
        public string Signature => (Plain ? "enum:" : string.Empty)
            + string.Join(";", Cases.Select(c => (Struct ? string.Empty : c.Variant) + "(" + string.Join(",", c.Parts.Select(p => p.Name + ":" + p.Type)) + ")"));
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

    /// <summary>What a component's or an item type's rows are read against.</summary>
    /// <param name="Variants">Its variant rows, among which an enum's are found by its row's name.</param>
    /// <param name="Items">Every item type's rows, by its type path, which a list names.</param>
    /// <param name="Unions">The records made so far.</param>
    private sealed record Scope(
        IReadOnlyList<VariantFieldModel> Variants, IReadOnlyDictionary<string, ComponentModel> Items, Unions Unions);

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
    /// name among the scope's, or a plain C# enum where none of its variants holds a value.
    /// </remarks>
    private static List<Case>? CasesOf(FieldModel field, IEnumerable<VariantFieldModel> parts, Scope scope)
    {
        var cases = new List<Case>();
        foreach (var variant in field.Extra.Split(',').Where(v => v.Length > 0))
        {
            var record = Identifier(variant);
            var held = new List<Part>();
            foreach (var part in parts.Where(p => p.Variant == variant))
            {
                // An enum's row inside the variant is named under it, as its values' rows are.
                var row = part.Field.Length == 0 ? field.Name + "." + variant : field.Name + "." + variant + "." + part.Field;
                if (PartOf(part.AsField, row, PartName(part.Field, record), scope) is not { } made) return null;
                held.Add(made);
            }
            cases.Add(new Case(variant, record, held));
        }

        // A variant called what a value is, as a mesh's morph weights have a variant Value beside a
        // value called Value, takes another name, since a record nested in the union is a member
        // every variant's record inherits.
        var names = new HashSet<string>(cases.SelectMany(c => c.Parts).Select(p => p.Name), StringComparer.Ordinal);
        for (var i = 0; i < cases.Count; i++)
        {
            if (names.Contains(cases[i].Record)) cases[i] = cases[i] with { Record = cases[i].Record + "Variant" };
        }

        return cases;
    }

    /// <summary>
    /// One value of a record, from the row it is read from, or nothing where a wrapper cannot type
    /// it. A value is held as its kind has it, an enum as a record, a plain enum or a nullable, and a
    /// list as the records its items are.
    /// </summary>
    /// <param name="field">The row, its name the part of the row's name a variant gives it.</param>
    /// <param name="row">The row's whole name, which an enum's variant rows and their own enums name.</param>
    /// <param name="name">The record's parameter name for it.</param>
    /// <param name="scope">The rows it is among.</param>
    private static Part? PartOf(FieldModel field, string row, string name, Scope scope)
    {
        if (field.Kind == "List") return ListOf(field, name, scope);
        if (Typed(field) is { } type) return new Part(name, type, field);
        if (field.Kind != "Enum") return null;

        var inner = CasesOf(field with { Name = row }, scope.Variants.Where(p => p.Enum == row), scope);
        if (inner is null) return null;

        // An Option holding one value is a nullable, as one a component holds is.
        if (inner.Count == 2
            && inner[0] is { Variant: "None", Parts.Count: 0 }
            && inner[1] is { Variant: "Some", Parts.Count: 1 } some
            && some.Parts[0].Field.Name.Length == 0)
        {
            return new Part(name, some.Parts[0].Type + "?", field, Some: some.Parts[0]);
        }

        var union = scope.Unions.Add(field.Rust, inner, isStruct: false, plain: inner.All(c => c.Parts.Count == 0));
        return new Part(name, "global::Bevy.Reflected." + union.Name, field, union, inner);
    }

    /// <summary>The kinds of the plain values a list of them is typed as, which a wrapper reads and writes one at a time.</summary>
    private static readonly HashSet<string> Plain = new(StringComparer.Ordinal)
    {
        "Float", "Double", "Bool", "Int", "String", "Vec2", "Vec3", "Vec4", "Quat", "Entity",
    };

    /// <summary>A list of records, its items those its item type's rows describe, or nothing where a wrapper cannot type them.</summary>
    private static Part? ListOf(FieldModel field, string name, Scope scope)
    {
        // A list of plain values, as a mesh's morph weights are, names their kind, and is read and
        // written an item at a time as a field of that kind is.
        if (Plain.Contains(field.Extra))
        {
            var open = field.Rust.IndexOf('<');
            var inner = open < 0 ? string.Empty : field.Rust.Substring(open + 1).TrimStart('[').Split(';', '>')[0].Trim();
            var element = new FieldModel(string.Empty, string.Empty, field.Extra, inner, string.Empty);
            return new Part(name, $"global::System.Collections.Generic.IReadOnlyList<{TypeOf(element)}>", field, Element: element);
        }

        // An item type with no rows, as a type id or a range of indices, has nothing a record holds.
        if (!scope.Items.TryGetValue(field.Extra, out var item) || item.Fields.Items.Count == 0) return null;

        var rows = new Scope(item.Variants.Items, scope.Items, scope.Unions);
        UnionModel record;
        if (item.Fields.Items.Count == 1 && item.Fields.Items[0] is { Name: "value", Kind: "Enum" } value)
        {
            // An enum's items, as a gradient's are, read and written as the union its row is.
            if (PartOf(value, value.Name, "Value", rows) is not { Union: { Plain: false } union } part) return null;
            record = union;
            record.Item = part;
        }
        else
        {
            var recordName = Identifier(Last(item.Path));
            var parts = new List<Part>();
            foreach (var row in item.Fields.Items)
            {
                if (PartOf(row, row.Name, PartName(row.Name, recordName), rows) is not { } made) return null;
                parts.Add(made);
            }
            record = scope.Unions.Add(item.Path, [new Case(string.Empty, recordName, parts)], isStruct: true);
        }

        record.Listed = true;
        return new Part(name, $"global::System.Collections.Generic.IReadOnlyList<global::Bevy.Reflected.{record.Name}>", field, List: record);
    }

    /// <summary>
    /// A path as a C# expression, a literal for a component's own and the item's path before it for
    /// one inside a list's item, which is known only as the list is read.
    /// </summary>
    private static string PathOf(string? prefix, string reflect) =>
        prefix is null ? $"\"{Quoted(reflect)}\"" : reflect.Length == 0 ? prefix : $"{prefix} + \"{Quoted(reflect)}\"";

    /// <summary>The expression reading one value of a record, an enum inside it by its variant.</summary>
    /// <param name="part">The value.</param>
    /// <param name="prefix">The variable holding the path of the list item it is in, or nothing for a component's own.</param>
    private static string Read(Part part, string? prefix)
    {
        var path = PathOf(prefix, part.Field.Reflect);
        var variant = $"global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, {path})";
        if (part.List is { } list) return $"global::Bevy.Reflected.{list.Name}.ReadList(_world, Entity, TypePath, {path})";
        if (part.Element is { } element)
            return $"global::Bevy.ReflectedValue.Items<{TypeOf(element)}>(_world, Entity, TypePath, {path}, global::Bevy.FieldKind.{element.Kind})";
        if (part.Some is { } some) return $"({variant} == \"None\" ? null : ({part.Type}){Read(some, prefix)})";
        if (part.Union is not { } union) return Reading(part.Field, path);
        if (union.Plain) return $"global::System.Enum.Parse<global::Bevy.Reflected.{union.Name}>({variant})";

        var arms = part.Cases!.Select(c =>
            $"\"{c.Variant}\" => new global::Bevy.Reflected.{union.Name}.{c.Record}({string.Join(", ", c.Parts.Select(p => Read(p, prefix)))}),");
        return $"({variant} switch {{ {string.Join(" ", arms)} var other => throw new global::System.InvalidOperationException("
            + "$\"'{" + path + "}' of {TypePath} holds {other}, which this wrapper was not generated with.\") })";
    }

    /// <summary>
    /// The statements writing one value of a record, an enum inside it by choosing its variant and
    /// then writing that variant's values, and a list by making it as long as the value and writing
    /// each item.
    /// </summary>
    /// <param name="part">The value.</param>
    /// <param name="value">The expression holding it.</param>
    /// <param name="depth">How many enums deep it is, which names the variable each level matches.</param>
    /// <param name="prefix">The variable holding the path of the list item it is in, or nothing for a component's own.</param>
    private static string Write(Part part, string value, int depth, string? prefix)
    {
        var path = PathOf(prefix, part.Field.Reflect);
        if (part.List is { } list) return $"global::Bevy.Reflected.{list.Name}.WriteList(_world, Entity, TypePath, {path}, {value});";
        if (part.Element is { } element)
            return $"global::Bevy.ReflectedValue.SetItems(_world, Entity, TypePath, {path}, global::Bevy.FieldKind.{element.Kind}, {value});";
        if (part.Some is { } some)
        {
            // Switched to Some only from None, since switching makes the variant again at its default.
            var present = "some" + depth;
            return $"if ({value} is not {{ }} {present}) _world.SetVariant(Entity, TypePath, {path}, \"None\"); "
                + $"else {{ if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, {path}) != \"Some\") _world.SetVariant(Entity, TypePath, {path}, \"Some\"); "
                + Write(some, present, depth + 1, prefix) + " }";
        }

        if (part.Union is not { } union) return Writing(part.Field, path, value);
        if (union.Plain) return $"_world.SetVariant(Entity, TypePath, {path}, {value}.ToString());";

        var held = "inner" + depth;
        var text = new StringBuilder($"switch ({value}) {{ ");
        foreach (var c in part.Cases!)
        {
            text.Append($"case global::Bevy.Reflected.{union.Name}.{c.Record} {held}: ");
            text.Append($"_world.SetVariant(Entity, TypePath, {path}, \"{c.Variant}\"); ");
            foreach (var p in c.Parts) text.Append(Write(p, held + "." + p.Name, depth + 1, prefix)).Append(' ');
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
                public sealed record {{union.Name}}({{Parameters(only)}})
                """);

            if (!union.Listed) return text.Append(";\n").ToString();

            var made = $"new global::Bevy.Reflected.{union.Name}({string.Join(", ", only.Parts.Select(p => Read(p, "item")))})";
            var written = string.Join("\n            ", only.Parts.Select(p => Write(p, "value[i]." + p.Name, 1, "item")));
            return text.Append("\n{\n").Append(Lists(union, made, written)).Append("}\n").ToString();
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

        if (union.Listed && union.Item is { } item)
            text.Append('\n').Append(Lists(union, Read(item, "item"), Write(item, "value[i]", 1, "item")));

        text.Append("}\n");
        return text.ToString();
    }

    /// <summary>
    /// The methods reading and writing a list of a record's values, which a wrapper's property and
    /// a record holding such a list call, the item's path made from the list's and its index.
    /// </summary>
    /// <remarks>
    /// Static, with the component's world, entity and type path as parameters named as a wrapper's
    /// members are, so a value's read and write read here as they do in a wrapper, and one record
    /// serves every component holding a list of the type.
    /// </remarks>
    /// <param name="union">The record the items are.</param>
    /// <param name="made">The expression reading one item at the path <c>item</c>.</param>
    /// <param name="written">The statements writing <c>value[i]</c> at the path <c>item</c>.</param>
    private static string Lists(UnionModel union, string made, string written)
    {
        var type = "global::Bevy.Reflected." + union.Name;
        return $$"""
                /// <summary>Reads the items of a list of these a component holds at <paramref name="at"/>.</summary>
                internal static {{type}}[] ReadList(
                    global::Bevy.EcsWorld _world, global::Bevy.Entity Entity, string TypePath, string at)
                {
                    var items = new {{type}}[global::Bevy.ReflectedValue.Count(_world, Entity, TypePath, at)];
                    for (var i = 0; i < items.Length; i++)
                    {
                        var item = global::Bevy.ReflectedValue.Item(at, i);
                        items[i] = {{made}};
                    }

                    return items;
                }

                /// <summary>Writes a list of these a component holds at <paramref name="at"/>, as long as it is given.</summary>
                internal static void WriteList(
                    global::Bevy.EcsWorld _world,
                    global::Bevy.Entity Entity,
                    string TypePath,
                    string at,
                    global::System.Collections.Generic.IReadOnlyList<{{type}}> value)
                {
                    global::Bevy.ReflectedValue.Resize(_world, Entity, TypePath, at, value.Count);
                    for (var i = 0; i < value.Count; i++)
                    {
                        var item = global::Bevy.ReflectedValue.Item(at, i);
                        {{written}}
                    }
                }

            """;
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
        Scope scope)
    {
        var cases = CasesOf(field, parts, scope);
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
                                : {{Read(part, null)}};
                            set
                            {
                                if (value is not { } held)
                                {
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                    return;
                                }

                                if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                    _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                                {{Write(part, "held", 1, null)}}
                            }
                        }

                    """);
                return true;
            }

            var inner = field.Rust.IndexOf('<') is var open and >= 0 && field.Rust.EndsWith(">", StringComparison.Ordinal)
                ? field.Rust.Substring(open + 1, field.Rust.Length - open - 2)
                : field.Rust;
            var record = scope.Unions.Add(inner, [some], isStruct: true);
            text.Append($$"""
                    public {{record.Name}}? {{unique}}
                    {
                        get => global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") == "None"
                            ? null
                            : new {{record.Name}}({{string.Join(", ", some.Parts.Select(p => Read(p, null)))}});
                        set
                        {
                            if (value is null)
                            {
                                _world.SetVariant(Entity, TypePath, "{{path}}", "None");
                                return;
                            }

                            if (global::Bevy.ReflectedValue.Variant(_world, Entity, TypePath, "{{path}}") != "Some")
                                _world.SetVariant(Entity, TypePath, "{{path}}", "Some");
                            {{string.Join("\n                ", some.Parts.Select(p => Write(p, "value." + p.Name, 1, null)))}}
                        }
                    }

                """);
            return true;
        }

        // The record is shared with every other field of the same Rust type, so its paths are this
        // field's own cases' rather than the record's.
        var union = scope.Unions.Add(field.Rust, cases, isStruct: false);
        var reads = string.Join("\n", cases.Select(c =>
            $"            \"{c.Variant}\" => new global::Bevy.Reflected.{union.Name}.{c.Record}({string.Join(", ", c.Parts.Select(p => Read(p, null)))}),"));
        var writes = string.Join("\n", cases.Select(c =>
        {
            var body = new StringBuilder();
            body.Append($"                case global::Bevy.Reflected.{union.Name}.{c.Record} held:\n");
            body.Append($"                    _world.SetVariant(Entity, TypePath, \"{path}\", \"{c.Variant}\");\n");
            foreach (var p in c.Parts)
                body.Append($"                    {Write(p, "held." + p.Name, 1, null)}\n");
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

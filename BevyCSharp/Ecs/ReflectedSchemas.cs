using System.Globalization;
using System.Text.Json;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Builds a <see cref="ComponentSchema"/> for every component Bevy reflects, from the description
/// the bridge reads out of Bevy's type registry.
/// </summary>
/// <remarks>
/// <para>
/// A generated schema reads a field through a closure over the C# struct. A reflected one reads it
/// through <see cref="EcsWorld.GetReflected"/> by Bevy's reflect path, and boxes the result as the
/// same CLR type a generated schema would (<see cref="float"/>, <see cref="Vec3"/>, an enum's name
/// as a <see cref="string"/>). Everything that draws, compares or records a field then treats both
/// alike, and the editor's history, which decides whether anything changed by comparing two boxed
/// reads, stays correct.
/// </para>
/// <para>
/// A nested struct is taken apart into rows inside a fold, as the generator does it, so a light's
/// shadow settings read the way a behavior's nested struct does. An enum that carries data is a row
/// choosing the variant, and each variant's fields are rows shown only while that variant is chosen,
/// through the same <see cref="FieldCondition"/> a <c>[ShowIf]</c> attribute produces. A field the
/// schema has no editor for (a string, a list, an asset handle) is a read-only row showing its JSON.
/// </para>
/// <para>
/// The description is parsed with <see cref="JsonDocument"/>, which reads without reflection, so the
/// library stays safe to trim and to compile ahead of time.
/// </para>
/// </remarks>
internal static class ReflectedSchemas
{
    /// <summary>
    /// How deep a struct inside a struct is taken apart before the rest is shown as JSON.
    /// </summary>
    /// <remarks>
    /// Bevy's components are shallow, and a type that holds itself through a box would otherwise be
    /// walked for ever. A field past this depth is still shown, as one row.
    /// </remarks>
    private const int Depth = 6;

    /// <summary>One reflected component, as the dump lists it.</summary>
    private sealed record Listed(string Path, string Short, int Id, bool Default);

    /// <summary>
    /// Builds a schema for every reflected component that no schema already describes.
    /// </summary>
    /// <param name="json">What <see cref="EcsWorld.DescribeReflected"/> answered.</param>
    /// <param name="taken">Component ids a mirror already describes, which a mirror keeps.</param>
    /// <param name="names">Names already in use, which a reflected schema does not take.</param>
    internal static List<ComponentSchema> Build(
        string json, IReadOnlySet<int> taken, IReadOnlySet<string> names)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var types = root.GetProperty("types");

        var listed = new List<Listed>();
        foreach (var entry in root.GetProperty("components").EnumerateArray())
        {
            var id = entry.GetProperty("id").GetInt32();
            if (taken.Contains(id)) continue;

            listed.Add(new Listed(
                entry.GetProperty("path").GetString()!,
                entry.GetProperty("short").GetString()!,
                id,
                entry.GetProperty("default").GetBoolean()));
        }

        // A short name says which component is meant only while one component has it. Bevy's crates
        // reuse names, and an inspector keys its rows and folds on the name, so a name two components
        // share is replaced by the full path on both rather than given to whichever came first.
        var shared = listed
            .GroupBy(component => component.Short, StringComparer.Ordinal)
            .Where(group => group.Count() > 1 || names.Contains(group.Key))
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        var schemas = new List<ComponentSchema>(listed.Count);
        foreach (var component in listed)
        {
            var fields = new List<ComponentField>();
            new Walk(component.Path, types, fields).Members(component.Path);

            var path = component.Path;
            schemas.Add(new ComponentSchema(
                shared.Contains(component.Short) ? path : component.Short,
                path,
                () => component.Id,
                fields,
                add: component.Default
                    ? (world, entity) => world.InsertReflected(entity, path)
                    : null,
                remove: (world, entity) => world.RemoveReflected(entity, path))
            {
                Origin = SchemaOrigin.Reflected,
            });
        }

        return schemas;
    }

    /// <summary>
    /// Turns one component's description into rows.
    /// </summary>
    /// <param name="component">The component's type path, which every field reads through.</param>
    /// <param name="types">Every described type, by path.</param>
    /// <param name="fields">Where the rows go, in the order Bevy declares them.</param>
    private sealed class Walk(string component, JsonElement types, List<ComponentField> fields)
    {
        /// <summary>A place in the walk, where a row is named, read, folded and shown.</summary>
        /// <param name="Name">The row's name, a path of the Rust names joined by dots.</param>
        /// <param name="Reflect">Bevy's reflect path from the component's root.</param>
        /// <param name="Fold">The fold it sits in, with slashes between levels.</param>
        /// <param name="Shown">What has to hold for it to be shown.</param>
        /// <param name="Within">The enum variants it belongs to, by the enum's reflect path.</param>
        /// <param name="Docs">Bevy's documentation of the field, which an editor build carries.</param>
        /// <param name="Depth">How many structs deep it is.</param>
        private sealed record At(
            string Name,
            string Reflect,
            string? Fold,
            FieldCondition[] Shown,
            Variant[] Within,
            string? Docs,
            int Depth);

        /// <summary>Adds a row for each field of the type at <paramref name="type"/>.</summary>
        public void Members(string type) =>
            Members(type, new At(string.Empty, string.Empty, null, [], [], null, 0));

        private void Members(string type, At at)
        {
            if (!types.TryGetProperty(type, out var described)) return;
            if (!described.TryGetProperty("fields", out var members)) return;

            foreach (var member in members.EnumerateArray())
            {
                var name = member.GetProperty("name").GetString()!;
                Member(
                    member.GetProperty("type").GetString()!,
                    Humanize(name),
                    at with
                    {
                        Name = Join(at.Name, name),
                        Reflect = at.Reflect + "." + name,
                        Docs = Documented(member),
                    });
            }
        }

        /// <summary>Adds the row or rows for one field.</summary>
        /// <param name="type">The field's type path.</param>
        /// <param name="label">What the row is called on screen.</param>
        /// <param name="at">Where the field is.</param>
        private void Member(string type, string label, At at)
        {
            if (Scalar(type) is { } scalar)
            {
                fields.Add(Row(at, label, scalar, type));
                return;
            }

            var described = types.TryGetProperty(type, out var found) ? found : default;

            // A list or an array of values the inspector can edit is a list field, read and written
            // as one JSON array. One of anything else stays JSON.
            if (described.ValueKind == JsonValueKind.Object
                && described.TryGetProperty("item", out var item)
                && Scalar(item.GetString()!) is { } itemKind)
            {
                fields.Add(Listed(at, label, Short(type), itemKind));
                return;
            }

            // A color is one swatch whatever space Bevy holds it in, converted by Bevy, rather than a
            // choice of space over rows of numbers that mean something different in each.
            if (described.ValueKind == JsonValueKind.Object && described.TryGetProperty("color", out _))
            {
                fields.Add(Shade(at, label, Short(type)));
                return;
            }

            // A handle is a reference to an asset, picked from the files of its kind. One whose
            // kind the bridge cannot load has no files to offer, so it is only shown.
            if (described.ValueKind == JsonValueKind.Object
                && described.TryGetProperty("asset", out var asset))
            {
                var held = asset.GetString();
                fields.Add(held is { Length: > 0 }
                    ? Handle(at, label, Short(type), held)
                    : Opaque(at, label, Short(type)));
                return;
            }

            var registered = described.ValueKind == JsonValueKind.Object
                && described.GetProperty("registered").GetBoolean();
            var kind = registered ? described.GetProperty("kind").GetString() : null;

            if (at.Depth >= Depth) kind = null;

            switch (kind)
            {
                case "struct" or "tuple_struct" when Single(described) is { } inner:
                    // A newtype such as a unit of measure is its one field under the outer name,
                    // so a row reads "Range" rather than "Range" holding a lone "0".
                    Member(inner.Type, label, at with { Reflect = at.Reflect + "." + inner.Name });
                    return;

                case "struct" or "tuple_struct":
                    Members(type, at with
                    {
                        Fold = at.Fold is null ? label : at.Fold + "/" + label,
                        Depth = at.Depth + 1,
                    });
                    return;

                case "enum":
                    Variants(type, described, label, at);
                    return;

                default:
                    fields.Add(Opaque(at, label, Short(type)));
                    return;
            }
        }

        /// <summary>
        /// Adds a row choosing an enum's variant, then the rows of each variant that has fields,
        /// each shown only while its variant is the one chosen.
        /// </summary>
        /// <remarks>
        /// A variant's rows sit at the enum's own level rather than in a fold of their own, since a
        /// fold whose rows are all hidden would still draw its header.
        /// </remarks>
        private void Variants(string type, JsonElement described, string label, At at)
        {
            var variants = described.GetProperty("variants").EnumerateArray().ToArray();
            var options = variants.Select(v => v.GetProperty("name").GetString()!).ToArray();

            fields.Add(Choice(at, label, Short(type), options));

            foreach (var variant in variants)
            {
                if (!variant.TryGetProperty("fields", out var members)) continue;

                var name = variant.GetProperty("name").GetString()!;
                var within = at with
                {
                    Name = Join(at.Name, name),
                    Shown = [.. at.Shown, new FieldCondition(at.Name, name)],
                    Within = [.. at.Within, new Variant(at.Reflect, name)],
                    Depth = at.Depth + 1,
                };

                var all = members.EnumerateArray().ToArray();
                if (all.Length == 1 && variant.GetProperty("kind").GetString() == "tuple")
                {
                    // A variant wrapping one value, as Color.Srgba wraps an Srgba, is that value's
                    // rows directly, and a variant field is reached without naming the variant.
                    Inline(all[0].GetProperty("type").GetString()!, Humanize(name), within with
                    {
                        Reflect = at.Reflect + "." + all[0].GetProperty("name").GetString(),
                    });
                    continue;
                }

                foreach (var member in all)
                {
                    var field = member.GetProperty("name").GetString()!;
                    Inline(member.GetProperty("type").GetString()!, Humanize(field), within with
                    {
                        Name = Join(within.Name, field),
                        Reflect = at.Reflect + "." + field,
                        Docs = Documented(member),
                    });
                }
            }
        }

        /// <summary>
        /// Adds a variant's value without the fold a struct would otherwise open, for the reason
        /// <see cref="Variants"/> gives.
        /// </summary>
        private void Inline(string type, string label, At at)
        {
            if (Scalar(type) is null
                && types.TryGetProperty(type, out var described)
                && described.GetProperty("registered").GetBoolean()
                && described.GetProperty("kind").GetString() == "struct"
                && at.Depth < Depth)
            {
                Members(type, at);
                return;
            }

            Member(type, label, at);
        }

        /// <summary>The one field of a newtype, or nothing when the type has another number.</summary>
        private static (string Name, string Type)? Single(JsonElement described)
        {
            if (described.GetProperty("kind").GetString() != "tuple_struct") return null;

            var members = described.GetProperty("fields");
            if (members.GetArrayLength() != 1) return null;

            var only = members[0];
            return (only.GetProperty("name").GetString()!, only.GetProperty("type").GetString()!);
        }

        /// <summary>What a type is called on screen, which is its short path.</summary>
        private string Short(string type) =>
            types.TryGetProperty(type, out var described)
            && described.ValueKind == JsonValueKind.Object
                ? described.GetProperty("short").GetString() ?? type
                : type;

        /// <summary>
        /// The hints every reflected row carries: its label, its tooltip, its fold and its
        /// conditions.
        /// </summary>
        private static FieldHints Hints(At at, string label) =>
            new(Label: label, Tooltip: at.Docs, Foldout: at.Fold, Conditions: at.Shown);

        /// <summary>
        /// The first paragraph of a field's documentation, or nothing when the build carries none.
        /// </summary>
        /// <remarks>
        /// The first paragraph says what the field is, and the rest of Bevy's comment is usually
        /// examples and links, which a tooltip has no room for and cannot follow.
        /// </remarks>
        private static string? Documented(JsonElement member)
        {
            if (!member.TryGetProperty("docs", out var docs)) return null;

            var text = docs.GetString()?.Trim();
            if (string.IsNullOrEmpty(text)) return null;

            var end = text.IndexOf("\n\n", StringComparison.Ordinal);
            var first = end < 0 ? text : text[..end];
            return string.Join(' ', first.Split('\n', StringSplitOptions.TrimEntries));
        }

        // Each row's closures copy the component's path into a local, so a schema kept for the run
        // holds strings rather than the walk and the parsed document it points into.

        /// <summary>A row for a value the inspector has an editor for.</summary>
        private ComponentField Row(At at, string label, FieldKind kind, string type)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                kind,
                type,
                (world, entity) => Holds(world, entity, owner, within)
                    ? Read(world, entity, owner, path, kind)
                    : null,
                (world, entity, value) => Holds(world, entity, owner, within)
                    && Write(world, entity, owner, path, kind, value),
                hints: Hints(at, label))
            {
                ReflectPath = path,
            };
        }

        /// <summary>A row holding an asset handle, picked from the files of its kind.</summary>
        private ComponentField Handle(At at, string label, string type, string kind)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                FieldKind.Asset,
                type,
                (world, entity) => Holds(world, entity, owner, within)
                    ? Attempted(() => world.GetReflectedAsset(entity, owner, path))
                    : null,
                // Nothing is not a value a handle can hold, since Bevy's handle has no empty state
                // to write, so choosing "Nothing" in the picker is refused rather than faked.
                (world, entity, value) => value is AssetHandle { IsValid: true } asset
                    && Holds(world, entity, owner, within)
                    && Sent(() => world.SetReflectedAsset(entity, owner, path, asset)),
                hints: Hints(at, label) with { Asset = kind })
            {
                ReflectPath = path,
            };
        }

        /// <summary>A row holding a list of values of one kind.</summary>
        private ComponentField Listed(At at, string label, string type, FieldKind item)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                FieldKind.List,
                type,
                (world, entity) => Holds(world, entity, owner, within)
                    && Guarded(() => world.GetReflected(entity, owner, path)) is { } json
                    ? ReflectedValue.DecodeList(json, item)
                    : null,
                (world, entity, value) => value is ListValue list
                    && Holds(world, entity, owner, within)
                    && ReflectedValue.EncodeList(list, item) is { } json
                    && Sent(() => world.SetReflected(entity, owner, path, json)),
                hints: Hints(at, label))
            {
                ReflectPath = path,
                ElementKind = item,
            };
        }

        /// <summary>A row holding a color, drawn as a swatch.</summary>
        private ComponentField Shade(At at, string label, string type)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                FieldKind.Color,
                type,
                (world, entity) => Holds(world, entity, owner, within)
                    ? Attempted(() => world.GetReflectedColor(entity, owner, path))
                    : null,
                (world, entity, value) => value is Color color
                    && Holds(world, entity, owner, within)
                    && Sent(() => world.SetReflectedColor(entity, owner, path, color)),
                hints: Hints(at, label))
            {
                ReflectPath = path,
            };
        }

        /// <summary>A row choosing one of an enum's variants by name.</summary>
        private ComponentField Choice(At at, string label, string type, string[] options)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                FieldKind.Enum,
                type,
                (world, entity) => Holds(world, entity, owner, within)
                    ? Guarded(() => world.GetVariant(entity, owner, path))
                    : null,
                (world, entity, value) => Holds(world, entity, owner, within)
                    && Sent(() => world.SetVariant(entity, owner, path, value.ToString()!)),
                options,
                Hints(at, label))
            {
                ReflectPath = path,
            };
        }

        /// <summary>A row showing a value the inspector has no editor for, as its JSON.</summary>
        private ComponentField Opaque(At at, string label, string type)
        {
            var (owner, path, within) = (component, at.Reflect, at.Within);
            return new ComponentField(
                at.Name,
                FieldKind.Opaque,
                type,
                // Some values have no JSON form at all, such as an asset handle, and a row that
                // throws every frame is worse than one naming what it holds.
                (world, entity) => Holds(world, entity, owner, within)
                    ? Guarded(() => world.GetReflected(entity, owner, path)) ?? type
                    : null,
                hints: Hints(at, label));
        }
    }

    /// <summary>One variant of an enum, named by the enum's reflect path and the variant's name.</summary>
    private readonly record struct Variant(string Path, string Name);

    /// <summary>
    /// Whether every enum a row sits inside holds the variant the row belongs to.
    /// </summary>
    /// <remarks>
    /// Bevy's reflect path does not name the variant, so <c>.color.0.red</c> is the red of whatever
    /// the color holds. Without this the Srgba row of a color holding LinearRgba would read
    /// LinearRgba's red, and write it. A row of a variant not held reads nothing and refuses to be
    /// written, as the field of an absent component does.
    /// </remarks>
    private static bool Holds(EcsWorld world, Entity entity, string component, Variant[] within)
    {
        foreach (var variant in within)
        {
            var held = Guarded(() => world.GetVariant(entity, component, variant.Path));
            if (held != variant.Name) return false;
        }

        return true;
    }

    /// <summary>The kind of a field whose type the inspector has an editor for, by type path.</summary>
    private static FieldKind? Scalar(string type) => type switch
    {
        "f32" => FieldKind.Float,
        "f64" => FieldKind.Double,
        "bool" => FieldKind.Bool,
        "u8" or "u16" or "u32" or "u64" or "usize"
            or "i8" or "i16" or "i32" or "i64" or "isize" => FieldKind.Int,
        "alloc::string::String" => FieldKind.String,
        "glam::Vec2" => FieldKind.Vec2,
        "glam::Vec3" or "glam::Vec3A" => FieldKind.Vec3,
        "glam::Vec4" => FieldKind.Vec4,
        "glam::Quat" => FieldKind.Quat,
        "bevy_ecs::entity::Entity" => FieldKind.Entity,
        _ => null,
    };

    /// <summary>
    /// Reads a field and boxes it as the CLR type a generated schema would, or
    /// <see langword="null"/> when the entity does not carry the component.
    /// </summary>
    private static object? Read(
        EcsWorld world, Entity entity, string component, string path, FieldKind kind)
    {
        var json = Guarded(() => world.GetReflected(entity, component, path));
        return json is null ? null : ReflectedValue.Decode(json, kind);
    }

    /// <summary>
    /// Writes a value a tool handed over, reporting whether it landed.
    /// </summary>
    /// <remarks>
    /// The value is coerced first, as a generated setter does, because a slider hands a float field
    /// a double and a text box hands it a string.
    /// </remarks>
    private static bool Write(
        EcsWorld world, Entity entity, string component, string path, FieldKind kind, object value)
    {
        var text = ReflectedValue.Encode(kind, value);
        return text is not null && Sent(() => world.SetReflected(entity, component, path, text));
    }

    /// <summary>
    /// Runs a read, answering <see langword="null"/> where the bridge refused it.
    /// </summary>
    /// <remarks>
    /// A row is read every frame it is drawn, and a refusal (a path the running Bevy no longer has,
    /// a value with no JSON form) would otherwise end the frame each time. The reason is still there
    /// for <see cref="EcsWorld.GetReflected"/> to give to a caller who asks directly.
    /// </remarks>
    private static string? Guarded(Func<string?> read)
    {
        try
        {
            return read();
        }
        catch (BevyNativeException error) when (error.Status != NativeStatus.NoWorld)
        {
            return null;
        }
    }

    /// <summary>
    /// Runs a read of a value, answering <see langword="null"/> where the bridge refused it, for
    /// the reason <see cref="Guarded"/> gives.
    /// </summary>
    private static object? Attempted<T>(Func<T?> read) where T : struct
    {
        try
        {
            return read();
        }
        catch (BevyNativeException error) when (error.Status != NativeStatus.NoWorld)
        {
            return null;
        }
    }

    /// <summary>Runs a write, reporting whether the bridge took it, as a field's setter does.</summary>
    private static bool Sent(Action write)
    {
        try
        {
            write();
            return true;
        }
        catch (BevyNativeException error) when (error.Status != NativeStatus.NoWorld)
        {
            return false;
        }
    }

    /// <summary>Joins a field's name onto the path of the row holding it.</summary>
    private static string Join(string path, string name) =>
        path.Length == 0 ? name : path + "." + name;

    /// <summary>
    /// Turns a Rust field name into a label: <c>shadows_enabled</c> becomes "Shadows enabled".
    /// </summary>
    private static string Humanize(string name)
    {
        if (name.Length == 0) return name;

        var words = name.Replace('_', ' ').Trim();
        return words.Length == 0
            ? name
            : char.ToUpper(words[0], CultureInfo.InvariantCulture) + words[1..];
    }
}

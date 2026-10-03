using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The fields that choose something that already exists.
/// </summary>
/// <remarks>
/// <para>
/// An asset, another entity, or some of a set of flags. What these have in common is that the field
/// cannot say what may be put in it without asking something else: the files under the asset root,
/// the entities in the world, the names the flags were declared with.
/// </para>
/// <para>
/// Apart from <see cref="ComponentFields"/> for the same reason <see cref="FieldNumbers"/> is,
/// because gathering what can be chosen is a different job from drawing a row, and a row that reads
/// the disk should be easy to find.
/// </para>
/// </remarks>
internal static class FieldPickers
{
    /// <summary>A file under the asset root, chosen from what suits the field.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="field">Which field.</param>
    /// <param name="id">What to call the widget, which is unique to the field.</param>
    /// <param name="value">What the field holds.</param>
    internal static void Asset(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        // What the picker chose since the last frame, written here, inside the row, so the row
        // records it as an edit it made and puts it in the history like any other.
        if (Take(entity, field) is AssetHandle picked) field.Write(ctx.Ecs, entity, picked);

        var handle = value as AssetHandle? ?? AssetHandle.None;
        var held = Called(handle);
        var kind = field.Hints.Asset ?? AssetKind.Mesh;

        // An image shows itself beside its name, at the height of the row, so a texture slot says
        // what it holds at a glance rather than by a path to read.
        if (kind == AssetKind.Image && AssetServer.PathOf(handle) is { Length: > 0 } picture)
        {
            var side = ImGui.GetFrameHeight();
            var at = ImGui.GetCursorScreenPos();
            ImGui.Dummy(new System.Numerics.Vector2(side, side));
            EditorDraw.Picture(ImGui.GetWindowDrawList(), picture, at, side);
            if (ImGui.IsItemHovered()) EditorWidgets.Tip(picture);

            ImGui.SameLine(0f, ImGui.GetStyle().ItemInnerSpacing.X);
        }

        // The grid the mesh and material rows open, so every asset is picked one way, by its
        // picture where it has one. The files are read when the window opens rather than each
        // frame it is up, since a texture slot also offers the images inside every model, and
        // reading every model sixty times a second to offer them is a cost for nothing.
        if (ImGui.Button($"{held}##{id}", new System.Numerics.Vector2(-1f, 0f)))
        {
            var offered = new Lazy<IReadOnlyList<PickerItem>>(() => Offered(entity, field, kind));
            PickerWindow.Open(Hint(kind), () => offered.Value, "Nothing to pick", "Pick", grid: true);
        }
    }

    /// <summary>What an asset field can be given: nothing, then the files that suit it.</summary>
    private static IReadOnlyList<PickerItem> Offered(Entity entity, ComponentField field, string kind)
    {
        var items = new List<PickerItem>
        {
            new("Nothing", EditorIcons.File, _ => Give(entity, field, AssetHandle.None)),
        };

        foreach (var file in EditorAssets.Every(Suits(field, kind)))
        {
            // Loaded when it is chosen rather than when the list was built. A list that loaded
            // everything it offered would load the project to ask a question.
            items.Add(new PickerItem(
                EditorAssets.NameOf(file),
                EditorAssets.IconOf(file),
                _ => Give(entity, field, AssetServer.Load(kind, file)),
                "Files",
                file));
        }

        // An image a model carries inside it is an image like any other, and the browser shows it
        // among the model's parts, so a texture slot offers it as well.
        if (kind == AssetKind.Image && field.Hints.Extensions is not { Length: > 0 })
        {
            foreach (var model in EditorAssets.Every(EditorAssets.ExtensionsFor(AssetKind.Gltf)))
            {
                foreach (var part in GltfContents.Read(model) ?? [])
                {
                    if (part.Kind != AssetKind.Image || part.File is not null) continue;

                    var path = part.PathIn(model);
                    items.Add(new PickerItem(
                        EditorAssets.NameOf(path),
                        EditorIcons.Image,
                        _ => Give(entity, field, AssetServer.Load(kind, path)),
                        "Inside models",
                        path));
                }
            }
        }

        return items;
    }

    /// <summary>What the picker's search box says while it waits, by what is being picked.</summary>
    private static string Hint(string kind) => kind switch
    {
        AssetKind.Image => "Pick an image",
        AssetKind.Audio => "Pick a sound",
        AssetKind.Font => "Pick a font",
        AssetKind.Scene or AssetKind.Gltf => "Pick a scene",
        _ => "Pick a file",
    };

    /// <summary>
    /// Holds what the picker chose for a field until the field's row is next drawn, which writes it.
    /// </summary>
    /// <remarks>
    /// The picker is a window drawn apart from the details, after them, so a value it wrote itself
    /// would land outside the row and never be seen changing by it. The row records an edit in the
    /// history, marks an instance's override and runs what the field asked to have run when it
    /// changes, and a value it never saw change gets none of that. Held by the entity and the field, so a value picked for one entity is not
    /// written to another selected in the meantime.
    /// </remarks>
    internal static void Give(Entity entity, ComponentField field, object value) => Picked[(entity, field)] = value;

    /// <summary>What the picker chose for a field, once, or nothing.</summary>
    internal static object? Take(Entity entity, ComponentField field) =>
        Picked.Remove((entity, field), out var value) ? value : null;

    private static readonly Dictionary<(Entity, ComponentField), object> Picked = [];

    /// <summary>A link to another entity, chosen from the ones with names.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="field">Which field.</param>
    /// <param name="id">What to call the widget, which is unique to the field.</param>
    /// <param name="value">What the field holds.</param>
    internal static void Link(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        var held = value as Entity? ?? Entity.None;

        var name = held.IsNone
            ? "none"
            : ctx.Ecs.NameOf(held) ?? $"Entity {held.Index}";

        EditorWidgets.Picking(id, name, () =>
        {
            if (ImGui.Selectable("Nothing")) field.Write(ctx.Ecs, entity, Entity.None);

            RoundedRows.Row();

            foreach (var other in ctx.Ecs.All())
            {
                if (ctx.Ecs.NameOf(other) is not { Length: > 0 } called) continue;
                if (EditorEntity.IsInterface(ctx.Ecs, other)) continue;

                var picked = other == held;

                if (ImGui.Selectable(called, picked)) field.Write(ctx.Ecs, entity, other);

                RoundedRows.Row(picked);
            }
        });
    }

    /// <summary>Any number of a fixed set of names, each its own box to tick.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="field">Which field.</param>
    /// <param name="id">What to call the widget, which is unique to the field.</param>
    /// <param name="value">What the field holds.</param>
    internal static void Flags(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        // Any number of a fixed set at once, so one box per name rather than one choice.
        var said = value?.ToString() ?? string.Empty;
        var chosen = said.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .ToHashSet();

        var changed = false;

        foreach (var option in field.Options)
        {
            var on = chosen.Contains(option);

            // The name of the flag is part of the identifier, not only of the label. ImGui hashes
            // only what follows the two hashes, so every box in a set of flags sharing the field's
            // id is a set of boxes ImGui cannot tell apart. It says so, in a window that takes the
            // keyboard with it.
            if (EditorWidgets.Ticked($"{option}##{id}.{option}", ref on))
            {
                if (on) chosen.Add(option);
                else chosen.Remove(option);

                changed = true;
            }
        }

        if (changed) field.Write(ctx.Ecs, entity, string.Join(", ", chosen));
    }

    /// <summary>What a handle's file is called, or a word for a field holding nothing.</summary>
    /// <remarks>
    /// The path rather than the key, because a key is a number the engine made up and the file is
    /// the thing somebody chose. Remembered per handle, since a key's path never changes and asking
    /// crosses the ABI and copies a string, which a row drawn sixty times a second should not do.
    /// </remarks>
    /// <param name="handle">What the field holds.</param>
    private static string Called(AssetHandle handle)
    {
        if (!handle.IsValid) return "none";
        if (Paths.TryGetValue(handle, out var known)) return known;

        // Nothing is remembered until there is a path to remember. A handle asked about before its
        // file has been read has none yet, and a row that cached that would say a number for as
        // long as the editor runs.
        if (AssetServer.PathOf(handle) is not { Length: > 0 } path)
        {
            return EditorText.Short(handle.ToString());
        }

        Paths[handle] = path;
        return path;
    }

    /// <summary>What each handle a field has held is called.</summary>
    private static readonly Dictionary<AssetHandle, string> Paths = [];

    /// <summary>Which files suit a field: what it asked for, or what its kind usually holds.</summary>
    private static IReadOnlyCollection<string> Suits(ComponentField field, string kind)
    {
        if (field.Hints.Extensions is { Length: > 0 } asked)
        {
            return asked.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [.. EditorAssets.ExtensionsFor(kind)];
    }
}

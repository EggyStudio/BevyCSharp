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
        // What it holds, and a list of what it could hold instead. The files are asked for when
        // the list opens rather than when the row is drawn, because a field that listed the
        // project every frame would read the disk sixty times a second.
        var handle = value as AssetHandle? ?? AssetHandle.None;
        var held = Called(handle);
        var kind = field.Hints.Asset ?? AssetKind.Mesh;

        EditorWidgets.Picking(id, held, () =>
        {
            if (ImGui.Selectable("Nothing")) field.Write(ctx.Ecs, entity, AssetHandle.None);

            RoundedRows.Row();

            foreach (var file in EditorAssets.Every(Suits(field, kind)))
            {
                var chosen = ImGui.Selectable(file, file == held);

                RoundedRows.Row(file == held);

                // Loaded when it is chosen rather than when the list was built. A list that
                // loaded everything it offered would load the project to ask a question.
                if (chosen) field.Write(ctx.Ecs, entity, AssetServer.Load(kind, file));
            }
        });
    }

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

namespace Bevy;

/// <summary>
/// What a scene file held for an entity that this build could not read, kept so the next save
/// writes it back.
/// </summary>
/// <remarks>
/// <para>
/// A component whose type this build does not have, or that Bevy would not take, and the values of
/// a known component that no field reads. Each is the JSON the file had for it. Without this a
/// branch that drops a type, or an older build opening a newer scene, would save the scene without
/// them and lose them for every branch after.
/// </para>
/// <para>
/// The component holds a handle into the managed store, freed by its remove hook and copied by its
/// clone hook, as an <see cref="EcsList{T}"/> is. It has no schema, so a tool does not show it.
/// </para>
/// </remarks>
internal struct SceneKept
{
    internal int Slot;
    internal int Generation;

    /// <summary>The kept components and values, or nothing for a handle that names no slot.</summary>
    internal readonly Kept? Value => EcsStore.Get(Slot, Generation) as Kept;

    /// <summary>Puts kept JSON in the store, for an entity to carry.</summary>
    internal static SceneKept Of(Kept kept)
    {
        var (slot, generation) = EcsStore.Allocate(kept);
        return new SceneKept { Slot = slot, Generation = generation };
    }

    /// <summary>Registers the hooks that free and copy the slot.</summary>
    /// <remarks>
    /// Called from <see cref="SceneFile"/>'s static constructor, which runs before anything in it
    /// can touch the component, and so before an app registers the type and attaches what is
    /// declared for it.
    /// </remarks>
    internal static void Initialize()
    {
        ComponentHooks.OnRemove(static (in SceneKept kept) => EcsStore.Release(kept.Slot, kept.Generation));
        ComponentHooks.OnClone(static (ref SceneKept kept) =>
        {
            if (kept.Value is { } value) kept = Of(value.Copy());
        });
    }

    /// <summary>The JSON a file held that this build could not read.</summary>
    internal sealed class Kept
    {
        /// <summary>Whole components, by the name the file gave them, with their JSON.</summary>
        public List<KeyValuePair<string, string>> Components { get; } = [];

        /// <summary>Values no field reads, by the component's current name.</summary>
        public Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>> Fields { get; } = [];

        public bool IsEmpty => Components.Count == 0 && Fields.Count == 0;

        public Kept Copy()
        {
            var copy = new Kept();
            copy.Components.AddRange(Components);
            foreach (var (name, values) in Fields) copy.Fields[name] = values;
            return copy;
        }
    }
}

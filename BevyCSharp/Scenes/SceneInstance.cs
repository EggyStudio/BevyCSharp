namespace Bevy;

/// <summary>
/// Marks the entity a scene asset was placed under as an instance, holding which scene it is and
/// the changes made to it.
/// </summary>
/// <remarks>
/// A handle into the managed store, freed with the entity and copied with a clone, as
/// the values a scene keeps for what it cannot read are. A tool asks whether an entity carries one
/// to learn that it is an instance, and reads what it holds through <see cref="SceneInstances"/>.
/// </remarks>
public struct SceneInstance
{
    internal int Slot;
    internal int Generation;

    internal readonly SceneInstances.Data? Value => EcsStore.Get(Slot, Generation) as SceneInstances.Data;

    internal static SceneInstance Of(SceneInstances.Data data)
    {
        var (slot, generation) = EcsStore.Allocate(data);
        return new SceneInstance { Slot = slot, Generation = generation };
    }

    /// <summary>Registers the hooks that free and copy the slot.</summary>
    /// <remarks>Called from the static constructors of everything that puts one on an entity.</remarks>
    internal static void Initialize()
    {
        ComponentHooks.OnRemove(static (in SceneInstance instance) => EcsStore.Release(instance.Slot, instance.Generation));
        ComponentHooks.OnClone(static (ref SceneInstance instance) =>
        {
            if (instance.Value is { } value) instance = Of(value.Copy());
        });
    }
}

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor is currently pointed at.
/// </summary>
/// <remarks>
/// <para>
/// One entity, held in one place, because the alternative is every panel holding its own idea of
/// what is selected and a web of panels telling each other. A hierarchy writes it, an inspector
/// reads it, a toolbar acts on it, and none of the three knows the others exist.
/// </para>
/// <para>
/// Selection is not an ECS component. It belongs to the tool rather than to the world: an entity
/// does not become different by being looked at, and a world saved while something was selected
/// should not carry that.
/// </para>
/// </remarks>
public static class EditorSelection
{
    /// <summary>The selected entity, or <see cref="Entity.None"/>.</summary>
    public static Entity Current { get; private set; } = Entity.None;

    /// <summary>Which frame the selection last changed on, so a panel can notice.</summary>
    public static ulong ChangedOn { get; private set; }

    /// <summary>Whether anything is selected.</summary>
    public static bool Any => !Current.IsNone;

    /// <summary>Points the editor at an entity.</summary>
    public static void Select(Entity entity)
    {
        if (entity == Current) return;

        Current = entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }

    /// <summary>Points it at nothing.</summary>
    public static void Clear() => Select(Entity.None);

    /// <summary>
    /// Drops a selection whose entity has gone.
    /// </summary>
    /// <remarks>
    /// Called once a frame by the shell. An entity can be despawned by anything, including a
    /// script the editor just reloaded, and an inspector reading a dead entity would show the
    /// bytes of whatever took its place in storage.
    /// </remarks>
    internal static void Prune(EcsWorld world)
    {
        if (Current.IsNone) return;
        if (world.IsAlive(Current)) return;

        Clear();
    }
}

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>What sort of thing the editor is pointed at.</summary>
public enum SelectionKind
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>An entity in the world.</summary>
    Entity,

    /// <summary>A file in the asset directory.</summary>
    Asset,
}

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
    /// <summary>Which kind of thing was picked last.</summary>
    /// <remarks>
    /// The data panel shows one thing, and this is how it knows which: picking a file does not
    /// deselect an entity, it just becomes the more recent answer to "what am I looking at".
    /// </remarks>
    public static SelectionKind Latest { get; internal set; } = SelectionKind.None;

    /// <summary>The selected entity, or <see cref="Entity.None"/>.</summary>
    public static Entity Current { get; private set; } = Entity.None;

    /// <summary>
    /// The camera the viewport is looking through.
    /// </summary>
    /// <remarks>
    /// Held beside the selection because everything that draws into the viewport needs it: a
    /// handle is projected through this camera, a click is turned into a ray through it, and the
    /// orientation cross is drawn in front of it.
    /// </remarks>
    public static Entity Camera { get; set; } = Entity.None;

    /// <summary>Which frame the selection last changed on, so a panel can notice.</summary>
    public static ulong ChangedOn { get; private set; }

    /// <summary>Whether anything is selected.</summary>
    public static bool Any => !Current.IsNone;

    /// <summary>Points the editor at an entity.</summary>
    public static void Select(Entity entity)
    {
        if (entity == Current) return;

        Current = entity;
        Latest = entity.IsNone ? SelectionKind.None : SelectionKind.Entity;
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

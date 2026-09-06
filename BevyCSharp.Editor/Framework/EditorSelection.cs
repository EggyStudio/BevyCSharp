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
/// Held in one place, because the alternative is every panel holding its own idea of what is
/// selected and a web of panels telling each other. A hierarchy writes it, an inspector reads it,
/// a toolbar acts on it, and none of the three knows the others exist.
/// </para>
/// <para>
/// More than one entity can be selected, and one of them is the current one: the last one picked.
/// Everything that acts on a single thing acts on that one, and everything that can act on many
/// (an inspector writing a field, a menu row deleting) reads the whole list. A list of one is the
/// ordinary case and reads exactly as it did when one was all there could be.
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

    /// <summary>The entity picked last, or <see cref="Entity.None"/>.</summary>
    /// <remarks>
    /// What everything acting on one thing acts on: the gizmo handles, the camera framing a
    /// selection, the inspector's heading. When several are selected it is the last one picked,
    /// which is the one somebody was looking at when they picked it.
    /// </remarks>
    public static Entity Current { get; private set; } = Entity.None;

    /// <summary>Everything selected, with <see cref="Current"/> last.</summary>
    public static IReadOnlyList<Entity> All => Chosen;

    /// <summary>How many are selected.</summary>
    public static int Count => Chosen.Count;

    /// <summary>The entities picked, oldest first.</summary>
    private static readonly List<Entity> Chosen = [];

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

    /// <summary>Points the editor at one entity, instead of whatever it was pointed at.</summary>
    public static void Select(Entity entity)
    {
        if (entity == Current && Chosen.Count <= 1) return;

        Chosen.Clear();
        if (!entity.IsNone) Chosen.Add(entity);

        Current = entity;
        Latest = entity.IsNone ? SelectionKind.None : SelectionKind.Entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }

    /// <summary>
    /// Adds an entity to the selection, or takes it out again.
    /// </summary>
    /// <remarks>
    /// What holding a modifier while clicking does, in every tool there has ever been. Taking the
    /// current one out leaves whichever was picked before it as the current one, so the handles
    /// stay on something rather than disappearing.
    /// </remarks>
    public static void Toggle(Entity entity)
    {
        if (entity.IsNone) return;

        if (Chosen.Remove(entity))
        {
            Current = Chosen.Count > 0 ? Chosen[^1] : Entity.None;
            Latest = Current.IsNone ? SelectionKind.None : SelectionKind.Entity;
            ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
            return;
        }

        Chosen.Add(entity);
        Current = entity;
        Latest = SelectionKind.Entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }

    /// <summary>Whether an entity is one of the selected.</summary>
    public static bool Holds(Entity entity) => Chosen.Contains(entity);

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
        if (Chosen.Count == 0) return;

        var went = Chosen.RemoveAll(entity => !world.IsAlive(entity));
        if (went == 0) return;

        Current = Chosen.Count > 0 ? Chosen[^1] : Entity.None;
        Latest = Current.IsNone ? SelectionKind.None : SelectionKind.Entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }
}

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

        // Choosing nothing is a decision, and what is remembered is only there to survive a
        // reload. Left in place it comes straight back on the next frame, which is a selection
        // that cannot be let go of.
        if (entity.IsNone) Named.Clear();

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
            if (Chosen.Count == 0) Named.Clear();

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
    /// bytes of whatever took its place in storage. It is also where a selection comes back: what
    /// a reload despawned it usually spawns again, under the same name.
    /// </remarks>
    public static void Prune(EcsWorld world)
    {
        if (Chosen.Count == 0)
        {
            Recover(world);
            return;
        }

        // What each of them is called, kept while they are all alive. An id says nothing once the
        // entity behind it is gone, and a name is the only thing about a selection that outlives
        // the entity holding it.
        //
        // Only while they are all alive: the frame something is despawned is the frame its name
        // can no longer be asked for, so remembering then would forget exactly the name that is
        // about to be needed.
        if (Chosen.TrueForAll(world.IsAlive)) Remember(world);

        var went = Chosen.RemoveAll(entity => !world.IsAlive(entity));
        if (went == 0) return;

        Current = Chosen.Count > 0 ? Chosen[^1] : Entity.None;
        Latest = Current.IsNone ? SelectionKind.None : SelectionKind.Entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }

    /// <summary>What the selection was called, in the order it was selected.</summary>
    private static readonly List<string> Named = [];

    /// <summary>Keeps the names of everything selected, while they can still be asked for.</summary>
    private static void Remember(EcsWorld world)
    {
        Named.Clear();

        foreach (var entity in Chosen)
        {
            if (world.NameOf(entity) is not { Length: > 0 } name) continue;

            Named.Add(name);
        }
    }

    /// <summary>
    /// Selects by name what was selected by id before a reload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A script that is reloaded despawns what it made and makes it again, and the new entities
    /// have new ids: what was selected is gone in the only sense the editor can see. Losing the
    /// selection on every save is the difference between editing a value while the game runs and
    /// finding the thing again each time.
    /// </para>
    /// <para>
    /// By name, which is the only thing that survives, and so wrong for two entities that share
    /// one: the first with the name is taken. That is worth it, and the alternative is a stable
    /// identity the engine does not have.
    /// </para>
    /// </remarks>
    private static void Recover(EcsWorld world)
    {
        if (Named.Count == 0) return;

        var found = new List<Entity>();

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) is not { Length: > 0 } name) continue;
            if (!Named.Contains(name)) continue;
            if (found.Contains(entity)) continue;

            found.Add(entity);
        }

        // All of them or none. Half a selection coming back is worse than none: an edit meant for
        // three things would reach two of them without saying so.
        if (found.Count != Named.Count) return;

        Named.Clear();
        Chosen.AddRange(found);

        Current = Chosen[^1];
        Latest = SelectionKind.Entity;
        ChangedOn = EditorShell.Context?.Time.FrameCount ?? 0;
    }
}

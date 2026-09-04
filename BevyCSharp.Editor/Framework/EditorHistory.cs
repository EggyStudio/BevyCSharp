using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>One change, and how to take it back or put it back.</summary>
/// <param name="What">A short line saying what was done, for a status strip or a menu.</param>
/// <param name="Undo">Puts the world back the way it was.</param>
/// <param name="Redo">Does it again.</param>
/// <param name="Key">
/// What this edit is to the same thing as, or <see langword="null"/> for an edit that stands
/// alone. Two edits with the same key, one straight after the other, are the same edit continued.
/// </param>
public sealed record EditorEdit(
    string What,
    Action<EcsWorld> Undo,
    Action<EcsWorld> Redo,
    string? Key = null);

/// <summary>
/// What the editor has changed, so that it can be taken back.
/// </summary>
/// <remarks>
/// <para>
/// A pair of stacks over closures rather than a log of component bytes. An edit knows how to
/// reverse itself because whatever made it knew both values at the time, and that is cheaper and
/// more honest than snapshotting a world and diffing it.
/// </para>
/// <para>
/// <b>What is not here.</b> Despawning is not undoable and is deliberately not recorded: an
/// entity's mesh and material are engine-side components with no mirror on this side, so what
/// came back would be an entity with the right name and nothing to draw. Recording it would make
/// undo look like it worked. The rule this follows is that an operation goes in the history only
/// when it can be reversed exactly.
/// </para>
/// </remarks>
public static class EditorHistory
{
    /// <summary>How many edits are kept.</summary>
    /// <remarks>
    /// Enough for an afternoon of nudging values, and bounded because each edit holds closures
    /// over whatever it captured.
    /// </remarks>
    private const int Depth = 128;

    private static readonly List<EditorEdit> Done = [];
    private static readonly List<EditorEdit> Undone = [];

    /// <summary>What the last change was, or <see langword="null"/> when nothing has changed.</summary>
    public static string? Last => Done.Count > 0 ? Done[^1].What : null;

    /// <summary>Whether there is anything to take back.</summary>
    public static bool CanUndo => Done.Count > 0;

    /// <summary>Whether there is anything to put back.</summary>
    public static bool CanRedo => Undone.Count > 0;

    /// <summary>
    /// Records a change that has already been made.
    /// </summary>
    /// <remarks>
    /// Recording clears what was undone, which is what every editor does: once the world has been
    /// changed by hand, the branch that was undone is no longer a thing to return to.
    /// </remarks>
    public static void Record(
        string what,
        Action<EcsWorld> undo,
        Action<EcsWorld> redo,
        string? key = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(what);
        ArgumentNullException.ThrowIfNull(undo);
        ArgumentNullException.ThrowIfNull(redo);

        Undone.Clear();

        // Typing a number into a field is one edit to the person doing it and a keystroke's worth
        // of edits to the program. When the last edit was to the same field and nothing else has
        // happened since, this continues it: the undo stays the value before the typing started
        // and the redo becomes whatever was typed last.
        if (key is not null && Done.Count > 0 && Done[^1].Key == key)
        {
            Done[^1] = Done[^1] with { What = what, Redo = redo };
            return;
        }

        Done.Add(new EditorEdit(what, undo, redo, key));

        if (Done.Count > Depth) Done.RemoveAt(0);
    }

    /// <summary>Takes the last change back, reporting what it was.</summary>
    public static string? Undo(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (Done.Count == 0) return null;

        var edit = Done[^1];
        Done.RemoveAt(Done.Count - 1);

        edit.Undo(world);
        Undone.Add(edit);

        return edit.What;
    }

    /// <summary>Puts the last undone change back, reporting what it was.</summary>
    public static string? Redo(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (Undone.Count == 0) return null;

        var edit = Undone[^1];
        Undone.RemoveAt(Undone.Count - 1);

        edit.Redo(world);
        Done.Add(edit);

        return edit.What;
    }

    /// <summary>Forgets everything, which a world being replaced calls for.</summary>
    public static void Clear()
    {
        Done.Clear();
        Undone.Clear();
    }
}

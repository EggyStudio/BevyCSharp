using Bevy;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor;

/// <summary>
/// The keys that open and close panels.
/// </summary>
/// <remarks>
/// <para>
/// Kept here rather than on the panels, so that what a key does is answerable by reading one
/// file, and so that <see cref="KeysPanel"/> can list the same table it is bound to rather than a
/// paragraph someone typed and then forgot to change.
/// </para>
/// <para>
/// Every one of these toggles: pressing it again closes what it opened. An editor where a key
/// only opens things is one where the way back is the mouse.
/// </para>
/// </remarks>
[Behavior]
public partial struct EditorKeys
{
    /// <summary>The camera the post panel binds to, set when the editor starts.</summary>
    /// <remarks>
    /// Static because a key press is not an entity's business: the behavior has no instances and
    /// exists only to hold the systems.
    /// </remarks>
    public static Entity Camera { get; set; } = Entity.None;

    /// <summary>What each key does, which is also what the key list shows.</summary>
    /// <remarks>
    /// One table, read by the system that acts on it and by the panel that lists it. A key that
    /// stops working stops being listed, because there is nowhere else for the list to come from.
    /// </remarks>
    public static readonly (Key Key, string Name, string Does)[] Panels =
    [
        (Key.F2, "F2", "hierarchy"),
        (Key.F3, "F3", "inspector"),
        (Key.F4, "F4", "post processing"),
        (Key.F5, "F5", "status strip"),
        (Key.F6, "F6", "this list"),
    ];

    /// <summary>What the editing keys do, listed beside the panel keys.</summary>
    public static readonly (Key Key, string Name, string Does)[] Editing =
    [
        (Key.Z, "Ctrl Z", "take back the last change"),
        (Key.Y, "Ctrl Y", "put it back"),
    ];

    /// <summary>Opens and closes panels from the keyboard.</summary>
    [OnUpdate]
    public static void Toggle(BehaviorContext ctx)
    {
        if (!App.HasEditor) return;

        if (ctx.Input.KeyPressed(Key.F2)) EditorShell.Toggle(() => new HierarchyPanel());
        if (ctx.Input.KeyPressed(Key.F3)) EditorShell.Toggle(() => new InspectorPanel());
        if (ctx.Input.KeyPressed(Key.F4)) EditorShell.Toggle(() => new PostPanel(Camera));
        if (ctx.Input.KeyPressed(Key.F5)) EditorShell.Toggle(() => new StatsPanel());
        if (ctx.Input.KeyPressed(Key.F6)) EditorShell.Toggle(() => new KeysPanel());
    }

    /// <summary>Takes the last change back, or puts it back.</summary>
    /// <remarks>
    /// Here rather than on a panel, because undo is the editor's and not any one panel's: a value
    /// changed in the inspector and an entity made in the hierarchy go on the same stack.
    /// </remarks>
    [OnUpdate]
    public static void History(BehaviorContext ctx)
    {
        if (!App.HasEditor) return;
        if (!ctx.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight])) return;

        if (ctx.Input.KeyPressed(Key.Z) && EditorHistory.Undo(ctx.Ecs) is { } undone)
            Console.WriteLine($"[editor] undid {undone}");

        if (ctx.Input.KeyPressed(Key.Y) && EditorHistory.Redo(ctx.Ecs) is { } redone)
            Console.WriteLine($"[editor] redid {redone}");
    }
}

using Bevy;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor;

/// <summary>
/// The keys, and what they do.
/// </summary>
/// <remarks>
/// <para>
/// Here rather than on the panels, so that what a key does is answerable by reading one file, and
/// so the strip along the bottom can list the same bindings that are acted on rather than a
/// paragraph somebody typed and then forgot to change.
/// </para>
/// <para>
/// A key that runs a menu command runs the menu's own row rather than a copy of it, which is what
/// keeps the two from drifting apart.
/// </para>
/// </remarks>
[Behavior]
public partial struct EditorKeys
{
    /// <summary>Chooses a tool, the way Q, W, E and R do everywhere else.</summary>
    /// <remarks>
    /// Not while the right button is held: that is the camera flying, and W and E belong to it
    /// then. Not while something is being typed into either, for the obvious reason.
    /// </remarks>
    [OnUpdate]
    public static void Tools(BehaviorContext ctx)
    {
        if (!App.HasEditor) return;
        if (!PanelBinding.Focused.IsNone) return;
        if (ctx.Input.MouseDown(MouseButton.Right)) return;

        foreach (var (key, tool, _) in EditorTools.Keys)
        {
            if (ctx.Input.KeyPressed(key)) EditorTools.Current = tool;
        }

        // X for the world's axes or the thing's own, which is where every editor puts it.
        if (ctx.Input.KeyPressed(Key.X))
        {
            EditorTools.Space = EditorTools.Space == ToolSpace.Local
                ? ToolSpace.Global
                : ToolSpace.Local;
        }

        // Held rather than pressed: snapping while a handle is being dragged is what a person
        // reaches for mid-drag, and a toggle would be the wrong shape for that.
        if (ctx.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight])
            && !ctx.Input.KeyDown(Key.ShiftLeft))
        {
            EditorTools.Snap = true;
        }
        else if (!SnapLocked)
        {
            EditorTools.Snap = false;
        }
    }

    /// <summary>Whether snapping was turned on from the toolbar rather than held.</summary>
    /// <remarks>
    /// So that holding Control does not silently undo a person's decision to leave it on.
    /// </remarks>
    public static bool SnapLocked { get; set; }

    /// <summary>Runs the commands that have a key of their own.</summary>
    [OnUpdate]
    public static void Commands(BehaviorContext ctx)
    {
        if (!App.HasEditor) return;
        if (!PanelBinding.Focused.IsNone) return;

        var input = ctx.Input;
        var control = input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]);

        if (control && input.KeyPressed(Key.Z) && EditorHistory.Undo(ctx.Ecs) is { } undone)
            Console.WriteLine($"[editor] undid {undone}");

        if (control && input.KeyPressed(Key.Y) && EditorHistory.Redo(ctx.Ecs) is { } redone)
            Console.WriteLine($"[editor] redid {redone}");

        if (control && input.KeyPressed(Key.S)) EditorProject.Save(ctx.Ecs);

        if (input.KeyPressed(Key.Delete)) Run(ctx, "Entity/Delete");

        // The menu, which is otherwise only reachable through a button on a panel that can be
        // closed. A person who closes everything should still have a way back.
        if (input.KeyPressed(Key.F1))
        {
            var (x, y) = input.MousePosition;
            EditorShell.ShowMenu(string.Empty, x, y);
        }
    }

    /// <summary>Runs a menu row by its path, so a key and a menu cannot disagree.</summary>
    private static void Run(BehaviorContext ctx, string path) =>
        EditorMenu.Find(path)?.Run?.Invoke(ctx.Ecs);
}

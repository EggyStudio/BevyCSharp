using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Choosing several things at once by dragging a box round them.
/// </summary>
/// <remarks>
/// <para>
/// What a drag on empty space means everywhere else a person has ever dragged on empty space. A
/// click picks one thing, and picking a dozen by clicking each of them with a modifier held is a
/// dozen chances to miss.
/// </para>
/// <para>
/// Anything whose box on the screen touches the one being dragged is taken, rather than only what
/// is wholly inside it: a drag round part of a large object is a drag that meant that object, and a
/// rule that needs the whole of a thing inside the box cannot reach anything larger than the view.
/// </para>
/// <para>
/// Drawn and decided here rather than in a behavior, because both halves need the frame the shell
/// is in the middle of: the box goes on ImGui's background list, and what the pointer is allowed to
/// start is a question about whether the interface wants it.
/// </para>
/// </remarks>
public static class MarqueeSelect
{
    /// <summary>Where the drag began, or nothing while there is no drag.</summary>
    private static Vector2? _from;

    /// <summary>Whether what it finds is added to the selection rather than replacing it.</summary>
    private static bool _adds;

    /// <summary>How far the pointer has to travel before a click becomes a drag, in pixels.</summary>
    private const float Slack = 4f;

    /// <summary>Whether a box is being dragged right now.</summary>
    /// <remarks>
    /// Asked by the picking that runs on a release: a release that ended a box is not also a click
    /// on whatever happened to be under it.
    /// </remarks>
    public static bool Dragging { get; private set; }

    /// <summary>
    /// Whether the scene was clicked this frame without a box being dragged.
    /// </summary>
    /// <remarks>
    /// What says a click landed on the sky. The engine answers a click that hit something by
    /// handing over what it hit and says nothing at all about one that hit nothing, so the only
    /// way to know the difference is to notice the click and wait to be told.
    /// </remarks>
    public static bool Clicked { get; private set; }

    /// <summary>How many clicks on the scene there have been, which a probe can check.</summary>
    public static int Clicks { get; private set; }

    /// <summary>Takes the pointer's part in this frame, and draws the box if there is one.</summary>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        // Only the tool that is about choosing things. The others own a drag on the viewport: it
        // moves, turns or scales what is already chosen.
        Clicked = false;

        if (EditorTools.Current != EditorTool.Select)
        {
            _from = null;
            Dragging = false;
            return;
        }

        var input = ctx.Input;
        var at = new Vector2(input.MouseX, input.MouseY);

        if (input.MousePressed(MouseButton.Left) && !ImGuiRuntime.WantsMouse)
        {
            _from = at;
            _adds = input.AnyKeyDown([Key.ShiftLeft, Key.ShiftRight]);
        }

        if (_from is not { } start)
        {
            Dragging = false;
            return;
        }

        var far = Vector2.Distance(start, at) > Slack;

        if (input.MouseDown(MouseButton.Left))
        {
            Dragging = far;
            if (far) Box(start, at);
            return;
        }

        _from = null;
        Clicked = !far;

        if (Clicked) Clicks++;

        // Still counted as a drag on the frame it ends, so that the release which closed the box is
        // not also read as a click by the picking that runs after this. It goes back to false on
        // the next frame, where there is no drag to find.
        Dragging = far;

        // A drag that went nowhere is a click, and a click is the engine's to answer: it raycasts
        // the scene and knows what is in front of what, which no rectangle on the screen does.
        if (far) Choose(ctx, start, at, _adds);
    }

    /// <summary>Draws the box being dragged.</summary>
    private static void Box(Vector2 from, Vector2 to)
    {
        var low = Vector2.Min(from, to);
        var high = Vector2.Max(from, to);

        var draw = ImGui.GetBackgroundDrawList();
        var accent = EditorTheme.LiveAccent;

        draw.AddRectFilled(low, high, ImGui.GetColorU32(EditorTheme.Alpha(accent, 0.25f)), 2f);
        draw.AddRect(low, high, ImGui.GetColorU32(accent), 2f, ImDrawFlags.None, 1.5f);
    }

    /// <summary>Takes everything the box touches.</summary>
    private static void Choose(BehaviorContext ctx, Vector2 from, Vector2 to, bool adds)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var low = Vector2.Min(from, to);
        var high = Vector2.Max(from, to);

        var found = new List<Entity>();

        foreach (var entity in ctx.Ecs.All())
        {
            if (entity == camera) continue;

            // The same things the world list shows, and for the same reason: the interface's own
            // entities and the engine's bookkeeping have meshes and so have boxes on the screen,
            // and a drag across the viewport that picks up the gizmo renderer has picked up
            // nothing a person meant.
            if (EditorEntity.IsInterface(ctx.Ecs, entity)) continue;
            if (EditorEntity.IsBookkeeping(ctx.Ecs, entity)) continue;

            // Having a box to draw is the same test the world list uses for something nobody
            // named, and it is the whole test here: what a drag over the viewport can take is
            // what the viewport is showing.
            if (!Render.TryGetBounds(entity, out var min, out var max)) continue;
            if (!Touches(camera, min, max, low, high)) continue;

            found.Add(entity);
        }

        if (found.Count == 0)
        {
            if (!adds) EditorSelection.Clear();
            return;
        }

        if (!adds) EditorSelection.Select(found[0]);

        foreach (var entity in found)
        {
            if (!EditorSelection.Holds(entity)) EditorSelection.Toggle(entity);
        }
    }

    /// <summary>
    /// Whether a box in the world lands anywhere inside a box on the screen.
    /// </summary>
    /// <remarks>
    /// Through its eight corners rather than its middle. A wall across the view has a middle that a
    /// small rectangle never contains, and dragging over a wall means the wall.
    /// </remarks>
    private static bool Touches(Entity camera, Vec3 min, Vec3 max, Vector2 low, Vector2 high)
    {
        var seen = false;
        var left = float.MaxValue;
        var top = float.MaxValue;
        var right = float.MinValue;
        var bottom = float.MinValue;

        for (var corner = 0; corner < 8; corner++)
        {
            var point = new Vec3(
                (corner & 1) == 0 ? min.X : max.X,
                (corner & 2) == 0 ? min.Y : max.Y,
                (corner & 4) == 0 ? min.Z : max.Z);

            if (!Render.TryProject(camera, point, out var x, out var y)) continue;

            seen = true;
            left = MathF.Min(left, x);
            top = MathF.Min(top, y);
            right = MathF.Max(right, x);
            bottom = MathF.Max(bottom, y);
        }

        if (!seen) return false;

        return right >= low.X && left <= high.X && bottom >= low.Y && top <= high.Y;
    }
}

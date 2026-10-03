using Bevy;
using BevyCSharp.Editor.Behaviors;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What a click on the scene selects, and what it clears.
/// </summary>
/// <remarks>
/// The editor answers this rather than a panel, because a selection belongs to the editor. The
/// engine raycasts the scene and knows nothing about the panels drawn over it, so which clicks
/// count and what an unanswered one means are decided here.
/// </remarks>
public static class EditorPicking
{
    /// <summary>The frame a click on the scene landed, while it is still waiting to be answered.</summary>
    private static ulong _emptyClick;

    /// <summary>The frame the engine last said a click had hit something.</summary>
    private static ulong _pickedOn;

    /// <summary>The frame the pointer's button last went down.</summary>
    private static ulong _pressedOn;

    /// <summary>Whether that press was on the scene rather than on the interface.</summary>
    private static bool _onScene;

    /// <summary>Whether it took hold of a transform handle at any point.</summary>
    private static bool _onHandle;

    /// <summary>How many frames the engine gets to say what a click hit before it hit nothing.</summary>
    private const ulong Patience = 3;


    /// <summary>
    /// Answers the clicks given to an editor with no window, which Bevy's picking cannot see, by
    /// casting a ray from the scene camera through each.
    /// </summary>
    /// <remarks>
    /// An editor opened with <c>--offscreen</c> and driven by <c>bcs</c> takes clicks on its
    /// panels from the interface and these from nothing else, so a click on the scene there
    /// selects what is under it and a click on nothing clears the selection, as it does in a
    /// window. The triangles the ray meets are the meshes', as a pick's are.
    /// </remarks>
    private static void Unwindowed()
    {
        while (SyntheticInput.TryTakeClickWithoutWindow(out var x, out var y))
        {
            if (EditorShell.PointerOverPanel) continue;

            var camera = EditorSelection.Camera;
            if (camera.IsNone || !Render.TryRay(camera, x, y, out var origin, out var direction)) continue;

            if (Picking.TryCast(origin, direction, out var hit, out _, out _)) EditorSelection.Select(hit);
            else EditorSelection.Clear();
        }
    }

    /// <summary>Takes this frame's clicks on the scene.</summary>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        MarqueeSelect.Tick(ctx);

        // Where the click that is being answered began. Asked at the press rather than now,
        // because the pointer moves. The engine raycasts the scene and knows nothing of the panels
        // drawn over it, so what decides whether a pick belongs to the editor is where the button
        // went down, and a hand that has since traveled over a panel has not changed that.
        if (ctx.Input.MousePressed(MouseButton.Left))
        {
            _pressedOn = EditorShell.Frame;
            _onScene = !EditorShell.PointerOverPanel;
            _onHandle = false;
        }

        // Remembered for the whole press rather than asked at the end of it. A drag on a handle
        // gives the axis up on the release, and the answer to what that release hit arrives a
        // frame or two later, by which time nothing is being dragged any more.
        if (TransformGizmo.DraggingOn(EditorShell.Frame)) _onHandle = true;

        // A click on the scene that hit nothing means nothing was meant, which is how every editor
        // clears a selection.
        //
        // Only when nothing has answered this click since the button went down. The engine
        // raycasts on its own schedule and the pointer is read on ours, so the answer can arrive
        // on the frame of the press, of the release, or after it; a rule that only waits for one
        // that comes later throws away a selection the moment it is made.
        var answered = _pickedOn >= _pressedOn;

        if (MarqueeSelect.Clicked && _onScene && !answered) _emptyClick = EditorShell.Frame;

        foreach (var picked in Picking.Drain())
        {
            if (!_onScene) break;

            // A release that ended a box is not also a click on whatever the pointer came to rest
            // over, because the box already said what it meant.
            if (MarqueeSelect.Dragging) break;

            // Nor is the release that ends a drag on a transform handle. Read as a click it
            // selects whatever was under the pointer, which throws away the rest of a selection
            // at the end of the very drag that was moving all of it.
            if (_onHandle) break;

            _pickedOn = EditorShell.Frame;
            _emptyClick = 0;

            EditorSelection.Select(picked);
        }

        Unwindowed();

        if (_emptyClick != 0 && EditorShell.Frame - _emptyClick >= Patience)
        {
            _emptyClick = 0;
            EditorSelection.Clear();
        }

    }
}

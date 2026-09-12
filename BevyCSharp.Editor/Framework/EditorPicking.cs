using Bevy;

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

    /// <summary>How many frames the engine gets to say what a click hit before it hit nothing.</summary>
    private const ulong Patience = 3;


    /// <summary>Takes this frame's clicks on the scene.</summary>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        MarqueeSelect.Tick(ctx);

        // Where the click that is being answered began. Asked at the press rather than now,
        // because the pointer moves: the engine raycasts the scene and knows nothing of the panels
        // drawn over it, so what decides whether a pick belongs to the editor is where the button
        // went down, and a hand that has since travelled over a panel has not changed that.
        if (ctx.Input.MousePressed(MouseButton.Left))
        {
            _pressedOn = EditorShell.Frame;
            _onScene = !ImGuiRuntime.WantsMouse;
        }

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
            // over: the box already said what it meant.
            if (MarqueeSelect.Dragging) break;

            _pickedOn = EditorShell.Frame;
            _emptyClick = 0;

            EditorSelection.Select(picked);
        }

        if (_emptyClick != 0 && EditorShell.Frame - _emptyClick >= Patience)
        {
            _emptyClick = 0;
            EditorSelection.Clear();
        }

    }
}

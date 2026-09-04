using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// A line along the bottom saying how the editor is doing.
/// </summary>
/// <remarks>
/// The smallest panel there is, and the one that shows a panel need not be interactive at all:
/// three readouts, no commands, and the same three files as everything else.
/// </remarks>
[EditorPanel("panels/stats.html", Root = "#stats", Region = EditorRegion.Bottom)]
public sealed partial class StatsPanel
{
    /// <summary>How fast the editor is running.</summary>
    [Bind("#stat-fps", Mode = BindMode.OneWay)]
    public string Rate { get; private set; } = string.Empty;

    /// <summary>How much is in the world.</summary>
    [Bind("#stat-world", Mode = BindMode.OneWay)]
    public string World { get; private set; } = string.Empty;

    /// <summary>What is selected.</summary>
    [Bind("#stat-selection", Mode = BindMode.OneWay)]
    public string Selected { get; private set; } = string.Empty;

    /// <summary>The change that would be taken back.</summary>
    [Bind("#stat-history", Mode = BindMode.OneWay)]
    public string Last { get; private set; } = string.Empty;

    /// <summary>How many entities there were when the count was last taken.</summary>
    private int _entities;

    /// <summary>Reads the frame and the world.</summary>
    /// <remarks>
    /// The entity count is taken twice a second rather than every frame. It walks the whole world
    /// and nothing about it is worth a frame's work sixty times a second, which is a different
    /// judgement from the hierarchy's because the hierarchy is what a person is reading.
    /// </remarks>
    [OnRefresh]
    public void Read()
    {
        if (EditorShell.Context is not { } ctx) return;

        Rate = $"{ctx.Time.SmoothedFps:F0} fps";

        if (ctx.Time.FrameCount % 30 == 0) _entities = ctx.Ecs.All().Length;
        World = $"{_entities} entities";

        Selected = EditorSelection.Current is { IsNone: false } entity
            ? $"selected {ctx.Ecs.NameOf(entity) ?? entity.Index.ToString()}"
            : "nothing selected";

        // What the last change was, so that Ctrl+Z has something visible to act on rather than
        // being a key that either works or does nothing and says neither.
        Last = EditorHistory.Last is { } what ? $"last {what}" : string.Empty;
    }
}

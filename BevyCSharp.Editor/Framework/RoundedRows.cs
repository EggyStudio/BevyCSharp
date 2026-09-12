using System.Numerics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Rounded highlights behind rows ImGui draws square.
/// </summary>
/// <remarks>
/// <para>
/// A menu row, a tree node and a selectable are all drawn by ImGui with a rectangle behind them and
/// no rounding to give it, and there is no style value for one: a look built on rounded shapes has
/// square corners wherever a list highlights a row.
/// </para>
/// <para>
/// What fixes it without rewriting those widgets is the draw list's own channels. The row goes on
/// the upper channel and its fill on the lower one, so a rectangle drawn after the row still ends
/// up behind it, which is what lets the fill be measured from the row it belongs to.
/// </para>
/// </remarks>
public static class RoundedRows
{
    /// <summary>Starts a region whose highlights are drawn rounded.</summary>
    /// <returns>The list to hand back to <see cref="End"/> and <see cref="Behind"/>.</returns>
    public static ImDrawListPtr Begin()
    {
        var draw = ImGui.GetWindowDrawList();

        draw.ChannelsSplit(2);
        draw.ChannelsSetCurrent(1);

        return draw;
    }

    /// <summary>Ends it, putting the fills under the rows.</summary>
    /// <param name="draw">What <see cref="Begin"/> returned.</param>
    public static void End(ImDrawListPtr draw) => draw.ChannelsMerge();

    /// <summary>Fills a rounded rectangle behind the row just drawn.</summary>
    /// <param name="draw">What <see cref="Begin"/> returned.</param>
    /// <param name="color">What to fill it with.</param>
    public static void Behind(ImDrawListPtr draw, Vector4 color)
    {
        var from = ImGui.GetItemRectMin();
        var to = ImGui.GetItemRectMax();

        // Half the row's height, so a row is a capsule at any rounding larger than that. The frame
        // rounding this look asks for is larger than anything is tall, on purpose.
        var rounding = MathF.Min(ImGui.GetStyle().FrameRounding, (to.Y - from.Y) * 0.5f);

        draw.ChannelsSetCurrent(0);
        draw.AddRectFilled(from, to, ImGui.GetColorU32(color), rounding);
        draw.ChannelsSetCurrent(1);
    }

    /// <summary>
    /// What a row's fill should be, or nothing when it wants none.
    /// </summary>
    /// <param name="chosen">Whether the row is the one selected.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    public static Vector4? Fill(bool chosen, bool over) => chosen
        ? EditorTheme.LiveAccent
        : over ? EditorTheme.LiveHover : null;
}

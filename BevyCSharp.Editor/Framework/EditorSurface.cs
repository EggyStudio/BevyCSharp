using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What every surface in the editor is drawn with, and how much air it keeps.
/// </summary>
/// <remarks>
/// The numbers and the few drawing primitives that the panels, the strip and the toolbars all
/// share. Kept together so that a card in one of them is the same shape, and the same distance
/// from its edge, as a card in another.
/// </remarks>
public static class EditorSurface
{
    /// <summary>
    /// What every window the editor places has in common.
    /// </summary>
    /// <remarks>
    /// The editor works out where each of its windows goes and how large it is, so ImGui is asked
    /// not to do any of that: no title bar to drag by, no corner to resize from, no remembered
    /// position, and no scrollbar, because what scrolls is a region inside rather than the window
    /// itself.
    /// </remarks>
    internal const ImGuiWindowFlags Placed =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoMove
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoSavedSettings
        | ImGuiWindowFlags.NoScrollbar;

    /// <summary>
    /// A window that holds something, on top of <see cref="Placed"/>.
    /// </summary>
    /// <remarks>
    /// The wheel never moves one of these. Without that, rolling over a list that has reached its
    /// end hands the wheel to whatever holds it and the whole panel slides up under its own edge:
    /// the world shrinks away, the split appears to climb, and nothing put it there but a scroll
    /// that had nowhere else to go.
    /// </remarks>
    internal const ImGuiWindowFlags Panel = Placed | ImGuiWindowFlags.NoScrollWithMouse;

    /// <summary>
    /// A window that is only the thing drawn in it, on top of <see cref="Placed"/>.
    /// </summary>
    /// <remarks>
    /// No plate behind it and no room round it. A group of buttons floating over the scene is a
    /// group of buttons, and a panel under them is a second thing to look at that says nothing.
    /// Taking focus is not wanted either, since pressing one is not a reason to stop typing.
    /// </remarks>
    internal const ImGuiWindowFlags Bare =
        Placed
        | ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoBackground;

    /// <summary>The gap between a panel's edge and the cards inside it.</summary>
    internal const float Gutter = 6f;

    /// <summary>How much air a card keeps inside its own edge.</summary>
    internal const float Air = 6f;

    /// <summary>
    /// A scrolling region with no fill of its own.
    /// </summary>
    /// <remarks>
    /// The card a region is drawn in is the surface; a second rectangle filling it edge to edge is
    /// the box inside a box this look does without. Ended with <see cref="EndRegion"/> whether or
    /// not it opened, like any other child window.
    /// </remarks>
    /// <param name="id">What to call it.</param>
    /// <param name="size">How large, in the usual child window terms.</param>
    /// <param name="child">Child flags, if it wants any.</param>
    /// <param name="window">Window flags, if it wants any.</param>
    /// <returns>Whether it is open and worth drawing into.</returns>
    internal static bool Region(
        string id,
        Vector2 size,
        ImGuiChildFlags child = ImGuiChildFlags.None,
        ImGuiWindowFlags window = ImGuiWindowFlags.None)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        var open = ImGui.BeginChild(id, size, child, window);

        ImGui.PopStyleColor();

        return open;
    }

    /// <summary>
    /// Ends a region, whether or not it opened.
    /// </summary>
    /// <remarks>
    /// The pair to <see cref="Region"/>, so that a region is begun and ended by name rather than
    /// by remembering that a child window has to be ended even when it is closed.
    /// </remarks>
    internal static void EndRegion() => ImGui.EndChild();

    /// <summary>
    /// A fill cut to the region it is drawn in, so that what is clipped keeps its corners.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A region clips with a rectangle, so a rounded card scrolled under its edge is cut square
    /// while every other edge in the editor is round. Nothing can round the cut itself. What is
    /// behind a floating panel here is the scene rather than a colour, so there is nothing to
    /// paint back over the corner.
    /// </para>
    /// <para>
    /// What can be done is to stop the fill at the edge rather than let it run under it. The
    /// rectangle then ends where the region does, and the rounding it was drawn with is the
    /// rounding the cut has. Only the fill is moved, so the text and the widgets on it stay where
    /// they were and scroll out of sight as they did.
    /// </para>
    /// </remarks>
    /// <param name="from">The fill's top left corner.</param>
    /// <param name="to">Its bottom right.</param>
    internal static (Vector2 From, Vector2 To) Clipped(Vector2 from, Vector2 to)
    {
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();

        return (
            new Vector2(from.X, MathF.Max(from.Y, min.Y)),
            new Vector2(to.X, MathF.Min(to.Y, max.Y)));
    }

    /// <summary>
    /// Draws an icon into a square, if the picture has loaded.
    /// </summary>
    /// <remarks>
    /// The icons are shapes cut out of white, so what colour one comes out is the tint it is drawn
    /// with, which is the theme's to decide. A picture that has not arrived yet draws nothing
    /// rather than a placeholder, because the asset server answers on a later frame and the row
    /// redraws then.
    /// </remarks>
    /// <param name="draw">What to draw into.</param>
    /// <param name="path">The icon's path under the asset root, or nothing.</param>
    /// <param name="at">Where its top left corner goes.</param>
    /// <param name="size">How large a square to draw it in.</param>
    /// <param name="lit">Whether it belongs to something chosen or in force.</param>
    internal static void Icon(ImDrawListPtr draw, string? path, Vector2 at, float size, bool lit)
    {
        if (path is not { Length: > 0 }) return;

        var picture = ImGuiTextures.Load(path);
        if (picture == 0) return;

        draw.AddImage(
            (IntPtr)picture,
            at,
            at + new Vector2(size, size),
            Vector2.Zero,
            Vector2.One,
            ImGui.GetColorU32(EditorTheme.IconTint(lit)));
    }

    /// <summary>
    /// The eye that says whether a thing is drawn.
    /// </summary>
    /// <remarks>
    /// Drawn rather than written, because the interface is set in a monospace font with no eye in
    /// it and a font that lacks a glyph draws a box that says nothing. Two lids and a pupil is the
    /// shape every editor has put on this, so it is read without being explained, and the same
    /// shape with a stroke through it is the shape for something that has been put away.
    /// </remarks>
    /// <param name="draw">What to draw into.</param>
    /// <param name="middle">Where the eye is centred.</param>
    /// <param name="size">How wide it is, corner to corner.</param>
    /// <param name="open">Whether the thing it belongs to is being drawn.</param>
    /// <param name="color">What to draw it in.</param>
    internal static void Eye(
        ImDrawListPtr draw, Vector2 middle, float size, bool open, uint color)
    {
        var across = size * 0.5f;

        // A quarter as tall as it is wide, which is the shape of an eye and not of a circle. The
        // control points are twice that out, because a quadratic curve reaches half way to the
        // point it is bent towards.
        var tall = size * 0.26f;

        var left = middle - new Vector2(across, 0f);
        var right = middle + new Vector2(across, 0f);
        var line = MathF.Max(1f, size * 0.09f);

        draw.PathClear();
        draw.PathLineTo(left);
        draw.PathBezierQuadraticCurveTo(middle - new Vector2(0f, tall * 2f), right, 0);
        draw.PathBezierQuadraticCurveTo(middle + new Vector2(0f, tall * 2f), left, 0);
        draw.PathStroke(color, ImDrawFlags.Closed, line);

        // The pupil only while it is open. An eye with a pupil and a stroke through it reads as an
        // eye that is looking anyway, and what is meant is one that is shut.
        if (open)
        {
            draw.AddCircleFilled(middle, tall * 0.62f, color, 0);
            return;
        }

        draw.AddLine(
            middle + new Vector2(-across * 0.7f, tall * 1.6f),
            middle + new Vector2(across * 0.7f, -tall * 1.6f),
            color,
            line);
    }

    /// <summary>
    /// What a floating window's background is painted in.
    /// </summary>
    /// <remarks>
    /// The panel while it floats over the scene, and the ground while it is docked, where there is
    /// no scene behind it to show through and a grey a shade off black is a frame nobody asked for.
    /// </remarks>
    internal static Vector4 Chrome() => EditorTheme.Alpha(
        EditorShell.Docked ? EditorTheme.Current.Ground : EditorTheme.Current.Panel,
        EditorShell.Docked ? 1f : EditorTheme.Current.WindowAlpha);

    /// <summary>
    /// The short pill that says a thing can be taken hold of and dragged.
    /// </summary>
    /// <remarks>
    /// Out of the way until it is wanted, when the panel is floating. A handle laid over a lit
    /// scene is one more mark on a picture somebody is trying to look at, and the edge it sits on
    /// already says where to reach; docked there is no picture under it and a handle nobody can
    /// see is a handle nobody finds.
    /// </remarks>
    /// <param name="grab">Half the pill's width and height.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    /// <param name="held">Whether it is being dragged.</param>
    /// <param name="onto">Which list to draw into, or the current window's.</param>
    /// <param name="middleX">Where to centre it across, or the middle of the item it belongs to.</param>
    internal static void Grab(Vector2 grab, bool over, bool held, ImDrawListPtr? onto = null, float? middleX = null)
    {
        var showing = held || over || EditorShell.Docked;
        if (!showing) return;

        var at = ImGui.GetItemRectMin();
        var to = ImGui.GetItemRectMax();

        var middle = new Vector2(middleX ?? ((at.X + to.X) * 0.5f), (at.Y + to.Y) * 0.5f);
        var draw = onto ?? ImGui.GetWindowDrawList();

        draw.AddRectFilled(
            middle - grab,
            middle + grab,
            ImGui.GetColorU32(held
                ? EditorTheme.LiveAccent
                : EditorTheme.Alpha(EditorTheme.LiveText, over ? 0.5f : 0.22f)),
            MathF.Min(grab.X, grab.Y));
    }

    /// <summary>How large a grab handle's pill is, as half its width and half its height.</summary>
    /// <remarks>
    /// One size for all of them. Two handles that do the same job at two thicknesses read as two
    /// different things, and there is no reason for either number to be the one it is beyond the
    /// other one matching it.
    /// </remarks>
    internal static readonly Vector2 Pill = new(26f, 2f);

    /// <summary>
    /// One card inside the panel, which is a fill a shade above it, rounded, with no line anywhere.
    /// </summary>
    /// <remarks>
    /// What separates two of these is the gap between them and the step in their fill. A border
    /// around each would be a third thing saying what those two already say, and three of them
    /// nested is the look this replaced.
    /// </remarks>
    internal static void Card(string id, Vector2 size, Action draw)
    {
        var stock = EditorTheme.Current.Stock;

        // Less air inside a card than a floating window keeps, because a card is already inside
        // one. The window's padding is the room round a thing standing on its own; repeating it at
        // every step inwards is how a panel ends up mostly margin.
        if (!stock) ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(Air, Air));

        // The card holds a scrolling region rather than being one, so the wheel belongs to what is
        // inside it and stops there.
        var open = ImGui.BeginChild(
            id,
            size,
            stock ? ImGuiChildFlags.None : ImGuiChildFlags.AlwaysUseWindowPadding,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        if (!stock) ImGui.PopStyleVar();

        if (open) draw();

        ImGui.EndChild();
    }

}

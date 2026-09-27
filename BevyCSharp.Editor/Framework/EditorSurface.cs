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
    /// end hands the wheel to whatever holds it and the whole panel slides up under its own edge.
    /// The world shrinks away, the split appears to climb, and nothing put it there but a scroll
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

    /// <summary>
    /// The gap between two surfaces, and between a surface and the edge it is inset from.
    /// </summary>
    /// <remarks>
    /// One number for every gap inside the editor: a panel's edge to its cards, one card to the
    /// next, the scene to whatever is beside it. What tells two surfaces apart is the step in
    /// their fill, and the gap only has to be wide enough to be read as deliberate. Two gaps of
    /// different widths in one picture read as an arrangement that has slipped.
    /// <para>
    /// Narrow, the way Blender keeps its areas apart, since every pixel of gap is taken from the
    /// scene and the panels on both sides of it, and a step in fill says where one surface ends
    /// as clearly at a few pixels as at a dozen.
    /// </para>
    /// </remarks>
    internal const float Gutter = 6f;

    /// <summary>How much air a card keeps inside its own edge.</summary>
    internal const float Air = 4f;

    /// <summary>
    /// Gives the next widget the whole row, less whatever the window's buttons lie over.
    /// </summary>
    /// <remarks>
    /// The buttons lie over the top of the panel's column, and a field under them is a field of the
    /// same gray with buttons of that gray on it. Which field that is depends on the split, the
    /// world's search box when the world is above the details and the entity's name when the two
    /// are side by side, so it is asked of the row rather than written into either panel. The
    /// buttons' place is the one they were drawn at last frame, since they are drawn after the
    /// panels, and they do not move between frames.
    /// </remarks>
    internal static void FullWidth()
    {
        var at = ImGui.GetCursorScreenPos();
        var right = at.X + ImGui.GetContentRegionAvail().X;
        var (min, max) = EditorSceneFrame.Buttons;

        var under = max.X > min.X
            && at.Y < max.Y
            && at.Y + ImGui.GetFrameHeight() > min.Y
            && right > min.X;

        ImGui.SetNextItemWidth(under ? -(right - min.X + Air) : -1f);
    }

    /// <summary>How much air a menu or a tooltip keeps inside its own edge.</summary>
    /// <remarks>
    /// The same for every one of them, pushed rather than taken from the style, because a popup
    /// opened while something else has the window padding pushed would otherwise wear that
    /// instead. What a menu looks like should not depend on what was on screen when it opened.
    /// </remarks>
    internal static readonly Vector2 Around = new(8f, 6f);

    /// <summary>
    /// How tall a button that floats over the scene is, and how tall a tab is.
    /// </summary>
    /// <remarks>
    /// A row of tabs along the bottom and a group of buttons in a corner are the same kind of
    /// thing, which is something pressed that lies on the scene rather than inside a panel, so
    /// they take one number between them. Larger than a field, because most of these hold a
    /// picture rather than a word and a picture drawn at a row's height is a picture nobody can
    /// read; small enough that a row of them is still a row of buttons rather than a bar.
    /// </remarks>
    internal const float Tall = 28f;

    /// <summary>
    /// How much air one of those keeps at each end when it has words in it rather than a picture.
    /// </summary>
    /// <remarks>
    /// More than a field keeps, because these have round ends and a word set against the inside of
    /// a curve looks closer to it than the same word against a straight edge.
    /// </remarks>
    internal const float Sides = 10f;

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
    /// <param name="child">Child flags, if it needs any.</param>
    /// <param name="window">Window flags, if it needs any.</param>
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
    /// <para>
    /// The pair to <see cref="Region"/>, so that a region is begun and ended by name rather than
    /// by remembering that a child window has to be ended even when it is closed.
    /// </para>
    /// <para>
    /// Where what it holds runs past an edge, that edge's corners are rounded off by laying the
    /// panel's color over them (<see cref="EditorDraw.RoundOff"/>). A region clips with a
    /// rectangle, so what scrolls under its edge is cut straight across, and the one square corner
    /// in a look made of round ones is the one at the end of every long list. A group keeps its own
    /// rounded corners where it is cut (see <see cref="Clipped"/>), so the region reads as a
    /// rounded card that what it holds slides under. Only where something runs past, since a region with its corners taken off and
    /// nothing under them would take a nick out of a first row sitting in its corner.
    /// </para>
    /// </remarks>
    internal static void EndRegion()
    {
        var scroll = ImGui.GetScrollY();
        var above = scroll > 0.5f;
        var below = scroll < ImGui.GetScrollMaxY() - 0.5f;

        if (above || below)
        {
            var min = ImGui.GetWindowPos();
            var max = min + ImGui.GetWindowSize();
            var draw = ImGui.GetWindowDrawList();
            var solid = ImGui.GetColorU32(EditorTheme.LivePanel);

            EditorDraw.RoundOff(
                draw,
                min,
                max,
                EditorTheme.Current.ChildRounding,
                solid,
                top: above,
                bottom: below);
        }

        ImGui.EndChild();
    }

    /// <summary>
    /// A fill cut to the region it is drawn in, so that what is clipped keeps its corners.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A region clips with a rectangle, so a rounded card scrolled under its edge is cut square
    /// while every other edge in the editor is round. Nothing can round the cut itself. What is
    /// behind a floating panel here is the scene rather than a color, so there is nothing to
    /// paint back over the corner.
    /// </para>
    /// <para>
    /// What can be done is to stop the fill at the edge rather than let it run under it. The
    /// rectangle then ends where the region does, and the rounding it was drawn with is the
    /// rounding the cut has. Only the fill is moved, so the text and the widgets on it stay where
    /// they were and scroll out of sight as they did.
    /// </para>
    /// </remarks>
    /// <param name="from">The fill's top left corner, moved down to the region's top edge.</param>
    /// <param name="to">Its bottom right, moved up to the region's bottom edge.</param>
    /// <returns>Whether any of it is in sight and worth drawing.</returns>
    internal static bool Clipped(ref Vector2 from, ref Vector2 to)
    {
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();

        from.Y = MathF.Max(from.Y, min.Y);
        to.Y = MathF.Min(to.Y, max.Y);

        // Nothing at all when the whole of it is past one edge or the other. A rectangle cut to
        // less than nothing is one ImGui fills as though its corners were the other way up, and
        // one cut to a hair is a line drawn along the edge it was cut against.
        return to.Y - from.Y >= 1f;
    }

    /// <summary>
    /// What something pressed that lies on the scene wears when it is neither held nor in force.
    /// </summary>
    /// <remarks>
    /// The field gray, which every button in a panel wears, so the toolbars, the tabs and the
    /// window's buttons are the same kind of thing as Add Component and a box to type in, rather
    /// than a kind of their own at a shade of their own.
    /// </remarks>
    internal static Vector4 Lying() => EditorTheme.LiveField;

    /// <summary>
    /// What a window the panels are laid out in is painted in, which is nothing.
    /// </summary>
    /// <remarks>
    /// Floating, what is between the panels is the scene. Docked, it is the ground, which the
    /// renderer clears the window to round the scene rather than this painting it, since anything
    /// painted here lies over the scene's rounded corners as well as round them.
    /// </remarks>
    internal static Vector4 Chrome() => Vector4.Zero;

    /// <summary>
    /// The short pill that says a thing can be taken hold of and dragged.
    /// </summary>
    /// <remarks>
    /// Out of the way until it is wanted, docked or floating. A handle drawn all the time is one
    /// more mark on the screen that says nothing until somebody reaches for it, and the gap it
    /// sits in already says where to reach. The pointer finds it, and it shows then.
    /// </remarks>
    /// <param name="grab">Half the pill's width and height.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    /// <param name="held">Whether it is being dragged.</param>
    /// <param name="onto">Which list to draw into, or the current window's.</param>
    /// <param name="middleX">Where to center it across, or the middle of the item it belongs to.</param>
    internal static void Grab(Vector2 grab, bool over, bool held, ImDrawListPtr? onto = null, float? middleX = null)
    {
        if (!held && !over) return;

        var at = ImGui.GetItemRectMin();
        var to = ImGui.GetItemRectMax();

        var middle = new Vector2(middleX ?? ((at.X + to.X) * 0.5f), (at.Y + to.Y) * 0.5f);
        var draw = onto ?? ImGui.GetWindowDrawList();

        // The colors a scrollbar's grab wears, because that is the other thing in the editor that
        // is taken hold of and slid, and two things that are dragged should not look like two
        // different kinds of thing.
        var color = held
            ? ImGuiCol.ScrollbarGrabActive
            : over ? ImGuiCol.ScrollbarGrabHovered : ImGuiCol.ScrollbarGrab;

        EditorDraw.Capsule(middle - grab, middle + grab, ImGui.GetColorU32(color), draw);
    }

    /// <summary>How large a grab handle's pill is, as half its width and half its height.</summary>
    /// <remarks>
    /// One size for all of them, and the thickness of the grab on a scrollbar, which is the other
    /// thing in the editor that is taken hold of and slid. Two handles that do the same job at two
    /// thicknesses read as two different things.
    /// </remarks>
    internal static readonly Vector2 Pill = new(22f, 2f);

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

    /// <summary>
    /// A pane inside a tab: a surface a step lighter than the tab, rounded, with air of its own,
    /// that scrolls what it holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a tab lays its parts out on, the way a component's rows lie on a group in the details.
    /// Text set straight on the tab runs from one edge of it to the other and reads as a log, and a
    /// tab with a little text in one corner reads as one that has not finished loading. A pane
    /// says where each part begins and ends, and gives what is in it room from the edge.
    /// </para>
    /// <para>
    /// No heading, since what is in a pane says what it is, as the panels do.
    /// </para>
    /// </remarks>
    /// <param name="id">What to call it.</param>
    /// <param name="size">How large, in the usual child window terms.</param>
    /// <param name="draw">What it holds.</param>
    internal static void Pane(string id, Vector2 size, Action draw)
    {
        ArgumentNullException.ThrowIfNull(draw);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, EditorTheme.LiveGroup);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, EditorTheme.Current.ChildRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));

        var open = ImGui.BeginChild(id, size, ImGuiChildFlags.AlwaysUseWindowPadding);

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();

        if (open) draw();

        ImGui.EndChild();
    }

    /// <summary>
    /// What an empty pane says, in its middle: what is not there, and under it, dimmer, how it
    /// comes to be.
    /// </summary>
    /// <remarks>
    /// In the middle, where the eye goes to an empty space, rather than in a corner where it reads
    /// as a line of a log. The second line is the useful one, since an empty pane is usually one
    /// somebody has not yet learned how to fill.
    /// </remarks>
    /// <param name="what">What is not there.</param>
    /// <param name="how">How it comes to be there, or nothing.</param>
    internal static void Empty(string what, string? how = null)
    {
        ArgumentNullException.ThrowIfNull(what);

        var room = ImGui.GetContentRegionAvail();
        var line = ImGui.GetTextLineHeightWithSpacing();
        var tall = how is null ? line : line * 2f;
        var start = ImGui.GetCursorPos();

        void Centered(string text, bool dim, float y)
        {
            var width = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPos(new Vector2(start.X + MathF.Max(0f, (room.X - width) * 0.5f), y));

            if (dim) ImGui.TextDisabled(text);
            else ImGui.TextUnformatted(text);
        }

        var top = start.Y + MathF.Max(0f, (room.Y - tall) * 0.5f);

        Centered(what, false, top);

        if (how is not null) Centered(how, true, top + line);
    }

    /// <summary>
    /// A heading over the rows that belong to it, with a rule carried out to the end of the row.
    /// </summary>
    /// <remarks>
    /// ImGui's own runs its rule to the edge of the whole region, which is further right than the
    /// fields reach and close enough to the card's edge to read as a line that ran out of room.
    /// This ends where the fields end, so the row is inset by the same amount at both ends.
    /// </remarks>
    /// <param name="text">What the heading says.</param>
    /// <param name="inset">How far short of the region's right edge the rule stops.</param>
    internal static void Heading(string text, float inset = 0f)
    {
        if (EditorTheme.Current.Stock)
        {
            ImGui.SeparatorText(text);
            return;
        }

        var style = ImGui.GetStyle();
        var thick = MathF.Max(1f, style.SeparatorTextBorderSize);

        var at = ImGui.GetCursorScreenPos();
        var word = ImGui.CalcTextSize(text);

        var width = MathF.Max(
            word.X + (style.SeparatorTextPadding.X * 2f),
            ImGui.GetContentRegionAvail().X - inset);

        var height = MathF.Max(word.Y + (style.SeparatorTextPadding.Y * 2f), thick);

        ImGui.Dummy(new Vector2(width, height));

        var draw = ImGui.GetWindowDrawList();
        var rule = MathF.Floor(at.Y + (height * 0.5f));
        var color = ImGui.GetColorU32(ImGuiCol.Separator);

        var from = at.X + style.SeparatorTextPadding.X;
        var to = from + word.X + style.ItemSpacing.X;

        if (from - style.ItemSpacing.X > at.X)
        {
            draw.AddLine(
                new Vector2(at.X, rule),
                new Vector2(from - style.ItemSpacing.X, rule),
                color,
                thick);
        }

        if (to < at.X + width)
        {
            draw.AddLine(new Vector2(to, rule), new Vector2(at.X + width, rule), color, thick);
        }

        draw.AddText(
            new Vector2(from, at.Y + style.SeparatorTextPadding.Y),
            ImGui.GetColorU32(ImGuiCol.Text),
            text);
    }
}

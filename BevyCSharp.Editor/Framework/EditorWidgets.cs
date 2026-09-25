using System.Numerics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The controls the editor draws that ImGui does not, or does not draw the way this look wants.
/// </summary>
/// <remarks>
/// <para>
/// A box to tick, a slider, a list to choose from, a flyout and a tooltip. Each of them is
/// ImGui's underneath, because what a widget does is worth more than what it looks like and
/// ImGui already answers every click, key and drag correctly. What is here is the difference
/// between how ImGui draws one and how the rest of this editor is drawn.
/// </para>
/// <para>
/// Apart from <see cref="EditorSurface"/>, which is about the room a panel keeps and the plates
/// things are laid on rather than about the things themselves.
/// </para>
/// </remarks>
public static class EditorWidgets
{
    /// <summary>
    /// Opens a flyout, with the air every flyout keeps.
    /// </summary>
    /// <remarks>
    /// Ended with <see cref="EndFlyout"/>, and only when it opened, which is the shape ImGui's own
    /// popups take.
    /// </remarks>
    /// <param name="id">What the popup is called.</param>
    internal static bool Flyout(string id)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);

        var open = ImGui.BeginPopup(id);

        if (!open) ImGui.PopStyleVar();

        return open;
    }

    /// <summary>Opens the flyout a right click asks for, with the same air.</summary>
    /// <param name="id">What the popup is called.</param>
    internal static bool FlyoutHere(string id)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);

        var open = ImGui.BeginPopupContextItem(id);

        if (!open) ImGui.PopStyleVar();

        return open;
    }

    /// <summary>Closes what <see cref="Flyout"/> or <see cref="FlyoutHere"/> opened.</summary>
    internal static void EndFlyout()
    {
        ImGui.EndPopup();
        ImGui.PopStyleVar();
    }

    /// <summary>
    /// What to say about the thing under the pointer.
    /// </summary>
    /// <remarks>
    /// The air is pushed here rather than left to the style, because a tooltip asked for inside a
    /// panel or a toolbar inherits whatever window padding that has pushed, and one of those is
    /// nothing at all, which draws the words against the edge of their own box.
    /// </remarks>
    /// <param name="text">What to say.</param>
    internal static void Tip(string text)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, EditorTheme.Current.ChildRounding);

        ImGui.SetTooltip(text);

        ImGui.PopStyleVar(2);
    }

    /// <summary>
    /// A box to tick, drawn as a circle with the tick in the text color.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ImGui's own box is a rounded square however round the style asks for, because it clamps the
    /// rounding to a quarter of the box so that a checkbox still reads as a checkbox. Here the
    /// round shape is the one every other control has, so the fill is drawn as a disc first and
    /// ImGui draws the tick over it with a fill of its own that is not there.
    /// </para>
    /// <para>
    /// The tick itself is ImGui's, drawn in the color the theme gives it, which is the one every
    /// other value is read in rather than the accent.
    /// </para>
    /// </remarks>
    /// <param name="label">What to call it, which is what ImGui hashes it by.</param>
    /// <param name="on">What it holds, and what it is left holding.</param>
    internal static bool Ticked(string label, ref bool on)
    {
        // The stock look is ImGui's decisions taken whole, and one of them is that a checkbox is a
        // box.
        if (EditorTheme.Current.Stock) return ImGui.Checkbox(label, ref on);

        var at = ImGui.GetCursorScreenPos();
        var size = ImGui.GetFrameHeight();
        var box = new Vector2(size, size);

        // Asked of the rectangle the box is about to take, because what it looks like has to be
        // drawn before ImGui is called and ImGui has not been called yet.
        var over = ImGui.IsMouseHoveringRect(at, at + box);
        var held = over && ImGui.IsMouseDown(ImGuiMouseButton.Left);

        var fill = held
            ? ImGuiCol.FrameBgActive
            : over ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg;

        ImGui.GetWindowDrawList().AddCircleFilled(
            at + (box * 0.5f), size * 0.5f, ImGui.GetColorU32(fill), 0);

        ImGui.PushStyleColor(ImGuiCol.FrameBg, 0u);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0u);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0u);

        var changed = ImGui.Checkbox(label, ref on);

        ImGui.PopStyleColor(3);

        return changed;
    }

    /// <summary>How wide a pill saying this is, which is what a row of them measures with.</summary>
    /// <param name="name">What the pill says.</param>
    internal static float PillWidth(string name) =>
        ImGui.CalcTextSize(name).X + (EditorSurface.Sides * 2f);

    /// <summary>
    /// One of a row of pills, where whichever is in force wears the accent.
    /// </summary>
    /// <remarks>
    /// The tabs along the bottom, the pages of the settings, the themes in the style tab and the
    /// levels a console shows are the same thing four times over: a row of words, one or more of
    /// which is in force. Drawn here rather than as a button per caller, so they are one size, one
    /// shape and one set of colors, and so the shape is a capsule, which ImGui's own button cannot
    /// quite be however far its rounding is pushed.
    /// </remarks>
    /// <param name="name">What it says, which is also what it is hashed by.</param>
    /// <param name="chosen">Whether this is the one in force.</param>
    /// <param name="idle">
    /// What it wears while it is neither chosen nor under the hand, or nothing for the plate the
    /// rest of them wear. A pill on a surface dark enough to read a bare word against passes one
    /// that is seen through.
    /// </param>
    /// <param name="height">
    /// How tall, or nothing for as tall as a field. A row of pills inside a panel stands beside
    /// fields and matches those; one lying on the scene stands beside the toolbar and is given
    /// <see cref="EditorSurface.Tall"/> to match that instead.
    /// </param>
    /// <returns>Whether it was pressed.</returns>
    internal static bool Pill(string name, bool chosen, Vector4? idle = null, float? height = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (EditorTheme.Current.Stock)
        {
            if (chosen) ImGui.PushStyleColor(ImGuiCol.Button, EditorTheme.LiveAccent);

            var stock = ImGui.Button(name);

            if (chosen) ImGui.PopStyleColor();

            return stock;
        }

        var draw = ImGui.GetWindowDrawList();
        var word = ImGui.CalcTextSize(name);

        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(PillWidth(name), height ?? ImGui.GetFrameHeight());

        // Its own answer rather than a click on it, so a pill acts when the button is let go the
        // way every other button does, and a press that slides off it acts on nothing.
        var pressed = ImGui.InvisibleButton($"##pill{name}", size);
        var over = ImGui.IsItemHovered();

        var fill = chosen
            ? over ? EditorTheme.Alpha(EditorTheme.LiveAccent, 0.85f) : EditorTheme.LiveAccent
            : over ? EditorTheme.LiveHover : idle ?? EditorSurface.Lying();

        if (fill.W > 0f) EditorDraw.Capsule(at, at + size, ImGui.GetColorU32(fill), draw);

        // White whether it is chosen or not. What says which one is in force is the pill under it,
        // and a gray word reads as one that cannot be pressed.
        draw.AddText(
            at + new Vector2(EditorSurface.Sides, (size.Y - word.Y) * 0.5f),
            ImGui.GetColorU32(EditorTheme.LiveText),
            name);

        return pressed;
    }

    /// <summary>
    /// A row holding one thing, which opens a list of what it could hold instead.
    /// </summary>
    /// <remarks>
    /// ImGui's own combo for the arrow and the keyboard it brings, with this editor's rows inside
    /// it, so the name under the pointer is a rounded row like the one in every other list and the
    /// list keeps the air every other flyout keeps.
    /// </remarks>
    /// <param name="id">What to call it, which is what ImGui hashes it by.</param>
    /// <param name="held">What it holds now, which is what the row says while the list is shut.</param>
    /// <param name="list">
    /// The rows to offer, drawn only while the list is open, each followed by
    /// <see cref="RoundedRows.Row"/>.
    /// </param>
    internal static void Picking(string id, string held, Action list)
    {
        ArgumentNullException.ThrowIfNull(list);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);

        if (ImGui.BeginCombo(id, held))
        {
            RoundedRows.Rows(list);
            ImGui.EndCombo();
        }

        ImGui.PopStyleVar();
    }

    /// <summary>
    /// One of a list of names, chosen from a flyout.
    /// </summary>
    /// <remarks>
    /// The list is the names themselves, which is every case where what can be chosen is known
    /// outright. Anything whose list has to be gathered, such as the files in a project, builds its
    /// own rows through <see cref="Picking"/> instead.
    /// </remarks>
    /// <param name="id">What to call it, which is what ImGui hashes it by.</param>
    /// <param name="held">What it holds now.</param>
    /// <param name="options">What it could hold instead.</param>
    /// <param name="onto">What to do with the one chosen.</param>
    internal static void Choice(
        string id, string held, IReadOnlyList<string> options, Action<string> onto)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onto);

        if (options.Count == 0) return;

        Picking(id, held, () =>
        {
            foreach (var option in options)
            {
                var picked = option == held;

                if (ImGui.Selectable(option, picked)) onto(option);

                RoundedRows.Row(picked);
            }
        });
    }

    /// <summary>What a color swatch offers, which is the color and a way through to a picker.</summary>
    private const ImGuiColorEditFlags Coloring =
        ImGuiColorEditFlags.NoInputs
        | ImGuiColorEditFlags.AlphaPreviewHalf
        | ImGuiColorEditFlags.AlphaBar;

    /// <summary>
    /// A color, as a swatch the width of its row with a picker behind it.
    /// </summary>
    /// <remarks>
    /// A button rather than ImGui's own color field, which draws its swatch as a square of one
    /// row's height however wide the row is and leaves the rest of it empty.
    /// </remarks>
    /// <param name="id">What to call it, which is what ImGui hashes it by.</param>
    /// <param name="color">The color, changed in place when the picker is used.</param>
    /// <returns>Whether it changed.</returns>
    public static bool Swatch(string id, ref Vector4 color)
    {
        var across = new Vector2(ImGui.GetContentRegionAvail().X, 0f);

        if (ImGui.ColorButton(id, color, Coloring, across)) ImGui.OpenPopup($"##pick{id}");

        if (!Flyout($"##pick{id}")) return false;

        var changed = ImGui.ColorPicker4($"##picker{id}", ref color, Coloring);

        EndFlyout();

        return changed;
    }

    /// <summary>Which slider is being dragged, while it is.</summary>
    private static string _sliding = string.Empty;

    /// <summary>
    /// A slider drawn as a groove with a round handle on it.
    /// </summary>
    /// <remarks>
    /// ImGui's own, with its fill and its handle turned off and both drawn here first instead. A
    /// rounded rectangle is only ever as round as half its shortest side less a pixel, which
    /// leaves a flat edge on anything meant to be a circle, and a groove and its handle are where
    /// that shows. Everything the slider does is still ImGui's, including the control click that
    /// opens it for typing.
    /// </remarks>
    /// <param name="id">Which field this is, so a drag can be followed between frames.</param>
    /// <param name="fraction">How far along the value sits, from nothing to all of it.</param>
    /// <param name="slide">The slider to call, once its own colors are out of the way.</param>
    /// <param name="readout">
    /// What to write on the bar, clear of the handle, or nothing at all for a bar that says
    /// nothing. Left out entirely to keep whatever ImGui writes on it, which is what a bar with a
    /// word rather than a number in it wants.
    /// </param>
    internal static bool Sliding(string id, float fraction, Func<bool> slide, string? readout = null)
    {
        ArgumentNullException.ThrowIfNull(slide);

        // Drawn by ImGui under the stock look, groove and handle and all, because that look is
        // ImGui's to decide.
        if (EditorTheme.Current.Stock) return slide();

        var draw = ImGui.GetWindowDrawList();

        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight());

        // Held is what ImGui said last frame, because the answer for this one arrives after the
        // call that draws it. A drag reads as held from its second frame, which is the frame the
        // handle first moves.
        var held = _sliding == id;
        var groove = held || ImGui.IsMouseHoveringRect(min, max)
            ? ImGuiCol.FrameBgHovered
            : ImGuiCol.FrameBg;

        EditorDraw.Capsule(min, max, ImGui.GetColorU32(groove), draw);

        // Where ImGui would have put its own handle. It keeps two pixels of the groove clear at
        // each end and slides the handle along what is left, so the same two numbers put a disc
        // exactly where the rectangle would have been.
        const float Clear = 2f;

        var handle = MathF.Max(1f, ImGui.GetStyle().GrabMinSize);
        var travel = MathF.Max(0f, max.X - min.X - (Clear * 2f) - handle);
        var along = min.X + Clear + (handle * 0.5f) + (travel * Math.Clamp(fraction, 0f, 1f));

        draw.AddCircleFilled(
            new Vector2(along, (min.Y + max.Y) * 0.5f),
            handle * 0.5f,
            ImGui.GetColorU32(held ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab),
            0);

        ImGui.PushStyleColor(ImGuiCol.FrameBg, 0u);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, 0u);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, 0u);
        ImGui.PushStyleColor(ImGuiCol.SliderGrab, 0u);
        ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, 0u);

        // ImGui writes the value in the middle of the bar, where the handle passes through it.
        // Hidden rather than left out, because the format it is left out of is also the one the
        // box is filled with when somebody types into it, and an empty one offers an empty box.
        var mine = readout is not null && !FieldNumbers.Typing(id);

        if (mine) ImGui.PushStyleColor(ImGuiCol.Text, 0u);

        var changed = slide();

        if (mine) ImGui.PopStyleColor();

        ImGui.PopStyleColor(5);

        if (mine && readout is { Length: > 0 }) Readout(draw, readout, min, max, along, handle);

        if (ImGui.IsItemActive()) _sliding = id;
        else if (held) _sliding = string.Empty;

        return changed;
    }

    /// <summary>
    /// The value a bar holds, written where the handle is not.
    /// </summary>
    /// <remarks>
    /// In the middle, until the handle comes near enough to touch it, and then pushed aside by as
    /// little as it takes. Moved rather than swapped from one side to the other, so the number
    /// drifts out of the way as the bar is dragged instead of jumping across it, and never outside
    /// the bar, because a number written past the end of what it belongs to belongs to nothing.
    /// </remarks>
    /// <param name="draw">What to draw into.</param>
    /// <param name="readout">What the bar holds, as it should be written.</param>
    /// <param name="min">The bar's top left.</param>
    /// <param name="max">The bar's bottom right.</param>
    /// <param name="along">Where the middle of the handle is.</param>
    /// <param name="handle">How wide the handle is.</param>
    private static void Readout(
        ImDrawListPtr draw, string readout, Vector2 min, Vector2 max, float along, float handle)
    {
        var style = ImGui.GetStyle();
        var word = ImGui.CalcTextSize(readout);

        var middle = (min.X + max.X) * 0.5f;
        var clear = (handle * 0.5f) + (word.X * 0.5f) + style.ItemInnerSpacing.X;

        var at = MathF.Abs(middle - along) >= clear
            ? middle
            : along < middle ? along + clear : along - clear;

        var edge = style.FramePadding.X + (word.X * 0.5f);
        var room = (max.X - min.X) * 0.5f;

        // A bar too narrow to hold the number at either end keeps it in the middle, which is at
        // least the same place every frame.
        at = edge <= room
            ? Math.Clamp(at, min.X + edge, max.X - edge)
            : middle;

        draw.AddText(
            new Vector2(at - (word.X * 0.5f), ((min.Y + max.Y) * 0.5f) - (word.Y * 0.5f)),
            ImGui.GetColorU32(ImGuiCol.Text),
            readout);
    }
}

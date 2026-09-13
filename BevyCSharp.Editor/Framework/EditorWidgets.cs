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
    /// A box to tick, drawn as a circle with the tick in the text colour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ImGui's own box is a rounded square however round the style asks for, because it clamps the
    /// rounding to a quarter of the box so that a checkbox still reads as a checkbox. Here the
    /// round shape is the one every other control has, so the fill is drawn as a disc first and
    /// ImGui draws the tick over it with a fill of its own that is not there.
    /// </para>
    /// <para>
    /// The tick itself is ImGui's, drawn in the colour the theme gives it, which is the one every
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

    /// <summary>
    /// One of a list of names, chosen from a flyout.
    /// </summary>
    /// <remarks>
    /// ImGui's own combo for the arrow and the keyboard it brings, with this editor's rows inside
    /// it, so the name under the pointer is a rounded row like the one in every other list and the
    /// list keeps the air every other flyout keeps.
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

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, EditorSurface.Around);

        if (ImGui.BeginCombo(id, held))
        {
            RoundedRows.Rows(() =>
            {
                foreach (var option in options)
                {
                    var picked = option == held;

                    if (ImGui.Selectable(option, picked)) onto(option);

                    RoundedRows.Row(picked);
                }
            });

            ImGui.EndCombo();
        }

        ImGui.PopStyleVar();
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
    /// <param name="slide">The slider to call, once its own colours are out of the way.</param>
    internal static bool Sliding(string id, float fraction, Func<bool> slide)
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

        var changed = slide();

        ImGui.PopStyleColor(5);

        if (ImGui.IsItemActive()) _sliding = id;
        else if (held) _sliding = string.Empty;

        return changed;
    }
}

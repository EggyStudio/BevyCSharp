using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// How a number in a field is written, and how one is dragged.
/// </summary>
/// <remarks>
/// <para>
/// Everything about a number that is not the widget it is drawn in. How many places it is written
/// to and when it is written whole, what a rotation shows while it is being turned, and the row of
/// three boxes a vector takes.
/// </para>
/// <para>
/// Apart from <see cref="ComponentFields"/> because a number that reads well is most of what an
/// inspector is, and because the rules here are worth finding without reading past the widget for
/// every other kind of value.
/// </para>
/// </remarks>
internal static class FieldNumbers
{
    /// <summary>One degree, in radians.</summary>
    internal const float Radians = MathF.PI / 180f;

    /// <summary>
    /// How many places a number nobody is typing into is written to, at most.
    /// </summary>
    /// <remarks>
    /// As much as a box this narrow shows without the last digits running under its edge. The
    /// value itself is never rounded to it, so what is shown is short and what is held is whole.
    /// </remarks>
    internal const int Places = 3;

    /// <summary>The formats for every number of places, so a frame builds no strings.</summary>
    private static readonly string[] Shortly = ["%.0f", "%.1f", "%.2f", "%.3f"];

    /// <summary>
    /// How a number that is open for typing is written.
    /// </summary>
    /// <remarks>
    /// Seven figures, because a single-precision number holds that many. ImGui fills the text box
    /// with the value put through the format the moment the box opens, so a field written at three
    /// places would offer 1.235 to somebody who came to correct 1.2345.
    /// </remarks>
    internal const string Figures = "%.7g";

    /// <summary>
    /// What keeps a drag from rounding the value to the format it is shown in.
    /// </summary>
    /// <remarks>
    /// ImGui rounds a dragged value to whatever its format can print unless it is told not to, so
    /// without this a field shown to three places is a field that cannot hold 1.2345 even after
    /// somebody has typed it in.
    /// </remarks>
    internal const ImGuiSliderFlags Whole = ImGuiSliderFlags.NoRoundToFormat;

    /// <summary>Which field is open for typing, while it is.</summary>
    private static string _opened = string.Empty;

    /// <summary>Whether a field is the one open for typing.</summary>
    /// <remarks>
    /// Asked by anything that draws over a number, since what is written while somebody is typing
    /// is theirs and not the editor's to replace.
    /// </remarks>
    /// <param name="id">Which field is asking.</param>
    internal static bool Typing(string id) => _opened == id;

    /// <summary>How the next field's number is written, which is who is about to read it.</summary>
    /// <param name="id">Which field is asking.</param>
    /// <param name="number">What the box holds, which decides how much of it there is to show.</param>
    internal static string Written(string id, float number)
    {
        if (_opened == id) return Figures;

        if (Opening())
        {
            _opened = id;
            return Figures;
        }

        return Shortly[Needed(number)];
    }

    /// <summary>
    /// How many places one number needs, up to <see cref="Places"/>.
    /// </summary>
    /// <remarks>
    /// The fewest that says what the value is. Three zeroes after every whole number is three
    /// characters of nothing in a column that is already tight, and a tenth written as 1.200 says
    /// that two digits were measured which were not.
    /// </remarks>
    internal static int Needed(float number)
    {
        for (var places = 0; places < Places; places++)
        {
            if (MathF.Abs(number - MathF.Round(number, places)) < 0.0005f) return places;
        }

        return Places;
    }

    /// <summary>
    /// Whether the gesture this frame is the one that opens the next field for typing.
    /// </summary>
    /// <remarks>
    /// ImGui turns a drag box into a text box on a double click or a control click, and fills the
    /// text with the value as the format writes it, both inside the one call that draws the
    /// widget. The format has to be right before that call rather than after it, so the gesture is
    /// read here instead of being asked about afterwards. The rectangle is the one the widget is
    /// about to take, which is where the cursor is and how wide the row said it may be.
    /// </remarks>
    private static bool Opening()
    {
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight());

        if (!ImGui.IsMouseHoveringRect(at, at + size)) return false;

        return ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)
            || (ImGui.GetIO().KeyCtrl && ImGui.IsMouseClicked(ImGuiMouseButton.Left));
    }

    /// <summary>Which rotation field is being turned, while it is being turned.</summary>
    /// <remarks>
    /// The angles are held here for as long as the box is held, because a rotation has more than
    /// one set of angles that describe it. Reading them back out of the quaternion on every frame
    /// of a drag means the numbers jump to a different decomposition halfway through, and a box
    /// whose value changes while it is being dragged cannot be dragged.
    /// </remarks>
    private static string _turning = string.Empty;

    /// <summary>The angles that field is being turned to, in degrees.</summary>
    private static Vector3 _turned;

    /// <summary>The angles to show for a rotation: the ones being typed, or the world's own.</summary>
    /// <param name="id">Which field is asking.</param>
    /// <param name="turn">What the world says it is rotated by.</param>
    internal static Vector3 Turning(string id, Quat turn)
    {
        if (_turning == id) return _turned;

        var euler = turn.ToEuler();

        return new Vector3(Tidy(euler.X), Tidy(euler.Y), Tidy(euler.Z));
    }

    /// <summary>Lets go of a box's full precision once ImGui says it is no longer in use.</summary>
    /// <param name="id">Which field, and which box of it.</param>
    internal static void Close(string id)
    {
        if (_opened == id && !ImGui.IsItemActive()) _opened = string.Empty;
    }

    /// <summary>Holds the angles a rotation is being turned to while the box is held.</summary>
    /// <param name="id">Which field is being turned.</param>
    /// <param name="degrees">What it is being turned to.</param>
    internal static void Turned(string id, Vector3 degrees)
    {
        _turning = id;
        _turned = degrees;
    }

    /// <summary>
    /// Lets go of those angles, so the rotation shows what the world says again.
    /// </summary>
    /// <param name="id">Which field was being turned.</param>
    /// <param name="holding">Whether any of its boxes is still held.</param>
    internal static void Settle(string id, bool holding)
    {
        if (_turning == id && !holding) _turning = string.Empty;
    }

    /// <summary>One angle in degrees, with the noise of the round trip taken off it.</summary>
    /// <remarks>
    /// A rotation that is exactly none comes back out of the quaternion as a few millionths of a
    /// degree, sometimes negative, which a box then shows as -0.000. That is not a rotation and
    /// reads as a fault.
    /// </remarks>
    private static float Tidy(float radians)
    {
        var degrees = radians / Radians;

        return MathF.Abs(degrees) < 0.0005f ? 0f : degrees;
    }

    /// <summary>
    /// A value as a person reads it, rather than as a round trip through binary writes it.
    /// </summary>
    /// <remarks>
    /// Four places at most, and none of them trailing zeroes. A number that came from a rotation
    /// or a division prints seventeen digits by default, and sixteen of them are the difference
    /// between what a float can hold and what was meant.
    /// </remarks>
    internal static string Say(object? value) => value switch
    {
        null => "-",
        float number => Digits(number),
        double number => Digits((float)number),
        Vec3 vector => $"{Digits(vector.X)}, {Digits(vector.Y)}, {Digits(vector.Z)}",
        Quat turn => Say(turn.ToEuler() * (1f / Radians)),
        _ => value.ToString() ?? "-",
    };

    /// <summary>One number, to four places at most.</summary>
    private static string Digits(float number) =>
        MathF.Round(number, 4).ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// Three number boxes on one row, each written to its own number of places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <c>DragFloat3</c> draws, except for the format. ImGui takes one format for the three
    /// boxes, and a vector whose Y is turning then writes X and Z as 0.000 while they are exactly
    /// nothing. Worse, the places change as Y passes each tenth, so the two boxes that are not
    /// changing are redrawn at a different width every frame and the numbers in them appear to
    /// shake. A number that is not moving has to look like it.
    /// </para>
    /// <para>
    /// The widths are ImGui's own arithmetic for a row of boxes, which is equal shares with the
    /// last one taking whatever the rounding left over.
    /// </para>
    /// </remarks>
    /// <param name="id">What the row is called, which each box takes a name under.</param>
    /// <param name="value">The three numbers, written back as they are dragged.</param>
    /// <param name="speed">How far a pixel of drag moves one of them.</param>
    /// <param name="active">Whether any of the three is being dragged or typed into.</param>
    /// <returns>Whether any of them changed.</returns>
    internal static bool Vector(string id, ref Vector3 value, float speed, out bool active)
    {
        var inner = ImGui.GetStyle().ItemInnerSpacing.X;
        var full = ImGui.CalcItemWidth();

        var one = MathF.Max(1f, MathF.Floor((full - (inner * 2f)) / 3f));
        var last = MathF.Max(1f, MathF.Floor(full - ((one + inner) * 2f)));

        var changed = false;

        active = false;

        ImGui.BeginGroup();
        ImGui.PushID(id);

        for (var axis = 0; axis < 3; axis++)
        {
            if (axis > 0) ImGui.SameLine(0f, inner);

            ImGui.PushID(axis);
            ImGui.SetNextItemWidth(axis == 2 ? last : one);

            var number = axis switch { 0 => value.X, 1 => value.Y, _ => value.Z };
            var key = $"{id}.{axis}";

            if (ImGui.DragFloat("##n", ref number, speed, 0f, 0f, Written(key, number), Whole))
            {
                if (axis == 0) value.X = number;
                else if (axis == 1) value.Y = number;
                else value.Z = number;

                changed = true;
            }

            if (ImGui.IsItemActive()) active = true;
            else if (_opened == key) _opened = string.Empty;

            ImGui.PopID();
        }

        ImGui.PopID();
        ImGui.EndGroup();

        return changed;
    }
}

using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The picker behind a color swatch: a spectrum to pick from, a hue and a clearness to slide, the
/// color as it was beside the color as it is, and the numbers, to read or to type.
/// </summary>
/// <remarks>
/// <para>
/// Laid out the way WinUI's color picker is, which is the arrangement most pickers people know
/// settle on. The large square is saturation across and brightness down at the hue the bar under
/// it sets, and the bar under that is how clear the color is. Below those, the color the swatch had
/// when it was opened beside the color it has now, so a change can be judged against where it
/// started, and a box that takes a hex code. Last, the numbers, as red, green and blue or as hue,
/// saturation and brightness, with a pill to switch between them.
/// </para>
/// <para>
/// Drawn here rather than by ImGui's own picker, which draws its square and its bars square
/// whatever the rounding says and is the one control in this look with corners. The gradients are
/// ImGui's own technique, two rectangles with a color at each corner, one running from white to
/// the hue across and one from clear to black down over it. Their corners are rounded by laying
/// the flyout's color over them in an antialiased arc (<see cref="EditorDraw.RoundOff"/>), since a
/// draw list cannot clip to a rounded shape.
/// </para>
/// <para>
/// The hue is kept here rather than worked out from the color every frame. A gray has no hue, so a
/// hue read back from one is whatever the conversion says, and dragging the square to a gray and
/// back would otherwise lose the hue somebody chose.
/// </para>
/// </remarks>
internal static class ColorPicker
{
    /// <summary>How wide the picker is, which the square and the bars take all of.</summary>
    private const float Width = 248f;

    /// <summary>How tall the square is.</summary>
    private const float Tall = 156f;

    /// <summary>How tall a bar is.</summary>
    private const float Bar = 14f;

    /// <summary>Which picker the kept state below belongs to.</summary>
    private static string _for = string.Empty;

    /// <summary>The color when this picker was opened, shown beside the color it has now.</summary>
    private static Vector4 _was;

    /// <summary>Hue, saturation and brightness as last chosen, from nothing to one.</summary>
    private static float _hue;
    private static float _saturation;
    private static float _value;

    /// <summary>Whether the numbers are hue, saturation and brightness rather than red, green and blue.</summary>
    private static bool _hsv;

    /// <summary>What the hex box holds while it is being typed into.</summary>
    private static string _hex = string.Empty;

    /// <summary>Draws the picker for a color, changing it in place.</summary>
    /// <param name="id">What the swatch is called, which the kept state belongs to.</param>
    /// <param name="color">The color, straight RGBA from nothing to one.</param>
    /// <param name="alpha">
    /// Whether the color has a clearness to set. Three numbers that are a color have none, and a
    /// bar that moved nothing would be a control that lies.
    /// </param>
    /// <returns>Whether it changed.</returns>
    internal static bool Draw(string id, ref Vector4 color, bool alpha = true)
    {
        if (_for != id || ImGui.IsWindowAppearing())
        {
            _for = id;
            _was = color;
            Take(color, keepHue: false);
        }
        else if (ToRgb(_hue, _saturation, _value) is var shown
                 && Vector3.Distance(shown, new Vector3(color.X, color.Y, color.Z)) > 1f / 512f)
        {
            // Changed from somewhere other than here, such as the undo of a moment ago.
            Take(color, keepHue: true);
        }

        var changed = false;
        var behind = ImGui.GetColorU32(ImGuiCol.PopupBg);

        changed |= Square(ref color, behind);

        ImGui.Dummy(new Vector2(0f, 2f));

        changed |= Hue(ref color, behind);

        if (alpha) changed |= Clearness(ref color, behind);

        ImGui.Dummy(new Vector2(0f, 2f));

        changed |= Preview(ref color);
        changed |= Numbers(ref color, alpha);

        return changed;
    }

    /// <summary>Takes hue, saturation and brightness from a color, keeping the hue where it has none.</summary>
    private static void Take(Vector4 color, bool keepHue)
    {
        ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out var hue, out var saturation, out var value);

        if (!keepHue || (saturation > 0f && value > 0f)) _hue = hue;

        _saturation = saturation;
        _value = value;
        _hex = EditorTheme.Hex(color);
    }

    /// <summary>Writes the kept hue, saturation and brightness back into the color.</summary>
    private static void Give(ref Vector4 color)
    {
        var rgb = ToRgb(_hue, _saturation, _value);
        color = new Vector4(rgb, color.W);
        _hex = EditorTheme.Hex(color);
    }

    private static Vector3 ToRgb(float hue, float saturation, float value)
    {
        ImGui.ColorConvertHSVtoRGB(hue, saturation, value, out var r, out var g, out var b);
        return new Vector3(r, g, b);
    }

    /// <summary>
    /// The square: saturation across, brightness down, at the hue the bar sets.
    /// </summary>
    private static bool Square(ref Vector4 color, uint behind)
    {
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(Width, Tall);

        ImGui.InvisibleButton("##square", size);

        var changed = false;

        if (ImGui.IsItemActive())
        {
            var pointer = ImGui.GetIO().MousePos - at;

            _saturation = Math.Clamp(pointer.X / size.X, 0f, 1f);
            _value = 1f - Math.Clamp(pointer.Y / size.Y, 0f, 1f);
            Give(ref color);
            changed = true;
        }

        var draw = ImGui.GetWindowDrawList();
        var pure = ImGui.GetColorU32(new Vector4(ToRgb(_hue, 1f, 1f), 1f));
        var white = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f));
        var black = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));
        var clear = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0f));

        draw.AddRectFilledMultiColor(at, at + size, white, pure, pure, white);
        draw.AddRectFilledMultiColor(at, at + size, clear, clear, black, black);
        EditorDraw.RoundOff(draw, at, at + size, EditorTheme.Current.ChildRounding, behind);

        Handle(draw, at + new Vector2(_saturation * size.X, (1f - _value) * size.Y), new Vector4(ToRgb(_hue, _saturation, _value), 1f));

        return changed;
    }

    /// <summary>The bar that sets the hue, running once round the color wheel.</summary>
    private static bool Hue(ref Vector4 color, uint behind)
    {
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(Width, Bar);

        ImGui.InvisibleButton("##hue", size);

        var changed = false;

        if (ImGui.IsItemActive())
        {
            _hue = Math.Clamp((ImGui.GetIO().MousePos.X - at.X) / size.X, 0f, 0.9999f);
            Give(ref color);
            changed = true;
        }

        var draw = ImGui.GetWindowDrawList();

        // Six stretches, one between each primary and secondary color, which is the wheel laid
        // flat.
        for (var stretch = 0; stretch < 6; stretch++)
        {
            var from = ImGui.GetColorU32(new Vector4(ToRgb(stretch / 6f, 1f, 1f), 1f));
            var to = ImGui.GetColorU32(new Vector4(ToRgb(((stretch + 1) % 6) / 6f, 1f, 1f), 1f));
            var left = at.X + (size.X * stretch / 6f);
            var right = at.X + (size.X * (stretch + 1) / 6f);

            draw.AddRectFilledMultiColor(new Vector2(left, at.Y), new Vector2(right, at.Y + size.Y), from, to, to, from);
        }

        EditorDraw.RoundOff(draw, at, at + size, size.Y * 0.5f, behind);
        Handle(draw, at + new Vector2(_hue * size.X, size.Y * 0.5f), new Vector4(ToRgb(_hue, 1f, 1f), 1f));

        return changed;
    }

    /// <summary>The bar that sets how clear the color is, over a checker so clear can be seen.</summary>
    private static bool Clearness(ref Vector4 color, uint behind)
    {
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(Width, Bar);

        ImGui.InvisibleButton("##alpha", size);

        var changed = false;

        if (ImGui.IsItemActive())
        {
            color.W = Math.Clamp((ImGui.GetIO().MousePos.X - at.X) / size.X, 0f, 1f);
            _hex = EditorTheme.Hex(color);
            changed = true;
        }

        var draw = ImGui.GetWindowDrawList();

        Checker(draw, at, at + size);

        var solid = new Vector4(color.X, color.Y, color.Z, 1f);

        draw.AddRectFilledMultiColor(
            at,
            at + size,
            ImGui.GetColorU32(solid with { W = 0f }),
            ImGui.GetColorU32(solid),
            ImGui.GetColorU32(solid),
            ImGui.GetColorU32(solid with { W = 0f }));

        EditorDraw.RoundOff(draw, at, at + size, size.Y * 0.5f, behind);
        Handle(draw, at + new Vector2(color.W * size.X, size.Y * 0.5f), color);

        return changed;
    }

    /// <summary>
    /// The color as it was beside the color as it is, then the hex box. Pressing the old half
    /// takes the color back to what it was.
    /// </summary>
    private static bool Preview(ref Vector4 color)
    {
        var height = ImGui.GetFrameHeight();
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(96f, height);
        var draw = ImGui.GetWindowDrawList();
        var changed = false;

        ImGui.InvisibleButton("##was", size with { X = size.X * 0.5f });

        if (ImGui.IsItemHovered()) EditorWidgets.Tip("Back to how it was");

        if (ImGui.IsItemClicked())
        {
            color = _was;
            Take(color, keepHue: false);
            changed = true;
        }

        ImGui.SameLine(0f, 0f);
        ImGui.Dummy(size with { X = size.X * 0.5f });

        var middle = at.X + (size.X * 0.5f);

        Checker(draw, at, at + size);

        draw.PushClipRect(at, new Vector2(middle, at.Y + size.Y), true);
        EditorDraw.Capsule(at, at + size, ImGui.GetColorU32(_was), draw);
        draw.PopClipRect();

        draw.PushClipRect(new Vector2(middle, at.Y), at + size, true);
        EditorDraw.Capsule(at, at + size, ImGui.GetColorU32(color), draw);
        draw.PopClipRect();

        EditorDraw.RoundOff(draw, at, at + size, size.Y * 0.5f, ImGui.GetColorU32(ImGuiCol.PopupBg));

        ImGui.SameLine();
        ImGui.SetNextItemWidth(Width - size.X - ImGui.GetStyle().ItemSpacing.X);

        // Taken when the box is left or Enter is pressed rather than on every character, since a
        // hex code is only a color once all of it is there.
        ImGui.InputText("##hex", ref _hex, 12, ImGuiInputTextFlags.CharsNoBlank);

        if (ImGui.IsItemDeactivated())
        {
            if (Parse(_hex) is { } typed)
            {
                color = typed;
                Take(color, keepHue: false);
                changed = true;
            }
            else
            {
                _hex = EditorTheme.Hex(color);
            }
        }

        return changed;
    }

    /// <summary>The numbers, as red, green and blue or as hue, saturation and brightness, and the clearness.</summary>
    private static bool Numbers(ref Vector4 color, bool alpha)
    {
        var changed = false;

        if (EditorWidgets.Pill("RGB", !_hsv)) _hsv = false;

        ImGui.SameLine();

        if (EditorWidgets.Pill("HSV", _hsv)) _hsv = true;

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var boxes = alpha ? 4f : 3f;
        var box = (Width - (spacing * (boxes - 1f))) / boxes;

        ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

        if (_hsv)
        {
            var hue = (int)MathF.Round(_hue * 360f);
            var saturation = (int)MathF.Round(_saturation * 100f);
            var value = (int)MathF.Round(_value * 100f);

            ImGui.SetNextItemWidth(box);
            var moved = ImGui.DragInt("##h", ref hue, 1f, 0, 359, "H %d");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragInt("##s", ref saturation, 0.5f, 0, 100, "S %d");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragInt("##v", ref value, 0.5f, 0, 100, "V %d");

            if (moved)
            {
                _hue = Math.Clamp(hue / 360f, 0f, 0.9999f);
                _saturation = saturation / 100f;
                _value = value / 100f;
                Give(ref color);
                changed = true;
            }
        }
        else
        {
            var red = (int)MathF.Round(color.X * 255f);
            var green = (int)MathF.Round(color.Y * 255f);
            var blue = (int)MathF.Round(color.Z * 255f);

            ImGui.SetNextItemWidth(box);
            var moved = ImGui.DragInt("##r", ref red, 1f, 0, 255, "R %d");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragInt("##g", ref green, 1f, 0, 255, "G %d");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragInt("##b", ref blue, 1f, 0, 255, "B %d");

            if (moved)
            {
                color = new Vector4(red / 255f, green / 255f, blue / 255f, color.W);
                Take(color, keepHue: true);
                changed = true;
            }
        }

        // How clear, in percent, which is how somebody says it.
        if (alpha)
        {
            var percent = (int)MathF.Round(color.W * 100f);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);

            if (ImGui.DragInt("##a", ref percent, 0.5f, 0, 100, "A %d%%"))
            {
                color.W = percent / 100f;
                _hex = EditorTheme.Hex(color);
                changed = true;
            }
        }

        ImGui.PopFont();

        return changed;
    }

    /// <summary>
    /// The round handle on the square and the bars: the color it points at, ringed in white and
    /// then in a hair of black, so it can be seen on any color under it.
    /// </summary>
    private static void Handle(ImDrawListPtr draw, Vector2 middle, Vector4 color)
    {
        const float Radius = 7f;

        draw.AddCircleFilled(middle, Radius, ImGui.GetColorU32(color with { W = 1f }), 24);
        draw.AddCircle(middle, Radius, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), 24, 2f);
        draw.AddCircle(middle, Radius + 1.5f, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f)), 24, 1f);
    }

    /// <summary>A checker of two grays, which is how clear is shown on a screen that cannot be.</summary>
    private static void Checker(ImDrawListPtr draw, Vector2 min, Vector2 max)
    {
        const float Square = 7f;

        var light = ImGui.GetColorU32(new Vector4(0.8f, 0.8f, 0.8f, 1f));
        var dark = ImGui.GetColorU32(new Vector4(0.55f, 0.55f, 0.55f, 1f));

        draw.AddRectFilled(min, max, light);
        draw.PushClipRect(min, max, true);

        var row = 0;

        for (var y = min.Y; y < max.Y; y += Square, row++)
        {
            for (var x = min.X + ((row % 2) * Square); x < max.X; x += Square * 2f)
            {
                draw.AddRectFilled(new Vector2(x, y), new Vector2(x + Square, y + Square), dark);
            }
        }

        draw.PopClipRect();
    }

    /// <summary>A hex code back into a color: six digits, or eight with alpha, with or without the mark.</summary>
    private static Vector4? Parse(string text)
    {
        var hex = text.Trim().TrimStart('#');

        if (hex.Length is not (6 or 8)) return null;

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return null;

        if (hex.Length == 6) value = (value << 8) | 0xFF;

        return new Vector4(
            ((value >> 24) & 0xFF) / 255f,
            ((value >> 16) & 0xFF) / 255f,
            ((value >> 8) & 0xFF) / 255f,
            (value & 0xFF) / 255f);
    }
}

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
/// started, and a box that takes a hex code. Last, the numbers, in RGB, HSV, HSL, OKLCH or OKLab,
/// with a list to switch between them.
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

    /// <summary>
    /// How much air the flyout keeps round the picker: more than a menu's, so a handle pulled to
    /// the end of a bar or the corner of the square still has room round it before the edge.
    /// </summary>
    internal static readonly Vector2 Air = new(14f, 14f);

    /// <summary>Which picker the kept state below belongs to.</summary>
    private static string _for = string.Empty;

    /// <summary>The color when this picker was opened, shown beside the color it has now.</summary>
    private static Vector4 _was;

    /// <summary>Hue, saturation and brightness as last chosen, from nothing to one.</summary>
    private static float _hue;
    private static float _saturation;
    private static float _value;

    /// <summary>The ways the numbers can be written, in the order the list offers them.</summary>
    private static readonly string[] Formats = ["RGB", "HSV", "HSL", "OKLCH", "OKLab"];

    /// <summary>Which of them the numbers are written in.</summary>
    private static string _format = "RGB";

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

        var middle = new Vector2(at.X + (size.X * 0.5f), at.Y + size.Y);

        // Two plain halves, then rounded as a whole. Two pills cut in half each leave their soft
        // edge over whatever is under them, which over a checker is a pale line round the shape.
        // The checker only where there is something clear to see through.
        if (_was.W < 1f || color.W < 1f) Checker(draw, at, at + size);

        draw.AddRectFilled(at, middle, ImGui.GetColorU32(_was));
        draw.AddRectFilled(middle with { Y = at.Y }, at + size, ImGui.GetColorU32(color));

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

    /// <summary>
    /// The numbers, in whichever format is chosen, and the clearness, with the list that chooses
    /// the format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RGB, HSV and HSL are the ones a color is usually typed in. OKLCH and OKLab are Björn
    /// Ottosson's perceptual spaces, where a step in lightness looks like the same step whatever
    /// the hue, which makes them the ones to pick a set of colors in that belong together, such as
    /// a theme's ladder of grays or a palette. CSS takes them as they are.
    /// </para>
    /// <para>
    /// A color set in OKLCH or OKLab that falls outside what the screen can show is brought back
    /// inside it, each channel clamped, which is what a browser does as well.
    /// </para>
    /// </remarks>
    private static bool Numbers(ref Vector4 color, bool alpha)
    {
        var changed = false;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        ImGui.SetNextItemWidth(alpha ? 96f : Width);
        EditorWidgets.Choice("##format", _format, Formats, chosen => _format = chosen);

        ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

        // How clear, in percent, which is how somebody says it, beside the list.
        if (alpha)
        {
            var percent = (int)MathF.Round(color.W * 100f);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(Width - 96f - spacing);

            if (ImGui.DragInt("##a", ref percent, 0.5f, 0, 100, "A %d%%"))
            {
                color.W = percent / 100f;
                _hex = EditorTheme.Hex(color);
                changed = true;
            }
        }

        var box = (Width - (spacing * 2f)) / 3f;

        bool Three(ref float x, ref float y, ref float z, (float Min, float Max, float Speed, string Format) a, (float Min, float Max, float Speed, string Format) b, (float Min, float Max, float Speed, string Format) c)
        {
            ImGui.SetNextItemWidth(box);
            var moved = ImGui.DragFloat("##x", ref x, a.Speed, a.Min, a.Max, a.Format, ImGuiSliderFlags.AlwaysClamp);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragFloat("##y", ref y, b.Speed, b.Min, b.Max, b.Format, ImGuiSliderFlags.AlwaysClamp);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(box);
            moved |= ImGui.DragFloat("##z", ref z, c.Speed, c.Min, c.Max, c.Format, ImGuiSliderFlags.AlwaysClamp);
            return moved;
        }

        var rgb = new Vector3(color.X, color.Y, color.Z);

        switch (_format)
        {
            case "HSV":
            {
                var h = _hue * 360f;
                var sat = _saturation * 100f;
                var val = _value * 100f;

                if (Three(ref h, ref sat, ref val, (0f, 359.9f, 1f, "H %.0f"), (0f, 100f, 0.5f, "S %.0f"), (0f, 100f, 0.5f, "V %.0f")))
                {
                    _hue = Math.Clamp(h / 360f, 0f, 0.9999f);
                    _saturation = sat / 100f;
                    _value = val / 100f;
                    Give(ref color);
                    changed = true;
                }

                break;
            }

            case "HSL":
            {
                var (h, sat, light) = ToHsl(rgb);
                h = _saturation > 0f && _value > 0f ? h : _hue * 360f;

                var sPercent = sat * 100f;
                var lPercent = light * 100f;

                if (Three(ref h, ref sPercent, ref lPercent, (0f, 359.9f, 1f, "H %.0f"), (0f, 100f, 0.5f, "S %.0f"), (0f, 100f, 0.5f, "L %.0f")))
                {
                    changed |= Set(ref color, FromHsl(h, sPercent / 100f, lPercent / 100f), h / 360f);
                }

                break;
            }

            case "OKLCH":
            {
                var (l, a, b) = ToOklab(rgb);
                var chroma = MathF.Sqrt((a * a) + (b * b));
                var hue = chroma > 1e-4f ? (MathF.Atan2(b, a) * 180f / MathF.PI + 360f) % 360f : _hue * 360f;
                var lPercent = l * 100f;

                if (Three(ref lPercent, ref chroma, ref hue, (0f, 100f, 0.5f, "L %.1f"), (0f, 0.4f, 0.002f, "C %.3f"), (0f, 359.9f, 1f, "H %.0f")))
                {
                    var angle = hue * MathF.PI / 180f;
                    changed |= Set(ref color, FromOklab(lPercent / 100f, chroma * MathF.Cos(angle), chroma * MathF.Sin(angle)), null);
                }

                break;
            }

            case "OKLab":
            {
                var (l, a, b) = ToOklab(rgb);
                var lPercent = l * 100f;

                if (Three(ref lPercent, ref a, ref b, (0f, 100f, 0.5f, "L %.1f"), (-0.4f, 0.4f, 0.002f, "a %.3f"), (-0.4f, 0.4f, 0.002f, "b %.3f")))
                {
                    changed |= Set(ref color, FromOklab(lPercent / 100f, a, b), null);
                }

                break;
            }

            default:
            {
                var red = color.X * 255f;
                var green = color.Y * 255f;
                var blue = color.Z * 255f;

                if (Three(ref red, ref green, ref blue, (0f, 255f, 1f, "R %.0f"), (0f, 255f, 1f, "G %.0f"), (0f, 255f, 1f, "B %.0f")))
                {
                    changed |= Set(ref color, new Vector3(red, green, blue) / 255f, null);
                }

                break;
            }
        }

        ImGui.PopFont();

        return changed;
    }

    /// <summary>
    /// Takes a color worked out from the numbers, keeping the hue the numbers named where the color
    /// itself has none.
    /// </summary>
    private static bool Set(ref Vector4 color, Vector3 rgb, float? hue)
    {
        color = new Vector4(Vector3.Clamp(rgb, Vector3.Zero, Vector3.One), color.W);
        Take(color, keepHue: true);

        if (hue is { } kept && (_saturation <= 0f || _value <= 0f)) _hue = Math.Clamp(kept, 0f, 0.9999f);

        return true;
    }

    /// <summary>Hue in degrees, saturation and lightness, from sRGB.</summary>
    internal static (float Hue, float Saturation, float Lightness) ToHsl(Vector3 rgb)
    {
        var max = MathF.Max(rgb.X, MathF.Max(rgb.Y, rgb.Z));
        var min = MathF.Min(rgb.X, MathF.Min(rgb.Y, rgb.Z));
        var light = (max + min) * 0.5f;
        var spread = max - min;

        if (spread <= 0f) return (0f, 0f, light);

        var sat = spread / (1f - MathF.Abs((2f * light) - 1f));

        ImGui.ColorConvertRGBtoHSV(rgb.X, rgb.Y, rgb.Z, out var h, out _, out _);

        return (h * 360f, Math.Clamp(sat, 0f, 1f), light);
    }

    /// <summary>sRGB from hue in degrees, saturation and lightness.</summary>
    internal static Vector3 FromHsl(float hue, float saturation, float lightness)
    {
        var chroma = (1f - MathF.Abs((2f * lightness) - 1f)) * saturation;
        var value = lightness + (chroma * 0.5f);
        var sat = value <= 0f ? 0f : chroma / value;

        return ToRgb(Math.Clamp(hue / 360f, 0f, 0.9999f), sat, value);
    }

    /// <summary>OKLab from sRGB, through linear light, as Ottosson defines it.</summary>
    internal static (float L, float A, float B) ToOklab(Vector3 rgb)
    {
        var (r, g, b, _) = EditorTheme.Linear(new Vector4(rgb, 1f));

        var l = MathF.Cbrt((0.4122214708f * r) + (0.5363325363f * g) + (0.0514459929f * b));
        var m = MathF.Cbrt((0.2119034982f * r) + (0.6806995451f * g) + (0.1073969566f * b));
        var s = MathF.Cbrt((0.0883024619f * r) + (0.2817188376f * g) + (0.6299787005f * b));

        return (
            (0.2104542553f * l) + (0.7936177850f * m) - (0.0040720468f * s),
            (1.9779984951f * l) - (2.4285922050f * m) + (0.4505937099f * s),
            (0.0259040371f * l) + (0.7827717662f * m) - (0.8086757660f * s));
    }

    /// <summary>sRGB from OKLab, clamped into what a screen shows.</summary>
    internal static Vector3 FromOklab(float lightness, float a, float b)
    {
        var l = lightness + (0.3963377774f * a) + (0.2158037573f * b);
        var m = lightness - (0.1055613458f * a) - (0.0638541728f * b);
        var s = lightness - (0.0894841775f * a) - (1.2914855480f * b);

        l *= l * l;
        m *= m * m;
        s *= s * s;

        var linear = new Vector3(
            (4.0767416621f * l) - (3.3077115913f * m) + (0.2309699292f * s),
            (-1.2684380046f * l) + (2.6097574011f * m) - (0.3413193965f * s),
            (-0.0041960863f * l) - (0.7034186147f * m) + (1.7076147010f * s));

        static float Encode(float channel)
        {
            channel = Math.Clamp(channel, 0f, 1f);
            return channel <= 0.0031308f ? channel * 12.92f : (1.055f * MathF.Pow(channel, 1f / 2.4f)) - 0.055f;
        }

        return new Vector3(Encode(linear.X), Encode(linear.Y), Encode(linear.Z));
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

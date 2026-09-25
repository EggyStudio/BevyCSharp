using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The shapes the editor draws by hand.
/// </summary>
/// <remarks>
/// What a rectangle with corners on it, a picture and an eye are made of. ImGui rounds a
/// rectangle by at most half its shortest side less a pixel, and several of its widgets clamp
/// further still, so anything meant to end in a half circle is drawn here instead of asked for.
/// </remarks>
public static class EditorDraw
{
    /// <summary>
    /// Draws an icon into a square, if the picture has loaded.
    /// </summary>
    /// <remarks>
    /// The icons are shapes cut out of white, so what color one comes out is the tint it is drawn
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
    /// Draws a picture as itself, fitted inside a square without being stretched.
    /// </summary>
    /// <remarks>
    /// Untinted, unlike <see cref="Icon"/>, because the editor's icons are shapes cut out of white
    /// and a file's own picture is what it is. The shape comes back only once the file has loaded,
    /// so a picture asked for on the frame a folder is opened is drawn square for that one frame
    /// and correctly from the next.
    /// </remarks>
    /// <param name="draw">The list to draw into.</param>
    /// <param name="path">The picture, under the asset root.</param>
    /// <param name="at">The top left of the square it is fitted into.</param>
    /// <param name="size">How large that square is.</param>
    /// <returns>True when something was drawn.</returns>
    internal static bool Picture(ImDrawListPtr draw, string path, Vector2 at, float size)
    {
        var picture = ImGuiTextures.Load(path);
        if (picture == 0) return false;

        var (width, height) = ImGuiTextures.SizeOf(picture);

        // The largest square that fits, until the file says otherwise.
        var shape = width > 0 && height > 0
            ? new Vector2(width, height)
            : new Vector2(size, size);

        var scale = MathF.Min(size / shape.X, size / shape.Y);
        var drawn = shape * scale;
        var inset = (new Vector2(size, size) - drawn) * 0.5f;

        draw.AddImage(
            (IntPtr)picture,
            at + inset,
            at + inset + drawn,
            Vector2.Zero,
            Vector2.One,
            ImGui.GetColorU32(Vector4.One));

        return true;
    }

    /// <summary>
    /// A rectangle with its corners taken off, rounded by exactly what it was asked for.
    /// </summary>
    /// <remarks>
    /// Every fill in the editor that has a corner goes through here. ImGui rounds a rectangle by
    /// at most half its shortest side less a pixel, so a shape meant to end in a half circle ends
    /// in one with a flat two pixels wide, and the flat shows on everything small: a handle, a
    /// row's pill, a closed card. This draws the four arcs itself and keeps the radius it is given.
    /// </remarks>
    /// <param name="min">The top left corner.</param>
    /// <param name="max">The bottom right.</param>
    /// <param name="radius">How round, at most half the shortest side.</param>
    /// <param name="color">What to fill it with.</param>
    /// <param name="onto">Which list to draw into, or the current window's.</param>
    internal static void Rounded(
        Vector2 min, Vector2 max, float radius, uint color, ImDrawListPtr? onto = null)
    {
        var across = max.X - min.X;
        var down = max.Y - min.Y;

        if (across <= 0f || down <= 0f) return;

        var draw = onto ?? ImGui.GetWindowDrawList();
        var round = MathF.Min(radius, MathF.Min(across, down) * 0.5f);

        if (round < 0.5f)
        {
            draw.AddRectFilled(min, max, color, 0f);
            return;
        }

        var quarter = MathF.PI * 0.5f;

        draw.PathClear();
        draw.PathArcTo(new Vector2(max.X - round, min.Y + round), round, -quarter, 0f, 0);
        draw.PathArcTo(new Vector2(max.X - round, max.Y - round), round, 0f, quarter, 0);
        draw.PathArcTo(new Vector2(min.X + round, max.Y - round), round, quarter, quarter * 2f, 0);
        draw.PathArcTo(new Vector2(min.X + round, min.Y + round), round, quarter * 2f, quarter * 3f, 0);
        draw.PathFillConvex(color);
    }

    /// <summary>A rectangle as round as its shortest side allows, which is a pill.</summary>
    /// <param name="min">The top left corner.</param>
    /// <param name="max">The bottom right.</param>
    /// <param name="color">What to fill it with.</param>
    /// <param name="onto">Which list to draw into, or the current window's.</param>
    internal static void Capsule(
        Vector2 min, Vector2 max, uint color, ImDrawListPtr? onto = null) =>
        Rounded(min, max, MathF.Max(max.X - min.X, max.Y - min.Y), color, onto);

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
    /// <param name="middle">Where the eye is centered.</param>
    /// <param name="size">How wide it is, corner to corner.</param>
    /// <param name="open">Whether the thing it belongs to is being drawn.</param>
    /// <param name="color">What to draw it in.</param>
    internal static void Eye(
        ImDrawListPtr draw, Vector2 middle, float size, bool open, uint color)
    {
        var across = size * 0.5f;

        // A quarter as tall as it is wide, which is the shape of an eye and not of a circle. The
        // control points are twice that out, because a quadratic curve reaches half way to the
        // point it is bent toward.
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
}

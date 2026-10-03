using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The tile an asset is shown as, wherever assets are shown as tiles: the asset browser and the
/// picker that chooses a mesh or a material.
/// </summary>
/// <remarks>
/// <para>
/// One face for both, so a mesh looks the same where it is browsed and where it is picked, and a
/// person finds it by the same picture. A tile is a rounded plate, the asset's picture on it, or
/// its kind's icon where it has none, a badge in its corner naming its kind, and its name under it,
/// cut to fit.
/// </para>
/// <para>
/// What happens when a tile is pressed is the caller's, since the browser selects and the picker
/// picks. This only says what a path looks like and draws it over the item made last.
/// </para>
/// </remarks>
internal static class AssetGrid
{
    /// <summary>How large an icon is drawn on a tile with no picture.</summary>
    private const float Mark = 34f;

    /// <summary>
    /// The picture a path wears, or nothing for one that wears its kind's icon.
    /// </summary>
    /// <remarks>
    /// An image is itself, because the interface loads a picture from a path and that is all it
    /// takes. A model, a mesh or a material wears the picture drawn of it once
    /// (<see cref="Thumbnails"/>), which asking for here sets drawing, and its icon until the
    /// picture is there. Everything else wears its kind's icon, since a sound or a script has
    /// nothing to look at.
    /// </remarks>
    internal static string? PictureOf(string path) => EditorAssets.KindOf(path) switch
    {
        "image" => path,
        "model" or "material" or "mesh" => Thumbnails.Of(path),
        _ => null,
    };

    /// <summary>The tiles too small to carry a badge, which would cover their picture.</summary>
    private const float Badged = 56f;

    /// <summary>The word a path's badge says, or nothing for a folder.</summary>
    /// <remarks>
    /// A picture alone does not say what a thing is. A model, a mesh of it and a material on a
    /// sphere are three gray shapes at a glance, and a texture and a screenshot of a scene look
    /// alike, so every tile names its kind in a word as well as showing it. The word is the
    /// singular of the chip that narrows the browser to that kind, so the two read as one.
    /// </remarks>
    internal static string BadgeOf(string path) => EditorAssets.KindOf(path) switch
    {
        "behavior script" => "Script",
        "file" => "File",
        var kind => char.ToUpperInvariant(kind[0]) + kind[1..],
    };

    /// <summary>Draws a tile's face over the square at <paramref name="at"/>.</summary>
    /// <param name="at">The tile's top left, in screen pixels.</param>
    /// <param name="size">How wide and how tall it is.</param>
    /// <param name="name">What it is called, written under the picture.</param>
    /// <param name="picture">The picture to wear, or nothing for the icon.</param>
    /// <param name="icon">The icon worn when there is no picture, or while it is being drawn.</param>
    /// <param name="picked">Whether it is the chosen one, which lights it.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    /// <param name="badge">The kind named in its corner, or nothing for a tile that says none.</param>
    internal static void Face(Vector2 at, float size, string name, string? picture, string icon, bool picked, bool over, string? badge = null)
    {
        var draw = ImGui.GetWindowDrawList();
        var line = ImGui.GetTextLineHeight();

        // Cut to the region rather than run under its edge, so a tile scrolled half out of sight
        // ends in a rounded corner instead of a square one.
        var top = at;
        var bottom = at + new Vector2(size, size);

        if (EditorSurface.Clipped(ref top, ref bottom))
        {
            EditorDraw.Rounded(
                top,
                bottom,
                ImGui.GetStyle().ChildRounding,
                ImGui.GetColorU32(picked
                    ? EditorTheme.LiveAccent
                    : over ? EditorTheme.LiveLift : EditorTheme.LiveGroup),
                draw);
        }

        var shown = picture is not null
                    && EditorDraw.Picture(
                        draw,
                        picture,
                        at + new Vector2(EditorSurface.Air, EditorSurface.Air),
                        size - (EditorSurface.Air * 2f) - line);

        if (!shown)
        {
            var middle = at + new Vector2((size - Mark) * 0.5f, (size - Mark) * 0.5f - (line * 0.6f));
            EditorDraw.Icon(draw, icon, middle, Mark, picked);
        }

        if (badge is not null && size >= Badged) Badge(draw, at, badge);

        // Its name under it, cut to what fits rather than spilling into the next tile.
        var fitted = EditorText.Fit(name, size - EditorSurface.Sides);
        var width = ImGui.CalcTextSize(fitted).X;

        draw.AddText(
            at + new Vector2((size - width) * 0.5f, size - line - EditorSurface.Air),
            ImGui.GetColorU32(EditorTheme.Ink(picked)),
            fitted);
    }

    /// <summary>A kind's word on a small plate in a tile's top left corner.</summary>
    /// <remarks>
    /// Smaller than the name under the tile, so the name stays what is read first, and on a plate
    /// of the panel's own color, so it reads over a white texture and a dark model alike. Drawn
    /// over the picture rather than beside it, since a tile has no room to spare.
    /// </remarks>
    private static void Badge(ImDrawListPtr draw, Vector2 at, string badge)
    {
        var font = ImGui.GetFontSize() * 0.75f;
        var word = ImGui.CalcTextSize(badge) * 0.75f;
        var pad = new Vector2(EditorSurface.Air * 0.75f, EditorSurface.Air * 0.25f);
        var from = at + new Vector2(EditorSurface.Air, EditorSurface.Air);
        var to = from + word + (pad * 2f);

        // Left out while the region's edge cuts it, since a plate this small cut square reads as a
        // smudge, and it comes back whole a scroll later.
        var top = from;
        var bottom = to;
        if (!EditorSurface.Clipped(ref top, ref bottom) || top != from || bottom != to) return;

        EditorDraw.Rounded(from, to, (to.Y - from.Y) * 0.5f, ImGui.GetColorU32(EditorTheme.Alpha(EditorTheme.LivePanel, 0.85f)), draw);
        draw.AddText(ImGui.GetFont(), font, from + pad, ImGui.GetColorU32(EditorTheme.Alpha(EditorTheme.LiveText, 0.8f)), badge);
    }
}

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
/// its kind's icon where it has none, and its name under it, cut to fit.
/// </para>
/// <para>
/// What happens when a tile is pressed is the caller's, since the browser selects and the picker
/// picks. This only says what a path looks like and draws it over the item just made.
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

    /// <summary>Draws a tile's face over the square at <paramref name="at"/>.</summary>
    /// <param name="at">The tile's top left, in screen pixels.</param>
    /// <param name="size">How wide and how tall it is.</param>
    /// <param name="name">What it is called, written under the picture.</param>
    /// <param name="picture">The picture to wear, or nothing for the icon.</param>
    /// <param name="icon">The icon worn when there is no picture, or while it is being drawn.</param>
    /// <param name="picked">Whether it is the chosen one, which lights it.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    internal static void Face(Vector2 at, float size, string name, string? picture, string icon, bool picked, bool over)
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

        // Its name under it, cut to what fits rather than spilling into the next tile.
        var fitted = EditorText.Fit(name, size - EditorSurface.Sides);
        var width = ImGui.CalcTextSize(fitted).X;

        draw.AddText(
            at + new Vector2((size - width) * 0.5f, size - line - EditorSurface.Air),
            ImGui.GetColorU32(EditorTheme.Ink(picked)),
            fitted);
    }
}

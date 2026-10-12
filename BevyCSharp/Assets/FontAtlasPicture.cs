namespace Bevy;

/// <summary>
/// The picture of one of the atlases Bevy draws a font's glyphs into, and the size of the font
/// drawn into it, as <see cref="Ui.FontAtlasPictures"/> answers them.
/// </summary>
/// <param name="Picture">The atlas's image, which grows as glyphs are added and can be drawn like any image.</param>
/// <param name="FontSize">The size of the font its glyphs are drawn at, in pixels.</param>
public readonly record struct FontAtlasPicture(AssetHandle Picture, float FontSize);

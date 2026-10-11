namespace Bevy;

/// <summary>Images packed into one atlas, from <see cref="Render2d.PackAtlas"/>.</summary>
/// <param name="Image">The packed image, which a sprite draws from.</param>
/// <param name="Layout">Where each image lies in it, for <see cref="SpriteSettings.Atlas"/>.</param>
/// <param name="Indices">
/// Each image's frame in the layout, in the order the images were given, for
/// <see cref="SpriteSettings.Frame"/>.
/// </param>
public sealed record PackedAtlas(AssetHandle Image, AssetHandle Layout, uint[] Indices);

namespace Bevy;

/// <summary>An image's texels as <see cref="Render.TryReadImage"/> reads them.</summary>
/// <param name="Width">Texels across.</param>
/// <param name="Height">Texels down.</param>
/// <param name="BytesPerTexel">Bytes each texel takes, four for an eight-bit RGBA picture.</param>
/// <param name="Data">The texels, row after row from the top.</param>
public sealed record ImagePixels(uint Width, uint Height, uint BytesPerTexel, byte[] Data);

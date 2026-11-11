using System.Buffers.Binary;
using System.IO.Compression;

namespace Bevy;

/// <summary>What a render target from <see cref="Render.CreateTarget"/> holds a pixel as.</summary>
public enum TargetFormat
{
    /// <summary>Eight bits a channel, sRGB, as a screen shows it.</summary>
    Rgba8,

    /// <summary>A half float a channel, linear, with room above white.</summary>
    Rgba16Float,
}

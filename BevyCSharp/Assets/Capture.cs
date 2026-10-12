using System.Buffers.Binary;
using System.IO.Compression;

namespace Bevy;

/// <summary>
/// A picture that was asked for and has not arrived yet.
/// </summary>
/// <remarks>
/// A capture is answered frames after it is asked for, because the picture has to come back off
/// the GPU, so what <see cref="Render.BeginCapture(AssetHandle)"/> can hand over at the time is a
/// name for the answer rather than the answer.
/// </remarks>
/// <param name="Id">What the engine knows this capture by.</param>
public readonly record struct Capture(int Id)
{
    /// <summary>True when this names a capture that was actually asked for.</summary>
    public bool IsValid => Id > 0;

    /// <inheritdoc/>
    public override string ToString() => $"capture {Id}";
}

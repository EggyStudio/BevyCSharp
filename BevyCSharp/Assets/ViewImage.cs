namespace Bevy;

/// <summary>An image a camera owns. See <see cref="Shaders.SetViewImages"/>.</summary>
/// <param name="Name">The name a shader on the camera reads or writes it by.</param>
/// <param name="Format">What it holds per pixel.</param>
/// <param name="Scale">A fraction of the camera's picture, one for the same size.</param>
/// <param name="History">Whether last frame's is kept too, as <c>Name_previous</c>.</param>
/// <param name="Mips">How many mip levels, each reachable as <c>Name_mip0</c> and on.</param>
/// <param name="ClearEachFrame">
/// Whether it is cleared to zero at the start of every frame, before anything on the camera runs,
/// for an image that draws or atomics accumulate into.
/// </param>
/// <param name="CopyAt">
/// A point of the frame at which the camera's picture is copied into the image, scaled to it and
/// point sampled, or null for none. With <paramref name="History"/>, <c>Name_previous</c> is then
/// last frame's picture, which a technique reusing last frame's lighting reads. Copied at
/// <see cref="FramePoint.BeforeTonemapping"/> it is the lit picture in its own units, and at
/// <see cref="FramePoint.AfterOpaque"/> the same without transparent geometry, which needs a camera
/// drawn once a pixel. Only a float or eight-bit format can hold it, and
/// <see cref="FramePoint.AfterPrepass"/> is refused, since nothing is lit there yet.
/// </param>
public readonly record struct ViewImage(
    string Name,
    ShaderImageFormat Format,
    float Scale = 1f,
    bool History = false,
    int Mips = 1,
    FramePoint? CopyAt = null,
    bool ClearEachFrame = false);

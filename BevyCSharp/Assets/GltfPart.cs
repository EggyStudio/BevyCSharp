using System.Buffers.Binary;
using System.Text.Json;

namespace Bevy;

/// <summary>One mesh, material or texture inside a glTF file, as Bevy labels it.</summary>
/// <param name="Label">
/// What follows the <c>#</c> in the asset path that loads it, such as <c>Mesh0/Primitive1</c>,
/// <c>Material2/std</c> or <c>Texture0</c>.
/// </param>
/// <param name="Name">What the file calls it, or a name made from its place when it calls it nothing.</param>
/// <param name="Kind">
/// <see cref="AssetKind.Mesh"/>, <see cref="AssetKind.StandardMaterial"/> or <see cref="AssetKind.Image"/>.
/// </param>
/// <param name="File">
/// For a texture whose picture is a file beside the model rather than bytes inside it, that file,
/// relative to the asset root. Nothing for every other part.
/// </param>
/// <remarks>
/// A texture is a part in two ways. Bytes inside the file, in a buffer or a data URI, are an image
/// Bevy labels <c>Texture{n}</c>. A URI naming a file is loaded from that file as any image is,
/// and Bevy gives it no label in the model, so the path that loads it is the file's own
/// (<see cref="File"/>), and <see cref="PathIn"/> answers it.
/// </remarks>
public sealed record GltfPart(string Label, string Name, string Kind, string? File = null)
{
    /// <summary>The asset path that loads this part of <paramref name="model"/>.</summary>
    /// <param name="model">The model file, relative to the asset root, as given to <see cref="GltfContents.Read"/>.</param>
    public string PathIn(string model) => File ?? model.Split('#')[0] + "#" + Label;
}

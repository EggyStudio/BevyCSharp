using System.Numerics;
using Bevy.Interop;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// The pictures an interface draws with.
/// </summary>
/// <remarks>
/// <para>
/// A file under the asset root, loaded by the engine the way every other asset is, and answered by
/// a name ImGui can put in a draw call. Asked for once per path and kept, because an icon is drawn
/// every frame and loaded once.
/// </para>
/// <para>
/// A picture is not there for a frame or two after it is asked for. Nothing is drawn for it until
/// it arrives, so an icon appears a moment after a panel opens.
/// </para>
/// </remarks>
public static unsafe class ImGuiTextures
{
    private static readonly Dictionary<string, ulong> Loaded = [];
    private static readonly Dictionary<int, ulong> Assets = [];

    /// <summary>What to call the picture at a path, loading it the first time it is asked for.</summary>
    public static ulong Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (Loaded.TryGetValue(path, out var picture)) return picture;

        picture = Native.bcs_imgui_picture(path);
        Loaded[path] = picture;

        return picture;
    }

    /// <summary>
    /// What to call a picture the caller already holds, such as one a camera is drawing into.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="Load"/>, which takes a path. This takes a handle, and a render
    /// target is one, so a thumbnail or a preview is a small scene drawn every frame rather than a
    /// picture somebody saved. Asked for once per handle and kept, because what it names does not
    /// change when the picture behind it is redrawn.
    /// </remarks>
    /// <param name="image">The image, usually from <see cref="Render.CreateTarget"/>.</param>
    /// <returns>A name ImGui can draw with, or zero when the handle names no image.</returns>
    public static ulong Of(AssetHandle image)
    {
        if (!image.IsValid) return 0;
        if (Assets.TryGetValue(image.Key, out var picture)) return picture;

        picture = Native.bcs_imgui_asset_texture(image.Key);
        Assets[image.Key] = picture;

        return picture;
    }

    /// <summary>
    /// How large a picture is, in pixels, or zero by zero while it is still loading.
    /// </summary>
    /// <remarks>
    /// What anything fitting a picture into a box needs, since a picture drawn into a square
    /// without knowing its shape is a picture stretched. A file is loaded in the background, so
    /// the first frame it is asked for answers zero and the frame after answers its size, which is
    /// why a caller reads it every frame rather than once.
    /// </remarks>
    /// <param name="picture">A name from <see cref="Load"/> or <see cref="Of"/>.</param>
    public static (uint Width, uint Height) SizeOf(ulong picture)
    {
        if (picture == 0) return (0, 0);

        uint width;
        uint height;

        if (Native.bcs_imgui_picture_size(picture, &width, &height) != NativeStatus.Ok)
            return (0, 0);

        return (width, height);
    }

    /// <summary>Draws a picture, tinted.</summary>
    /// <remarks>
    /// Tinted rather than drawn as it is, because the editor's icons are shapes cut out of white.
    /// The interface picks the color one appears in by what it means, not the file.
    /// </remarks>
    public static void Draw(string path, float size, Vector4 tint)
    {
        var picture = Load(path);

        if (picture == 0)
        {
            ImGui.Dummy(new Vector2(size, size));
            return;
        }

        ImGui.Image(
            (IntPtr)picture,
            new Vector2(size, size),
            Vector2.Zero,
            Vector2.One,
            tint);
    }

    /// <summary>A button that is a picture.</summary>
    public static bool Button(string id, string path, float size, Vector4 tint)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        var picture = Load(path);

        if (picture == 0) return ImGui.Button($"##{id}", new Vector2(size + 8f, size + 8f));

        return ImGui.ImageButton(
            id,
            (IntPtr)picture,
            new Vector2(size, size),
            Vector2.Zero,
            Vector2.One,
            new Vector4(0f, 0f, 0f, 0f),
            tint);
    }
}

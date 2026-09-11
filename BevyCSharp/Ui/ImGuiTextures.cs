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
/// a name ImGui can put in a draw call. Asked for once per path and kept: an icon is drawn every
/// frame and loaded once.
/// </para>
/// <para>
/// A picture is not there for a frame or two after it is asked for. Nothing is drawn for it until
/// it arrives, which is what an icon appearing a moment after a panel opens looks like.
/// </para>
/// </remarks>
public static class ImGuiTextures
{
    private static readonly Dictionary<string, ulong> Loaded = [];

    /// <summary>What to call the picture at a path, loading it the first time it is asked for.</summary>
    public static ulong Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (Loaded.TryGetValue(path, out var picture)) return picture;

        picture = Native.bcs_imgui_picture(path);
        Loaded[path] = picture;

        return picture;
    }

    /// <summary>Draws a picture, tinted.</summary>
    /// <remarks>
    /// Tinted rather than drawn as it is, because the editor's icons are shapes cut out of white:
    /// what colour one appears in is what the interface says it means, not what the file says.
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

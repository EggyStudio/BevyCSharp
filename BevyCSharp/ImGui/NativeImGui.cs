using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The bridge's entry points for ImGui.</summary>
/// <remarks>
/// Dear ImGui runs on this side, where it owns the windows, the widgets and what they are worth.
/// All that crosses is the triangles it asked for, once a frame, and the pictures they read from.
/// </remarks>
internal static unsafe partial class NativeImGui
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeImGui() => NativeLoader.Initialize();

    /// <summary>Hands over this frame's triangles.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_imgui_frame(NativeImGuiFrame* frame);

    /// <summary>Takes a picture the interface draws with, and answers what to call it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_imgui_texture(byte* pixels, uint width, uint height);

    /// <summary>Takes a picture from a file under the asset root.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_imgui_picture(string path);

    /// <summary>Writes how large a picture is, in pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_imgui_picture_size(ulong picture, uint* width, uint* height);

    /// <summary>Names an image asset the caller already has, so the interface can draw it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_imgui_asset_texture(int image);

    /// <summary>Forgets a picture.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_imgui_drop_texture(ulong texture);
}

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    // ---------------------------------------------------------------------------------------
    // The page.
    //
    // One document holds the whole interface. What is here is a document interface and nothing
    // else, so no widgets, no panels and no placement. Where a thing sits and what it looks like is
    // CSS, read by the same engine a browser reads it with.
    // ---------------------------------------------------------------------------------------

    // ---------------------------------------------------------------------------------------
    // The interface.
    //
    // Dear ImGui runs on this side, where it owns the windows, the widgets and what they are worth.
    // All that crosses is the triangles it asked for, once a frame, and the pictures they read
    // from. ---------------------------------------------------------------------------------------

    /// <summary>Moves, presses or releases the pointer, as though a hand had.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_pointer(float x, float y, int action, int button);

    /// <summary>Copies the connected gamepads out, answering how many there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepads(NativeGamepad* gamepads, int capacity);

    /// <summary>Rumbles a pad, or stops it where the seconds are zero or less.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_rumble(ulong entity, float strong, float weak, float seconds);

    /// <summary>Connects a pretended pad, answering its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_gamepad_connect(byte* name, uint length);

    /// <summary>Disconnects a pretended pad.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_disconnect(ulong entity);

    /// <summary>Sets one of a pad's buttons.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_button(ulong entity, int button, float value);

    /// <summary>Sets one of a pad's axes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_axis(ulong entity, int axis, float value);

    /// <summary>Presses or releases a key, with whatever text it typed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_key(int key, int action, byte* text, uint length);

    /// <summary>Writes how many physical pixels a logical one is.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_scale(float* scale);

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

    /// <summary>Sets a camera's field of view and clip distances.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_perspective(ulong camera, float fovDegrees, float near, float far);

    /// <summary>Rounds the corners of a camera's picture, showing a fill outside them, or squares them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_rounded_corners(ulong camera, float radius, float r, float g, float b, float a);

    /// <summary>Sets the world's clear color.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_clear_color(float r, float g, float b, float a);

    /// <summary>Gives a camera part of the window to draw into, or all of it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_viewport(
        ulong camera, uint x, uint y, uint width, uint height);

    /// <summary>Throws if <paramref name="status"/> is a failure code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Check(int status, string operation)
    {
        if (status < 0) NativeStatus.Throw(status, operation);
        return status;
    }
}

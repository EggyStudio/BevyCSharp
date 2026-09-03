using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>App configuration handed to the native bridge at construction.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeConfig
{
    /// <summary>UTF-8 window title.</summary>
    public byte* Title;

    /// <summary>Requested window width.</summary>
    public uint Width;

    /// <summary>Requested window height.</summary>
    public uint Height;

    /// <summary>Non-zero to present with vsync.</summary>
    public uint Vsync;

    /// <summary>Non-zero to skip window creation.</summary>
    public uint Headless;

    /// <summary>Frame cap for headless runs; 0 for uncapped.</summary>
    public uint HeadlessFps;

    /// <summary>Frames to run before exiting; 0 to run until exit is requested.</summary>
    public uint HeadlessFrames;

    /// <summary>Graphics API to pin the renderer to; 0 leaves the choice to wgpu.</summary>
    public uint Backend;

    /// <summary>Fixed timestep rate in Hz; 0 keeps Bevy's default of 64.</summary>
    public double FixedHz;

    /// <summary>UTF-8 assets directory, or null for Bevy's default.</summary>
    public byte* AssetRoot;

    /// <summary>Non-zero to reload an asset when its file changes on disk.</summary>
    public uint WatchAssets;
}

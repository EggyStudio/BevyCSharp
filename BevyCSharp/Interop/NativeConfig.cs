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

    /// <summary>Non-zero to build in the HTML and CSS interface.</summary>
    public uint Gui;

    /// <summary>Non-zero to draw with no window, into an image a capture reads back.</summary>
    public uint Offscreen;

    /// <summary>How many world units a meter is, for spatial sound. Zero keeps Bevy's own.</summary>
    public float SpatialScale;

    /// <summary>Meshlet clusters the GPU keeps room for, or zero for no meshlets.</summary>
    public uint MeshletClusters;

    /// <summary>Non-zero to measure how long every render pass takes.</summary>
    public uint GpuTimings;

    /// <summary>Non-zero to light with Bevy's ray tracing where a camera asks.</summary>
    public uint RayTracedLighting;

    /// <summary>Non-zero to make the window see-through where what is drawn has no alpha.</summary>
    public uint Transparent;

    /// <summary>Non-zero to have the desktop draw the title bar where it only does for X11 windows.</summary>
    public uint DesktopTitleBar;

    /// <summary>UTF-8 path of the player's directory, registered as Bevy's <c>user</c> asset source.</summary>
    public byte* UserRoot;

    /// <summary>Non-zero to open the window at <see cref="X"/> and <see cref="Y"/>.</summary>
    public uint HasPosition;

    /// <summary>Where the window's top left corner opens, in physical pixels.</summary>
    public int X;

    /// <summary>See <see cref="X"/>.</summary>
    public int Y;

    /// <summary>Non-zero to add Bevy's wireframe plugins, for 3D and 2D meshes.</summary>
    public uint Wireframes;

    /// <summary>Non-zero to add Bevy's frame time diagnostics and its log of them once a second.</summary>
    public uint LogFrameTimes;

    /// <summary>The window's scale factor in place of the display's, or zero for the display's.</summary>
    public float ScaleFactor;

    /// <summary>Seconds each frame advances the clock by, or zero for the machine's clock.</summary>
    public double FrameSeconds;

    /// <summary>Non-zero to add Bevy's mesh picking.</summary>
    public uint MeshPicking;
}

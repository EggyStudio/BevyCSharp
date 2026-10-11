// Bevy's custom_cursor_image example, examples/window/custom_cursor_image.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// Draws the pointer from one frame of a sheet of crosshairs, stepping through its frames four a
// second. T takes the sheet's atlas away, so the pointer is the whole sheet, and puts it back, X
// and Y flip the pointer, and C cycles through the four quarters of the frame and back to all of it.
//
// The sheet and its atlas go on the window's pointer through Window.SetCursorImage and
// Window.SetCursorAtlas, and the frame, the flips and the quarter are written through the window's
// CursorIconRef, as Bevy's systems write its CursorIcon. An offscreen run has no window and no
// pointer, so it shows the instructions alone.
internal static class CustomCursorImageExample
{
    // The sheet's frames, sixty-four pixels square, twenty across and ten down, five apart.
    private static AssetHandle _layout;

    // The atlas's frame while T has it taken away, Bevy's cached TextureAtlas.
    private static ulong? _cached;

    // Half a frame, the size of each quarter C cycles through.
    private const uint RectSize = 32;

    private static readonly OptionURect[] Sections =
    [
        new OptionURect.Some(0, 0, RectSize, RectSize),
        new OptionURect.Some(RectSize, 0, 2 * RectSize, RectSize),
        new OptionURect.Some(0, RectSize, RectSize, 2 * RectSize),
        new OptionURect.Some(RectSize, RectSize, 2 * RectSize, 2 * RectSize),
        new OptionURect.None(),
    ];

    public static void Build(App app)
    {
        _cached = null;
        app.Startup(Setup, "custom_cursor_image.Setup");
        app.Update(ToggleTextureAtlas, "custom_cursor_image.ToggleTextureAtlas");
        app.Update(ToggleFlips, "custom_cursor_image.ToggleFlips");
        app.Update(CycleRect, "custom_cursor_image.CycleRect");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SpawnCamera3d();
        Ui.SpawnText(
            "Press T to toggle the cursor's `texture_atlas`.\n\n"
            + "Press X to toggle the cursor's `flip_x` setting.\n\n"
            + "Press Y to toggle the cursor's `flip_y` setting.\n\n"
            + "Press C to cycle through the sections of the cursor's image using `rect`.",
            new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

        var window = Window.Entity();
        if (window == Entity.None) return;

        var animation = CursorAnimation.New(0, 199, 1, 4);
        _layout = Render2d.CreateAtlas(64, 64, 20, 10, padding: (5, 5));
        Window.SetCursorImage(AssetServer.Load(AssetKind.Image, "cursors/kenney_crosshairPack/Tilesheet/crosshairs_tilesheet_white.png"));
        Window.SetCursorAtlas(_layout, (uint)animation.FirstSpriteIndex);
        ecs.Add(window, animation);
    }

    // The window's pointer as the image it is, or nothing where it is not one.
    internal static (CursorIconRef Icon, CustomCursor.Image Image)? Pointer(EcsWorld ecs)
    {
        var window = Window.Entity();
        if (window == Entity.None || ecs.Get<CursorIconRef>(window) is not { } icon) return null;
        return icon.Value is CursorIcon.Custom { Value: CustomCursor.Image image } ? (icon, image) : null;
    }

    // Writes the pointer's image back, its variant kept, so the hotspot and the atlas's layout,
    // which the record does not hold, stay as they were set.
    internal static void Write(CursorIconRef icon, CustomCursor.Image image) => icon.Value = new CursorIcon.Custom(image);

    private static void ToggleTextureAtlas(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.T) || Pointer(ctx.Ecs) is not (_, var image)) return;

        if (image.TextureAtlas is OptionTextureAtlas.Some { Index: var index })
        {
            _cached = index;
            Window.SetCursorAtlas(null);
        }
        else if (_cached is { } cached)
        {
            Window.SetCursorAtlas(_layout, (uint)cached);
            _cached = null;
        }
    }

    private static void ToggleFlips(BehaviorContext ctx)
    {
        var (x, y) = (ctx.Input.KeyPressed(Key.X), ctx.Input.KeyPressed(Key.Y));
        if (!(x || y) || Pointer(ctx.Ecs) is not (var icon, var image)) return;

        Write(icon, image with { FlipX = image.FlipX ^ x, FlipY = image.FlipY ^ y });
    }

    private static void CycleRect(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.C) || Pointer(ctx.Ecs) is not (var icon, var image)) return;

        // The quarter after the one shown, or the first where the whole frame is.
        var at = Array.IndexOf(Sections, image.Rect);
        Write(icon, image with { Rect = Sections[(at + 1) % Sections.Length] });
    }
}

/// <summary>How the pointer's frames run, which ones, how far each step goes, and how fast.</summary>
[Behavior]
public partial struct CursorAnimation
{
    /// <summary>The first frame on the sheet.</summary>
    public int FirstSpriteIndex;

    /// <summary>The last frame on the sheet.</summary>
    public int LastSpriteIndex;

    /// <summary>How many frames each step moves on.</summary>
    public int Increment;

    /// <summary>How many steps a second.</summary>
    public int Fps;

    /// <summary>The time the frame shown has left, set going again for each next one.</summary>
    public GameTimer FrameTimer;

    /// <summary>An animation over the frames given, its first frame's timer already running.</summary>
    public static CursorAnimation New(int first, int last, int increment, int fps) =>
        new() { FirstSpriteIndex = first, LastSpriteIndex = last, Increment = increment, Fps = fps, FrameTimer = TimerFromFps(fps) };

    /// <summary>A frame's time at the speed given, once.</summary>
    public static GameTimer TimerFromFps(int fps) => GameTimer.FromSeconds(1f / fps, TimerMode.Once);

    /// <summary>On to the next frame as the frame's time runs out, back to the first past the last.</summary>
    [OnUpdate]
    public void ExecuteAnimation(BehaviorContext ctx)
    {
        FrameTimer.Tick(ctx.Time.Delta);
        if (!FrameTimer.Finished || CustomCursorImageExample.Pointer(ctx.Ecs) is not (var icon, var image)
            || image.TextureAtlas is not OptionTextureAtlas.Some { Index: var index })
            return;

        var next = index + (ulong)Increment;
        if (next > (ulong)LastSpriteIndex) next = (ulong)FirstSpriteIndex;

        CustomCursorImageExample.Write(icon, image with { TextureAtlas = new OptionTextureAtlas.Some(next) });
        FrameTimer = TimerFromFps(Fps);
    }
}

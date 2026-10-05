using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// The settings of a window changed as it runs. Its title counts the seconds, V turns vertical sync
// off and on, T moves it below, among and above other windows, 1, 2 and 3 take away and give back
// its minimize, maximize and close buttons, Space hides and grabs the pointer, F swaps its light and
// dark theme, and the left and right buttons step through pointers. It opens hidden and shows itself
// on the third frame.
//
// Bevy's pointers include an image of its own, which needs its custom_cursor feature, and it logs
// its frame time each second. Neither is here, the first since the bridge does not compile that
// feature in.
internal static class WindowSettings
{
    // Bevy's window, named for the paths inside its theme, which no wrapper types.
    private const string BevyWindow = "bevy_window::window::Window";

    private static readonly CursorShape[] Cursors = [CursorShape.Default, CursorShape.Pointer, CursorShape.Wait, CursorShape.Text];

    private static int _cursor;
    private static bool _cursorFree;

    public static void Configure(Config config)
    {
        config.Title = "I am a window!";
        (config.Width, config.Height) = (500, 300);
    }

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            (_cursor, _cursorFree) = (0, true);
            var window = Window.Entity();
            if (window == Entity.None) return;

            var settings = ctx.Ecs.Wrap<WindowRef>(window);
            settings.Name = "bevy.app";
            settings.PresentMode = WindowRef.PresentModeVariant.AutoVsync;
            settings.EnabledButtonsMaximize = false;
            settings.Visible = false;
            SetTheme(ctx.Ecs, window, "Dark");
            Window.SetCursorShape(Cursors[0]);
        }, "window_settings.InitCursorIcons");

        app.Update(Settings, "window_settings.Settings");
    }

    // The theme is an option of a choice, set to some through the wrapper and then to which by
    // the path inside it, which no wrapper types.
    private static void SetTheme(EcsWorld ecs, Entity window, string theme)
    {
        ecs.Wrap<WindowRef>(window).WindowTheme = WindowRef.WindowThemeVariant.Some;
        ecs.SetVariant(window, BevyWindow, ".window_theme.0", theme);
    }

    private static void Settings(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var window = Window.Entity();
        if (window == Entity.None) return;
        var settings = ecs.Wrap<WindowRef>(window);

        if (ctx.Time.FrameCount == 3) settings.Visible = true;
        Window.SetTitle($"Seconds since startup: {MathF.Round(ctx.Time.Elapsed)}");

        if (input.KeyPressed(Key.V))
        {
            settings.PresentMode = settings.PresentMode == WindowRef.PresentModeVariant.AutoVsync
                ? WindowRef.PresentModeVariant.AutoNoVsync
                : WindowRef.PresentModeVariant.AutoVsync;
            Console.WriteLine($"PRESENT_MODE: {settings.PresentMode}");
        }

        if (input.KeyPressed(Key.T))
        {
            settings.WindowLevel = settings.WindowLevel switch
            {
                WindowRef.WindowLevelVariant.AlwaysOnBottom => WindowRef.WindowLevelVariant.Normal,
                WindowRef.WindowLevelVariant.Normal => WindowRef.WindowLevelVariant.AlwaysOnTop,
                _ => WindowRef.WindowLevelVariant.AlwaysOnBottom,
            };
            Console.WriteLine($"WINDOW_LEVEL: {settings.WindowLevel}");
        }

        if (input.KeyPressed(Key.Digit1)) settings.EnabledButtonsMinimize = !settings.EnabledButtonsMinimize;
        if (input.KeyPressed(Key.Digit2)) settings.EnabledButtonsMaximize = !settings.EnabledButtonsMaximize;
        if (input.KeyPressed(Key.Digit3)) settings.EnabledButtonsClose = !settings.EnabledButtonsClose;

        if (input.KeyPressed(Key.Space))
        {
            _cursorFree = !_cursorFree;
            Window.SetCursor(_cursorFree ? CursorGrab.None : CursorGrab.Locked, visible: _cursorFree);
        }

        if (input.KeyPressed(Key.F) && settings.WindowTheme == WindowRef.WindowThemeVariant.Some)
            SetTheme(ecs, window, ecs.GetVariant(window, BevyWindow, ".window_theme.0") == "Light" ? "Dark" : "Light");

        if (input.MousePressed(MouseButton.Left)) _cursor = (_cursor + 1) % Cursors.Length;
        else if (input.MousePressed(MouseButton.Right)) _cursor = (_cursor + Cursors.Length - 1) % Cursors.Length;
        if (input.MousePressed(MouseButton.Left) || input.MousePressed(MouseButton.Right)) Window.SetCursorShape(Cursors[_cursor]);
    }
}

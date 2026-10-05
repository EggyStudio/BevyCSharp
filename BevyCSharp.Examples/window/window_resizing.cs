using System.Globalization;
using Bevy;

namespace BevyCSharp.Examples.Windowing;

// The window's size, shown each time it changes, and three sizes to change it to with 1, 2 and 3.
//
// Bevy writes the size from each resize message it reads. Here the size is read each frame and
// written when it differs from the last, which is the same moments.
internal static class WindowResizing
{
    private static readonly (uint Width, uint Height) Small = (640, 360), Medium = (800, 600), Large = (1920, 1080);

    private static Entity _text;
    private static (uint Width, uint Height)? _last;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            _last = null;
            Render2d.SpawnCamera2d();
            var row = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f) });
            _text = Ui.SpawnText("Resolution", new UiSettings(), 42f);
            ctx.Ecs.SetParent(_text, row);
        }, "window_resizing.Setup");

        app.Update(ctx =>
        {
            var size = Window.Size();
            if (size == _last) return;
            _last = size;
            Ui.SetText(_text, string.Format(CultureInfo.InvariantCulture, "{0:0.0} x {1:0.0}", (float)size.Width, (float)size.Height));
        }, "window_resizing.OnResizeSystem");

        app.Update(ctx =>
        {
            if (Window.Entity() == Entity.None) return;
            if (ctx.Input.KeyPressed(Key.Digit1)) Window.SetSize(Small.Width, Small.Height);
            if (ctx.Input.KeyPressed(Key.Digit2)) Window.SetSize(Medium.Width, Medium.Height);
            if (ctx.Input.KeyPressed(Key.Digit3)) Window.SetSize(Large.Width, Large.Height);
        }, "window_resizing.ToggleResolution");
    }
}

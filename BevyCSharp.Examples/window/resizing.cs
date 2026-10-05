// Bevy's resizing example, tests/window/resizing.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// A window that shrinks and grows by four pixels a frame, its height to one, then its width, then
// back, over a scene drawn by a 3D camera and a 2D one above it, its scale factor held at one.
internal static class Resizing
{
    private const int MaxSize = 401, MinSize = 1, Step = 4;

    private enum Phase { ContractingY, ContractingX, ExpandingY, ExpandingX }

    private static Phase _phase;
    private static int _width, _height;
    private static bool _firstComplete;

    public static void Configure(Config config)
    {
        config.Title = "Resizing";
        (config.Width, config.Height) = (MaxSize, MaxSize);
    }

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            (_phase, _width, _height, _firstComplete) = (Phase.ContractingY, MaxSize, MaxSize, false);
            if (Window.Entity() is var window && window != Entity.None) ctx.Ecs.Wrap<WindowRef>(window).ResolutionScaleFactorOverride = 1f;
            TestScene.Setup(ctx.Ecs);
        }, "resizing.Setup");

        app.Update(_ =>
        {
            // Bevy leaves the first frame alone.
            if (!_firstComplete)
            {
                _firstComplete = true;
                return;
            }

            switch (_phase)
            {
                case Phase.ContractingY when _height <= MinSize: _phase = Phase.ContractingX; break;
                case Phase.ContractingY: _height -= Step; break;
                case Phase.ContractingX when _width <= MinSize: _phase = Phase.ExpandingY; break;
                case Phase.ContractingX: _width -= Step; break;
                case Phase.ExpandingY when _height >= MaxSize: _phase = Phase.ExpandingX; break;
                case Phase.ExpandingY: _height += Step; break;
                case Phase.ExpandingX when _width >= MaxSize: _phase = Phase.ContractingY; break;
                default: _width += Step; break;
            }

            if (Window.Entity() != Entity.None) Window.SetSize((uint)_width, (uint)_height);
        }, "resizing.ChangeWindowSize");
    }
}

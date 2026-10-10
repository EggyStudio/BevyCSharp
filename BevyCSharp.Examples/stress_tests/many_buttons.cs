// Bevy's many_buttons example, examples/stress_tests/many_buttons.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// A grid of interface buttons, 110 a side unless --buttons says, each in a color of the rainbow,
// lit orange red while the pointer is over it, every fourth an image unless --image-freq says, to
// measure layout, text, the pointer and drawing at once. --text labels each button, --no-borders
// leaves the borders off, --relayout and --recompute-text lay everything out again each frame,
// --grid lays the buttons out as a grid rather than rows, --respawn spawns the whole tree again
// each frame, --display-none hides it all, --no-camera spawns no camera, and --many-cameras draws
// each button with a camera of its own.
internal static class ManyButtons
{
    private const float FontSize = 7f;

    // Bevy's Args, read from the command line.
    private static bool _text, _noBorders, _grid, _displayNone, _manyCameras;
    private static int _buttons = 110, _imageFreq = 4;

    private static Entity _root;
    private static readonly List<Entity> Nodes = [], Texts = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        (_text, _noBorders, _grid, _displayNone, _manyCameras) = (
            arguments.Contains("--text"), arguments.Contains("--no-borders"), arguments.Contains("--grid"),
            arguments.Contains("--display-none"), arguments.Contains("--many-cameras"));
        _buttons = Option(arguments, "--buttons", 110);
        _imageFreq = Option(arguments, "--image-freq", 4);
        Nodes.Clear();
        Texts.Clear();

        StressTest.Warn();
        app.Update(SetTextColorsChanged, "many_buttons.SetTextColorsChanged");

        if (!arguments.Contains("--no-camera")) app.Startup(_ => Render2d.SpawnCamera2d(), "many_buttons.Camera");

        if (_manyCameras) app.Startup(SetupManyCameras, "many_buttons.SetupManyCameras");
        else if (_grid) app.Startup(SetupGrid, "many_buttons.SetupGrid");
        else app.Startup(SetupFlex, "many_buttons.SetupFlex");

        if (arguments.Contains("--relayout")) app.Update(Relayout, "many_buttons.Relayout");
        if (arguments.Contains("--recompute-text")) app.Update(RecomputeText, "many_buttons.RecomputeText");
        if (arguments.Contains("--respawn"))
        {
            app.Update(ctx =>
            {
                ctx.Ecs.Despawn(_root);
                Nodes.Clear();
                Texts.Clear();
                if (_grid) SetupGrid(ctx);
                else SetupFlex(ctx);
            }, "many_buttons.Respawn");
        }
    }

    private static int Option(string[] arguments, string name, int fallback)
    {
        var at = Array.IndexOf(arguments, name);
        return at >= 0 && at + 1 < arguments.Length && int.TryParse(arguments[at + 1], out var value) ? value : fallback;
    }

    // Every text's color marked changed each frame, which Bevy does by reaching it mutably, and
    // here by writing it again as it is.
    private static void SetTextColorsChanged(BehaviorContext ctx)
    {
        foreach (var text in Texts)
        {
            var color = ctx.Ecs.Wrap<TextColorRef>(text);
            color.Value = color.Value;
        }
    }

    private static void Relayout(BehaviorContext ctx)
    {
        foreach (var node in Nodes)
        {
            var layout = ctx.Ecs.Wrap<NodeRef>(node);
            layout.Width = layout.Width;
        }
    }

    private static void RecomputeText(BehaviorContext ctx)
    {
        foreach (var text in Texts)
        {
            var written = ctx.Ecs.Wrap<TextRef>(text);
            written.Value = written.Value;
        }
    }

    private static AssetHandle[]? Images() => _imageFreq > 0
        ? [AssetServer.Load(AssetKind.Image, "branding/icon.png"), AssetServer.Load(AssetKind.Image, "textures/Game Icons/wrench.png")]
        : null;

    // Around the color wheel by the row, its lightness high.
    private static (float R, float G, float B, float A) AsRainbow(int i) => Color.FromHsl(i / (float)_buttons * 360f, 0.9f, 0.8f);

    private static void SetupFlex(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var images = Images();
        _root = Node(ecs, new UiSettings
        {
            Display = _displayNone ? UiDisplay.None : UiDisplay.Flex,
            Direction = UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
        });

        for (var column = 0; column < _buttons; column++)
        {
            var row = Node(ecs, new UiSettings());
            ecs.SetParent(row, _root);
            for (var line = 0; line < _buttons; line++)
                ecs.SetParent(SpawnButton(ecs, column, line, images), row);
        }
    }

    private static void SetupGrid(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var images = Images();
        _root = Node(ecs, new UiSettings
        {
            Display = _displayNone ? UiDisplay.None : UiDisplay.Grid,
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
        });
        UiGrid.Set(_root, new GridSettings { Columns = [Track.Flex(1f).Repeated(_buttons)], Rows = [Track.Flex(1f).Repeated(_buttons)] });

        for (var column = 0; column < _buttons; column++)
            for (var line = 0; line < _buttons; line++)
                ecs.SetParent(SpawnButton(ecs, column, line, images), _root);
    }

    // A camera a button, each button in a tree of its own drawn by its camera, placed where the
    // grid would have put it.
    private static void SetupManyCameras(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var images = Images();
        for (var column = 0; column < _buttons; column++)
        {
            for (var line = 0; line < _buttons; line++)
            {
                var camera = Render2d.SpawnCamera2d(column * _buttons + line + 1);
                var root = Node(ecs, new UiSettings
                {
                    Display = _displayNone ? UiDisplay.None : UiDisplay.Flex,
                    Direction = UiDirection.Column,
                    Justify = UiJustify.Center,
                    Align = UiAlign.Center,
                    Width = Length.Percent(100f),
                    Height = Length.Percent(100f),
                    Camera = camera,
                });
                var place = Node(ecs, new UiSettings { Absolute = true });
                var layout = ecs.Wrap<NodeRef>(place);
                (layout.Top, layout.Left) = (new Val.Vh(column * 100f / _buttons), new Val.Vw(line * 100f / _buttons));
                ecs.SetParent(place, root);
                ecs.SetParent(SpawnButton(ecs, column, line, images), place);
            }
        }
    }

    private static Entity Node(EcsWorld ecs, UiSettings settings)
    {
        var node = Ui.SpawnNode(settings);
        Nodes.Add(node);
        return node;
    }

    // A button nine tenths of the window across and down, shared by every button in a row or a
    // column, with a margin of a twentieth of its size, its border half-clear white, an image where
    // one is given and a label of two spans with --text.
    private static Entity SpawnButton(EcsWorld ecs, int column, int line, AssetHandle[]? images)
    {
        var color = AsRainbow(line % Math.Max(column, 1));
        var button = Node(ecs, new UiSettings
        {
            Interactive = true,
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Color = color,
            BorderColor = (1f, 1f, 1f, 0.5f),
        });

        var layout = ecs.Wrap<NodeRef>(button);
        var (width, height) = (90f / _buttons, 90f / _buttons);
        (layout.Width, layout.Height) = (new Val.Vw(width), new Val.Vh(height));
        (layout.MarginLeft, layout.MarginRight) = (new Val.Vw(width * 0.05f), new Val.Vw(width * 0.05f));
        (layout.MarginTop, layout.MarginBottom) = (new Val.Vh(height * 0.05f), new Val.Vh(height * 0.05f));
        if (!_noBorders)
        {
            var border = new Val.VMin(0.05f * 90f / _buttons);
            (layout.BorderLeft, layout.BorderRight, layout.BorderTop, layout.BorderBottom) = (border, border, border, border);
        }

        ecs.Add(button, new IdleColor { Idle = new Vec4(color.R, color.G, color.B, color.A) });

        if (images is not null) Ui.SetImage(button, images[(column + line) / _imageFreq % images.Length]);

        if (_text)
        {
            // Split in two spans, to measure text of many spans.
            var label = Ui.SpawnText($"{column}, ", new UiSettings { Color = Color.FromSrgb(0.5f, 0.2f, 0.2f) }, FontSize);
            Ui.SpawnTextSpan(label, $"{line}", new UiTextSettings { FontSize = FontSize }, Color.FromSrgb(0.2f, 0.2f, 0.5f));
            ecs.SetParent(label, button);
            Texts.Add(label);
        }

        return button;
    }
}

/// <summary>A button's color when the pointer is not over it.</summary>
[Behavior]
public partial struct IdleColor
{
    /// <summary>The color it goes back to, linear RGBA.</summary>
    [Color]
    public Vec4 Idle;

    /// <summary>
    /// Orange red while hovered, and its own color otherwise, each time the pointer's relation to
    /// it changes.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonSystem(BehaviorContext ctx)
    {
        var (r, g, b, a) = Ui.InteractionOf(ctx.Entity) == UiInteraction.Hovered ? Color.FromSrgb8(255, 69, 0) : (Idle.X, Idle.Y, Idle.Z, Idle.W);
        ctx.Ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = new Color(r, g, b, a);
    }
}

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates color grading, a camera's exposure, temperature, tint and hue over the whole
// picture and its saturation, contrast, gamma, gain and lift in the highlights, midtones and
// shadows apart, each chosen with a button and changed with the arrow keys.
internal static class ColorGrading
{
    private const float AdjustmentSpeed = 0.003f;

    private static readonly string[] GlobalOptions = ["Exposure", "Temperature", "Tint", "Hue"];
    private static readonly string[] Sections = ["Highlights", "Midtones", "Shadows"];
    private static readonly string[] SectionOptions = ["Saturation", "Contrast", "Gamma", "Gain", "Lift"];

    // An option, by its section, or none for the global ones, and its name.
    private readonly record struct Option(string? Section, string Name)
    {
        public override string ToString() => Section is null ? $"\"{Name}\"" : $"\"{Name}\" for \"{Section}\"";
    }

    // Each option's field of Bevy's ColorGrading, read and written through its wrapper.
    private static readonly Dictionary<(string? Section, string Name), (Func<ColorGradingRef, float> Get, Action<ColorGradingRef, float> Set)> Fields = new()
    {
        [(null, "Exposure")] = (g => g.GlobalExposure, (g, v) => g.GlobalExposure = v),
        [(null, "Temperature")] = (g => g.GlobalTemperature, (g, v) => g.GlobalTemperature = v),
        [(null, "Tint")] = (g => g.GlobalTint, (g, v) => g.GlobalTint = v),
        [(null, "Hue")] = (g => g.GlobalHue, (g, v) => g.GlobalHue = v),
        [("Highlights", "Saturation")] = (g => g.HighlightsSaturation, (g, v) => g.HighlightsSaturation = v),
        [("Highlights", "Contrast")] = (g => g.HighlightsContrast, (g, v) => g.HighlightsContrast = v),
        [("Highlights", "Gamma")] = (g => g.HighlightsGamma, (g, v) => g.HighlightsGamma = v),
        [("Highlights", "Gain")] = (g => g.HighlightsGain, (g, v) => g.HighlightsGain = v),
        [("Highlights", "Lift")] = (g => g.HighlightsLift, (g, v) => g.HighlightsLift = v),
        [("Midtones", "Saturation")] = (g => g.MidtonesSaturation, (g, v) => g.MidtonesSaturation = v),
        [("Midtones", "Contrast")] = (g => g.MidtonesContrast, (g, v) => g.MidtonesContrast = v),
        [("Midtones", "Gamma")] = (g => g.MidtonesGamma, (g, v) => g.MidtonesGamma = v),
        [("Midtones", "Gain")] = (g => g.MidtonesGain, (g, v) => g.MidtonesGain = v),
        [("Midtones", "Lift")] = (g => g.MidtonesLift, (g, v) => g.MidtonesLift = v),
        [("Shadows", "Saturation")] = (g => g.ShadowsSaturation, (g, v) => g.ShadowsSaturation = v),
        [("Shadows", "Contrast")] = (g => g.ShadowsContrast, (g, v) => g.ShadowsContrast = v),
        [("Shadows", "Gamma")] = (g => g.ShadowsGamma, (g, v) => g.ShadowsGamma = v),
        [("Shadows", "Gain")] = (g => g.ShadowsGain, (g, v) => g.ShadowsGain = v),
        [("Shadows", "Lift")] = (g => g.ShadowsLift, (g, v) => g.ShadowsLift = v),
    };

    private sealed record Widget(Option Option, Entity Button, Entity Label, Entity Value);

    private static readonly List<Widget> Widgets = [];
    private static Entity _camera, _help;
    private static Option _selected;
    private static bool _changed;

    public static void Build(App app)
    {
        Widgets.Clear();
        (_selected, _changed) = (new Option(null, "Exposure"), true);

        app.Startup(Setup, "color_grading.Setup");
        app.SpawnGltf("models/TonemappingTest/TonemappingTest.gltf");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
            ctx.Ecs.Set(root, new Transform(new Vec3(0.5f, 0f, -0.5f), Quat.FromRotationY(-0.15f * MathF.PI), Vec3.One)));

        app.Chain(Stage.Update,
            new SystemDescriptor(world => HandleButtonPresses(), "color_grading.HandleButtonPresses"),
            new SystemDescriptor(world => AdjustOption(new BehaviorContext(world)), "color_grading.AdjustColorGradingOption"),
            new SystemDescriptor(world => UpdateUiState(new BehaviorContext(world)), "color_grading.UpdateUiState"));
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's directional light, turned by its EulerRot::ZYX, with its cascades kept close.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 15_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
        Render.SetShadowCascades(sun, maximum: 3f, firstBound: 0.9f);

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        var small = new UiTextSettings { Font = font, FontSize = 15f };

        var column = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Direction = UiDirection.Column,
            RowGap = Length.Px(6f),
            Left = Length.Px(12f),
            Bottom = Length.Px(12f),
        });

        // The global options first, under a blank as wide as the sections' names, then a row a section.
        var global = Ui.SpawnNode(new UiSettings());
        ecs.SetParent(global, column);
        ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Px(125f) }), global);
        foreach (var name in GlobalOptions) Button(ecs, global, new Option(null, name), small);

        foreach (var section in Sections)
        {
            var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center });
            ecs.SetParent(row, column);
            ecs.SetParent(Ui.SpawnText(section, new UiSettings { Width = Length.Px(125f) }, small), row);
            foreach (var name in SectionOptions) Button(ecs, row, new Option(section, name), small);
        }

        _help = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Left = Length.Px(12f), Top = Length.Px(12f) }, new UiTextSettings { Font = font });

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
        ecs.Insert<ColorGradingRef>(_camera);

        var fogColor = Scene.Srgb8(43, 44, 47);
        var fog = ecs.Insert<DistanceFogRef>(_camera);
        fog.Color = new Color(fogColor.R, fogColor.G, fogColor.B, fogColor.A);
        fog.Falloff = new FogFalloff.Linear(1f, 8f);

        Render.SetEnvironmentMap(
            _camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            2000f);
    }

    // A pill of a button with the option's name on its left and its value on its right.
    private static void Button(EcsWorld ecs, Entity row, Option option, UiTextSettings style)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Border = Sides.All(Length.Px(1f)),
            BorderColor = (1f, 1f, 1f, 1f),
            Color = (0f, 0f, 0f, 1f),
            Width = Length.Px(200f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Padding = new Sides(Length.Px(12f), Length.Px(6f), Length.Px(12f), Length.Px(6f)),
            Margin = new Sides(Length.Zero, Length.Zero, Length.Px(12f), Length.Zero),
            Corners = Corners.All(Length.Px(float.MaxValue)),
        });
        ecs.SetParent(button, row);

        var label = Ui.SpawnText(option.Name, new UiSettings(), style);
        ecs.SetParent(label, button);
        ecs.SetParent(Ui.SpawnNode(new UiSettings { Grow = 1f }), button);
        var value = Ui.SpawnText(Format(DefaultOf(option)), new UiSettings(), style);
        ecs.SetParent(value, button);

        Widgets.Add(new Widget(option, button, label, value));
    }

    // Bevy's ColorGrading::default, which the buttons show before the camera has one to read.
    private static float DefaultOf(Option option) => option.Name switch
    {
        "Saturation" or "Contrast" or "Gamma" or "Gain" => 1f,
        _ => 0f,
    };

    private static void HandleButtonPresses()
    {
        foreach (var widget in Widgets)
        {
            if (Ui.InteractionOf(widget.Button) != UiInteraction.Pressed || widget.Option == _selected) continue;
            (_selected, _changed) = (widget.Option, true);
        }
    }

    private static void AdjustOption(BehaviorContext ctx)
    {
        var delta = 0f;
        if (ctx.Input.KeyDown(Key.ArrowLeft)) delta -= AdjustmentSpeed;
        if (ctx.Input.KeyDown(Key.ArrowRight)) delta += AdjustmentSpeed;
        if (delta == 0f) return;

        Fields[(_selected.Section, _selected.Name)].Set(ctx.Ecs.Wrap<ColorGradingRef>(_camera), Value(ctx.Ecs, _selected) + delta);
        _changed = true;
    }

    private static void UpdateUiState(BehaviorContext ctx)
    {
        if (!_changed) return;
        _changed = false;

        var ecs = ctx.Ecs;
        foreach (var widget in Widgets)
        {
            var chosen = widget.Option == _selected;
            ecs.Wrap<BackgroundColorRef>(widget.Button).Value = chosen ? Color.White : Color.Black;
            var border = ecs.Wrap<BorderColorRef>(widget.Button);
            (border.Top, border.Right, border.Bottom, border.Left) = chosen ? (Color.Black, Color.Black, Color.Black, Color.Black) : (Color.White, Color.White, Color.White, Color.White);

            foreach (var text in new[] { widget.Label, widget.Value })
                ecs.Wrap<TextColorRef>(text).Value = chosen ? Color.Black : Color.White;
            if (chosen) Ui.SetText(widget.Value, Format(Value(ecs, widget.Option)));
        }

        Ui.SetText(_help, HelpText());
    }

    private static float Value(EcsWorld ecs, Option option) =>
        ecs.Get<ColorGradingRef>(_camera) is { } grading ? Fields[(option.Section, option.Name)].Get(grading) : DefaultOf(option);

    private static string HelpText() => $"Press Left/Right to adjust {_selected}";

    private static string Format(float value) => value.ToString("0.000", CultureInfo.InvariantCulture);

}

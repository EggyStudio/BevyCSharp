// Bevy's tonemapping example, examples/3d/tonemapping.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Tonemapping = Bevy.Reflected.TonemappingRef.ValueVariant;

// Compares Bevy's tonemapping methods on a basic scene, a sweep of colors far brighter than white,
// and an image dropped on the window, each method with color grading of its own changed with the
// arrow keys.
internal static class TonemappingExample
{
    // One method's grading: exposure, gamma, saturation before tonemapping and after it.
    private sealed record Grading(float Exposure = 0f, float Gamma = 1f, float PreSaturation = 1f, float PostSaturation = 1f);

    private static readonly Tonemapping[] Methods =
    [
        Tonemapping.None, Tonemapping.Reinhard, Tonemapping.ReinhardLuminance, Tonemapping.AcesFitted, Tonemapping.AgX,
        Tonemapping.SomewhatBoringDisplayTransform, Tonemapping.TonyMcMapface, Tonemapping.BlenderFilmic, Tonemapping.KhronosPbrNeutral,
    ];

    private static readonly Dictionary<Tonemapping, Grading> PerMethod = [];
    private static readonly Dictionary<int, List<Entity>> Scenes = [];
    private static Entity _camera, _text, _viewer, _dropText;
    private static AssetHandle _viewerMaterial;
    private static Tonemapping _method;
    private static int _scene, _selected;
    private static bool _hideUi;

    private static readonly Transform CameraAt = Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY);

    public static void Build(App app)
    {
        PerMethod.Clear();
        Scenes.Clear();
        foreach (var method in Methods) PerMethod[method] = Recommended(method);
        (_method, _scene, _selected, _hideUi) = (Tonemapping.TonyMcMapface, 1, 0, false);

        app.Startup(Setup, "tonemapping.Setup");
        app.SpawnGltf("models/TonemappingTest/TonemappingTest.gltf", (_, root) => InScene(1, root));
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(new Vec3(0.5f, 0f, -0.5f), Quat.FromRotationY(-0.15f * MathF.PI), Vec3.One));
            InScene(1, root);
        });

        app.Update(DragDropImage, "tonemapping.DragDropImage");
        app.Update(ToggleScene, "tonemapping.ToggleScene");
        app.Update(ToggleTonemappingMethod, "tonemapping.ToggleTonemappingMethod");
        app.Update(UpdateColorGrading, "tonemapping.UpdateColorGradingSettings");
        app.Update(UpdateUi, "tonemapping.UpdateUi");
    }

    // Bevy's basic_scene_recommendation, the grading each method looks right with on the basic scene.
    private static Grading Recommended(Tonemapping method) => method switch
    {
        Tonemapping.Reinhard or Tonemapping.ReinhardLuminance => new Grading(Exposure: 0.5f),
        Tonemapping.AcesFitted => new Grading(Exposure: 0.35f),
        Tonemapping.AgX => new Grading(Exposure: -0.2f, PreSaturation: 1.1f, PostSaturation: 1.1f),
        _ => new Grading(),
    };

    private static void InScene(int number, Entity entity)
    {
        if (!Scenes.TryGetValue(number, out var list)) Scenes[number] = list = [];
        list.Add(entity);
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        _camera = ecs.Camera(CameraAt);
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
        ApplyGrading(ecs);

        _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        // The basic scene's sun, turned by Bevy's EulerRot::ZYX, with its cascades kept close.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 15_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
        Render.SetShadowCascades(sun, maximum: 3f, firstBound: 0.9f);
        InScene(1, sun);

        // A square a step in front of the camera, painted by the test pattern shader.
        var inFront = CameraAt with { Translation = CameraAt.Translation + CameraAt.Rotation * -Vec3.UnitZ };
        var sweep = ecs.Spawn();
        ecs.Add(sweep, inFront);
        Render.SetMesh(ecs, sweep, Render.CreateMesh(MeshShape.Rectangle, 0.7f, 0.7f));
        Render.SetMaterial(ecs, sweep, Shaders.CreateMaterial(Shaders.CreateProgram("shaders/tonemapping_test_patterns.slang")));
        ecs.Insert<VisibilityRef>(sweep).Value = VisibilityRef.ValueVariant.Hidden;
        InScene(2, sweep);

        // An unlit square for an image dropped on the window, and the line asking for one.
        _viewerMaterial = Render.CreateMaterial(new MaterialSettings { Unlit = true });
        _viewer = ecs.Mesh(Render.CreateMesh(MeshShape.Rectangle, 1f, 1f), _viewerMaterial, inFront);
        ecs.Insert<VisibilityRef>(_viewer).Value = VisibilityRef.ValueVariant.Hidden;
        InScene(3, _viewer);

        _dropText = Ui.SpawnText("Drag and drop an HDR or EXR file",
            new UiSettings { AlignSelf = UiAlignSelf.Center, Margin = Sides.All(Length.Auto), Color = (0f, 0f, 0f, 1f) },
            new UiTextSettings { FontSize = 36f, Justify = TextJustify.Center });
        ecs.Insert<VisibilityRef>(_dropText).Value = VisibilityRef.ValueVariant.Hidden;
        InScene(3, _dropText);
    }

    private static void DragDropImage(BehaviorContext ctx)
    {
        foreach (var dropped in ctx.Read<FileDropped>())
        {
            Render.WriteMaterial(_viewerMaterial, new MaterialSettings { Unlit = true, BaseColorTexture = AssetServer.Load(AssetKind.Image, dropped.Path) });
            if (_dropText != Entity.None && ctx.Ecs.IsAlive(_dropText)) ctx.Ecs.Despawn(_dropText);
            _dropText = Entity.None;
        }
    }

    private static void ToggleScene(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var pressed = input.KeyPressed(Key.Q) ? 1 : input.KeyPressed(Key.W) ? 2 : input.KeyPressed(Key.E) ? 3 : 0;
        if (pressed == 0) return;

        _scene = pressed;
        foreach (var (number, entities) in Scenes)
            foreach (var entity in entities)
                if (ctx.Ecs.IsAlive(entity))
                    ctx.Ecs.Wrap<VisibilityRef>(entity).Value = number == pressed ? VisibilityRef.ValueVariant.Visible : VisibilityRef.ValueVariant.Hidden;
    }

    private static void ToggleTonemappingMethod(BehaviorContext ctx)
    {
        var keys = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        for (var i = 0; i < keys.Length; i++)
        {
            if (!ctx.Input.KeyPressed(keys[i])) continue;
            _method = Methods[i];
            ctx.Ecs.Wrap<TonemappingRef>(_camera).Value = _method;
        }
        ApplyGrading(ctx.Ecs);
    }

    private static void UpdateColorGrading(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var grading = PerMethod[_method];
        var dt = ctx.Time.Delta * 0.25f;
        if (input.KeyDown(Key.ArrowLeft)) dt = -dt;

        if (input.KeyPressed(Key.ArrowDown)) _selected = (_selected + 1) % 4;
        if (input.KeyPressed(Key.ArrowUp)) _selected = (_selected + 3) % 4;

        if (input.KeyDown(Key.ArrowLeft) || input.KeyDown(Key.ArrowRight))
        {
            PerMethod[_method] = _selected switch
            {
                0 => grading with { Exposure = grading.Exposure + dt },
                1 => grading with { Gamma = grading.Gamma + dt },
                2 => grading with { PreSaturation = grading.PreSaturation + dt },
                _ => grading with { PostSaturation = grading.PostSaturation + dt },
            };
        }

        if (input.KeyPressed(Key.Space))
            foreach (var method in Methods) PerMethod[method] = new Grading();
        if (input.KeyPressed(Key.Enter) && _scene == 1)
            foreach (var method in Methods) PerMethod[method] = Recommended(method);
    }

    // The chosen method's grading on the camera, gamma and saturation alike in every section.
    private static void ApplyGrading(EcsWorld ecs)
    {
        var grading = PerMethod[_method];
        var camera = ecs.Wrap<ColorGradingRef>(_camera);
        (camera.GlobalExposure, camera.GlobalPostSaturation) = (grading.Exposure, grading.PostSaturation);
        (camera.ShadowsGamma, camera.MidtonesGamma, camera.HighlightsGamma) = (grading.Gamma, grading.Gamma, grading.Gamma);
        (camera.ShadowsSaturation, camera.MidtonesSaturation, camera.HighlightsSaturation) = (grading.PreSaturation, grading.PreSaturation, grading.PreSaturation);
    }

    private static void UpdateUi(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.H)) _hideUi = !_hideUi;
        if (_hideUi)
        {
            Ui.SetText(_text, string.Empty);
            return;
        }

        var grading = PerMethod[_method];
        var text = new StringBuilder("(H) Hide UI\n\nTest Scene: \n");
        text.Append($"(Q) {(_scene == 1 ? ">" : "")} Basic Scene\n");
        text.Append($"(W) {(_scene == 2 ? ">" : "")} Color Sweep\n");
        text.Append($"(E) {(_scene == 3 ? ">" : "")} Image Viewer\n");
        text.Append("\n\nTonemapping Method:\n");

        string[] names = ["Disabled", "Reinhard", "Reinhard Luminance", "ACES Fitted", "AgX", "SomewhatBoringDisplayTransform", "TonyMcMapface", "Blender Filmic", "Khronos PBR Neutral"];
        for (var i = 0; i < Methods.Length; i++)
        {
            // Bevy's own line for Reinhard marks it with a space after the arrow, kept as it is.
            var mark = _method == Methods[i] ? (Methods[i] == Tonemapping.Reinhard ? "> " : ">") : "";
            text.Append($"({i + 1}) {mark} {names[i]}\n");
        }

        text.Append("\n\nColor Grading:\n(arrow keys)\n");
        string[] parameters = ["Exposure", "Gamma", "PreSaturation", "PostSaturation"];
        float[] values = [grading.Exposure, grading.Gamma, grading.PreSaturation, grading.PostSaturation];
        for (var i = 0; i < parameters.Length; i++)
        {
            if (_selected == i) text.Append("> ");
            text.Append(FormattableString.Invariant($"{parameters[i]}: {values[i]:0.00}\n"));
        }

        text.Append("(Space) Reset all to default\n");
        if (_scene == 1) text.Append("(Enter) Reset all to scene recommendation\n");
        Ui.SetText(_text, text.ToString());
    }
}

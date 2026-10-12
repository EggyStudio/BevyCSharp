// Bevy's solari_reflections example, examples/3d/solari_reflections.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// A test bed for Solari's reflections, a room lit by a lamp in its ceiling, holding a panel and a
// sphere of a test material, two mirrors facing each other, a cube going round and the flight
// helmet. Space pauses the objects and the arrows scrub them back and forth, 1 makes the test
// material metal or not, 3 to 7 choose its roughness from mirror to diffuse and the slider sets any
// between, 8 and 9 pan and slide the right mirror, and P draws with the reference path tracer in
// the realtime lighting's place, which starts over each time anything changes. The camera flies as
// Bevy's free camera does where its crate is compiled in, and Bevy's 2, DLSS's ray reconstruction,
// says the app was not built with it, as Bevy's does when it was not.
//
// Needs a bridge built with --solari and a GPU with ray queries.
internal static class SolariReflections
{
    private static readonly Color MetallicBaseColor = Color.FromSrgb(0.95f, 0.95f, 0.97f);
    private static readonly Color DielectricBaseColor = Color.FromSrgb(0.15f, 0.28f, 0.55f);

    private const float DeltaRoughness = 0.0314f;
    private const float GuideRoughness = 0.25f;

    internal static readonly (Key Key, string Name, float Roughness)[] MaterialPresets =
    [
        (Key.Digit3, "Mirror", 0f),
        (Key.Digit4, "Near-mirror", DeltaRoughness),
        (Key.Digit5, "Glossy", 0.12f),
        (Key.Digit6, "Satin", GuideRoughness - 0.01f),
        (Key.Digit7, "Diffuse", 1f),
    ];

    private const float PhaseRate = 0.8f;
    private const float MirrorYaw = 0.22f;
    private const float MirrorPitch = -0.16f;
    private const float MirrorPanRange = 0.55f;
    private const float MirrorBaseX = 2.4f;
    private const float MirrorSlideRange = 1.5f;

    private static readonly Color SliderTrack = Color.FromSrgb(0.45f, 0.45f, 0.48f);
    private static readonly Color SliderThumb = Color.FromSrgb(0.45f, 0.95f, 0.45f);
    private static readonly Color SliderTick = Color.FromSrgb(0.95f, 0.78f, 0.25f);
    private const float SliderWidth = 180f;
    private const float SliderThumbSize = 14f;
    private const float SliderTrackHeight = 8f;

    private static readonly Color HeadingColor = Color.FromSrgb(0.72f, 0.83f, 1f);
    private static readonly Color KeyColor = Color.FromSrgb(0.95f, 0.82f, 0.4f);
    private static readonly Color LabelColor = Color.FromSrgb(0.92f, 0.92f, 0.92f);
    private const float HeadingSize = 10f;
    private const float LabelSize = 11f;
    private const float KeyWidth = 42f;
    private const float KeyGap = 8f;

    // A plane Bevy builds facing up turned to face along each axis, as its Plane3d is given a normal.
    private static readonly Quat FacingZ = Quat.FromRotationX(MathF.PI / 2f);
    private static readonly Quat FacingNegativeZ = Quat.FromRotationX(-MathF.PI / 2f);
    private static readonly Quat FacingX = Quat.FromRotationZ(-MathF.PI / 2f);
    private static readonly Quat FacingNegativeX = Quat.FromRotationZ(MathF.PI / 2f);
    private static readonly Quat FacingDown = Quat.FromRotationX(MathF.PI);

    // Bevy's DemoState, as it stands and as it stood a frame ago, a change between them starting the
    // path tracer over as Bevy's change detection does.
    internal static State Now;
    private static State _before;

    internal readonly record struct State(bool Metallic, float Roughness, bool Paused, float Phase, bool PanMirror, bool SlideMirror);

    // Bevy's TestMaterial, and what the example keeps of its entities where Bevy queries for them.
    private static AssetHandle _testMaterial;
    private static Entity _camera, _orbiting, _sliding, _mirror, _slider;
    internal static bool PathTracing;

    // The roughness the label and the thumb last showed, none at first so the first frame writes them.
    private static float _shown;
    private static readonly List<Entity> Helmets = [];
    private static readonly HashSet<Entity> Traced = [];

    public static void Configure(Config config)
    {
        config.RayTracedLighting = true;
        config.PathTracer = true;
        config.GpuTimings = true;
    }

    public static void Build(App app)
    {
        Now = _before = new State(true, MaterialPresets[0].Roughness, false, 0f, false, false);
        (PathTracing, _shown) = (false, float.NaN);
        Helmets.Clear();
        Traced.Clear();

        app.Startup(Setup, "solari_reflections.Setup");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(new Vec3(4.8f, 0f, -1.6f), Quat.FromRotationY(-0.4f), new Vec3(4f)));
            Helmets.Add(root);
        });

        // Bevy's Update systems in one, since the path tracer is told to start over after the
        // objects have moved.
        app.Update(ctx =>
        {
            AddRaytracingMeshes(ctx);
            PauseScene(ctx);
            ToggleMetallic(ctx);
            TogglePathtracer(ctx);
            ToggleMirrorMotion(ctx);
            SelectPreset(ctx);
            UpdateRoughness(ctx);
            MoveObjects(ctx);
            ResetPathtracer(ctx);
        }, "solari_reflections.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        _testMaterial = Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = MetallicBaseColor,
            Metallic = 1f,
            Roughness = Now.Roughness,
        });

        var mirror = Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = Color.FromSrgb(0.95f, 0.95f, 0.97f),
            Metallic = 1f,
            Roughness = 0f,
        });

        SpawnRoom(ecs);

        _mirror = SpawnTraced(ecs, Plane(2f, 1.5f), _testMaterial, new Transform(new Vec3(MirrorBaseX, 1.7f, -3f), MirrorRotation(0f) * FacingZ, Vec3.One));
        ecs.Add(_mirror, new PanningMirror());

        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.6f, 0.6f, 0.6f);
        _orbiting = SpawnTraced(ecs, cube, Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.9f, 0.15f, 0.05f), Roughness = 0.6f }), Transform.At(3.5f, 1.5f, 2.5f));
        ecs.Add(_orbiting, new OrbitingObject());

        _sliding = SpawnTraced(ecs, Render.CreateMesh(MeshShape.Sphere, 0.8f), _testMaterial, Transform.At(0f, 0.9f, 3.6f));
        ecs.Add(_sliding, new SlidingObject());

        var nested = Plane(1.3f, 1.9f);
        foreach (var (x, yaw) in new[] { (-5.2f, 1.04f), (-1.8f, -1.04f) })
            SpawnTraced(ecs, nested, mirror, new Transform(new Vec3(x, 2f, -3.5f), Quat.FromRotationY(yaw) * FacingZ, Vec3.One));

        SpawnTraced(ecs, cube, Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.95f, 0.75f, 0.1f), Roughness = 0.6f }), new Transform(new Vec3(-3.5f, 2f, -4f), Quat.Identity, new Vec3(0.6f)));

        // Bevy's indoor exposure, and its free camera walking at three and running at ten, which the
        // examples' camera runs at three times its walk.
        _camera = ecs.SpawnCamera3d(
            Transform.LookingAt(new Vec3(0f, 2.8f, 9.5f), new Vec3(0f, 1.6f, -1f), Vec3.UnitY),
            new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
        ecs.Add(_camera, new FreeCamera { Speed = 3f });
        Render.SetExposure(_camera, 7f);
        if (Render.RayTracingActive) Render.SetRayTracedLighting(_camera, true);

        SpawnUi(ecs);
    }

    private static void SpawnRoom(EcsWorld ecs)
    {
        // The floor, its texture coordinates scaled nine times so the checker repeats, since the
        // rays read a mesh's coordinates and not a material's scale of them.
        var floor = Render.CreateMesh(new MeshData
        {
            Positions = [new Vec3(-9f, 0f, -10.5f), new Vec3(9f, 0f, -10.5f), new Vec3(-9f, 0f, 10.5f), new Vec3(9f, 0f, 10.5f)],
            Normals = [Vec3.UnitY, Vec3.UnitY, Vec3.UnitY, Vec3.UnitY],
            Uvs = [0f, 0f, 9f, 0f, 0f, 9f, 9f, 9f],
            Indices = [3, 1, 2, 0, 2, 1],
        });
        var checker = AssetServer.LoadImage("textures/uv_checker_bw.png", new TextureSettings { Wrap = TextureWrap.Repeat });
        SpawnTraced(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = checker, Roughness = 0.9f }), Transform.At(0f, 0f, 1.5f));

        var neutral = Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.55f, 0.54f, 0.52f), Roughness = 0.9f });

        var endWall = Plane(9f, 4f);
        foreach (var (z, facing) in new[] { (-9f, FacingZ), (12f, FacingNegativeZ) })
            SpawnTraced(ecs, endWall, neutral, new Transform(new Vec3(0f, 4f, z), facing, Vec3.One));

        var sideWall = Plane(4f, 10.5f);
        foreach (var (x, facing, tint) in new[] { (-9f, FacingX, Color.FromSrgb(0.6f, 0.25f, 0.22f)), (9f, FacingNegativeX, Color.FromSrgb(0.22f, 0.3f, 0.6f)) })
            SpawnTraced(ecs, sideWall, Render.CreateMaterial(new MaterialSettings { BaseColor = tint, Roughness = 0.9f }), new Transform(new Vec3(x, 4f, 1.5f), facing, Vec3.One));

        SpawnTraced(ecs, Plane(10.5f, 10.5f), neutral, new Transform(new Vec3(0f, 8f, 1.5f), FacingDown, Vec3.One));

        var lamp = Render.CreateMaterial(new MaterialSettings { Emissive = (40_000f, 38_000f, 34_000f, 1f) });
        SpawnTraced(ecs, Plane(1.75f, 1.75f), lamp, new Transform(new Vec3(0f, 7.8f, 0.5f), FacingDown, Vec3.One));
    }

    // Bevy's Plane3d of the half size given, built facing up and turned by its entity.
    private static AssetHandle Plane(float halfWidth, float halfDepth) => Render.CreateMesh(MeshShape.Plane, halfWidth * 2f, halfDepth * 2f);

    // An entity drawing a mesh the rays meet as well, as Bevy's RaytracingMesh3d beside its Mesh3d.
    private static Entity SpawnTraced(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Transform transform)
    {
        var entity = ecs.SpawnMesh(mesh, material, transform);
        if (Render.RayTracingActive) Render.SetRayTraced(entity, mesh);
        return entity;
    }

    // The panel in the bottom corner, its rows written each frame by their behaviors, and the
    // passes' times in the top corner.
    private static void SpawnUi(EcsWorld ecs)
    {
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Bottom = Length.Px(12f),
            Left = Length.Px(12f),
            Direction = UiDirection.Column,
            Align = UiAlign.FlexStart,
            Padding = Sides.All(Length.Px(10f)),
            Corners = Corners.All(Length.Px(4f)),
            Color = (0f, 0f, 0f, 0.92f),
        });

        void Add(Entity child) => ecs.SetParent(child, panel);

        Add(Heading("PLAYBACK"));
        Add(Row(ecs, "Space", ControlRowKind.Pause));
        Add(StaticRow(ecs, "< >", "Scrub objects"));
        Add(Heading("TEST MATERIAL"));
        Add(Row(ecs, "1", ControlRowKind.Metallic));
        Add(Row(ecs, "3-7", ControlRowKind.Presets));

        var roughness = Ui.SpawnNode(new UiSettings
        {
            Align = UiAlign.Center,
            ColumnGap = Length.Px(KeyGap),
            Margin = new Sides(Length.Px(KeyWidth + KeyGap), Length.Zero, Length.Zero, Length.Zero),
        });
        var value = Label(string.Empty);
        ecs.Add(value, new RoughnessValueText());
        ecs.SetParent(value, roughness);
        _slider = RoughnessSlider(ecs);
        ecs.SetParent(_slider, roughness);
        Add(roughness);

        Add(Heading("RIGHT MIRROR"));
        Add(Row(ecs, "8", ControlRowKind.MirrorPan));
        Add(Row(ecs, "9", ControlRowKind.MirrorSlide));
        Add(Heading("RENDERER"));
        Add(Row(ecs, "P", ControlRowKind.Renderer));
        Add(Row(ecs, "2", ControlRowKind.Denoising));

        var corner = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Right = Length.Px(0f),
            Padding = Sides.All(Length.Px(4f)),
            Corners = new Corners(Length.Zero, Length.Zero, Length.Zero, Length.Px(4f)),
            Color = Color.FromSrgb(0.1f, 0.1f, 0.1f, 0.8f),
        });
        var performance = Ui.SpawnText(string.Empty, new UiSettings(), new UiTextSettings { FontSize = 8f });
        ecs.Add(performance, new ReflectionTimings());
        ecs.SetParent(performance, corner);
    }

    private static Entity Heading(string title) =>
        Ui.SpawnText(title, new UiSettings
        {
            Color = HeadingColor,
            Margin = new Sides(Length.Zero, Length.Px(7f), Length.Zero, Length.Px(3f)),
        }, new UiTextSettings { FontSize = HeadingSize });

    private static Entity Label(string text) =>
        Ui.SpawnText(text, new UiSettings { Color = LabelColor }, new UiTextSettings { FontSize = LabelSize });

    private static Entity Row(EcsWorld ecs, string key, ControlRowKind kind)
    {
        var label = Label(string.Empty);
        ecs.Add(label, new ControlRow { Kind = kind });
        return RowOf(ecs, key, label);
    }

    private static Entity StaticRow(EcsWorld ecs, string key, string text) => RowOf(ecs, key, Label(text));

    private static Entity RowOf(EcsWorld ecs, string key, Entity label)
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(KeyGap) });

        var keyLabel = Ui.SpawnNode(new UiSettings { Width = Length.Px(KeyWidth), Justify = UiJustify.FlexEnd });
        ecs.SetParent(Ui.SpawnText(key, new UiSettings { Color = KeyColor }, new UiTextSettings { FontSize = LabelSize }), keyLabel);
        ecs.SetParent(keyLabel, row);
        ecs.SetParent(label, row);
        return row;
    }

    // Bevy's roughness_slider, a track, two ticks where Solari's handling of roughness changes, and
    // a thumb, its travel inset by the thumb's width so it stops at the track's end.
    private static Entity RoughnessSlider(EcsWorld ecs)
    {
        var slider = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(SliderWidth),
            Height = Length.Px(SliderThumbSize),
            Direction = UiDirection.Column,
            Align = UiAlign.Stretch,
            Justify = UiJustify.Center,
        });
        ecs.Insert<SliderRef>(slider).TrackClick = SliderRef.TrackClickVariant.Snap;
        ecs.Insert<SliderValueRef>(slider).Value = Now.Roughness;
        var range = ecs.Insert<SliderRangeRef>(slider);
        (range.Start, range.End) = (0f, 1f);
        Ui.SelfUpdate(slider, UiWidgetKind.Slider);

        ecs.SetParent(Ui.SpawnNode(new UiSettings
        {
            Height = Length.Px(SliderTrackHeight),
            Corners = Corners.All(Length.Px(SliderTrackHeight / 2f)),
            Color = SliderTrack,
        }), slider);

        var travel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(0f),
            Right = Length.Px(SliderThumbSize),
            Top = Length.Px(0f),
            Bottom = Length.Px(0f),
        });
        ecs.SetParent(travel, slider);

        foreach (var at in new[] { DeltaRoughness, GuideRoughness })
        {
            var tick = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Left = Length.Percent(at * 100f),
                Width = Length.Px(SliderThumbSize),
                Height = Length.Px(SliderThumbSize),
                Justify = UiJustify.Center,
            });
            ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Px(2f), Height = Length.Px(SliderThumbSize), Color = SliderTick }), tick);
            ecs.SetParent(tick, travel);
        }

        var thumb = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Width = Length.Px(SliderThumbSize),
            Height = Length.Px(SliderThumbSize),
            Left = Length.Percent(Now.Roughness * 100f),
            Corners = Corners.All(Length.Px(1_000_000f)),
            Color = SliderThumb,
        });
        ecs.Insert<SliderThumbRef>(thumb);
        ecs.Add(thumb, new RoughnessSliderThumb());
        ecs.SetParent(thumb, travel);
        return slider;
    }

    // Each mesh of the helmet is given to the rays once it has loaded, as Bevy's observer does
    // when its scene is ready, and the bridge gives one without texture coordinates or tangents
    // coordinates and tangents of zero, as Bevy's does.
    private static void AddRaytracingMeshes(BehaviorContext ctx)
    {
        if (!Render.RayTracingActive) return;
        var ecs = ctx.Ecs;

        foreach (var root in Helmets)
        {
            foreach (var entity in ecs.Descendants(root))
            {
                if (Traced.Contains(entity)) continue;
                var mesh = Render.MeshOf(ecs, entity);
                if (mesh == AssetHandle.None || AssetServer.StateOf(mesh) != AssetLoadState.Loaded) continue;

                Render.SetRayTraced(entity, mesh);
                Traced.Add(entity);
            }
        }
    }

    private static void PauseScene(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Space)) Now = Now with { Paused = !Now.Paused };
    }

    private static void ToggleMetallic(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Digit1)) return;

        Now = Now with { Metallic = !Now.Metallic };
        if (!Render.TryReadMaterial(_testMaterial, out var settings)) return;

        (settings.Metallic, settings.BaseColor) = Now.Metallic ? (1f, MetallicBaseColor) : (0f, DielectricBaseColor);
        Render.WriteMaterial(_testMaterial, settings);
    }

    private static void ToggleMirrorMotion(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Digit8)) Now = Now with { PanMirror = !Now.PanMirror };
        if (ctx.Input.KeyPressed(Key.Digit9)) Now = Now with { SlideMirror = !Now.SlideMirror };
    }

    // Bevy's toggle_pathtracer, the camera drawn by the path tracer in the realtime lighting's place
    // or the other way round.
    private static void TogglePathtracer(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.P) || !Render.RayTracingActive) return;

        PathTracing = !PathTracing;
        if (PathTracing) Render.SetPathTraced(_camera, true);
        else Render.SetRayTracedLighting(_camera, true);
    }

    // Bevy's reset_pathtracer, the gathering started over in a frame the state changed.
    private static void ResetPathtracer(BehaviorContext ctx)
    {
        if (PathTracing && Now != _before && ctx.Ecs.Get<PathtracerRef>(_camera) is { } pathtracer) pathtracer.Reset = true;
        _before = Now;
    }

    // A preset puts its roughness on the slider, which the next step puts on the material.
    private static void SelectPreset(BehaviorContext ctx)
    {
        foreach (var (key, _, roughness) in MaterialPresets)
        {
            if (ctx.Input.KeyPressed(key)) ctx.Ecs.Wrap<SliderValueRef>(_slider).Value = roughness;
        }
    }

    // Bevy's update_roughness, the slider's value, moved or chosen, put on the test material, its
    // thumb and its label. Bevy's runs when the value changes, and here the value is compared.
    private static void UpdateRoughness(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var value = ecs.Wrap<SliderValueRef>(_slider).Value;
        if (value == _shown) return;

        _shown = value;
        Now = Now with { Roughness = value };
        if (Render.TryReadMaterial(_testMaterial, out var settings))
        {
            settings.Roughness = value;
            Render.WriteMaterial(_testMaterial, settings);
        }

        foreach (var label in ecs.EntitiesWith<RoughnessValueText>())
            Ui.SetText(label, FormattableString.Invariant($"Roughness {value:0.00}"));

        var range = ecs.Wrap<SliderRangeRef>(_slider);
        var position = Math.Clamp((value - range.Start) / (range.End - range.Start), 0f, 1f);
        foreach (var thumb in ecs.EntitiesWith<RoughnessSliderThumb>())
            ecs.Wrap<NodeRef>(thumb).Left = new Val.Percent(position * 100f);
    }

    private static Quat MirrorRotation(float pan) => Quat.FromRotationY(MirrorYaw + pan) * Quat.FromRotationX(MirrorPitch);

    // Bevy's move_objects, the phase moving on at its rate unless paused, and scrubbed by the
    // arrows, and the three objects placed by it.
    private static void MoveObjects(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var scrub = (input.KeyDown(Key.ArrowRight) ? 1f : 0f) - (input.KeyDown(Key.ArrowLeft) ? 1f : 0f);
        var playback = Now.Paused ? 0f : 1f;
        var delta = (playback + scrub) * ctx.Time.Delta * PhaseRate;
        if (delta != 0f) Now = Now with { Phase = Now.Phase + delta };

        var phase = Now.Phase;
        ecs.Set(_orbiting, new Transform(
            new Vec3(3.5f + MathF.Sin(phase) * 2.8f, 1.5f, 0.5f + MathF.Cos(phase) * 2.6f),
            Quat.FromRotationY(phase * 0.7f),
            Vec3.One));

        var sliding = ecs.GetOrDefault<Transform>(_sliding);
        sliding.Translation = new Vec3(MathF.Sin(phase * 0.7f) * 5.5f, sliding.Translation.Y, sliding.Translation.Z);
        ecs.Set(_sliding, sliding);

        var pan = Now.PanMirror ? MathF.Sin(phase * 1.1f) * MirrorPanRange : 0f;
        var mirror = ecs.GetOrDefault<Transform>(_mirror);
        mirror.Rotation = MirrorRotation(pan) * FacingZ;
        mirror.Translation = new Vec3(Now.SlideMirror ? MirrorBaseX + MathF.Sin(phase * 0.9f) * MirrorSlideRange : MirrorBaseX, mirror.Translation.Y, mirror.Translation.Z);
        ecs.Set(_mirror, mirror);
    }

    // Bevy's update_control_text, one row's text as things stand.
    internal static string ControlText(ControlRowKind kind) => kind switch
    {
        ControlRowKind.Pause => Now.Paused ? "Resume" : "Pause",
        ControlRowKind.Metallic => Now.Metallic ? "Switch to dielectric" : "Switch to metallic",
        ControlRowKind.Presets => string.Concat(MaterialPresets.Select(preset =>
            MathF.Abs(preset.Roughness - Now.Roughness) < 1e-4f ? $"[{preset.Name}] " : $"{preset.Name} ")),
        ControlRowKind.MirrorPan => Now.PanMirror ? "Stop panning" : "Pan back and forth",
        ControlRowKind.MirrorSlide => Now.SlideMirror ? "Stop sliding" : "Slide back and forth",
        ControlRowKind.Renderer => PathTracing
            ? "Switch to realtime lighting  -  pause to let the pathtracer converge"
            : "Switch to reference pathtracer",
        _ => "App not compiled with DLSS support",
    };

    // Bevy's update_performance_text, each of Solari's passes as the GPU timed it, smoothed over the
    // last frames as Bevy reads them from its store of diagnostics, or that the path tracer is not
    // timed.
    internal static string Performance()
    {
        if (PathTracing) return "Pathtracer (untimed)";

        var performance = new StringBuilder();
        var total = 0.0;
        foreach (var (label, path) in new[]
        {
            ("Light tiles", "render/solari_lighting/presample_light_tiles/elapsed_gpu"),
            ("World cache", "render/solari_lighting/world_cache/elapsed_gpu"),
            ("Lighting", "render/solari_lighting/lighting/elapsed_gpu"),
        })
        {
            if (!Diagnostics.TryRead(path, out var reading) || reading.Smoothed is not { } milliseconds) continue;
            performance.Append(FormattableString.Invariant($"{label,-17}  {milliseconds:0.00} ms\n"));
            total += milliseconds;
        }

        performance.Append(FormattableString.Invariant($"{"Total",-17}  {total:0.00} ms\n"));
        return performance.ToString();
    }
}

/// <summary>Which of the controls a row of the panel says.</summary>
public enum ControlRowKind
{
    /// <summary>Pausing the objects.</summary>
    Pause,

    /// <summary>The test material metal or not.</summary>
    Metallic,

    /// <summary>The roughness presets.</summary>
    Presets,

    /// <summary>The right mirror panning.</summary>
    MirrorPan,

    /// <summary>The right mirror sliding.</summary>
    MirrorSlide,

    /// <summary>The realtime lighting or the path tracer.</summary>
    Renderer,

    /// <summary>DLSS's ray reconstruction, which the bridge does not build.</summary>
    Denoising,
}

/// <summary>A row of the panel, saying one control as things stand.</summary>
[Behavior]
public partial struct ControlRow
{
    /// <summary>Which control it says.</summary>
    public ControlRowKind Kind;

    /// <summary>The row's text written, after Update, as Bevy writes it in PostUpdate.</summary>
    [OnPostUpdate]
    public void UpdateControlText(BehaviorContext ctx) => Ui.SetText(ctx.Entity, SolariReflections.ControlText(Kind));
}

/// <summary>The text that says how long each of Solari's passes took.</summary>
[Behavior]
public partial struct ReflectionTimings
{
    /// <summary>The passes' times written, after Update, as Bevy writes them in PostUpdate.</summary>
    [OnPostUpdate]
    public void UpdatePerformanceText(BehaviorContext ctx) => Ui.SetText(ctx.Entity, SolariReflections.Performance());
}

/// <summary>Bevy's OrbitingObject, the cube that goes round.</summary>
[Behavior]
public partial struct OrbitingObject;

/// <summary>Bevy's SlidingObject, the sphere that slides from side to side.</summary>
[Behavior]
public partial struct SlidingObject;

/// <summary>Bevy's PanningMirror, the panel of the test material that pans and slides.</summary>
[Behavior]
public partial struct PanningMirror;

/// <summary>Bevy's RoughnessValueText, the label saying the roughness.</summary>
[Behavior]
public partial struct RoughnessValueText;

/// <summary>Bevy's RoughnessSliderThumb, the slider's thumb.</summary>
[Behavior]
public partial struct RoughnessSliderThumb;

// Bevy's ssr example, examples/3d/ssr.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Shows screen-space reflections on a camera drawing deferred, over water whose ripples are a
// material of the example's own drawn into Bevy's deferred buffers, over a metal floor or a red
// one, with a cube, the flight helmet or a row of capsules standing on it. The buttons turn the
// reflections on and off, choose the model and the floor, and set the reflections' ranges of
// roughness and their fade at the picture's edges, and the camera orbits with A and D and comes in
// and out with W, S and the wheel.
internal static class Ssr
{
    private const float KeyboardZoomSpeed = 0.1f;
    private const float KeyboardOrbitSpeed = 0.02f;
    private const float WheelZoomSpeed = 0.25f;
    private const float ZoomMin = 2f, ZoomMax = 12f;

    private enum DisplayedModel
    {
        Cube,
        FlightHelmet,
        Capsules,
    }

    private enum DisplayedBase
    {
        Water,
        Metallic,
        RedPlane,
    }

    // Bevy's AppSettings resource, the ends of the three ranges kept in the order RangeEnd names
    // them.
    private static bool _ssrOn;
    private static DisplayedModel _model;
    private static DisplayedBase _base;
    private static readonly float[] Ends = new float[6];

    private static Entity _camera;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        (_ssrOn, _model, _base) = (true, DisplayedModel.Cube, DisplayedBase.Water);
        float[] ends = [0f, 0.01f, 0.99f, 1f, 0f, 0f];
        ends.CopyTo(Ends, 0);

        app.Startup(Setup, "ssr.Setup");

        // Hidden as it comes in, unless it was chosen while it loaded, as Bevy spawns it hidden
        // and its query finds it from the start.
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(Vec3.Zero, Quat.Identity, new Vec3(2.5f)));
            ctx.Ecs.Add(root, new FlightHelmetModel());
            Show<FlightHelmetModel>(ctx.Ecs, _model == DisplayedModel.FlightHelmet);
        });

        app.Update(MoveCamera, "ssr.MoveCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Deferred, which screen-space reflections need, for Bevy's own materials as well as the
        // water's, and multisampling off on the camera, which deferred drawing does not support.
        Render.SetDeferredRendering(true);

        var cube = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f),
            Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (1f, 1f, 1f, 1f),
                BaseColorTexture = AssetServer.Load(AssetKind.Image, "branding/icon.png"),
            }),
            Transform.At(0f, 0.5f, 0f));
        ecs.Add(cube, new CubeModel());

        // A row of black capsules, from glossy to rough, under one parent.
        var capsules = ecs.Spawn();
        ecs.Add(capsules, Transform.At(0f, 0.5f, 0f));
        ecs.Insert<VisibilityRef>(capsules).Value = Visibility.Hidden;
        ecs.Add(capsules, new CapsulesParent());
        var capsule = Render.CreateMesh(MeshShape.Capsule, 0.4f, 0.5f);
        for (var i = 0; i < 5; i++)
        {
            var roughness = i * 0.25f;
            var child = ecs.SpawnMesh(
                capsule,
                Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Roughness = MathF.Max(roughness, 0.08f) }),
                Transform.At((i * 1.1f) - (1.1f * 2f), 0.5f, 0f));
            ecs.Add(child, new CapsuleModel());
            ecs.SetParent(child, capsules);
        }

        var plane = Render.CreateMesh(MeshShape.Plane, 2f, 2f);
        var wide = new Transform(Vec3.Zero, Quat.Identity, new Vec3(100f));

        var darkGray = Color.FromHex("#a9a9a9");
        var metallic = ecs.SpawnMesh(plane, Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (darkGray.R, darkGray.G, darkGray.B, 1f),
            Metallic = 1f,
            Roughness = 0.3f,
        }), wide);
        ecs.Add(metallic, new MetallicBaseModel());
        ecs.Wrap<VisibilityRef>(metallic).Value = Visibility.Hidden;

        var red = ecs.SpawnMesh(plane, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f), Metallic = 0f, Roughness = 0.2f }), wide);
        ecs.Add(red, new RedPlaneBaseModel());
        ecs.Wrap<VisibilityRef>(red).Value = Visibility.Hidden;

        // The water, its ripples four octaves of a normal map drifting over each other, drawn into
        // the deferred buffers by its own shader's deferred stage. Its octaves are Bevy's numbers,
        // which Bevy's example calls random values for some variety.
        var water = Shaders.CreateMaterial(Shaders.CreateProgram(new ShaderProgramSettings
            {
                PrepassVertex = new ShaderStage("shaders/water_material.slang", "prepass_vertex"),
                Deferred = "shaders/water_material.slang",
            }))
            .SetTexture("water_normals", AssetServer.LoadImage("textures/water_normals.png", TextureSettings.Data))
            .SetSampler("water_sampler", SamplerSettings.Linear)
            .Set("octave_vectors", new[] { new Vector4(0.080f, 0.059f, 0.073f, -0.062f), new Vector4(0.153f, 0.138f, -0.149f, -0.195f) })
            .Set("octave_scales", new Vector4(1f, 2.1f, 7.9f, 14.9f) * 5f)
            .Set("octave_strengths", new Vector4(0.16f, 0.18f, 0.093f, 0.044f))
            .Set("roughness", 0.09f);
        ecs.Add(ecs.SpawnMesh(plane, water, wide), new WaterModel());

        // The camera, with an environment map and a skybox so the water has more to reflect than
        // the cube, temporal antialiasing, ambient occlusion and the reflections.
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-1.25f, 2.25f, 4.5f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true, Msaa = 1, AntiAlias = AntiAliasPass.Temporal });
        Render.SetAmbientOcclusion(_camera, AmbientOcclusionQuality.High);
        var specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
        Render.SetEnvironmentMap(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"), specular, 5000f);
        Render.SetSkybox(_camera, specular, 5000f);
        Reflect();

        Theme.BasicExampleTheme(Color.White);
        var column = Radio.MainUiNode();
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "SSR", [(true, "On"), (false, "Off")], 0), UiWidgetKind.RadioGroup);
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Model",
            [(DisplayedModel.Cube, "Cube"), (DisplayedModel.FlightHelmet, "Flight Helmet"), (DisplayedModel.Capsules, "Capsules")], 0), UiWidgetKind.RadioGroup);
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Base",
            [(DisplayedBase.Water, "Water"), (DisplayedBase.Metallic, "Metallic"), (DisplayedBase.RedPlane, "Red Plane")], 0), UiWidgetKind.RadioGroup);
        ecs.Observe<ValueChange<Entity>>(on => HandleValueChangeSsrOn(on.Event.Value));
        ecs.Observe<ValueChange<Entity>>(on => HandleValueChangeDisplayedModel(on.Ecs, on.Event.Value));
        ecs.Observe<ValueChange<Entity>>(on => HandleValueChangeDisplayedBase(on.Ecs, on.Event.Value));
        RangeRow(ecs, column, "Min Roughness", RangeEnd.MinRoughnessStart, RangeEnd.MinRoughnessEnd);
        RangeRow(ecs, column, "Max Roughness", RangeEnd.MaxRoughnessStart, RangeEnd.MaxRoughnessEnd);
        RangeRow(ecs, column, "Edge Fadeout", RangeEnd.EdgeFadeoutStart, RangeEnd.EdgeFadeoutEnd);
    }

    // In and out with W, S and the wheel, between two distances, and around with A and D.
    private static void MoveCamera(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var distanceDelta = 0f;
        var thetaDelta = 0f;
        if (input.KeyDown(Key.W)) distanceDelta -= KeyboardZoomSpeed;
        if (input.KeyDown(Key.S)) distanceDelta += KeyboardZoomSpeed;
        if (input.KeyDown(Key.A)) thetaDelta += KeyboardOrbitSpeed;
        if (input.KeyDown(Key.D)) thetaDelta -= KeyboardOrbitSpeed;
        distanceDelta -= input.WheelY * WheelZoomSpeed;

        var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
        if (distanceDelta != 0f)
        {
            var localZ = (transform.Rotation * new Vec3(0f, 0f, 1f)).Normalized;
            transform = transform with { Translation = localZ * Math.Clamp(transform.Translation.Length + distanceDelta, ZoomMin, ZoomMax) };
        }

        if (thetaDelta != 0f) transform = Transform.LookingAt(Quat.FromRotationY(thetaDelta) * transform.Translation, Vec3.Zero, Vec3.UnitY);

        if (distanceDelta != 0f || thetaDelta != 0f) ctx.Ecs.Set(_camera, transform);
    }

    private static void HandleValueChangeSsrOn(Entity button)
    {
        if (!Radio.OptionValue<bool>(button, out var on)) return;

        _ssrOn = on;
        Reflect();
    }

    private static void HandleValueChangeDisplayedModel(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<DisplayedModel>(button, out var model)) return;

        _model = model;
        Show<CubeModel>(ecs, _model == DisplayedModel.Cube);
        Show<FlightHelmetModel>(ecs, _model == DisplayedModel.FlightHelmet);
        Show<CapsuleModel>(ecs, _model == DisplayedModel.Capsules);
        Show<CapsulesParent>(ecs, _model == DisplayedModel.Capsules);
    }

    private static void HandleValueChangeDisplayedBase(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<DisplayedBase>(button, out var floor)) return;

        _base = floor;
        Show<MetallicBaseModel>(ecs, _base == DisplayedBase.Metallic);
        Show<RedPlaneBaseModel>(ecs, _base == DisplayedBase.RedPlane);
        Show<WaterModel>(ecs, _base == DisplayedBase.Water);
    }

    // Bevy's handle_value_change_number_input, the end of the range its input sets written back to
    // the input and to the reflections.
    private static void HandleValueChangeNumberInput(On<ValueChange<float>> on, RangeEnd end)
    {
        Ends[(int)end] = on.Event.Value;
        Feathers.SetNumber(on.Event.Source, on.Event.Value);
        Reflect();
    }

    // The reflections with the ranges chosen, or none.
    private static void Reflect() =>
        Render.SetScreenSpaceReflections(_camera, _ssrOn
            ? new ReflectionSettings
            {
                FadeInRoughness = (Ends[(int)RangeEnd.MinRoughnessStart], Ends[(int)RangeEnd.MinRoughnessEnd]),
                FadeOutRoughness = (Ends[(int)RangeEnd.MaxRoughnessStart], Ends[(int)RangeEnd.MaxRoughnessEnd]),
                EdgeFade = (Ends[(int)RangeEnd.EdgeFadeoutStart], Ends[(int)RangeEnd.EdgeFadeoutEnd]),
            }
            : null);

    private static void Show<T>(EcsWorld ecs, bool shown) where T : unmanaged
    {
        foreach (var entity in ecs.EntitiesWith<T>())
            ecs.Wrap<VisibilityRef>(entity).Value = shown ? Visibility.Visible : Visibility.Hidden;
    }

    // A row setting a range, its title, then the start and the end each a number input, as Bevy's
    // range_row lays one out.
    private static void RangeRow(EcsWorld ecs, Entity column, string title, RangeEnd start, RangeEnd end)
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center });
        ecs.SetParent(row, column);

        var name = Ui.SpawnNode(new UiSettings { Width = Length.Px(150f) });
        ecs.SetParent(name, row);
        Feathers.Label(name, title);

        RangeControls(ecs, row, start);

        var to = Ui.SpawnNode(new UiSettings { Margin = new Sides(Length.Px(10f), Length.Zero, Length.Px(10f), Length.Zero) });
        ecs.SetParent(to, row);
        Feathers.LabelSmall(to, "to");

        RangeControls(ecs, row, end);
    }

    // Bevy's range_controls, a number input of three digits held between 0 and 1. Its settings are
    // written before its value, since it writes its text as its value changes.
    private static void RangeControls(EcsWorld ecs, Entity row, RangeEnd end)
    {
        var input = Feathers.NumberInput(row);
        ecs.Insert<NumberInputPrecisionRef>(input).Value = 3;
        Feathers.SetNumberLimit(input, 0f, 1f, hard: true);
        Feathers.SetNumber(input, Ends[(int)end]);
        ecs.Wrap<NodeRef>(input).AlignItems = NodeRef.AlignItemsVariant.Center;
        ecs.Observe<ValueChange<float>>(input, on => HandleValueChangeNumberInput(on, end));
    }
}

/// <summary>Which end of which of the ssr example's ranges a number input sets.</summary>
public enum RangeEnd
{
    /// <summary>Where reflections begin to fade in with roughness.</summary>
    MinRoughnessStart,

    /// <summary>Where they have faded in.</summary>
    MinRoughnessEnd,

    /// <summary>Where they begin to fade out with roughness.</summary>
    MaxRoughnessStart,

    /// <summary>Where they have faded out.</summary>
    MaxRoughnessEnd,

    /// <summary>Where they begin to fade toward the picture's edge.</summary>
    EdgeFadeoutStart,

    /// <summary>Where they have faded out at the edge.</summary>
    EdgeFadeoutEnd,
}

/// <summary>The cube, turned about Y with the time.</summary>
[Behavior]
public partial struct CubeModel
{
    /// <summary>
    /// Turned as Bevy's <c>rotate_model</c> turns it, by the seconds since the start.
    /// </summary>
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) => transform.Rotation = Quat.FromRotationY(ctx.Time.Elapsed);
}

/// <summary>The flight helmet, turned about Y with the time as the cube is.</summary>
[Behavior]
public partial struct FlightHelmetModel
{
    /// <summary>
    /// Turned as Bevy's <c>rotate_model</c> turns it, by the seconds since the start.
    /// </summary>
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) => transform.Rotation = Quat.FromRotationY(ctx.Time.Elapsed);
}

/// <summary>One capsule of the row.</summary>
[Behavior]
public partial struct CapsuleModel;

/// <summary>The parent of the row of capsules.</summary>
[Behavior]
public partial struct CapsulesParent;

/// <summary>The metal floor.</summary>
[Behavior]
public partial struct MetallicBaseModel;

/// <summary>The red floor, which is not metal.</summary>
[Behavior]
public partial struct RedPlaneBaseModel;

/// <summary>The water.</summary>
[Behavior]
public partial struct WaterModel;

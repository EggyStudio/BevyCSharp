using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The hub, the middle of the map, a cube turning in place over a ground that runs out to every
/// zone, under a sky and a sun.
/// </summary>
/// <remarks>
/// <para>
/// Skipped on a build without a renderer, so the rest of the program runs unchanged either way.
/// The camera and the sun are kept for the panel's settings, which put the picture's settings on
/// the camera and make the sun again with or without shadows (<see cref="Applied"/>).
/// </para>
/// <para>
/// The ground is wide enough for every zone round the hub, with a slab under it for what falls,
/// and the signposts at the hub's edge are <see cref="Zones"/>'.
/// </para>
/// </remarks>
[Behavior]
public partial struct Scene
{
    /// <summary>Radians per second about the vertical axis.</summary>
    public float YawSpeed;

    /// <summary>Radians per second about the horizontal axis.</summary>
    public float PitchSpeed;

    /// <summary>Current yaw in radians.</summary>
    public float Yaw;

    /// <summary>Current pitch in radians.</summary>
    public float Pitch;

    /// <summary>How high the ground is, below the cube's middle.</summary>
    public const float GroundHeight = -1.2f;

    /// <summary>How far the ground runs, each side, from one edge to the other.</summary>
    public const float GroundSize = 240f;

    /// <summary>The ground, the cube and the lamp, for a behavior that builds on the scene.</summary>
    internal static (Entity Ground, Entity Cube, Entity Lamp) Parts { get; private set; }

    /// <summary>
    /// The camera the view is drawn with, or nothing before it is made or in a run with no
    /// renderer.
    /// </summary>
    internal static Entity? Camera { get; private set; }

    /// <summary>
    /// The sun, which <see cref="DayNight"/> carries across the sky, or nothing before it is made.
    /// </summary>
    internal static Entity Sun => _sun;

    private static Entity _sun = Entity.None;
    private static bool? _shadows;

    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var eye = Zones.All[0].Eye;
        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 55f });
        Camera = camera;
        ctx.Ecs.SetName(camera, "Scene camera");

        // Lit by rays where the program asked for Solari and the bridge and the GPU have it.
        if (Render.RayTracingActive) Render.SetRayTracedLighting(camera, true);
        ctx.Ecs.Add(camera, Transform.LookingAt(eye, Vec3.Zero, Vec3.UnitY));

        // Steerable from the mouse and keyboard, starting from the direction set above.
        ctx.Ecs.Add(camera, FlyCamera.LookingAt(eye, Vec3.Zero));

        Console.WriteLine(
            "[Scene] in spectator mode, hold the right button to look and fly with WASD, Q and E; "
            + "middle button to slide; wheel to move along the view; Alt and the left button to "
            + "orbit; F to frame the origin");

        // The sky, scattered from the sun below rather than painted. The camera sees it where the
        // scene does not cover, and it tints everything in the distance. Not where meshlets run,
        // whose pipelines Bevy builds without an atmosphere's bindings, and the settings put the
        // dusk sky there in its place.
        if (!Render.MeshletsActive) Render.SetAtmosphere(camera, new AtmosphereSettings());

        // What the camera does with the picture once the scene is drawn, from the panel's
        // settings. The high dynamic range target makes the rest worth having. Without it nothing
        // is brighter than white, so the tonemapper has nothing to bring down and bloom has nothing
        // to scatter.
        Render.SetPostProcessing(camera, Applied.Picture(Settings.Current));

        // The lens, the vignette pulling the eye in from the corners unless the effects page took
        // it off, and the rest of that page.
        Render.SetEffects(camera, Applied.Lens(Settings.Current));

        _sun = Entity.None;
        _shadows = null;
        Light(ctx.Ecs, Settings.Current.Shadows);

        // A cool rim from the other side, so the cube reads as a solid rather than a silhouette
        // against the dark clear color.
        var rim = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Spot,
            Intensity = 40_000f,
            Color = (0.4f, 0.6f, 1f),
            Range = 24f,
            InnerAngle = 0.15f,
            OuterAngle = 0.5f,
        });
        ctx.Ecs.Add(rim, Transform.LookingAt(new Vec3(-4f, 3f, -5f), Vec3.Zero, Vec3.UnitY));

        // A lamp, emissive well past white so there is something for the bloom to scatter.
        //
        // The numbers are luminance in nits, and the camera divides them by its exposure, which at
        // Bevy's own setting is about a thousand. Thousands here arrive as the handful of multiples
        // of white that blow the sphere out and feed the bloom. It is lit rather than unlit because
        // Bevy adds the emission as part of the lighting, so an unlit sphere would show its base
        // color and nothing else.
        var lamp = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, lamp, Render.CreateMesh(MeshShape.Sphere, 0.6f));
        Render.SetMaterial(ctx.Ecs, lamp, Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (1f, 0.6f, 0.2f, 1f),
            Emissive = (12_000f, 5_000f, 1_000f, 1f),
        }));
        ctx.Ecs.Add(lamp, Transform.At(-2.5f, 1.2f, 1.5f));

        var ground = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Plane, GroundSize, GroundSize));
        // Tiling takes both halves: a repeating sampler, and UVs that run past one. The plane's
        // own UVs stop at one however large it is, so without the scale this shows a single
        // stretched copy, a square of the checker two units across.
        Render.SetMaterial(ctx.Ecs, ground, Render.CreateMaterial(new MaterialSettings
        {
            BaseColorTexture = AssetServer.LoadImage("textures/checker.png", TextureSettings.Tiling),
            UvScale = (GroundSize / 2f, GroundSize / 2f),
            Roughness = 0.9f,
        }));
        ctx.Ecs.Add(ground, Transform.At(0f, GroundHeight, 0f));
        ctx.Ecs.SetName(ground, "Ground");

        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.6f, 1.6f, 1.6f));
        Render.SetMaterial(ctx.Ecs, cube,
            Render.CreateMaterial(0.25f, 0.55f, 0.85f, metallic: 0.1f, roughness: 0.35f));
        ctx.Ecs.Add(cube, Transform.Identity);

        // Turning about two axes rather than one, so the cube reads as a solid rather than a
        // flat outline.
        ctx.Ecs.Add(cube, new Scene { YawSpeed = 0.9f, PitchSpeed = 0.35f });

        Parts = (ground, cube, lamp);

        // A HUD: a panel pinned to a corner with a line of text inside it, below, where the admin
        // panel and the overlay leave room. Nesting is ordinary parenting, so the text moves with
        // the panel.
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(16f),
            Bottom = Length.Px(16f),
            Padding = Length.Px(10f),
            Color = (0f, 0f, 0f, 0.45f),
        });

        var readout = Ui.SpawnText("frame 0", new UiSettings { Color = (0.9f, 0.95f, 1f, 1f) }, 18f);
        ctx.Ecs.SetParent(readout, panel);
        ctx.Ecs.Add(readout, new Hud());

        Console.WriteLine(
            "[Scene] the hub, with a signpost to each zone. F11 toggles fullscreen and Tab locks the cursor.");
    }

    /// <summary>
    /// The sun, made again where its shadows are to change, since a light's settings are given as
    /// it is made.
    /// </summary>
    internal static void Light(EcsWorld ecs, bool shadows)
    {
        if (_shadows == shadows && ecs.IsAlive(_sun)) return;

        if (ecs.IsAlive(_sun)) ecs.Despawn(_sun);
        _sun = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 12_000f,
            Color = (1f, 0.95f, 0.85f),
            // Solari traces the shadows itself, so the sun casts none of its own where it runs.
            Shadows = shadows && !Render.RayTracingActive,
        });
        ecs.Add(_sun, Transform.LookingAt(new Vec3(6f, 2.5f, 4f), Vec3.Zero, Vec3.UnitY));
        ecs.SetName(_sun, "Sun");
        _shadows = shadows;
    }

    /// <summary>Keeps the HUD's readout current.</summary>
    /// <remarks>
    /// The text is rewritten in place. Respawning it every frame would work and would churn an
    /// entity sixty times a second for a string that changes.
    /// </remarks>
    [OnUpdate]
    public static void UpdateHud(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        foreach (var row in ctx.Ecs.Query<Hud>(markChanged: false))
            Ui.SetText(row.Entity, $"frame {ctx.Time.FrameCount}   {ctx.Time.SmoothedFps:F0} fps");
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Yaw += YawSpeed * ctx.Time.Delta;
        Pitch += PitchSpeed * ctx.Time.Delta;

        ref var transform = ref ctx.Ecs.GetRef<Transform>(ctx.Entity);

        transform.Rotation = Quat.FromRotationY(Yaw) * Quat.FromRotationX(Pitch);
    }
}

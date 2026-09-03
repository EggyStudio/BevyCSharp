using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// The default scene: a cube turning in place, lit, above a ground plane.
/// </summary>
/// <remarks>
/// Skipped on a build without a renderer, so the rest of the sample runs unchanged either way.
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

    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var eye = new Vec3(3.5f, 3f, 6f);
        var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 55f });
        ctx.Ecs.Add(camera, Transform.LookingAt(eye, Vec3.Zero, Vec3.UnitY));

        // Steerable from the mouse and keyboard, starting from the direction set above.
        ctx.Ecs.Add(camera, FlyCamera.LookingAt(eye, Vec3.Zero));

        Console.WriteLine(
            "[Scene] camera: hold the right button to look and fly with WASD, Q and E; "
            + "middle button to slide; wheel to move along the view; Alt and the left button to "
            + "orbit; F to frame the origin");

        // The sky, scattered from the sun below rather than painted: it is what the camera sees
        // where the scene does not cover, and what tints everything in the distance.
        Render.SetAtmosphere(camera, new AtmosphereSettings());

        // What the camera does with the picture once the scene is drawn. The high dynamic range
        // target is what makes the rest worth having: without it nothing is brighter than white,
        // so the tonemapper has nothing to bring down and bloom has nothing to scatter.
        Render.SetPostProcessing(camera, new PostSettings
        {
            Hdr = true,
            Tonemapper = Tonemapper.AgX,
            Bloom = true,
            BloomIntensity = 0.3f,
            AntiAlias = AntiAliasPass.Fxaa,
            Msaa = 1,
        });

        // The lens it is drawn through. Focus is on the cube at the origin, so the checker runs
        // soft towards the horizon, and the vignette pulls the eye in from the corners. Judge
        // either against a run with this call removed, which is the only way to tell an effect
        // from an imagined one.
        Render.SetEffects(camera, new EffectSettings
        {
            DepthOfField = DepthOfFieldMode.Bokeh,
            FocalDistance = 7.4f,
            MaxDepth = 60f,
            Vignette = 0.45f,
            VignetteRadius = 0.7f,
        });

        var sun = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 12_000f,
            Color = (1f, 0.95f, 0.85f),
        });
        ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(6f, 2.5f, 4f), Vec3.Zero, Vec3.UnitY));

        // A cool rim from the other side, so the cube reads as a solid rather than a silhouette
        // against the dark clear colour.
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
        // The numbers are luminance in nits, and the camera divides them by its exposure, which
        // at Bevy's own setting is about a thousand. Thousands here are what arrive as the
        // handful of multiples of white that blow the sphere out and feed the bloom. It is lit
        // rather than unlit because Bevy adds the emission as part of the lighting, so an unlit
        // sphere would show its base colour and nothing else.
        var lamp = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, lamp, Render.CreateMesh(MeshShape.Sphere, 0.6f));
        Render.SetMaterial(ctx.Ecs, lamp, Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (1f, 0.6f, 0.2f, 1f),
            Emissive = (12_000f, 5_000f, 1_000f, 1f),
        }));
        ctx.Ecs.Add(lamp, Transform.At(-2.5f, 1.2f, 1.5f));

        var ground = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Plane, 24f, 24f));
        // Tiling takes both halves: a repeating sampler, and UVs that run past one. The plane's
        // own UVs stop at one however large it is, so without the scale this shows a single
        // stretched copy.
        Render.SetMaterial(ctx.Ecs, ground, Render.CreateMaterial(new MaterialSettings
        {
            BaseColorTexture = AssetServer.LoadImage("textures/checker.png", TextureSettings.Tiling),
            UvScale = (12f, 12f),
            Roughness = 0.9f,
        }));
        ctx.Ecs.Add(ground, Transform.At(0f, -1.2f, 0f));

        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.6f, 1.6f, 1.6f));
        Render.SetMaterial(ctx.Ecs, cube,
            Render.CreateMaterial(0.25f, 0.55f, 0.85f, metallic: 0.1f, roughness: 0.35f));
        ctx.Ecs.Add(cube, Transform.Identity);

        // Turning about two axes rather than one, so the cube reads as a solid rather than a
        // flat outline.
        ctx.Ecs.Add(cube, new Scene { YawSpeed = 0.9f, PitchSpeed = 0.35f });

        // A HUD: a panel pinned to a corner with a line of text inside it. Nesting is ordinary
        // parenting, so the text moves with the panel.
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(16f),
            Top = Length.Px(16f),
            Padding = Length.Px(10f),
            Color = (0f, 0f, 0f, 0.45f),
        });

        var readout = Ui.SpawnText("frame 0", new UiSettings { Color = (0.9f, 0.95f, 1f, 1f) }, 18f);
        ctx.Ecs.SetParent(readout, panel);
        ctx.Ecs.Add(readout, new Hud());

        Console.WriteLine(
            "[Scene] a rotating cube. Escape closes the window, F11 toggles fullscreen, "
            + "Tab locks the cursor, F2 draws the orbits, F3 plays a chime.");
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

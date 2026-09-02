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

        var camera = Render.SpawnCamera3d(new CameraSettings
        {
            FieldOfView = 55f,
            Clear = ClearMode.Custom,
            ClearColor = (0, 0, 0, 1f),
        });
        ctx.Ecs.Add(camera, Transform.LookingAt(new Vec3(3.5f, 3f, 6f), Vec3.Zero, Vec3.UnitY));

        var sun = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = 12_000f,
            Color = (1f, 0.95f, 0.85f),
        });
        ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 8f, 5f), Vec3.Zero, Vec3.UnitY));

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

        // A HUD: a panel pinned to a corner, stacking what it holds in a column. Only the panel
        // is placed by hand; everything inside is put where the layout says, which is what the
        // direction, the gap and the padding are for.
        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(16f),
            Top = Length.Px(16f),
            Direction = UiDirection.Column,
            Align = UiAlign.Start,
            RowGap = Length.Px(8f),
            Padding = Length.Px(10f),
            Border = Length.Px(1f),
            BorderColor = (0.45f, 0.65f, 0.95f, 0.7f),
            Color = (0f, 0f, 0f, 0.45f),
        });

        var readout = Ui.SpawnText("frame 0", new UiSettings { Color = (0.9f, 0.95f, 1f, 1f) }, 18f);
        ctx.Ecs.SetParent(readout, panel);
        ctx.Ecs.Add(readout, new Hud());

        // A button: the same kind of node, asked to report the pointer. It sits under the readout
        // because the panel stacks its children, not because it was told where to go. The caption
        // is a child, so the pointer is tracked on the box the eye sees rather than on the glyphs.
        var button = Ui.SpawnNode(new UiSettings
        {
            Padding = Length.Px(10f),
            Border = Length.Px(1f),
            BorderColor = (0.6f, 0.8f, 1f, 0.9f),
            Interactive = true,
            Color = (0.12f, 0.24f, 0.4f, 0.85f),
        });
        ctx.Ecs.SetParent(button, panel);

        var caption = Ui.SpawnText(
            "clicks: 0", new UiSettings { Color = (0.9f, 0.95f, 1f, 1f) }, 18f);
        ctx.Ecs.SetParent(caption, button);
        ctx.Ecs.Add(button, new Clickable());

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

/// <summary>
/// A UI button that counts what it has been clicked.
/// </summary>
/// <remarks>
/// The idiom for reading a click: <see cref="UiInteraction.Pressed"/> holds for as long as the
/// pointer is down, so the click is the edge into it and the previous answer has to be kept. A
/// behavior field is where it goes, since the behavior is a component on the button itself.
/// </remarks>
[Behavior]
public partial struct Clickable
{
    /// <summary>How the pointer stood on the node last frame.</summary>
    public UiInteraction Previous;

    /// <summary>How often the edge into a press has been seen.</summary>
    public int Clicks;

    [OnUpdate]
    public void Watch(BehaviorContext ctx)
    {
        var state = Ui.InteractionOf(ctx.Entity);
        if (state == UiInteraction.Pressed && Previous != UiInteraction.Pressed)
        {
            Clicks++;
            Console.WriteLine($"[Ui] the button was clicked ({Clicks})");
        }

        Previous = state;

        var caption = ctx.Ecs.ChildrenOf(ctx.Entity);
        if (caption.Length == 0) return;

        Ui.SetText(caption[0], state switch
        {
            UiInteraction.Pressed => $"clicks: {Clicks}  (down)",
            UiInteraction.Hovered => $"clicks: {Clicks}  (hover)",
            _ => $"clicks: {Clicks}",
        });
    }
}

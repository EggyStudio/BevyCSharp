using Bevy;

namespace BevyCSharp.Examples;

/// <summary>One of Bevy's examples written here, by Bevy's name.</summary>
/// <param name="Name">Bevy's name for it, which is also its file's.</param>
/// <param name="Build">What it adds to the app, as Bevy's <c>main</c> adds its systems.</param>
/// <param name="Configure">What it changes about the app before it runs, where it changes anything.</param>
/// <param name="Prints">
/// For an example with nothing to draw, how many frames it runs headless, and what it prints in
/// them is its capture. Zero for one that draws.
/// </param>
/// <param name="Returned">What runs once the app has stopped and the program is back in its own code.</param>
/// <remarks>
/// Bevy's examples of the ECS mostly print to the console, some in a window left empty, and one
/// that prints is run here with no window at all, so it needs no renderer and its output is
/// the same on any machine.
/// </remarks>
internal sealed record Example(string Name, Action<App> Build, Action<Config>? Configure = null, uint Prints = 0, Action? Returned = null);

/// <summary>Helpers for what Bevy's examples say in one word and the bridge in several.</summary>
internal static class Scene
{
    /// <summary>
    /// The size the example was opened at, in pixels, for one that divides it between cameras,
    /// since an offscreen run has no window to ask.
    /// </summary>
    public static (uint Width, uint Height) Size { get; set; } = (1280, 720);

    /// <summary>Runs <paramref name="setup"/> once as the app starts, as Bevy's <c>Startup</c> systems do.</summary>
    public static App Startup(this App app, Action<BehaviorContext> setup, string name = "Example.Setup") =>
        app.AddSystem(Stage.Startup, new SystemDescriptor(world => setup(new BehaviorContext(world)), name));

    /// <summary>Runs <paramref name="update"/> every frame, as Bevy's <c>Update</c> systems do.</summary>
    public static App Update(this App app, Action<BehaviorContext> update, string name = "Example.Update") =>
        app.AddSystem(Stage.Update, new SystemDescriptor(world => update(new BehaviorContext(world)), name));

    /// <summary>
    /// Runs <paramref name="run"/> in <paramref name="stage"/>, as a system Bevy's example adds to
    /// that schedule, and only while <paramref name="runIf"/> passes where one is given.
    /// </summary>
    public static App On(this App app, Stage stage, Action<BehaviorContext> run, string name, Func<World, bool>? runIf = null)
    {
        var descriptor = new SystemDescriptor(world => run(new BehaviorContext(world)), name);
        return app.AddSystem(stage, runIf is null ? descriptor : descriptor.RunIf(runIf));
    }

    /// <summary>A color given in sRGB, as Bevy's <c>Color::srgb</c>, in the linear terms a material takes.</summary>
    public static (float R, float G, float B, float A) Srgb(float r, float g, float b, float a = 1f)
    {
        var linear = Color.FromSrgb(r, g, b, a);
        return (linear.R, linear.G, linear.B, linear.A);
    }

    /// <summary>A color given as sRGB bytes, as Bevy's <c>Color::srgb_u8</c>.</summary>
    public static (float R, float G, float B, float A) Srgb8(byte r, byte g, byte b) => Srgb(r / 255f, g / 255f, b / 255f);

    /// <summary>
    /// A color given as hue in degrees, saturation and lightness, as Bevy's <c>Hsla</c>, in the
    /// linear terms a material takes.
    /// </summary>
    public static (float R, float G, float B, float A) Hsl(float hue, float saturation, float lightness)
    {
        // Bevy's conversion, from HSL to sRGB by the chroma and the hue's sixth of the circle.
        var chroma = (1f - MathF.Abs(2f * lightness - 1f)) * saturation;
        var h = (hue % 360f + 360f) % 360f / 60f;
        var x = chroma * (1f - MathF.Abs(h % 2f - 1f));
        var (r, g, b) = (int)h switch
        {
            0 => (chroma, x, 0f),
            1 => (x, chroma, 0f),
            2 => (0f, chroma, x),
            3 => (0f, x, chroma),
            4 => (x, 0f, chroma),
            _ => (chroma, 0f, x),
        };
        var m = lightness - chroma / 2f;
        return Srgb(r + m, g + m, b + m);
    }

    /// <summary>A material of one color, as Bevy makes one from a <c>Color</c>.</summary>
    public static AssetHandle Material((float R, float G, float B, float A) color) =>
        Render.CreateMaterial(new MaterialSettings { BaseColor = color });

    /// <summary>
    /// Spawns a glTF file's scene once it has loaded, as Bevy's <c>WorldAssetRoot</c> does, and
    /// hands the root it spawned to <paramref name="spawned"/>.
    /// </summary>
    public static void SpawnGltf(this App app, string path, Action<BehaviorContext, Entity>? spawned = null, int scene = 0)
    {
        var handle = AssetHandle.None;
        var done = false;

        app.Update(ctx =>
        {
            if (done) return;
            if (handle == AssetHandle.None) handle = AssetServer.LoadGltfScene(path, scene);
            if (AssetServer.StateOf(handle) != AssetLoadState.Loaded) return;

            done = true;
            var root = ctx.Ecs.SpawnScene(handle);
            spawned?.Invoke(ctx, root);
        }, $"Example.SpawnGltf({path})");
    }

    /// <summary>Spawns an entity drawn with a mesh and a material, placed by a transform.</summary>
    public static Entity Mesh(this EcsWorld ecs, AssetHandle mesh, AssetHandle material, Transform at)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, at);
        Render.SetMesh(ecs, entity, mesh);
        Render.SetMaterial(ecs, entity, material);
        return entity;
    }

    /// <summary>
    /// A point light as Bevy's default one is, a million lumens reaching twenty units and casting
    /// no shadow unless asked.
    /// </summary>
    public static Entity PointLight(this EcsWorld ecs, Vec3 at, bool shadows = false, float intensity = 1_000_000f, float range = 20f, float radius = 0f)
    {
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = intensity, Range = range, Radius = radius, Shadows = shadows });
        ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
        return light;
    }

    /// <summary>A camera placed by a transform, with Bevy's defaults otherwise.</summary>
    public static Entity Camera(this EcsWorld ecs, Transform at, CameraSettings? settings = null)
    {
        var camera = settings is null ? Render.SpawnCamera3d() : Render.SpawnCamera3d(settings);
        ecs.Add(camera, at);
        return camera;
    }
}

/// <summary>
/// Flies its camera as Bevy's <c>FreeCamera</c> does: WASD across, E and Q up and down, Shift to
/// run, and the mouse turning it while its left button is held.
/// </summary>
/// <remarks>
/// Bevy grabs the cursor while the button is held, which an example here leaves to the platform,
/// and an offscreen run, with no pointer, turns it not at all.
/// </remarks>
[Behavior]
public partial struct FreeCamera
{
    /// <summary>Units a second walking, and three times that running.</summary>
    public float Speed;

    private float _yaw;
    private float _pitch;
    private bool _started;

    [OnUpdate]
    public void Fly(BehaviorContext ctx, ref Transform transform)
    {
        if (!_started)
        {
            var euler = transform.Rotation.ToEuler();
            (_pitch, _yaw, _started) = (euler.X, euler.Y, true);
            if (Speed <= 0f) Speed = 5f;
        }

        var input = ctx.Input;
        if (input.MouseDown(MouseButton.Left))
        {
            // A fifth of a degree a pixel, Bevy's own sensitivity.
            const float Sensitivity = 0.2f * MathF.PI / 180f;
            _yaw -= input.MouseDeltaX * Sensitivity;
            _pitch = Math.Clamp(_pitch - input.MouseDeltaY * Sensitivity, -MathF.PI / 2f, MathF.PI / 2f);
        }

        transform.Rotation = Quat.FromRotationY(_yaw) * Quat.FromRotationX(_pitch);

        var forward = transform.Rotation * -Vec3.UnitZ;
        var right = transform.Rotation * Vec3.UnitX;
        var move = Vec3.Zero;
        if (input.KeyDown(Key.W)) move += forward;
        if (input.KeyDown(Key.S)) move -= forward;
        if (input.KeyDown(Key.D)) move += right;
        if (input.KeyDown(Key.A)) move -= right;
        if (input.KeyDown(Key.E)) move += Vec3.UnitY;
        if (input.KeyDown(Key.Q)) move -= Vec3.UnitY;

        var speed = Speed * (input.KeyDown(Key.ShiftLeft) ? 3f : 1f);
        transform.Translation += move * (speed * ctx.Time.Delta);
    }
}

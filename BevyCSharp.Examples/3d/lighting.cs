// Bevy's lighting example, examples/3d/lighting.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates different lights of various types and colors, some static, some moving over a simple
// scene.
internal static class Lighting
{
    private static readonly (float R, float G, float B) OrangeRed = Linear(255, 69, 0);

    private static float _aperture = 1f;
    private static float _shutter = 1f / 125f;
    private static float _sensitivity = 100f;
    private static bool _ambient = true;
    private static Entity _camera;
    private static Entity _text;

    public static void Build(App app)
    {
        app.Startup(Setup);
        app.Update(Update, "lighting.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var matte = (float R, float G, float B, float A) => Render.CreateMaterial(new MaterialSettings { BaseColor = (R, G, B, A), Roughness = 1f });
        var indigo = Scene.Srgb8(75, 0, 130);

        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 10f, 10f), matte(1f, 1f, 1f, 1f), Transform.Identity);

        // The left wall and the back one.
        var wall = Render.CreateMesh(MeshShape.Cuboid, 5f, 0.15f, 5f);
        var walls = matte(indigo.R, indigo.G, indigo.B, 1f);
        ecs.Mesh(wall, walls, new Transform(new Vec3(2.5f, 2.5f, 0f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
        ecs.Mesh(wall, walls, new Transform(new Vec3(0f, 2.5f, -2.5f), Quat.FromRotationX(MathF.PI / 2f), Vec3.One));

        // Bevy's logo, to show the shadows of a mask.
        var logo = ecs.Mesh(
            Render.CreateMesh(MeshShape.Rectangle, 2f, 0.5f),
            Render.CreateMaterial(new MaterialSettings
            {
                BaseColorTexture = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_light.png"),
                Roughness = 1f,
                AlphaMode = AlphaMode.Mask,
                AlphaCutoff = 0.5f,
                DoubleSided = true,
            }),
            new Transform(new Vec3(-2.2f, 0.5f, 1f), Quat.FromRotationY(MathF.PI / 8f), Vec3.One));
        ecs.Add(logo, new Movable());

        var cube = ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Scene.Material(Scene.Srgb8(255, 20, 147)), Transform.At(0f, 0.5f, 0f));
        ecs.Add(cube, new Movable());

        var sphere = ecs.Mesh(Render.CreateMesh(MeshShape.Sphere, 0.5f), Scene.Material(Scene.Srgb8(50, 205, 50)), Transform.At(1.5f, 1f, 1.5f));
        ecs.Add(sphere, new Movable());

        Render.SetAmbientLight(OrangeRed, 200f);

        // A red point light, a green spot light and a blue point light, each with a small glowing
        // shape of its color where it is.
        var red = Light(ecs, LightKind.Point, (1f, 0f, 0f), Transform.At(1f, 2f, 0f));
        Bulb(ecs, red, Render.CreateMesh(MeshShape.Sphere, 0.1f), (1f, 0f, 0f, 1f), (4f, 0f, 0f, 1f), Transform.Identity);

        var green = Light(ecs, LightKind.Spot, (0f, 1f, 0f), Transform.LookingAt(new Vec3(-1f, 2f, 0f), new Vec3(-1f, 0f, 0f), Vec3.UnitZ));
        Bulb(ecs, green, Render.CreateMesh(MeshShape.Capsule, 0.1f, 0.25f), (0f, 1f, 0f, 1f), (0f, 4f, 0f, 1f), new Transform(Vec3.Zero, Quat.FromRotationX(MathF.PI / 2f), Vec3.One));

        var blue = Light(ecs, LightKind.Point, (0f, 0f, 1f), Transform.At(0f, 4f, 0f));
        Bulb(ecs, blue, Render.CreateMesh(MeshShape.Sphere, 0.1f), (0f, 0f, 1f, 1f), (0f, 0f, 713f, 1f), Transform.Identity);

        // The sun, an overcast day's thousand lux, with its cascades drawn in to this small scene.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1000f, Shadows = true });
        ecs.Add(sun, new Transform(new Vec3(0f, 2f, 0f), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        ecs.Add(sun, new Sun());
        Render.SetShadowCascades(sun, maximum: 10f, firstBound: 4f);

        _text = Ui.SpawnText(Instructions(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
        Render.SetLensExposure(_camera, _aperture, _shutter, _sensitivity);
    }

    private static void Update(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var changed = false;

        if (input.KeyPressed(Key.Digit2)) { _aperture *= 2f; changed = true; }
        else if (input.KeyPressed(Key.Digit1)) { _aperture *= 0.5f; changed = true; }
        if (input.KeyPressed(Key.Digit4)) { _shutter *= 2f; changed = true; }
        else if (input.KeyPressed(Key.Digit3)) { _shutter *= 0.5f; changed = true; }
        if (input.KeyPressed(Key.Digit6)) { _sensitivity += 100f; changed = true; }
        else if (input.KeyPressed(Key.Digit5)) { _sensitivity -= 100f; changed = true; }
        if (input.KeyPressed(Key.R)) { (_aperture, _shutter, _sensitivity) = (1f, 1f / 125f, 100f); changed = true; }

        if (input.KeyPressed(Key.Space))
        {
            _ambient = !_ambient;
            Render.SetAmbientLight(OrangeRed, _ambient ? 200f : 0f);
            changed = true;
        }

        if (!changed) return;

        Render.SetLensExposure(_camera, _aperture, _shutter, _sensitivity);
        Ui.SetText(_text, Instructions());
    }

    private static string Instructions() =>
        $"Ambient light is {(_ambient ? "on" : "off")}\n"
        + $"Aperture: f/{_aperture:0}\n"
        + $"Shutter speed: 1/{1f / _shutter:0}s\n"
        + $"Sensitivity: ISO {_sensitivity:0}\n"
        + "\n\nControls\n---------------\n"
        + "Arrow keys - Move objects\n"
        + "Space - Toggle ambient light\n"
        + "1/2 - Decrease/Increase aperture\n"
        + "3/4 - Decrease/Increase shutter speed\n"
        + "5/6 - Decrease/Increase sensitivity\n"
        + "R - Reset exposure";

    private static Entity Light(EcsWorld ecs, LightKind kind, (float R, float G, float B) color, Transform at)
    {
        var light = Render.SpawnLight(new LightSettings
        {
            Kind = kind,
            Intensity = 100_000f,
            Color = color,
            Shadows = true,
            InnerAngle = 0.6f,
            OuterAngle = 0.8f,
        });
        ecs.Add(light, at);
        return light;
    }

    private static void Bulb(EcsWorld ecs, Entity light, AssetHandle mesh, (float R, float G, float B, float A) color, (float R, float G, float B, float A) glow, Transform at)
    {
        var bulb = ecs.Mesh(mesh, Render.CreateMaterial(new MaterialSettings { BaseColor = color, Emissive = glow }), at);
        ecs.SetParent(bulb, light);
    }

    private static (float R, float G, float B) Linear(byte r, byte g, byte b)
    {
        var color = Scene.Srgb8(r, g, b);
        return (color.R, color.G, color.B);
    }
}

/// <summary>Moves with the arrow keys, two units a second.</summary>
[Behavior]
public partial struct Movable
{
    [OnUpdate]
    public void Move(BehaviorContext ctx, ref Transform transform)
    {
        var input = ctx.Input;
        var x = (input.KeyDown(Key.ArrowRight) ? 1f : 0f) - (input.KeyDown(Key.ArrowLeft) ? 1f : 0f);
        var y = (input.KeyDown(Key.ArrowUp) ? 1f : 0f) - (input.KeyDown(Key.ArrowDown) ? 1f : 0f);
        transform.Translation += new Vec3(x, y, 0f) * (ctx.Time.Delta * 2f);
    }
}

/// <summary>The sun, which turns about Y half a radian a second.</summary>
[Behavior]
public partial struct Sun
{
    [OnUpdate]
    public void Turn(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(ctx.Time.Delta * 0.5f) * transform.Rotation;
}

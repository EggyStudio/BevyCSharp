// Bevy's random_sampling example, examples/math/random_sampling.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Maths;

// This example shows how to sample random points from primitive shapes. Points are scattered
// inside a translucent cube or on its surface, one or a hundred at a time, and the camera turns
// about the cube as the mouse is dragged.
internal static class RandomSampling
{
    private const string Controls = """
        Controls:
        M: Toggle between sampling boundary and interior.
        R: Restart (erase all samples).
        S: Add one random sample.
        D: Add 100 random samples.
        Rotate camera by holding left mouse and panning left/right.
        """;

    // Bevy's resources, the mode, the shape, its seeded randomness, and what a sample is drawn with.
    private static bool _interior = true;
    private static readonly Cuboid Shape = Cuboid.FromLength(2.9f);
    private static Random _random = new(19878367);
    private static (AssetHandle Mesh, AssetHandle Material) _point;
    private static bool _mousePressed;
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(Setup, "random_sampling.Setup");
        app.Update(HandleMouse, "random_sampling.HandleMouse");
        app.Update(HandleKeypress, "random_sampling.HandleKeypress");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        // Seeded so a run scatters the same points, as Bevy seeds its ChaCha8Rng, though .NET's
        // generator draws other points from its seed.
        (_random, _interior, _mousePressed) = (new Random(19878367), true, false);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 12f, 12f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, -2.5f, 0f));

        // The cube the points are taken from, translucent and seen from both sides.
        var size = Shape.Size;
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z),
            Render.CreateMaterial(new MaterialSettings { BaseColor = (0.2f, 0.1f, 0.6f, 0.3f), AlphaMode = AlphaMode.Blend, DoubleSided = true }),
            Transform.Identity);

        // Bevy's default point light, a million lumens, which the settings here leave at less.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Shadows = true });
        ecs.Add(light, Transform.At(4f, 8f, 4f));
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));

        _point = (
            Render.CreateMesh(MeshShape.Sphere, 0.03f),
            Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.8f, 0.8f, 1f), Metallic = 0.8f }));

        Ui.SpawnText(Controls, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // Bevy's handle_keypress.
    private static void HandleKeypress(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        if (input.KeyPressed(Key.R))
        {
            foreach (var sample in ecs.EntitiesWith<SamplePoint>().ToArray()) ecs.Despawn(sample);
        }

        if (input.KeyPressed(Key.S)) Spawn(ecs, Sample());

        // Bevy's interior_dist and boundary_dist, a hundred drawn from the one source.
        if (input.KeyPressed(Key.D))
        {
            for (var i = 0; i < 100; i++) Spawn(ecs, Sample());
        }

        if (input.KeyPressed(Key.M)) _interior = !_interior;
    }

    private static Vec3 Sample() => _interior ? Shape.SampleInterior(_random) : Shape.SampleBoundary(_random);

    private static void Spawn(EcsWorld ecs, Vec3 at)
    {
        var point = ecs.SpawnMesh(_point.Mesh, _point.Material, new Transform(at));
        ecs.Add(point, new SamplePoint());
    }

    // Bevy's handle_mouse, the camera turned about the cube's middle by the mouse's travel across
    // while the left button is held.
    private static void HandleMouse(BehaviorContext ctx)
    {
        foreach (var press in ctx.Read<MouseButtonInput>())
        {
            if (press.Button == MouseButton.Left) _mousePressed = press.State == ButtonState.Pressed;
        }

        if (!_mousePressed || ctx.Input.MouseDeltaX == 0f) return;

        var turn = Quat.FromRotationY(-ctx.Input.MouseDeltaX / 150f);
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        ctx.Ecs.Set(_camera, camera with { Translation = turn * camera.Translation, Rotation = turn * camera.Rotation });
    }
}

/// <summary>A point sampled from the shape, which R takes away again.</summary>
[Behavior]
public partial struct SamplePoint;

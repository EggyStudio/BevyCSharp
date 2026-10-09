// Bevy's many_cubes example, examples/stress_tests/many_cubes.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// A great many cubes, 1.6 million unless --instance-count says, on the inside of a sphere around a
// turning camera, on the faces of a cube with --layout cube, or packed tight with --layout dense,
// to measure culling, batching and drawing. --mesh-count and --material-texture-count draw them
// from more meshes and textures, --vary-material-data-per-instance gives each a material of its
// own, --no-frustum-culling draws them all, --shadows has the light cast shadows, --rotate-cubes
// and --animate-materials change them every frame, --motion-blur blurs them, and --benchmark
// turns the camera by the same step each frame.
//
// Bevy's --no-automatic-batching, --no-indirect-drawing and --no-cpu-culling put on components
// Bevy does not reflect, which the bridge cannot reach, and are left out.
internal static class ManyCubes
{
    private const int Width = 200, Height = 200;

    // Bevy's Args, read from the command line.
    private static string _layout = "sphere";
    private static bool _benchmark, _varyMaterialDataPerInstance, _noFrustumCulling, _shadows, _rotateCubes, _animateMaterials, _motionBlur;
    private static int _materialTextureCount, _meshCount = 1, _instanceCount = 1_600_000;

    private static Entity _camera;
    private static float _printing;
    private static int _meshes;
    private static readonly List<AssetHandle> Materials = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        _layout = Option(arguments, "--layout") ?? "sphere";
        (_benchmark, _varyMaterialDataPerInstance, _noFrustumCulling, _shadows, _rotateCubes, _animateMaterials, _motionBlur) = (
            arguments.Contains("--benchmark"), arguments.Contains("--vary-material-data-per-instance"), arguments.Contains("--no-frustum-culling"),
            arguments.Contains("--shadows"), arguments.Contains("--rotate-cubes"), arguments.Contains("--animate-materials"), arguments.Contains("--motion-blur"));
        _materialTextureCount = int.TryParse(Option(arguments, "--material-texture-count"), out var textures) ? textures : 0;
        _meshCount = int.TryParse(Option(arguments, "--mesh-count"), out var meshes) ? meshes : 1;
        _instanceCount = int.TryParse(Option(arguments, "--instance-count"), out var instances) ? instances : 1_600_000;
        (_printing, _meshes) = (0f, 0);
        Materials.Clear();

        app.Startup(Setup, "many_cubes.Setup");
        app.Update(PrintMeshCount, "many_cubes.PrintMeshCount");
        if (_layout != "dense") app.Update(MoveCamera, "many_cubes.MoveCamera");
        if (_animateMaterials) app.Update(UpdateMaterials, "many_cubes.UpdateMaterials");
    }

    private static string? Option(string[] arguments, string name)
    {
        var at = Array.IndexOf(arguments, name);
        return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : null;
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;
        var meshes = InitMeshes();
        var materials = InitMaterials(InitTextures());

        // Seeded so a run draws the same as the last, as Bevy seeds its own, though .NET's numbers
        // are not ChaCha's.
        var materialRandom = new Random(42);
        (AssetHandle Mesh, Transform Transform) PickMesh() => meshes[materialRandom.Next(meshes.Length)];
        AssetHandle PickMaterial() => materials[materialRandom.Next(materials.Length)];

        var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f) });
        switch (_layout)
        {
            case "cube":
            {
                const float Scale = 2.5f;
                // Four cubes a step, less one row and column in ten to break up any moiré, so the
                // grid is sized to come to the count asked for.
                var factor = 5f / 9f * MathF.Sqrt(_instanceCount) / (MathF.Sqrt(Height) * MathF.Sqrt(Width));
                var (across, up) = ((int)MathF.Ceiling(Width * factor), (int)MathF.Ceiling(Height * factor));
                for (var x = 0; x < across; x++)
                {
                    for (var y = 0; y < up; y++)
                    {
                        if (x % 10 == 0 || y % 10 == 0) continue;
                        Cube(ecs, PickMesh(), PickMaterial(), Transform.At(x * Scale, y * Scale, 0f));
                        Cube(ecs, PickMesh(), PickMaterial(), Transform.At(x * Scale, up * Scale, y * Scale));
                        Cube(ecs, PickMesh(), PickMaterial(), Transform.At(x * Scale, 0f, y * Scale));
                        Cube(ecs, PickMesh(), PickMaterial(), Transform.At(0f, x * Scale, y * Scale));
                    }
                }

                var center = 0.5f * Scale * new Vec3(across, up, across);
                _camera = Camera(ecs, Transform.At(center.X, center.Y, center.Z));
                Box(ecs, white, 2f * 1.1f * center, center);
                break;
            }

            case "dense":
            {
                var size = MathF.Round(MathF.Cbrt(_instanceCount));
                const float Gap = 1.25f;
                for (var i = 0; i < _instanceCount; i++)
                {
                    var (x, y, z) = (i % size, i / size % size, i / (size * size));
                    Cube(ecs, PickMesh(), PickMaterial(), Transform.At(x * Gap, y * Gap, z * Gap));
                }

                _camera = Camera(ecs, Transform.LookingAt(new Vec3(100f, 90f, 100f), new Vec3(0f, -10f, 0f), Vec3.UnitY));
                break;
            }

            default:
            {
                // A spiral over the sphere, which keeps about as many cubes in view whichever way
                // the camera looks, in doubles so the spread has no seams.
                var radius = Width * 2.5;
                var goldenRatio = 0.5 * (1.0 + Math.Sqrt(5.0));
                const double Epsilon = 0.36;
                for (var i = 0; i < _instanceCount; i++)
                {
                    var theta = Math.PI * 2.0 * (i / goldenRatio);
                    var phi = Math.Acos(1.0 - 2.0 * (i + Epsilon) / (_instanceCount - 1.0 + 2.0 * Epsilon));
                    var at = new Vec3((float)(Math.Cos(theta) * Math.Sin(phi) * radius), (float)(Math.Sin(theta) * Math.Sin(phi) * radius), (float)(Math.Cos(phi) * radius));
                    var (mesh, shape) = PickMesh();
                    var place = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY);
                    Cube(ecs, (mesh, shape), PickMaterial(), new Transform(place.Translation, place.Rotation * shape.Rotation, shape.Scale));
                }

                _camera = Camera(ecs, Transform.Identity);
                Box(ecs, white, new Vec3((float)(radius * 2.2)), Vec3.Zero);
                break;
            }
        }

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = _shadows });
        ecs.Add(light, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, -1f), Vec3.UnitY));
    }

    private static Entity Camera(EcsWorld ecs, Transform at)
    {
        var camera = ecs.SpawnCamera3d(at);
        if (_motionBlur) ecs.Insert<MotionBlurRef>(camera).ShutterAngle = 3f;
        return camera;
    }

    // An inside-out box around the cubes for shadows to fall on, casting none itself.
    private static void Box(EcsWorld ecs, AssetHandle material, Vec3 size, Vec3 at)
    {
        var box = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z), material, new Transform(at, Quat.Identity, new Vec3(-1f)));
        ecs.Insert<NotShadowCasterRef>(box);
        _meshes++;
    }

    private static void Cube(EcsWorld ecs, (AssetHandle Mesh, Transform Shape) mesh, AssetHandle material, Transform at)
    {
        var cube = ecs.SpawnMesh(mesh.Mesh, material, at);
        if (_noFrustumCulling) ecs.Insert<NoFrustumCullingRef>(cube);
        if (_rotateCubes) ecs.Add(cube, new Spinning());
        _meshes++;
    }

    // Single-pixel textures of seeded colors, as many as asked for.
    private static AssetHandle[] InitTextures()
    {
        var random = new Random(42);
        return [.. Enumerable.Range(0, _materialTextureCount).Select(_ =>
            Render.CreateImage([(byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 255], 1, 1))];
    }

    // A white material on the first texture, then as many of seeded colors and textures as there
    // are textures, or one for each cube with --vary-material-data-per-instance.
    private static AssetHandle[] InitMaterials(AssetHandle[] textures)
    {
        var capacity = Math.Max(_varyMaterialDataPerInstance ? _instanceCount : _materialTextureCount, 1);
        Materials.Add(Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), BaseColorTexture = textures.FirstOrDefault() }));

        var (colors, picks) = (new Random(42), new Random(42));
        while (Materials.Count < capacity)
        {
            Materials.Add(Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = Color.FromSrgb8((byte)colors.Next(256), (byte)colors.Next(256), (byte)colors.Next(256)),
                BaseColorTexture = textures.Length > 0 ? textures[picks.Next(textures.Length)] : AssetHandle.None,
            }));
        }

        return [.. Materials];
    }

    // As many meshes as asked for, each a shape of fifteen in turn at a seeded size, with how each
    // is turned so a flat one faces the way a cube's face would.
    private static (AssetHandle, Transform)[] InitMeshes()
    {
        var random = new Random(42);
        var facing = Transform.LookingAt(Vec3.Zero, Vec3.UnitZ, Vec3.UnitY);
        var meshes = new (AssetHandle, Transform)[Math.Max(_meshCount, 1)];
        for (var variant = 0; variant < meshes.Length; variant++)
        {
            var radius = 0.25f + random.NextSingle() * 0.5f;
            var kind = variant % 15;
            meshes[variant] = kind switch
            {
                0 => (Render.CreateMesh(MeshShape.Cuboid, 2f * radius, 2f * radius, 2f * radius), Transform.Identity),
                1 => (Render.CreateMesh(MeshShape.Capsule, radius, 2f * radius), Transform.Identity),
                2 => (Render.CreateMesh(MeshShape.Circle, radius), facing),
                3 => (Render.CreateMesh(Triangle(radius)), facing),
                4 => (Render.CreateMesh(MeshShape.Rectangle, 2f * radius, 2f * radius), facing),
                >= 5 and <= 8 => (Render.CreateMesh(MeshShape.RegularPolygon, radius, kind), facing),
                9 => (Render.CreateMesh(MeshShape.Cylinder, radius, 2f * radius), Transform.Identity),
                10 => (Render.CreateMesh(MeshShape.Ellipse, radius, 0.5f * radius), facing),
                // A plane facing away along Z rather than up, as Bevy builds this one.
                11 => (Render.CreateMesh(MeshShape.Plane, radius, radius), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One)),
                12 => (Render.CreateMesh(MeshShape.Sphere, radius), Transform.Identity),
                // Looking up, which Bevy asks for along its up and settles by turning about X.
                13 => (Render.CreateMesh(MeshShape.Torus, 0.5f * radius, 1.5f * radius), new Transform(Vec3.Zero, Quat.FromRotationX(MathF.PI / 2f), Vec3.One)),
                _ => (Render.CreateMesh(MeshShape.Capsule2d, radius, 2f * radius), facing),
            };
        }

        return meshes;
    }

    // An equilateral triangle on a circle of a radius, its first corner along X.
    private static MeshData Triangle(float radius)
    {
        var corners = Enumerable.Range(0, 3)
            .Select(i => new Vec3(MathF.Cos(i * MathF.Tau / 3f) * radius, MathF.Sin(i * MathF.Tau / 3f) * radius, 0f))
            .ToArray();
        return new MeshData { Positions = corners, Normals = [Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ], Indices = [0, 1, 2] };
    }

    // Turned about its own Z and X, by a fixed step with --benchmark.
    private static void MoveCamera(BehaviorContext ctx)
    {
        var delta = 0.15f * (_benchmark ? 1f / 60f : ctx.Time.Delta);
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        camera.Rotation = Quat.FromRotationX(delta) * (Quat.FromRotationZ(delta) * camera.Rotation);
        ctx.Ecs.Set(_camera, camera);
    }

    // The meshes and those a view saw, once a second. The seen are counted over everything that
    // may be seen, which here is the meshes and the light.
    private static void PrintMeshCount(BehaviorContext ctx)
    {
        _printing += ctx.Time.Delta;
        if (_printing < 1f) return;

        _printing -= 1f;
        var visible = 0;
        foreach (var row in ctx.Ecs.Query<ViewVisibility>(markChanged: false))
            if (row.Component.IsVisible) visible++;
        Console.WriteLine($"Meshes: {_meshes} - Visible Meshes {visible}");
    }

    // Every material's hue moved on with the clock, by the fast way Bevy's example works one out.
    private static void UpdateMaterials(BehaviorContext ctx)
    {
        var elapsed = ctx.Time.Elapsed;
        for (var i = 0; i < Materials.Count; i++)
        {
            var hue = ((elapsed + i * 0.005f) % 1f + 1f) % 1f;
            var (r, g, b) = (MathF.Abs(hue * 6f - 3f) - 1f, 2f - MathF.Abs(hue * 6f - 2f), 2f - MathF.Abs(hue * 6f - 4f));
            Render.WriteMaterial(Materials[i], new MaterialSettings { BaseColor = (r, g, b, 1f) });
        }
    }
}

/// <summary>A cube that turns about the world's up, fast, with --rotate-cubes.</summary>
[Behavior]
public partial struct Spinning
{
    /// <summary>Turned a little each frame.</summary>
    [OnUpdate]
    public void RotateCubes(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(10f * ctx.Time.Delta) * transform.Rotation;
}

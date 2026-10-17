// Bevy's decal example, examples/3d/decal.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Three forward decals of one checker texture over a white floor scattered with turned cubes. Blue
// and opaque, green and blended, and red and added, each drawn over the last by its depth bias, and
// each laid over the cubes' sides as over the floor. A decal reads the surfaces it is drawn on from
// the camera's depth prepass, and the camera flies as Bevy's free camera does.
//
// Bevy's turns its cubes by a seeded ChaCha8 generator, and this by .NET's, seeded too, so each
// run is the same and the cubes are turned otherwise.
internal static class DecalExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var checker = AssetServer.Load(AssetKind.Image, "textures/uv_checker_bw.png");

        void SpawnDecal(string name, Color color, AlphaMode mode, float depthBias, Vec3 at)
        {
            var material = Render.CreateDecalMaterial(
                new MaterialSettings { BaseColorTexture = checker, BaseColor = color, AlphaMode = mode },
                depthFadeFactor: 1f, depthBias: depthBias);
            var decal = ecs.Spawn();
            ecs.SetName(decal, name);
            ecs.Add(decal, new Transform(at, Quat.Identity, new Vec3(4f)));
            Render.SetForwardDecal(decal, material);
        }

        SpawnDecal("Decal Blue", Color.FromSrgb(0.3f, 0.5f, 0.8f), AlphaMode.Opaque, 0f, new Vec3(-1f, 0f, -1f));
        SpawnDecal("Decal Green", Color.FromSrgb(0f, 1f, 0f, 0.5f), AlphaMode.Blend, 2f, new Vec3(1f, 0f, -1f));
        SpawnDecal("Decal Red", Color.FromSrgb(1f, 0f, 0f, 0.5f), AlphaMode.Add, 4f, new Vec3(0f, 0f, 1f));

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(2f, 9.5f, 2.5f), Vec3.Zero, Vec3.UnitY));
        ecs.SetName(camera, "Camera");
        ecs.Insert<DepthPrepassRef>(camera);
        ecs.Add(camera, new FreeCamera());

        var white = Render.CreateMaterial(Color.White);
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Rectangle, 10f, 10f), white,
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        // A hundred cubes turned every way, to show a decal laid over what is not flat.
        const int Count = 10;
        var random = new Random(1987836746);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.6f, 0.6f, 0.6f);
        for (var i = 0; i < Count; i++)
            for (var j = 0; j < Count; j++)
            {
                var axis = new Vec3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                var degrees = random.Next(0, 360);
                var turn = axis.LengthSquared > 0f ? Quat.FromAxisAngle(axis.Normalized, degrees * MathF.PI / 180f) : Quat.Identity;
                ecs.SpawnMesh(cube, white, new Transform(new Vec3((-Count + 1) / 2f + i, -0.2f, (-Count + 1) / 2f + j), turn, Vec3.One));
            }

        var light = ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ecs.SetName(light, "Light");
    }, "decal.Setup");
}

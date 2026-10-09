// Bevy's order_independent_transparency example, examples/3d/order_independent_transparency.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A simple 3D scene showing how alpha blending can break and how order independent transparency
// (OIT) can fix it. T toggles OIT and C cycles through four test scenes.
//
// Bevy's example draws everything on render layer 1, which says nothing about how it is drawn and
// is left at the default layer here.
internal static class OrderIndependentTransparency
{
    private static readonly List<Entity> Spawned = [];
    private static Entity _camera, _text;
    private static bool _oit;
    private static int _scene;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Spawned.Clear();
            (_oit, _scene) = (true, 0);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 10f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Msaa = 1 });
            Shaders.SetPrepass(_camera, depth: true);
            Render.SetSortedTransparency(_camera, true);

            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f));
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

            Spheres(ecs);
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.T))
            {
                _oit = !_oit;
                Render.SetSortedTransparency(_camera, _oit);
                Ui.SetText(_text, Text());
            }

            if (!ctx.Input.KeyPressed(Key.C)) return;

            foreach (var entity in Spawned) ctx.Ecs.Despawn(entity);
            Spawned.Clear();

            _scene = (_scene + 1) % 4;
            switch (_scene)
            {
                case 0: Spheres(ctx.Ecs); break;
                case 1: Quads(ctx.Ecs); break;
                case 2: OcclusionTest(ctx.Ecs); break;
                default: AutoInstancingTest(ctx.Ecs); break;
            }
        }, "order_independent_transparency.Update");
    }

    private static readonly (float R, float G, float B) Red = (1f, 0f, 0f), Green = (0f, 0.5f, 0f), Blue = (0f, 0f, 1f), Yellow = (1f, 1f, 0f);

    // A color in sRGB with an alpha, blended.
    private static AssetHandle Blend((float R, float G, float B) color, float alpha) =>
        Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(color.R, color.G, color.B, alpha), AlphaMode = AlphaMode.Blend });

    private static void Spawn(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Transform at) =>
        Spawned.Add(ecs.SpawnMesh(mesh, material, at));

    private static void Spheres(EcsWorld ecs)
    {
        var sphere = Render.CreateMesh(MeshShape.Sphere, 2f);
        Spawn(ecs, sphere, Blend(Red, 0.25f), Transform.At(-1f, 0.75f, 0f));
        Spawn(ecs, sphere, Blend(Green, 0.25f), Transform.At(0f, -0.75f, 0f));
        Spawn(ecs, sphere, Blend(Blue, 0.25f), Transform.At(1f, 0.75f, 0f));
    }

    private static void Quads(EcsWorld ecs)
    {
        var quad = Render.CreateMesh(MeshShape.Rectangle, 3f, 3f);
        var turn = Quat.FromRotationY(0.5f);
        Transform At(float x, float y, float z) => new(turn * new Vec3(x, y, z), turn, Vec3.One);

        Spawn(ecs, quad, Blend(Red, 0.5f), At(1f, -0.1f, 0f));
        Spawn(ecs, quad, Blend(Blue, 0.8f), At(0.5f, 0.2f, -0.5f));
        Spawn(ecs, quad, Blend((0f, 1f, 0f), 0.5f), At(0f, 0.4f, -1f));
        Spawn(ecs, quad, Blend(Yellow, 0.3f), At(-0.5f, 0.6f, -1.1f));
        Spawn(ecs, quad, Blend(Blue, 0.2f), At(-0.8f, 0.8f, -1.2f));
    }

    // A transparent sphere behind a cube, in front of one, and between two.
    private static void OcclusionTest(EcsWorld ecs)
    {
        var sphere = Render.CreateMesh(MeshShape.Sphere, 1f);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var solid = Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f));

        Spawn(ecs, cube, solid, Transform.At(-2.5f, 0f, 2f));
        Spawn(ecs, sphere, Blend(Red, 0.5f), Transform.At(-2.5f, 0f, 0f));
        Spawn(ecs, cube, solid, Transform.At(-2.5f, 0f, 1f));
        Spawn(ecs, sphere, Blend(Red, 0.5f), Transform.At(0f, 0f, 0f));
        Spawn(ecs, cube, solid, Transform.At(2.5f, 0f, -2f));
        Spawn(ecs, sphere, Blend(Red, 0.5f), Transform.At(2.5f, 0f, 0f));
    }

    // Twenty-seven cubes sharing a mesh and a material, which Bevy draws as one instanced batch.
    private static void AutoInstancingTest(EcsWorld ecs)
    {
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var material = Render.CreateMaterial(new MaterialSettings { AlphaMode = AlphaMode.Blend, BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/slice_square.png") });
        for (var z = -1; z <= 1; z++)
            for (var y = -1; y <= 1; y++)
                for (var x = -1; x <= 1; x++)
                    Spawn(ecs, cube, material, Transform.At(x * 2f, y * 2f, z * 2f));
    }

    private static string Text() => $"Press T to toggle OIT\n{(_oit ? "OIT Enabled" : "OIT disabled")}\nPress C to cycle test scenes";
}

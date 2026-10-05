using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows meshes made by code while the app runs rather than loaded: a cone generated on a worker and
// added once it is done, and a torus put in place of an empty mesh a moment later, each drawn by an
// entity that had its mesh before the mesh had anything in it.
internal static class GeneratedAssets
{
    private static Task<MeshData>? _cone;
    private static AssetHandle _reserved;
    private static Entity _coneEntity;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.Camera(Transform.At(0f, 0f, 5f));
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(-1f, -1f, -1f), Vec3.UnitY));

            var material = Render.CreateMaterial(new MaterialSettings());

            // The cone's vertices are worked out on a worker, as Bevy's add_async does, and made a
            // mesh and handed to the entity when they are done, since a mesh is made in a system.
            _cone = Task.Run(() => Cone(1f, 2f, 32));
            _coneEntity = ecs.Spawn();
            ecs.Add(_coneEntity, Transform.At(-2f, 0f, 0f));
            Render.SetMaterial(ecs, _coneEntity, material);

            // The torus's entity draws a mesh with nothing in it yet, as Bevy's reserved handle is,
            // which the next frame fills in.
            _reserved = Render.CreateMesh(MeshShape.Triangle, 0f);
            ecs.Mesh(_reserved, material, new Transform(new Vec3(2f, 0f, 0f), Quat.FromRotationX(50f * MathF.PI / 180f), Vec3.One));
        }, "generated_assets.Setup");

        var generated = false;
        app.Update(ctx =>
        {
            if (!generated)
            {
                Render.RebuildMesh(_reserved, MeshShape.Torus, 0.8f, 1.2f);
                generated = true;
            }

            if (_cone is { IsCompleted: true } done)
            {
                Render.SetMesh(ctx.Ecs, _coneEntity, Render.CreateMesh(done.Result));
                _cone = null;
            }
        }, "generated_assets.GenerateMesh");
    }

    // A cone standing on its base, its tip up the Y axis, as Bevy's Cone is: a ring of sides
    // meeting at the tip, and a disc beneath, each face its own vertices so it is lit flat.
    private static MeshData Cone(float radius, float height, int segments)
    {
        var positions = new List<Vec3>();
        var indices = new List<uint>();
        var (top, bottom) = (height / 2f, -height / 2f);

        Vec3 Rim(int i)
        {
            var angle = 2f * MathF.PI * i / segments;
            return new Vec3(MathF.Cos(angle) * radius, bottom, -MathF.Sin(angle) * radius);
        }

        for (var i = 0; i < segments; i++)
        {
            var at = (uint)positions.Count;
            positions.AddRange([Rim(i), Rim(i + 1), new Vec3(0f, top, 0f)]);
            indices.AddRange([at, at + 1, at + 2]);

            at = (uint)positions.Count;
            positions.AddRange([Rim(i + 1), Rim(i), new Vec3(0f, bottom, 0f)]);
            indices.AddRange([at, at + 1, at + 2]);
        }

        return new MeshData { Positions = [.. positions], Indices = [.. indices] };
    }
}

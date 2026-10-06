// Bevy's custom_skinned_mesh example, examples/animation/custom_skinned_mesh.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Skinned mesh example with mesh and joints data defined in code. Ten strips, each skinned to two
// joints, the upper of which moves in its own way, rotating, stretching or sliding, with its axes
// drawn. Taken by Bevy from the glTF tutorial's simple skin.
internal static class CustomSkinnedMesh
{
    // Seeded, so a capture is the same each time, as Bevy seeds its own; .NET's generator draws
    // other colors from the seed than Bevy's ChaCha8Rng does.
    private static Random _rng = new(42);

    public static void Build(App app)
    {
        app.Startup(Setup, "custom_skinned_mesh.Setup");
    }

    // Bevy's setup, a mesh and a skeleton of two joints for it, the second marked to be animated.
    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _rng = new Random(42);
        Render.SetAmbientLight((1f, 1f, 1f), 3000f);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(2.5f, 2.5f, 9f), Vec3.Zero, Vec3.UnitY));

        // The inverse bindposes of a skeleton of two joints.
        var skin = Render.CreateSkin([Transform.At(-0.5f, -1f, 0f), Transform.At(-0.5f, -1f, 0f)]);

        // A strip one wide and two high, its texture's left half, facing the camera.
        var mesh = Render.CreateMesh(new MeshData
        {
            Positions =
            [
                new(0f, 0f, 0f), new(1f, 0f, 0f), new(0f, 0.5f, 0f), new(1f, 0.5f, 0f), new(0f, 1f, 0f),
                new(1f, 1f, 0f), new(0f, 1.5f, 0f), new(1f, 1.5f, 0f), new(0f, 2f, 0f), new(1f, 2f, 0f),
            ],
            Uvs = [0f, 0f, 0.5f, 0f, 0f, 0.25f, 0.5f, 0.25f, 0f, 0.5f, 0.5f, 0.5f, 0f, 0.75f, 0.5f, 0.75f, 0f, 1f, 0.5f, 1f],
            Normals = [.. Enumerable.Repeat(Vec3.UnitZ, 10)],
            Indices = [0, 1, 3, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 7, 4, 7, 6, 6, 7, 9, 6, 9, 8],
        });

        // Each vertex's four joints, indices into the joints SetSkin is given, and how much each
        // moves it, adding up to one. The foot follows the first joint alone and the head the
        // second, and the rows between share them.
        Render.SetMeshJoints(mesh,
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0],
            [
                1.00f, 0.00f, 0f, 0f, 1.00f, 0.00f, 0f, 0f, 0.75f, 0.25f, 0f, 0f, 0.75f, 0.25f, 0f, 0f, 0.50f, 0.50f, 0f, 0f,
                0.50f, 0.50f, 0f, 0f, 0.25f, 0.75f, 0f, 0f, 0.25f, 0.75f, 0f, 0f, 0.00f, 1.00f, 0f, 0f, 0.00f, 1.00f, 0f, 0f,
            ]);

        var checker = AssetServer.Load(AssetKind.Image, "textures/uv_checker_bw.png");
        for (var i = -5; i < 5; i++)
        {
            // The quads moved back a little, so they do not fight over depth or hide the axes.
            var foot = ecs.Spawn();
            ecs.Add(foot, Transform.At(i * 1.5f, 0f, -MathF.Abs(i * 0.01f)));
            var head = ecs.Spawn();
            ecs.Add(head, Transform.Identity);
            ecs.Add(head, new AnimatedJoint { Index = i });
            ecs.SetParent(head, foot);

            // The skinned mesh's own transform moves nothing, since its joints place its vertices.
            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (_rng.NextSingle(), _rng.NextSingle(), _rng.NextSingle(), 1f),
                BaseColorTexture = checker,
            });
            var strip = ecs.SpawnMesh(mesh, material, Transform.Identity);
            Render.SetSkin(strip, skin, [foot, head]);
        }
    }
}

/// <summary>A joint animated by <see cref="JointAnimation"/>, each by its own motion.</summary>
[Behavior]
public partial struct AnimatedJoint
{
    /// <summary>Which strip's joint, from -5 to 4, choosing its motion.</summary>
    public int Index;

    /// <summary>Bevy's joint_animation, the joint moved by its index, and its axes drawn.</summary>
    [OnUpdate]
    public void JointAnimation(BehaviorContext ctx, ref Transform transform)
    {
        var t = ctx.Time.Elapsed;
        switch (Index)
        {
            case -5: transform.Rotation = Quat.FromRotationX(MathF.PI / 2f * MathF.Sin(t)); break;
            case -4: transform.Rotation = Quat.FromRotationY(MathF.PI / 2f * MathF.Sin(t)); break;
            case -3: transform.Rotation = Quat.FromRotationZ(MathF.PI / 2f * MathF.Sin(t)); break;
            case -2: transform.Scale.X = MathF.Sin(t) + 1f; break;
            case -1: transform.Scale.Y = MathF.Sin(t) + 1f; break;
            case 0:
                transform.Translation.X = 0.5f * MathF.Sin(t);
                transform.Translation.Y = MathF.Cos(t);
                break;
            case 1:
                transform.Translation.Y = MathF.Sin(t);
                transform.Translation.Z = MathF.Cos(t);
                break;
            case 2: transform.Translation.X = MathF.Sin(t); break;
            case 3:
                transform.Translation.Y = MathF.Sin(t);
                transform.Scale.X = MathF.Sin(t) + 1f;
                break;
        }

        // The joint's axes, beside the strip it moves, behind what stands in front of them.
        var axis = transform;
        axis.Translation.X += Index * 1.5f;
        Gizmos.Axes(axis, 1f, inFront: false);
    }
}

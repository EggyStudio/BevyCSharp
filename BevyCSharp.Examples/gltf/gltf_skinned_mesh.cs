// Bevy's gltf_skinned_mesh example, examples/gltf/gltf_skinned_mesh.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gltf;

// A skinned mesh from a glTF file, a strip on two joints, bent by turning its second joint to and
// fro.
internal static class GltfSkinnedMesh
{
    private static Entity _scene;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render.SetAmbientLight((1f, 1f, 1f), 750f);
            ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), new Vec3(0f, 1f, 0f), Vec3.UnitY));
            _scene = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene("models/SimpleSkin/SimpleSkin.gltf"));
        }, "gltf_skinned_mesh.Setup");

        app.Update(JointAnimation, "gltf_skinned_mesh.JointAnimation");
    }

    // Each skinned mesh's node, whose second child is the first joint, and that joint's first child
    // the second joint, which swings a quarter turn either way.
    private static void JointAnimation(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.Descendants(_scene))
        {
            if (ecs.Get<SkinnedMeshRef>(entity) is null) continue;
            var meshNode = ecs.ParentOf(entity);
            var nodeChildren = ecs.ChildrenOf(meshNode);
            if (nodeChildren.Length < 2) continue;
            var firstJointChildren = ecs.ChildrenOf(nodeChildren[1]);
            if (firstJointChildren.Length < 1) continue;

            var secondJoint = firstJointChildren[0];
            var at = ecs.GetOrDefault<Transform>(secondJoint);
            ecs.Set(secondJoint, at with { Rotation = Quat.FromRotationZ(MathF.PI / 2f * MathF.Sin(ctx.Time.Elapsed)) });
        }
    }
}

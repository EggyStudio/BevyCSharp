using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a mesh made in code and skinned to joint entities, bent as its joints move.</summary>
/// <remarks>
/// The mesh is the strip Bevy's <c>custom_skinned_mesh</c> example builds, a one by two rectangle of
/// ten vertices, its foot following the first joint alone and its head the second alone, and the
/// rows between them shared. Drawn unlit and white on black, so a pixel is the mesh or the ground.
/// </remarks>
[Collection("engine")]
public sealed class SkinTests
{
    // The strip's vertices, two a row from the foot up, and the joints and weights of each row.
    private static readonly Vec3[] Positions =
        [.. Enumerable.Range(0, 10).Select(i => new Vec3(i % 2, (i / 2) * 0.5f, 0f))];
    private static readonly ushort[] Joints =
        [.. Enumerable.Range(0, 10).SelectMany(i => i < 2 ? new ushort[] { 0, 0, 0, 0 } : [0, 1, 0, 0])];
    private static readonly float[] Weights =
        [.. Enumerable.Range(0, 10).SelectMany(i => new[] { 1f - (i / 2) * 0.25f, (i / 2) * 0.25f, 0f, 0f })];

    private static MeshData Strip() => new()
    {
        Positions = Positions,
        Normals = [.. Enumerable.Repeat(Vec3.UnitZ, 10)],
        Indices = [0, 1, 3, 0, 3, 2, 2, 3, 5, 2, 5, 4, 4, 5, 7, 4, 7, 6, 6, 7, 9, 6, 9, 8],
    };

    /// <summary>
    /// A skinned strip stands where its joints and bindposes put it, and moving its head's joint to
    /// the side carries the head with it while the foot stays.
    /// </summary>
    [SkippableFact]
    public void MovingAJointBendsTheMeshItSkins()
    {
        Needs.Renderer();

        var head = Entity.None;
        var meshInfo = default(MeshInfo);

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 4f), Vec3.Zero, Vec3.UnitY));

                var mesh = Render.CreateMesh(Strip());
                Render.SetMeshJoints(mesh, Joints, Weights);
                Assert.True(Render.TryGetMeshInfo(mesh, out meshInfo));

                // Both bindposes are the strip's middle, so the strip stands centered on the
                // first joint, at the origin, one wide and two high.
                var skin = Render.CreateSkin([Transform.At(-0.5f, -1f, 0f), Transform.At(-0.5f, -1f, 0f)]);
                var foot = ecs.Spawn();
                ecs.Add(foot, Transform.Identity);
                head = ecs.Spawn();
                ecs.Add(head, Transform.Identity);
                ecs.SetParent(head, foot);

                var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Unlit = true });
                var strip = ecs.SpawnMesh(mesh, white, Transform.Identity);
                Render.SetSkin(strip, skin, [foot, head]);
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("standing")
            .Do("moving the head aside", world => world.Resource<EcsWorld>().Set(head, Transform.At(1f, 0f, 0f)))
            .Wait(10)
            .Capture("bent")
            .Go();

        Assert.True((meshInfo.Attributes & (MeshAttributes.Joints | MeshAttributes.Weights)) == (MeshAttributes.Joints | MeshAttributes.Weights),
            $"the mesh holds {meshInfo.Attributes}");

        // Ninety-six pixels across show about 3.3 units at the strip's distance, so the strip's
        // half-unit sides are fifteen pixels from the middle and its top twenty-nine above it.
        var (standing, bent) = (run.Picture("standing"), run.Picture("bent"));
        bool Lit(CapturedImage picture, uint x, uint y) => picture.At(x, y).R > 128;

        // Near the top on the left of the middle, where the head stood and left, and to the right
        // of the strip, where it went. The foot, at the bottom, stays.
        Assert.True(Lit(standing, 37, 22) && !Lit(bent, 37, 22), $"the head's place was {standing.At(37, 22)} standing and {bent.At(37, 22)} bent");
        Assert.True(!Lit(standing, 74, 22) && Lit(bent, 74, 22), $"to its right was {standing.At(74, 22)} standing and {bent.At(74, 22)} bent");
        Assert.True(Lit(standing, 48, 74) && Lit(bent, 48, 74), $"the foot was {standing.At(48, 74)} standing and {bent.At(48, 74)} bent");
    }

    /// <summary>
    /// Joints for another number of vertices than the mesh holds, or not four a vertex, are refused
    /// with the reason, and a skin needs a bindpose and a joint.
    /// </summary>
    [SkippableFact]
    public void JointsThatDoNotFitTheMeshAreRefused()
    {
        Needs.Renderer();

        Exception? fewer = null, uneven = null, noPose = null, noJoint = null;
        using var app = new App(Config.OffscreenFor(64, 64, frames: 2));
        app.AddPlugin(new EnginePlugin());
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var mesh = Render.CreateMesh(Strip());
            fewer = Record.Exception(() => Render.SetMeshJoints(mesh, Joints.AsSpan(0, 8), Weights.AsSpan(0, 8)));
            uneven = Record.Exception(() => Render.SetMeshJoints(mesh, Joints.AsSpan(0, 6), Weights.AsSpan(0, 6)));
            noPose = Record.Exception(() => Render.CreateSkin([]));
            var skin = Render.CreateSkin([Transform.Identity]);
            noJoint = Record.Exception(() => Render.SetSkin(world.Resource<EcsWorld>().Spawn(), skin, []));
        }, "Test.Refusals"));
        Assert.Equal(0, app.Run());

        Assert.Contains("which", Assert.IsType<ArgumentException>(fewer).Message);
        Assert.Contains("four of each", Assert.IsType<ArgumentException>(uneven).Message);
        Assert.IsType<ArgumentException>(noPose);
        Assert.IsType<ArgumentException>(noJoint);
    }
}

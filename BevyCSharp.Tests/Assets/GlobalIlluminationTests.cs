using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Global illumination's world probes, traced through the scene's distance field: the qualities'
/// cascades and the far cascade's cells, the colors the field paints a ray's hit by, the light a
/// probe gathers on each face read back from the GPU, a probe inside a mesh passed over, and a far
/// cascade holding a wall the counted ones do not reach.
/// </summary>
[Collection("engine")]
public sealed class GlobalIlluminationTests
{
    /// <summary>Each quality traces 3DEngine's cascades and directions, no more than the field's counted cascades, and one more in its far cascade.</summary>
    [Fact]
    public void EachQualityTracesItsCascadesAndDirections()
    {
        Assert.Equal([8, 8], WorldProbes.TexelsAt(GlobalIllumination.Low));
        Assert.Equal([8, 8, 16], WorldProbes.TexelsAt(GlobalIllumination.Medium));
        Assert.Equal([8, 16, 16, 16], WorldProbes.TexelsAt(GlobalIllumination.High));
        Assert.Empty(WorldProbes.TexelsAt(GlobalIllumination.Off));

        var three = new SceneFieldPlan(3, 0.25f, 1);
        Assert.Equal(3, WorldProbes.CascadesFor(GlobalIllumination.High, three));
        Assert.Equal([8, 16, 16], WorldProbes.TexelsFor(GlobalIllumination.High, 3, far: false));

        // The far cascade's probes follow the cascade before's directions.
        var far = new SceneFieldPlan(4, 0.25f, 1, farCell: 7.5f);
        Assert.Equal(3, WorldProbes.CascadesFor(GlobalIllumination.Low, far));
        Assert.Equal([8, 8, 8], WorldProbes.TexelsFor(GlobalIllumination.Low, 3, far: true));
        Assert.Equal(5, WorldProbes.CascadesFor(GlobalIllumination.High, far));
        Assert.Equal([8, 16, 16, 16, 16], WorldProbes.TexelsFor(GlobalIllumination.High, 5, far: true));

        // A probe sends 256 rays at every quality, four a texel of 8 a side and one of 16.
        Assert.All([8, 16], n => Assert.Equal(256, n * n * WorldProbes.RaysAt(n)));
    }

    /// <summary>
    /// A far cascade's cells reach its distance ahead of the eye from its middle's lead, rounded up to
    /// a sixteenth of twice the last counted cascade's cell and never finer than that, and its probes
    /// stand in it.
    /// </summary>
    [Fact]
    public void AFarCascadesCellsReachItsDistanceAheadOfTheEye()
    {
        // 400 over 0.85 of 64 cells is 7.35, rounded up to a quarter, a sixteenth of four.
        Assert.Equal(7.5f, SceneFieldPlan.FarCellFor(400f, 4, 0.25f), 4);
        Assert.Equal(4f, SceneFieldPlan.FarCellFor(10f, 4, 0.25f), 4);
        Assert.Equal(0f, SceneFieldPlan.FarCellFor(0f, 4, 0.25f));
        Assert.Equal(0f, SceneFieldPlan.FarCellFor(float.PositiveInfinity, 4, 0.25f));

        var plan = new SceneFieldPlan(4, 0.25f, 1, farCell: 7.5f);
        Assert.True(plan.HasFar);
        Assert.Equal((5, 4), (plan.Cascades, plan.Counted));
        Assert.Equal(2f, plan.CellOf(3));
        Assert.Equal(7.5f, plan.CellOf(4));
        Assert.Equal(4, plan.CascadeOfProbes(2, 3));
        Assert.Equal(1, plan.CascadeOfProbes(1, 3));
        Assert.Equal(2, new SceneFieldPlan(4, 0.25f, 1).CascadeOfProbes(2, 3));
    }

    /// <summary>
    /// Each cell of the field is painted the color of its nearest surface, a red box's cells beside
    /// its face red and the white floor's white, and a cell no surface comes within the band of is
    /// left unpainted, read back from the image the probes' rays read.
    /// </summary>
    [SkippableFact]
    public void TheFieldPaintsEachCellItsNearestSurfacesColor()
    {
        Needs.Shaders();
        var origin = Vector3.Zero;
        var paint = ReadBack(
            ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Floor(ecs, (0.9f, 0.9f, 0.9f));
                Block(ecs, (0.8f, 0.05f, 0.05f), new Vec3(0f, 1f, 0f), new Vec3(2f, 2f, 2f));
                Render.SetSceneField(1, 0.25f);
            },
            stills: 2,
            () =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                return SceneField.Albedo;
            });

        (byte R, byte G, byte B, byte A) At(Vector3 p)
        {
            var cell = (p - origin) / 0.25f;
            var (x, y, z) = ((int)cell.X, (int)cell.Y, (int)cell.Z);
            var at = ((z * SceneFieldPlan.Resolution + y) * SceneFieldPlan.Resolution + x) * 4;
            return (paint[at], paint[at + 1], paint[at + 2], paint[at + 3]);
        }

        var red = At(new Vector3(1.1f, 1f, 0.1f));
        Assert.True(red.A == 255 && red.R > 100 && red.G < 30 && red.B < 30, $"a cell beside the red box's face reads {red}");
        var white = At(new Vector3(-4.1f, 0.1f, 3.1f));
        Assert.True(white.A == 255 && white.R > 150 && white.G > 150 && white.B > 150, $"a cell above the white floor reads {white}");
        var open = At(new Vector3(-4.1f, 2.9f, 3.1f));
        Assert.True(open.A == 0, $"a cell far from every surface reads {open}");
    }

    /// <summary>
    /// A probe a meter before a sunlit red wall gathers red light on its face toward the wall, the
    /// wall's light the rays bring back, and far less red on its face away from it, which looks out
    /// over the white floor.
    /// </summary>
    [SkippableFact]
    public void AProbeBeforeASunlitRedWallTakesItsRedOnTheFaceTowardIt()
    {
        Needs.Shaders();
        var faces = Probe(
            ecs =>
            {
                Floor(ecs, (0.8f, 0.8f, 0.8f));
                Block(ecs, (0.8f, 0.05f, 0.05f), new Vec3(-2.2f, 2f, 0f), new Vec3(0.4f, 4f, 8f));
                Sun(ecs, new Vec3(4f, 6f, 1f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.High);
            },
            stills: 2,
            new Vector3(-1f, 1f, 0f));

        var (toward, away) = (faces[1], faces[0]);
        Assert.True(toward.X > 0.01f, $"the face toward the wall gathers {toward}");
        Assert.True(toward.X > 2 * toward.Y, $"the face toward the wall gathers {toward}, not red");
        Assert.True(toward.X / toward.Y > 2 * (away.X / Math.Max(away.Y, 1e-6f)), $"the face toward the wall gathers {toward} and the face away {away}");
    }

    /// <summary>
    /// A probe in the middle of a solid block, more than its spacing from every face, is passed over,
    /// its faces holding nothing, while a probe in the open beside the block holds light.
    /// </summary>
    [SkippableFact]
    public void AProbeInsideASolidBlockHoldsNothing()
    {
        Needs.Shaders();
        var inside = Probe(
            ecs =>
            {
                Floor(ecs, (0.8f, 0.8f, 0.8f));
                Block(ecs, (0.5f, 0.5f, 0.5f), new Vec3(0f, 3f, 0f), new Vec3(6f, 6f, 6f));
                Sun(ecs, new Vec3(4f, 6f, 1f));
                Render.SetSceneField(1, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
            stills: 2,
            new Vector3(0f, 3f, 0f),
            new Vector3(5f, 1f, 5f));

        Assert.All(inside[..6], face => Assert.True(face.W < 0.5f, $"a face of the probe inside reads {face}"));
        Assert.All(inside[6..], face => Assert.True(face.W > 1.5f, $"a face of the probe outside reads {face}"));
    }

    /// <summary>
    /// A first-cascade probe looking at a black wall 200 units wide forty units ahead of the eye, past
    /// the field's counted cascades, gathers the sky through the wall without the far cascade and under
    /// half of it with the far cascade, which holds the wall, as 3DEngine's probe read 1.14 of a sky of
    /// pi with it and 3.14 without. The wall stands ahead, where the far cascade lies a third of its
    /// width beyond the eye, since one beside the eye passes the far cascade's side within the probe's
    /// view.
    /// </summary>
    [SkippableFact]
    public void AFarCascadeHoldsAWallPastTheCountedOnes()
    {
        Needs.Shaders();
        var without = FacingTheWall(farCascade: 0f);
        var with = FacingTheWall(farCascade: 100f);
        Assert.True(without > MathF.PI * 0.8f, $"without the far cascade the face toward the wall gathers {without} of a sky of pi");
        Assert.True(with < 0.5f * without, $"with the far cascade the face toward the wall gathers {with}, without {without}");

        static float FacingTheWall(float farCascade) => Probe(
            ecs =>
            {
                Block(ecs, (0f, 0f, 0f), new Vec3(0f, 1.2f, -40f), new Vec3(200f, 200f, 1f));
                Render.SetSceneField(2, 0.25f, farCascade: farCascade);
                Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
            stills: 1,
            new Vector3(0f, 0f, 0f),
            sky: true)[5].X;
    }

    /// <summary>
    /// The six faces of the first cascade's probes nearest each of <paramref name="points"/>, +x, -x,
    /// +y, -y, +z and -z in turn, the light in rgb and the probe's place in alpha, once the field holds
    /// the scene's still meshes and the probes have traced for a while.
    /// </summary>
    private static Vector4[] Probe(Action<EcsWorld> scene, int stills, params Vector3[] points) => Probe(scene, stills, points, sky: false);

    private static Vector4[] Probe(Action<EcsWorld> scene, int stills, Vector3 point, bool sky) => Probe(scene, stills, [point], sky);

    private static Vector4[] Probe(Action<EcsWorld> scene, int stills, Vector3[] points, bool sky)
    {
        var origin = Vector3.Zero;
        var spacing = 0f;
        var texels = ReadBack(
            ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                // A white sky of a light of one where it is asked for, at the camera's exposure.
                Render.SetAmbientLight(camera, (1f, 1f, 1f), sky ? 1f / Exposure : 0f);
                scene(ecs);
            },
            stills,
            () =>
            {
                var plan = SceneField.Plan!;
                origin = plan.BuiltOrigin(0)!.Value;
                spacing = WorldProbes.ProbeSpacing * plan.CellOf(0);
                return WorldProbes.Cubes;
            },
            () => WorldProbes.TracedFrames >= 60);

        const int p = WorldProbes.Probes;
        var halves = MemoryMarshal.Cast<byte, Half>(texels);
        var faces = new Vector4[points.Length * 6];
        for (var i = 0; i < points.Length; i++)
        {
            var at = Vector3.Clamp((points[i] - origin) / spacing - new Vector3(0.5f), Vector3.Zero, new Vector3(p - 1));
            var (x, y, z) = ((int)MathF.Round(at.X), (int)MathF.Round(at.Y), (int)MathF.Round(at.Z));
            for (var face = 0; face < 6; face++)
            {
                var texel = ((z * p + y) * 6 * p + face * p + x) * 4;
                faces[i * 6 + face] = new Vector4((float)halves[texel], (float)halves[texel + 1], (float)halves[texel + 2], (float)halves[texel + 3]);
            }
        }

        return faces;
    }

    /// <summary>A camera's exposure as Bevy works it out at its default of 9.7 EV, the light that reads as one on its picture.</summary>
    private static readonly float Exposure = 1f / (1.2f * MathF.Pow(2f, 9.7f));

    /// <summary>
    /// The texels of the image <paramref name="image"/> names, read back once the field holds
    /// <paramref name="stills"/> still meshes and every cascade is built, and once
    /// <paramref name="ready"/> says so where it is given, a few frames after.
    /// </summary>
    private static byte[] ReadBack(Action<EcsWorld> scene, int stills, Func<AssetHandle> image, Func<bool>? ready = null)
    {
        var read = default(BufferRead);
        byte[]? texels = null;
        new PictureRun { Width = 32, Height = 32, Scene = scene }
            .Until("settled", _ => SceneField.Plan is { } plan && plan.StillCount == stills && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("ready", _ => ready?.Invoke() ?? true)
            .Wait(3)
            .Do("asking for the image", _ => read = Shaders.BeginImageRead(image()))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out texels))
            .Go();

        Assert.NotNull(texels);
        return texels!;
    }

    private static void Floor(EcsWorld ecs, (float R, float G, float B) color) =>
        Block(ecs, color, new Vec3(0f, -0.1f, 0f), new Vec3(20f, 0.2f, 20f));

    private static void Block(EcsWorld ecs, (float R, float G, float B) color, Vec3 at, Vec3 size)
    {
        var block = ecs.Spawn();
        Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
        Render.SetMaterial(ecs, block, Render.CreateMaterial(color.R, color.G, color.B, roughness: 1f));
        ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
    }

    private static void Sun(EcsWorld ecs, Vec3 from)
    {
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false, Intensity = 10000f });
        ecs.Add(sun, Transform.LookingAt(from, Vec3.Zero, Vec3.UnitY));
    }
}

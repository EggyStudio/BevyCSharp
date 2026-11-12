using System.Numerics;
using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// The scene's distance field as it is seen and as it shadows, the slice its window draws read back
/// across a box, and the sun's contact shadows through the field against the shadow map's own on
/// the same pixels, which is how a mismatch of the light worked out again from the G-buffer shows as
/// a number rather than as a look.
/// </summary>
[Collection("engine")]
public sealed class SceneFieldShadowTests(ITestOutputHelper output)
{
    /// <summary>
    /// A slice across a box through its middle is red inside, black at its faces and gray past the
    /// band, read from the image the window shows.
    /// </summary>
    [SkippableFact]
    public void TheWindowsSliceAcrossABoxIsRedInsideBlackAtItsFacesAndGrayBeyond()
    {
        Needs.Shaders();
        var read = default(BufferRead);
        byte[]? pixels = null;
        var origin = Vector3.Zero;

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 2f, new Vec3(0f, 1f, 0f));
                Render.SetSceneField(1, 0.25f);
            },
        };

        AssetHandle image = AssetHandle.None;
        // The slice's program compiles beside the three the field is built with and can come after
        // them, and until it has the slice is no image, for as long as a second where other tests
        // share the GPU, so it is asked for each frame until it answers one.
        run.Until("settled", _ => SceneField.Plan is { StillCount: 1 } plan && plan.Shapes.Count == 0)
            .Until("slicing across y through the box's middle", _ =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                image = SceneField.Slice(0, 1, (int)MathF.Floor((1f - origin.Y) / 0.25f));
                return image != AssetHandle.None;
            })
            .Wait(3)
            .Do("asking for the slice", _ => read = Shaders.BeginImageRead(image))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out pixels))
            .Go();

        Assert.NotNull(pixels);
        const int side = SceneFieldPlan.Resolution * 4;
        (byte R, byte G, byte B) At(float x, float z)
        {
            // The pixel under the point, four to a cell.
            var (column, row) = ((int)MathF.Floor((x - origin.X) / 0.25f * 4f), (int)MathF.Floor((z - origin.Z) / 0.25f * 4f));
            var at = (row * side + column) * 4;
            return (pixels![at], pixels[at + 1], pixels[at + 2]);
        }

        // Four cells deep, the darkest red the window paints.
        var inside = At(0f, 0f);
        Assert.True(inside.R > 60 && inside.R > 2 * inside.G && inside.R > 2 * inside.B, $"the box's middle reads {inside}");
        var face = At(0.99f, 0.0625f);
        Assert.True(face.R < 40 && face.G < 40 && face.B < 40, $"the cell at the box's face reads {face}");
        var beyond = At(5f, 0f);
        Assert.True(beyond.R == beyond.G && beyond.G == beyond.B && beyond.R > 100, $"a cell past the band reads {beyond}");
    }

    /// <summary>
    /// A pillar's shadow on the ground drawn by the field alone, the sun's shadow map off, is as deep
    /// as the map's own on the pixels both darken, covers most of the map's, and leaves the lit
    /// ground as it was, so the light Bevy gave the ground is the light taken back.
    /// </summary>
    /// <remarks>
    /// The field's shadow is soft, darkened by how near a surface its ray passes as a share of how
    /// far along it is, so toward its tip, far from the pillar, it fades where the map's hard shadow
    /// runs on, and the pixels compared are those both darken under half the lit ground's level. On
    /// the RTX 4070 they differ by 1.2 levels on average, the field covering 73% of the map's
    /// shadow, and the lit ground by 0.4.
    /// </remarks>
    [SkippableFact]
    public void TheFieldsShadowIsAsDeepAsTheShadowMapsOwnWhereBothDarken()
    {
        Needs.Shaders();
        var mapped = Shadowed(mapShadows: true, null);
        var traced = Shadowed(mapShadows: false, FieldShadows.Everywhere);

        var (lit, shade, ground) = Levels(mapped, traced);
        Assert.True(ground > 4000, $"{ground} pixels of the ground lit");
        Assert.True(shade.Core > 500, $"{shade.Core} pixels of the map's shadow");
        Assert.True(shade.Both > 0.6 * shade.Core, $"the field darkens {shade.Both} of the map's {shade.Core} pixels of shadow");
        Assert.True(shade.Mean < 4, $"the field's shadow differs from the map's by {shade.Mean:0.0} levels on average, {shade.Most} at most");
        Assert.True(lit.Mean < 1.5, $"the lit ground differs by {lit.Mean:0.0} levels on average, {lit.Most} at most");
    }

    /// <summary>
    /// Past the shadow map's reach alone, the default, the field changes nothing the map holds, so a
    /// picture whose shadow the map draws is the picture with the field's shadows on.
    /// </summary>
    [SkippableFact]
    public void PastTheShadowMapTheFieldLeavesWhatTheMapHoldsAsItWas()
    {
        Needs.Shaders();
        var mapped = Shadowed(mapShadows: true, null);
        var both = Shadowed(mapShadows: true, FieldShadows.PastShadowMap);

        var (lit, shade, _) = Levels(mapped, both);
        Assert.True(shade.Both == shade.Core, $"{shade.Both} of the map's {shade.Core} pixels of shadow stay dark");
        Assert.True(shade.Mean < 1 && lit.Mean < 1, $"the shadow differs by {shade.Mean:0.0} levels and the lit ground by {lit.Mean:0.0}");
    }

    /// <summary>
    /// A sun under the horizon, which Bevy's atmosphere puts out, has no light taken back through
    /// the field, so a pillar's side facing it reads as it does with the field's shadows off.
    /// </summary>
    /// <remarks>
    /// The field sees the ground stand between a sun below the horizon and every surface facing it,
    /// so shadows that took back the light a directional light sends a surface as it is above the
    /// air would take back all of a light Bevy gives none of and leave the pillar's side black. The
    /// light is taken back through the camera's air as Bevy dims it.
    /// </remarks>
    [SkippableFact]
    public void ASunUnderTheHorizonHasNoLightTakenBackThroughTheField()
    {
        Needs.Shaders();
        var off = UnderTheHorizon(FieldShadows.Off);
        var on = UnderTheHorizon(FieldShadows.Everywhere);

        var (sum, darker, pixels) = (0.0, 0, 0);
        for (var y = 0u; y < off.Height; y++)
        {
            for (var x = 0u; x < off.Width; x++)
            {
                var (a, b) = (off.At(x, y), on.At(x, y));
                var (levelA, levelB) = ((a.R + a.G + a.B) / 3, (b.R + b.G + b.B) / 3);
                if (levelA < 8) continue;
                pixels++;
                sum += Math.Abs(levelA - levelB);
                if (levelB < levelA - 8) darker++;
            }
        }

        output.WriteLine($"{darker} of {pixels} lit pixels read darker by more than 8 levels with the field's shadows, {sum / pixels:0.0} levels apart on average");
        Assert.True(pixels > 2000, $"{pixels} pixels lit");
        Assert.True(darker < pixels / 100, $"{darker} of {pixels} pixels read darker by more than 8 levels with the field's shadows, {sum / pixels:0.0} levels apart on average");
    }

    /// <summary>
    /// The pillar on the ground under Bevy's atmosphere, a sun of 10,000 lux fifteen degrees under
    /// the horizon and an ambient light of 400 lighting it, with the field's shadows as given.
    /// </summary>
    private static CapturedImage UnderTheHorizon(FieldShadows shadows)
    {
        var run = new PictureRun
        {
            Width = 160,
            Height = 120,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(4f, 3f, 6f), new Vec3(0f, 1f, 0f), Vec3.UnitY));
                Shaders.SetPrepass(camera, depth: true, deferred: true);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.None });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 400f);

                var stone = Render.CreateMaterial(0.6f, 0.6f, 0.6f);
                var ground = ecs.Spawn();
                Render.SetMesh(ecs, ground, Render.CreateMesh(MeshShape.Cuboid, 20f, 0.2f, 20f));
                Render.SetMaterial(ecs, ground, stone);
                ecs.Add(ground, Transform.At(0f, -0.1f, 0f));
                var pillar = ecs.Spawn();
                Render.SetMesh(ecs, pillar, Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 1f));
                Render.SetMaterial(ecs, pillar, stone);
                ecs.Add(pillar, Transform.At(0f, 1f, 0f));

                // Fifteen degrees under the horizon, toward the camera's side of the pillar.
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(1f, -MathF.Tan(15f * MathF.PI / 180f), 1.5f), Vec3.Zero, Vec3.UnitY));

                Render.SetSceneField(1, 0.125f);
                Render.SetSceneFieldShadows(shadows);
            },
        };

        run.Until("settled", _ => SceneField.Plan is { StillCount: 2 } plan && plan.Shapes.Count == 0)
            .Wait(20)
            .Capture("under the horizon")
            .Go();
        return run.Picture("under the horizon");
    }

    /// <summary>
    /// The engine's contact shadows draw beside a program's own draws on the camera, which stay
    /// drawn, and give the camera a picture in floats while they are on it and take it away after.
    /// </summary>
    [SkippableFact]
    public void AProgramsOwnDrawOnTheCameraStaysBesideTheFieldsShadows()
    {
        Needs.Shaders();
        var camera = Entity.None;
        ShaderInstance tint = default;
        var floats = new List<bool>();

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f));
                Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });

                // A green triangle over the whole view added to the picture, the program's own draw.
                tint = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawVertex = ShaderStage.Slang(Tint, "vertex"),
                    DrawFragment = ShaderStage.Slang(Tint, "fragment"),
                }));
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(tint, FramePoint.AfterTonemapping, 3, 1, DrawBlend.Add, writesDepth: false));
                Render.SetSceneField(1, 0.25f);
                Render.SetSceneFieldShadows(FieldShadows.Everywhere);
            },
        };

        run.Until("the shadows drawn", _ => SceneField.Plan is { StillCount: 1 } && ShadowsOn())
            .Wait(10)
            .Capture("tinted")
            .Do("reading the camera's picture", world => floats.Add(world.Resource<EcsWorld>().Get<Bevy.Reflected.HdrRef>(camera) is not null))
            .Do("turning the shadows off", _ => Render.SetSceneFieldShadows(FieldShadows.Off))
            .Wait(3)
            .Do("reading it again", world => floats.Add(world.Resource<EcsWorld>().Get<Bevy.Reflected.HdrRef>(camera) is not null))
            .Go();

        // The background, black where nothing is drawn, takes the program's green.
        var (r, g, b, _) = run.Picture("tinted").At(2, 2);
        Assert.True(g > 100 && r < 40 && b < 40, $"the corner reads {r}, {g}, {b}");
        Assert.Equal(new[] { true, false }, floats);
    }

    internal const string Tint = """
        import bcs_pass;

        [shader("vertex")]
        FullscreenOutput vertex(uint index : SV_VertexID)
        {
            var corner = fullscreenTriangle(index);
            corner.position.z = 1.0;
            return corner;
        }

        [shader("fragment")]
        float4 fragment(FullscreenOutput input) : SV_Target
        {
            return float4(0.0, 0.5, 0.0, 0.0);
        }
        """;

    // Whether the field's shadows are on the camera, read through the runner's own record.
    private static bool ShadowsOn() => SceneField.ShadowCamera != Entity.None;

    /// <summary>
    /// A pillar standing on the ground under a low sun, its shadow drawn by the sun's shadow map or
    /// not and by the field as asked, and the picture once the field is built. The camera is
    /// deferred and draws in high dynamic range either way, so both pictures are lit by the same
    /// path, with no tone mapping and the sun dim enough that the lit ground stays under white, so a
    /// level is the light itself.
    /// </summary>
    private static CapturedImage Shadowed(bool mapShadows, FieldShadows? field)
    {
        var run = new PictureRun
        {
            Width = 160,
            Height = 120,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
                ecs.Add(camera, Transform.LookingAt(new Vec3(4f, 4f, 6f), new Vec3(-0.5f, 0.3f, -0.5f), Vec3.UnitY));
                Shaders.SetPrepass(camera, depth: true, deferred: true);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.None });

                var stone = Render.CreateMaterial(0.6f, 0.6f, 0.6f);
                var ground = ecs.Spawn();
                Render.SetMesh(ecs, ground, Render.CreateMesh(MeshShape.Cuboid, 20f, 0.2f, 20f));
                Render.SetMaterial(ecs, ground, stone);
                ecs.Add(ground, Transform.At(0f, -0.1f, 0f));
                var pillar = ecs.Spawn();
                Render.SetMesh(ecs, pillar, Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 1f));
                Render.SetMaterial(ecs, pillar, stone);
                ecs.Add(pillar, Transform.At(0f, 1f, 0f));

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = mapShadows, Intensity = 3000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 2f, -1f), Vec3.Zero, Vec3.UnitY));

                Render.SetSceneField(1, 0.125f);
                if (field is { } shadows) Render.SetSceneFieldShadows(shadows);
                else Render.SetSceneFieldShadows(FieldShadows.Off);
            },
        };

        run.Until("settled", _ => SceneField.Plan is { StillCount: 2 } plan && plan.Shapes.Count == 0)
            .Wait(20)
            .Capture("shadowed")
            .Go();
        return run.Picture("shadowed");
    }

    /// <summary>
    /// The levels two pictures of the same scene differ by, over the ground the first lights, and
    /// over the shadow both darken under half the lit ground's level, with how many pixels the
    /// first darkens so and how many of those the second does, and how many of the lit ground
    /// there are.
    /// </summary>
    private static ((double Mean, int Most) Lit, (double Mean, int Most, int Core, int Both) Shade, int Ground) Levels(CapturedImage first, CapturedImage second)
    {
        static int Level(CapturedImage picture, uint x, uint y)
        {
            var (r, g, b, _) = picture.At(x, y);
            return (r + g + b) / 3;
        }

        // The lit ground's level, the brightest common level of the lower half of the picture.
        var levels = new List<int>();
        for (var y = first.Height / 2; y < first.Height; y++)
            for (var x = 0u; x < first.Width; x++)
                levels.Add(Level(first, x, y));
        levels.Sort();
        var litLevel = levels[(int)(levels.Count * 0.9)];

        double litSum = 0, shadeSum = 0;
        int litCount = 0, litMost = 0, core = 0, both = 0, shadeMost = 0;
        for (var y = 0u; y < first.Height; y++)
        {
            for (var x = 0u; x < first.Width; x++)
            {
                var (a, b) = (Level(first, x, y), Level(second, x, y));
                var difference = Math.Abs(a - b);
                if (y >= first.Height / 2 && Math.Abs(a - litLevel) <= 4)
                {
                    litSum += difference;
                    litCount++;
                    litMost = Math.Max(litMost, difference);
                }
                else if (a > 0 && a < litLevel / 2)
                {
                    core++;
                    if (b >= litLevel / 2) continue;
                    shadeSum += difference;
                    both++;
                    shadeMost = Math.Max(shadeMost, difference);
                }
            }
        }

        return ((litCount > 0 ? litSum / litCount : 0, litMost), (both > 0 ? shadeSum / both : 0, shadeMost, core, both), litCount);
    }
}

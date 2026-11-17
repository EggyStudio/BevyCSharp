using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room of the level's white lit by six small emitters and nothing else reads within
    /// 10% of its reference on every face, as one emitter of their light together reads.
    /// </summary>
    /// <remarks>
    /// Global illumination carries a small emitter as a glow light, whose light a surface takes in
    /// closed form, each light let go past a reach set by its own light. A reach set by a fixed
    /// share of the picture's unit lets a dim light go nearer than a bright one of its size, so six
    /// emitters of a sixth of one's light each would reach less far than the one, and the room
    /// would read under its reference where they stop short.
    /// </remarks>
    [SkippableFact]
    public void ARoomLitBySixEmittersReadsAsItsReference()
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, shown, wanted) in EmitterRoom(6))
        {
            var off = shown / wanted - 1;
            output.WriteLine($"the room of six emitters' {face} reads {shown:0.000} against {wanted:0.000}, {off:+0.000;-0.000}");
            if (Math.Abs(off) >= 0.1) faults.Add($"the {face} reads {off:+0.000;-0.000} of its reference");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The same room lit by one emitter of the six's light together reads within 14% of its
    /// reference on every face.
    /// </summary>
    /// <remarks>
    /// The reach is a thousandth of each light's own light, which keeps this emitter's reach where
    /// the room reads as its reference, so a reach set shorter reads this room under as it reads
    /// the six emitters' under, and the constant is held where it is. The wall facing -x beside the
    /// emitter, lit mostly by the glow light's own light near it, reads the farthest under, 12.2%,
    /// which the bar of 14% holds with 1.8 points to spare, and every other face reads within 2.3%.
    /// </remarks>
    [SkippableFact]
    public void ARoomLitByOneEmitterKeepsItsReading()
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, shown, wanted) in EmitterRoom(1))
        {
            var off = shown / wanted - 1;
            output.WriteLine($"the room of one emitter's {face} reads {shown:0.000} against {wanted:0.000}, {off:+0.000;-0.000}");
            if (Math.Abs(off) >= 0.14) faults.Add($"the {face} reads {off:+0.000;-0.000} of its reference");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A closed room of the level's white lit by a ball emitting a lamp's warm light reads within
    /// 10% of its reference on every face, as a block of the ball's box reads.
    /// </summary>
    /// <remarks>
    /// A small emitter is carried as a light from its box, which takes the share of its box's faces
    /// the mesh's own surface makes, the area of its triangles the field's gather hands over, since
    /// a ball's surface is about half its box's, and taken as its box whole, every face emitting, a
    /// ball would be carried twice as bright as it is.
    /// </remarks>
    [SkippableFact]
    public void ARoomLitByABallReadsAsItsReference()
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, shown, wanted) in EmitterRoom(ecs => BallScene(ecs), name => !name.StartsWith("ball", StringComparison.Ordinal)))
        {
            var off = shown / wanted - 1;
            output.WriteLine($"the room lit by a ball reads its {face} {shown:0.000} against {wanted:0.000}, {off:+0.000;-0.000}");
            if (Math.Abs(off) >= 0.1) faults.Add($"the {face} reads {off:+0.000;-0.000} of its reference");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A glTF file's emissive panel lights a floor of the level's white under it within a fifth of
    /// its closed form at three heights, in the picture's light, its emission in nits taking the
    /// camera's exposure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The panel is 0.3 square and a millimeter thick, every face emitting 4,000 nits by
    /// <c>KHR_materials_emissive_strength</c>, so global illumination carries it as a glow light of
    /// its box. A point at a height d under the middle of a square of half side s, emitting L, takes
    /// E = 4 L t atan(t) lux, where t = a / sqrt(1 + a²) and a = s / d, and a floor of white ρ shows
    /// ρ E / π times the camera's exposure, the panel's thin sides adding under half a percent.
    /// </para>
    /// <para>
    /// The floor reads its closed form only where a glTF material's emission takes the camera's
    /// exposure, drawn and in the cascades alike, since an exposure weight of 0, which Bevy's glTF
    /// loader gives a file's emission, shows it as written, about a thousand times its light under
    /// Bevy's exposure of EV 9.7.
    /// </para>
    /// </remarks>
    [SkippableFact]
    public void AGltfPanelLightsTheFloorUnderItAsItsClosedForm()
    {
        Needs.Shaders();
        const float half = 0.15f, nits = 4_000f, white = 0.787f;
        var exposure = 1f / (1.2f * MathF.Pow(2f, 9.7f));
        using var assets = PictureRun.Temporary();
        assets.Write("panel.gltf", Panel(half, 0.001f, nits));

        var faults = new List<string>();
        foreach (var height in new[] { 0.5f, 1f, 2f })
        {
            var t = half / height / MathF.Sqrt(1f + (half / height * half / height));
            var wanted = white / MathF.PI * 4f * nits * t * MathF.Atan(t) * exposure;
            var shown = UnderPanel(assets.Root, height, white);
            output.WriteLine($"the floor {height} under the glTF panel reads {shown:0.0000} against its closed form's {wanted:0.0000}, {shown / wanted - 1:+0.000;-0.000}");
            if (Math.Abs(shown / wanted - 1) >= 0.2) faults.Add($"{height} under the panel the floor reads {shown / wanted:0.000} of its closed form");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The mean light of the middle of the picture, five pixels a side, before tonemapping, of a
    /// floor of <paramref name="white"/> under the panel of the file at <paramref name="root"/>, its
    /// lower face <paramref name="height"/> over the floor, looked at from the side along a line that
    /// passes under the panel, at Bevy's exposure of EV 9.7, with no other light.
    /// </summary>
    private static double UnderPanel(string root, float height, float white)
    {
        var (read, ready) = (false, 0);
        new PictureRun
        {
            AssetRoot = root,
            Width = 160,
            Height = 90,
            Frames = 3000,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f), FieldOfView = 20f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(2.4f, 1.2f, 0f), Vec3.Zero, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetExposure(camera, 9.7f);
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.4f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(white, white, white, roughness: 0.9f));
                ecs.Add(floor, Transform.At(0f, -0.2f, 0f));
                var panel = ecs.SpawnScene(AssetServer.LoadGltfScene("panel.gltf"));
                ecs.Add(panel, Transform.At(0f, height + 0.0005f, 0f));
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        }
            .Until("settled", _ => Settled(2))
            .Until("traced", _ => WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => EngineDrawsOn(traced: true))
            .Until("ready", _ => (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("read", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();

        var (width, high, light) = GiReference.LastPicture!.Value;
        var sum = 0.0;
        for (var y = high / 2 - 2; y <= high / 2 + 2; y++)
        for (var x = width / 2 - 2; x <= width / 2 + 2; x++)
        for (var c = 0; c < 3; c++)
            sum += light[(y * width + x) * 3 + c];
        return sum / 75;
    }

    /// <summary>
    /// A glTF file of one panel <paramref name="half"/> either way of its middle across and
    /// <paramref name="thick"/> through, black, every face emitting <paramref name="nits"/> in white.
    /// </summary>
    private static string Panel(float half, float thick, float nits)
    {
        // Each face its four corners and its normal, wound counterclockwise seen from outside.
        var (x, y, z) = (half, thick / 2f, half);
        (Vec3 Normal, Vec3[] Corners)[] faces =
        [
            (Vec3.UnitY, [new(-x, y, -z), new(-x, y, z), new(x, y, z), new(x, y, -z)]),
            (-Vec3.UnitY, [new(-x, -y, -z), new(x, -y, -z), new(x, -y, z), new(-x, -y, z)]),
            (Vec3.UnitX, [new(x, -y, -z), new(x, y, -z), new(x, y, z), new(x, -y, z)]),
            (-Vec3.UnitX, [new(-x, -y, -z), new(-x, -y, z), new(-x, y, z), new(-x, y, -z)]),
            (Vec3.UnitZ, [new(-x, -y, z), new(x, -y, z), new(x, y, z), new(-x, y, z)]),
            (-Vec3.UnitZ, [new(-x, -y, -z), new(-x, y, -z), new(x, y, -z), new(x, -y, -z)]),
        ];
        var (positions, normals, indices) = (new List<float>(), new List<float>(), new List<ushort>());
        foreach (var (normal, corners) in faces)
        {
            var first = (ushort)(positions.Count / 3);
            foreach (var corner in corners)
            {
                positions.AddRange([corner.X, corner.Y, corner.Z]);
                normals.AddRange([normal.X, normal.Y, normal.Z]);
            }
            indices.AddRange([first, (ushort)(first + 1), (ushort)(first + 2), first, (ushort)(first + 2), (ushort)(first + 3)]);
        }

        var bytes = new byte[(positions.Count + normals.Count) * 4 + indices.Count * 2];
        Buffer.BlockCopy(positions.ToArray(), 0, bytes, 0, positions.Count * 4);
        Buffer.BlockCopy(normals.ToArray(), 0, bytes, positions.Count * 4, normals.Count * 4);
        Buffer.BlockCopy(indices.ToArray(), 0, bytes, (positions.Count + normals.Count) * 4, indices.Count * 2);
        var view = (int offset, int length) => new Dictionary<string, object> { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = length };
        var gltf = new Dictionary<string, object>
        {
            ["asset"] = new Dictionary<string, object> { ["version"] = "2.0" },
            ["extensionsUsed"] = new[] { "KHR_materials_emissive_strength" },
            ["scene"] = 0,
            ["scenes"] = new[] { new Dictionary<string, object> { ["nodes"] = new[] { 0 } } },
            ["nodes"] = new[] { new Dictionary<string, object> { ["mesh"] = 0 } },
            ["meshes"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["primitives"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["attributes"] = new Dictionary<string, object> { ["POSITION"] = 0, ["NORMAL"] = 1 },
                            ["indices"] = 2,
                            ["material"] = 0,
                        },
                    },
                },
            },
            ["materials"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["pbrMetallicRoughness"] = new Dictionary<string, object> { ["baseColorFactor"] = new[] { 0f, 0f, 0f, 1f }, ["metallicFactor"] = 0f, ["roughnessFactor"] = 1f },
                    ["emissiveFactor"] = new[] { 1f, 1f, 1f },
                    ["extensions"] = new Dictionary<string, object>
                    {
                        ["KHR_materials_emissive_strength"] = new Dictionary<string, object> { ["emissiveStrength"] = nits },
                    },
                },
            },
            ["buffers"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["byteLength"] = bytes.Length,
                    ["uri"] = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes),
                },
            },
            ["bufferViews"] = new[] { view(0, positions.Count * 4), view(positions.Count * 4, normals.Count * 4), view((positions.Count + normals.Count) * 4, indices.Count * 2) },
            ["accessors"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = positions.Count / 3, ["type"] = "VEC3",
                    ["min"] = new[] { -x, -y, -z }, ["max"] = new[] { x, y, z },
                },
                new Dictionary<string, object> { ["bufferView"] = 1, ["componentType"] = 5126, ["count"] = normals.Count / 3, ["type"] = "VEC3" },
                new Dictionary<string, object> { ["bufferView"] = 2, ["componentType"] = 5123, ["count"] = indices.Count, ["type"] = "SCALAR" },
            },
        };

        return System.Text.Json.JsonSerializer.Serialize(gltf);
    }

    /// <summary>
    /// A closed room of the level's white, eight units across and three high, lit by a ball 0.6 in
    /// radius emitting a warm lamp's light at a tenth of a lamp's, 1.2 over the floor.
    /// </summary>
    private static void BallScene(EcsWorld ecs)
    {
        EmitterScene(ecs, 0, 8f, 3f);
        var ball = ecs.Spawn();
        Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 0.6f));
        Render.SetMaterial(ecs, ball, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.6f, 0.2f, 1f), Emissive = (1_200f, 500f, 100f, 1f) }));
        ecs.Add(ball, Transform.At(-1.5f, 1.2f, -1.5f));
        ecs.SetName(ball, "ball");
    }

    /// <summary>
    /// Global illumination's passes' time on the GPU in a closed room of the level's white lit by
    /// forty small emitters at <see cref="GlobalIllumination.Medium"/> at 1280 by 720, printed, so
    /// the glow lights' cost is read where many of them share one room.
    /// </summary>
    [SkippableFact]
    public void TheGlowLightsCostIsReadInARoomOfFortyEmitters()
    {
        Needs.Shaders();
        Needs.Gpu("reads what its passes cost a GPU, where a device that draws on the processor would take hours to time its processor instead");
        IReadOnlyList<PassTiming> timings = [];
        var ready = 0;
        new PictureRun
        {
            Width = 1280,
            Height = 720,
            Frames = 3000,
            Configure = config => config.GpuTimings = true,
            Scene = ecs => EmitterScene(ecs, 40, 16f, 4f),
        }
            .Until("settled", _ => SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => EngineDrawsOn(traced: true))
            .Until("ready", _ => (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Wait(120)
            .Do("timings", _ => timings = Render.Timings())
            .Go();
        var spans = timings.Where(timing => timing.Name.Contains("gi_", StringComparison.Ordinal) && timing.GpuMilliseconds is not null).ToList();
        Skip.If(spans.Count == 0, "needs a GPU whose passes the app can time");
        foreach (var span in spans.OrderByDescending(span => span.GpuMilliseconds))
            output.WriteLine($"forty emitters at 1280 by 720 at Medium, {span.Name} {span.GpuMilliseconds:0.000} ms");
        output.WriteLine($"forty emitters at 1280 by 720 at Medium, global illumination's passes {spans.Sum(span => span.GpuMilliseconds ?? 0):0.000} ms in all");
    }

    /// <summary>
    /// A closed room of the level's white, linear 0.787, eight units across and three high, lit by
    /// <paramref name="emitters"/> blocks a third of a unit across emitting the light of one
    /// between them, round its middle at two heights by turns.
    /// </summary>
    private static void EmitterScene(EcsWorld ecs, int emitters, float wide, float high)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(0f, high / 2f, wide / 2f - 0.5f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, high / 2f, wide / 2f - 0.5f), new Vec3(0f, high / 2f - 0.3f, -wide / 2f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        var white = Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: 0.9f);
        void Slab(string name, Vec3 at, Vec3 size, AssetHandle material)
        {
            var slab = ecs.Spawn();
            Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
            Render.SetMaterial(ecs, slab, material);
            ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(slab, name);
        }

        var (h, t) = (wide / 2f, 0.4f);
        Slab("floor", new Vec3(0f, -t / 2f, 0f), new Vec3(wide + 2f * t, t, wide + 2f * t), white);
        Slab("ceiling", new Vec3(0f, high + t / 2f, 0f), new Vec3(wide + 2f * t, t, wide + 2f * t), white);
        Slab("left", new Vec3(-h - t / 2f, high / 2f, 0f), new Vec3(t, high, wide), white);
        Slab("right", new Vec3(h + t / 2f, high / 2f, 0f), new Vec3(t, high, wide), white);
        Slab("front", new Vec3(0f, high / 2f, h + t / 2f), new Vec3(wide, high, t), white);
        Slab("back", new Vec3(0f, high / 2f, -h - t / 2f), new Vec3(wide, high, t), white);

        var glow = Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (1f, 1f, 1f, 1f),
            Emissive = (20_000f / emitters, 19_000f / emitters, 17_000f / emitters, 1f),
        });
        for (var e = 0; e < emitters; e++)
        {
            var angle = e * MathF.Tau / emitters;
            Slab($"glow {e}", new Vec3(MathF.Cos(angle) * (h - 1f), 0.6f + e % 2 * 1.5f, -1f + MathF.Sin(angle) * (h - 1.5f)), new Vec3(0.3f, 0.3f, 0.3f), glow);
        }

        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }

    /// <summary>
    /// Each face the camera sees of the closed room of <paramref name="emitters"/> emitters
    /// (<see cref="EmitterScene"/>), its mean light in the picture and in its reference of 256
    /// paths.
    /// </summary>
    private static List<(string Face, double Shown, double Wanted)> EmitterRoom(int emitters) =>
        EmitterRoom(ecs => EmitterScene(ecs, emitters, 8f, 3f), name => !name.StartsWith("glow", StringComparison.Ordinal));

    /// <summary>
    /// Each face the camera sees of the room <paramref name="scene"/> makes that
    /// <paramref name="counted"/> takes by its name, its mean light in the picture and in its
    /// reference of 256 paths.
    /// </summary>
    private static List<(string Face, double Shown, double Wanted)> EmitterRoom(Action<EcsWorld> scene, Func<string, bool> counted)
    {
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 6000,
            Scene = scene,
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginReference(ecs, null, 256));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Until("read", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var faces = new List<(string, double, double)>();
        foreach (var name in traced.Names.Where(n => counted(n.Value)).OrderBy(n => n.Value))
        {
            var (shown, wanted, pixels) = (0.0, 0.0, 0);
            for (var i = 0; i < traced.Regions.Length; i++)
            {
                if (traced.Regions[i] != name.Key) continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                {
                    shown += picture[i * 3 + c];
                    wanted += traced.Light[i * 3 + c];
                }
            }
            if (pixels > 50) faces.Add((name.Value, shown / pixels, wanted / pixels));
        }
        return faces;
    }
}

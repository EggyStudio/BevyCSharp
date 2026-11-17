using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A wall whose red is in its texture and whose metal is in its map, as a scene pack's walls
    /// hold theirs, colors the floor beside it as the same wall painted red by its factors does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both walls are glTF files. The textured one is white and full metal by its factors, its base
    /// color a checker of two reds and its metallic and roughness map rough with no metal, and the
    /// painted one takes the checker's mean in linear light as its factor, rough and with no metal.
    /// A low sun from in front lights the wall's face and grazes the floor, so the light the wall
    /// throws onto the floor half a meter in front of it is its red, and the floor's red past its
    /// green is read there, before tonemapping, at Bevy's exposure of EV 9.7.
    /// </para>
    /// <para>
    /// Global illumination's field holds one color a mesh and reads no texel, so it paints a surface
    /// its factors times its textures' means. Painted by its factors alone, the textured wall stands
    /// white and full metal there, takes no diffuse light and throws no red.
    /// </para>
    /// </remarks>
    [SkippableFact]
    public void ATexturedWallColorsTheFloorBesideItAsTheSameWallPaintedByItsFactors()
    {
        Needs.Shaders();
        (byte R, byte G, byte B)[] reds = [(230, 40, 40), (150, 20, 25)];
        static float Decode(byte value)
        {
            var v = value / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
        float Mean(Func<(byte R, byte G, byte B), byte> channel) => reds.Average(red => Decode(channel(red)));
        float[] mean = [Mean(red => red.R), Mean(red => red.G), Mean(red => red.B)];

        using var assets = PictureRun.Temporary();
        var checker = new byte[8 * 8 * 4];
        for (var i = 0; i < 64; i++)
        {
            var (r, g, b) = reds[(i % 8 + i / 8) % 2];
            (checker[i * 4], checker[i * 4 + 1], checker[i * 4 + 2], checker[i * 4 + 3]) = (r, g, b, 255);
        }
        var map = new byte[8 * 8 * 4];
        for (var i = 0; i < 64; i++) (map[i * 4], map[i * 4 + 1], map[i * 4 + 2], map[i * 4 + 3]) = (255, 255, 0, 255);
        File.WriteAllBytes(Path.Combine(assets.Root, "checker.png"), PngWriter.Still(8, 8, checker));
        File.WriteAllBytes(Path.Combine(assets.Root, "rough.png"), PngWriter.Still(8, 8, map));
        assets.Write("textured.gltf", Wall(new Dictionary<string, object>
        {
            ["baseColorFactor"] = new[] { 1f, 1f, 1f, 1f },
            ["baseColorTexture"] = new Dictionary<string, object> { ["index"] = 0 },
            ["metallicFactor"] = 1f,
            ["roughnessFactor"] = 1f,
            ["metallicRoughnessTexture"] = new Dictionary<string, object> { ["index"] = 1 },
        }, ["checker.png", "rough.png"]));
        assets.Write("painted.gltf", Wall(new Dictionary<string, object>
        {
            ["baseColorFactor"] = new[] { mean[0], mean[1], mean[2], 1f },
            ["metallicFactor"] = 0f,
            ["roughnessFactor"] = 1f,
        }, []));

        var painted = BesideWall(assets.Root, "painted.gltf");
        var textured = BesideWall(assets.Root, "textured.gltf");
        var (red, wanted) = (textured.R - textured.G, painted.R - painted.G);
        output.WriteLine($"the floor beside the painted wall reads {painted.R:0.0000} {painted.G:0.0000} {painted.B:0.0000}, its red past its green {wanted:0.0000}");
        output.WriteLine($"the floor beside the textured wall reads {textured.R:0.0000} {textured.G:0.0000} {textured.B:0.0000}, its red past its green {red:0.0000}, {red / wanted - 1:+0.000;-0.000}");
        Assert.True(wanted > 0.01, $"The floor beside the painted wall reads {wanted:0.0000} of red past its green, too little to hold the textured one to.");
        Assert.True(Math.Abs(red / wanted - 1) < 0.2, $"The floor beside the textured wall reads {red / wanted:0.000} of the red past its green that it reads beside the painted one.");
    }

    /// <summary>
    /// The mean light of each channel of the middle of the picture, five pixels a side, before
    /// tonemapping, of a white floor half a meter in front of the wall of the file named, lit by a
    /// sun 10 degrees up from in front of the wall, at Bevy's exposure of EV 9.7, with no other light.
    /// </summary>
    private static (double R, double G, double B) BesideWall(string root, string wall)
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
                ecs.Add(camera, Transform.LookingAt(new Vec3(3f, 1.5f, 0f), Vec3.Zero, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetExposure(camera, 9.7f);
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 12f, 0.4f, 12f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: 0.9f));
                ecs.Add(floor, Transform.At(0f, -0.2f, 0f));
                ecs.Add(ecs.SpawnScene(AssetServer.LoadGltfScene(wall)), Transform.At(-0.75f, 1.5f, 0f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Color = (1f, 1f, 1f), Shadows = true });
                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(-MathF.Cos(MathF.PI / 18f), -MathF.Sin(MathF.PI / 18f), 0f), Vec3.UnitY));
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
        var sum = new double[3];
        for (var y = high / 2 - 2; y <= high / 2 + 2; y++)
        for (var x = width / 2 - 2; x <= width / 2 + 2; x++)
        for (var c = 0; c < 3; c++)
            sum[c] += light[(y * width + x) * 3 + c];
        return (sum[0] / 25, sum[1] / 25, sum[2] / 25);
    }

    /// <summary>
    /// A glTF file of a wall half a meter thick, 3 high and 4 across, standing on its middle, of a
    /// material whose metallic and roughness are given, its textures, where it has them, the files
    /// named, each face mapped over the whole of them.
    /// </summary>
    private static string Wall(Dictionary<string, object> pbr, string[] images)
    {
        // Each face its four corners and its normal, wound counterclockwise seen from outside.
        var (x, y, z) = (0.25f, 1.5f, 2f);
        (Vec3 Normal, Vec3[] Corners)[] faces =
        [
            (Vec3.UnitY, [new(-x, y, -z), new(-x, y, z), new(x, y, z), new(x, y, -z)]),
            (-Vec3.UnitY, [new(-x, -y, -z), new(x, -y, -z), new(x, -y, z), new(-x, -y, z)]),
            (Vec3.UnitX, [new(x, -y, -z), new(x, y, -z), new(x, y, z), new(x, -y, z)]),
            (-Vec3.UnitX, [new(-x, -y, -z), new(-x, -y, z), new(-x, y, z), new(-x, y, -z)]),
            (Vec3.UnitZ, [new(-x, -y, z), new(x, -y, z), new(x, y, z), new(-x, y, z)]),
            (-Vec3.UnitZ, [new(-x, -y, -z), new(-x, y, -z), new(x, y, -z), new(x, -y, -z)]),
        ];
        float[] corners = [0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f];
        var (positions, normals, uvs, indices) = (new List<float>(), new List<float>(), new List<float>(), new List<ushort>());
        foreach (var (normal, square) in faces)
        {
            var first = (ushort)(positions.Count / 3);
            foreach (var corner in square)
            {
                positions.AddRange([corner.X, corner.Y, corner.Z]);
                normals.AddRange([normal.X, normal.Y, normal.Z]);
            }
            uvs.AddRange(corners);
            indices.AddRange([first, (ushort)(first + 1), (ushort)(first + 2), first, (ushort)(first + 2), (ushort)(first + 3)]);
        }

        var (p, n, u) = (positions.Count * 4, normals.Count * 4, uvs.Count * 4);
        var bytes = new byte[p + n + u + indices.Count * 2];
        Buffer.BlockCopy(positions.ToArray(), 0, bytes, 0, p);
        Buffer.BlockCopy(normals.ToArray(), 0, bytes, p, n);
        Buffer.BlockCopy(uvs.ToArray(), 0, bytes, p + n, u);
        Buffer.BlockCopy(indices.ToArray(), 0, bytes, p + n + u, indices.Count * 2);
        var view = (int offset, int length) => new Dictionary<string, object> { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = length };
        var accessor = (int view, int count, string type, int component) => new Dictionary<string, object>
        {
            ["bufferView"] = view, ["componentType"] = component, ["count"] = count, ["type"] = type,
        };
        var position = accessor(0, positions.Count / 3, "VEC3", 5126);
        position["min"] = new[] { -x, -y, -z };
        position["max"] = new[] { x, y, z };
        var gltf = new Dictionary<string, object>
        {
            ["asset"] = new Dictionary<string, object> { ["version"] = "2.0" },
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
                            ["attributes"] = new Dictionary<string, object> { ["POSITION"] = 0, ["NORMAL"] = 1, ["TEXCOORD_0"] = 2 },
                            ["indices"] = 3,
                            ["material"] = 0,
                        },
                    },
                },
            },
            ["materials"] = new[] { new Dictionary<string, object> { ["pbrMetallicRoughness"] = pbr } },
            ["buffers"] = new[]
            {
                new Dictionary<string, object> { ["byteLength"] = bytes.Length, ["uri"] = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) },
            },
            ["bufferViews"] = new[] { view(0, p), view(p, n), view(p + n, u), view(p + n + u, indices.Count * 2) },
            ["accessors"] = new[]
            {
                position,
                accessor(1, normals.Count / 3, "VEC3", 5126),
                accessor(2, uvs.Count / 2, "VEC2", 5126),
                accessor(3, indices.Count, "SCALAR", 5123),
            },
        };
        if (images.Length > 0)
        {
            gltf["images"] = images.Select(image => new Dictionary<string, object> { ["uri"] = image }).ToArray();
            gltf["textures"] = images.Select((_, i) => new Dictionary<string, object> { ["source"] = i }).ToArray();
        }
        return System.Text.Json.JsonSerializer.Serialize(gltf);
    }
}

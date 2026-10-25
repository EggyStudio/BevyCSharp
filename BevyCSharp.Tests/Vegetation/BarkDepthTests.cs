using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// The bark's depth for parallax occlusion, a fourth file of each bark set in BC4 at half the
/// albedo's size, the sample's derived from its normal, loaded through Bevy, and its cost read in a
/// forest at 1080p.
/// </summary>
[Collection("engine")]
public sealed class BarkDepthTests(ITestOutputHelper output)
{
    private static readonly string Collection = Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "Vegetation", "Textures");

    [Fact]
    public void EveryBarkCarriesItsDepthAndNoLeafSetDoes()
    {
        foreach (var set in TreeTextures.All.Where(set => set.Albedo.StartsWith($"{TreeTextures.Folder}/{set.Name}/", StringComparison.Ordinal)))
        {
            if (set.Kind == TreeTextureKind.Leaves)
            {
                Assert.Null(set.Depth);
                continue;
            }

            Assert.Equal($"{TreeTextures.Folder}/{set.Name}/depth.ktx2", set.Depth);
            var albedo = File.ReadAllBytes(Path.Combine(Collection, set.Name, "albedo.ktx2"));
            var depth = File.ReadAllBytes(Path.Combine(Collection, set.Name, "depth.ktx2"));
            Assert.Equal(139u, BitConverter.ToUInt32(depth, 12));
            Assert.Equal(BitConverter.ToUInt32(albedo, 20) / 2, BitConverter.ToUInt32(depth, 20));
            Assert.Equal(BitConverter.ToUInt32(albedo, 24) / 2, BitConverter.ToUInt32(depth, 24));
        }

        // The sample's depth, derived from its normal map, is half its 64 square, in BC4 under zstd.
        var sample = File.ReadAllBytes(Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Tests", "references", "textures", "sample", "depth.ktx2"));
        Assert.Equal((139u, 32u, 32u, 2u), (BitConverter.ToUInt32(sample, 12), BitConverter.ToUInt32(sample, 20), BitConverter.ToUInt32(sample, 24), BitConverter.ToUInt32(sample, 44)));
    }

    [SkippableFact]
    public void ABarksDepthLoadsAtHalfItsAlbedo()
    {
        Needs.Renderer();
        var set = TreeTextures.Find(TreeSpecies.WeepingWillow.BarkSet!)!;
        var depth = AssetHandle.None;
        (uint, uint) size = default;
        var state = AssetLoadState.Unknown;
        var run = new PictureRun { Scene = _ => depth = set.LoadDepth() };
        run.Until("loaded", _ => AssetServer.StateOf(depth) is AssetLoadState.Loaded or AssetLoadState.Failed)
            .Do("size", world =>
            {
                state = AssetServer.StateOf(depth);
                if (Render.TryImageSize(depth, out var width, out var height, out _)) size = (width, height);
            })
            .Go();

        Skip.IfNot(Render.CompressedFormats.HasFlag(CompressedImageFormats.Bc) || state == AssetLoadState.Loaded, "needs a GPU that samples BC4");
        Assert.Equal(AssetLoadState.Loaded, state);
        Assert.Equal((128u, 512u), size);
        Assert.Equal(AssetHandle.None, TreeTextures.Find(TreeSpecies.WeepingWillow.LeafSet!)!.LoadDepth());
    }

    [SkippableFact]
    public void TheDepthsCostIsReadInAForest()
    {
        Needs.Shaders();
        var flat = OpaquePass(deep: false);
        var deep = OpaquePass(deep: true);
        Skip.If(flat is null || deep is null, "needs a GPU whose passes the app can time");
        output.WriteLine($"24 black tupelos at 1920 by 1080, the opaque pass {flat:0.00} ms with flat bark and {deep:0.00} ms with depth");
    }

    /// <summary>The opaque pass's time, smoothed over the frames before, drawing a forest of tupelos at 1080p with its bark flat or with depth.</summary>
    private static double? OpaquePass(bool deep)
    {
        var species = TreeSpecies.BlackTupelo;
        var skeletons = new[] { Trees.Grow(species, 1), Trees.Grow(species, 2), Trees.Grow(species, 3) };
        IReadOnlyList<PassTiming> timings = [];
        var run = new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Frames = 900,
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f, Far = 5_000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 12f), new Vec3(0f, 4f, -20f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-3f, 5f, 2f), Vec3.Zero, Vec3.UnitY));

                var bark = Trees.BarkMaterial(species);
                if (!deep)
                {
                    var (albedo, normal, surface) = TreeTextures.Find(species.BarkSet!)!.Load();
                    bark = Render.CreateMaterial(new MaterialSettings { BaseColorTexture = albedo, NormalMap = normal, OcclusionTexture = surface, MetallicRoughnessTexture = surface, Roughness = 1f, Metallic = 1f });
                }

                var stems = skeletons.Select(skeleton => Modeling.Meshes(Trees.Build(skeleton))[0]).ToArray();
                var crowns = skeletons.Select((skeleton, i) => Render.CreateMesh(Trees.BuildLeaves(skeleton, species, (uint)(i + 1)))).ToArray();
                var leaves = Trees.LeafMaterial(species);

                // Six trees a row in four rows going away, each a third of the three trees grown.
                for (var i = 0; i < 24; i++)
                {
                    var at = Transform.At((i % 6 - 2.5f) * 7f + i / 6 % 2 * 3f, 0f, -(i / 6) * 9f);
                    foreach (var (mesh, material) in new[] { (stems[i % 3], bark), (crowns[i % 3], leaves) })
                    {
                        var entity = ecs.Spawn();
                        Render.SetMesh(ecs, entity, mesh);
                        Render.SetMaterial(ecs, entity, material);
                        ecs.Add(entity, at);
                    }
                }
            },
        };
        run.Wait(10).Until("ready", _ => Render.PipelinesReady()).Wait(240).Do("timings", _ => timings = Render.Timings()).Go();
        return timings.FirstOrDefault(timing => timing.Name == "main_opaque_pass_3d").GpuMilliseconds;
    }
}

using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a material can be made of: its factors, how it handles alpha, and its textures.
/// </summary>
/// <remarks>
/// What a surface looks like needs a GPU and an eye. What is checked here is that every setting
/// is accepted and that a texture handle reaches the material, which is the part that would
/// otherwise fail silently by drawing an untextured surface.
/// </remarks>
[Collection("engine")]
public sealed class MaterialTests
{
    private const string Texture = "textures/checker.png";

    [Fact]
    public void TheHarnessLooksForAssetsWhereTheyWereCopied()
    {
        // Every asset test fails obscurely if this is wrong, and it is wrong under any host that
        // is not the assembly's own directory, which is what a test runner usually is. Asserting
        // the path directly turns that into one clear failure instead of five confusing ones.
        Assert.True(
            Directory.Exists(EngineHarness.AssetDirectory),
            $"no assets directory at {EngineHarness.AssetDirectory}");

        Assert.True(
            File.Exists(Path.Combine(EngineHarness.AssetDirectory, "textures", "checker.png")),
            $"the texture fixture is missing from {EngineHarness.AssetDirectory}");
    }

    [Fact]
    public void AMaterialRefusesATextureHandleThatNamesNothing()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var image = AssetServer.Load(AssetKind.Image, Texture);
            AssetServer.Release(image);

            // A released handle keeps its key, so this is what a use-after-release looks like
            // from the bridge's side. Drawing untextured and reporting success would leave the
            // caller with a wrong picture and nothing to go on, which is why every other call
            // taking an asset key refuses one that names nothing.
            var stale = Assert.Throws<BevyNativeException>(() => Render.CreateMaterial(
                new MaterialSettings { BaseColorTexture = image }));

            Assert.Equal(NativeStatus.NoComponent, stale.Status);

            // Naming no texture at all stays the ordinary case, and builds an untextured
            // material rather than failing.
            var plain = Render.CreateMaterial(new MaterialSettings());
            Assert.True(plain.IsValid);
        });

        harness.Run();
    }

    [Fact]
    public void AnImageLoadsOnAnyBuild()
    {
        // No renderer guard: decoding a PNG is work on data, not on a GPU, which is why the
        // minimal profile carries the image formats too. This is the test that would have caught
        // the loader never being registered.
        using var harness = new EngineHarness(frames: 40, fps: 120);
        var state = AssetLoadState.Loading;
        var handle = AssetHandle.None;

        harness.OnContext(Stage.Startup, _ => handle = AssetServer.Load(AssetKind.Image, Texture));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = handle.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
    }

    [Fact]
    public void ATextureLoadsAndCanBeBoundToAMaterial()
    {
        using var harness = new EngineHarness(frames: 40, fps: 120);
        if (!App.HasRenderer) return;

        var image = AssetHandle.None;
        var material = AssetHandle.None;
        var imageState = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ =>
        {
            image = AssetServer.Load(AssetKind.Image, Texture);

            // Bound before the image has finished loading, on purpose: a material holds a handle
            // rather than pixels, so the texture arrives when it arrives.
            material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColorTexture = image,
                Roughness = 0.8f,
            });
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            imageState = image.State;
            if (imageState != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.True(material.IsValid);
        Assert.Equal(AssetLoadState.Loaded, imageState);
    }

    [Fact]
    public void EverySettingIsAccepted()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var glass = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (0.8f, 0.9f, 1f, 0.25f),
                AlphaMode = AlphaMode.Blend,
                Roughness = 0.05f,
            });

            var foliage = Render.CreateMaterial(new MaterialSettings
            {
                AlphaMode = AlphaMode.Mask,
                AlphaCutoff = 0.4f,
                DoubleSided = true,
            });

            var sign = Render.CreateMaterial(new MaterialSettings
            {
                Unlit = true,
                Emissive = (2f, 0.5f, 0f, 1f),
                AlphaMode = AlphaMode.Add,
            });

            Assert.True(glass.IsValid);
            Assert.True(foliage.IsValid);
            Assert.True(sign.IsValid);
            Assert.Equal(3, new HashSet<AssetHandle> { glass, foliage, sign }.Count);
        });

        harness.Run();
    }

    [Fact]
    public void EveryTextureSlotTakesAHandle()
    {
        // All five maps at once, to check the keys are read into the right slots rather than one
        // being dropped or two being swapped. Nothing here can see the result, so this is a
        // check that the call is accepted, not that the shading is right.
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var image = AssetServer.Load(AssetKind.Image, Texture);

            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColorTexture = image,
                NormalMap = image,
                MetallicRoughnessTexture = image,
                EmissiveTexture = image,
                OcclusionTexture = image,
            });

            Assert.True(material.IsValid);
        });

        harness.Run();
    }

    [Fact]
    public void AnImageLoadsWithAnExplicitSampler()
    {
        // Sampler settings ride along with the load rather than being set afterwards, so a wrong
        // one is a wrong asset rather than a wrong draw call. Any build: this is decoding.
        using var harness = new EngineHarness(frames: 40, fps: 240);
        var tiling = AssetLoadState.Loading;
        var data = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ =>
        {
            _tiling = AssetServer.LoadImage(Texture, TextureSettings.Tiling);
            _data = AssetServer.LoadImage(Texture, TextureSettings.Data);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            tiling = _tiling.State;
            data = _data.State;
            if (tiling != AssetLoadState.Loading && data != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, tiling);
        Assert.Equal(AssetLoadState.Loaded, data);

        // The same file at two samplers is two assets, because the settings are part of what was
        // asked for. A floor and its normal map can share a path and still be read differently.
        Assert.NotEqual(_tiling, _data);
    }

    [Fact]
    public void AMaterialCanTileItsTexture()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var floor = Render.CreateMaterial(new MaterialSettings
            {
                BaseColorTexture = AssetServer.LoadImage(Texture, TextureSettings.Tiling),
                UvScale = (8f, 8f),
                UvRotation = 0.25f,
                UvOffset = (0.5f, 0f),
            });

            Assert.True(floor.IsValid);
        });

        harness.Run();
    }

    [Fact]
    public void AnisotropyIsIgnoredRatherThanRefusedWhenFilteringIsNearest()
    {
        // The graphics API treats the combination as a validation failure rather than something
        // to overlook, so the bridge drops the anisotropy instead of letting it reach a draw.
        using var harness = new EngineHarness(frames: 40, fps: 240);
        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, _ => _rough = AssetServer.LoadImage(Texture,
            new TextureSettings { Anisotropy = 16, MagFilter = TextureFilter.Nearest }));

        harness.OnContext(Stage.Update, ctx =>
        {
            state = _rough.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.Equal(AssetLoadState.Loaded, state);
    }

    [Fact]
    public void TheShortColourOverloadStillWorks()
    {
        // The old argument list, kept so existing code and the simple case stay one line.
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
            Assert.True(Render.CreateMaterial(0.25f, 0.55f, 0.85f).IsValid));

        harness.Run();
    }

    [Fact]
    public void AMaterialWithNoTexturesIsStillAMaterial()
    {
        // The unset handles have to read as "no texture" rather than as a bad key.
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
            Assert.True(Render.CreateMaterial(new MaterialSettings()).IsValid));

        harness.Run();
    }

    private static AssetHandle _tiling;
    private static AssetHandle _data;
    private static AssetHandle _rough;
}

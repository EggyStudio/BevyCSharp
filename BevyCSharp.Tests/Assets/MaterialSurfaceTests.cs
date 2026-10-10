using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material's finer surface, reflectance, clearcoat, transmission, attenuation,
/// anisotropy, the exposure a lightmap is shown at, parallax mapping, the specular tint and how the
/// material is drawn, through the bridge and through the JSON a scene or a material file holds.
/// </summary>
[Collection("engine")]
public sealed class MaterialSurfaceTests
{
    private static MaterialSettings Glassy() => new()
    {
        Reflectance = 0.7f,
        Clearcoat = 0.9f,
        ClearcoatRoughness = 0.15f,
        Transmission = 0.8f,
        DiffuseTransmission = 0.2f,
        Thickness = 0.05f,
        RefractiveIndex = 1.33f,
        AttenuationDistance = 2.5f,
        AttenuationColor = (0.9f, 0.5f, 0.25f, 1f),
        AnisotropyStrength = 0.7f,
        AnisotropyRotation = 0.3f,
        LightmapExposure = 250f,
        ParallaxDepthScale = 0.09f,
        ParallaxMethod = ParallaxMethod.Relief,
        ReliefSteps = 4,
        ParallaxLayers = 32f,
        SpecularTint = (1f, 0.5f, 0f, 1f),
        OpaqueRenderMethod = OpaqueRenderMethod.Forward,
    };

    [SkippableFact]
    public void TheFinerSurfaceComesBackFromTheEngine()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        MaterialSettings? read = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            var glassy = Glassy();
            glassy.AnisotropyTexture = Render.CreateImage([128, 255, 255, 255], 1, 1, srgb: false);
            glassy.SpecularTintTexture = Render.CreateImage([255, 128, 0, 255], 1, 1);
            var material = Render.CreateMaterial(glassy);
            Assert.True(Render.TryReadMaterial(material, out read));
        });

        harness.Run();

        Assert.NotNull(read);
        var made = Glassy();
        Assert.Equal(made.Reflectance, read.Reflectance, 4);
        Assert.Equal(made.Clearcoat, read.Clearcoat, 4);
        Assert.Equal(made.ClearcoatRoughness, read.ClearcoatRoughness, 4);
        Assert.Equal(made.Transmission, read.Transmission, 4);
        Assert.Equal(made.DiffuseTransmission, read.DiffuseTransmission, 4);
        Assert.Equal(made.Thickness, read.Thickness, 4);
        Assert.Equal(made.RefractiveIndex, read.RefractiveIndex, 4);
        Assert.Equal(made.AttenuationDistance, read.AttenuationDistance, 4);
        Assert.Equal(made.AttenuationColor, read.AttenuationColor);
        Assert.Equal(made.AnisotropyStrength, read.AnisotropyStrength, 4);
        Assert.Equal(made.AnisotropyRotation, read.AnisotropyRotation, 4);

        Assert.Equal(made.LightmapExposure, read.LightmapExposure, 4);
        Assert.Equal(made.ParallaxDepthScale, read.ParallaxDepthScale, 4);
        Assert.Equal((ParallaxMethod.Relief, 4u), (read.ParallaxMethod, read.ReliefSteps));
        Assert.Equal(made.ParallaxLayers, read.ParallaxLayers, 4);
        Assert.Equal(made.SpecularTint, read.SpecularTint);

        // Last in the mirror, after the maps, so it reads true only if every field before it lines up.
        Assert.Equal(OpaqueRenderMethod.Forward, read.OpaqueRenderMethod);

        // The maps in the last slots of each group come back in those slots and no other, which a
        // field out of step between the two sides would move.
        Assert.True(read.AnisotropyTexture.IsValid);
        Assert.True(read.SpecularTintTexture.IsValid);
        Assert.False(read.SpecularTexture.IsValid);
        Assert.False(read.DepthMap.IsValid);
        Assert.False(read.ThicknessTexture.IsValid);
        Assert.False(read.ClearcoatTexture.IsValid);
    }

    [Fact]
    public void TheFinerSurfaceIsWrittenOnlyWhereItDiffers()
    {
        var references = new SceneReferences();

        // A plain material says nothing of them, so a file written before they existed reads the same.
        var plain = Written(new MaterialSettings(), references);
        Assert.False(plain.RootElement.TryGetProperty("clearcoat", out _));
        Assert.Equal(new MaterialSettings().RefractiveIndex, MaterialJson.Read(plain.RootElement).RefractiveIndex);

        var glass = Written(Glassy(), references);
        var read = MaterialJson.Read(glass.RootElement);
        Assert.Equal(0.9f, read.Clearcoat);
        Assert.Equal(1.33f, read.RefractiveIndex);
        Assert.Equal(0.8f, read.Transmission);
        Assert.Equal(0.7f, read.Reflectance);
        Assert.Equal(2.5f, read.AttenuationDistance);
        Assert.Equal((0.9f, 0.5f, 0.25f, 1f), read.AttenuationColor);
        Assert.Equal(0.7f, read.AnisotropyStrength);
        Assert.Equal(250f, read.LightmapExposure);
        Assert.False(plain.RootElement.TryGetProperty("lightmapExposure", out _));
        Assert.Equal(0.09f, read.ParallaxDepthScale);
        Assert.Equal((ParallaxMethod.Relief, 4u), (read.ParallaxMethod, read.ReliefSteps));
        Assert.Equal(32f, read.ParallaxLayers);
        Assert.Equal((1f, 0.5f, 0f, 1f), read.SpecularTint);
        Assert.Equal(OpaqueRenderMethod.Forward, read.OpaqueRenderMethod);
        Assert.False(plain.RootElement.TryGetProperty("parallaxMethod", out _));
        Assert.False(plain.RootElement.TryGetProperty("opaqueRenderMethod", out _));

        // Infinity, a clear material's distance, is left out and read back as itself.
        Assert.False(plain.RootElement.TryGetProperty("attenuationDistance", out _));
        Assert.True(float.IsPositiveInfinity(MaterialJson.Read(plain.RootElement).AttenuationDistance));
    }

    private static JsonDocument Written(MaterialSettings settings, SceneReferences references)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) MaterialJson.Write(json, settings, references);
        return JsonDocument.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}

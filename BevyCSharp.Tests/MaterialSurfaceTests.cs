using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material's finer surface, reflectance, clearcoat and transmission, through the bridge
/// and through the JSON a scene or a material file holds.
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
    };

    [SkippableFact]
    public void TheFinerSurfaceComesBackFromTheEngine()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        MaterialSettings? read = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            var material = Render.CreateMaterial(Glassy());
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
    }

    private static JsonDocument Written(MaterialSettings settings, SceneReferences references)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) MaterialJson.Write(json, settings, references);
        return JsonDocument.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}

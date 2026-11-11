using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A color made from what a person picks, and handed to settings that take four numbers.</summary>
public sealed class ColorTests
{
    [Fact]
    public void BytesAreTheSrgbTheyNameOverTwoHundredFiftyFive()
    {
        Assert.Equal(Color.FromSrgb(128 / 255f, 64 / 255f, 32 / 255f, 128 / 255f), Color.FromSrgb8(128, 64, 32, 128));
        Assert.Equal(Color.White, Color.FromSrgb8(255, 255, 255));
        Assert.Equal(Color.Black, Color.FromSrgb8(0, 0, 0));
    }

    [Theory]
    [InlineData(0f, 255, 0, 0)]
    [InlineData(120f, 0, 255, 0)]
    [InlineData(240f, 0, 0, 255)]
    [InlineData(60f, 255, 255, 0)]
    [InlineData(-120f, 0, 0, 255)]
    [InlineData(480f, 0, 255, 0)]
    public void AHueAtFullSaturationAndHalfLightnessIsItsPrimaryOrMix(float hue, byte r, byte g, byte b)
    {
        AssertClose(Color.FromSrgb8(r, g, b), Color.FromHsl(hue, 1f, 0.5f));
    }

    [Fact]
    public void NoSaturationIsGrayAtTheLightnessGiven()
    {
        AssertClose(Color.FromSrgb(0.25f, 0.25f, 0.25f), Color.FromHsl(200f, 0f, 0.25f));
        AssertClose(Color.White, Color.FromHsl(10f, 1f, 1f));
        Assert.Equal(0.5f, Color.FromHsl(10f, 1f, 0.5f, 0.5f).A);
    }

    [Fact]
    public void AColorIsTheTupleSettingsTakeAndTakesOneApart()
    {
        var color = Color.FromSrgb8(200, 100, 50);
        (float R, float G, float B, float A) tuple = color;
        Assert.Equal((color.R, color.G, color.B, color.A), tuple);

        var picked = new[] { color, (1f, 1f, 1f, 1f) };
        Assert.Equal(tuple, picked[0]);

        var material = new MaterialSettings { BaseColor = color };
        Assert.Equal(tuple, material.BaseColor);

        var (r, g, b, a) = color;
        Assert.Equal(color, new Color(r, g, b, a));
    }

    private static void AssertClose(Color expected, Color actual)
    {
        Assert.Equal(expected.R, actual.R, 4);
        Assert.Equal(expected.G, actual.G, 4);
        Assert.Equal(expected.B, actual.B, 4);
        Assert.Equal(expected.A, actual.A, 4);
    }
}

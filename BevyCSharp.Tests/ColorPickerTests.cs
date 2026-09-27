using System.Numerics;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The color spaces the picker writes numbers in, which have to come back as the color they came
/// from, or typing a number in one and reading it back in another moves the color.
/// </summary>
public sealed class ColorPickerTests
{
    [Fact]
    public void OklabMatchesTheValuesItsAuthorPublishes()
    {
        // White is lightness one with no color, and pure sRGB red is Ottosson's worked example.
        var (whiteL, whiteA, whiteB) = ColorPicker.ToOklab(Vector3.One);

        Assert.Equal(1f, whiteL, 3);
        Assert.Equal(0f, whiteA, 3);
        Assert.Equal(0f, whiteB, 3);

        var (redL, redA, redB) = ColorPicker.ToOklab(new Vector3(1f, 0f, 0f));

        Assert.Equal(0.628f, redL, 2);
        Assert.Equal(0.225f, redA, 2);
        Assert.Equal(0.126f, redB, 2);
    }

    [Fact]
    public void AColorComesBackThroughOklab()
    {
        foreach (var color in new[] { new Vector3(0.2f, 0.5f, 0.9f), new Vector3(0.9f, 0.1f, 0.3f), new Vector3(0.24f, 0.24f, 0.24f) })
        {
            var (l, a, b) = ColorPicker.ToOklab(color);
            var back = ColorPicker.FromOklab(l, a, b);

            Assert.True(Vector3.Distance(color, back) < 1e-3f, $"{color} came back as {back}");
        }
    }

    [Fact]
    public void AColorOutsideTheScreenIsBroughtInside()
    {
        // As saturated a green as OKLCH can name, which no screen shows, comes back as a color one does.
        var back = ColorPicker.FromOklab(0.8f, -0.4f, 0.4f);

        Assert.InRange(back.X, 0f, 1f);
        Assert.InRange(back.Y, 0f, 1f);
        Assert.InRange(back.Z, 0f, 1f);
    }

    [Fact]
    public void WhatAScreenCanShowIsToldFromWhatItCannot()
    {
        // Mid gray is shown as it is, and a green far more vivid than sRGB reaches is not.
        Assert.True(ColorPicker.InGamut(0.6f, 0f, 0f));
        Assert.False(ColorPicker.InGamut(0.8f, -0.4f, 0.4f));

        // And every color that came from the screen is one it can show.
        var (l, a, b) = ColorPicker.ToOklab(new Vector3(0.9f, 0.1f, 0.3f));
        Assert.True(ColorPicker.InGamut(l, a, b));
    }
}

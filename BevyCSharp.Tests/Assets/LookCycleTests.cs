using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A look keyed by the sun's height (<see cref="LookCycle"/>) and a look given again as the day turns.</summary>
[Collection("engine")]
public sealed class LookCycleTests
{
    [Fact]
    public void BetweenTwoKeysTheLookIsPartOfTheWayFromOneToTheOther()
    {
        var cycle = new LookCycle()
            .Add(-6f, new LookSettings { Saturation = 1.2f, Tint = (0.9f, 1f, 1.1f) })
            .Add(2f, new LookSettings { Saturation = 1.6f, Tint = (1.1f, 1f, 0.9f) });

        var look = cycle.At(-2f);
        Assert.Equal(1.4f, look.Saturation, 4);
        Assert.Equal((1f, 1f, 1f), (MathF.Round(look.Tint.R, 4), MathF.Round(look.Tint.G, 4), MathF.Round(look.Tint.B, 4)));
        Assert.Equal(1.2f, cycle.At(-40f).Saturation);
        Assert.Equal(1.6f, cycle.At(60f).Saturation);
    }

    [Fact]
    public void ALookReadOffACycleLeavesTheKeyAlone()
    {
        var cycle = new LookCycle().Add(0f, new LookSettings { Saturation = 1.3f });
        cycle.At(0f).Saturation = 0f;
        Assert.Equal(1.3f, cycle.At(0f).Saturation);
    }

    [Fact]
    public void TheCurveAndTheDitherChangeHalfWay()
    {
        var from = new LookSettings { Curve = LookCurve.Knee, Dither = true };
        var to = new LookSettings { Curve = LookCurve.Numbered, Dither = false };
        Assert.Equal((LookCurve.Knee, true), (LookSettings.Lerp(from, to, 0.49f).Curve, LookSettings.Lerp(from, to, 0.49f).Dither));
        Assert.Equal((LookCurve.Numbered, false), (LookSettings.Lerp(from, to, 0.5f).Curve, LookSettings.Lerp(from, to, 0.5f).Dither));
    }

    [Fact]
    public void TheDaysCycleIsVividByDayWarmAtSunsetAndCoolAtNight()
    {
        var day = LookCycle.Day();
        var vivid = LookSettings.Vivid();
        var noon = day.At(60f);
        Assert.Equal((vivid.Saturation, vivid.Contrast, vivid.Tint), (noon.Saturation, noon.Contrast, noon.Tint));

        var sunset = day.At(2f);
        Assert.True(sunset.Tint.R > sunset.Tint.B && sunset.Saturation > vivid.Saturation, $"the sunset's tint {sunset.Tint}");

        var night = day.At(-30f);
        Assert.True(night.Tint.B > night.Tint.R && night.Saturation < vivid.Saturation, $"the night's tint {night.Tint}");
        Assert.All(day.Keys, key => Assert.Equal((LookCurve.Numbered, vivid.Exposure), (key.Look.Curve, key.Look.Exposure)));
    }

    [Fact]
    public void ACycleRefusesAHeightThatIsNotANumberAndAnswersNothingWithNoKeys()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LookCycle().Add(float.NaN, new LookSettings()));
        Assert.Throws<InvalidOperationException>(() => new LookCycle().At(0f));
    }

    [SkippableFact]
    public void ALookGivenAgainChangesThePicture()
    {
        Needs.Shaders();
        // Gray, then given again with its color back, as a cycle gives a camera its look every
        // frame, which changes the look's numbers alone.
        var camera = Entity.None;
        var run = LookTests.RampRun(new LookSettings { Saturation = 0f }, hdr: true, spawned: c => camera = c);
        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady() && Looks.All().All(l => l.Ready))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("gray")
            .Do("giving the look again", _ => Render.SetLook(camera, new LookSettings { Saturation = 1f, Dither = false }))
            .Wait(10)
            .Capture("colored")
            .Go();

        var gray = run.Picture("gray").At(600, 1);
        var colored = run.Picture("colored").At(600, 1);
        Assert.True(Math.Abs(gray.R - gray.G) <= 2, $"the red row under a gray look reads {gray}");
        Assert.True(colored.R - Math.Max(colored.G, colored.B) > 100, $"the red row given its color back reads {colored}");
    }
}

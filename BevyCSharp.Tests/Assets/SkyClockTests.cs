using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Where <see cref="SkyClock"/> puts the sun, the moon and the stars, against the spherical astronomy it follows.</summary>
public sealed class SkyClockTests
{
    [Fact]
    public void AtTheEquinoxNoonsSunStandsAsHighAsTheLatitudeLets()
    {
        var clock = new SkyClock { Latitude = 45f, DayOfYear = 79f, Hour = 12f };
        Assert.InRange(clock.SunAltitude, 44f, 46f);
        clock.Hour = 0f;
        Assert.InRange(clock.SunAltitude, -46f, -44f);
    }

    [Fact]
    public void WithNoTiltTheSunRisesInTheEastAtSixAndSetsAtEighteen()
    {
        var clock = new SkyClock { Latitude = 30f, AxialTilt = 0f, DayOfYear = 200f, Hour = 6f };
        Assert.InRange(clock.SunAltitude, -1f, 1f);
        Assert.True(clock.SunDirection.X > 0.9f, $"the rising sun stands along {clock.SunDirection}");
        clock.Hour = 18f;
        Assert.InRange(clock.SunAltitude, -1f, 1f);
        Assert.True(clock.SunDirection.X < -0.9f, $"the setting sun stands along {clock.SunDirection}");
    }

    [Fact]
    public void TheSolsticesNoonsStandAsHighAsTheTiltSays()
    {
        var summer = new SkyClock { Latitude = 45f, DayOfYear = 171f, Hour = 12f };
        var winter = new SkyClock { Latitude = 45f, DayOfYear = 354f, Hour = 12f };
        Assert.InRange(summer.SunAltitude, 67.4f, 69.4f);
        Assert.InRange(winter.SunAltitude, 20.6f, 22.6f);
    }

    [Fact]
    public void TheFullMoonStandsOppositeTheSunAndTheNewBesideIt()
    {
        var full = new SkyClock { DayOfYear = 79f, Hour = 0f, MoonAgeAtStart = (SkyClock.SynodicMonth / 2f) - 79f };
        Assert.True(Vec3.Dot(full.MoonDirection, full.SunDirection) < -0.95f, $"the full moon stands along {full.MoonDirection}, the sun along {full.SunDirection}");
        Assert.True(full.MoonLit > 0.99f && full.MoonAltitude > 0f, $"the full moon is lit {full.MoonLit} at {full.MoonAltitude} degrees at midnight");

        var fresh = new SkyClock { DayOfYear = 79f, Hour = 12f, MoonAgeAtStart = -79.5f };
        Assert.True(Vec3.Dot(fresh.MoonDirection, fresh.SunDirection) > 0.95f, $"the new moon stands along {fresh.MoonDirection}, the sun along {fresh.SunDirection}");
        Assert.True(fresh.MoonLit < 0.01f, $"the new moon is lit {fresh.MoonLit}");
    }

    [Fact]
    public void AdvancingPastMidnightTurnsTheDayOver()
    {
        var clock = new SkyClock { DayOfYear = 364.5f, Hour = 22f };
        clock.Advance(5f);
        Assert.InRange(clock.Hour, 2.99f, 3.01f);
        Assert.InRange(clock.DayOfYear, 0.49f, 0.51f);
        clock.Advance(-4f);
        Assert.InRange(clock.Hour, 22.99f, 23.01f);
        Assert.InRange(clock.DayOfYear, 364.49f, 364.51f);
    }

    [Fact]
    public void ThePoleStarStandsStillAsHighAsTheLatitude()
    {
        var clock = new SkyClock { Latitude = 52f };
        foreach (var hour in new[] { 0f, 7f, 15.5f })
        {
            clock.Hour = hour;
            var pole = clock.TowardSky(1.3f, MathF.PI / 2f);
            Assert.True(Vec3.Dot(pole, clock.Pole) > 0.9999f, $"at {hour} the pole star stands along {pole} where the pole is {clock.Pole}");
        }
        Assert.InRange(MathF.Asin(clock.Pole.Y) * 180f / MathF.PI, 51.9f, 52.1f);
    }

    [Fact]
    public void AStarComesBackAfterASiderealDay()
    {
        var clock = new SkyClock { Latitude = 40f, DayOfYear = 100f, Hour = 21f };
        var before = clock.TowardSky(2.1f, 0.4f);
        clock.Advance(23.9345f);
        var after = clock.TowardSky(2.1f, 0.4f);
        Assert.True(Vec3.Dot(before, after) > 0.99995f, $"the star stood along {before} and a sidereal day on along {after}");
    }

    [Fact]
    public void ThePartsOfTheDayFollowTheSun()
    {
        var clock = new SkyClock { Latitude = 45f, DayOfYear = 79f };
        (float Hour, DayPhase Phase)[] expected = [(0f, DayPhase.Night), (5.5f, DayPhase.Dawn), (12f, DayPhase.Day), (18.5f, DayPhase.Dusk), (22f, DayPhase.Night)];
        foreach (var (hour, phase) in expected)
        {
            clock.Hour = hour;
            Assert.Equal(phase, clock.Phase);
        }
    }
}

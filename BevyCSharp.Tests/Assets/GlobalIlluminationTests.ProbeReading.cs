using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A probe in a closed room of 8 holds its reference's light along its ways (<c>gi.probe</c>)
    /// held at its first bounce, at two past it and at every one (<c>gi.bounces</c>), and held at a
    /// count it stands still, read twice alike.
    /// </summary>
    /// <remarks>
    /// The probe's light at a count of bounces is read against paths meeting that many faces past
    /// the first, drawn across each texel at 256 paths a way. The room's light is mostly its lamp's
    /// first bounce and the bounces after it, the white walls taking 0.787 of what reaches them, so
    /// a probe that lost a share at each bounce, or a hold that carried the light a bounce too far
    /// or too short, reads off its reference by more than the reference's noise. The probe reads
    /// 1.8% under, 0.1% over and 1.6% over, and stood still a frame early it read two bounces past
    /// the first 20% under, so the bar is 10%.
    /// </remarks>
    [SkippableFact]
    public void AProbeInAClosedRoomHoldsItsReferencesLightAtEachBounceItIsHeldAt()
    {
        Needs.Shaders();
        var supported = false;
        var ready = 0;
        var at = new Vector3(1f, 1.5f, 1f);
        var read = new List<(string Held, (double Probe, double Middle, double Drawn, int Within) Reading)>();
        var run = new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 12000,
            Scene = ecs => ClosedRoom(ecs, 8f),
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 300)
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3);

        // Each hold, the paths' faces past the first it is read against, and whether it is asked
        // again or read again as it stands, every bounce waited for over 400 frames.
        foreach (var (held, faces, asked, label) in new (int?, int, bool, string)[]
            { (0, 0, true, "the first bounce"), (2, 2, true, "two bounces past it"), (2, 2, false, "two again"), (null, -1, true, "every bounce") })
        {
            var (waited, begun) = (0, false);
            run = run.Do($"holding at {label}", _ =>
                {
                    if (supported && asked) WorldProbes.HoldBounces(held);
                })
                .Until($"held at {label}", _ => !supported || (held is null ? ++waited >= 400 : WorldProbes.HoldReached))
                .Until($"read at {label}", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginProbe(ecs, at, 0, 256, faces));
                        return false;
                    }
                    if (GiReference.StepProbe(ecs) is null) return false;
                    read.Add((label, GiReference.LastProbe!.Value));
                    return true;
                });
        }
        run.Go();
        Needs.RayQueries(supported);

        var faults = new List<string>();
        foreach (var (label, reading) in read)
        {
            var off = reading.Probe / reading.Drawn - 1;
            output.WriteLine($"at {label} the probe holds {reading.Probe:0.0000} against {reading.Drawn:0.0000} drawn and {reading.Middle:0.0000} along the middles, {off:+0.000;-0.000}");
            if (Math.Abs(off) > 0.1) faults.Add($"at {label} the probe holds {off:+0.000;-0.000} of its reference's light");
        }
        if (read.Count == 4 && read[1].Reading.Probe != read[2].Reading.Probe)
            faults.Add($"held at two bounces the probe read {read[1].Reading.Probe:0.000000} and then {read[2].Reading.Probe:0.000000}");
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }
}

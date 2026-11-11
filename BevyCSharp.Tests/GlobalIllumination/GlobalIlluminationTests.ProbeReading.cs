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
    /// or too short, reads off its reference by more than the reference's noise. The reference's
    /// paths are lit as Bevy's lighting lights a face and scatter by its gloss, as the probes' rays
    /// carry the gloss of what they meet, and the probe reads a few percent under them, the probes
    /// lighting a face by Lambert's term and its gloss's share as though even (shadeProbeHit) where
    /// the reference lights it by Burley's term and the GGX lobe toward the probe. So the bar is
    /// 10%, nearer than a hold a bounce off reads.
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

    /// <summary>
    /// A screen probe on the floor of a closed room of 8 sums its sixteen rays' light within 10% of
    /// the reference's along the same ways (<c>gi.screenprobe</c>), held at its first bounce and at
    /// every one, each ray met somewhere.
    /// </summary>
    /// <remarks>
    /// The screen kernel writes the probe's rays as it traces them, and the reference traces from
    /// where they leave along each, so a ray logged or read back wrong, a reference traced from the
    /// wrong place, or a probe summing its rays as it does not reads off the reference by more than
    /// its paths' noise. The probe on the floor under the picture's lower middle reads a few
    /// percent under its reference, its paths lit as Bevy's lighting lights a face and scattering
    /// by its gloss, and with its ways handed to the reference in the wrong order it reads farther
    /// off than the bar of 10%.
    /// </remarks>
    [SkippableFact]
    public void AScreenProbeInAClosedRoomSumsItsReferencesLightAlongItsWays()
    {
        Needs.Shaders();
        var supported = false;
        var ready = 0;
        var read = new List<(string Held, (double Probe, double Along, double Drawn, int OnPicture, int InField, int Beyond, int PastReach) Reading)>();
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
        foreach (var (held, faces, label) in new (int?, int, string)[] { (0, 0, "the first bounce"), (null, -1, "every bounce") })
        {
            var (waited, begun) = (0, false);
            run = run.Do($"holding at {label}", _ => { if (supported) WorldProbes.HoldBounces(held); })
                .Until($"held at {label}", _ => !supported || ++waited >= (held is null ? 400 : 30))
                .Until($"read at {label}", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginScreenProbe(ecs, 80, 70, 256, faces));
                        return false;
                    }
                    if (GiReference.StepScreenProbe(ecs) is null) return false;
                    read.Add((label, GiReference.LastScreenProbe!.Value));
                    return true;
                });
        }
        run.Go();
        Needs.RayQueries(supported);

        var faults = new List<string>();
        foreach (var (label, reading) in read)
        {
            var off = reading.Probe / reading.Along - 1;
            var met = reading.OnPicture + reading.InField + reading.Beyond + reading.PastReach;
            output.WriteLine($"at {label} the screen probe sums {reading.Probe:0.0000} against {reading.Along:0.0000} along its ways and {reading.Drawn:0.0000} drawn across them, {off:+0.000;-0.000}, its rays met {reading.OnPicture} on the picture, {reading.InField} in the field, {reading.Beyond} beyond and {reading.PastReach} past their reach");
            if (Math.Abs(off) > 0.1) faults.Add($"at {label} the screen probe sums {off:+0.000;-0.000} of its reference's light along its ways");
            if (met != 16) faults.Add($"at {label} {met} of the screen probe's sixteen rays were met");
        }
        Assert.True(read.Count == 2 && faults.Count == 0, string.Join("; ", faults));
    }
}

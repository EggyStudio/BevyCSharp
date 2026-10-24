using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An exposed emissive on a camera drawn deferred, as the field's shadows and global illumination
/// draw one, shows the light it shows drawn forward, the bridge's mend of Bevy's G-buffer
/// (<c>render/mended.rs</c>) applying the camera's exposure as the G-buffer is written, where Bevy
/// 0.20 keeps the emissive's color alone and shows it unexposed, about a thousand times too bright
/// at Bevy's default exposure.
/// </summary>
[Collection("engine")]
public sealed class DeferredEmissiveTests
{
    /// <summary>A lamp panel's emission in nits, exposed by the camera, as the feature test's Cornell box has it.</summary>
    private static readonly (float R, float G, float B, float A) Lamp = (40_000f, 38_000f, 34_000f, 1f);

    /// <summary>
    /// An exposed emissive panel reads the same light before tonemapping on a camera drawn deferred
    /// as on one drawn forward, within 2%, and the mend reports itself made.
    /// </summary>
    [SkippableFact]
    public void AnExposedEmissiveReadsTheSameDeferredAsForward()
    {
        Needs.Shaders();
        var (forward, _) = Panel(deferred: false, reference: false);
        var (deferred, mended) = Panel(deferred: true, reference: false);
        Assert.True(mended == true, $"the bridge's mend of Bevy's deferred G-buffer reads {mended?.ToString() ?? "not made"}");
        Assert.True(forward > 1f, $"drawn forward the panel reads {forward}");
        Assert.True(Math.Abs(deferred / forward - 1f) < 0.02f, $"drawn deferred the panel reads {deferred}, forward {forward}");
    }

    /// <summary>
    /// The lamp panel on a camera drawn deferred reads within 2% of a path-traced reference of the
    /// same view (<c>gi.reference</c>), whose panel emits its light exposed as Bevy shows it, where
    /// unmended it read 40,053 against the reference's 49 in the feature test's Cornell box.
    /// </summary>
    [SkippableFact]
    public void ALampPanelOnADeferredCameraReadsWithinItsReference()
    {
        Needs.Shaders();
        var (seen, _) = Panel(deferred: true, reference: true);
        if (float.IsNaN(seen)) Needs.RayQueries(false);
        var traced = GiReference.Last!;
        var middle = traced.Light.AsSpan(((int)traced.Height / 2 * traced.Width + traced.Width / 2) * 3, 3);
        var wanted = 0.2126f * middle[0] + 0.7152f * middle[1] + 0.0722f * middle[2];
        Assert.True(Math.Abs(seen / wanted - 1f) < 0.02f, $"drawn deferred the panel reads {seen}, its reference {wanted}");
    }

    /// <summary>
    /// The luminance of the middle pixel of a camera's picture before tonemapping, looking straight at
    /// an exposed emissive panel that fills it, drawn deferred or forward, with whether the mend was
    /// made, and a reference traced first where asked, or NaN where the GPU traces no rays for one.
    /// </summary>
    private static (float Light, bool? Mended) Panel(bool deferred, bool reference)
    {
        var (light, mended) = (0f, (bool?)null);
        var (supported, begun, read) = (true, false, false);
        new PictureRun
        {
            Width = 32,
            Height = 32,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0f, 3f));
                // Drawn once a pixel either way, which deferred needs, so the two differ in that alone,
                // and in floats, so the light is read before tonemapping as it is.
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                if (deferred) Shaders.SetPrepass(camera, depth: true, deferred: true);
                var panel = ecs.Spawn();
                Render.SetMesh(ecs, panel, Render.CreateMesh(MeshShape.Cuboid, 4f, 4f, 0.1f));
                Render.SetMaterial(ecs, panel, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Emissive = Lamp }));
                ecs.Add(panel, Transform.At(0f, 0f, 0f));
            },
        }
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(10)
            .Until("referenced", world =>
            {
                if (!reference) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    supported = Shaders.SupportsRayQueries;
                    if (supported) Assert.Null(GiReference.BeginReference(ecs, null, 64));
                    return !supported;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Until("read", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    mended = Render.EmissiveExposureMended;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();

        if (!supported) return (float.NaN, mended);
        var (width, height, picture) = GiReference.LastPicture!.Value;
        var at = (height / 2 * width + width / 2) * 3;
        light = 0.2126f * picture[at] + 0.7152f * picture[at + 1] + 0.0722f * picture[at + 2];
        return (light, mended);
    }
}

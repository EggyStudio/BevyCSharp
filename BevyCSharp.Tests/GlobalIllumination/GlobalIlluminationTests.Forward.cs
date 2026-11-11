using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A floor of a standard material drawn forward, seen at a slant under a bright environment
    /// map and an ambient light, reads as Bevy lit it with global illumination on as with it off.
    /// </summary>
    /// <remarks>
    /// The light pass lights the G-buffer's surfaces and is to pass over a pixel no deferred
    /// material drew, telling it apart from Bevy's clear of the G-buffer, black with an alpha of
    /// one, which reads as a black mirror, so a pixel drawn forward taken for a surface would take
    /// the reflection of the sky Bevy gives a mirror at a slant with nothing given, leaving the
    /// feature test's meadow's leaves dark red.
    /// </remarks>
    [SkippableFact]
    public void ASurfaceDrawnForwardKeepsTheLightBevyGaveIt()
    {
        Needs.Shaders();
        var ready = 0;
        var run = new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 3000,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0.8f, 6f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0.8f, 6f), new Vec3(0f, 0f, -4f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 400f);
                var sky = Cube(0.4f, 0.6f, 1f);
                Render.SetEnvironmentMap(camera, sky, sky, intensity: 3000f);
                Floor(ecs, (0.8f, 0.8f, 0.8f));
                Sun(ecs, new Vec3(3f, 6f, 2f));

                // A red sheet over the floor in front of the camera, drawn forward.
                var sheet = ecs.Spawn();
                Render.SetMesh(ecs, sheet, Render.CreateMesh(MeshShape.Cuboid, 6f, 0.02f, 8f));
                Render.SetMaterial(ecs, sheet, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.6f, 0.1f, 0.1f, 1f),
                    Roughness = 0.5f,
                    OpaqueRenderMethod = OpaqueRenderMethod.Forward,
                }));
                ecs.Add(sheet, Transform.At(0f, 0.01f, 0f));
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("off")
            .Do("turning global illumination on", _ => Render.SetGlobalIllumination(GlobalIllumination.Medium))
            .Until("settled", _ => SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => WorldProbes.TracedFrames >= 60)
            .Until("drawn", _ => EngineDrawsOn(traced: true))
            .Until("ready", _ => (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Wait(10)
            .Capture("on")
            .Go();

        // The sheet across the lower part of the picture, seen at a slant.
        var (off, on) = (Mean(run.Picture("off"), 30, 60, 100, 24), Mean(run.Picture("on"), 30, 60, 100, 24));
        output.WriteLine($"the sheet reads {on} with global illumination on against {off} with it off");
        Assert.True(off.X > 40, $"the sheet is lit, {off}");
        Assert.True(Vector3.Distance(on, off) < 0.05f * off.Length(), $"the sheet reads {on} with global illumination on against {off} with it off");
    }

    /// <summary>
    /// A metal floor polished nearly to a mirror reflects a sunlit red wall drawn forward standing
    /// on it with global illumination on, as it reflects one the G-buffer holds.
    /// </summary>
    /// <remarks>
    /// The reflection is traced through the picture's depth and passes over a face it meets from
    /// behind, which it reads from the G-buffer's normal. A wall drawn forward has no texel there
    /// but Bevy's clear, whose normal decodes as facing along -z, so a ray from the floor meeting
    /// the wall would be turned away, and the trace takes such a texel as met. The trace's other
    /// paths and the field find the wall all the same, a wall thinner than the field's cells as
    /// well, so the floor reflects it nearly as brightly without the check, and this holds that a
    /// wall drawn forward is reflected rather than the check itself.
    /// </remarks>
    [SkippableFact]
    public void AMirrorFloorReflectsARedWallDrawnForward()
    {
        Needs.Shaders();
        var off = Seen(GlobalIllumination.Off);
        var on = Seen(GlobalIllumination.Low);
        output.WriteLine($"with global illumination on the floor reads {on}, off {off}");
        Assert.True(on.R > off.R + 30, $"with global illumination on the floor reads {on}, off {off}");
        Assert.True(on.R > on.G + 30, $"with global illumination on the floor reads {on}, not red");

        static (int R, int G, int B) Seen(GlobalIllumination quality) => Looking(
            ecs =>
            {
                var floor = ecs.Spawn();
                Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 20f, 0.2f, 20f));
                Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.9f, 0.9f, 0.9f, 1f),
                    Metallic = 1f,
                    Roughness = 0.1f,
                }));
                ecs.Add(floor, Transform.At(0f, -0.1f, 0f));
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, 6f, 3f, 0.4f));
                Render.SetMaterial(ecs, wall, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.8f, 0.05f, 0.05f, 1f),
                    Roughness = 1f,
                    OpaqueRenderMethod = OpaqueRenderMethod.Forward,
                }));
                ecs.Add(wall, Transform.At(0f, 1.5f, -2f));
                Sun(ecs, new Vec3(1f, 6f, 6f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(quality);
            },
            stills: 2,
            eye: new Vec3(0f, 1.2f, 4f),
            at: new Vec3(0f, 0f, 0f),
            ambient: 0f,
            traced: quality != GlobalIllumination.Off);
    }

    /// <summary>
    /// A leaf in a closed room lit by a lamp behind it, its face toward the eye in shade, reads the
    /// light the probes give it as a card of a standard material beside it reads it, both of one
    /// color and facing one way under an ambient light the probes' light takes the place of.
    /// </summary>
    /// <remarks>
    /// The light pass lights the G-buffer's surfaces, and a leaf is drawn forward, since the
    /// G-buffer holds one normal and a leaf is lit by two, so with the feature test's fill off
    /// under global illumination the meadow's leaves and grass took the sky's small ambient light
    /// alone in shade and lay near black. The leaf material reads the probes as the pass does.
    /// </remarks>
    [SkippableFact]
    public void ALeafInShadeTakesTheProbesLightAsTheSurfaceBesideIt()
    {
        Needs.Shaders();
        var run = LeafRoom(lamp: true, ambient: 300f, through: 0f, on: true);
        var (card, leaf) = (Mean(run.Picture("on"), 40, 38, 16, 16), Mean(run.Picture("on"), 105, 38, 16, 16));
        output.WriteLine($"the leaf reads {leaf} against the card's {card}");
        Assert.True(card.Y > 25, $"the card beside the leaf is lit by the room's light, {card}");
        Assert.True(Vector3.Distance(leaf, card) < 0.12f * card.Length(), $"the leaf reads {leaf} against the card's {card}");
    }

    /// <summary>
    /// A leaf that lets all its light through, in a closed room with no lamp, gives back the light
    /// Bevy's ambient light sends through it once global illumination is on, as the light pass
    /// takes the ambient light back from the surfaces around it.
    /// </summary>
    /// <remarks>
    /// Bevy gives the light let through a leaf from all around at an occlusion of one, so the leaf
    /// works it out again and takes it back, where taking back its diffuse light alone left the
    /// light through it lit by an ambient light the room's walls shut out.
    /// </remarks>
    [SkippableFact]
    public void ALeafGivesBackTheLightBevyLetsThroughItFromAllAround()
    {
        Needs.Shaders();
        var run = LeafRoom(lamp: false, ambient: 1000f, through: 1f, on: false);
        var (off, on) = (Mean(run.Picture("off"), 105, 38, 16, 16), Mean(run.Picture("on"), 105, 38, 16, 16));
        output.WriteLine($"the leaf reads {on} with global illumination on against {off} with it off");
        Assert.True(off.Y > 25, $"the ambient light reaches the leaf through it with global illumination off, {off}");
        Assert.True(on.Y < 0.35f * off.Y, $"the leaf reads {on} with global illumination on against {off} with it off");
    }

    /// <summary>
    /// A closed room under a camera looking at its back wall, a card of a standard material on the
    /// left and a leaf of the leaf material on the right, both green, an eighth of a unit thick,
    /// facing the camera, with a lamp behind them where asked, pictured with global illumination at
    /// <see cref="GlobalIllumination.Medium"/> as "on" and, where asked, first without as "off".
    /// </summary>
    private static PictureRun LeafRoom(bool lamp, float ambient, float through, bool on)
    {
        var ready = 0;
        var run = new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 3000,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.5f, 3f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.5f, 3f), new Vec3(0f, 1.2f, -4f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), ambient);

                var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f, roughness: 0.9f);
                void Slab(Vec3 at, Vec3 size, AssetHandle material)
                {
                    var slab = ecs.Spawn();
                    Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, slab, material);
                    ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
                }

                Slab(new Vec3(0f, -0.2f, 0f), new Vec3(8.8f, 0.4f, 8.8f), white);
                Slab(new Vec3(0f, 3.2f, 0f), new Vec3(8.8f, 0.4f, 8.8f), white);
                Slab(new Vec3(-4.2f, 1.5f, 0f), new Vec3(0.4f, 3f, 8f), white);
                Slab(new Vec3(4.2f, 1.5f, 0f), new Vec3(0.4f, 3f, 8f), white);
                Slab(new Vec3(0f, 1.5f, 4.2f), new Vec3(8f, 3f, 0.4f), white);
                Slab(new Vec3(0f, 1.5f, -4.2f), new Vec3(8f, 3f, 0.4f), white);

                var color = (0.2f, 0.6f, 0.2f, 1f);
                Slab(new Vec3(-1.2f, 1.3f, -1f), new Vec3(1.6f, 1.6f, 0.125f), Render.CreateMaterial(new MaterialSettings { BaseColor = color, Roughness = 1f, Reflectance = 0f }));
                Slab(new Vec3(1.2f, 1.3f, -1f), new Vec3(1.6f, 1.6f, 0.125f), Render.CreateLeafMaterial(new MaterialSettings
                {
                    BaseColor = color,
                    Roughness = 1f,
                    Reflectance = 0f,
                    DiffuseTransmission = through,
                    AlphaMode = AlphaMode.Mask,
                    DoubleSided = true,
                }, (0f, 0f, 0f, 0f)));

                if (lamp)
                {
                    var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 300_000f, Range = 14f, Radius = 0.2f, Shadows = false });
                    ecs.Add(light, Transform.At(0f, 2.4f, -3f));
                }

                if (on) Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        };

        if (!on)
            run.Wait(ShaderMaterialTests.Settled).Capture("off").Do("turning global illumination on", _ => Render.SetGlobalIllumination(GlobalIllumination.Medium));
        run.Until("settled", _ => SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => EngineDrawsOn(traced: true))
            .Until("ready", _ => (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Wait(10)
            .Capture("on")
            .Go();
        return run;
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room lit by the ambient light alone reads no light below nothing with Bevy's
    /// screen-space ambient occlusion on, where the occlusion darkens Bevy's own light in it.
    /// </summary>
    /// <remarks>
    /// The light pass takes back the ambient light Bevy's lighting gave each surface, and in a room
    /// closed to the sky the probes add almost none in its place, so the room reads near nothing
    /// with global illumination on. Bevy's lighting gives a surface the ambient light times the
    /// lesser of its material's occlusion and the screen's, so a pass taking it back under the
    /// material's alone took more than Bevy gave wherever the screen's darkens a corner, and the
    /// picture read below nothing there, which tonemapping shows as black where the probes' light
    /// should have stood. The room's light with global illumination off is read with and without
    /// the occlusion as well, so the test cannot pass with the occlusion never turned on.
    /// </remarks>
    [SkippableFact]
    public void AClosedRoomWithBevysAmbientOcclusionReadsNoLightBelowNothing()
    {
        Needs.Shaders();

        var supported = false;
        var ready = 0;
        var pictures = new Dictionary<string, float[]>();
        var camera = Entity.None;

        // Reads the camera's light before tonemapping over the frames it takes, kept under a name.
        Func<World, bool> Read(string name)
        {
            var begun = false;
            return world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }

                if (GiReference.Step(ecs) is null) return false;
                pictures[name] = GiReference.LastPicture!.Value.Light;
                return true;
            };
        }

        bool Ready(World _) => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3;

        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 12000,
            Scene = ecs => camera = ClosedRoom(ecs),
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 300)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", Ready)
            .Do("occluding", _ =>
            {
                if (supported) Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.High);
            })
            .Wait(30)
            .Until("ready to occlude", Ready)
            .Until("read with global illumination", Read("on"))
            .Do("global illumination off", _ =>
            {
                if (supported) Render.SetGlobalIllumination(GlobalIllumination.Off);
            })
            .Wait(30)
            .Until("ready without it", Ready)
            .Until("read without global illumination", Read("occluded"))
            .Do("occlusion off", _ =>
            {
                if (supported) Render.SetAmbientOcclusion(camera, null);
            })
            .Wait(30)
            .Until("ready without occlusion", Ready)
            .Until("read without either", Read("open"))
            .Go();

        Needs.RayQueries(supported);

        // Bevy's own light with the occlusion on and off, which says the occlusion darkened it.
        var (occluded, open) = (pictures["occluded"].Sum(), pictures["open"].Sum());
        output.WriteLine($"Bevy's light in the room reads {occluded / open:0.000} of itself with the occlusion on");
        Assert.True(occluded < 0.95f * open, $"the occlusion darkened Bevy's light in the room to {occluded / open:0.000} of itself, too little to read the pass by");

        // The light the room reads with global illumination on, a pixel at a time, in the units in
        // which the walls' white reads 0.787 under the ambient light with it off.
        var on = pictures["on"];
        var (lowest, below) = (0f, 0);
        for (var i = 0; i < on.Length / 3; i++)
        {
            var light = 0.2126f * on[i * 3] + 0.7152f * on[i * 3 + 1] + 0.0722f * on[i * 3 + 2];
            lowest = Math.Min(lowest, light);
            if (light < -0.02f) below++;
        }

        output.WriteLine($"with global illumination the room reads {lowest:0.000} at its lowest, {below} pixels under -0.02");
        Assert.True(below == 0, $"with global illumination {below} pixels of the room read below nothing, the lowest {lowest:0.000}");
    }

    /// <summary>
    /// A room of the level's white 4 units across and 3 high, closed on every side and reflecting
    /// nothing, under a white sky of a light of one and no other light, the camera inside it
    /// looking into a far corner, in a field of four cascades of a quarter unit. Answers the
    /// camera.
    /// </summary>
    private static Entity ClosedRoom(EcsWorld ecs)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(1.2f, 1.6f, 1.2f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(1.2f, 1.6f, 1.2f), new Vec3(-2f, 0f, -2f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight(camera, (1f, 1f, 1f), 1f / (Exposure * AmbientReturned));

        var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.787f, 0.787f, 0.787f, 1f), Roughness = 0.9f, Reflectance = 0f });
        foreach (var (size, at) in new[]
        {
            (new Vec3(4.4f, 0.2f, 4.4f), new Vec3(0f, -0.1f, 0f)),
            (new Vec3(4.4f, 0.2f, 4.4f), new Vec3(0f, 3.1f, 0f)),
            (new Vec3(0.2f, 3f, 4.4f), new Vec3(-2.1f, 1.5f, 0f)),
            (new Vec3(0.2f, 3f, 4.4f), new Vec3(2.1f, 1.5f, 0f)),
            (new Vec3(4f, 3f, 0.2f), new Vec3(0f, 1.5f, -2.1f)),
            (new Vec3(4f, 3f, 0.2f), new Vec3(0f, 1.5f, 2.1f)),
        })
        {
            var part = ecs.Spawn();
            Render.SetMesh(ecs, part, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
            Render.SetMaterial(ecs, part, white);
            ecs.Add(part, Transform.At(at.X, at.Y, at.Z));
        }

        Render.SetSceneField(4, 0.25f);
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
        return camera;
    }
}

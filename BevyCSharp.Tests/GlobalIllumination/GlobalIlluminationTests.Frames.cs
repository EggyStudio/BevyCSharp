using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room lit through indirect light alone by a wide emissive panel on its ceiling, at
    /// <see cref="GlobalIllumination.Low"/>, reads under a twentieth of its lit level within two
    /// frames of the panel going dark.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each frame a probe's rays take the indirect light the probes held the frame before, at the
    /// share of it the probe's own light kept against the frame before's, so the indirect light
    /// goes as the light that made it goes. The share is worked out from the light every ray of the
    /// probe brought. Both of Low's cascades cast four rays a texel, a probe's rays summed in four
    /// workgroups, and a share read from the first workgroup's sum alone is a band of the sphere
    /// the panel can lie outside of. The room then kept its light over the frames each bounce takes,
    /// 37 of its 75 two frames after and 14 six frames after, where with every workgroup summed it
    /// reads under 1 from two frames after.
    /// </para>
    /// <para>
    /// The panel is wider than an emitter the field carries as a glow light, whose light a probe
    /// takes in closed form in every band, so its light reaches the probes only through their rays.
    /// A room of half the size lit by a panel the field carries reads alike either way.
    /// </para>
    /// </remarks>
    [SkippableFact]
    public void TheIndirectLightGoesWithinTwoFramesOfThePanelThatWentDark()
    {
        Needs.Shaders();
        var supported = false;
        var (panel, dark, ready) = (Entity.None, AssetHandle.None, 0);
        var tickets = new List<Capture>();
        var asked = ulong.MaxValue;
        var pictures = new Dictionary<int, CapturedImage>();

        var run = new PictureRun
        {
            Width = 160,
            Height = 96,
            Frames = 6000,
            Scene = ecs =>
            {
                panel = PanelRoom(ecs);
                dark = Render.CreateMaterial(0f, 0f, 0f);
            },
        };
        run.Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 120)
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Capture("lit")
            .Do("putting the panel out", world =>
            {
                if (!supported) return;
                Render.SetMaterial(world.Resource<EcsWorld>(), panel, dark);
            })
            // A picture asked for each frame from the one the panel went dark in, each read as it
            // comes back off the GPU. One a frame, since Bevy drops a second picture of the same
            // target asked for in a frame, and the step before ends in the frame this one starts.
            .Until("captured the frames after", world =>
            {
                if (!supported) return true;
                var now = world.Resource<Time>().FrameCount;
                if (tickets.Count < 7 && now != asked)
                {
                    tickets.Add(Render.BeginCapture());
                    asked = now;
                }

                for (var frame = 0; frame < tickets.Count; frame++)
                    if (!pictures.ContainsKey(frame) && Render.TryReadCapture(tickets[frame], out var picture) && picture is not null)
                        pictures[frame] = picture;
                return pictures.Count == 7;
            })
            .Go();
        Needs.RayQueries(supported);

        var lit = Mean(run.Picture("lit"), 0, 0, 160, 96).X;
        var after = Enumerable.Range(0, 7).Select(frame => Mean(pictures[frame], 0, 0, 160, 96).X).ToList();
        var levels = string.Join(", ", after.Select(level => $"{level:0.00}"));
        output.WriteLine($"the room reads {lit:0.00} lit and {levels} the frames after");

        Assert.True(lit > 40, $"the panel lights its room through indirect light, reading {lit:0.00}");
        var gone = after.FindIndex(level => level < lit / 20);
        Assert.True(gone is >= 0 and <= 2, $"the indirect light goes with the panel, the room reading {levels} the frames after");
    }

    /// <summary>
    /// A closed room of the level's white, eight units across and three high, an emissive panel 4.4
    /// across on its ceiling's middle and the camera inside by its front wall, the panel's own face
    /// out of view, lit by nothing else, global illumination at <see cref="GlobalIllumination.Low"/>.
    /// </summary>
    /// <remarks>
    /// The panel gives the light of one 1.2 across at an emissive strength of 2,000, which lights
    /// the room's white well inside the picture's range.
    /// </remarks>
    private static Entity PanelRoom(EcsWorld ecs)
    {
        var (h, high, t, side) = (4f, 3f, 0.3f, 4.4f);
        var eye = new Vec3(0f, 1.2f, h - 0.4f);
        var camera = PictureRun.Camera(ecs, eye);
        ecs.Add(camera, Transform.LookingAt(eye, new Vec3(0f, 0.9f, -h), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        var white = Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: 0.9f);
        Entity Slab(string name, Vec3 at, Vec3 size, AssetHandle material)
        {
            var slab = ecs.Spawn();
            Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
            Render.SetMaterial(ecs, slab, material);
            ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(slab, name);
            return slab;
        }

        var (across, wide) = (2f * h, 2f * h + 2f * t);
        Slab("floor", new Vec3(0f, -t / 2f, 0f), new Vec3(wide, t, wide), white);
        Slab("ceiling", new Vec3(0f, high + t / 2f, 0f), new Vec3(wide, t, wide), white);
        Slab("back", new Vec3(0f, high / 2f, -h - t / 2f), new Vec3(across, high, t), white);
        Slab("front", new Vec3(0f, high / 2f, h + t / 2f), new Vec3(across, high, t), white);
        Slab("left", new Vec3(-h - t / 2f, high / 2f, 0f), new Vec3(t, high, across), white);
        Slab("right", new Vec3(h + t / 2f, high / 2f, 0f), new Vec3(t, high, across), white);

        var glowing = 2_000f * 1.2f * 1.2f / (side * side);
        var glow = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Emissive = (glowing, glowing, glowing, 1f) });
        var panel = Slab("panel", new Vec3(0f, high - 0.03f, 0f), new Vec3(side, 0.06f, side), glow);

        Render.SetGlobalIllumination(GlobalIllumination.Low);
        return panel;
    }
}

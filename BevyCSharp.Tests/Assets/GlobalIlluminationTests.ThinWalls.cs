using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room whose walls, thinner than the field's cells, stand on a floor running on past
    /// them, a bright lamp outside it low beside one wall, reads inside under 0.005 on every face,
    /// as its reference, which no light reaches, reads nothing.
    /// </summary>
    /// <remarks>
    /// The field holds a wall thinner than a cell half a cell thick about its middle where a cell
    /// lies in front of one of its faces with the other straight behind it. The splat took a face
    /// as lying behind a cell within 25 degrees of straight back, and a wall standing on a floor
    /// has a bottom face buried against the floor's top, so along the wall's foot that face, its
    /// edge 22 degrees off straight back from the cells there, lay nearer than the far face and was
    /// kept as the face behind. Looking down and not against the face in front, it held nothing,
    /// those cells kept the distance in front, and every probe's ray along the floor passed under
    /// the wall, the room's faces reading 0.03 to 0.04, as a ceiling laid on the walls let rays
    /// over them. The walls are 0.3 thick in cells of 0.5, as the feature test's hall's walls stand
    /// in its second cascade, each face a tenth from the middles of the cells beside it, and the
    /// room lies between the probes, every 4 units, so none stands in a wall. The screen probes are
    /// left out, so the room reads the light the world's probes bring it, which the field's hold
    /// decides.
    /// </remarks>
    [SkippableFact]
    public void ALampOutsideAThinWalledRoomLightsNothingUnderItsWalls()
    {
        var faults = ThinWalledRoomFaces(screen: false).Where(f => f.Light >= 0.005).Select(f => $"the {f.Face} reads {f.Light:0.0000} where its reference reads {f.Reference:0.0000}").ToList();
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The same room with the screen probes in reads inside under 0.04 on every face, each probe's
    /// hit beside a wall hiding the lamp beyond it.
    /// </summary>
    /// <remarks>
    /// A probe's hit takes the light of the lamps that cast shadows less what the field hides of
    /// it, found by a march toward each lamp from a cell and a half off the surface, which was kept
    /// two cells clear at its start as at the lamp's end, a unit past its point in cells of 0.5, so
    /// a wall that near the hit was passed over and the lamp outside lit the faces the screen
    /// probes read inside 0.014 to 0.055. What the floor keeps comes from the screen probes' rays
    /// that start at a wall's foot and meet the wall at once, the hit's normal from the field's
    /// slope pointing out through the wall, which is read apart.
    /// </remarks>
    [SkippableFact]
    public void ALampOutsideAThinWalledRoomIsHiddenFromTheHitsBesideItsWalls()
    {
        var faults = ThinWalledRoomFaces(screen: true).Where(f => f.Light >= 0.04).Select(f => $"the {f.Face} reads {f.Light:0.0000} where its reference reads {f.Reference:0.0000}").ToList();
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The same room with its faces at a roughness of 0.5 and the screen probes left out reads
    /// inside under 0.005 on every face, the light about each face's mirror way, which crosses the
    /// east wall toward the lamp from the floor before it, kept to the probes inside the room.
    /// </summary>
    /// <remarks>
    /// A face's gloss reflects the light the probes it sees hold along its mirror way, each probe
    /// weighed by how much of the face it sees with its reach blended between its texels and a
    /// slack of half the probes' spacing (<c>probeSeesBlended</c>) and left out where it sees none,
    /// where the light about its normal takes a slack of an eighth, so this room reads whether that
    /// slack lets a probe past the wall lend the light it sees there. The floor reads 0.0012. With
    /// a white board standing outside past the lamp, which the probes beyond the wall see along the
    /// floor's mirror way, the floor read 0.0123, against 0.0135 with each probe weighed by
    /// <c>probeSees</c>, 0.0122 with no weight for sight at all and 0.0106 with no light along the
    /// mirror way, the probes beyond the wall weighed a hundredfold reading 0.0258, so those probes
    /// are among the ones read and the slack lends the reflection nothing <c>probeSees</c> keeps
    /// out. What passed 0.005 there was the light about the normal, which the board brought through
    /// the wall by the reach of the probes beyond it, and with each ray's reach its own (<see
    /// cref="ALitBoardOutsideAThinWalledRoomLightsNothingInside"/>) the floor reads 0.0046.
    /// </remarks>
    [SkippableFact]
    public void ALampOutsideAThinWalledRoomOfGlossyFacesIsReflectedNowhereInside()
    {
        var faults = ThinWalledRoomFaces(screen: false, roughness: 0.5f).Where(f => f.Light >= 0.005).Select(f => $"the {f.Face} reads {f.Light:0.0000} where its reference reads {f.Reference:0.0000}").ToList();
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The same room with a white board standing outside past the lamp, which the lamp lights and
    /// the probes beyond the east wall see, reads inside under 0.005 on every face.
    /// </summary>
    /// <remarks>
    /// A probe's reach along a texel of its octahedron was the mean of the texel's four rays, so
    /// where one of them passed beside the room's corner and met nothing, the texel read 5.8 units
    /// where the wall stood at 2.6 along its other ways, and the probe beyond the east wall, lit by
    /// the board, saw an eighth of the floor inside, which read 0.0119. With each ray's reach its
    /// own, a texel of sixteen a side a ray, the floor reads 0.0037. A ray's reach stands for a
    /// cell of ways about 15 degrees wide, so an edge narrower than that seen from a probe can still
    /// let one cell read long.
    /// </remarks>
    [SkippableFact]
    public void ALitBoardOutsideAThinWalledRoomLightsNothingInside()
    {
        var faults = ThinWalledRoomFaces(screen: false, board: true).Where(f => f.Light >= 0.005).Select(f => $"the {f.Face} reads {f.Light:0.0000} where its reference reads {f.Reference:0.0000}").ToList();
        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// Each face the camera sees inside the closed room of thin walls (<see
    /// cref="ThinWalledRoom"/>), its mean light a channel in the picture and in its reference of 64
    /// paths, the screen probes in or left out as <paramref name="screen"/> says, its faces at
    /// <paramref name="roughness"/>, a white board standing outside it where <paramref name="board"/>
    /// says so.
    /// </summary>
    private List<(string Face, double Light, double Reference)> ThinWalledRoomFaces(bool screen, float roughness = 0.9f, bool board = false)
    {
        Needs.Shaders();
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 6000,
            Scene = ecs => ThinWalledRoom(ecs, screen, roughness, board),
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 240)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginReference(ecs, null, 64));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Until("read", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var faces = new List<(string, double, double)>();
        foreach (var name in traced.Names.OrderBy(n => n.Value))
        {
            var (shown, wanted, pixels) = (0.0, 0.0, 0);
            for (var i = 0; i < traced.Regions.Length; i++)
            {
                if (traced.Regions[i] != name.Key) continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                {
                    shown += picture[i * 3 + c];
                    wanted += traced.Light[i * 3 + c];
                }
            }
            if (pixels <= 50) continue;
            var (light, reference) = (shown / pixels / 3, wanted / pixels / 3);
            output.WriteLine($"the thin-walled room's {name.Value} reads {light:0.0000} against {reference:0.0000}, the screen probes {(screen ? "in" : "out")}, its faces at {roughness}{(board ? ", the board outside" : "")}");
            faces.Add((name.Value, light, reference));
        }

        return faces;
    }

    /// <summary>
    /// A closed room four units across and three high of the level's white, its walls 0.3 thick
    /// standing on a floor that runs on past them, its ceiling laid on them, the north and south
    /// walls between the east and west walls' ends, lit by a bright lamp outside the east wall half
    /// a unit over the floor, the camera inside looking down toward the east wall, in a field of
    /// cells half a unit wide, the screen probes in or left out as <paramref name="screen"/> says,
    /// every face at <paramref name="roughness"/>, and where <paramref name="board"/> says so a white
    /// board three high and eight long standing outside past the lamp, facing the east wall.
    /// </summary>
    private static void ThinWalledRoom(EcsWorld ecs, bool screen, float roughness, bool board = false)
    {
        var camera = PictureRun.Camera(ecs, new Vec3(-0.4f, 1.6f, 2.4f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(-0.4f, 1.6f, 2.4f), new Vec3(2.8f, 0.2f, -0.2f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        var white = Render.CreateMaterial(0.787f, 0.787f, 0.787f, roughness: roughness);
        void Box(string name, float x0, float x1, float y0, float y1, float z0, float z1)
        {
            var box = ecs.Spawn();
            Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, x1 - x0, y1 - y0, z1 - z0));
            Render.SetMaterial(ecs, box, white);
            ecs.Add(box, Transform.At((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f));
            ecs.SetName(box, name);
        }

        const float Half = 0.15f, High = 3f, Low = -1f, Up = 3f;
        Box("floor", -8f, 8f, -0.6f, 0f, -8f, 8f);
        Box("east", Up - Half, Up + Half, 0f, High, Low - Half, Up + Half);
        Box("west", Low - Half, Low + Half, 0f, High, Low - Half, Up + Half);
        Box("north", Low + Half, Up - Half, 0f, High, Low - Half, Low + Half);
        Box("south", Low + Half, Up - Half, 0f, High, Up - Half, Up + Half);
        Box("ceiling", Low - Half, Up + Half, High, High + 0.6f, Low - Half, Up + Half);
        if (board) Box("board", Up + 3f, Up + 3.3f, 0f, High, Low - 2f, Up + 2f);
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 250_000f, Range = 10f, Radius = 0.05f, Shadows = true });
        ecs.Add(lamp, Transform.At(Up + 1.2f, 0.5f, 1f));
        Render.SetSceneField(4, 0.5f);
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
        Render.SetGlobalIlluminationScreenProbes(screen);
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room of the level's white <paramref name="size"/> units across and three high, lit
    /// by one shadowed lamp under its ceiling, reads the light past the direct on every face within
    /// 15% of its reference's through all the bounces.
    /// </summary>
    /// <remarks>
    /// A ray's hit takes the light it sends on from the faces of the probes around it, and a
    /// probe's face holds the light reaching a surface where the probe stands, a unit or more over
    /// the floor it is read for, nearer the walls and the ceiling than the floor is. So the faces'
    /// light is read toward the light the surface each is read for gathers, the probe's rays moved
    /// to start from where its own ray along the face's opposite way met one and each patch they
    /// met turned toward it (gi_ambient.slang), and each face past the first keeps for its diffuse
    /// share what its gloss leaves, at a probe's ray's hit and in the reference. The rooms of 4, 8
    /// and 16 read within 7% of their references, so the bar of 15% fails a face that leaves its
    /// reference by twice that. In the bounce rooms' grazing floor the layers' heights against the
    /// lamp's bright spot under its ceiling decide how near its reference the floor reads
    /// (<see cref="BounceRoomsTests"/>).
    /// </remarks>
    [SkippableTheory]
    [InlineData(4f)]
    [InlineData(8f)]
    [InlineData(16f)]
    public void AClosedRoomReadsItsLightPastTheDirectAsItsReferenceThroughEveryBounce(float size)
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, past) in PastTheDirect(ecs => ClosedRoom(ecs, size), screen: true))
        {
            output.WriteLine($"the room of {size}'s {face} reads {past:0.000} of its reference's light past the direct");
            if (Math.Abs(past - 1) >= 0.15) faults.Add($"the {face} reads {past:0.000} of its reference's light past the direct");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A closed room of 8 of smooth walls, at a roughness of 0.1 and a reflectance of 0.5, lit by one
    /// shadowed lamp under its ceiling, reads the light past the direct on every face within the
    /// bound of its reference's through all the bounces, the level's white within 20% and walls as
    /// white as 0.95 within half, the guard that their light does not run away.
    /// </summary>
    /// <remarks>
    /// A probe's ray's hit sends on its color's diffuse share keeping what its gloss leaves, beside
    /// its gloss's share toward the ray, so a smooth white face met at a grazing ray sends on no
    /// more light than reached it, and the reference's faces past the first keep the same. What is
    /// left is the hit's gloss taken as though the light reached it evenly from all around, a probe
    /// there holding its first bounce as its reference and each bounce past it over, where the
    /// reference's gloss reflects the light along its mirror way, dimmer past the first bounce at a
    /// grazing ray than the light from all around. The whiter the walls, the more that excess
    /// gathers, so the walls as white as 0.95 read up to 1.38 of their reference's light past the
    /// direct and the level's white up to 1.16, which the bounds of half and a fifth hold.
    /// </remarks>
    [SkippableTheory]
    [InlineData(0.787f, 0.2)]
    [InlineData(0.95f, 0.5)]
    public void AClosedRoomOfSmoothWhiteWallsReadsItsLightPastTheDirectAsItsReference(float white, double bound)
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, past) in PastTheDirect(ecs => ClosedRoom(ecs, 8f, 0.1f, white), screen: true))
        {
            output.WriteLine($"the smooth room of {white}'s {face} reads {past:0.000} of its reference's light past the direct");
            if (Math.Abs(past - 1) >= bound) faults.Add($"the {face} reads {past:0.000} of its reference's light past the direct");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// The bounce rooms' Cornell box closed by a white wall across its front and empty of its
    /// blocks, in their field of three cascades of 0.15, read on the world's probes alone, reads
    /// the light past the direct on its back wall, floor and side walls within 6% of its
    /// reference's.
    /// </summary>
    /// <remarks>
    /// The box is five units high and its lamp hangs 0.8 under its ceiling. A probe's face is right
    /// at its own point, so what the box reads past its reference lies in reading the faces for a
    /// surface. Moved to the surfaces by each patch's distance alone, a patch the probe saw
    /// squarely and the surface sees at a slant, as a wall's band under the ceiling, would count
    /// for as much as it did at the probe, so each patch is turned by the field's normal there, and
    /// each pixel's read of the probes is taken toward the faces at the surfaces as a ray's hit
    /// takes them. The reference's faces reflect the box by their gloss, the walls at a roughness
    /// of 0.5, and the picture reads a wall's gloss from the eight by eight directions of the
    /// probes the wall sees along the mirror way, with the probes' rays carrying the gloss of what
    /// they meet and each face past the first keeping for its diffuse share what its gloss leaves,
    /// so the box's faces read up to 4% over their reference, under the bound of 6%.
    /// </remarks>
    [SkippableFact]
    public void AClosedCornellBoxOnTheWorldsProbesReadsItsLightPastTheDirectAsItsReference()
    {
        Needs.Shaders();
        var faults = new List<string>();
        foreach (var (face, past) in PastTheDirect(ClosedCornellBox, screen: false))
        {
            output.WriteLine($"the closed Cornell box's {face} reads {past:0.000} of its reference's light past the direct");
            if (Math.Abs(past - 1) >= 0.06) faults.Add($"the {face} reads {past:0.000} of its reference's light past the direct");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// Each face the camera sees in <paramref name="scene"/>, what its light in the picture past
    /// its reference's direct light reads over its reference's light past the direct, against
    /// references of 256 paths, one of the direct light alone and one of all the bounces, the
    /// screen probes read or, where <paramref name="screen"/> is false, traced and left unread.
    /// </summary>
    private List<(string Face, double Past)> PastTheDirect(Action<EcsWorld> scene, bool screen)
    {
        var supported = false;
        var (direct, begun, read, ready) = ((GiReference.Traced?)null, false, false, 0);
        try
        {
            new PictureRun
            {
                Width = 160,
                Height = 90,
                Frames = 12000,
                Scene = ecs =>
                {
                    scene(ecs);
                    WorldProbes.ScreenUnread = !screen;
                },
            }
                .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
                .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                    && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
                .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 300)
                .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
                .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
                .Until("referenced", world =>
                {
                    if (!supported) return true;
                    var ecs = world.Resource<EcsWorld>();
                    if (!begun)
                    {
                        begun = true;
                        Assert.Null(GiReference.BeginReference(ecs, null, 256, direct is null ? 0 : -1));
                        return false;
                    }
                    if (GiReference.Step(ecs) is null) return false;
                    if (direct is not null) return true;
                    (direct, begun) = (GiReference.Last!, false);
                    return false;
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
        }
        finally
        {
            WorldProbes.ScreenUnread = false;
        }
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var faces = new List<(string, double)>();
        foreach (var name in traced.Names.OrderBy(n => n.Value))
        {
            var (shown, under, wanted, pixels) = (0.0, 0.0, 0.0, 0);
            for (var i = 0; i < traced.Regions.Length; i++)
            {
                if (traced.Regions[i] != name.Key) continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                {
                    shown += picture[i * 3 + c];
                    under += direct!.Light[i * 3 + c];
                    wanted += traced.Light[i * 3 + c];
                }
            }
            if (pixels > 40 && wanted > under) faces.Add((name.Value, (shown - under) / (wanted - under)));
        }
        return faces;
    }

    /// <summary>
    /// A closed room of the level's white <paramref name="size"/> units across and three high, its
    /// faces at <paramref name="roughness"/> as white as <paramref name="white"/>, its walls, floor
    /// and ceiling 0.4 thick, lit by a
    /// shadowed lamp half a unit under its ceiling near its middle, its light growing with the
    /// floor's area from 60,000 in a room of 4, the camera in one corner looking across to the far
    /// one.
    /// </summary>
    private static void ClosedRoom(EcsWorld ecs, float size, float roughness = 0.9f, float white = 0.787f)
    {
        var h = size / 2f;
        var camera = PictureRun.Camera(ecs, new Vec3(-h + 0.4f, 1.6f, h - 0.4f));
        ecs.Add(camera, Transform.LookingAt(new Vec3(-h + 0.4f, 1.6f, h - 0.4f), new Vec3(h, 0.8f, -h), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        var walls = Render.CreateMaterial(white, white, white, roughness: roughness);
        void Slab(string name, Vec3 at, Vec3 extent)
        {
            var slab = ecs.Spawn();
            Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, extent.X, extent.Y, extent.Z));
            Render.SetMaterial(ecs, slab, walls);
            ecs.Add(slab, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(slab, name);
        }

        const float High = 3f, T = 0.4f;
        Slab("floor", new Vec3(0f, -T / 2f, 0f), new Vec3(size + 2f * T, T, size + 2f * T));
        Slab("ceiling", new Vec3(0f, High + T / 2f, 0f), new Vec3(size + 2f * T, T, size + 2f * T));
        Slab("left", new Vec3(-h - T / 2f, High / 2f, 0f), new Vec3(T, High, size));
        Slab("right", new Vec3(h + T / 2f, High / 2f, 0f), new Vec3(T, High, size));
        Slab("front", new Vec3(0f, High / 2f, h + T / 2f), new Vec3(size, High, T));
        Slab("back", new Vec3(0f, High / 2f, -h - T / 2f), new Vec3(size, High, T));
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 60_000f * size * size / 16f, Color = (1f, 1f, 1f), Range = 2f * size, Radius = 0.05f, Shadows = true });
        ecs.Add(lamp, Transform.At(0.3f, High - 0.5f, -0.4f));
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }

    /// <summary>
    /// The bounce rooms' Cornell box (<see cref="BounceRoomsTests"/>), its walls, floor and
    /// ceiling, its red and green side walls and the emissive panel under its ceiling with the lamp
    /// below it, closed across its front by a white wall and its blocks left out, in a field of
    /// three cascades of 0.15 as the bounce rooms have it, the camera inside near its front looking
    /// at its back wall.
    /// </summary>
    private static void ClosedCornellBox(EcsWorld ecs)
    {
        static float Decode(int value)
        {
            var v = value / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
        static (float, float, float) Linear((int R, int G, int B) c) => (Decode(c.R), Decode(c.G), Decode(c.B));
        var exposure = 1f / (1.2f * MathF.Pow(2f, 9.7f));
        var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f), FieldOfView = 70f });
        ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2.5f, 2.2f), new Vec3(0f, 1.6f, -2.5f), Vec3.UnitY));
        Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Color = Linear((255, 236, 210)), Intensity = 9f * 4f * MathF.PI * MathF.PI / exposure, Range = 12f, Shadows = false });
        ecs.Add(lamp, Transform.At(0f, 4.2f, 0f));
        AssetHandle Material((int R, int G, int B) color, (int R, int G, int B)? emission = null, float intensity = 0f)
        {
            var (r, g, b) = Linear(color);
            var glow = emission is { } e ? Linear(e) : (0f, 0f, 0f);
            return Render.CreateMaterial(new MaterialSettings { BaseColor = (r, g, b, 1f), Roughness = 0.5f, Emissive = (glow.Item1 * intensity, glow.Item2 * intensity, glow.Item3 * intensity, 0f) });
        }
        void Box(string name, Vec3 middle, Vec3 size, AssetHandle material)
        {
            var box = ecs.Spawn();
            Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
            Render.SetMaterial(ecs, box, material);
            ecs.Add(box, Transform.At(middle.X, middle.Y, middle.Z));
            ecs.SetName(box, name);
        }

        var white = Material((220, 220, 215));
        Box("cornell floor", new Vec3(0f, -0.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f), white);
        Box("cornell ceiling", new Vec3(0f, 5.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f), white);
        Box("cornell back wall", new Vec3(0f, 2.5f, -2.65f), new Vec3(5.6f, 5.6f, 0.3f), white);
        Box("cornell front wall", new Vec3(0f, 2.5f, 2.65f), new Vec3(5.6f, 5.6f, 0.3f), white);
        Box("cornell red wall", new Vec3(-2.65f, 2.5f, 0f), new Vec3(0.3f, 5.6f, 5.6f), Material((200, 30, 30)));
        Box("cornell green wall", new Vec3(2.65f, 2.5f, 0f), new Vec3(0.3f, 5.6f, 5.6f), Material((30, 170, 40)));
        Box("cornell lamp panel", new Vec3(0f, 4.97f, 0f), new Vec3(1.6f, 0.06f, 1.6f), Material((255, 255, 255), (255, 240, 220), 6f));
        Render.SetSceneField(3, 0.15f, 2);
        Render.SetGlobalIllumination(GlobalIllumination.Medium);
    }
}

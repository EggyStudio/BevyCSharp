using Bevy;
using Bevy.Physics;
using Bevy.Reflected;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The light hall north of the hub, a roofed building of eight bays off an aisle, each showing one
/// way of lighting a scene.
/// </summary>
/// <remarks>
/// <para>
/// The hall is shut but for a doorway facing the road from the hub, so its bays are lit by what
/// they hold rather than by the sun, where the sun casts shadows. Down the west side are point
/// lights in red, green and blue whose shadows cross in their mixtures, a spot light shaped by a
/// cookie into a window's frame, a light the size of a panel whose shadows soften as they stretch
/// away, and a spot shining down through slats into a fog volume, its shafts drawn by the camera's
/// volumetric fog. Down the east side are a reflection probe captured once, which a chrome sphere
/// and a gold one reflect the bay in, an irradiance volume made in code, which lights a white
/// figure from each side in another color with no light in the bay, clustered decals over a wall,
/// the floor and a corner, and tubes and a sphere glowing past white for the bloom.
/// </para>
/// <para>
/// Every picture the bays use is drawn here a pixel at a time, the cookie, the decals and the
/// volume, so the hall needs nothing from a download and a run on the workflow draws it whole.
/// Each bay is named over its opening in the gizmos' stroke font. The panel's effects page turns
/// on ambient occlusion and the rest across the whole map, and the bays are where they show.
/// </para>
/// </remarks>
[Behavior]
public partial struct Lights
{
    private const float Ground = Scene.GroundHeight;

    /// <summary>How high the hall's walls stand.</summary>
    private const float Height = 5.5f;

    /// <summary>How thick its walls are.</summary>
    private const float Wall = 0.3f;

    /// <summary>The front of the hall, toward the hub, and its back.</summary>
    private const float Front = -56f, Back = -88f;

    /// <summary>The middle of each bay along the hall, from the door.</summary>
    private static readonly float[] Bays = [-60f, -68f, -76f, -84f];

    /// <summary>Whether the camera has been given volumetric fog.</summary>
    private static bool _fogged;

    /// <summary>Builds the hall and its bays.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        _fogged = false;
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var ecs = ctx.Ecs;
        Shell(ecs);

        PointLights(ecs, Bays[0]);
        Cookie(ecs, Bays[1]);
        Sized(ecs, Bays[2]);
        Fog(ecs, Bays[3]);

        Probe(ecs, Bays[0]);
        Volume(ecs, Bays[1]);
        Decals(ecs, Bays[2]);
        Glow(ecs, Bays[3]);
    }

    /// <summary>
    /// Gives the camera volumetric fog once it is made, which the fog bay's shafts are drawn by.
    /// </summary>
    /// <remarks>
    /// Bevy marches only through fog volumes, so a camera with the fog and none in view costs
    /// next to nothing. The fog's own ambient light is off, or the bay's volume would glow evenly
    /// rather than where the light crosses it.
    /// </remarks>
    [OnUpdate]
    public static void Fogged(BehaviorContext ctx)
    {
        if (_fogged || !App.HasRenderer || ctx.Res<Config>().Headless) return;
        if (Scene.Camera is not { } camera || !ctx.Ecs.IsAlive(camera)) return;

        var fog = ctx.Ecs.Insert<VolumetricFogRef>(camera);
        (fog.StepCount, fog.AmbientIntensity) = (64u, 0f);
        _fogged = true;
    }

    /// <summary>Names each bay over its opening, every frame, as gizmos do.</summary>
    [OnUpdate]
    public static void Label(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var ink = (0.95f, 0.92f, 0.8f, 1f);
        Gizmos.Text("Light hall", new Vec3(0f, Ground + 4.75f, Front + (Wall / 2f) + 0.05f), Quat.Identity, 0.4f, (0f, 0f), (0.12f, 0.08f, 0.05f, 1f), inFront: false);

        string[] west = ["Point lights in red, green and blue", "A spot light through a cookie", "A light the size of a panel", "Fog and a light through slats"];
        string[] east = ["A reflection probe", "An irradiance volume", "Clustered decals", "Emission and bloom"];
        var facingEast = Quat.FromRotationY(MathF.PI / 2f);
        var facingWest = Quat.FromRotationY(-MathF.PI / 2f);
        for (var bay = 0; bay < Bays.Length; bay++)
        {
            Gizmos.Text(west[bay], new Vec3(-4.05f, Ground + 4.9f, Bays[bay]), facingEast, 0.2f, (0f, 0f), ink, inFront: false);
            Gizmos.Text(east[bay], new Vec3(4.05f, Ground + 4.9f, Bays[bay]), facingWest, 0.2f, (0f, 0f), ink, inFront: false);
        }
    }

    /// <summary>
    /// The walls, the partitions between the bays, the roof, the floor and the aisle's lamps.
    /// </summary>
    private static void Shell(EcsWorld ecs)
    {
        var plaster = Render.CreateMaterial(0.7f, 0.68f, 0.64f, roughness: 0.9f);
        var concrete = Render.CreateMaterial(0.42f, 0.42f, 0.42f, roughness: 0.8f);
        var roof = Render.CreateMaterial(0.3f, 0.28f, 0.27f, roughness: 0.9f);

        const float Half = 12f, Door = 2f, DoorHeight = 4f;
        var middle = (Front + Back) / 2f;
        var length = Front - Back;
        var up = Ground + (Height / 2f);

        // The front wall either side of the doorway and over it.
        var side = Half - Door;
        Blocks.Static(ecs, "Hall front wall, west", new Vec3(-Door - (side / 2f), up, Front), new Vec3(side, Height, Wall), plaster);
        Blocks.Static(ecs, "Hall front wall, east", new Vec3(Door + (side / 2f), up, Front), new Vec3(side, Height, Wall), plaster);
        Blocks.Static(ecs, "Hall lintel", new Vec3(0f, Ground + DoorHeight + ((Height - DoorHeight) / 2f), Front), new Vec3(Door * 2f, Height - DoorHeight, Wall), plaster);

        Blocks.Static(ecs, "Hall back wall", new Vec3(0f, up, Back), new Vec3((Half * 2f) + Wall, Height, Wall), plaster);
        Blocks.Static(ecs, "Hall west wall", new Vec3(-Half, up, middle), new Vec3(Wall, Height, length), plaster);
        Blocks.Static(ecs, "Hall east wall", new Vec3(Half, up, middle), new Vec3(Wall, Height, length), plaster);
        Blocks.Static(ecs, "Hall roof", new Vec3(0f, Ground + Height + 0.15f, middle), new Vec3((Half * 2f) + Wall, 0.3f, length + Wall), roof);
        Blocks.Static(ecs, "Hall floor", new Vec3(0f, Ground + 0.02f, middle), new Vec3(Half * 2f, 0.04f, length), concrete);

        // Partitions between the bays, from the aisle's edge to the outer walls.
        for (var bay = 0; bay < Bays.Length - 1; bay++)
        {
            var z = (Bays[bay] + Bays[bay + 1]) / 2f;
            Blocks.Static(ecs, $"Hall partition {bay + 1}, west", new Vec3(-8f, up, z), new Vec3(8f, Height, Wall), plaster);
            Blocks.Static(ecs, $"Hall partition {bay + 1}, east", new Vec3(8f, up, z), new Vec3(8f, Height, Wall), plaster);
        }

        // Dim lamps down the aisle, enough to walk by and no more, so each bay's own light is
        // what lights it.
        foreach (var z in new[] { -60f, -72f, -84f })
        {
            var lamp = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Intensity = 60_000f,
                Color = (1f, 0.85f, 0.65f),
                Range = 9f,
                Shadows = false,
            });
            ecs.Add(lamp, Transform.At(0f, Ground + Height - 0.4f, z));
            ecs.SetName(lamp, $"Aisle lamp at {z}");
        }
    }

    /// <summary>
    /// Three point lights in red, green and blue round a column and a sphere, each casting its own
    /// shadow, so where one light is blocked the other two mix in its place.
    /// </summary>
    private static void PointLights(EcsWorld ecs, float z)
    {
        var white = Render.CreateMaterial(0.85f, 0.85f, 0.85f, roughness: 0.6f);
        Blocks.Static(ecs, "Column", new Vec3(-8.5f, Ground + 1.3f, z - 0.3f), new Vec3(0.5f, 2.6f, 0.5f), white);
        Blocks.Shape(ecs, "Sphere under three lights", Render.CreateMesh(MeshShape.Sphere, 0.5f), white, Transform.At(-6.6f, Ground + 0.5f, z + 1.4f), ColliderShape.Sphere);

        (Vec3 At, (float, float, float) Color, string Name)[] lights =
        [
            (new Vec3(-5.6f, Ground + 3.6f, z + 2.6f), (1f, 0.1f, 0.1f), "Red light"),
            (new Vec3(-10.6f, Ground + 3.6f, z + 1.6f), (0.1f, 1f, 0.1f), "Green light"),
            (new Vec3(-7.2f, Ground + 3.6f, z - 3f), (0.15f, 0.3f, 1f), "Blue light"),
        ];
        foreach (var (at, color, name) in lights)
        {
            var light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Intensity = 250_000f,
                Color = color,
                Range = 10f,
                Radius = 0.05f,
                Shadows = true,
            });
            ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(light, name);
        }
    }

    /// <summary>
    /// A spot light from the ceiling shaped by a cookie into a window of four panes, thrown on the
    /// bay's back wall round the shadow of a figure.
    /// </summary>
    private static void Cookie(EcsWorld ecs, float z)
    {
        var white = Render.CreateMaterial(0.85f, 0.85f, 0.85f, roughness: 0.6f);
        Blocks.Shape(ecs, "Figure", Render.CreateMesh(MeshShape.Capsule, 0.3f, 1.2f), white, Transform.At(-9.6f, Ground + 0.9f, z), ColliderShape.Capsule);
        Blocks.Shape(ecs, "Figure's head", Render.CreateMesh(MeshShape.Sphere, 0.25f), white, Transform.At(-9.6f, Ground + 2.05f, z), ColliderShape.Sphere);

        var spot = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Spot,
            Intensity = 900_000f,
            Color = (1f, 0.92f, 0.75f),
            Range = 16f,
            InnerAngle = 0.3f,
            OuterAngle = 0.42f,
            Shadows = true,
        });
        ecs.Add(spot, Transform.LookingAt(new Vec3(-4.6f, Ground + 4.8f, z), new Vec3(-11.8f, Ground + 1.6f, z), Vec3.UnitY));
        ecs.SetName(spot, "Spot through a cookie");
        Render.SetLightCookie(spot, WindowFrame());
    }

    /// <summary>
    /// A panel glowing under the ceiling with a light the panel's size behind it, over three thin
    /// posts and a sphere whose shadows are sharp where they stand and soft farther out.
    /// </summary>
    private static void Sized(EcsWorld ecs, float z)
    {
        var white = Render.CreateMaterial(0.85f, 0.85f, 0.85f, roughness: 0.6f);
        var glow = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Emissive = (30_000f, 29_000f, 26_000f, 1f) });
        Blocks.Static(ecs, "Panel lamp", new Vec3(-8f, Ground + Height - 0.04f, z), new Vec3(1.6f, 0.06f, 1.6f), glow);

        for (var post = 0; post < 3; post++)
            Blocks.Static(ecs, $"Post {post + 1}", new Vec3(-9.8f + (post * 0.9f), Ground + 1.2f, z + 1.8f), new Vec3(0.12f, 2.4f, 0.12f), white);

        Blocks.Shape(ecs, "Sphere under a panel", Render.CreateMesh(MeshShape.Sphere, 0.45f), white, Transform.At(-6.4f, Ground + 0.45f, z - 1.4f), ColliderShape.Sphere);

        var light = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Point,
            Intensity = 400_000f,
            Color = (1f, 0.96f, 0.9f),
            Range = 12f,
            Shadows = true,
        });
        ecs.Add(light, Transform.At(-8f, Ground + Height - 0.3f, z));
        ecs.SetName(light, "Panel light");

        // The penumbra a light eight tenths across casts, which is the panel's size.
        Render.SetSoftShadows(light, 0.8f);
    }

    /// <summary>
    /// A fog volume filling the bay and a spot light shining down into it through slats, the
    /// shafts between them drawn by the camera's volumetric fog.
    /// </summary>
    /// <remarks>
    /// A light only lights fog where it carries <see cref="VolumetricLightRef"/>, and the shafts
    /// are cut by its shadow map, so the spot casts shadows.
    /// </remarks>
    private static void Fog(EcsWorld ecs, float z)
    {
        var steel = Render.CreateMaterial(0.25f, 0.25f, 0.27f, metallic: 0.8f, roughness: 0.5f);
        for (var slat = 0; slat < 6; slat++)
            Blocks.Static(ecs, $"Slat {slat + 1}", new Vec3(-8f, Ground + 3.8f, z - 1.25f + (slat * 0.5f)), new Vec3(2.6f, 0.08f, 0.22f), steel);

        var fog = ecs.Spawn();
        ecs.Add(fog, new Transform(new Vec3(-8f, Ground + (Height / 2f), z), Quat.Identity, new Vec3(7.6f, Height, 7.6f)));
        var volume = ecs.Insert<FogVolumeRef>(fog);
        (volume.DensityFactor, volume.Scattering, volume.Absorption) = (0.12f, 0.6f, 0.05f);
        ecs.SetName(fog, "Fog volume");

        var spot = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Spot,
            Intensity = 1_200_000f,
            Color = (0.95f, 0.97f, 1f),
            Range = 12f,
            InnerAngle = 0.25f,
            OuterAngle = 0.4f,
            Shadows = true,
        });
        ecs.Add(spot, Transform.LookingAt(new Vec3(-8f, Ground + Height - 0.2f, z), new Vec3(-8.6f, Ground, z + 0.4f), Vec3.UnitX));
        ecs.Insert<VolumetricLightRef>(spot);
        ecs.SetName(spot, "Spot into the fog");
    }

    /// <summary>
    /// A reflection probe filling the bay, captured once the bay has drawn, which a chrome sphere,
    /// a chrome block and a rough gold sphere reflect the bay's colored walls in.
    /// </summary>
    /// <remarks>
    /// Without it they would reflect the camera's environment, which has nothing in it here, so
    /// the chrome would be black but for the highlights of the lamp.
    /// </remarks>
    private static void Probe(EcsWorld ecs, float z)
    {
        var orange = Render.CreateMaterial(0.9f, 0.4f, 0.1f, roughness: 0.8f);
        var teal = Render.CreateMaterial(0.1f, 0.6f, 0.6f, roughness: 0.8f);
        var yellow = Render.CreateMaterial(0.9f, 0.8f, 0.15f, roughness: 0.8f);
        var chrome = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.95f, 0.95f, 0.95f, 1f), Metallic = 1f, Roughness = 0.08f });
        var gold = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.78f, 0.34f, 1f), Metallic = 1f, Roughness = 0.35f });

        const float Paint = 0.1f;
        var up = Ground + (Height / 2f);
        Blocks.Static(ecs, "Orange wall", new Vec3(12f - (Wall / 2f) - (Paint / 2f), up, z), new Vec3(Paint, Height, 7.6f), orange);
        Blocks.Static(ecs, "Teal wall", new Vec3(8f, up, z + 4f - (Wall / 2f) - (Paint / 2f)), new Vec3(7.6f, Height, Paint), teal);
        Blocks.Static(ecs, "Yellow wall", new Vec3(8f, up, z - 4f + (Wall / 2f) + (Paint / 2f)), new Vec3(7.6f, Height, Paint), yellow);

        Blocks.Static(ecs, "Pedestal", new Vec3(8.6f, Ground + 0.4f, z), new Vec3(1f, 0.8f, 1f), Render.CreateMaterial(0.2f, 0.2f, 0.22f, roughness: 0.7f));
        Blocks.Shape(ecs, "Chrome sphere", Render.CreateMesh(MeshShape.Sphere, 0.75f), chrome, Transform.At(8.6f, Ground + 1.55f, z), ColliderShape.Sphere);
        Blocks.Shape(ecs, "Gold sphere", Render.CreateMesh(MeshShape.Sphere, 0.6f), gold, Transform.At(6.4f, Ground + 0.6f, z + 2f), ColliderShape.Sphere);
        Blocks.Static(ecs, "Chrome block", new Vec3(6.6f, Ground + 0.5f, z - 2f), new Vec3(1f, 1f, 1f), chrome, 0.5f);

        // A lamp of some size, since a point's highlight on the chrome is a few texels of the
        // capture bright enough that filtering the cube scatters them over the walls as squares.
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 300_000f, Range = 10f, Radius = 0.3f, Shadows = false });
        ecs.Add(lamp, Transform.At(7f, Ground + Height - 0.6f, z));
        ecs.SetName(lamp, "Probe bay lamp");

        var probe = ecs.Spawn();
        ecs.Add(probe, Bay(z));
        ecs.SetName(probe, "Reflection probe");
        Render.SetProbeCapture(probe, new ProbeCaptureSettings { Size = 256, Live = false });
    }

    /// <summary>
    /// An irradiance volume filling the bay, made here a texel at a time, lighting a white figure
    /// orange from the east, blue from the aisle, green from above, magenta from the door and cyan
    /// from the hall's back, with no light in the bay.
    /// </summary>
    private static void Volume(EcsWorld ecs, float z)
    {
        var white = Render.CreateMaterial(0.9f, 0.9f, 0.9f, roughness: 0.7f);
        Blocks.Static(ecs, "Plinth", new Vec3(8.2f, Ground + 0.5f, z), new Vec3(1.2f, 1f, 1.2f), white, 0.4f);
        Blocks.Shape(ecs, "Lit sphere", Render.CreateMesh(MeshShape.Sphere, 0.7f), white, Transform.At(8.2f, Ground + 1.7f, z), ColliderShape.Sphere);
        Blocks.Shape(
            ecs,
            "Lit ring",
            Render.CreateMesh(MeshShape.Torus, 0.35f, 0.75f),
            white,
            new Transform(new Vec3(6f, Ground + 0.85f, z + 1.6f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));

        var volume = ecs.Spawn();
        ecs.Add(volume, Bay(z));
        ecs.SetName(volume, "Irradiance volume");
        Render.SetIrradianceVolume(volume, Irradiance(), intensity: 800f);
    }

    /// <summary>
    /// The box of a probe filling an east bay, a unit cube scaled to it, reaching past the roof's
    /// underside so the roof is not cut by the box's top in a flicker of stripes.
    /// </summary>
    private static Transform Bay(float z) =>
        new(new Vec3(8f, Ground + ((Height + 0.5f) / 2f), z), Quat.Identity, new Vec3(8f, Height + 0.5f, 8f));

    /// <summary>
    /// Clustered decals over the bay's back wall, its floor and a corner they wrap round.
    /// </summary>
    /// <remarks>
    /// A clustered decal is a box whose picture is laid on whatever lies inside it, seen along the
    /// box's length, so one at a corner wraps the two walls and the floor as paint would.
    /// </remarks>
    private static void Decals(EcsWorld ecs, float z)
    {
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 300_000f, Range = 10f, Shadows = false });
        ecs.Add(lamp, Transform.At(7f, Ground + Height - 0.6f, z));
        ecs.SetName(lamp, "Decal bay lamp");

        var target = Target();
        var stripes = Stripes();
        Decal(ecs, "Target on the wall", target, new Vec3(10.6f, Ground + 2.6f, z), new Vec3(12.4f, Ground + 2.6f, z), 2.4f, Vec3.UnitY);
        Decal(ecs, "Stripes on the floor", stripes, new Vec3(7f, Ground + 0.8f, z + 1.5f), new Vec3(7f, Ground - 0.4f, z + 1.5f), 2.6f, Vec3.UnitX);
        Decal(ecs, "Target in the corner", target, new Vec3(10.9f, Ground + 1.1f, z - 2.9f), new Vec3(12.4f, Ground - 0.4f, z - 4.4f), 2.6f, Vec3.UnitY);
    }

    /// <summary>
    /// A decal halfway from a point to what it looks at, as long as the way between and as wide as
    /// asked, as Bevy's clustered decals example places them.
    /// </summary>
    private static void Decal(EcsWorld ecs, string name, AssetHandle image, Vec3 start, Vec3 toward, float size, Vec3 up)
    {
        var direction = toward - start;
        var center = start + (direction * 0.5f);
        var decal = ecs.Spawn();
        ecs.Add(decal, Transform.LookingAt(center, center + direction, up) with { Scale = new Vec3(size * 0.5f, size * 0.5f, direction.Length) });
        var clustered = ecs.Insert<ClusteredDecalRef>(decal);
        clustered.BaseColorTexture = image;
        ecs.SetName(decal, name);
    }

    /// <summary>
    /// Tubes and a sphere glowing well past white on a dark wall, for the bloom to scatter, with
    /// a dim light of each tube's color to put its glow on the wall.
    /// </summary>
    private static void Glow(EcsWorld ecs, float z)
    {
        var dark = Render.CreateMaterial(0.05f, 0.05f, 0.06f, roughness: 0.6f);
        Blocks.Static(ecs, "Dark wall", new Vec3(12f - (Wall / 2f) - 0.05f, Ground + (Height / 2f), z), new Vec3(0.1f, Height, 7.6f), dark);

        AssetHandle Neon(float r, float g, float b) => Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = (r, g, b, 1f),
            Emissive = (r * 25_000f, g * 25_000f, b * 25_000f, 1f),
        });

        var pink = Neon(1f, 0.1f, 0.6f);
        var cyan = Neon(0.1f, 0.9f, 1f);
        var amber = Neon(1f, 0.55f, 0.05f);
        var tube = Render.CreateMesh(MeshShape.Capsule, 0.06f, 3f);
        var wall = 11.7f;

        // A pink tube across, a cyan one standing, a cyan ring and an amber sphere.
        Blocks.Shape(ecs, "Pink tube", tube, pink, new Transform(new Vec3(wall, Ground + 3.6f, z), Quat.FromRotationX(MathF.PI / 2f), Vec3.One));
        Blocks.Shape(ecs, "Cyan tube", tube, cyan, Transform.At(wall, Ground + 2.2f, z - 2.4f));
        Blocks.Shape(
            ecs,
            "Cyan ring",
            Render.CreateMesh(MeshShape.Torus, 0.75f, 0.85f),
            cyan,
            new Transform(new Vec3(wall, Ground + 2f, z + 1.6f), Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
        Blocks.Shape(ecs, "Amber sphere", Render.CreateMesh(MeshShape.Sphere, 0.35f), amber, Transform.At(9.6f, Ground + 0.35f, z), ColliderShape.Sphere);

        foreach (var (at, color) in new[] { (new Vec3(11f, Ground + 3.6f, z), (1f, 0.1f, 0.6f)), (new Vec3(11f, Ground + 2f, z - 0.4f), (0.1f, 0.9f, 1f)) })
        {
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 30_000f, Color = color, Range = 5f, Shadows = false });
            ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
            ecs.SetName(light, "Neon glow");
        }
    }

    /// <summary>
    /// A cookie of a window of four panes, light through the panes and none through the frame,
    /// the border dark so no light leaks past it.
    /// </summary>
    private static AssetHandle WindowFrame()
    {
        const int Size = 64;
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var border = x < 5 || y < 5 || x >= Size - 5 || y >= Size - 5;
                var bar = Math.Abs(x - (Size / 2)) < 2 || Math.Abs(y - (Size / 2)) < 2;
                var level = border || bar ? (byte)0 : (byte)255;
                var at = ((y * Size) + x) * 4;
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (level, level, level, 255);
            }
        }

        return Render.CreateImage(pixels, Size, Size, srgb: false);
    }

    /// <summary>A red ring with a dot in the middle, clear elsewhere.</summary>
    private static AssetHandle Target()
    {
        const int Size = 128;
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var distance = MathF.Sqrt(MathF.Pow(x + 0.5f - (Size / 2f), 2f) + MathF.Pow(y + 0.5f - (Size / 2f), 2f)) / (Size / 2f);
                var painted = distance < 0.2f || (distance > 0.55f && distance < 0.85f);
                var at = ((y * Size) + x) * 4;
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (200, 30, 30, painted ? (byte)255 : (byte)0);
            }
        }

        return Render.CreateImage(pixels, Size, Size);
    }

    /// <summary>Diagonal yellow and black stripes inside a clear margin.</summary>
    private static AssetHandle Stripes()
    {
        const int Size = 128, Margin = 6;
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                var inside = x >= Margin && y >= Margin && x < Size - Margin && y < Size - Margin;
                var yellow = ((x + y) / 16) % 2 == 0;
                var at = ((y * Size) + x) * 4;
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = yellow
                    ? ((byte)240, (byte)200, (byte)20, inside ? (byte)255 : (byte)0)
                    : ((byte)20, (byte)20, (byte)20, inside ? (byte)255 : (byte)0);
            }
        }

        return Render.CreateImage(pixels, Size, Size);
    }

    /// <summary>
    /// An irradiance volume four points across, three high and four deep, each point keeping a
    /// color for each way a surface can face, brighter near the floor.
    /// </summary>
    /// <remarks>
    /// The image is the grid's width wide, and its height is twice the grid's height for the two
    /// signs of an axis, times three times its depth for the three axes, stacked into slices,
    /// which is the packing Bevy samples and <c>bcs_scene</c>'s <c>irradiance_texel</c> writes.
    /// A surface facing along an axis keeps the light it receives from that side.
    /// </remarks>
    private static AssetHandle Irradiance()
    {
        const int X = 4, Y = 3, Z = 4;
        (float R, float G, float B)[] sides =
        [
            (1f, 0.45f, 0.1f),   // facing east, away from the aisle
            (0.15f, 0.35f, 1f),  // facing the aisle
            (0.3f, 0.9f, 0.3f),  // facing up
            (0.15f, 0.08f, 0.08f), // facing down
            (0.9f, 0.2f, 0.7f),  // facing the door
            (0.3f, 0.9f, 0.9f),  // facing the hall's back
        ];

        var pixels = new byte[X * (2 * Y) * (3 * Z) * 4];
        for (var side = 0; side < 6; side++)
        {
            var (axis, negative) = (side / 2, side % 2);
            var (r, g, b) = sides[side];
            for (var point = 0; point < X * Y * Z; point++)
            {
                var (x, y, z) = (point % X, point / X % Y, point / (X * Y));
                var row = ((z + (axis * Z)) * 2 * Y) + y + (negative * Y);
                var at = ((row * X) + x) * 4;
                var fade = 1f - (0.2f * y);
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) =
                    ((byte)(r * fade * 255f), (byte)(g * fade * 255f), (byte)(b * fade * 255f), 255);
            }
        }

        var image = Render.CreateImage(pixels, X, 2 * Y * 3 * Z, srgb: false);
        Render.MakeVolume(image, 3 * Z);
        return image;
    }
}

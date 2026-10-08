using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The render gallery west of the hub, a wall of spheres across metallic and roughness with rows
/// for clearcoat and anisotropy, and a Cornell box.
/// </summary>
/// <remarks>
/// <para>
/// The wall faces the road from the hub, so the player arrives in front of it. Its seven columns go
/// from a dielectric to a metal and its five rows from polished to rough, as Bevy's <c>pbr</c>
/// example lays them out, with a row above of red spheres under a clearcoat from polished to rough
/// (<c>clearcoat</c>) and a row of brushed metal spheres from no anisotropy to full
/// (<c>anisotropy</c>). Each row is named in the gizmos' stroke font beside it.
/// </para>
/// <para>
/// The Cornell box stands south of the wall, open toward the road, a white room with a red wall
/// and a green one, a tall block and a short one, and an emissive panel under the ceiling with a
/// point light beneath it. It is lit by the point light's shadow map, and where the program was
/// started with ray-traced lighting (the panel's graphics page, from the next start) and the
/// bridge and the GPU have Solari, by rays from the panel instead, the walls' colors bleeding onto
/// the blocks as the light bounces, the room's meshes given to the rays as they are made.
/// </para>
/// <para>
/// Every sphere and block is a static body (<see cref="Blocks"/>), so the player walks round the
/// wall and into the box rather than through them.
/// </para>
/// </remarks>
[Behavior]
public partial struct Gallery
{
    private const float Ground = Scene.GroundHeight;

    /// <summary>Builds the wall of spheres and the Cornell box.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var ecs = ctx.Ecs;

        // Given tangents, which a primitive is made without and anisotropy reads the brushing's
        // direction from, so the brushed row is not drawn as a blaze of white.
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.45f);
        Render.GenerateTangents(sphere);

        // Seven columns from dielectric to metal along the wall, five rows from polished to rough
        // up it, the wall facing east along the road from the hub.
        for (var column = 0; column < 7; column++)
        {
            var metallic = column / 6f;
            for (var row = 0; row < 5; row++)
            {
                var roughness = 0.08f + (row / 4f * 0.92f);
                Ball(ecs, sphere, $"Sphere at metallic {metallic:0.00}, roughness {roughness:0.00}", Spot(column, row),
                    new MaterialSettings { BaseColor = (0.9f, 0.55f, 0.3f, 1f), Metallic = metallic, Roughness = roughness });
            }

            // A red clearcoated row above, its coat from polished to rough, and a brushed row on top.
            Ball(ecs, sphere, $"Clearcoat sphere {column + 1}", Spot(column, 5.3f), new MaterialSettings
            {
                BaseColor = (0.8f, 0.05f, 0.05f, 1f),
                Roughness = 0.6f,
                Clearcoat = 1f,
                ClearcoatRoughness = column / 6f,
            });
            Ball(ecs, sphere, $"Anisotropic sphere {column + 1}", Spot(column, 6.5f), new MaterialSettings
            {
                BaseColor = (0.4f, 0.4f, 0.43f, 1f),
                Metallic = 1f,
                Roughness = 0.45f,
                AnisotropyStrength = column / 6f,
                AnisotropyRotation = 0.5f,
            });
        }

        // A backing for the wall, so each sphere stands out against something matte, running on to
        // the right as the wall is faced to hold the rows' names.
        Blocks.Static(ecs, "Gallery wall", new Vec3(-64.2f, Ground + 4.2f, -2.4f), new Vec3(0.3f, 8.8f, 13.8f), Render.CreateMaterial(0.12f, 0.12f, 0.13f, roughness: 1f));

        Cornell(ecs);
    }

    /// <summary>Names the rows and columns of the wall and the box, every frame, as gizmos do.</summary>
    [OnUpdate]
    public static void Label(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var facing = Quat.FromRotationY(MathF.PI / 2f);
        var ink = (0.95f, 0.92f, 0.8f, 1f);
        // The rows are named on the wall's right as it is faced, toward the Cornell box, where the
        // names have room to run without leaving the picture taken from the zone's eye.
        Gizmos.Text("metallic, from none to whole", new Vec3(-63.9f, Ground + 0.25f, 0f), facing, 0.2f, (0f, 0f), ink, inFront: false);
        Gizmos.Text("roughness, up", new Vec3(-63.9f, Spot(0, 2f).Y, -4.7f), facing, 0.22f, (-0.5f, 0f), ink, inFront: false);
        Gizmos.Text("clearcoat, polished to rough", new Vec3(-63.9f, Spot(0, 5.3f).Y, -4.7f), facing, 0.2f, (-0.5f, 0f), ink, inFront: false);
        Gizmos.Text("anisotropy, none to whole", new Vec3(-63.9f, Spot(0, 6.5f).Y, -4.7f), facing, 0.2f, (-0.5f, 0f), ink, inFront: false);
        Gizmos.Text(
            Render.RayTracingActive ? "Cornell box, lit by rays" : Lights.CastShadows ? "Cornell box, lit by a shadow map" : "Cornell box, lit with no shadows while meshlets run",
            new Vec3(-67.9f, Ground + 5.6f, -13f), facing, 0.25f, (0f, -0.5f), ink, inFront: false);
    }

    /// <summary>
    /// Where a sphere of the wall goes, by its column across and its row up, the first column on
    /// the left of a view facing the wall, which looks west, its left to the south.
    /// </summary>
    private static Vec3 Spot(int column, float row) => new(-63.5f, Ground + 1.05f + (row * 1.05f), (3 - column) * 1.15f);

    /// <summary>A white room open toward the road, a red wall and a green one, two blocks and a lamp.</summary>
    private static void Cornell(EcsWorld ecs)
    {
        const float Size = 5f;
        // A little over the ground, so the box's floor and the ground do not fight for the same
        // depth.
        var middle = new Vec3(-70.5f, Ground + 0.02f, -13f);
        var white = Render.CreateMaterial(0.73f, 0.73f, 0.73f, roughness: 0.9f);
        var red = Render.CreateMaterial(0.65f, 0.05f, 0.05f, roughness: 0.9f);
        var green = Render.CreateMaterial(0.12f, 0.45f, 0.15f, roughness: 0.9f);
        var glow = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Emissive = (40_000f, 38_000f, 34_000f, 1f) });

        const float Wall = 0.2f;
        var room = new List<Entity>
        {
            Blocks.Static(ecs, "Cornell floor", middle + new Vec3(0f, -Wall / 2f, 0f), new Vec3(Size, Wall, Size), white),
            Blocks.Static(ecs, "Cornell ceiling", middle + new Vec3(0f, Size + (Wall / 2f), 0f), new Vec3(Size, Wall, Size), white),
            Blocks.Static(ecs, "Cornell back wall", middle + new Vec3(-(Size / 2f) - (Wall / 2f), Size / 2f, 0f), new Vec3(Wall, Size, Size), white),
            Blocks.Static(ecs, "Cornell red wall", middle + new Vec3(0f, Size / 2f, -(Size / 2f) - (Wall / 2f)), new Vec3(Size, Size, Wall), red),
            Blocks.Static(ecs, "Cornell green wall", middle + new Vec3(0f, Size / 2f, (Size / 2f) + (Wall / 2f)), new Vec3(Size, Size, Wall), green),
            Blocks.Static(ecs, "Cornell tall block", middle + new Vec3(-0.9f, 1.5f, -0.8f), new Vec3(1.4f, 3f, 1.4f), white, 0.35f),
            Blocks.Static(ecs, "Cornell short block", middle + new Vec3(0.8f, 0.7f, 0.9f), new Vec3(1.4f, 1.4f, 1.4f), white, -0.3f),
            Blocks.Static(ecs, "Cornell lamp", middle + new Vec3(0f, Size - 0.05f, 0f), new Vec3(1.2f, 0.05f, 1.2f), glow),
        };

        var lamp = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Point,
            Intensity = 120_000f,
            Range = 12f,
            Radius = 0.3f,
            Shadows = !Render.RayTracingActive && Lights.CastShadows,
        });
        ecs.Add(lamp, Transform.At(middle.X, middle.Y + Size - 0.4f, middle.Z));
        ecs.SetName(lamp, "Cornell light");

        // Where Solari runs, its rays meet the room's meshes, and the panel lights the room.
        if (!Render.RayTracingActive) return;

        foreach (var piece in room) Render.SetRayTraced(piece, Render.MeshOf(ecs, piece));
    }

    private static void Ball(EcsWorld ecs, AssetHandle mesh, string name, Vec3 at, MaterialSettings material) =>
        Blocks.Shape(ecs, name, mesh, Render.CreateMaterial(material), Transform.At(at.X, at.Y, at.Z), ColliderShape.Sphere);
}

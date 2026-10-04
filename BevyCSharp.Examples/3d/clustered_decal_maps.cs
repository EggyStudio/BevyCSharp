using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates clustered decals with normal, metallic-roughness and emissive maps: a Bevy logo is
// stamped on a scratched wall each second, grows in, stays ten seconds and shrinks away, with its
// gold outline drawn, and the buttons turn its glow on and off.
internal static class ClusteredDecalMaps
{
    private const float PlaneHalfSize = 2f;
    private const float DecalMinSize = 0.5f, DecalMaxSize = 1.5f;
    private const float AnimateIn = 0.3f, Idle = 10f, AnimateOut = 0.3f;

    // A decal, how large it grows, and when it was stamped.
    private sealed record Decal(Entity Entity, float Size, float Born);

    private static readonly List<Decal> Decals = [];
    private static AssetHandle _baseColor, _normal, _metallicRoughness, _emissive;
    private static bool _emissiveDecals;
    private static float _nextSpawn;
    private static Random _random = new();
    private static RadioButtons<bool>? _buttons;

    public static void Build(App app)
    {
        Decals.Clear();
        (_emissiveDecals, _nextSpawn) = (false, 1f);
        _random = new Random(19878367);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var linear = new TextureSettings { Srgb = false };
            _baseColor = AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png");
            _normal = AssetServer.LoadImage("clustered_decal_maps/BevyLogo-Normal.png", linear);
            _metallicRoughness = AssetServer.LoadImage("clustered_decal_maps/BevyLogo-MetallicRoughness.png", linear);
            _emissive = AssetServer.Load(AssetKind.Image, "clustered_decal_maps/BevyLogo-Emissive.png");

            // A crimson wall facing the camera, scratched by its normal map.
            var crimson = Scene.Srgb8(220, 20, 60);
            ecs.Mesh(
                Render.CreateMesh(MeshShape.Plane, PlaneHalfSize * 2f, PlaneHalfSize * 2f),
                Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = crimson,
                    NormalMap = AssetServer.LoadImage("textures/ScratchedGold-Normal.png", linear),
                }),
                new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

            ecs.PointLight(new Vec3(8f, 16f, -8f), intensity: 10_000_000f, range: 100f);

            var camera = ecs.Camera(Transform.LookingAt(new Vec3(2f, 0f, -7f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true });

            _buttons = new RadioButtons<bool>(ecs, RadioButtons<bool>.Column(), "Emissive Decals", [(true, "On"), (false, "Off")], _emissiveDecals);
        }, "clustered_decal_maps.Setup");

        app.Update(SpawnDecal, "clustered_decal_maps.SpawnDecal");
        app.Update(AnimateDecals, "clustered_decal_maps.AnimateDecals");
        app.Update(ctx =>
        {
            if (_buttons is null || !_buttons.Pressed(out var on) || on == _emissiveDecals) return;
            _emissiveDecals = on;
            _buttons.Select(ctx.Ecs, on);
        }, "clustered_decal_maps.HandleEmissionTypeChange");
    }

    // A decal each second somewhere on the wall, turned a random way and given a random size.
    private static void SpawnDecal(BehaviorContext ctx)
    {
        var now = ctx.Time.Elapsed;
        if (now < _nextSpawn) return;
        _nextSpawn += 1f;

        var ecs = ctx.Ecs;
        var at = new Vec3(Between(-PlaneHalfSize, PlaneHalfSize), Between(-PlaneHalfSize, PlaneHalfSize), 0f);
        var size = Between(DecalMinSize, DecalMaxSize);
        var theta = Between(0f, MathF.PI);

        var entity = ecs.Spawn();
        var look = Transform.LookingAt(at, at + Vec3.UnitZ, new Vec3(MathF.Cos(theta), MathF.Sin(theta), 0f));
        ecs.Add(entity, look with { Scale = Vec3.Zero });

        var decal = ecs.Insert<ClusteredDecalRef>(entity);
        decal.BaseColorTexture = _baseColor;
        decal.NormalMapTexture = _normal;
        decal.MetallicRoughnessTexture = _metallicRoughness;
        decal.EmissiveTexture = _emissiveDecals ? _emissive : null;
        Decals.Add(new Decal(entity, size, now));
    }

    // Each decal grows in, stays, and shrinks away, and its outline is drawn as it stands.
    private static void AnimateDecals(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var now = ctx.Time.Elapsed;
        var gold = Scene.Srgb(1f, 215f / 255f, 0f);

        foreach (var decal in Decals.ToArray())
        {
            var age = now - decal.Born;
            if (age >= AnimateIn + Idle + AnimateOut)
            {
                ecs.Despawn(decal.Entity);
                Decals.Remove(decal);
                continue;
            }

            var factor = age < AnimateIn ? age / AnimateIn
                : age < AnimateIn + Idle ? 1f
                : 1f - (age - AnimateIn - Idle) / AnimateOut;
            var transform = ecs.GetOrDefault<Transform>(decal.Entity);
            var scale = new Vec3(decal.Size * factor, decal.Size * factor, 1f);
            ecs.Set(decal.Entity, transform with { Scale = scale });
            Gizmos.Box(transform.Translation, transform.Rotation, scale, gold);
        }
    }

    private static float Between(float low, float high) => low + _random.NextSingle() * (high - low);
}

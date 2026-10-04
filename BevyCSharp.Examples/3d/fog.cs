using System.Globalization;
using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Distance-based fog visual effects are used in many games to give a soft falloff of visibility to
// the player for performance and/or visual design reasons. This example shows linear, exponential
// and squared exponential fog, Bevy's DistanceFog put on the camera through reflection.
internal static class Fog
{
    private const string DistanceFog = "bevy_pbr::fog::DistanceFog";

    private enum Falloff { Linear, Exponential, ExponentialSquared }

    private static Entity _camera, _text;
    private static Falloff _falloff = Falloff.Linear;
    private static float _start = 5f, _end = 20f, _density = 0.07f;
    private static Vec4 _color;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_falloff, _start, _end, _density) = (Falloff.Linear, 5f, 20f, 0.07f);
            _color = new Vec4(0.25f, 0.25f, 0.25f, 1f);

            _camera = ecs.Camera(Transform.Identity);
            ecs.InsertReflected(_camera, DistanceFog);
            Apply(ecs);

            // A pyramid of stone steps, four pillars on its top and a green glass ball over them.
            var stone = Color.FromHex("28221B");
            var stoneMaterial = Render.CreateMaterial(new MaterialSettings { BaseColor = (stone.R, stone.G, stone.B, 1f), Roughness = 1f });
            foreach (var (x, z) in new[] { (-1.5f, -1.5f), (1.5f, -1.5f), (1.5f, 1.5f), (-1.5f, 1.5f) })
                ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 3f, 1f), stoneMaterial, Transform.At(x, 1.5f, z));

            var glass = Color.FromHex("126212CC");
            var ball = ecs.Mesh(
                Render.CreateMesh(MeshShape.Sphere, 0.5f),
                Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (glass.R, glass.G, glass.B, glass.A),
                    Reflectance = 1f,
                    Roughness = 0f,
                    Metallic = 0.5f,
                    AlphaMode = AlphaMode.Blend,
                }),
                new Transform(new Vec3(0f, 4f, 0f), Quat.Identity, new Vec3(1.75f)));
            Render.SetMeshFlags(ecs, ball, MeshFlags.NoShadowCasting | MeshFlags.NoShadowReceiving);

            for (var i = 0; i < 50; i++)
            {
                var half = i / 2f + 3f;
                ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 2f * half, 0.5f, 2f * half), stoneMaterial, Transform.At(0f, -i / 2f + 0.25f, 0f));
            }

            // A box around everything, which the fog colors.
            var sky = Color.FromHex("888888");
            ecs.Mesh(
                Render.CreateMesh(MeshShape.Cuboid, 2f, 1f, 1f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = (sky.R, sky.G, sky.B, 1f), Unlit = true, DoubleSided = true }),
                new Transform(Vec3.Zero, Quat.Identity, new Vec3(1_000_000f)));

            ecs.PointLight(new Vec3(0f, 1f, 0f), shadows: true);

            _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(Update, "fog.UpdateSystem");
    }

    private static void Update(BehaviorContext ctx)
    {
        var now = ctx.Time.Elapsed;
        var delta = ctx.Time.Delta;
        var input = ctx.Input;

        // The camera circles the pyramid, nearer and farther as it goes.
        var orbit = 8f + MathF.Sin(now / 10f) * 7f;
        ctx.Ecs.Set(_camera, Transform.LookingAt(new Vec3(MathF.Cos(now / 5f) * orbit, 12f - orbit / 2f, MathF.Sin(now / 5f) * orbit), Vec3.Zero, Vec3.UnitY));

        if (input.KeyDown(Key.Digit1)) _falloff = Falloff.Linear;
        if (input.KeyDown(Key.Digit2)) _falloff = Falloff.Exponential;
        if (input.KeyDown(Key.Digit3)) _falloff = Falloff.ExponentialSquared;

        if (_falloff == Falloff.Linear)
        {
            if (input.KeyDown(Key.A)) _start -= delta * 3f;
            if (input.KeyDown(Key.S)) _start += delta * 3f;
            if (input.KeyDown(Key.Z)) _end -= delta * 3f;
            if (input.KeyDown(Key.X)) _end += delta * 3f;
        }
        else
        {
            if (input.KeyDown(Key.A)) _density = MathF.Max(0f, _density - delta * 0.5f * _density);
            if (input.KeyDown(Key.S)) _density += delta * 0.5f * _density;
        }

        static float Step(float value, bool down, bool up, float delta) =>
            Math.Clamp(value + (up ? 0.1f * delta : 0f) - (down ? 0.1f * delta : 0f), 0f, 1f);

        _color = new Vec4(
            Step(_color.X, input.KeyDown(Key.Minus), input.KeyDown(Key.Equal), delta),
            Step(_color.Y, input.KeyDown(Key.BracketLeft), input.KeyDown(Key.BracketRight), delta),
            Step(_color.Z, input.KeyDown(Key.Semicolon), input.KeyDown(Key.Quote), delta),
            Step(_color.W, input.KeyDown(Key.Period), input.KeyDown(Key.Slash), delta));

        Apply(ctx.Ecs);

        var text = _falloff switch
        {
            Falloff.Linear => FormattableString.Invariant($"Fog Falloff: Linear {{ start: {_start:0.0}, end: {_end:0.0} }}"),
            Falloff.Exponential => FormattableString.Invariant($"Fog Falloff: Exponential {{ density: {_density:0.000} }}"),
            _ => FormattableString.Invariant($"Fog Falloff: ExponentialSquared {{ density: {_density:0.000} }}"),
        };
        text += FormattableString.Invariant($"\nFog Color: srgba({_color.X:0.00}, {_color.Y:0.00}, {_color.Z:0.00}, {_color.W:0.00})")
            + "\n\n1 / 2 / 3 - Fog Falloff Mode"
            + (_falloff == Falloff.Linear ? "\nA / S - Move Start Distance\nZ / X - Move End Distance" : "\nA / S - Change Density")
            + "\n\n- / = - Red\n[ / ] - Green\n; / ' - Blue\n. / ? - Alpha";
        Ui.SetText(_text, text);
    }

    // The fog as it stands, written to the camera's component.
    private static void Apply(EcsWorld ecs)
    {
        ecs.SetReflectedColor(_camera, DistanceFog, ".color", Color.FromSrgb(_color.X, _color.Y, _color.Z, _color.W));
        ecs.SetVariant(_camera, DistanceFog, ".falloff", _falloff.ToString());

        if (_falloff == Falloff.Linear)
        {
            ecs.SetReflected(_camera, DistanceFog, ".falloff.start", _start.ToString(CultureInfo.InvariantCulture));
            ecs.SetReflected(_camera, DistanceFog, ".falloff.end", _end.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            ecs.SetReflected(_camera, DistanceFog, ".falloff.density", _density.ToString(CultureInfo.InvariantCulture));
        }
    }
}

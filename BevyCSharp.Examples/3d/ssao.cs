// Bevy's ssao example, examples/3d/ssao.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// A scene showcasing screen space ambient occlusion.
internal static class Ssao
{
    private static Entity _camera, _text;
    // Bevy's default occlusion, its thickness and its radius.
    private const float DefaultThickness = 0.25f, DefaultRadius = 0.5f * 1.457f;

    private static AmbientOcclusionQuality? _quality;
    private static float _thickness, _radius;
    private static bool _temporal;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetAmbientLight((1f, 1f, 1f), 1000f);
            (_quality, _thickness, _radius, _temporal) = (AmbientOcclusionQuality.High, DefaultThickness, DefaultRadius, true);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2f, -2f), Vec3.Zero, Vec3.UnitY));
            Apply(ecs);

            var gray = Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.5f, 0.5f, 0.5f), Roughness = 1f, Reflectance = 0f });
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            ecs.SpawnMesh(cube, gray, Transform.At(0f, 0f, 1f));
            ecs.SpawnMesh(cube, gray, Transform.At(0f, -1f, 0f));
            ecs.SpawnMesh(cube, gray, Transform.At(1f, 0f, 0f));

            var sphere = ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.Sphere, 0.4f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.4f, 0.4f, 0.4f), Roughness = 1f, Reflectance = 0f }),
                Transform.Identity);
            ecs.Add(sphere, new SphereMarker());

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));

            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            var input = ctx.Input;

            // An arrow pressed with no occlusion on puts Bevy's default on and changes that, as
            // Bevy's update starts from the default where the camera has none.
            if (_quality is null && (input.KeyPressed(Key.ArrowUp) || input.KeyPressed(Key.ArrowDown) || input.KeyPressed(Key.ArrowLeft) || input.KeyPressed(Key.ArrowRight)))
                (_quality, _thickness, _radius) = (AmbientOcclusionQuality.High, DefaultThickness, DefaultRadius);

            var changed = true;
            if (input.KeyPressed(Key.Digit1)) _quality = null;
            else if (input.KeyPressed(Key.Digit2)) _quality = AmbientOcclusionQuality.Low;
            else if (input.KeyPressed(Key.Digit3)) _quality = AmbientOcclusionQuality.Medium;
            else if (input.KeyPressed(Key.Digit4)) _quality = AmbientOcclusionQuality.High;
            else if (input.KeyPressed(Key.Digit5)) _quality = AmbientOcclusionQuality.Ultra;
            else if (input.KeyPressed(Key.ArrowUp)) _thickness = MathF.Min(_thickness * 2f, 4f);
            else if (input.KeyPressed(Key.ArrowDown)) _thickness = MathF.Max(_thickness * 0.5f, 0.0625f);
            else if (input.KeyPressed(Key.ArrowRight)) _radius = MathF.Min(_radius * 1.25f, 8f);
            else if (input.KeyPressed(Key.ArrowLeft)) _radius = MathF.Max(_radius * 0.8f, 0.1f);
            else if (input.KeyPressed(Key.Space)) _temporal = !_temporal;
            else changed = false;

            if (!changed) return;
            Apply(ctx.Ecs);
            Ui.SetText(_text, Text());
        }, "ssao.Update");
    }

    // High quality with temporal antialiasing, as Bevy's default occlusion and its example camera.
    private static void Apply(EcsWorld ecs)
    {
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true, Msaa = 1, AntiAlias = _temporal ? AntiAliasPass.Temporal : AntiAliasPass.None });
        Render.SetAmbientOcclusion(_camera, _quality, _thickness);
        if (ecs.Get<ScreenSpaceAmbientOcclusionRef>(_camera) is { } occlusion) occlusion.Radius = _radius;
    }

    private static string Text()
    {
        string Mark(AmbientOcclusionQuality? quality) => _quality == quality ? "*" : "";
        var text = _quality is null ? "" : FormattableString.Invariant($"Radius: {_radius} (Left/Right)\nConstant object thickness: {_thickness} (Up/Down)\n\n");
        return text
            + "SSAO Quality:\n"
            + $"(1) {Mark(null)}Off{Mark(null)}\n"
            + $"(2) {Mark(AmbientOcclusionQuality.Low)}Low{Mark(AmbientOcclusionQuality.Low)}\n"
            + $"(3) {Mark(AmbientOcclusionQuality.Medium)}Medium{Mark(AmbientOcclusionQuality.Medium)}\n"
            + $"(4) {Mark(AmbientOcclusionQuality.High)}High{Mark(AmbientOcclusionQuality.High)}\n"
            + $"(5) {Mark(AmbientOcclusionQuality.Ultra)}Ultra{Mark(AmbientOcclusionQuality.Ultra)}\n\n"
            + "Temporal Antialiasing:\n"
            + (_temporal ? "(Space) Enabled" : "(Space) Disabled");
    }
}

/// <summary>The sphere that bobs among the cubes, to show the occlusion change as it moves.</summary>
[Behavior]
public partial struct SphereMarker
{
    /// <summary>Raised and lowered by the sine of the time, as Bevy's <c>update</c> moves it.</summary>
    [OnUpdate]
    public void Bob(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation.Y = MathF.Sin(ctx.Time.Elapsed / 1.7f) * 0.7f;
}

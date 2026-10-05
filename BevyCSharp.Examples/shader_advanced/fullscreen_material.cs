// Bevy's fullscreen_material example, examples/shader_advanced/fullscreen_material.rs at v0.19.1,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// A full screen material, a chromatic aberration whose strength rises and falls, over a cube. T
// takes the effect off the camera and puts it back. Bevy's FullscreenMaterial is a component of
// the camera holding the material's values, and here the shader is a program with a pass stage the
// camera runs.
internal static class FullscreenMaterial
{
    private const float Frequency = 2f, MaxIntensity = 0.015f;

    private static Entity _camera, _text;
    private static ShaderInstance _effect;
    private static bool _on;
    private static float _intensity, _lastIntensity, _phaseOffset;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_on, _intensity, _lastIntensity, _phaseOffset) = (true, 0f, 0f, 0f);
            _camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
            _effect = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/fullscreen_effect.slang" }));
            Shaders.SetPasses(_camera, new ShaderPass(_effect, AfterTonemapping: true));

            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Scene.Material(Scene.Srgb(0.8f, 0.7f, 0.6f)), Transform.Identity);
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1_000f, Shadows = false });
            ecs.Add(sun, Transform.Identity);

            _text = Ui.SpawnText("(T) FullscreenEffect: On", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "fullscreen_material.Setup");

        // A sine between nothing and the most, picking up where it was whenever the effect comes
        // back at nothing, as Bevy's shifts its phase to meet the value it was given.
        app.Update(ctx =>
        {
            if (!_on) return;
            var t = ctx.Time.Elapsed;
            if (_intensity != _lastIntensity) _phaseOffset = MathF.Asin(_intensity / MaxIntensity * 2f - 1f) - t * Frequency;
            var intensity = (MathF.Sin(t * Frequency + _phaseOffset) + 1f) / 2f;
            _lastIntensity = _intensity = intensity * MaxIntensity;
            _effect.Set("settings.intensity", _intensity);
        }, "fullscreen_material.UpdateIntensity");

        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.T)) return;
            _on = !_on;
            if (_on)
            {
                _intensity = 0f;
                Shaders.SetPasses(_camera, new ShaderPass(_effect, AfterTonemapping: true));
            }
            else
            {
                Shaders.SetPasses(_camera);
            }

            Ui.SetText(_text, _on ? "(T) FullscreenEffect: On" : "(T) FullscreenEffect: Off");
        }, "fullscreen_material.ToggleEffect");
    }
}

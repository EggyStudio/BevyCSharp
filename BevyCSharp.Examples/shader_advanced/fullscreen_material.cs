// Bevy's fullscreen_material example, examples/shader_advanced/fullscreen_material.rs at v0.20.0,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// A full screen material, a chromatic aberration whose strength rises and falls, over a cube. T
// takes the effect off the camera and puts it back. Bevy's FullscreenMaterial is a component of
// the camera holding the material's values, and here the shader is a program with a pass stage the
// camera runs.
internal static class FullscreenMaterial
{
    internal const float Frequency = 2f, MaxIntensity = 0.015f;

    // The pass's values on the GPU, which the camera's effect is copied into each frame.
    internal static ShaderInstance Effect;

    // Bevy's Locals of update_intensity, the intensity it last gave and the phase it shifted to.
    internal static float LastIntensity, PhaseOffset;

    private static Entity _camera, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (LastIntensity, PhaseOffset) = (0f, 0f);
            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
            Effect = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/fullscreen_effect.slang" }));
            Shaders.SetPasses(_camera, new ShaderPass(Effect, AfterTonemapping: true));
            ecs.Add(_camera, new FullscreenEffect());

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.Identity);
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1_000f, Shadows = false });
            ecs.Add(sun, Transform.Identity);

            _text = Ui.SpawnText("(T) FullscreenEffect: On", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "fullscreen_material.Setup");

        app.Update(ToggleEffect, "fullscreen_material.ToggleEffect");
    }

    // T takes the effect off the camera, and its pass with it, or puts both back at nothing, as
    // Bevy's removes the component and inserts it again.
    private static void ToggleEffect(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.T)) return;

        var ecs = ctx.Ecs;
        if (ecs.Has<FullscreenEffect>(_camera))
        {
            ecs.Remove<FullscreenEffect>(_camera);
            Shaders.SetPasses(_camera);
            Ui.SetText(_text, "(T) FullscreenEffect: Off");
        }
        else
        {
            ecs.Add(_camera, new FullscreenEffect());
            Shaders.SetPasses(_camera, new ShaderPass(Effect, AfterTonemapping: true));
            Ui.SetText(_text, "(T) FullscreenEffect: On");
        }
    }
}

/// <summary>A camera's full screen chromatic aberration, how far apart its pass pulls the colors.</summary>
[Behavior]
public partial struct FullscreenEffect
{
    /// <summary>How strong it is.</summary>
    public float Intensity;

    /// <summary>
    /// A sine between nothing and the most, picking up where it was whenever the effect comes back
    /// at nothing, as Bevy's shifts its phase to meet the value it was given.
    /// </summary>
    [OnUpdate]
    public void UpdateIntensity(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed;
        if (Intensity != FullscreenMaterial.LastIntensity)
            FullscreenMaterial.PhaseOffset = MathF.Asin(Intensity / FullscreenMaterial.MaxIntensity * 2f - 1f) - t * FullscreenMaterial.Frequency;

        var intensity = (MathF.Sin(t * FullscreenMaterial.Frequency + FullscreenMaterial.PhaseOffset) + 1f) / 2f;
        FullscreenMaterial.LastIntensity = Intensity = intensity * FullscreenMaterial.MaxIntensity;
    }

    /// <summary>
    /// Copied into the pass's values once everything else has set it, as Bevy extracts the
    /// component into its render world each frame.
    /// </summary>
    [OnPostUpdate]
    public void Extract(BehaviorContext ctx) => FullscreenMaterial.Effect.Set("settings.intensity", Intensity);
}

using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Screen-space global illumination on F5: light bounced off what is on screen onto what is near
/// it, so the lamp-lit cube warms the ground around it and a colored surface tints its neighbors.
/// </summary>
/// <remarks>
/// <para>
/// A small, plain version of the technique, written to show what a package of this kind is built
/// from rather than to compete with one. Three shaders in <c>assets/shaders</c> do the work: one
/// ray a pixel at half size, marched through the depth buffer, taking the light it meets from last
/// frame's lit picture (<c>gi_trace.slang</c>); the rays of many frames averaged, found through
/// the motion vectors (<c>gi_accumulate.slang</c>); and the result added to the picture times the
/// surface's color, before glass and tonemapping (<c>gi_composite.slang</c>).
/// </para>
/// <para>
/// All of it is the camera's: images it owns and keeps from frame to frame, compute it runs after
/// opaque geometry, a draw it makes, and the G-buffer, depth, motion, blue noise and environment
/// that shaders on a camera read. Each file reloads when saved, like every shader in the sample.
/// </para>
/// </remarks>
[Behavior]
public partial struct ScreenSpaceLight
{
    private static ShaderInstance _trace;
    private static ShaderInstance _accumulate;
    private static ShaderInstance _composite;
    private static Entity _camera;
    private static bool _on;

    /// <summary>Makes the three programs.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        _trace = Shaders.CreateInstance(Shaders.CreateProgram(
                new ShaderProgramSettings { Compute = "shaders/gi_trace.slang" }))
            .Set("steps", 16u)
            .Set("reach", 3f)
            .Set("thickness", 0.4f);

        _accumulate = Shaders.CreateInstance(Shaders.CreateProgram(
                new ShaderProgramSettings { Compute = "shaders/gi_accumulate.slang" }))
            .Set("blend", 0.08f);

        _composite = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
            {
                DrawVertex = "shaders/gi_composite.slang",
                DrawFragment = "shaders/gi_composite.slang",
            }))
            .Set("strength", 1f);

        Console.WriteLine("[ScreenSpaceLight] F5 toggles screen-space global illumination");
    }

    /// <summary>Puts it on and off.</summary>
    [OnUpdate]
    public static void Toggle(BehaviorContext ctx)
    {
        if (!_trace.IsValid || !ctx.Input.KeyPressed(Key.F5) || Camera(ctx.Ecs) is not { } camera) return;

        _on = !_on;
        Apply(camera, _on);
        Console.WriteLine($"[ScreenSpaceLight] {(_on ? "on" : "off")}");
    }

    /// <summary>Sets the camera up to run the three steps, or takes them off it.</summary>
    [Command("sample.gi", "Screen-space global illumination on the sample's camera: sample.gi <on|off>")]
    internal static string Command(string state)
    {
        if (!_trace.IsValid || Camera(ConsoleHost.Ecs) is not { } camera) return "there is no camera to light";

        _on = state.Trim() is "on" or "1" or "true";
        Apply(camera, _on);
        return $"screen-space global illumination is {(_on ? "on" : "off")}";
    }

    /// <summary>
    /// The sample's camera, found the first time it is asked for, since the scene that spawns it
    /// may start after this does.
    /// </summary>
    private static Entity? Camera(EcsWorld ecs)
    {
        if (_camera == Entity.None)
        {
            foreach (var row in ecs.Query<FlyCamera>(markChanged: false))
            {
                _camera = row.Entity;
                break;
            }
        }

        return _camera == Entity.None ? null : _camera;
    }

    private static void Apply(Entity camera, bool on)
    {
        if (!on)
        {
            Shaders.SetViewDraws(camera);
            Shaders.SetViewDispatches(camera);
            Shaders.SetViewImages(camera);
            return;
        }

        // Depth to march through, motion to find last frame's light by, and the G-buffer for each
        // surface's normal and color.
        Shaders.SetPrepass(camera, depth: true, motion: true, deferred: true);

        Shaders.SetViewImages(
            camera,
            // Last frame's lit picture, before tonemapping, which is the light a ray picks up.
            new ViewImage("lit", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true, CopyAt: FramePoint.BeforeTonemapping),
            new ViewImage("gathered", ShaderImageFormat.Rgba16Float, Scale: 0.5f),
            new ViewImage("gi", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true));

        Shaders.SetViewDispatches(
            camera,
            ViewDispatch.PerPixel(_trace, FramePoint.AfterOpaque, scale: 0.5f),
            ViewDispatch.PerPixel(_accumulate, FramePoint.AfterOpaque, scale: 0.5f));

        Shaders.SetViewDraws(
            camera,
            ViewDraw.Fixed(_composite, FramePoint.AfterOpaque, 3, blend: DrawBlend.Add, writesDepth: false));
    }
}

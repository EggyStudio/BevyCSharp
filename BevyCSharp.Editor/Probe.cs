using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor;

/// <summary>TEMPORARY: flies the camera up so a capture shows the grid changing spacing.</summary>
[Behavior]
public partial struct Probe
{
    /// <summary>Runs whatever BCS_PROBE names.</summary>
    [OnUpdate]
    public static void Run(BehaviorContext ctx)
    {
        if (Environment.GetEnvironmentVariable("BCS_PROBE") is not { Length: > 0 } script) return;
        if (ctx.Time.FrameCount != 100) return;
        if (!float.TryParse(script, out var height)) return;

        var camera = EditorSelection.Camera;
        var transform = ctx.Ecs.GetOrDefault<Transform>(camera);

        transform.Translation = new Vec3(0f, height, height * 0.9f);
        transform.Rotation = Quat.FromRotationX(-0.7f);

        ctx.Ecs.Set(camera, transform);
    }
}

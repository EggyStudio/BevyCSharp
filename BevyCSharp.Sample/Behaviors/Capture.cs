using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Writes the window to a PNG on a chosen frame, when asked for by the environment.
/// </summary>
/// <remarks>
/// How the sample is checked without somebody looking at it: a run with <c>BCS_SHOT</c> set leaves
/// a picture behind, and a picture is the only thing that says whether an interface laid itself
/// out. Nothing happens without the variable, so an ordinary run pays a comparison a frame.
/// </remarks>
[Behavior]
public partial struct Capture
{
    /// <summary>Which frame to take, so a run catches the same moment every time.</summary>
    private static ulong _at;

    /// <summary>Whether the picture has been taken.</summary>
    private static bool _taken;

    /// <summary>Takes the picture, once.</summary>
    [OnRender]
    public static void Shoot(BehaviorContext ctx)
    {
        if (_taken) return;
        if (Environment.GetEnvironmentVariable("BCS_SHOT") is not { Length: > 0 } path) return;

        if (_at == 0)
        {
            _at = ulong.TryParse(
                Environment.GetEnvironmentVariable("BCS_SHOT_FRAME"), out var chosen)
                ? chosen
                : 120;
        }

        if (ctx.Time.FrameCount < _at) return;

        _taken = true;
        Render.Screenshot(path);
        Console.WriteLine($"[sample] wrote {path} on frame {ctx.Time.FrameCount}");
    }
}

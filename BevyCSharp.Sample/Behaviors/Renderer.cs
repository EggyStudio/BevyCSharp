using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Sets up the window and reports which graphics adapter the renderer actually picked.
/// </summary>
/// <remarks>
/// <para>
/// Every method here is inert in a headless run, so the sample's behavior scripts are identical
/// in both modes, which is the point. The engine decides whether there is a renderer; the
/// scripts do not branch on it.
/// </para>
/// <para>
/// The camera is a stopgap: Bevy draws nothing without one, and Bevy's own render components are
/// not bridged to C# yet, so managed code cannot spawn a camera itself. Until they are, a
/// windowed run shows a cleared frame rather than geometry, while the behaviors below tick away
/// exactly as they do headless.
/// </para>
/// </remarks>
[Behavior]
public partial struct Renderer
{
    /// <summary>Gives the window a camera and prints the adapter that was chosen.</summary>
    [OnStartup]
    public static void Describe(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        App.SpawnRenderCamera();

        var adapter = App.DescribeAdapter();
        Console.WriteLine(adapter is null
            ? "[Renderer] the renderer has not reported an adapter"
            : $"[Renderer] adapter: {adapter}");
    }

    /// <summary>Closes the window on Escape, so the sample is easy to get out of.</summary>
    [OnUpdate]
    public static void QuitOnEscape(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Escape)) ctx.Exit();
    }

    /// <summary>Prints the frame rate every second while the window is open.</summary>
    [OnLast]
    [ToggleKey(Key.F1)]
    public static void PrintFps(BehaviorContext ctx)
    {
        if (ctx.Res<Config>().Headless) return;
        if (ctx.Time.FrameCount == 0 || ctx.Time.FrameCount % 60 != 0) return;

        Console.WriteLine(
            $"[Renderer] {ctx.Time.SmoothedFps,6:F1} fps   "
            + $"frame {ctx.Time.FrameCount,6}   "
            + $"spinners {ctx.Ecs.Count<Spinner>()}");
    }
}

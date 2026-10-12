// Bevy's render_recovery example, examples/app/render_recovery.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Application;

// Demonstrates how to trigger various rendering errors, and how the renderer recovers from them.
// A cube on a disc turns under the camera, O, V, D and L cause an error of each kind on the GPU,
// and 1 to 5 choose what an error does. Bevy's 2 panics, and a C# game cannot panic the renderer's
// thread, so this one stops drawing and ends the run with Rust's panic code, 101, once the error
// arrives, as the logs example ends its run.
internal static class RenderRecovery
{
    // Whether an error ends the run, as Bevy's panicking handler does, and how many had arrived when
    // that was chosen.
    private static bool _panicOnError;
    private static int _seen;

    private static Entity _camera;

    public static void Build(App app)
    {
        (_panicOnError, _seen) = (false, 0);
        Render.ErrorPolicy = RenderErrorPolicy.Quit;

        app.Startup(Setup, "render_recovery.Setup");
        app.Update(UpdateCamera, "render_recovery.UpdateCamera");
        app.Update(Input, "render_recovery.Input");
        app.Update(PanicOnError, "render_recovery.PanicOnError");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // A circular base, which Bevy's circle makes facing +Z, laid flat.
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Circle, 4f),
            Render.CreateMaterial((1f, 1f, 1f, 1f)),
            new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f),
            Render.CreateMaterial(Color.FromSrgb8(124, 144, 255)),
            Transform.At(0f, 1f, 0f));

        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));

        Ui.SpawnText(
            "Test at your own risk: you may need to restart your computer to fully recover\n"
            + "Press O to trigger an OutOfMemory error\n"
            + "Press V to trigger a Validation error\n"
            + "Press D to Destroy the render device (causes device lost error)\n"
            + "Press L to Loop infinitely in a compute shader (causes device lost error)\n"
            + "\n"
            + "Press 1 to ignore errors, pretending nothing happened and continue rendering.\n"
            + "Press 2 to panic on error.\n"
            + "Press 3 to signals app exit on error.\n"
            + "Press 4 to keeps the app alive, but stops rendering further on error.\n"
            + "Press 5 to attempt renderer recovery.\n",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // The camera goes round the scene, a turn every twenty-one seconds.
    private static void UpdateCamera(BehaviorContext ctx)
    {
        var (s, c) = MathF.SinCos(ctx.Time.Elapsed * 0.3f);
        ctx.Ecs.Set(_camera, Transform.LookingAt(new Vec3(s * 10f, 4.5f, c * 10f), Vec3.Zero, Vec3.UnitY));
    }

    private static void Input(BehaviorContext ctx)
    {
        var input = ctx.Input;

        if (input.KeyPressed(Key.O)) Render.CauseError(RenderErrorCause.OutOfMemory);
        if (input.KeyPressed(Key.V)) Render.CauseError(RenderErrorCause.Validation);
        if (input.KeyPressed(Key.D)) Render.CauseError(RenderErrorCause.DeviceLost);
        if (input.KeyPressed(Key.L)) Render.CauseError(RenderErrorCause.EndlessLoop);

        if (input.KeyPressed(Key.Digit1)) Choose(RenderErrorPolicy.Ignore);
        if (input.KeyPressed(Key.Digit2))
        {
            Choose(RenderErrorPolicy.StopRendering);
            (_panicOnError, _seen) = (true, Render.ErrorCount);
        }
        if (input.KeyPressed(Key.Digit3)) Choose(RenderErrorPolicy.Quit);
        if (input.KeyPressed(Key.Digit4)) Choose(RenderErrorPolicy.StopRendering);
        if (input.KeyPressed(Key.Digit5)) Choose(RenderErrorPolicy.Recover);
    }

    private static void Choose(RenderErrorPolicy policy)
    {
        Render.ErrorPolicy = policy;
        _panicOnError = false;
    }

    // Bevy's panicking handler, an error that arrived ending the run.
    private static void PanicOnError(BehaviorContext ctx)
    {
        if (!_panicOnError || Render.ErrorCount == _seen) return;

        Log.Error($"Rendering error {Shaders.LastRenderError}");
        ctx.Exit(101);
    }
}

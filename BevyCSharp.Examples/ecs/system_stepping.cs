// Bevy's system_stepping example, examples/ecs/system_stepping.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Steps through the systems of Update in the order they run, a frame at a time, saying before each
// frame what it asks of the stepping and which systems will run.
//
// Bevy's example changes the stepping between calls to app.update() from its main, which a C#
// program, whose loop the bridge runs, does from a system in a stage that is not stepped. A call
// made there is taken at the start of the next frame, as Bevy takes one made between two updates,
// so each frame's account is printed at the end of the frame before it, and the first by Startup.
//
// Bevy's Update holds the example's four systems alone, and this app's holds every example's
// behaviors as well, so the rest are left to run, as Bevy's breakout leaves Bevy's own, and a step
// runs one of the four as Bevy's does.
internal static class SystemStepping
{
    private static readonly string[] Own =
        ["SystemStepping.UpdateSystemOne", "SystemStepping.UpdateSystemTwo", "SystemStepping.UpdateSystemThree", "SystemStepping.UpdateSystemFour"];

    public static void Build(App app)
    {
        app.Chain(Stage.Update,
            new SystemDescriptor(UpdateSystemOne),
            new SystemDescriptor(UpdateSystemTwo),
            new SystemDescriptor(UpdateSystemThree));
        app.AddSystem(Stage.Update, new SystemDescriptor(UpdateSystemFour));
        app.AddSystem(Stage.PreUpdate, new SystemDescriptor(PreUpdateSystem));

        app.Startup(_ => Console.WriteLine(Normal), "system_stepping.Start");

        // The frames counted from the first, each step said at the end of the frame before the one
        // it changes.
        var frame = 0;
        app.On(Stage.Last, _ =>
        {
            frame++;
            switch (frame)
            {
                case 1:
                    // Bevy's Stepping resource is made by the first call, and the stage is added a
                    // frame early so its systems are known when stepping is turned on, which with
                    // stepping off changes nothing, as this frame's account says of the resource.
                    Console.WriteLine(Resource);
                    Stepping.Add(Stage.Update);
                    break;
                case 2:
                    Console.WriteLine(Enabled);
                    foreach (var other in Stepping.Systems!.Select(system => system.Name).Except(Own))
                        Stepping.AlwaysRun(Stage.Update, other);
                    Stepping.Enable();
                    break;
                case 3:
                    Console.WriteLine(Step);
                    Stepping.StepFrame();
                    break;
                case 4:
                    Console.WriteLine(AfterStep);
                    break;
                case 5:
                    Console.WriteLine(Continue);
                    Stepping.ContinueFrame();
                    break;
                case >= 6 and <= 9:
                    if (frame == 6) Console.WriteLine(FourSteps);
                    Stepping.StepFrame();
                    break;
                case >= 10 and <= 12:
                    if (frame == 10)
                    {
                        Console.WriteLine(AlwaysRun);
                        Stepping.AlwaysRun(Stage.Update, "SystemStepping.UpdateSystemTwo");
                    }
                    Stepping.StepFrame();
                    break;
                case 13:
                    Console.WriteLine(NeverRun);
                    Stepping.NeverRun(Stage.Update, "SystemStepping.UpdateSystemTwo");
                    Stepping.ContinueFrame();
                    break;
                case 14:
                    Console.WriteLine(Breakpoint);
                    Stepping.SetBreakpoint(Stage.Update, "SystemStepping.UpdateSystemTwo");
                    Stepping.ContinueFrame();
                    break;
                case 15:
                    Stepping.StepFrame();
                    break;
                case 16:
                    Stepping.ContinueFrame();
                    break;
                case 17:
                    Console.WriteLine(ClearBreakpoint);
                    Stepping.ClearBreakpoint(Stage.Update, "SystemStepping.UpdateSystemTwo");
                    Stepping.ContinueFrame();
                    break;
                case 18:
                    Console.WriteLine(Disabled);
                    Stepping.Disable();
                    break;
            }
        }, "system_stepping.Script");
    }

    private static void PreUpdateSystem(World _) => Console.WriteLine("▶ pre_update_system");
    private static void UpdateSystemOne(World _) => Console.WriteLine("▶ update_system_one");
    private static void UpdateSystemTwo(World _) => Console.WriteLine("▶ update_system_two");
    private static void UpdateSystemThree(World _) => Console.WriteLine("▶ update_system_three");
    private static void UpdateSystemFour(World _) => Console.WriteLine("▶ update_system_four");

    private const string Normal = """

            Actions: call app.update()
             Result: All systems run normally
        """;

    private const string Resource = """

            Actions: Add the Stepping resource then call app.update()
             Result: All systems run normally.  Stepping has no effect unless explicitly
                     configured for a Schedule, and Stepping has been enabled.
        """;

    private const string Enabled = """

            Actions: Add the Update Schedule to Stepping; enable Stepping; call
                     app.update()
             Result: Only the systems in PreUpdate run.  When Stepping is enabled,
                     systems in the configured schedules will not run unless:
                     * Stepping::step_frame() is called
                     * Stepping::continue_frame() is called
                     * System has been configured to always run
        """;

    private const string Step = """

            Actions: call Stepping.step_frame(); call app.update()
             Result: The PreUpdate systems run, and one Update system will run.  In
                     Stepping, step means run the next system across all the schedules 
                     that have been added to the Stepping resource.
        """;

    private const string AfterStep = """

            Actions: call app.update()
             Result: Only the PreUpdate systems run.  The previous call to
                     Stepping::step_frame() only applies for the next call to
                     app.update()/the next frame rendered.
            
        """;

    private const string Continue = """

            Actions: call Stepping::continue_frame(); call app.update()
             Result: PreUpdate system will run, and all remaining Update systems will
                     run.  Stepping::continue_frame() tells stepping to run all systems
                     starting after the last run system until it hits the end of the
                     frame, or it encounters a system with a breakpoint set.  In this
                     case, we previously performed a step, running one system in Update.
                     This continue will cause all remaining systems in Update to run.
        """;

    private const string FourSteps = """

            Actions: call Stepping::step_frame() & app.update() four times in a row
             Result: PreUpdate system runs every time we call app.update(), along with
                     one system from the Update schedule each time.  This shows what
                     execution would look like to step through an entire frame of 
                     systems.
        """;

    private const string AlwaysRun = """

            Actions: Stepping::always_run(Update, update_system_two); step through all
                     systems
             Result: PreUpdate system and update_system_two() will run every time we
                     call app.update().  We'll also only need to step three times to
                     execute all systems in the frame.  Stepping::always_run() allows
                     us to granularly allow systems to run when stepping is enabled.
        """;

    private const string NeverRun = """

            Actions: Stepping::never_run(Update, update_system_two); continue through
                     all systems
             Result: All systems except update_system_two() will execute.
                     Stepping::never_run() allows us to disable systems while Stepping
                     is enabled.
        """;

    private const string Breakpoint = """

            Actions: Stepping::set_breakpoint(Update, update_system_two); continue,
                     step, continue
             Result: During the first continue, pre_update_system() and
                     update_system_one() will run.  update_system_four() may also run
                     as it has no dependency on update_system_two() or
                     update_system_three().  Nether update_system_two() nor
                     update_system_three() will run in the first app.update() call as
                     they form a chained dependency on update_system_one() and run
                     in order of one, two, three.  Stepping stops system execution in
                     the Update schedule when it encounters the breakpoint for
                     update_system_two().
                     During the step we run update_system_two() along with the
                     pre_update_system().
                     During the final continue pre_update_system() and
                     update_system_three() run.
        """;

    private const string ClearBreakpoint = """

            Actions: Stepping::clear_breakpoint(Update, update_system_two); continue
                     through all systems
             Result: All systems will run
        """;

    private const string Disabled = """

            Actions: Stepping::disable(); app.update()
             Result: All systems will run.  With Stepping disabled, there's no need to
                     call Stepping::step_frame() or Stepping::continue_frame() to run
                     systems in the Update schedule.
        """;
}

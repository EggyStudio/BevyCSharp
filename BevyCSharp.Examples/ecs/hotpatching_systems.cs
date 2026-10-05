using System.Collections.Concurrent;
using System.Diagnostics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// Changes a system while the app runs. Bevy's example runs under the Dioxus CLI, which patches the
// code of a system into the running program when its file is saved, and .NET does the same for any
// method's body when the program is run with dotnet watch:
//
//   dotnet watch --project BevyCSharp.Examples -- hotpatching_systems
//
// Change the text UpdateText writes and save, and the next frame shows the new text. Change the
// color OnClick sets, save and click the text, and it takes the new color. A method that is no
// system can be changed so too, as the wait on the thread below shows.
internal static class HotpatchingSystems
{
    private static Entity _node, _text;
    private static UiInteraction _last;

    // Bevy keeps the sending end in a resource, for its click observer to tell the thread it has
    // something to do.
    private static BlockingCollection<bool>? _tasks;

    public static void Build(App app)
    {
        // A thread of an earlier run in this process is told to stop rather than left waiting.
        _tasks?.CompleteAdding();
        _tasks = new BlockingCollection<bool>();
        StartThread(_tasks);
        _last = UiInteraction.None;

        app.Startup(Setup, "hotpatching_systems.Setup");
        app.Update(UpdateText, "hotpatching_systems.UpdateText");
        app.Update(OnClick, "hotpatching_systems.OnClick");
    }

    private static void UpdateText(BehaviorContext ctx)
    {
        // Anything in the body of a system can be changed, and a change to this text shows on the
        // next frame.
        Ui.SetText(_text, "before");
    }

    // Bevy observes a click on the whole window's node. A click here is a press let go over it,
    // read from the node's interaction as it changes.
    private static void OnClick(BehaviorContext ctx)
    {
        var interaction = Ui.InteractionOf(_node);
        var clicked = _last == UiInteraction.Pressed && interaction == UiInteraction.Hovered;
        _last = interaction;
        if (!clicked) return;

        // A change to this color shows on the next click.
        ctx.Ecs.Wrap<TextColorRef>(_text).Value = Color.FromHex("#dc2626");

        _tasks?.Add(true);
    }

    private static void Setup(BehaviorContext ctx)
    {
        Render2d.SpawnCamera2d();

        _node = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Direction = UiDirection.Column,
            Interactive = true,
        });
        _text = Ui.SpawnText("", new UiSettings(), 100f);
        ctx.Ecs.SetParent(_text, _node);
    }

    // A thread that waits for a click, then waits as long as Duration says and reports how long
    // it took. .NET runs the new body of a changed method from the next call of it, so the loop,
    // which is never called again, runs as it was, while a change to the wait Duration gives is
    // taken on the next click.
    private static void StartThread(BlockingCollection<bool> tasks)
    {
        new Thread(() =>
        {
            foreach (var _ in tasks.GetConsumingEnumerable())
            {
                var start = Stopwatch.GetTimestamp();
                Thread.Sleep(Duration());
                Console.WriteLine($"done after {Stopwatch.GetElapsedTime(start)}");
            }
        })
        { IsBackground = true, Name = "hotpatching_systems" }.Start();
    }

    private static TimeSpan Duration() => TimeSpan.FromSeconds(2);
}

using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// TEMPORARY PROBE for the HTML surface round trip.
[Behavior]
public partial struct XuiProbe
{
    private static UiDocument _doc;

    [OnStartup]
    public static void Open(BehaviorContext ctx)
    {
        if (!App.HasEditor || ctx.Res<Config>().Headless) return;
        _doc = Xui.Open("panels/spike.html");
        Console.WriteLine($"[probe] opened {_doc}, IsOpen={_doc.IsOpen}");
    }

    [OnUpdate]
    public static void Probe(BehaviorContext ctx)
    {
        if (!_doc.IsOpen || ctx.Time.FrameCount != 120) return;

        foreach (var id in new[] { "title", "bloom", "intensity", "name", "apply", "missing" })
            Console.WriteLine($"[probe] '{id}' -> {Xui.Element(id)}");

        var slider = Xui.Element("intensity");
        Xui.SetNumber(slider, 0.75f);
        Console.WriteLine($"[probe] slider round trip -> {Xui.GetNumber(slider)}");

        var check = Xui.Element("bloom");
        Xui.SetFlag(check, true);
        Console.WriteLine($"[probe] checkbox round trip -> {Xui.GetFlag(check)}");

        var field = Xui.Element("name");
        Xui.SetText(field, "a name with an é in it");
        Console.WriteLine($"[probe] text round trip -> '{Xui.GetText(field)}'");

        Console.WriteLine($"[probe] heading text -> '{Xui.GetText(Xui.Element("title"))}'");
    }

    [OnUpdate]
    public static void Events(BehaviorContext ctx)
    {
        if (!_doc.IsOpen) return;
        foreach (var e in Xui.Drain())
            Console.WriteLine($"[probe] {e.Kind} on {e.Element}");
    }
}

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates the order nodes are drawn in, five colored boxes inside a gray one, raised or
// lowered among their siblings by a z-index and against the whole interface by a global one.
internal static class ZIndex
{
    private enum Order { None, Local, Global }

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render.SetClearColor((0f, 0f, 0f, 1f));
        Render2d.SpawnCamera2d();

        var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        var gray = Ui.SpawnNode(new UiSettings { Width = Length.Px(180f), Height = Length.Px(100f), Color = Scene.Srgb8(128, 128, 128) });
        ecs.SetParent(gray, middle);

        foreach (var (left, bottom, width, height, color, kind, index) in new[]
        {
            (10f, 40f, 100f, 50f, Scene.Srgb8(255, 0, 0), Order.None, 0),
            (45f, 30f, 100f, 50f, Scene.Srgb8(0, 0, 255), Order.Local, 2),
            (70f, 20f, 100f, 75f, Scene.Srgb8(0, 255, 0), Order.Local, -1),
            (15f, 10f, 100f, 60f, Scene.Srgb8(128, 0, 128), Order.Global, 1),
            (-15f, -15f, 100f, 125f, Scene.Srgb8(255, 255, 0), Order.Global, -1),
        })
        {
            var box = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(left), Bottom = Length.Px(bottom), Width = Length.Px(width), Height = Length.Px(height), Color = color });
            if (kind == Order.Local) ecs.Insert<ZIndexRef>(box).Value = index;
            else if (kind == Order.Global) ecs.Insert<GlobalZIndexRef>(box).Value = index;
            ecs.SetParent(box, gray);
        }
    }, "z_index.Setup");
}

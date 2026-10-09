// Bevy's multi_window_text example, examples/window/multi_window_text.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// Renders text to multiple windows with different scale factors using both Text and Text2d. The
// primary window is held at a scale of one and the secondary one at two, so its text is laid out
// from glyphs drawn at twice the resolution and stands twice as large. Each window has a 2D camera
// of its own on a render layer of its own, the interface text names the camera it is drawn by, and
// one Text2d on both layers is drawn by both cameras, laid out once at the larger scale.
internal static class MultiWindowText
{
    private static readonly Color Yellow = Color.FromSrgb(1f, 1f, 0f);
    private static readonly Color LightCyan = Color.FromSrgb(224f / 255f, 1f, 1f);

    public static void Configure(Config config)
    {
        config.Title = "Primary window";
        config.ScaleFactor = 1f;
    }

    public static void Build(App app) => app.Startup(SetupScene, "multi_window_text.SetupScene");

    private static void SetupScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The first camera draws the primary window and the default layer, layer zero.
        Render2d.SpawnCamera2d();

        var secondaryWindow = ecs.Spawn();
        var window = ecs.Insert<WindowRef>(secondaryWindow);
        window.Title = "Secondary Window";
        window.ResolutionScaleFactorOverride = 2f;

        // The second camera draws the secondary window and layer one alone.
        var secondaryWindowCamera = Render2d.SpawnCamera2d();
        Render.SetLayers(ecs, secondaryWindowCamera, 1u << 1);
        Render.SetCameraTarget(secondaryWindowCamera, secondaryWindow);

        // An interface node naming no camera is drawn by the one on the primary window, and one
        // naming the second camera by that one. Nodes ignore render layers.
        Label(ecs, "UI Text Primary Window", Entity.None);
        Label(ecs, "UI Text Secondary Window", secondaryWindowCamera);

        Text2d(ecs, "Text2d Primary Window", Yellow, 1u << 0, 0f);
        Text2d(ecs, "Text2d Secondary Window", Yellow, 1u << 1, 0f);

        // On both layers, so drawn by both cameras, from one layout made at the secondary window's
        // scale and drawn smaller on the primary window.
        Text2d(ecs, "Text2d Both Windows", LightCyan, (1u << 0) | (1u << 1), -50f);
    }

    private static void Label(EcsWorld ecs, string text, Entity camera)
    {
        var node = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f), Camera = camera });
        var label = Ui.SpawnText(text, new UiSettings(), 30f);
        ecs.Insert<TextShadowRef>(label);
        ecs.SetParent(label, node);
    }

    private static void Text2d(EcsWorld ecs, string text, Color color, uint layers, float y)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.At(0f, y, 0f));
        ecs.Insert<Text2dRef>(entity).Value = text;
        ecs.Insert<TextColorRef>(entity).Value = color;
        ecs.Insert<TextFontRef>(entity).FontSize = new FontSize.Px(30f);
        ecs.Insert<Text2dShadowRef>(entity);
        Render.SetLayers(ecs, entity, layers);
    }
}

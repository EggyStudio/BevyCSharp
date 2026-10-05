using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Shows how a text gizmo's anchor decides which point of it stands at its position: each word
// turns about the point marked with a cross, which is its left, right, middle, top or bottom.
internal static class AnchoredTextGizmos
{
    public static void Build(App app)
    {
        app.Startup(_ => Render2d.SpawnCamera2d(), "anchored_text_gizmos.SetupCamera");

        app.Update(ctx =>
        {
            var t = ctx.Time.Elapsed;
            foreach (var (label, anchor, color) in new[]
            {
                ("left", (-0.5f, 0f), (1f, 0f, 0f, 1f)),
                ("right", (0.5f, 0f), Scene.Srgb8(255, 165, 0)),
                ("center", (0f, 0f), (1f, 1f, 0f, 1f)),
                ("top", (0f, 0.5f), Scene.Srgb8(0, 128, 0)),
                ("bottom", (0f, -0.5f), (0f, 0f, 1f, 1f)),
            })
            {
                var position = (350f * anchor.Item1, 350f * anchor.Item2);
                Gizmos.Text2d("+", position, 0f, 12f, (0f, 0f), (1f, 1f, 1f, 1f));
                Gizmos.Text2d(label, position, t, 25f, anchor, color);
            }
        }, "anchored_text_gizmos.Anchors");
    }
}

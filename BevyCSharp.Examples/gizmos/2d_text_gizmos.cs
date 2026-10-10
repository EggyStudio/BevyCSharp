// Bevy's 2d_text_gizmos example, examples/gizmos/2d_text_gizmos.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Draws many text gizmos at once, fifty labels, the frame rate, a large line and every glyph, as a
// measure of what they cost. Bevy's opens a window of 1920 by 1080, and in a window of the size
// the examples here open, the labels toward its edges run past them.
internal static class TextGizmos2d
{
    private const int TextCount = 50;
    private const float StartX = -800f, StartY = 200f, XStep = 250f, YStep = 50f;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            Render2d.SpawnCamera2d();
            Gizmos.Configure(width: 1f);
        }, "2d_text_gizmos.Setup");

        app.Update(ctx =>
        {
            (float, float, float, float)[] colors = [(1f, 0f, 0f, 1f), Color.FromSrgb8(0, 128, 0), (0f, 0f, 1f, 1f), (1f, 1f, 0f, 1f)];
            for (var i = 0; i < TextCount; i++)
            {
                var (row, column) = (i / 5, i % 5);
                Gizmos.Text2d($"label {i}", (StartX + column * XStep, StartY - row * YStep), 2f * MathF.PI / 180f, 25f, (0f, 0f), colors[i % 4]);
            }

            Gizmos.Text2d(FormattableString.Invariant($"fps: {ctx.Time.SmoothedFps:0.0}"), (600f, StartY + 150f), 0f, 25f, (0f, 0f), (1f, 1f, 1f, 1f));
            Gizmos.Text2d("lxgh", (-300f, StartY + 200f), 0f, 150f, (0f, 0f), (1f, 1f, 1f, 1f));
            Gizmos.Text2d(TextGizmosFont.AllGlyphs, (600f, 0f), 0f, 30f, (0f, 0f), (1f, 1f, 1f, 1f));
        }, "2d_text_gizmos.Draw");
    }
}

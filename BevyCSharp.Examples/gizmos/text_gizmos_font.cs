using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Shows every glyph of the Simplex stroke font text gizmos are drawn in.
internal static class TextGizmosFont
{
    internal const string AllGlyphs = " !\"#$%&'()*\n+,-./012345\n6789:;<=>?@\nABCDEFGHIJK\nLMNOPQRSTUV\nWXYZ[\\]^_`a\nbcdefghijkl\nmnopqrstuvw\nxyz{|}~";

    public static void Build(App app)
    {
        app.Startup(_ => Render2d.SpawnCamera2d(), "text_gizmos_font.SetupCamera");
        app.Update(_ => Gizmos.Text2d(AllGlyphs, (0f, 0f), 0f, 40f, (0f, 0f), (1f, 1f, 1f, 1f)), "text_gizmos_font.DrawAllGlyphs");
    }
}

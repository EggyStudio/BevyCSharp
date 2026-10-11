// Bevy's ui_material example, examples/ui/ui_material.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// A node drawn by an interface material of a shader's own, Bevy's banner as a progress bar that
// fills and empties every two seconds, tinted through the colors of the rainbow inside a rainbow
// border with rounded corners, the node's dark blue outline drawn a hundred pixels out as any
// node's would be.
internal static class UiMaterialExample
{
    private const float Duration = 2f;

    private static ShaderMaterial _material;

    public static void Build(App app)
    {
        app.Startup(Setup, "ui_material.Setup");
        app.Update(Animate, "ui_material.Animate");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
        });

        const float bannerScaleFactor = 0.5f;
        var banner = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Width = Length.Px(905f * bannerScaleFactor),
            Height = Length.Px(363f * bannerScaleFactor),
            Border = Length.Px(20f),
            Corners = Length.Px(20f),
        });
        ecs.SetParent(banner, root);

        var program = Shaders.CreateProgram(new ShaderProgramSettings { FragmentUi = "shaders/custom_ui_material.slang" });
        _material = Shaders.CreateUiMaterial(program)
            .Set("color", Vector4.One)
            .Set("slider", new Vector4(0.5f))
            .SetTexture("material_color_texture", AssetServer.Load(AssetKind.Image, "branding/banner.png"))
            .Set("border_color", Vector4.One);
        Ui.SetMaterial(banner, _material);

        // Interface material nodes can have outlines and shadows like any other node.
        var outline = ecs.Insert<OutlineRef>(banner);
        (outline.Width, outline.Offset, outline.Color) = (new Val.Px(2f), new Val.Px(100f), Color.FromSrgb(0f, 0f, 139f / 255f));
    }

    // Bevy's animate, filling the slider over two seconds and emptying it over two more, the image
    // and the border tinted around the color wheel.
    private static void Animate(BehaviorContext ctx)
    {
        var elapsed = ctx.Time.Elapsed;
        var hue = elapsed * 60f % 360f;
        var color = Color.FromHsl(hue, 1f, 0.5f);
        var border = Color.FromHsl(hue, 0.75f, 0.75f);

        _material
            .Set("color", new Vector4(color.R, color.G, color.B, color.A))
            .Set("slider", new Vector4(MathF.Abs(elapsed % (Duration * 2f) - Duration) / Duration, 0f, 0f, 0f))
            .Set("border_color", new Vector4(border.R, border.G, border.B, border.A));
    }
}

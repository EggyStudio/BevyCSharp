// Bevy's ui_texture_slice_flip_and_tile example,
// examples/ui/images/ui_texture_slice_flip_and_tile.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows nine-slice images that tile their sides and middle rather than stretch them, each flipped
// four ways and drawn at five sizes, with Bevy's UiScale resource at two so the pixels can be
// counted.
internal static class UiTextureSliceFlipAndTile
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        if (ecs.Resource<UiScaleRef>() is { } scale) scale.Value = 2f;

        // Sampled by the nearest pixel, so a tiled slice does not bleed into the one beside it.
        var image = AssetServer.LoadImage("textures/fantasy_ui_borders/numbered_slices.png", new TextureSettings());
        Render2d.SpawnCamera2d();

        var page = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            AlignContent = UiJustify.Center,
            Wrap = UiWrap.Wrap,
            ColumnGap = Length.Px(10f),
            RowGap = Length.Px(10f),
        });

        // The image is 48 pixels square, cut sixteen in from each edge into nine slices of sixteen,
        // and each tiled slice laid at the size it is in the image.
        foreach (var (columns, rows) in new[] { (3, 3), (4, 4), (5, 4), (4, 5), (5, 5) })
        {
            foreach (var (flipX, flipY) in new[] { (false, false), (false, true), (true, false), (true, true) })
            {
                var node = Ui.SpawnNode(new UiSettings { Width = Length.Px(16f * columns), Height = Length.Px(16f * rows) });
                Ui.SetImage(node, new UiImageSettings
                {
                    Image = image,
                    FlipX = flipX,
                    FlipY = flipY,
                    Mode = UiImageMode.Sliced,
                    SliceBorder = (16f, 16f, 16f, 16f),
                    SliceTiling = SliceTiling.All,
                    TileStretch = 1f,
                });
                ecs.SetParent(node, page);
            }
        }
    }, "ui_texture_slice_flip_and_tile.Setup");
}

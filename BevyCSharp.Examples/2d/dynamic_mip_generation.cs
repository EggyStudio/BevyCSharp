// Bevy's dynamic_mip_generation example, examples/2d/dynamic_mip_generation.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// An image of two ellipses in colors picked at random, its top level written here and every level
// below it made on the GPU each frame. On the left it grows and shrinks, so the GPU samples each
// level in turn, and on the right each level is shown alone, labeled with its size. The buttons at
// the bottom left draw the image again in new colors, turn the making of its levels off, so a new
// image fades out as it shrinks, and change its width and height.
internal static class DynamicMipGeneration
{
    private const float AnimationPeriod = 2f;
    private const float MipSlicesMarginLeft = 64f;
    private const float MipSlicesMarginRight = 12f;
    private const float MipSlicesWidth = 1f / 6f;
    private const float FontSize = 16f;

    private static readonly uint[] Sizes = [240, 480, 640, 1080, 1920];

    // Bevy's AppStatus. The random colors come from .NET's generator seeded as Bevy's is.
    private static bool _generate = true;
    private static uint _width = 640, _height = 480;
    private static readonly Random Rng = new(unchecked((int)19878367467713L));

    // Bevy's option values, a type for each row, since a row's handler knows its own by its type.
    private enum EnableMipGeneration { On, Off }
    private readonly record struct ImageWidth(uint Size);
    private readonly record struct ImageHeight(uint Size);

    private static AssetHandle _rectangle, _font;
    private static Entity _animated;
    private static readonly List<Entity> Views = [];
    private static (uint, uint) _window;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        app.Startup(Setup, "dynamic_mip_generation.Setup");
        app.Update(AnimateImageScale, "dynamic_mip_generation.AnimateImageScale");
        app.Update(HandleWindowResizeEvents, "dynamic_mip_generation.HandleWindowResizeEvents");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        _rectangle = Render.CreateMesh(MeshShape.Rectangle, 1f, 1f);
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        // Bevy's spawn_ui, the button and the three rows of choices at the bottom left.
        Theme.BasicExampleTheme(Color.White);
        var column = Radio.MainUiNode();
        var regenerate = Feathers.Button(column, "Regenerate Top Mip Level");
        var node = ecs.Wrap<NodeRef>(regenerate);
        (node.JustifyContent, node.AlignItems) = (NodeRef.JustifyContentVariant.Center, NodeRef.AlignItemsVariant.Center);
        ecs.Wrap<BackgroundColorRef>(regenerate).Value = Color.Black;
        ecs.Observe<Activate>(regenerate, on => Regenerate(on.Ecs));

        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Mip Generation", [(EnableMipGeneration.On, "On"), (EnableMipGeneration.Off, "Off")], 0), UiWidgetKind.RadioGroup);
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Image Width", [.. Sizes.Select(size => (new ImageWidth(size), size.ToString()))], 2), UiWidgetKind.RadioGroup);
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Image Height", [.. Sizes.Select(size => (new ImageHeight(size), size.ToString()))], 1), UiWidgetKind.RadioGroup);
        ecs.Observe<ValueChange<Entity>>(on => HandleEnableMipGenerationChange(on.Event.Value));
        ecs.Observe<ValueChange<Entity>>(on => HandleImageSizeSettingChange(on.Ecs, on.Event.Value));

        Regenerate(ecs);
    }

    // Turning the making of levels on or off draws nothing again, as in Bevy's.
    private static void HandleEnableMipGenerationChange(Entity button)
    {
        if (Radio.OptionValue<EnableMipGeneration>(button, out var enable))
            _generate = enable == EnableMipGeneration.On;
    }

    private static void HandleImageSizeSettingChange(EcsWorld ecs, Entity button)
    {
        if (Radio.OptionValue<ImageWidth>(button, out var width)) _width = width.Size;
        else if (Radio.OptionValue<ImageHeight>(button, out var height)) _height = height.Size;
        else return;

        Regenerate(ecs);
    }

    // A window resized draws the image again, as Bevy's handle_window_resize_events does.
    private static void HandleWindowResizeEvents(BehaviorContext ctx)
    {
        if (Window.Size() != _window) Regenerate(ctx.Ecs);
    }

    // Bevy's animate_image_scale, the image on the left grown and shrunk by a triangle wave.
    private static void AnimateImageScale(BehaviorContext ctx)
    {
        if (!ctx.Ecs.IsAlive(_animated)) return;

        var size = AnimatedMeshSize(WindowSize());
        var wave = TriangleWave(ctx.Time.Elapsed, AnimationPeriod);
        ref var transform = ref ctx.Ecs.GetRef<Transform>(_animated);
        transform.Scale = new Vec3(size.X * wave, size.Y * wave, 1f);
    }

    // Bevy's regenerate_image_when_requested, the views despawned, the image drawn again and the
    // views spawned for it.
    private static void Regenerate(EcsWorld ecs)
    {
        foreach (var view in Views) ecs.Despawn(view);
        Views.Clear();

        _window = Window.Size();
        var image = Render.CreateImage(ImageData(), _width, _height, srgb: false);
        if (_generate) Render.SetMipGeneration(image, true);

        SpawnAnimatedMesh(ecs, image);
        SpawnMipLevelViews(ecs, image);
    }

    // Bevy's spawn_animated_mesh, the image that grows and shrinks on the left.
    private static void SpawnAnimatedMesh(EcsWorld ecs, AssetHandle image)
    {
        var window = WindowSize();
        var area = AnimatedMeshArea(window);
        var size = AnimatedMeshSize(window);

        _animated = ecs.Spawn();
        ecs.Add(_animated, new Transform(new Vec3(area.X * 0.5f - window.X * 0.5f, area.Y * 0.5f - window.Y * 0.5f, 0f), Quat.Identity, new Vec3(size.X, size.Y, 1f)));
        Render2d.SetMesh(ecs, _animated, _rectangle);
        Render2d.SetMaterial(ecs, _animated, Render2d.CreateMaterial(new ColorMaterialSettings { Texture = image }));
        Views.Add(_animated);
    }

    // Bevy's spawn_mip_level_views, the column of levels on the right, each by itself and labeled.
    private static void SpawnMipLevelViews(EcsWorld ecs, AssetHandle image)
    {
        var window = WindowSize();
        var maxSlice = MaxMipSliceSize(window);
        var yOrigin = window.Y * 0.5f - VerticalMipSliceSpacing(window) * 0.5f;
        var ySpacing = VerticalMipSliceSpacing(window);
        var xOrigin = HorizontalMipSliceOrigin(window);
        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/single_mip_level.slang" });

        var level = 0u;
        foreach (var (width, height) in MipSizes())
        {
            var yCenter = yOrigin - ySpacing * level;

            // Each level fitted to its place in the column, keeping its shape.
            var scale = MathF.Min(MathF.Min(maxSlice.X / width, maxSlice.Y / height), 1f);

            var slice = ecs.Spawn();
            ecs.Add(slice, new Transform(new Vec3(xOrigin, yCenter, 0f), Quat.Identity, new Vec3(width * scale, height * scale, 1f)));
            Render2d.SetMesh(ecs, slice, _rectangle);
            Render2d.SetMaterial(ecs, slice, Shaders.CreateMaterial2d(program, AlphaMode2d.Blend)
                .Set("mip_level", level)
                .SetTexture("base_color_texture", image));
            Views.Add(slice);

            var label = ecs.Spawn();
            ecs.Add(label, Transform.At(xOrigin - maxSlice.X * 0.5f - 64f, yCenter, 0f));
            ecs.Insert<Text2dRef>(label).Value = $"Level {level}\n{width}×{height}";
            var font = ecs.Insert<TextFontRef>(label);
            (font.Font, font.FontSize) = (new FontSource.Handle(_font), new FontSize.Px(FontSize));
            ecs.Insert<TextLayoutRef>(label).Justify = TextLayoutRef.JustifyVariant.Center;
            ecs.Insert<Text2dShadowRef>(label);
            Views.Add(label);

            level++;
        }
    }

    // Bevy's generate_image_data, the inner ellipse in one random color and the ring around it in
    // another, clear outside both.
    private static byte[] ImageData()
    {
        var outer = new byte[3];
        var inner = new byte[3];
        Rng.NextBytes(outer);
        Rng.NextBytes(inner);

        var pixels = new byte[_width * _height * 4];
        var center = new Vec2(_width * 0.5f, _height * 0.5f);
        var outerRadii = center;
        var innerRadii = center * 0.5f;

        for (var y = 0u; y < _height; y++)
        {
            for (var x = 0u; x < _width; x++)
            {
                var p = new Vec2(x, y);
                var color = InEllipse(p, center, innerRadii) ? inner : InEllipse(p, center, outerRadii) ? outer : null;
                if (color is null) continue;

                var at = (int)(4 * (x + y * _width));
                (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = (color[0], color[1], color[2], 255);
            }
        }

        return pixels;
    }

    private static bool InEllipse(Vec2 point, Vec2 center, Vec2 radii)
    {
        var d = point - center;
        return d.X * d.X / (radii.X * radii.X) + d.Y * d.Y / (radii.Y * radii.Y) < 1f;
    }

    // Bevy's MipmapSizeIterator, each level half the one above, rounding down, to one pixel.
    private static IEnumerable<(uint Width, uint Height)> MipSizes()
    {
        var (width, height) = (_width, _height);
        while (true)
        {
            yield return (width, height);
            if (width == 1 && height == 1) yield break;
            (width, height) = (Math.Max(width / 2, 1), Math.Max(height / 2, 1));
        }
    }

    private static uint MipLevelCount() => 32 - (uint)System.Numerics.BitOperations.LeadingZeroCount(Math.Max(_width, _height));

    private static float TriangleWave(float time, float wavelength) =>
        2f * MathF.Abs(time / wavelength - MathF.Floor(time / wavelength + 0.5f));

    private static Vec2 WindowSize()
    {
        var (width, height) = Window.Size();
        return new Vec2(width, height);
    }

    private static float VerticalMipSliceSpacing(Vec2 window) => window.Y / MipLevelCount();

    private static Vec2 MaxMipSliceSize(Vec2 window) => new(window.X * MipSlicesWidth, VerticalMipSliceSpacing(window));

    private static float HorizontalMipSliceOrigin(Vec2 window) =>
        window.X * 0.5f - MaxMipSliceSize(window).X * 0.5f - MipSlicesMarginRight;

    private static Vec2 AnimatedMeshArea(Vec2 window) =>
        new(HorizontalMipSliceOrigin(window) * 2f - MipSlicesMarginLeft * 2f, window.Y);

    private static Vec2 AnimatedMeshSize(Vec2 window)
    {
        var area = AnimatedMeshArea(window);
        var scale = MathF.Min(area.X / _width, area.Y / _height);
        return new Vec2(_width * scale, _height * scale);
    }
}

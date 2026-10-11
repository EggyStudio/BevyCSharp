// Bevy's image_node_resizing example, examples/ui/images/image_node_resizing.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// How an image node sized by its picture differs from one stretched to its node, in two boxes the
// arrow keys resize, with Bevy's outlines of every node drawn but the text's so each box shows.
internal static class ImageNodeResizing
{
    private const string TextPrefix = "Compare NodeImageMode(Auto, Stretch) press `Up`/`Down` to resize height, press `Left`/`Right` to resize width\n";
    private const float MinResize = 1f;
    private const float MinWidth = 50f, MaxWidth = 100f, MinHeight = 10f, MaxHeight = 50f;
    private const float InitWidth = (MinWidth + MaxWidth) / 2f, InitHeight = (MinHeight + MaxHeight) / 2f;

    // Bevy's tailwind blue-100.
    private static readonly Color Blue100 = Color.FromSrgb(0.85882354f, 0.91764706f, 0.99607843f);

    private static readonly List<Entity> Groups = [];
    private static Entity _text;
    private static (float Height, float Width) _size;

    public static void Build(App app)
    {
        Groups.Clear();
        _size = (InitHeight, InitWidth);
        app.Startup(Setup, "image_node_resizing.Setup");
        app.Update(Resize, "image_node_resizing.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's outlines of every node, for seeing where the layout put each box.
        if (ecs.Resource<GlobalUiDebugOptionsRef>() is { } options) options.Enabled = true;

        var image = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        Render2d.SpawnCamera2d();

        var container = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Width = Length.Percent(100f), Height = Length.Percent(100f), Color = Color.White });
        UiGrid.Set(container, new GridSettings { Rows = [Track.MinContent, Track.Flex(1f)] });

        // The keys and the sizes, the one node left without an outline.
        _text = Ui.SpawnText(Describe(), new UiSettings { Padding = Sides.All(Length.Px(6f)), Color = (0f, 0f, 0f, 1f) });
        UiGrid.Place(_text, new GridPlacement { RowSpan = 1 });
        ecs.Insert<UiDebugOptionsRef>(_text).Enabled = false;
        ecs.SetParent(_text, container);

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.SpaceAround, Padding = Sides.All(Length.Px(10f)), Color = Color.Black });
        UiGrid.Place(column, new GridPlacement { RowSpan = 1 });
        ecs.SetParent(column, container);

        // Sized by the picture and the layout's limits on it.
        var auto = Group(ecs, column);
        for (var i = 0; i < 4; i++)
        {
            var node = Ui.SpawnNode(new UiSettings());
            Ui.SetImage(node, new UiImageSettings { Image = image, Mode = UiImageMode.Auto });
            ecs.SetParent(node, auto);
        }

        // Stretched to the node, a tenth to two fifths of the box across.
        var stretch = Group(ecs, column);
        foreach (var width in new[] { 10f, 20f, 30f, 40f })
        {
            var node = Ui.SpawnNode(new UiSettings { Height = Length.Percent(100f), Width = Length.Percent(width) });
            Ui.SetImage(node, new UiImageSettings { Image = image, Mode = UiImageMode.Stretch });
            ecs.SetParent(node, stretch);
        }
    }

    // A box of images the keys resize.
    private static Entity Group(EcsWorld ecs, Entity parent)
    {
        var group = Ui.SpawnNode(new UiSettings { Justify = UiJustify.Start, Width = Length.Percent(InitWidth), Height = Length.Percent(InitHeight), Color = Blue100 });
        ecs.SetParent(group, parent);
        Groups.Add(group);
        return group;
    }

    // Bevy's update, on_trigger_image_group and update_text, each held arrow growing or shrinking
    // the boxes by a hundredth of the screen a frame, within their limits, and the text saying so.
    private static void Resize(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var height = (input.KeyDown(Key.ArrowUp) ? MinResize : 0f) - (input.KeyDown(Key.ArrowDown) ? MinResize : 0f);
        var width = (input.KeyDown(Key.ArrowRight) ? MinResize : 0f) - (input.KeyDown(Key.ArrowLeft) ? MinResize : 0f);
        if (height == 0f && width == 0f) return;

        _size = (Math.Clamp(_size.Height + height, MinHeight, MaxHeight), Math.Clamp(_size.Width + width, MinWidth, MaxWidth));
        foreach (var group in Groups)
        {
            var node = ctx.Ecs.Wrap<NodeRef>(group);
            (node.Height, node.Width) = (new Val.Percent(_size.Height), new Val.Percent(_size.Width));
        }

        Ui.SetText(_text, Describe());
    }

    private static string Describe() => FormattableString.Invariant($"{TextPrefix}height : {_size.Height}%, width : {_size.Width}%");
}

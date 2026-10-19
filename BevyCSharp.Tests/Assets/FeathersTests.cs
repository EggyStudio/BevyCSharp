using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Feathers, Bevy's styled widgets, spawned from C# a control at a time, drawn in the dark
/// theme's colors and reporting what is done with them as Bevy's widgets do.
/// </summary>
[Collection("engine")]
public sealed class FeathersTests
{
    /// <summary>The fields the bridge reads, at the places it reads them.</summary>
    [Fact]
    public void ThePropsAreLaidOutAsTheBridgeReadsThem()
    {
        Assert.Equal(8, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.Min)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.Token)).ToInt32());
        Assert.Equal(24, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.TokenLength)).ToInt32());
        Assert.Equal(28, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.Value)).ToInt32());
        Assert.Equal(36, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.Count)).ToInt32());
        Assert.Equal(52, Marshal.OffsetOf<NativeFeathersProps>(nameof(NativeFeathersProps.Top)).ToInt32());
        Assert.Equal(56, Marshal.SizeOf<NativeFeathersProps>());
    }

    /// <summary>
    /// The tokens are Bevy's, every one of its list and nothing else, read from Feathers' source in
    /// the cargo registry, which a machine that has built the bridge holds.
    /// </summary>
    [SkippableFact]
    public void TheTokensAreBevys()
    {
        var home = Environment.GetEnvironmentVariable("CARGO_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo");
        var source = Directory.Exists(Path.Combine(home, "registry", "src"))
            ? Directory.GetDirectories(Path.Combine(home, "registry", "src"))
                .Select(index => Path.Combine(index, "bevy_feathers-0.20.0", "src", "tokens.rs"))
                .FirstOrDefault(File.Exists)
            : null;
        Skip.If(source is null, "Feathers' source is not in the cargo registry, which a build of the bridge fetches");

        var bevys = Regex.Matches(File.ReadAllText(source!), @"pub const \w+: ThemeToken =\s*ThemeToken::new_static\(""([^""]+)""\)")
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var ours = typeof(FeathersToken).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(bevys);
        Assert.Equal(bevys, ours);
    }

    /// <summary>
    /// A control is refused where the app did not ask for Feathers, rather than spawned with no
    /// theme to color it.
    /// </summary>
    [SkippableFact]
    public void AControlNeedsTheAppToAskForFeathers()
    {
        Needs.Renderer();

        BevyNativeException? refused = null;
        var run = new PictureRun
        {
            Scene = _ =>
            {
                try { Feathers.Button(Entity.None, "Go"); }
                catch (BevyNativeException e) { refused = e; }
            },
        };
        run.Until("started", world => world.Resource<Time>().FrameCount >= 1).Go();

        Assert.NotNull(refused);
    }

    /// <summary>
    /// A button and a checkbox drawn on the window's color from the theme, a click on the button
    /// reported once as Bevy's widget reports it, a click on the checkbox checking it through Bevy's
    /// own observer, a primary button carrying the variant it was made with, and a caption's node
    /// written as soon as it is spawned keeping what was written.
    /// </summary>
    [SkippableFact]
    public void ControlsAreDrawnInTheThemeAndReportWhatIsDoneWithThem()
    {
        Needs.Renderer();

        Entity button = Entity.None, checkbox = Entity.None, primary = Entity.None, caption = Entity.None;
        Val? margin = null;
        var activated = 0;
        bool? checkedAfter = null;
        ButtonVariantRef.ValueVariant? variant = null;

        // One column at the window's corner, a row of Feathers' 24 pixels a control.
        var run = new PictureRun
        {
            Width = 200,
            Height = 120,
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var column = Ui.SpawnNode(new UiSettings
                {
                    Absolute = true,
                    Left = Length.Px(0f),
                    Top = Length.Px(0f),
                    Width = Length.Px(200f),
                    Height = Length.Px(120f),
                    Direction = UiDirection.Column,
                    Align = UiAlign.Start,
                });
                Feathers.ThemeColor(column, FeathersToken.WindowBg);

                button = Feathers.Button(column, "Go");
                ecs.Observe<Activate>(button, _ => activated++);
                checkbox = Feathers.Checkbox(column, "Check");
                Ui.SelfUpdate(checkbox, UiWidgetKind.Checkbox);
                primary = Feathers.Button(column, "Primary", ButtonVariant.Primary);

                // Written as soon as it is spawned, which needs its scene applied by then.
                caption = Feathers.Caption(column, "0");
                ecs.Wrap<NodeRef>(caption).MarginLeft = new Val.Px(10f);
            },
        };

        run.Until("compiled", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Capture("drawn")
            .Do("press the button", _ => SyntheticInput.Press(12f, 12f))
            .Wait(2)
            .Do("release the button", _ => SyntheticInput.Release(12f, 12f))
            .Wait(2)
            .Do("press the checkbox", _ => SyntheticInput.Press(8f, 36f))
            .Wait(2)
            .Do("release the checkbox", _ => SyntheticInput.Release(8f, 36f))
            .Wait(4)
            .Do("read", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                checkedAfter = ecs.Get<CheckedRef>(checkbox) is not null;
                variant = ecs.Get<ButtonVariantRef>(primary)?.Value;
                margin = ecs.Wrap<NodeRef>(caption).MarginLeft;
            })
            .Go();

        var corner = run.Picture("drawn").At(190, 110);
        Assert.True(corner is { R: > 20, G: > 20, B: > 20 } && corner.R < 120, $"the window's color from the theme came out {corner}");
        Assert.Equal(1, activated);
        Assert.True(checkedAfter, "the checkbox was not checked by the click");
        Assert.Equal(ButtonVariantRef.ValueVariant.Primary, variant);
        Assert.Equal(new Val.Px(10f), margin);
    }

    /// <summary>
    /// A number input holds each of its four types exactly as written, a long beyond what a double
    /// holds whole among them, and takes its limits and units.
    /// </summary>
    [SkippableFact]
    public void ANumberInputHoldsEachTypeAsWritten()
    {
        Needs.Renderer();

        var read = new List<NumberInputValue?>();
        var run = new PictureRun
        {
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                var input = Feathers.NumberInput(Entity.None, "X", FeathersToken.TextInputXAxis);
                read.Add(Feathers.NumberOf(input));
                Feathers.SetNumber(input, 1.5f);
                read.Add(Feathers.NumberOf(input));
                Feathers.SetNumber(input, 0.1);
                read.Add(Feathers.NumberOf(input));
                Feathers.SetNumber(input, -7);
                read.Add(Feathers.NumberOf(input));
                Feathers.SetNumber(input, long.MaxValue - 1);
                read.Add(Feathers.NumberOf(input));
                Feathers.SetNumberLimit(input, 0L, 10L, hard: true);
                Feathers.SetNumberUnits(input, NumberUnits.LengthMeters);
            },
        };
        run.Until("started", world => world.Resource<Time>().FrameCount >= 2).Go();

        Assert.Equal(new NumberInputValue(NumberKind.Float, 1.5, 0), read[1]);
        Assert.Equal(new NumberInputValue(NumberKind.Double, 0.1, 0), read[2]);
        Assert.Equal(new NumberInputValue(NumberKind.Int, -7, -7), read[3]);
        Assert.Equal(NumberKind.Long, read[4]!.Value.Kind);
        Assert.Equal(long.MaxValue - 1, read[4]!.Value.Whole);
    }

    /// <summary>
    /// A click on a virtual key is reported on the keyboard with the key's text and where it
    /// stands, and the text is typed into a field through <see cref="Ui.EditText"/>, as Bevy's
    /// example types it.
    /// </summary>
    [SkippableFact]
    public void AVirtualKeyIsReportedWithItsTextAndTypesIntoAField()
    {
        Needs.Renderer();

        VirtualKeyPressed? pressed = null;
        Entity field = Entity.None;
        string? typed = null;

        var run = new PictureRun
        {
            Width = 300,
            Height = 200,
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                // A column, which stretches the keyboard and its rows to its width.
                var corner = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f), Width = Length.Px(120f), Direction = UiDirection.Column });
                var keyboard = Feathers.VirtualKeyboard(corner, [["Q", "W"], ["A"]]);
                field = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(120f), Width = Length.Px(200f) });
                Ui.SetEditableText(field, new UiEditableTextSettings { Text = "ab" });
                ecs.Observe<VirtualKeyPressed>(keyboard, on =>
                {
                    pressed = on.Event;
                    Ui.EditText(field, TextEdit.Insert(on.Event.Key));
                });
            },
        };

        // The second row's one key, which fills the row, below the first's.
        run.Until("compiled", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Do("press", _ => SyntheticInput.Press(60f, 40f))
            .Wait(2)
            .Do("release", _ => SyntheticInput.Release(60f, 40f))
            .Wait(4)
            .Do("read", _ => typed = Ui.EditableTextOf(field))
            .Go();

        Assert.Equal(new VirtualKeyPressed(pressed!.Value.Keyboard, "A", 1, 0), pressed);
        Assert.Equal("abA", typed);
    }

    /// <summary>
    /// Every kind of control has its scene applied as it is spawned, so a margin written on it at
    /// once is the one it keeps, rather than one its scene, applied later, writes over.
    /// </summary>
    [SkippableFact]
    public void EveryKindKeepsWhatIsWrittenAsSoonAsItIsSpawned()
    {
        Needs.Renderer();

        var spawned = new List<(string Kind, Entity Control)>();
        var margins = new List<(string Kind, Val? Margin)>();

        var run = new PictureRun
        {
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                var root = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column });
                var menu = Feathers.Menu(root);
                var popup = Feathers.MenuPopup(menu);
                var pane = Feathers.Pane(root);
                var subpane = Feathers.Subpane(root);
                var group = Feathers.Group(root);
                var container = Feathers.TextInputContainer(root);
                var dialog = Feathers.Dialog(root, 200f);
                var list = Feathers.ListView(root, ["One", "Two"]);
                var header = Feathers.DialogHeader(Feathers.ContentsOf(dialog));
                (string, Func<Entity>)[] kinds =
                [
                    ("button", () => Feathers.Button(root, "Go")),
                    ("tool button", () => Feathers.ToolButton(root, "+")),
                    ("checkbox", () => Feathers.Checkbox(root, "Check")),
                    ("radio", () => Feathers.Radio(root, "Pick")),
                    ("toggle switch", () => Feathers.ToggleSwitch(root)),
                    ("slider", () => Feathers.Slider(root)),
                    ("caption", () => Feathers.Caption(root, "Caption")),
                    ("label", () => Feathers.Label(root, "Label")),
                    ("dim label", () => Feathers.LabelDim(root, "Dim")),
                    ("small label", () => Feathers.LabelSmall(root, "Small")),
                    ("number input", () => Feathers.NumberInput(root, "X")),
                    ("virtual keyboard", () => Feathers.VirtualKeyboard(root, [["A"]])),
                    ("menu", () => menu),
                    ("menu button", () => Feathers.MenuButton(menu, "Menu")),
                    ("menu tool button", () => Feathers.MenuToolButton(Feathers.Menu(root), "M")),
                    ("menu popup", () => popup),
                    ("menu item", () => Feathers.MenuItem(popup, "Item")),
                    ("menu divider", () => Feathers.MenuDivider(popup)),
                    ("select", () => Feathers.Select(root, ["One", "Two"], selected: 1)),
                    ("list view", () => list),
                    ("list row", () => Feathers.ListRow(Feathers.ContentsOf(list), "Three")),
                    ("disclosure toggle", () => Feathers.DisclosureToggle(root)),
                    ("text input container", () => container),
                    ("text input", () => Feathers.TextInput(container, 10f, 9)),
                    ("color swatch", () => Feathers.ColorSwatch(root, 30f)),
                    ("color plane", () => Feathers.ColorPlane(root, ColorPlaneAxes.RedBlue)),
                    ("color slider", () => Feathers.ColorSlider(root, ColorChannel.Red, 0.5f)),
                    ("color input", () => Feathers.ColorInput(root)),
                    ("pane", () => pane),
                    ("pane header", () => Feathers.PaneHeader(pane)),
                    ("pane header divider", () => Feathers.PaneHeaderDivider(Feathers.PaneHeader(pane))),
                    ("pane body", () => Feathers.PaneBody(pane)),
                    ("subpane", () => subpane),
                    ("subpane header", () => Feathers.SubpaneHeader(subpane)),
                    ("subpane body", () => Feathers.SubpaneBody(subpane)),
                    ("group", () => group),
                    ("group header", () => Feathers.GroupHeader(group)),
                    ("group body", () => Feathers.GroupBody(group)),
                    ("flex spacer", () => Feathers.FlexSpacer(root)),
                    ("icon", () => Feathers.Icon(root, FeathersIcon.X)),
                    ("dialog", () => dialog),
                    ("floating dialog", () => Feathers.FloatingDialog(root, "Title", 200f)),
                    ("dialog header", () => header),
                    ("dialog body", () => Feathers.DialogBody(Feathers.ContentsOf(dialog))),
                    ("dialog footer", () => Feathers.DialogFooter(Feathers.ContentsOf(dialog))),
                    ("dialog close", () => Feathers.DialogClose(header)),
                ];
                foreach (var (kind, make) in kinds)
                {
                    var control = make();
                    ecs.Wrap<NodeRef>(control).MarginLeft = new Val.Px(3f);
                    spawned.Add((kind, control));
                }
            },
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("read", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                foreach (var (kind, control) in spawned) margins.Add((kind, ecs.Get<NodeRef>(control)?.MarginLeft));
            })
            .Go();

        Assert.Equal(46, margins.Count);
        Assert.All(margins, read => Assert.True(read.Margin == new Val.Px(3f), $"the {read.Kind} kept {read.Margin}"));
    }

    /// <summary>
    /// What Bevy's props would hold as a scene goes where they would put it, a modal dialog's
    /// contents in its panel, a floating dialog's in its body, and a list's and a select's rows in
    /// the part that scrolls.
    /// </summary>
    [SkippableFact]
    public void ContentsGoWhereBevysPropsWouldPutThem()
    {
        Needs.Renderer();

        bool panel = false, body = false;
        int listRows = 0, selectRows = 0;

        var run = new PictureRun
        {
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                panel = ecs.Get<ModalDialogRef>(Feathers.ContentsOf(Feathers.Dialog(Entity.None))) is not null;
                body = ecs.Get<FeathersDialogBodyRef>(Feathers.ContentsOf(Feathers.FloatingDialog(Entity.None, "Title"))) is not null;

                var rows = ecs.ChildrenOf(Feathers.ContentsOf(Feathers.ListView(Entity.None, ["One", "Two", "Three"], selected: 2)));
                listRows = rows.Length;
                selectRows = ecs.ChildrenOf(Feathers.ContentsOf(Feathers.Select(Entity.None, ["One", "Two"]))).Length;
            },
        };
        run.Until("started", world => world.Resource<Time>().FrameCount >= 1).Go();

        Assert.True(panel, "a dialog's contents do not go in its panel");
        Assert.True(body, "a floating dialog's contents do not go in its body");
        Assert.Equal(3, listRows);
        Assert.Equal(2, selectRows);
    }

    /// <summary>
    /// A press on a color plane reports the point as a <see cref="Vec2"/>, and a dialog's close
    /// button reports <see cref="RequestClose"/> up to the dialog.
    /// </summary>
    [SkippableFact]
    public void APlaneReportsThePointAndACloseButtonAsksTheDialogToClose()
    {
        Needs.Renderer();

        Vec2? point = null;
        Entity dialog = Entity.None, close = Entity.None;
        RequestClose? asked = null;

        var run = new PictureRun
        {
            Width = 400,
            Height = 300,
            Configure = config => config.Feathers = true,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var column = Ui.SpawnNode(new UiSettings
                {
                    Absolute = true,
                    Left = Length.Px(0f),
                    Top = Length.Px(0f),
                    Width = Length.Px(200f),
                    Direction = UiDirection.Column,
                });
                var plane = Feathers.ColorPlane(column, ColorPlaneAxes.RedGreen);
                ecs.Observe<ValueChange<Vec2>>(plane, on => point = on.Event.Value);

                dialog = Feathers.FloatingDialog(Entity.None, "Title", width: 120f, left: 250f, top: 100f);
                ecs.Observe<RequestClose>(dialog, on => asked = on.Event);
            },
        };

        run.Until("compiled", world => world.Resource<Time>().FrameCount >= 3 && Render.PipelinesReady()).Wait(5)
            .Do("press the plane", _ => SyntheticInput.Press(100f, 50f))
            .Wait(2)
            .Do("release the plane", _ => SyntheticInput.Release(100f, 50f))
            .Wait(2)
            .Do("find the close button", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                close = ecs.Descendants(dialog).First(entity => ecs.Get<FeathersDialogCloseRef>(entity) is not null);
            })
            // The close button at the right of the title bar, six pixels in from its edges.
            .Do("press the close button", _ => SyntheticInput.Press(352f, 118f))
            .Wait(2)
            .Do("release the close button", _ => SyntheticInput.Release(352f, 118f))
            .Wait(4)
            .Go();

        Assert.True(point is { } p && MathF.Abs(p.X - 0.5f) < 0.1f && MathF.Abs(p.Y - 0.5f) < 0.1f, $"the plane reported {point}");
        Assert.Equal(close, asked?.Source);
    }

    /// <summary>
    /// A color crosses Bevy's color spaces as Bevy converts it, pure green in HSL, back again through
    /// Okhsl, and out as hex as Bevy writes it.
    /// </summary>
    [Fact]
    public void AColorCrossesBevysSpacesAsBevyConvertsIt()
    {
        var green = Color.From(ColorSpace.Hsl, new Vec4(120f, 1f, 0.5f, 1f));
        Assert.Equal(0f, green.R, 4);
        Assert.Equal(1f, green.G, 4);
        Assert.Equal(0f, green.B, 4);

        var amber = Color.FromSrgb(0.57254905f, 0.2509804f, 0.05490196f);
        var back = Color.From(ColorSpace.Okhsl, amber.In(ColorSpace.Okhsl));
        Assert.Equal(amber.R, back.R, 3);
        Assert.Equal(amber.G, back.G, 3);
        Assert.Equal(amber.B, back.B, 3);

        Assert.Equal("#FF8000", Color.FromSrgb8(255, 128, 0).ToHex());
        Assert.Equal("#FF800080", Color.FromSrgb8(255, 128, 0, 128).ToHex());
    }
}

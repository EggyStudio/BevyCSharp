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
        Assert.Equal(32, Marshal.SizeOf<NativeFeathersProps>());
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
}

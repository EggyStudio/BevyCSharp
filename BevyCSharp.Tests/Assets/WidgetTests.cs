using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's widgets, put on nodes through their wrappers, and what they report.</summary>
/// <remarks>
/// A widget changes as the pointer works it, through Bevy's picking, which an offscreen run points
/// at through the image it draws into, so a widget is clicked here as a hand would click it.
/// </remarks>
[Collection("engine")]
public sealed class WidgetTests
{
    /// <summary>Each field of the report sits where the bridge writes it.</summary>
    [Theory]
    [InlineData(nameof(NativeWidgetEvent.Entity), 0)]
    [InlineData(nameof(NativeWidgetEvent.Other), 8)]
    [InlineData(nameof(NativeWidgetEvent.Kind), 16)]
    [InlineData(nameof(NativeWidgetEvent.Value), 20)]
    [InlineData(nameof(NativeWidgetEvent.Flag), 24)]
    [InlineData(nameof(NativeWidgetEvent.IsFinal), 28)]
    [InlineData(nameof(NativeWidgetEvent.Action), 32)]
    [InlineData(nameof(NativeWidgetEvent.Navigation), 36)]
    public void EveryFieldOfTheReportSitsWhereTheBridgePutsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeWidgetEvent>(field).ToInt32());

    /// <summary>The report is the size the bridge writes.</summary>
    [Fact]
    public void TheReportIsTheSizeTheBridgeWrites() => Assert.Equal(40, Marshal.SizeOf<NativeWidgetEvent>());

    /// <summary>
    /// A button, a checkbox, a slider, a radio group and a menu button, each clicked, report what
    /// they were asked as Bevy's <c>Activate</c>, <c>ValueChange</c> and <c>MenuEvent</c>, observed
    /// at the widget, and the menu's at the owner it goes up to.
    /// </summary>
    [SkippableFact]
    public void ClickedWidgetsReportWhatTheyWereAsked()
    {
        Needs.Renderer();

        var activated = new List<Entity>();
        var flags = new List<ValueChange<bool>>();
        var numbers = new List<ValueChange<float>>();
        var choices = new List<ValueChange<Entity>>();
        var menus = new List<(Entity At, MenuEvent Event)>();
        Entity button = default, checkbox = default, slider = default, group = default, second = default, menuButton = default, owner = default;
        var frame = 0;

        using var app = new App(Config.OffscreenFor(400, 200, frames: 60));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            Entity At(float left, float top, float width, float height, UiDirection direction = UiDirection.Row) => Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Direction = direction,
                Left = Length.Px(left),
                Top = Length.Px(top),
                Width = Length.Px(width),
                Height = Length.Px(height),
            });

            button = At(0f, 0f, 60f, 40f);
            ecs.Insert<ButtonRef>(button);
            ecs.Observe<Activate>(button, on => activated.Add(on.Event.Entity));

            checkbox = At(100f, 0f, 40f, 40f);
            ecs.Insert<CheckboxRef>(checkbox);
            ecs.Observe<ValueChange<bool>>(checkbox, on => flags.Add(on.Event));

            // A click on its track snaps it to where the click was, three quarters along.
            slider = At(200f, 0f, 100f, 20f);
            ecs.Insert<SliderRef>(slider).TrackClick = SliderRef.TrackClickVariant.Snap;
            var range = ecs.Insert<SliderRangeRef>(slider);
            (range.Start, range.End) = (0f, 100f);
            ecs.Insert<SliderValueRef>(slider).Value = 0f;
            ecs.Observe<ValueChange<float>>(slider, on => numbers.Add(on.Event));

            // Its buttons one over the other, the second from 140 to 180 down.
            group = At(0f, 100f, 100f, 80f, UiDirection.Column);
            ecs.Insert<RadioGroupRef>(group);
            var first = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(40f) });
            second = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(40f) });
            foreach (var radio in new[] { first, second })
            {
                ecs.Insert<RadioButtonRef>(radio);
                ecs.SetParent(radio, group);
            }
            ecs.Observe<ValueChange<Entity>>(group, on => choices.Add(on.Event));

            owner = At(200f, 100f, 100f, 40f);
            menuButton = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(40f) });
            ecs.Insert<ButtonRef>(menuButton);
            ecs.Insert<MenuButtonRef>(menuButton);
            ecs.SetParent(menuButton, owner);
            ecs.Observe<MenuEvent>(owner, on => menus.Add((on.Entity, on.Event)));
        }, "Test.Setup"));

        // Each widget pointed at, pressed and let go over a few frames, since what the pointer is
        // over is found a frame after it moves.
        (float X, float Y)[] targets = [(30f, 20f), (120f, 20f), (275f, 10f), (50f, 160f), (250f, 120f)];
        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            var step = frame - 10;
            if (step < 0 || step >= targets.Length * 6) return;

            var (x, y) = targets[step / 6];
            switch (step % 6)
            {
                case 0: SyntheticInput.MoveTo(x, y); break;
                case 2: SyntheticInput.Press(x, y); break;
                case 4: SyntheticInput.Release(x, y); break;
            }
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        Assert.Equal([button], activated);

        var flag = Assert.Single(flags);
        Assert.Equal(checkbox, flag.Source);
        Assert.True(flag.Value);

        var number = Assert.Single(numbers);
        Assert.Equal(slider, number.Source);
        Assert.InRange(number.Value, 70f, 80f);

        var choice = Assert.Single(choices);
        Assert.Equal(group, choice.Source);
        Assert.Equal(second, choice.Value);

        // Asked of the menu's button and heard at its owner, as it went up.
        var (at, menu) = Assert.Single(menus);
        Assert.Equal(owner, at);
        Assert.Equal(menuButton, menu.Source);
        Assert.Equal(MenuAction.Toggle, menu.Action);
    }

    /// <summary>
    /// A tab list reports the tab clicked as Bevy's <c>ValueChange</c> of an optional entity, and
    /// made to keep its own state, its <c>SelectedTab</c> follows the click.
    /// </summary>
    [SkippableFact]
    public void ATabListReportsTheTabClickedAndKeepsIt()
    {
        Needs.Renderer();

        var changes = new List<ValueChange<Entity?>>();
        Entity list = default, third = default;
        Entity? selected = null;
        var frame = 0;

        using var app = new App(Config.OffscreenFor(400, 100, frames: 40));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            // Three tabs in a row, each a hundred pixels wide, the third from 200 to 300 across.
            list = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f) });
            ecs.Insert<TabListRef>(list);
            for (var i = 0; i < 3; i++)
            {
                var tab = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(40f) });
                ecs.Insert<TabRef>(tab);
                ecs.SetParent(tab, list);
                third = tab;
            }

            Ui.SelfUpdate(list, UiWidgetKind.TabList);
            ecs.Observe<ValueChange<Entity?>>(list, on => changes.Add(on.Event));
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            frame++;
            if (frame == 10) SyntheticInput.MoveTo(250f, 20f);
            if (frame == 12) SyntheticInput.Press(250f, 20f);
            if (frame == 14) SyntheticInput.Release(250f, 20f);
            if (frame == 30) selected = world.Resource<EcsWorld>().Get<SelectedTabRef>(list)?.Value;
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        var change = Assert.Single(changes);
        Assert.Equal(list, change.Source);
        Assert.Equal(third, change.Value);
        Assert.Equal(third, selected);
    }

    /// <summary>
    /// A slider, a checkbox and a radio group are made from their wrappers and made to keep their
    /// own state, the slider's value is written and read through its wrapper, and an entity that is
    /// gone is refused.
    /// </summary>
    [SkippableFact]
    public void WidgetsAreBuiltSetAndMadeToKeepTheirOwnState()
    {
        Needs.Renderer();

        float? value = null;
        bool checkedRead = false;
        BevyNativeException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();

                var slider = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Height = Length.Px(12f) });
                ecs.Insert<SliderRef>(slider);
                var range = ecs.Insert<SliderRangeRef>(slider);
                (range.Start, range.End) = (0f, 10f);
                ecs.Insert<SliderValueRef>(slider).Value = 7.5f;
                Ui.SelfUpdate(slider, UiWidgetKind.Slider);
                value = ecs.Get<SliderValueRef>(slider)?.Value;

                var checkbox = Ui.SpawnNode(new UiSettings());
                ecs.Insert<CheckboxRef>(checkbox);
                ecs.Insert<CheckedRef>(checkbox);
                Ui.SelfUpdate(checkbox, UiWidgetKind.Checkbox);
                checkedRead = ecs.Get<CheckedRef>(checkbox) is not null;

                var group = Ui.SpawnNode(new UiSettings());
                ecs.Insert<RadioGroupRef>(group);
                Ui.SelfUpdate(group, UiWidgetKind.RadioGroup);

                var gone = ecs.Spawn();
                ecs.Despawn(gone);
                refused = Assert.Throws<BevyNativeException>(() => Ui.SelfUpdate(gone, UiWidgetKind.Slider));
            },
        };

        run.Wait(3).Go();

        Assert.Equal(7.5f, value);
        Assert.True(checkedRead);
        Assert.Equal(NativeStatus.NoEntity, refused!.Status);
    }
}

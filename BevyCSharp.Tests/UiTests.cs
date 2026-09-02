using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers building a UI: nodes, text, and changing what the text says.
/// </summary>
/// <remarks>
/// Whether anything appears on screen needs a window and an eye. What is checked here is that the
/// entities and components exist, that text can be rewritten in place, and that a build without a
/// renderer refuses rather than handing back an entity that would never draw.
/// </remarks>
[Collection("engine")]
public sealed class UiTests
{
    [Fact]
    public void ANodeAndSomeTextAreOrdinaryEntities()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var panel = Entity.None;
        var label = Entity.None;
        var alive = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            panel = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Left = Length.Px(16f),
                Top = Length.Px(16f),
                Width = Length.Percent(30f),
                Height = Length.Px(64f),
                Padding = Length.Px(8f),
                Color = (0f, 0f, 0f, 0.5f),
            });

            label = Ui.SpawnText("Score: 0", new UiSettings { Color = (1f, 1f, 1f, 1f) }, 24f);

            // Nesting is what lays a screen out, and it is the ECS parenting that already exists.
            ctx.Ecs.SetParent(label, panel);
        });

        harness.OnContext(Stage.Last, ctx =>
            alive = ctx.Ecs.IsAlive(panel)
                && ctx.Ecs.IsAlive(label)
                && ctx.Ecs.ParentOf(label) == panel);

        harness.Run();

        Assert.NotEqual(Entity.None, panel);
        Assert.NotEqual(Entity.None, label);
        Assert.NotEqual(panel, label);
        Assert.True(alive, "the nodes did not survive as parented entities");
    }

    [Fact]
    public void TextIsRewrittenInPlace()
    {
        // A score changes every frame; the entity behind it should not, or everything holding a
        // reference to it would have to be told.
        using var harness = new EngineHarness(frames: 4);
        if (!App.HasRenderer) return;

        var label = Entity.None;
        var sameEntity = false;

        harness.OnContext(Stage.Startup, _ =>
            label = Ui.SpawnText("0", new UiSettings(), 18f));

        harness.OnContext(Stage.Update, ctx =>
        {
            Ui.SetText(label, $"frame {ctx.Time.FrameCount}");
            sameEntity = ctx.Ecs.IsAlive(label);
        });

        harness.Run();

        Assert.True(sameEntity);
    }

    [Fact]
    public void SettingTextOnSomethingThatIsNotTextIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var plain = ctx.Ecs.Spawn();

            var ex = Assert.Throws<BevyNativeException>(() => Ui.SetText(plain, "nope"));
            Assert.Equal(NativeStatus.NotPresent, ex.Status);

            var gone = Assert.Throws<BevyNativeException>(() => Ui.SetText(Entity.None, "nope"));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ALaidOutNodeTakesEveryFieldTheLayoutHas()
    {
        // Whether the screen looks right needs a window and an eye; the sample is where that is
        // confirmed. What is checked here is that a fully described node is accepted and comes
        // back as an ordinary parented entity, which is what would break silently.
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var menu = Entity.None;
        var children = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            menu = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Left = Length.Percent(30f),
                Top = Length.Percent(25f),
                Width = Length.Px(280f),
                Direction = UiDirection.Column,
                Justify = UiJustify.SpaceBetween,
                Align = UiAlign.Center,
                RowGap = Length.Px(12f),
                ColumnGap = Length.Px(4f),
                Padding = Length.Px(16f),
                Margin = Length.Px(8f),
                Border = Length.Px(2f),
                Color = (0f, 0f, 0f, 0.6f),
                BorderColor = (0.4f, 0.7f, 1f, 1f),
            });

            foreach (var caption in new[] { "Play", "Options", "Quit" })
            {
                var row = Ui.SpawnText(caption, new UiSettings
                {
                    Interactive = true,
                    Padding = Length.Px(6f),
                    Color = (1f, 1f, 1f, 1f),
                }, 20f);

                ctx.Ecs.SetParent(row, menu);
            }
        });

        harness.OnContext(Stage.Last, ctx => children = ctx.Ecs.ChildrenOf(menu).Length);

        harness.Run();

        Assert.NotEqual(Entity.None, menu);
        Assert.Equal(3, children);
    }

    [Fact]
    public void AnUnknownLayoutCodeFallsBackRatherThanFailing()
    {
        // The managed side names the enums, so a bridge older than the assembly calling it can be
        // handed a code it has never heard of. A plainly laid out screen beats no screen.
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var node = Ui.SpawnNode(new UiSettings
            {
                Direction = (UiDirection)99,
                Justify = (UiJustify)99,
                Align = (UiAlign)99,
            });

            Assert.True(ctx.Ecs.IsAlive(node));
        });

        harness.Run();
    }

    [Fact]
    public void AnInteractiveNodeCarriesTheComponentThePointerUpdates()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var button = Entity.None;
        var plain = Entity.None;
        var carried = false;
        var plainCarried = true;
        var state = UiInteraction.Pressed;

        harness.OnContext(Stage.Startup, ctx =>
        {
            button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Width = Length.Px(120f),
                Height = Length.Px(40f),
            });

            plain = Ui.SpawnNode(new UiSettings { Width = Length.Px(120f) });

            carried = ctx.Ecs.HasById(button, NativeComponents.Interaction);
            plainCarried = ctx.Ecs.HasById(plain, NativeComponents.Interaction);
        });

        // Nothing moves a pointer here, and the system that would notice comes with the window,
        // so None is the whole range this can report. What is being checked is that the node is
        // set up to be asked at all.
        harness.OnContext(Stage.Last, _ => state = Ui.InteractionOf(button));

        harness.Run();

        Assert.True(carried, "an interactive node did not get Bevy's Interaction");
        Assert.False(plainCarried, "a plain node was given Interaction it never asked for");
        Assert.Equal(UiInteraction.None, state);
    }

    [Fact]
    public void AskingANodeThatDoesNotReactIsRefused()
    {
        // A button that quietly never fires is the harder mistake to find, so "it was never set
        // up to notice" is a different answer from "nothing is touching it".
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var plain = Ui.SpawnNode(new UiSettings());

            var ex = Assert.Throws<BevyNativeException>(() => Ui.InteractionOf(plain));
            Assert.Equal(NativeStatus.NotPresent, ex.Status);

            var gone = Assert.Throws<BevyNativeException>(() => Ui.InteractionOf(Entity.None));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);

            var bare = ctx.Ecs.Spawn();
            var notANode = Assert.Throws<BevyNativeException>(() => Ui.InteractionOf(bare));
            Assert.Equal(NativeStatus.NotPresent, notANode.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ABuildWithoutARendererSaysSoRatherThanReturningNothing()
    {
        using var harness = new EngineHarness(frames: 3);
        if (App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(() => Ui.SpawnNode(new UiSettings()));
            Assert.Equal(NativeStatus.Unsupported, ex.Status);
            Assert.Contains("--render", ex.Message);

            var interaction = Assert.Throws<BevyNativeException>(
                () => Ui.InteractionOf(Entity.None));
            Assert.Equal(NativeStatus.Unsupported, interaction.Status);
        });

        harness.Run();
    }

    [Fact]
    public void ALengthCarriesItsUnit()
    {
        // No engine needed: the point is that a bare number cannot say what it means.
        Assert.Equal(LengthUnit.Auto, Length.Auto.Unit);
        Assert.Equal(LengthUnit.Px, Length.Px(12f).Unit);
        Assert.Equal(LengthUnit.Percent, Length.Percent(50f).Unit);

        Assert.Equal("auto", Length.Auto.ToString());
        Assert.Equal("12px", Length.Px(12f).ToString());
        Assert.Equal("50%", Length.Percent(50f).ToString());
    }
}

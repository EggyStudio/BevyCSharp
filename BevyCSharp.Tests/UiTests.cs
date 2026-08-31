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
    public void ABuildWithoutARendererSaysSoRatherThanReturningNothing()
    {
        using var harness = new EngineHarness(frames: 3);
        if (App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(() => Ui.SpawnNode(new UiSettings()));
            Assert.Equal(NativeStatus.Unsupported, ex.Status);
            Assert.Contains("--render", ex.Message);
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

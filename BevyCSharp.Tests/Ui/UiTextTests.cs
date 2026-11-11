using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the interface's text, its color, rewriting what it says, how it breaks to a width, the
/// font it is set in, the lines drawn under and through it, and its font's features and axes.
/// </summary>
[Collection("engine")]
public sealed class UiTextTests
{
    [SkippableFact]
    public void TextGivenNoColorIsWhiteAndTextGivenOneKeepsIt()
    {
        // A node's color starts transparent, so a plain node draws nothing, and text that took
        // the same would be text nobody could see.
        using var harness = new EngineHarness(frames: 3);
        Needs.Renderer();

        Color? plain = null, red = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var white = Ui.SpawnText("plain", new UiSettings());
            var tinted = Ui.SpawnText("red", new UiSettings { Color = (1f, 0f, 0f, 1f) });
            plain = ctx.Ecs.GetReflectedColor(white, "bevy_text::text::TextColor", ".0");
            red = ctx.Ecs.GetReflectedColor(tinted, "bevy_text::text::TextColor", ".0");
        });

        harness.Run();

        Assert.Equal(new Color(1f, 1f, 1f, 1f), plain);
        Assert.Equal(new Color(1f, 0f, 0f, 1f), red);
    }

    [SkippableFact]
    public void TextIsRewrittenInPlace()
    {
        // A score changes every frame; the entity behind it should not, or everything holding a
        // reference to it would have to be told.
        using var harness = new EngineHarness(frames: 4);
        Needs.Renderer();

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

    [SkippableFact]
    public void SettingTextOnSomethingThatIsNotTextIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        Needs.Renderer();

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

    [SkippableFact]
    public void ALongStringIsBrokenToTheWidthItIsGiven()
    {
        // Whether the lines land where they should needs an eye; the sample is where that is
        // confirmed. What is checked here is that a text node takes every way of being set and
        // stays an ordinary entity, and that rewriting it in place still works afterwards.
        using var harness = new EngineHarness(frames: 4);
        Needs.Renderer();

        const string Paragraph =
            "A sentence long enough that it cannot sit on one line inside a narrow panel.";

        var wrapped = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var panel = Ui.SpawnNode(new UiSettings { Width = Length.Px(180f) });

            wrapped = Ui.SpawnText(Paragraph, new UiSettings(), new UiTextSettings
            {
                FontSize = 14f,
                Justify = TextJustify.Center,
                Wrap = TextWrap.WordBoundary,
            });
            ctx.Ecs.SetParent(wrapped, panel);

            // The other three ways of breaking, and a run that refuses to wrap at all.
            foreach (var wrap in new[] { TextWrap.AnyCharacter, TextWrap.WordOrCharacter, TextWrap.NoWrap })
            {
                var line = Ui.SpawnText(
                    Paragraph, new UiSettings(), new UiTextSettings { Wrap = wrap });
                ctx.Ecs.SetParent(line, panel);
            }

            foreach (var justify in new[]
                     {
                         TextJustify.Left, TextJustify.Right, TextJustify.Justified,
                         TextJustify.Start, TextJustify.End,
                     })
            {
                Ui.SpawnText(Paragraph, new UiSettings(), new UiTextSettings { Justify = justify });
            }

            // The size-only overload is the same call underneath.
            var label = Ui.SpawnText("plain", new UiSettings(), 18f);
            Assert.True(ctx.Ecs.IsAlive(label));
        });

        harness.OnContext(Stage.Update, _ => Ui.SetText(wrapped, "shorter"));

        harness.Run();

        Assert.NotEqual(Entity.None, wrapped);
    }

    [SkippableFact]
    [ExpectsError("bevy", "missing.ttf")]
    public void TextIsSetInBevysOwnFontUntilAnotherIsNamed()
    {
        using var harness = new EngineHarness(frames: 30, fps: 240);
        Needs.Renderer();

        var font = AssetHandle.None;
        var state = AssetLoadState.Loading;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // No font named, which is the font Bevy carries and needs no asset.
            var run = Ui.SpawnText(
                "words", new UiSettings { Color = (1f, 1f, 1f, 1f) }, new UiTextSettings());

            Assert.True(ctx.Ecs.IsAlive(run));

            // A font is loaded like any other asset, and a windowless run gets that far, because
            // the asset is a file and only drawing with it needs a window.
            font = AssetServer.Load(AssetKind.Font, "fonts/missing.ttf");
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            state = font.State;
            if (state != AssetLoadState.Loading) ctx.Exit();
        });

        harness.Run();

        Assert.True(font.IsValid, "the font kind was not accepted");
        Assert.Equal(AssetLoadState.Failed, state);
    }

    /// <summary>
    /// A line under text and a line through it are drawn where they are put on, and gone where
    /// they are taken off.
    /// </summary>
    [SkippableFact]
    public void LinesUnderAndThroughTextAreDrawnAndTakenOff()
    {
        Needs.Renderer();

        var text = Entity.None;
        var run = new PictureRun
        {
            Width = 320,
            Height = 96,
            Scene = _ =>
            {
                Render2d.SpawnCamera2d();
                text = Ui.SpawnText("MMMMMM", new UiSettings { Absolute = true, Left = Length.Px(10f), Top = Length.Px(10f) }, 40f);
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("plain")
            .Do("lining it", _ =>
            {
                Ui.SetUnderline(text);
                Ui.SetStrikethrough(text);
            })
            .Wait(ShaderMaterialTests.Settled)
            .Capture("lined")
            .Do("taking the lines off", _ =>
            {
                Ui.SetUnderline(text, false);
                Ui.SetStrikethrough(text, false);
            })
            .Wait(ShaderMaterialTests.Settled)
            .Capture("taken off")
            .Go();

        int Light(string name)
        {
            var picture = run.Picture(name);
            var count = 0;
            for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (r > 180 && g > 180 && b > 180) count++;
            }
            return count;
        }

        var (plain, lined, takenOff) = (Light("plain"), Light("lined"), Light("taken off"));
        Assert.True(plain > 200, $"the text drew {plain} light pixels");
        Assert.True(lined > plain + 100, $"the lines added {lined - plain} light pixels to the text's {plain}");
        Assert.InRange(takenOff, plain - plain / 20, plain + plain / 20);
    }

    /// <summary>
    /// A run's OpenType features and its variable axes reach its <c>TextFont</c>, each replacing what
    /// it had, and a tag that is not four letters or a run that is gone is refused.
    /// </summary>
    [SkippableFact]
    public void FontFeaturesAndVariationsReachTheTextsFont()
    {
        Needs.Renderer();

        string? features = null, variations = null, replaced = null;
        ArgumentException? badTag = null;
        BevyNativeException? gone = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var text = Ui.SpawnText("fi 1/2", new UiSettings());
                Ui.SetFontFeatures(text, ("liga", 0u), ("frac", 1u));
                Ui.SetFontVariations(text, ("wght", 650f));
                features = ecs.GetReflected(text, TextFontRef.TypePath, ".font_features");
                variations = ecs.GetReflected(text, TextFontRef.TypePath, ".font_variations");

                Ui.SetFontVariations(text, ("wdth", 75f));
                replaced = ecs.GetReflected(text, TextFontRef.TypePath, ".font_variations");

                badTag = Assert.Throws<ArgumentException>(() => Ui.SetFontFeatures(text, ("ligatures", 1u)));
                var dead = ecs.Spawn();
                ecs.Despawn(dead);
                gone = Assert.Throws<BevyNativeException>(() => Ui.SetFontVariations(dead, ("wght", 400f)));
            },
        };
        run.Wait(2).Go();

        // Each tag as its four bytes, liga, frac, wght and wdth, beside its value.
        Assert.Equal("{\"features\":[[[108,105,103,97],0],[[102,114,97,99],1]]}", features);
        Assert.Equal("{\"variations\":[[[119,103,104,116],650.0]]}", variations);
        Assert.Equal("{\"variations\":[[[119,100,116,104],75.0]]}", replaced);
        Assert.NotNull(badTag);
        Assert.Equal(NativeStatus.NoEntity, gone!.Status);
    }

}

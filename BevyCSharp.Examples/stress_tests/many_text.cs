// Bevy's many_text example, examples/stress_tests/many_text.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.StressTests;

// Interface text in four fonts, every way of justifying and breaking it. --animate has its
// paragraphs swapped and its digits counted on every frame, so it is laid out again every frame,
// --set-font-changed every font written again each frame, and --respawn the whole tree spawned
// again.
internal static class ManyText
{
    // Bevy's --animate, which runs the systems that change the text each frame.
    internal static bool Animate;

    internal const string LoremText1 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";
    internal const string LoremText2 = "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

    // Bevy's palette of CSS colors, and the white text is drawn in where none is given.
    private static readonly (float R, float G, float B, float A) White = (1f, 1f, 1f, 1f), Yellow = Color.FromSrgb8(255, 255, 0), Navy = Color.FromSrgb8(0, 0, 128),
        PaleGreen = Color.FromSrgb8(152, 251, 152), MistyRose = Color.FromSrgb8(255, 228, 225), Maroon = Color.FromSrgb8(128, 0, 0);

    private static readonly string[] Fonts =
    [
        "fonts/EBGaramond12-Regular.otf",
        "fonts/FiraMono-Medium.ttf",
        "fonts/FiraSans-Bold.ttf",
        "fonts/MonaSans-VariableFont.ttf",
    ];

    // Bevy's ManyTextRoot, the grid every text hangs from, and each text, whose font
    // --set-font-changed writes again.
    private static Entity _root;
    private static readonly List<Entity> Texts = [];

    // Bevy's window for its stress tests, 1920 by 1080 at a scale factor of one with no vertical
    // sync, drawn as fast as it can, and its frame times logged once a second by Bevy's plugins.
    public static void Configure(Config config)
    {
        (config.Width, config.Height, config.ScaleFactor) = (1920, 1080, 1f);
        config.Vsync = false;
        config.HeadlessFps = 0;
        config.LogFrameTimes = true;
    }

    public static void Build(App app)
    {
        var arguments = Environment.GetCommandLineArgs();
        Animate = arguments.Contains("--animate");
        Texts.Clear();
        NumberSpan.Count = 0;

        app.Startup(_ =>
        {
            StressTest.Warn();
            Render2d.SpawnCamera2d();
        }, "many_text.SetupCamera");
        app.Startup(SetupText, "many_text.SetupText");

        if (arguments.Contains("--set-font-changed")) app.Update(SetFontChanged, "many_text.SetFontChanged");
        if (arguments.Contains("--respawn")) app.Update(ctx => { DespawnLayout(ctx); SetupText(ctx); }, "many_text.Respawn");
    }

    private static void SetupText(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Texts.Clear();
        _root = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Width = Length.Percent(100f), Height = Length.Percent(100f), ColumnGap = Length.Px(2f) });
        UiGrid.Set(_root, new GridSettings { Columns = [Track.Flex(1f).Repeated(4)] });

        foreach (var path in Fonts)
        {
            var font = AssetServer.Load(AssetKind.Font, path);
            var column = Ui.SpawnNode(new UiSettings
            {
                Direction = UiDirection.Column,
                Border = Sides.All(Length.Px(1f)),
                Padding = Sides.All(Length.Px(1f)),
                RowGap = Length.Px(2f),
                BorderColor = (1f, 1f, 1f, 1f),
            });
            ecs.SetParent(column, _root);

            Text(ecs, column, path, new UiTextSettings { Font = font, FontSize = 10f });
            foreach (var justify in new[] { TextJustify.Left, TextJustify.Center, TextJustify.Right, TextJustify.Justified })
            {
                foreach (var wrap in new[] { TextWrap.AnyCharacter, TextWrap.WordBoundary })
                {
                    var linebreak = wrap == TextWrap.AnyCharacter ? "AnyCharacter" : "WordBoundary";
                    Text(ecs, column, $"Justify::{justify}, LineBreak::{linebreak}", new UiTextSettings { Font = font, FontSize = 10f }, Yellow);
                    ecs.Add(Text(ecs, column, LoremText1, new UiTextSettings { Font = font, FontSize = 10f, Justify = justify, Wrap = wrap }, Navy), new Lorem { Second = false });
                    ecs.Add(Text(ecs, column, LoremText2, new UiTextSettings { Font = font, FontSize = 11f, Justify = justify, Wrap = wrap }, PaleGreen), new Lorem { Second = true });
                }
            }

            var paragraph = Text(ecs, column, LoremText1, new UiTextSettings { Font = font, FontSize = 12f }, MistyRose);
            ecs.Add(paragraph, new Lorem { Second = false });
            Ui.SpawnTextSpan(paragraph, " ", new UiTextSettings { Font = font, FontSize = 13f }, White);
            var span = Ui.SpawnTextSpan(paragraph, LoremText2, new UiTextSettings { Font = font, FontSize = 13f }, Maroon);
            ecs.Add(span, new Lorem { Second = true });

            // In Bevy's default font at its default size, which only its spans set.
            var numbers = Text(ecs, column, "", new UiTextSettings { Wrap = TextWrap.AnyCharacter });
            for (var i = 0; i < 100; i++)
            {
                var digit = i % 10;
                var number = Ui.SpawnTextSpan(numbers, digit.ToString(), new UiTextSettings { Font = font, FontSize = 6f + digit }, White);
                ecs.Add(number, new NumberSpan());
            }
        }
    }

    private static Entity Text(EcsWorld ecs, Entity parent, string text, UiTextSettings style, (float R, float G, float B, float A)? color = null)
    {
        var settings = color is { } tint ? new UiSettings { Color = tint } : new UiSettings();
        var entity = Ui.SpawnText(text, settings, style);
        ecs.SetParent(entity, parent);
        Texts.Add(entity);
        return entity;
    }

    // Every text's font written again as it is, which Bevy does by reaching it mutably.
    private static void SetFontChanged(BehaviorContext ctx)
    {
        foreach (var text in Texts)
        {
            var font = ctx.Ecs.Wrap<TextFontRef>(text);
            font.FontSize = font.FontSize;
        }
    }

    private static void DespawnLayout(BehaviorContext ctx) => ctx.Ecs.Despawn(_root);
}

/// <summary>A paragraph that swaps between the two every frame.</summary>
[Behavior]
public partial struct Lorem
{
    /// <summary>Whether it shows the second paragraph and is swapped to the first next.</summary>
    public bool Second;

    /// <summary>
    /// Swapped to the other paragraph. Bevy's query asks for interface text, which a span is not,
    /// so a span carrying one keeps its paragraph.
    /// </summary>
    [OnUpdate]
    public void UpdateLoremText(BehaviorContext ctx)
    {
        if (!ManyText.Animate || ctx.Ecs.Get<TextRef>(ctx.Entity) is null) return;

        Ui.SetText(ctx.Entity, Second ? ManyText.LoremText1 : ManyText.LoremText2);
        Second = !Second;
    }
}

/// <summary>A digit of the counting text, moved on each frame by a count that rises each frame.</summary>
[Behavior]
public partial struct NumberSpan
{
    /// <summary>What each digit is moved on by this frame, Bevy's Local of the system.</summary>
    public static uint Count;

    /// <summary>Every digit moved on by the count, and the count raised.</summary>
    [OnUpdate]
    public static void UpdateNumberText(BehaviorContext ctx)
    {
        if (!ManyText.Animate) return;

        foreach (var row in ctx.Ecs.Query<NumberSpan>(markChanged: false))
        {
            var span = ctx.Ecs.Wrap<TextSpanRef>(row.Entity);
            span.Value = ((uint.Parse(span.Value, System.Globalization.CultureInfo.InvariantCulture) + Count) % 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        Count = (Count + 1) % 10;
    }
}

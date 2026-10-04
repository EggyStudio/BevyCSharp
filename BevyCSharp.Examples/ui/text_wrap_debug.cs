using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates text wrapping, the same five lines in columns too narrow for them, each row wrapping
// a different way and each column spreading its lines a different way.
internal static class TextWrapDebug
{
    private const string TextLayout = "bevy_text::text::TextLayout";
    private const string Background = "bevy_ui::ui_node::BackgroundColor";

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var style = new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 12f };

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Direction = UiDirection.Column, Color = (0f, 0f, 0f, 1f) });
        foreach (var linebreak in new[] { "AnyCharacter", "WordBoundary", "WordOrCharacter", "NoWrap" })
        {
            var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Justify = UiJustify.SpaceAround, Align = UiAlign.Center, Width = Length.Percent(100f), Height = Length.Percent(50f) });
            ecs.SetParent(row, root);

            var justifications = new[] { UiJustify.Center, UiJustify.FlexStart, UiJustify.FlexEnd, UiJustify.SpaceAround, UiJustify.SpaceBetween, UiJustify.SpaceEvenly };
            for (var i = 0; i < justifications.Length; i++)
            {
                var c = 0.3f + i * 0.1f;
                var column = Ui.SpawnNode(new UiSettings
                {
                    Justify = justifications[i],
                    Direction = UiDirection.Column,
                    Width = Length.Percent(16f),
                    Height = Length.Percent(95f),
                    OverflowX = UiOverflow.Clip,
                    Color = Scene.Srgb(0.5f, c, 1f - c),
                });
                ecs.SetParent(column, row);

                string[] messages =
                [
                    $"JustifyContent::{justifications[i]}",
                    $"LineBreakOn::{linebreak}",
                    "Line 1\nLine 2",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas auctor, nunc ac faucibus fringilla.",
                    "pneumonoultramicroscopicsilicovolcanoconiosis",
                ];
                for (var j = 0; j < messages.Length; j++)
                {
                    var text = Ui.SpawnText(messages[j], new UiSettings(), style);
                    ecs.SetVariant(text, TextLayout, ".linebreak", linebreak);
                    ecs.InsertReflected(text, Background);
                    ecs.SetReflectedColor(text, Background, ".0", Color.FromSrgb(0.8f - j * 0.2f, 0f, 0f));
                    ecs.SetParent(text, column);
                }
            }
        }
    }, "text_wrap_debug.Setup");
}

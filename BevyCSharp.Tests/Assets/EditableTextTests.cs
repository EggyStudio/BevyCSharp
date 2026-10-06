using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's editable text, a field the player types into.</summary>
[Collection("engine")]
public sealed class EditableTextTests
{
    /// <summary>
    /// A field holds the text it was made with, takes the focus Bevy's <c>AutoFocus</c> gives it,
    /// and is replaced and cleared from C#.
    /// </summary>
    [SkippableFact]
    public void AFieldHoldsItsTextAndIsReplacedAndCleared()
    {
        Needs.Renderer();

        var field = Entity.None;
        Entity? focused = null;
        string? first = null, replaced = null, cleared = null;

        var run = new PictureRun
        {
            Width = 320,
            Height = 96,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                field = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                Ui.SetEditableText(field, new UiEditableTextSettings { Text = "a1", MaxCharacters = 4, Allowed = "0123456789abcdef" });
                ecs.Insert<AutoFocusRef>(field);
            },
        };

        run.Wait(3)
            .Do("reading it", world =>
            {
                first = Ui.EditableTextOf(field);
                focused = world.Resource<EcsWorld>().Resource<InputFocusRef>()?.CurrentFocus;
            })
            .Do("replacing it", _ => Ui.SetEditableValue(field, "c0de"))
            .Wait(2)
            .Do("reading it again", _ => replaced = Ui.EditableTextOf(field))
            .Do("clearing it", _ => Ui.SetEditableValue(field, string.Empty))
            .Wait(2)
            .Do("reading it once more", _ => cleared = Ui.EditableTextOf(field))
            .Go();

        Assert.Equal("a1", first);
        Assert.Equal(field, focused);
        Assert.Equal("c0de", replaced);
        Assert.Equal(string.Empty, cleared);
    }

    /// <summary>
    /// A field focused from C# in a run with no window takes the keys typed, Backspace and Enter
    /// among them by their names, and each key comes up from the field to the row holding it.
    /// </summary>
    [SkippableFact]
    public void AFocusedFieldTakesKeysOffscreenAndItsRowHearsThem()
    {
        Needs.Renderer();

        var (row, field) = (Entity.None, Entity.None);
        var heard = new List<string>();
        string? typed = null;

        var run = new PictureRun
        {
            Width = 320,
            Height = 96,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                row = Ui.SpawnNode(new UiSettings { Width = Length.Px(300f) });
                field = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                ecs.SetParent(field, row);
                Ui.SetEditableText(field, new UiEditableTextSettings());
                Ui.Focus(field);

                ecs.Observe<FocusedInput<KeyboardInput>>(row, on =>
                {
                    var input = on.Event.Input;
                    if (input.State == ButtonState.Pressed)
                        heard.Add($"{input.LogicalKey} {(on.Event.FocusedEntity == field ? "from the field" : "from elsewhere")}");
                });
            },
        };

        run.Wait(2)
            .Do("typing a", _ => SyntheticInput.Tap(Key.A, "a"))
            .Wait(2)
            .Do("typing b", _ => SyntheticInput.Tap(Key.B, "b"))
            .Wait(2)
            .Do("rubbing it out", _ => SyntheticInput.Tap(Key.Backspace))
            .Wait(2)
            .Do("pressing Enter", _ => SyntheticInput.Tap(Key.Enter))
            .Wait(3)
            .Do("reading it", _ => typed = Ui.EditableTextOf(field))
            .Go();

        Assert.Equal("a", typed);
        Assert.Equal(
            ["Character(\"a\") from the field", "Character(\"b\") from the field", "Backspace from the field", "Enter from the field"],
            heard);
    }

    /// <summary>
    /// The tab order says where the focus moves next, from the field that has it, and wrapping
    /// round, and the focus is moved there from C#.
    /// </summary>
    [SkippableFact]
    public void TheTabOrderSaysWhereTheFocusMovesAndItIsMovedThere()
    {
        Needs.Renderer();

        var (first, second) = (Entity.None, Entity.None);
        Entity? afterFirst = null, afterSecond = null, focused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var group = Ui.SpawnNode(new UiSettings());
                ecs.Insert<TabGroupRef>(group);
                (first, second) = (Ui.SpawnNode(new UiSettings()), Ui.SpawnNode(new UiSettings()));
                ecs.Insert<TabIndexRef>(first).Value = 0;
                ecs.Insert<TabIndexRef>(second).Value = 1;
                ecs.SetParent(first, group);
                ecs.SetParent(second, group);
            },
        };

        run.Wait(2)
            .Do("from the first", _ =>
            {
                Ui.Focus(first);
                afterFirst = Ui.Navigate(NavAction.Next);
            })
            .Do("from the second", _ =>
            {
                Ui.Focus(second);
                afterSecond = Ui.Navigate(NavAction.Next);
                if (afterSecond is { } next) Ui.Focus(next);
            })
            .Wait(2)
            .Do("reading the focus", world => focused = world.Resource<EcsWorld>().Resource<InputFocusRef>()?.CurrentFocus)
            .Go();

        Assert.Equal(second, afterFirst);
        Assert.Equal(first, afterSecond);
        Assert.Equal(first, focused);
    }

    /// <summary>
    /// A field set again is changed where it stands, taking its new settings and still drawing the
    /// text it holds, where one inserted over it held the text and drew none.
    /// </summary>
    [SkippableFact]
    public void AFieldSetAgainStillDrawsItsText()
    {
        Needs.Renderer();

        var field = Entity.None;
        string? held = null;

        var run = new PictureRun
        {
            Width = 320,
            Height = 96,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                field = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                Ui.SetEditableText(field, new UiEditableTextSettings { Text = "WWWW" });
                ecs.Wrap<TextFontRef>(field).FontSize = new FontSize.Px(30f);
            },
        };

        // Settled frames rather than a few, since the text is drawn once its glyphs are, which a
        // run among others may take longer to reach.
        run.Wait(ShaderMaterialTests.Settled)
            .Capture("before")
            .Do("setting it again", _ => Ui.SetEditableText(field, new UiEditableTextSettings { Text = "WWWW", VisibleLines = 2f }))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("after")
            .Do("reading it", _ => held = Ui.EditableTextOf(field))
            .Go();

        static int Light(CapturedImage picture)
        {
            var count = 0;
            for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (r > 180 && g > 180 && b > 180) count++;
            }
            return count;
        }

        var before = Light(run.Picture("before"));
        Assert.True(before > 50, $"the field drew {before} light pixels of its text before it was set again");
        Assert.Equal("WWWW", held);
        Assert.InRange(Light(run.Picture("after")), before / 2, before * 2);
    }

    /// <summary>A node that is no text field has no text to read, which is null rather than empty.</summary>
    [SkippableFact]
    public void ANodeThatIsNoFieldHasNoText()
    {
        Needs.Renderer();

        string? text = "unread";
        var run = new PictureRun { Scene = _ => text = Ui.EditableTextOf(Ui.SpawnNode(new UiSettings())) };
        run.Wait(2).Go();

        Assert.Null(text);
    }
}

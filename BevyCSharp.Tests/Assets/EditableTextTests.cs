using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's editable text, a field the player types into.</summary>
[Collection("engine")]
public sealed class EditableTextTests
{
    /// <summary>The field's settings and its cursor's sit where the bridge reads them.</summary>
    [Fact]
    public void TheFieldsSettingsAreWhereTheBridgeReadsThem()
    {
        Assert.Equal(16, Marshal.OffsetOf<NativeEditableTextConfig>(nameof(NativeEditableTextConfig.Mode)).ToInt32());
        Assert.Equal(20, Marshal.SizeOf<NativeEditableTextConfig>());
        Assert.Equal(48, Marshal.OffsetOf<NativeTextCursor>(nameof(NativeTextCursor.SelectedText)).ToInt32());
        Assert.Equal(64, Marshal.OffsetOf<NativeTextCursor>(nameof(NativeTextCursor.HasSelectedText)).ToInt32());
        Assert.Equal(72, Marshal.SizeOf<NativeTextCursor>());
    }

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
    /// Tab in a run with no window moves the focus along the tab order, from nothing to the first
    /// node and on to the second, and Shift with it back, as Bevy's observer on the window moves it
    /// in a run with one.
    /// </summary>
    [SkippableFact]
    public void TabMovesTheFocusInARunWithNoWindow()
    {
        Needs.Renderer();

        var (first, second) = (Entity.None, Entity.None);
        var seen = new List<Entity?>();

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

        Action<World> Look(List<Entity?> into) => world => into.Add(world.Resource<EcsWorld>().Resource<InputFocusRef>()?.CurrentFocus);

        run.Wait(2)
            .Do("pressing Tab with nothing focused", _ => SyntheticInput.Tap(Key.Tab))
            .Wait(3)
            .Do("looking", Look(seen))
            .Do("pressing Tab again", _ => SyntheticInput.Tap(Key.Tab))
            .Wait(3)
            .Do("looking again", Look(seen))
            .Do("holding Shift", _ => SyntheticInput.Press(Key.ShiftLeft))
            .Wait(2)
            .Do("pressing Tab with Shift held", _ => SyntheticInput.Tap(Key.Tab))
            .Wait(3)
            .Do("letting Shift go and looking", world =>
            {
                SyntheticInput.Lift(Key.ShiftLeft);
                Look(seen)(world);
            })
            .Go();

        Assert.Equal([first, second, first], seen);
    }

    /// <summary>
    /// A field reports its edits as Bevy's <c>TextEditChange</c> as it is typed into, and a field
    /// made read only takes none of what is typed while it has the focus.
    /// </summary>
    [SkippableFact]
    public void AFieldReportsItsEditsAndAReadOnlyOneTakesNoTyping()
    {
        Needs.Renderer();

        var (editable, readOnly) = (Entity.None, Entity.None);
        var edits = 0;
        string? typed = null, kept = null;

        var run = new PictureRun
        {
            Width = 320,
            Height = 96,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                editable = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                Ui.SetEditableText(editable, new UiEditableTextSettings());
                readOnly = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                Ui.SetEditableText(readOnly, new UiEditableTextSettings { Text = "fixed", Mode = TextReadWriteMode.ReadOnly });
                Ui.Focus(editable);

                ecs.Observe<TextEditChange>(editable, _ => edits++);
            },
        };

        run.Wait(2)
            .Do("typing into the field", _ => SyntheticInput.Tap(Key.A, "a"))
            .Wait(3)
            .Do("reading it and focusing the read-only one", _ =>
            {
                typed = Ui.EditableTextOf(editable);
                Ui.Focus(readOnly);
            })
            .Wait(2)
            .Do("typing into the read-only one", _ => SyntheticInput.Tap(Key.B, "b"))
            .Wait(3)
            .Do("reading it", _ => kept = Ui.EditableTextOf(readOnly))
            .Go();

        Assert.Equal("a", typed);
        Assert.True(edits > 0, "the field reported no edit");
        Assert.Equal("fixed", kept);
    }

    /// <summary>
    /// A field of more lines than it shows has a viewport shorter than its text, which is scrolled
    /// from C# and grows as the field is made taller, its text kept, and its cursor is styled; a
    /// node that is no field has no viewport and takes no cursor.
    /// </summary>
    [SkippableFact]
    public void AFieldsViewportIsReadScrolledAndGrownAndItsCursorStyled()
    {
        Needs.Renderer();

        var (field, plain) = (Entity.None, Entity.None);
        TextViewport? before = null, scrolled = null, taller = null, none = null;
        float textHeight = 0f;
        string? kept = null;
        BevyNativeException? refused = null;
        const string Lines = "one\ntwo\nthree\nfour\nfive\nsix\nseven\neight";

        var run = new PictureRun
        {
            Width = 320,
            Height = 320,
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                field = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f) });
                Ui.SetEditableText(field, new UiEditableTextSettings { Text = Lines, VisibleLines = 2f, AllowNewlines = true });
                Ui.SetTextCursor(field, new UiTextCursorSettings { Color = Color.White, SelectedText = Color.Black, SelectionRadius = 0.25f });
                plain = Ui.SpawnNode(new UiSettings());
            },
        };

        run.Wait(4)
            .Do("reading the viewport", world =>
            {
                before = Ui.TextViewportOf(field);
                textHeight = world.Resource<EcsWorld>().Get<TextLayoutInfoRef>(field)?.Size.Y ?? 0f;
                Ui.ScrollText(field, new Vec2(0f, 10f));
            })
            .Wait(2)
            .Do("reading it scrolled and making the field taller", _ =>
            {
                scrolled = Ui.TextViewportOf(field);
                Ui.SetVisibleLines(field, 4f);
            })
            .Wait(4)
            .Do("reading it taller", _ =>
            {
                taller = Ui.TextViewportOf(field);
                kept = Ui.EditableTextOf(field);
                none = Ui.TextViewportOf(plain);
                refused = Assert.Throws<BevyNativeException>(() => Ui.SetTextCursor(plain, new UiTextCursorSettings()));
            })
            .Go();

        Assert.NotNull(before);
        Assert.True(before.Value.Size.Y > 0f && textHeight > before.Value.Size.Y * 2f, $"the text was {textHeight} tall in a viewport of {before}");
        Assert.Equal(10f, scrolled!.Value.Offset.Y);
        Assert.InRange(taller!.Value.Size.Y, before.Value.Size.Y * 1.8f, before.Value.Size.Y * 2.2f);
        Assert.Equal(Lines, kept);
        Assert.Null(none);
        Assert.Equal(NativeStatus.NotPresent, refused!.Status);
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

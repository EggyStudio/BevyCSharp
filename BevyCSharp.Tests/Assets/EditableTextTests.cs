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
    /// <remarks>
    /// Typing is left out, since Bevy hands a key to the focused field through the primary window
    /// and an offscreen run has none.
    /// </remarks>
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

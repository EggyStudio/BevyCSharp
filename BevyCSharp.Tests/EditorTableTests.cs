using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The editor's tables, which are what makes it extensible rather than editable.
/// </summary>
/// <remarks>
/// The menu, the toolbar, the kinds an entity can be and the pages of settings are all lists of
/// records with an order and a lookup. None of them needs a window, none of them draws anything,
/// and all of them are what a game touches when it adds something of its own — so they are the
/// part of the editor worth a test rather than a screenshot.
/// </remarks>
public sealed class EditorTableTests
{
    [Fact]
    public void APageExistsBecauseSomethingIsOnIt()
    {
        var page = Fresh();

        Assert.DoesNotContain(page, EditorSettings.Pages);

        EditorSettings.Fact(page, "Where", () => "here");

        Assert.Contains(page, EditorSettings.Pages);
        Assert.Single(EditorSettings.On(page));
    }

    [Fact]
    public void SettingsComeBackInOrder()
    {
        var page = Fresh();

        EditorSettings.Fact(page, "Third", () => "c", 30);
        EditorSettings.Fact(page, "First", () => "a", 10);
        EditorSettings.Fact(page, "Second", () => "b", 20);

        Assert.Equal(
            ["First", "Second", "Third"],
            EditorSettings.On(page).Select(entry => entry.Label));
    }

    [Fact]
    public void WhatCanBeChangedSurvivesBeingWrittenDown()
    {
        var page = Fresh();

        var text = "before";
        var number = 1f;
        var flag = false;

        EditorSettings.Text(page, "Text", () => text, value => text = value);
        EditorSettings.Number(page, "Number", () => number, value => number = value);
        EditorSettings.Flag(page, "Flag", () => flag, value => flag = value);

        text = "after";
        number = 2.5f;
        flag = true;

        var saved = EditorSettings.Describe();

        text = "lost";
        number = 0f;
        flag = false;

        EditorSettings.Restore(saved);

        Assert.Equal("after", text);
        Assert.Equal(2.5f, number, 1e-4f);
        Assert.True(flag);
    }

    [Fact]
    public void WhatCannotBeChangedIsNotWrittenDown()
    {
        var page = Fresh();

        EditorSettings.Heading(page, "A heading");
        EditorSettings.Fact(page, "A fact", () => "read only");
        EditorSettings.Action(page, "An action", () => { });

        // Three entries on the page, and nothing about any of them worth keeping: a heading says
        // nothing, a fact is worked out again next time, and an action is not a value at all.
        Assert.Equal(3, EditorSettings.On(page).Count);
        Assert.DoesNotContain(page, EditorSettings.Describe());
    }

    [Fact]
    public void RestoringSkipsWhatThisBuildHasNeverHeardOf()
    {
        var page = Fresh();
        var kept = "here";

        EditorSettings.Text(page, "Kept", () => kept, value => kept = value);

        // A file written by a build that had a setting since renamed. It has to be ignored, not
        // refused: somebody's preferences outlive the version that wrote them.
        EditorSettings.Restore($"{page}\tGone\tsomething\n{page}\tKept\tthere");

        Assert.Equal("there", kept);
    }

    [Fact]
    public void AWayOfTellingWhatAnEntityIsIsSortedByOrder()
    {
        var kinds = EditorKinds.All;

        for (var i = 1; i < kinds.Count; i++)
        {
            Assert.True(
                kinds[i - 1].Order <= kinds[i].Order,
                $"{kinds[i - 1].Mark} came before {kinds[i].Mark} out of order");
        }
    }

    /// <summary>A page name nothing else in this run uses, since the table is one static list.</summary>
    private static string Fresh() => $"Test {Guid.NewGuid():N}";
}

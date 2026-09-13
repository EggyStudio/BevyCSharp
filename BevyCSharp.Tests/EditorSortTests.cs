using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The order names are listed in.
/// </summary>
/// <remarks>
/// The editor numbers what it spawns, so a hierarchy and an asset directory are both full of runs
/// like Cube, Cube 2, Cube 10. Where those land is the difference between a list somebody can read
/// down and one they have to search, and it is decided by a comparer rather than by a panel, so it
/// is worth a test rather than a screenshot.
/// </remarks>
public sealed class EditorSortTests
{
    [Fact]
    public void DigitsCompareAsNumbersNotAsCharacters()
    {
        Assert.True(EditorSort.Naturally("Cube 2", "Cube 10") < 0);
        Assert.True(EditorSort.Naturally("Cube 10", "Cube 2") > 0);
    }

    [Fact]
    public void ARunSortsTheWayItIsCounted()
    {
        var names = new List<string> { "Cube 10", "Cube", "Cube 2", "Cube 1" };
        names.Sort(EditorSort.Naturally);

        Assert.Equal(["Cube", "Cube 1", "Cube 2", "Cube 10"], names);
    }

    [Fact]
    public void LeadingZeroesSayNothingAboutSize()
    {
        Assert.True(EditorSort.Naturally("shot007.png", "shot8.png") < 0);
    }

    [Fact]
    public void CaseIsNotWhatDecidesTheOrder()
    {
        Assert.True(EditorSort.Naturally("apple", "Banana") < 0);
        Assert.True(EditorSort.Naturally("Apple", "banana") < 0);
    }

    [Fact]
    public void TwoDifferentNamesAreNeverEqual()
    {
        // A sorted list that calls them equal drops one of them wherever it likes, which is an
        // order that changes between frames.
        Assert.NotEqual(0, EditorSort.Naturally("Cube", "cube"));
        Assert.Equal(0, EditorSort.Naturally("Cube", "Cube"));
    }

    [Fact]
    public void ANumberTooBigToHoldStillCompares()
    {
        var huge = new string('9', 40);

        Assert.True(EditorSort.Naturally("thing 5", $"thing {huge}") < 0);
    }
}

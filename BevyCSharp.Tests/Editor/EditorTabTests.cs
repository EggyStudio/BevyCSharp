using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The tabs along the bottom of the editor, as a list anything can add to.
/// </summary>
/// <remarks>
/// Raising one is by name rather than by number, because a game that adds a tab of its own changes
/// what every number after it means. Nothing here draws anything, so none of it needs an interface
/// to be running.
/// </remarks>
public sealed class EditorTabTests
{
    [Fact]
    public void ATabIsRaisedAndPutAwayByName()
    {
        using var tabs = new Listed("First", "Second");

        EditorShell.Show("Second");
        Assert.Equal(1, EditorShell.OpenTab);

        // Again, because asking for the one already showing is how a person closes it.
        EditorShell.Show("Second");
        Assert.Equal(-1, EditorShell.OpenTab);
    }

    [Fact]
    public void AskingForATabThatIsNotThereChangesNothing()
    {
        using var tabs = new Listed("First", "Second");

        EditorShell.Show("First");
        EditorShell.Show("Nothing of the sort");

        Assert.Equal(0, EditorShell.OpenTab);
    }

    /// <summary>The tab list, put back the way it was found.</summary>
    /// <remarks>
    /// The list is the editor's own and outlives a test, so what a test adds it takes away again.
    /// </remarks>
    private sealed class Listed : IDisposable
    {
        private readonly List<EditorTab> _was = [.. EditorShell.Tabs];
        private readonly int _open = EditorShell.OpenTab;

        public Listed(params string[] names)
        {
            EditorShell.Tabs.Clear();

            foreach (var name in names) EditorShell.Tabs.Add(new EditorTab(name, () => { }));
        }

        public void Dispose()
        {
            EditorShell.Tabs.Clear();
            EditorShell.Tabs.AddRange(_was);
            EditorShell.OpenTab = _open;
        }
    }
}

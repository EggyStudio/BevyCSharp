using Bevy;
using Xunit;
using BevyCSharp.Editor.Framework;

namespace Bevy.Tests;

/// <summary>
/// The log a console shows, and what it does with what is typed into it.
/// </summary>
/// <remarks>
/// None of this needs an engine: a console is a list of lines, a list of commands and a history,
/// and the panels that draw it are the only part that needs a window.
/// </remarks>
public sealed class ConsoleTests
{
    [Fact]
    public void ARepeatedLineIsCountedRatherThanRepeated()
    {
        ConsoleLog.Clear();

        ConsoleLog.Write(LogLevel.Warning, "the same thing");
        ConsoleLog.Write(LogLevel.Warning, "the same thing");
        ConsoleLog.Write(LogLevel.Warning, "the same thing");
        ConsoleLog.Write(LogLevel.Info, "something else");

        var lines = ConsoleLog.All();

        // What produces a repeat is a warning inside something that runs every frame, and sixty
        // copies a second is a log that holds nothing else.
        Assert.Equal(2, lines.Length);
        Assert.Equal(3, lines[0].Count);
        Assert.Equal("the same thing", lines[0].Text);
        Assert.Equal(1, lines[1].Count);

        Assert.Equal("the same thing  (3)", ConsoleView.Written(lines[0]));
        Assert.Equal("something else", ConsoleView.Written(lines[1]));
    }

    [Fact]
    public void OnlyTheLevelsAskedForAreShown()
    {
        ConsoleLog.Clear();

        ConsoleLog.Write(LogLevel.Info, "ordinary");
        ConsoleLog.Write(LogLevel.Warning, "careful");
        ConsoleLog.Write(LogLevel.Error, "broken");

        var view = new ConsoleView { ShowInfo = false };

        Assert.Equal(["careful", "broken"], view.Lines().Select(line => line.Text));

        view.ShowWarnings = false;
        Assert.Equal(["broken"], view.Lines().Select(line => line.Text));

        // And what is being looked for narrows whatever is left.
        view.ShowInfo = true;
        view.ShowWarnings = true;
        view.Search = " care";

        Assert.Equal(["careful"], view.Lines().Select(line => line.Text));
    }

    [Fact]
    public void WhatWasTypedIsShownBeforeWhatAnsweredIt()
    {
        ConsoleLog.Clear();

        var view = new ConsoleView();
        view.Run("echo hello");

        var lines = ConsoleLog.All();

        // Without the question, a console of answers is one where nobody can tell which line is
        // answering what.
        Assert.Equal("> echo hello", lines[0].Text);
        Assert.Equal("hello", lines[1].Text);
        Assert.Equal(LogLevel.Echo, lines[0].Level);
    }

    [Fact]
    public void TheArrowsWalkBackThroughWhatWasTyped()
    {
        ConsoleLog.Clear();

        var view = new ConsoleView();
        view.Run("echo one");
        view.Run("echo two");
        view.Run("echo two");

        // A command run twice in a row is remembered once: the arrows are for getting back to a
        // command, and two presses that do nothing are two presses too many.
        Assert.Equal("echo two", view.Back(string.Empty));
        Assert.Equal("echo one", view.Back(string.Empty));
        Assert.Equal("echo two", view.Forward(string.Empty));

        // Past the end is back to an empty line, which is where it started.
        Assert.Equal(string.Empty, view.Forward(string.Empty));
    }

    [Fact]
    public void HalfATypedNameCompletesToACommand()
    {
        var view = new ConsoleView();

        Assert.Equal("echo", view.Completion("ec"));
        Assert.Contains("Writes what follows", view.Hint("ec"));

        // A name that is already whole completes to nothing, and a line with arguments in it is
        // past completing.
        Assert.Null(view.Completion("echo"));
        Assert.Null(view.Completion("echo something"));

        // What it says then is what the command takes.
        Assert.Contains("<text>", view.Hint("echo something"));
    }

    [Fact]
    public void QuotedWordsStayTogether()
    {
        Assert.Equal(["one", "two"], ConsoleCommands.Split("one two"));
        Assert.Equal(["one two", "three"], ConsoleCommands.Split("\"one two\" three"));
        Assert.Empty(ConsoleCommands.Split("   "));
    }
}

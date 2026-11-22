using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The process's console, the Diagnostics module's over the engine's commands, what the program
/// writes into it and what it does with what is typed into it.
/// </summary>
/// <remarks>
/// None of this needs an engine, because a console is a list of lines, a list of commands and a
/// history, and the panels that draw it are the only part that needs a window. In the engine
/// collection all the same, since the console is the process's and <c>CrashLogTests</c> tee the
/// output into it and write lines of their own while they run.
/// </remarks>
[Collection("engine")]
public sealed class ConsoleTests : IDisposable
{
    public void Dispose() => ConsoleLog.Model.Edit(string.Empty);

    [Fact]
    public void ARepeatedLineIsCountedRatherThanRepeated()
    {
        ConsoleLog.Clear();

        ConsoleLog.Write(ConsoleLevel.Warning, "the same thing");
        ConsoleLog.Write(ConsoleLevel.Warning, "the same thing");
        ConsoleLog.Write(ConsoleLevel.Warning, "the same thing");
        ConsoleLog.Write(ConsoleLevel.Info, "something else");

        var lines = ConsoleLog.All();

        // What produces a repeat is a warning inside something that runs every frame, and sixty
        // copies a second is a log that holds nothing else.
        Assert.Equal(2, lines.Count);
        Assert.Equal(3, lines[0].Count);
        Assert.Equal("the same thing", lines[0].Text);
        Assert.Equal(1, lines[1].Count);
    }

    [Fact]
    public void WhatWasTypedIsShownBeforeWhatAnsweredIt()
    {
        ConsoleLog.Clear();

        ConsoleHost.Submit("echo hello");

        var lines = ConsoleLog.All();

        // Without the question, a console of answers is one where nobody can tell which line is
        // answering what.
        Assert.Equal("> echo hello", lines[0].Text);
        Assert.Equal("hello", lines[1].Text);
        Assert.Equal(ConsoleLevel.Input, lines[0].Level);
    }

    [Fact]
    public void HalfATypedNameCompletesToAnEnginesCommand()
    {
        // The engine's assemblies loaded, so every command they declare is registered.
        _ = ConsoleHost.Commands;
        var model = ConsoleLog.Model;
        model.Edit("ec");

        // The engine's own commands are the console's, completed from the registry they are
        // generated into.
        Assert.Contains("echo", model.Candidates);
        Assert.True(model.Accept());
        Assert.StartsWith("echo", model.Typed);

        // It then says what the command takes.
        model.Edit("echo something");
        Assert.Contains("<text>", model.Hint);
    }
}

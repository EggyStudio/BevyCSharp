using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The engine's settings panel (<see cref="Debug.Panel"/>), whose answers reach the console as a
/// typed line's do.
/// </summary>
/// <remarks>
/// In the engine collection, since the panel and the console are the process's, and every test of
/// either reads them. The panel is closed again after, from whatever page it was left on.
/// </remarks>
[Collection("engine")]
public sealed class SettingsPanelTests : IDisposable
{
    public void Dispose()
    {
        while (Debug.Panel.Open) Debug.Panel.Press(PanelInput.Back);
    }

    [Fact]
    public void WhatThePanelAnswersIsWrittenToTheConsole()
    {
        _ = Debug.Keys;
        Debug.Panel.Open = true;
        Pick("keys");
        Debug.Panel.Press(PanelInput.Accept);
        Pick("overlay");

        // A key is text, which no step changes, so the step is refused with its reason.
        Debug.Panel.Press(PanelInput.Right);

        Assert.Equal(Answer.BadArgument, Debug.Panel.Answer?.Code);
        Assert.Equal(ConsoleLevel.Error, ConsoleLog.All()[^1].Level);
        Assert.Equal(Debug.Panel.Answer?.Text, ConsoleLog.All()[^1].Text);
    }

    // Selects the shown page's row of that label, from the first row down.
    private static void Pick(string label)
    {
        for (var i = 0; i < Debug.Panel.Rows.Count; i++) Debug.Panel.Press(PanelInput.Up);
        for (var i = 0; i < Debug.Panel.Rows.Count && Debug.Panel.Rows[Debug.Panel.Selected].Label != label; i++)
            Debug.Panel.Press(PanelInput.Down);

        Assert.Equal(label, Debug.Panel.Rows[Debug.Panel.Selected].Label);
    }
}

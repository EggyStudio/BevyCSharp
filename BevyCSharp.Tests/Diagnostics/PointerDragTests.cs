using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers how <c>input.drag</c> reads its words and the steps it takes, which need no engine. The
/// drag itself needs a window or an interface to send a pointer to, and is played in a running
/// editor instead.
/// </summary>
public sealed class PointerDragTests
{
    [Fact]
    public void ADragIsReadWithTheLeftButtonUnlessAnotherIsNamed()
    {
        Assert.True(ConsoleWorldCommands.TryReadDrag("400 300 120 -40 10", out var left, out _));
        Assert.Equal(new ConsoleWorldCommands.PointerDrag(400f, 300f, 120f, -40f, 10, MouseButton.Left), left);

        Assert.True(ConsoleWorldCommands.TryReadDrag("0 0 5 5 2 right", out var right, out _));
        Assert.Equal(MouseButton.Right, right.Button);
    }

    [Theory]
    [InlineData("")]
    [InlineData("400 300 120 0")]
    [InlineData("400 300 120 0 0")]
    [InlineData("400 300 120 0 ten")]
    [InlineData("400 300 120 0 10 Back")]
    [InlineData("400 300 120 0 10 Left extra")]
    public void ADragMissingAWordOrNamingNoButtonIsRefused(string line)
    {
        Assert.False(ConsoleWorldCommands.TryReadDrag(line, out _, out var problem));
        Assert.NotEmpty(problem);
    }

    [Fact]
    public void EachStepIsAnEqualPartOfTheWayAndTheLastIsTheEnd()
    {
        var drag = new ConsoleWorldCommands.PointerDrag(100f, 50f, 40f, -20f, 4, MouseButton.Left);

        Assert.Equal((110f, 45f), drag.At(1));
        Assert.Equal((120f, 40f), drag.At(2));
        Assert.Equal((140f, 30f), drag.At(4));
    }

    [Fact]
    public void ADragLongerThanACallerWaitsIsCutShort()
    {
        Assert.True(ConsoleWorldCommands.TryReadDrag("0 0 10 0 100000", out var drag, out _));
        Assert.True(drag.Frames < (int)ConsoleHost.LaterFrames);
    }
}

using System.Text;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace BevyCSharp.Tests;

/// <summary>
/// Covers how text comes back across the boundary.
/// </summary>
/// <remarks>
/// The writer half is exercised without the native library, because what is worth testing is the
/// caller's side of the convention: the probe buffer, the retry when the text does not fit, and
/// what happens when the answer changes between the two calls.
/// </remarks>
public sealed unsafe class TextConventionTests
{
    /// <summary>Builds a writer that reports <paramref name="text"/> and writes it when it fits.</summary>
    private static Native.TextWriter Writing(string text, Action? onCall = null)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        return (buffer, capacity) =>
        {
            onCall?.Invoke();
            if (capacity < bytes.Length) return bytes.Length;

            bytes.AsSpan().CopyTo(new Span<byte>(buffer, capacity));
            return bytes.Length;
        };
    }

    [Fact]
    public void ShortTextIsReadInOneCall()
    {
        var calls = 0;
        var text = Native.ReadText(Writing("Built-in Retina Display", () => calls++), "test");

        Assert.Equal("Built-in Retina Display", text);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void EmptyTextIsNotNull()
    {
        Assert.Equal(string.Empty, Native.ReadText(Writing(string.Empty), "test"));
    }

    [Fact]
    public void LongTextIsReadOnTheSecondCall()
    {
        // Longer than the probe buffer, so the first call reports the length without writing and
        // the second is made against a buffer sized from that answer.
        var expected = new string('p', 4096);
        var calls = 0;

        var text = Native.ReadText(Writing(expected, () => calls++), "test");

        Assert.Equal(expected, text);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void MultiByteCharactersSurviveBothPaths()
    {
        const string Short = "Bildschirm groß, éè, 🖥";
        var long_ = string.Concat(Enumerable.Repeat("ゆきの下", 400));

        Assert.Equal(Short, Native.ReadText(Writing(Short), "test"));
        Assert.Equal(long_, Native.ReadText(Writing(long_), "test"));
    }

    [Fact]
    public void FailureCodesThrow()
    {
        var failure = Assert.Throws<BevyNativeException>(
            () => Native.ReadText((_, _) => (int)NativeStatus.NoEntity, "reading a name"));

        Assert.Contains("reading a name", failure.Message);
    }

    [Fact]
    public void TextThatGrowsBetweenCallsIsTruncatedRatherThanOverrunning()
    {
        // The two calls are not one atomic read, so the second can report more than the buffer
        // sized from the first. Reading only what was asked for is what keeps that safe.
        var calls = 0;
        var text = Native.ReadText(
            (buffer, capacity) =>
            {
                if (calls++ == 0) return 300;

                new Span<byte>(buffer, capacity).Fill((byte)'x');
                return 900;
            },
            "test");

        Assert.Equal(new string('x', 300), text);
    }
}

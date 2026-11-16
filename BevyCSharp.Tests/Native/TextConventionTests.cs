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
/// caller's side of the convention, the probe buffer, the retry when the text does not fit, and
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

    /// <summary>
    /// Builds a writer whose text is the next of <paramref name="texts"/> at each call and the last
    /// after them, as a text read while something else changes it.
    /// </summary>
    private static Native.TextWriter Changing(params string[] texts)
    {
        var calls = 0;
        return (buffer, capacity) => Writing(texts[Math.Min(calls++, texts.Length - 1)])(buffer, capacity);
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
    public void TextThatGrowsBetweenCallsIsAskedForAgain()
    {
        // The two calls are not one atomic read, so the second can report more than the buffer
        // sized from the first, and a writer given too little writes nothing, as the memory
        // command's text grows while the render thread allocates.
        var text = Native.ReadText(Changing(new('a', 300), new('b', 900), new('c', 900)), "test");

        Assert.Equal(new string('c', 900), text);
    }

    [Fact]
    public void TextThatShrinksBetweenCallsIsReadAsWritten()
    {
        Assert.Equal(new string('b', 600), Native.ReadText(Changing(new('a', 900), new('b', 600)), "test"));
    }

    [Fact]
    public void TextLongerAtEveryAskIsAnError()
    {
        // Longer at each of its first twenty calls, more than a reader asks for it before it gives
        // up, so an error rather than a loop that ends only when the writer's text stops growing.
        var texts = Enumerable.Range(0, 20).Select(i => new string('g', 300 + i)).ToArray();

        var failure = Assert.Throws<InvalidOperationException>(
            () => Native.ReadText(Changing(texts), "reading a growing text"));

        Assert.Contains("reading a growing text", failure.Message);
    }
}

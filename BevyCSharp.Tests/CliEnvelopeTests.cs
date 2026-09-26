using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the shape every answer takes, which is the whole of what a caller may depend on.
/// </summary>
/// <remarks>
/// The fields and the codes are the contract. A sentence can be reworded at any time and a payload
/// can grow, but a caller that branches on <c>success</c> and reads <c>errors[0].code</c> has to
/// keep working, so this asserts those.
/// </remarks>
public sealed class CliEnvelopeTests
{
    [Fact]
    public void ASuccessCarriesNoErrors()
    {
        using var document = JsonDocument.Parse(
            CliJson.Ok("status", writer => writer.WriteNumber("frame", 7)));

        var root = document.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("status", root.GetProperty("command").GetString());
        Assert.Equal(7, root.GetProperty("data").GetProperty("frame").GetInt32());
        Assert.Empty(root.GetProperty("errors").EnumerateArray());
        Assert.Empty(root.GetProperty("warnings").EnumerateArray());
    }

    [Fact]
    public void AFailureCarriesACodeAndASentence()
    {
        using var document = JsonDocument.Parse(
            CliJson.Fail("command", "UNKNOWN_COMMAND", "'nope' is not a command."));

        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);

        var error = Assert.Single(root.GetProperty("errors").EnumerateArray().ToArray());

        Assert.Equal("UNKNOWN_COMMAND", error.GetProperty("code").GetString());
        Assert.Contains("nope", error.GetProperty("message").GetString());
    }

    /// <summary>
    /// A failure may still carry a payload, which is why nothing should read <c>data</c> to
    /// decide whether something worked.
    /// </summary>
    [Fact]
    public void AFailureMayStillCarryAPayload()
    {
        using var document = JsonDocument.Parse(CliJson.Envelope(
            "command",
            success: false,
            data: writer => writer.WriteString("result", "no renderer"),
            errors: [new CliError("NO_RENDERER", "This bridge cannot draw.")]));

        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("no renderer", root.GetProperty("data").GetProperty("result").GetString());
    }

    [Fact]
    public void AnIdIsEchoedBackWhenThereWasOne()
    {
        using var withId = JsonDocument.Parse(CliJson.Ok("ping", id: "42"));
        using var without = JsonDocument.Parse(CliJson.Ok("ping"));

        Assert.Equal("42", withId.RootElement.GetProperty("id").GetString());
        Assert.False(without.RootElement.TryGetProperty("id", out _));
    }

    /// <summary>What a person types, and what a tool sends, arrive as the same thing.</summary>
    [Theory]
    [InlineData("\"Spawn/Cube\"", "Spawn/Cube")]
    [InlineData("\"a b\"", "a b")]
    [InlineData("\"say \\\"hi\\\"\"", "say \"hi\"")]
    [InlineData("world.SetName(e, \"x\")", "world.SetName(e, \"x\")")]
    [InlineData("\"a\" and \"b\"", "\"a\" and \"b\"")]
    [InlineData("plain", "plain")]
    public void OneEnclosingPairOfQuotesComesOff(string line, string expected) =>
        Assert.Equal(expected, ConsoleCommands.Unwrap(line));
}

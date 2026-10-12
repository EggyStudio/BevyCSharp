using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Bevy;

namespace Bevy;

/// <summary>What a console command run in another app answered.</summary>
/// <param name="Success">Whether it worked.</param>
/// <param name="Result">What it said, or nothing.</param>
/// <param name="Error">Why it failed, when it did.</param>
public sealed record CliAnswer(bool Success, string? Result, string? Error);

namespace Bevy;

/// <summary>What one of Bevy's diagnostics holds, as <see cref="Diagnostics"/> reads it.</summary>
/// <remarks>
/// A value the diagnostic has none of yet, before its first measurement, is null. The suffix comes
/// with <see cref="Diagnostics.All"/> and is empty from <see cref="Diagnostics.TryRead"/>.
/// </remarks>
/// <param name="Path">What it is called, such as <c>fps</c>.</param>
/// <param name="Suffix">What Bevy's log prints after its numbers, such as <c>" ms"</c>.</param>
/// <param name="Enabled">Whether it is measured and logged.</param>
/// <param name="History">How many measurements it keeps for its average.</param>
/// <param name="Value">The last measurement.</param>
/// <param name="Smoothed">The measurements smoothed over time, which Bevy's log prints first.</param>
/// <param name="Average">The mean of the measurements kept.</param>
public readonly record struct DiagnosticReading(
    string Path,
    string Suffix,
    bool Enabled,
    int History,
    double? Value,
    double? Smoothed,
    double? Average);

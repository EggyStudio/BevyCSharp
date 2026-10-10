using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The managed mirror of a diagnostic's reading has to agree with the bridge's field by field, the
/// same numbers asserted on the Rust side.
/// </summary>
public sealed class NativeDiagnosticLayoutTests
{
    [Theory]
    [InlineData(nameof(NativeDiagnostic.Average), 16)]
    [InlineData(nameof(NativeDiagnostic.History), 24)]
    [InlineData(nameof(NativeDiagnostic.Enabled), 28)]
    public void EveryFieldSitsWhereTheBridgePutIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeDiagnostic>(field).ToInt32());

    [Fact]
    public void TheWholeReadingIsTheSizeTheBridgeWrites() =>
        Assert.Equal(32, Marshal.SizeOf<NativeDiagnostic>());
}

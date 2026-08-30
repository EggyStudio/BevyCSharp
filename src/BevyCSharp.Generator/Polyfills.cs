// Analyzers must target netstandard2.0 because that is what the Roslyn compiler host loads.
// These are the few runtime types that modern C# syntax lowers to but netstandard2.0 does not
// declare. Defining them here is the standard workaround; the compiler only needs the symbols
// to exist, and marking them internal keeps them out of the analyzer's public surface.

namespace System.Runtime.CompilerServices
{
    /// <summary>Required by the compiler for <c>init</c> accessors and <c>record</c> types.</summary>
    internal static class IsExternalInit;
}

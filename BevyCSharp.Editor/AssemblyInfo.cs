using System.Runtime.CompilerServices;

// The test suite checks a few of the editor's own decisions directly, such as how many places a
// number is written to, which are not worth widening the public API for.
[assembly: InternalsVisibleTo("BevyCSharp.Tests")]

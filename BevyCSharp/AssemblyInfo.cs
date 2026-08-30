using System.Runtime.CompilerServices;

// The test suite drives a few internal seams directly, most importantly the frame-state
// snapshot that the engine normally fills, so input tests exercise the real code path instead of
// a parallel fake. Nothing here widens the public API.
[assembly: InternalsVisibleTo("BevyCSharp.Tests")]

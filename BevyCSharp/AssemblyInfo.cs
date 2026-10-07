using System.Runtime.CompilerServices;

// The test suite drives a few internal seams directly, most importantly the frame-state
// snapshot that the engine normally fills, so input tests exercise the real code path instead of
// a parallel fake. Nothing here widens the public API.
[assembly: InternalsVisibleTo("BevyCSharp.Tests")]

// The script host lets go of what a retired generation of scripts left in the app (App.ForgetAssembly),
// which is the engine's own business and no game's, so it is reached here rather than made public.
[assembly: InternalsVisibleTo("BevyCSharp.Scripting")]

using System.Reflection;
using System.Runtime.Loader;
using Bevy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Compiles a fragment of C# and runs it against the live world.
/// </summary>
/// <remarks>
/// <para>
/// The escape hatch that makes a command line complete. A catalog of commands covers what somebody
/// thought to write a command for; this covers the rest, and it does it against the world that is
/// already loaded rather than a fresh one. Reading a field nobody exposed, calling a method on the
/// way to deciding whether it deserves a command, or spawning something to look at is one line.
/// </para>
/// <para>
/// Each fragment is compiled into its own collectible load context and dropped afterwards, the same
/// way <see cref="ScriptHost"/> handles a generation of scripts. Nothing accumulates but the cost
/// of compiling, which is a few tens of milliseconds against a Roslyn that is already loaded.
/// </para>
/// <para>
/// It runs arbitrary code in this process, which is the point and also the whole of its danger. It
/// is exactly as privileged as the person at the terminal, and no more. It lives in the editor
/// rather than in the library so that a shipped game carries no compiler.
/// </para>
/// </remarks>
public static class EditorEval
{
    private static MetadataReference[]? _references;

    /// <summary>
    /// Compiles <paramref name="code"/> and runs it, answering with what it evaluated to.
    /// </summary>
    /// <remarks>
    /// A fragment with no semicolon in it is an expression and its value is the answer; anything
    /// else is a block of statements and has to <c>return</c> to say something. The world is in
    /// scope as <c>world</c>.
    /// </remarks>
    /// <param name="code">The fragment.</param>
    /// <returns>What it answered, or what was wrong with it.</returns>
    public static string Run(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "nothing to evaluate";

        var body = code.Contains(';') ? code : $"return {code};";

        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Bevy;
            using BevyCSharp.Editor;
            using BevyCSharp.Editor.Framework;

            public static class Fragment
            {
                public static object? Run(EcsWorld world)
                {
                    {{body}}
                }
            }
            """;

        var name = $"Eval.Fragment{Interlocked.Increment(ref _generation)}";

        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source, path: "<eval>")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var image = new MemoryStream();
        var built = compilation.Emit(image);

        if (!built.Success)
        {
            ConsoleHost.Fail("EVAL_COMPILE_FAILED", "The fragment did not compile.");

            return string.Join(
                Environment.NewLine,
                built.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.GetMessage())
                    .Take(5));
        }

        var context = new EvalContext(name);

        try
        {
            image.Position = 0;

            var assembly = context.LoadFromStream(image);
            var method = assembly.GetType("Fragment")?.GetMethod(
                "Run", BindingFlags.Public | BindingFlags.Static);

            if (method is null)
            {
                ConsoleHost.Fail("EVAL_FAILED", "The fragment compiled but had no entry point.");
                return "the fragment compiled but had no entry point";
            }

            var answer = method.Invoke(null, [EditorShell.Ecs]);
            return Describe(answer);
        }
        catch (TargetInvocationException error)
        {
            // What the fragment threw, not the reflection wrapper around it, which says nothing.
            var inner = error.InnerException ?? error;

            ConsoleHost.Fail("EVAL_THREW", $"{inner.GetType().Name}: {inner.Message}");
            return $"{inner.GetType().Name}: {inner.Message}";
        }
        finally
        {
            // Dropped whether or not it worked. A fragment's assembly is of no use after its one
            // call, and the alternative is a process that grows by one assembly per question.
            context.Unload();
        }
    }

    /// <summary>Compiles and runs a file, for a fragment too long to type on one line.</summary>
    public static string RunFile(string path)
    {
        if (!File.Exists(path))
        {
            ConsoleHost.Fail("NO_SUCH_FILE", $"There is no file at {path}.");
            return $"no file at {path}";
        }

        return Run(File.ReadAllText(path));
    }

    private static int _generation;

    /// <summary>What a fragment is compiled against: everything this process already loaded.</summary>
    private static MetadataReference[] References() => _references ??=
    [
        .. AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
    ];

    /// <summary>An answer as one line, with a collection shown by its contents.</summary>
    private static string Describe(object? answer) => answer switch
    {
        null => "null",
        string text => text,
        System.Collections.IEnumerable items => string.Join(
            ", ", items.Cast<object?>().Take(50).Select(item => item?.ToString() ?? "null")),
        _ => answer.ToString() ?? "null",
    };

    /// <summary>One fragment's assembly, in a context that can be dropped.</summary>
    private sealed class EvalContext(string name)
        : AssemblyLoadContext(name, isCollectible: true);
}

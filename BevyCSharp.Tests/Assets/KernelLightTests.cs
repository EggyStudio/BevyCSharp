using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The kernels lit by the brightest directional light choose it first in each entry, before any of
/// a group's threads returns or takes a branch of its own, and hand it on, as the comment over
/// clouds.slang's <c>brightest()</c> says.
/// </summary>
/// <remarks>
/// Mesa's llvmpipe, the software Vulkan the workflow's runners draw with, faults on the
/// transmittance read in that function's loop where the loop runs once a group's threads have
/// parted. The clouds' march chose its light inside its branches from item 69's third phase on, and
/// the feature test ended within a minute of its start there. A GPU draws the kernel either way, so
/// a suite run on one would never see the call move back inside a branch, and the test reads the
/// sources instead.
/// </remarks>
public sealed class KernelLightTests
{
    private static readonly string Root = FindRoot();

    // An entry whose body opens on the light's choice, kept in a variable it hands on.
    private static readonly Regex ChosenFirst = new(@"\[shader\(""compute""\)\]\s*\[numthreads\([^)]*\)\]\s*void\s+\w+\([^)]*\)\s*\{\s*int\s+\w+\s*=\s*brightest\(\);");

    [Fact]
    public void EachEntryChoosesItsLightBeforeItsThreadsPart()
    {
        var kernels = Directory.GetFiles(Path.Combine(Root, "BevyCSharp", "Assets", "Kernels"), "*.slang")
            .Select(path => (Name: Path.GetFileName(path), Code: Code(File.ReadAllText(path))))
            .Where(kernel => kernel.Code.Contains("int brightest()"))
            .ToArray();
        foreach (var name in new[] { "clouds.slang", "fog_froxels.slang", "precipitation.slang" })
            Assert.Contains(name, kernels.Select(kernel => kernel.Name));

        foreach (var (name, code) in kernels)
        {
            // Every use but the definition, each of which must open an entry.
            var calls = Regex.Matches(code, @"\bbrightest\(\)").Count - 1;
            var first = ChosenFirst.Matches(code).Count;
            Assert.True(calls > 0, $"{name} defines brightest() and never calls it");
            Assert.True(first == calls, $"{name} calls brightest() {calls} times, {first} of them first in an entry");
        }
    }

    // A kernel's code without its comments, so a comment naming the function is not taken as a use.
    private static string Code(string text) => Regex.Replace(text, @"//[^\n]*|/\*.*?\*/", "", RegexOptions.Singleline);

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

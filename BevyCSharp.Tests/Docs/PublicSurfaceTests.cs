using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The library's public surface against <c>BevyCSharp/PublicApi.txt</c>, the listing checked in
/// beside it, so a commit that adds, removes or reshapes anything a game can call changes that file
/// too and is read as such a change, as N 2.1 of NORM.md has it.
/// </summary>
/// <remarks>
/// <c>build/api.sh</c> writes the listing again from the built library, through this test with
/// <c>BCS_WRITE_API</c> set, once a change to the surface is meant.
/// </remarks>
public sealed class PublicSurfaceTests
{
    private static string Listing() => Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "PublicApi.txt");

    [Fact]
    public void ThePublicSurfaceIsTheListingCheckedIn()
    {
        var built = PublicSurface.Write(typeof(App).Assembly);
        if (Environment.GetEnvironmentVariable("BCS_WRITE_API") == "1") File.WriteAllText(Listing(), built);

        var checkedIn = File.Exists(Listing()) ? File.ReadAllText(Listing()).ReplaceLineEndings("\n") : "";
        if (built == checkedIn) return;

        // Each line that differs, under the type it belongs to, so the failure says what changed.
        var added = Lines(built).Except(Lines(checkedIn)).ToList();
        var removed = Lines(checkedIn).Except(Lines(built)).ToList();
        var report = string.Join("\n", removed.Select(line => "- " + line).Concat(added.Select(line => "+ " + line)).Take(60));
        Assert.True(report.Length == 0, $"The public surface changed without BevyCSharp/PublicApi.txt, which build/api.sh writes again:\n{report}");
        Assert.True(built == checkedIn, "The listing holds the same lines in another order, which build/api.sh writes again.");
    }

    [Fact]
    public void AListingSpellsMembersAsASignatureDoes()
    {
        var listing = PublicSurface.Write(typeof(App).Assembly);

        Assert.Contains("static class Bevy.Render\n", listing);
        Assert.Contains("  static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f)\n", listing);
        Assert.Contains("  static bool ReleaseWhenUnused(AssetHandle handle)\n", listing);
        Assert.Contains("  PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance)\n", listing);
        Assert.DoesNotContain("<Clone>", listing);
        Assert.DoesNotContain(" internal ", listing);
    }

    // Each member line with its type's header in front, so a line moved between types differs.
    private static IEnumerable<string> Lines(string listing)
    {
        var type = "";
        foreach (var line in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith(' ')) yield return type = line;
            else yield return type + " |" + line;
        }
    }
}

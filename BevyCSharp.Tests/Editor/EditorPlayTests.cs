using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers how an export tells whether the render bridge it built before is current.</summary>
public sealed class EditorPlayTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-export-");

    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// A bridge older than any file it is built from is built again, a shader, the crate's build
    /// script and the workspace's lock among them, and one older than its tests or fixtures alone
    /// is not.
    /// </summary>
    [Fact]
    public void ABridgeOlderThanAnythingItIsBuiltFromIsBuiltAgain()
    {
        var native = Path.Combine(_folder.Path, "native");
        var library = _folder.File("libbevy_csharp.so");
        var at = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] files =
        [
            "bevy_csharp/src/lib.rs",
            "bevy_csharp/src/render/bcs_gi.slang",
            "bevy_csharp/src/render/tests.rs",
            "bevy_csharp/src/fixtures/scene.json",
            "bevy_csharp/Cargo.toml",
            "bevy_csharp/build.rs",
            "Cargo.toml",
            "Cargo.lock",
        ];
        foreach (var file in files) Touch(Path.Combine(native, file), at);
        Touch(library, at.AddHours(1));

        // Each file made newer than the library in turn, and put back.
        var current = new List<string> { $"none {EditorPlay.Current(library, native)}" };
        foreach (var file in files)
        {
            var path = Path.Combine(native, file);
            File.SetLastWriteTimeUtc(path, at.AddHours(2));
            current.Add($"{file} {EditorPlay.Current(library, native)}");
            File.SetLastWriteTimeUtc(path, at);
        }

        Assert.Equal(
        [
            "none True",
            "bevy_csharp/src/lib.rs False",
            "bevy_csharp/src/render/bcs_gi.slang False",
            "bevy_csharp/src/render/tests.rs True",
            "bevy_csharp/src/fixtures/scene.json True",
            "bevy_csharp/Cargo.toml False",
            "bevy_csharp/build.rs False",
            "Cargo.toml False",
            "Cargo.lock False",
        ], current);
    }

    /// <summary>Writes an empty file at a time of its own, its folders made.</summary>
    private static void Touch(string path, DateTime at)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, string.Empty);
        File.SetLastWriteTimeUtc(path, at);
    }
}

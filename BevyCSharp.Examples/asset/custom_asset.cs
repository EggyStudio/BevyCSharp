// Bevy's custom_asset example, examples/asset/custom_asset.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using System.Text.RegularExpressions;
using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows assets of a game's own types read by a loader of its own: a CustomAsset read from a RON
// file, one from a file with no extension, and the same file read again as a blob of bytes, each
// printed once all three are in.
//
// Bevy registers an asset type and a loader for it with the asset server. A C# game reads its own
// files with its own code, here through AssetFiles, which reads from the asset folder or what the
// game carries, and keeps what it read as it likes. Its data assets (DataAssets) are the same thing
// with the editor's inspector and saving on top, for assets that are a C# type's fields.
internal static class CustomAssetExample
{
    private sealed record CustomAsset(int Value);

    private sealed record Blob(byte[] Bytes);

    private static Task<CustomAsset>? _asset, _other;
    private static Task<Blob>? _blob;
    private static bool _printed;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            _printed = false;

            // Read on workers, as Bevy's loaders run, so the frames go on while they read.
            _asset = Task.Run(() => LoadCustom("data/asset.custom"));
            _other = Task.Run(() => LoadCustom("data/asset_no_extension"));
            _blob = Task.Run(() =>
            {
                Console.WriteLine("Loading Blob...");
                return new Blob(AssetFiles.ReadAllBytes("data/asset.custom"));
            });
        }, "custom_asset.Setup");

        app.Update(_ =>
        {
            if (_printed) return;
            if (!_asset!.IsCompleted) { Console.WriteLine("Custom Asset Not Ready"); return; }
            if (!_other!.IsCompleted) { Console.WriteLine("Other Custom Asset Not Ready"); return; }
            if (!_blob!.IsCompleted) { Console.WriteLine("Blob Not Ready"); return; }

            Console.WriteLine($"Custom asset loaded: CustomAsset {{ value: {_asset.Result.Value} }}");
            Console.WriteLine($"Custom asset loaded: CustomAsset {{ value: {_other.Result.Value} }}");
            Console.WriteLine($"Blob Size: {_blob.Result.Bytes.Length} Bytes");
            _printed = true;
        }, "custom_asset.PrintOnLoad");
    }

    // The RON these files hold, CustomAsset (value: 42), read for its one field.
    private static CustomAsset LoadCustom(string path)
    {
        var text = AssetFiles.ReadAllText(path);
        var value = Regex.Match(text, @"value\s*:\s*(-?\d+)");
        if (!value.Success) throw new InvalidDataException($"{path} is no CustomAsset.");
        return new CustomAsset(int.Parse(value.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
    }
}

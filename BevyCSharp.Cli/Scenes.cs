using Bevy;

namespace BevyCSharp.Cli;

/// <summary>
/// The scene packs from a terminal, the manifests in the checkout's <c>scenes</c> folder, a pack
/// fetched into the folder every game on the machine shares, and a folder written as a pack, which
/// the script that makes a scene pack calls.
/// </summary>
/// <remarks>
/// The same <see cref="ScenePacks"/> the feature test's panel fetches through, so a pack fetched
/// here is the one the panel finds fetched. A fetch tells its progress on the error stream, for a
/// person watching, and answers on the output stream as every verb does.
/// </remarks>
internal static class Scenes
{
    /// <summary>Runs <c>scenes</c>, <c>scenes fetch &lt;name&gt;</c> or <c>scenes pack &lt;folder&gt; &lt;pack&gt;</c>.</summary>
    public static int Run(Options options, string[] rest) => rest switch
    {
        [] or ["list"] => List(options),
        ["fetch", var name] => Fetch(options, name),
        ["pack", var folder, var pack] => Pack(options, folder, pack),
        _ => Output.Refuse(
            options,
            "scenes",
            "BAD_ARGUMENT",
            "Run 'bcs scenes' to list the packs, 'bcs scenes fetch <name>' to fetch one, or 'bcs scenes pack <folder> <pack>' to write a folder as a pack."),
    };

    /// <summary>The checkout's manifests.</summary>
    private static string Manifests => Path.Combine(Repo.Root ?? Environment.CurrentDirectory, "scenes");

    private static int List(Options options)
    {
        var packs = ScenePacks.List(Manifests, out var problems);

        var envelope = CliJson.Envelope(
            "scenes",
            true,
            writer =>
            {
                writer.WriteString("folder", ScenePacks.Folder);
                writer.WriteStartArray("packs");
                foreach (var pack in packs)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", pack.Name);
                    writer.WriteString("title", pack.Title);
                    writer.WriteString("license", pack.License);
                    writer.WriteNumber("size", pack.Size);
                    writer.WriteBoolean("fetched", ScenePacks.IsFetched(pack));
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            },
            warnings: problems);

        return Output.Print(options, envelope, data =>
        {
            foreach (var pack in data.GetProperty("packs").EnumerateArray())
            {
                var fetched = pack.GetProperty("fetched").GetBoolean() ? "fetched" : "not fetched";
                Console.WriteLine($"{pack.GetProperty("name").GetString(),-24} {pack.GetProperty("size").GetInt64() / 1_000_000,6} MB  {fetched,-12} {pack.GetProperty("title").GetString()}");
            }

            Console.WriteLine($"kept in {data.GetProperty("folder").GetString()}");
        });
    }

    private static int Fetch(Options options, string name)
    {
        var file = Path.Combine(Manifests, name + ".json");
        if (!ScenePacks.TryRead(file, out var pack, out var problem))
        {
            return Output.Refuse(options, "scenes", File.Exists(file) ? "BAD_MANIFEST" : "NO_SUCH_SCENE", problem);
        }

        if (ScenePacks.IsFetched(pack)) return Output.Say(options, "scenes", $"{pack.Title} is fetched, in {ScenePacks.PathOf(pack)}");

        // Told in whole percents, each once, which is a line a second or so at a home's speed.
        var said = -1;
        var progress = new Progress<double>(fraction =>
        {
            var percent = (int)(fraction * 100);
            if (percent == said || options.Quiet) return;
            said = percent;
            Console.Error.Write($"\rfetching {pack.Title}, {percent}%");
        });

        var failed = ScenePacks.FetchAsync(pack, progress).GetAwaiter().GetResult();
        if (!options.Quiet) Console.Error.WriteLine();

        return failed is null
            ? Output.Say(options, "scenes", $"{pack.Title} is fetched and checked, in {ScenePacks.PathOf(pack)}. {pack.Attribution}")
            : Output.Refuse(options, "scenes", "FETCH_FAILED", failed);
    }

    private static int Pack(Options options, string folder, string pack)
    {
        if (!Directory.Exists(folder)) return Output.Refuse(options, "scenes", "BAD_ARGUMENT", $"There is no folder {folder} to write as a pack.");

        var count = AssetPack.Write(folder, pack);
        return Output.Say(options, "scenes", $"wrote {count} files from {folder} to {pack}");
    }
}

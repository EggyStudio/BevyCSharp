using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bevy;

/// <summary>
/// The scene packs a game can show, read from their manifests, fetched into a folder every game on
/// the machine shares, checked, and mounted for both sides of the bridge to read.
/// </summary>
/// <remarks>
/// <para>
/// A manifest is a <c>.json</c> file named for its pack, kept beside the game or in the repository,
/// holding a <see cref="ScenePack"/>. <see cref="FetchAsync"/> downloads the pack into
/// <see cref="Folder"/>, counting the bytes as they come for a progress bar and hashing them on the
/// way, and keeps it only where its SHA-256 is the manifest's, so a pack cut short or replaced is
/// never mounted. <see cref="TryMount"/> mounts a fetched pack under <c>packs/</c> and its name
/// while an app runs (<see cref="AssetFiles.Mount"/>), and answers the path its model is loaded by.
/// </para>
/// <para>
/// The folder is shared, since a scene is no one game's own and a pack is large enough that a
/// second copy costs, and <c>BCS_SCENE_PACKS</c> names another, as a workflow caching the packs
/// between runs does. Nothing is thrown for a manifest, a download or a pack that is wrong; each
/// answers with the reason, naming the file or the address, as N 2.6 of NORM.md has it.
/// </para>
/// </remarks>
public static class ScenePacks
{
    /// <summary>The folder under the asset root that a pack is mounted in, under its name.</summary>
    public const string MountFolder = "packs";

    /// <summary>The one client every fetch shares, as a client is meant to be.</summary>
    private static readonly HttpClient Http = new();

    private static string? _folder;

    /// <summary>
    /// Where fetched packs are kept, the platform's data directory under <c>BevyCSharp</c> unless
    /// <c>BCS_SCENE_PACKS</c> names another or this is set.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to go back to the default.</remarks>
    public static string Folder
    {
        get => _folder
            ?? (Environment.GetEnvironmentVariable("BCS_SCENE_PACKS") is { Length: > 0 } named ? named : null)
            ?? Path.Combine(UserData.PlatformDirectory(), "BevyCSharp", "scene-packs");
        set => _folder = value;
    }

    /// <summary>Reads every manifest in a folder, in the order of their names.</summary>
    /// <param name="folder">A folder of manifests, each a <c>.json</c> file.</param>
    /// <param name="problems">Why each manifest that did not read did not, naming its file.</param>
    /// <returns>The manifests that read, none where the folder is not there.</returns>
    public static IReadOnlyList<ScenePack> List(string folder, out IReadOnlyList<string> problems)
    {
        var found = new List<ScenePack>();
        var wrong = new List<string>();
        problems = wrong;
        if (!Directory.Exists(folder)) return found;

        foreach (var file in Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            if (TryRead(file, out var pack, out var problem)) found.Add(pack);
            else wrong.Add(problem);
        }

        return found;
    }

    /// <summary>Reads one manifest, or says why it cannot, naming the file.</summary>
    /// <remarks>
    /// The pack's name is the file's, whatever the file says, so a manifest copied under another
    /// name is fetched and mounted under that one.
    /// </remarks>
    public static bool TryRead(string file, [NotNullWhen(true)] out ScenePack? pack, [NotNullWhen(false)] out string? problem)
    {
        pack = null;

        try
        {
            ScenePack? read;
            using (var stream = File.OpenRead(file)) read = JsonSerializer.Deserialize(stream, ScenePackJson.Default.ScenePack);

            if (read is null)
            {
                problem = $"{file} holds no scene pack.";
                return false;
            }

            var name = Path.GetFileNameWithoutExtension(file);
            read = read with { Name = name, Title = read.Title.Length > 0 ? read.Title : name };
            if (Missing(read) is { } field)
            {
                problem = $"{file} gives no {field}, so its pack cannot be fetched and checked.";
                return false;
            }

            pack = read;
            problem = null;
            return true;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            problem = $"{file} could not be read as a scene pack. {error.Message}";
            return false;
        }
    }

    /// <summary>Where a pack is kept once fetched.</summary>
    public static string PathOf(ScenePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        return Path.Combine(Folder, pack.Name + ".pack");
    }

    /// <summary>
    /// Whether a pack has been fetched, by its file being there at the manifest's size, the hash
    /// having been checked as it arrived.
    /// </summary>
    public static bool IsFetched(ScenePack pack)
    {
        var file = new FileInfo(PathOf(pack));
        return file.Exists && file.Length == pack.Size;
    }

    /// <summary>
    /// Fetches a pack into <see cref="Folder"/>, telling how far it has come, and keeps it where its
    /// SHA-256 is the manifest's.
    /// </summary>
    /// <remarks>
    /// The bytes go to a file beside the pack's and are renamed over it once checked, so a fetch
    /// stopped half way or a pack that is not the manifest's leaves the one fetched before, if any,
    /// and nothing new. A manifest's address is HTTPS for a pack published as a release asset, or a
    /// file on this machine, written as a path or a <c>file:</c> address, for one made here and not
    /// yet published.
    /// </remarks>
    /// <param name="pack">The manifest.</param>
    /// <param name="progress">Told the fraction fetched, from nothing to one, as the bytes come.</param>
    /// <param name="cancel">Stops the fetch, which is then answered as stopped.</param>
    /// <returns>Nothing where the pack was fetched and checked, or why it was not.</returns>
    public static async Task<string?> FetchAsync(ScenePack pack, IProgress<double>? progress = null, CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(pack);

        var path = PathOf(pack);
        var part = path + ".part";

        try
        {
            Directory.CreateDirectory(Folder);

            long fetched = 0;
            string hash;
            await using (var into = File.Create(part))
            {
                using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using var from = await Open(pack.Url, cancel).ConfigureAwait(false);

                var buffer = new byte[1 << 20];
                int read;
                while ((read = await from.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
                {
                    hasher.AppendData(buffer, 0, read);
                    await into.WriteAsync(buffer.AsMemory(0, read), cancel).ConfigureAwait(false);
                    fetched += read;
                    progress?.Report(Math.Min(1.0, (double)fetched / pack.Size));
                }

                hash = Convert.ToHexStringLower(hasher.GetHashAndReset());
            }

            if (fetched != pack.Size || !hash.Equals(pack.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(part);
                return $"What {pack.Url} gave is not the pack {pack.Title}'s manifest names, {fetched} bytes with SHA-256 {hash} where {pack.Size} with {pack.Sha256.ToLowerInvariant()} were expected.";
            }

            File.Move(part, path, overwrite: true);
            progress?.Report(1.0);
            return null;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or UnauthorizedAccessException or OperationCanceledException or UriFormatException)
        {
            try
            {
                File.Delete(part);
            }
            catch (IOException)
            {
                // Left for the next fetch, which writes over it.
            }

            return error is OperationCanceledException
                ? $"Fetching {pack.Title} was stopped."
                : $"{pack.Title} could not be fetched from {pack.Url}. {error.Message}";
        }
    }

    /// <summary>
    /// Mounts a fetched pack under <c>packs/</c> and its name while an app runs, and answers the path
    /// its model is loaded by, or says why it cannot.
    /// </summary>
    /// <remarks>
    /// The pack is checked for its size and for the model its manifest names, not hashed again,
    /// since that was done as it arrived and would read the whole of it at every start.
    /// </remarks>
    /// <param name="pack">The manifest.</param>
    /// <param name="model">The model's path under the asset root, for <see cref="AssetServer"/>.</param>
    /// <param name="problem">Why it was not mounted, naming the file.</param>
    public static bool TryMount(ScenePack pack, [NotNullWhen(true)] out string? model, [NotNullWhen(false)] out string? problem)
    {
        ArgumentNullException.ThrowIfNull(pack);
        model = null;

        var path = PathOf(pack);
        if (!IsFetched(pack))
        {
            problem = $"{pack.Title} has not been fetched to {path}.";
            return false;
        }

        if (!AssetPack.TryOpen(path, out var opened, out problem)) return false;

        if (!opened.Contains(pack.Model))
        {
            opened.Dispose();
            problem = $"{path} holds no {pack.Model}, the model its manifest names.";
            return false;
        }

        var folder = $"{MountFolder}/{pack.Name}";
        AssetFiles.Mount(folder, opened);
        model = $"{folder}/{pack.Model}";
        return true;
    }

    /// <summary>Stops reading a mounted pack.</summary>
    /// <returns>Whether it was mounted.</returns>
    public static bool Unmount(ScenePack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        return AssetFiles.Unmount($"{MountFolder}/{pack.Name}");
    }

    /// <summary>The first field a manifest leaves out that a fetch needs, or nothing.</summary>
    private static string? Missing(ScenePack pack) =>
        pack.Url.Length == 0 ? "address"
        : pack.Size <= 0 ? "size"
        : pack.Sha256.Length != 64 || !pack.Sha256.All(Uri.IsHexDigit) ? "SHA-256"
        : pack.Model.Length == 0 ? "model"
        : null;

    /// <summary>The bytes at an address, over HTTP or from a file on this machine.</summary>
    private static async Task<Stream> Open(string url, CancellationToken cancel)
    {
        if (Path.IsPathRooted(url)) return File.OpenRead(url);

        var address = new Uri(url, UriKind.Absolute);
        if (address.IsFile) return File.OpenRead(address.LocalPath);

        var response = await Http.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
    }
}

/// <summary>How a manifest is read, generated so nothing reflects.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(ScenePack))]
internal sealed partial class ScenePackJson : JsonSerializerContext;

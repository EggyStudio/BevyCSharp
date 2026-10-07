using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Bevy;

/// <summary>
/// The directory a game keeps its own files in, named in a path by <c>user://</c>.
/// </summary>
/// <remarks>
/// <para>
/// Writable and the player's: settings, saves, anything the game writes. It is the platform's data
/// directory under the game's name, <c>$XDG_DATA_HOME</c> or <c>~/.local/share</c> on Linux,
/// <c>%APPDATA%</c> on Windows and <c>~/Library/Application Support</c> on macOS, so a game's
/// files sit where the platform's own tools and backups look for them. <c>assets://</c> is the
/// other root, the game's content, which a shipped game cannot write to.
/// </para>
/// <para>
/// The name is <see cref="Config.GameName"/>, or the title, set when an app starts. A test or a
/// tool can point <see cref="Root"/> somewhere of its own instead.
/// </para>
/// <para>
/// The managed side resolves these paths, and an app registers the directory with Bevy as its
/// <c>user</c> asset source as it starts, making it first, so a texture or a model the game wrote
/// there loads through <see cref="AssetServer"/> as <c>user://…</c>. The source is built once, so
/// a <see cref="Root"/> changed after an app starts reaches the managed side and not Bevy.
/// </para>
/// </remarks>
public static class UserData
{
    /// <summary>The prefix naming a path under the player's directory.</summary>
    public const string Prefix = "user://";

    private static string? _root;

    /// <summary>The name of the game, which its directory is called.</summary>
    /// <remarks>Characters a file name cannot hold are replaced, so any title makes a directory.</remarks>
    public static string Name { get; set; } = "BevyCSharp";

    /// <summary>
    /// The directory itself, the platform's data directory under the game's name unless set.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to go back to the platform's.</remarks>
    public static string Root
    {
        get => _root ?? Path.Combine(PlatformDirectory(), Safe(Name));
        set => _root = value;
    }

    /// <summary>The full path of a <c>user://</c> path, or of a path relative to the directory.</summary>
    public static string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path.StartsWith(Prefix, StringComparison.Ordinal)) path = path[Prefix.Length..];
        return Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Writes text to a file through a temporary one renamed over it.</summary>
    /// <remarks>
    /// The rename replaces the old file whole, so a crash while writing leaves the last version
    /// rather than half of the new one. The directory is made if it is not there.
    /// </remarks>
    public static void WriteAtomically(string full, ReadOnlySpan<byte> bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(full))!);

        var temporary = full + ".tmp";
        using (var file = File.Create(temporary)) file.Write(bytes);
        File.Move(temporary, full, overwrite: true);
    }

    /// <summary>The platform's directory for an application's own data.</summary>
    private static string PlatformDirectory() =>
        Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

    /// <summary>A name with what a file name cannot hold replaced.</summary>
    private static string Safe(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string([.. name.Select(character => invalid.Contains(character) ? '_' : character)]).Trim();
        return safe.Length == 0 ? "BevyCSharp" : safe;
    }
}

/// <summary>A persistent value as a save game carries it, whatever its type.</summary>
public interface IPersistentValue
{
    /// <summary>What it is called, which a save keys it by.</summary>
    string Name { get; }

    /// <summary>Writes the value as JSON.</summary>
    void Write(Utf8JsonWriter json);

    /// <summary>Replaces the value with the one JSON holds, reporting whether it could be read.</summary>
    /// <remarks>The value is changed and not written to its own file, as <c>Set</c> leaves it.</remarks>
    bool Read(JsonElement json);
}

/// <summary>
/// A value kept in a file of the player's own between runs, such as settings or progress.
/// </summary>
/// <typeparam name="T">What is kept.</typeparam>
/// <remarks>
/// <para>
/// Read when made, and written when <see cref="Persist"/> is called, as JSON through the type
/// information a <c>System.Text.Json</c> source-generated context gives, so nothing reflects and it
/// survives trimming and AOT:
/// </para>
/// <code>
/// [JsonSerializable(typeof(Settings))]
/// partial class GameJson : JsonSerializerContext;
///
/// var settings = new Persistent&lt;Settings&gt;("settings", GameJson.Default.Settings, () =&gt; new Settings());
/// settings.Update(value =&gt; value with { Volume = 0.5f });
/// settings.Persist();
/// </code>
/// <para>
/// What the file holds is read over the default, so a field the file does not name, as one a
/// later version of the game added, keeps the default's value rather than its type's zero. A
/// file that is missing or cannot be read gives the default instead, with the reason in
/// <see cref="Problem"/>, so a settings file broken by hand or by an older build starts the game
/// rather than stopping it. The file is not written until asked, so a default is not saved over a
/// file that only failed to read. Writes go through a temporary file renamed over the old one
/// (<see cref="UserData.WriteAtomically"/>).
/// </para>
/// <para>
/// The shape of Rust's <c>bevy-persistent</c>, which keeps a resource the same way. That one needs
/// a type the bridge can name, and a game's settings are C# types, so this is its managed twin.
/// </para>
/// </remarks>
public sealed class Persistent<T> : IPersistentValue
{
    private readonly JsonTypeInfo<T> _info;
    private readonly Func<T> _fallback;

    /// <summary>Reads the value from its file, or starts from the default.</summary>
    /// <param name="name">What it is called, which names its file when no path is given.</param>
    /// <param name="info">How to read and write it, from a source-generated context.</param>
    /// <param name="fallback">The value to start from when there is no file to read.</param>
    /// <param name="path">
    /// Where it is kept, as a <c>user://</c> path or an absolute one, or nothing for
    /// <c>user://</c> and the name with <c>.json</c> after it.
    /// </param>
    public Persistent(string name, JsonTypeInfo<T> info, Func<T> fallback, string? path = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(fallback);

        Name = name;
        _info = info;
        _fallback = fallback;

        var where = path ?? UserData.Prefix + name + ".json";
        FullPath = where.StartsWith(UserData.Prefix, StringComparison.Ordinal) || !Path.IsPathRooted(where)
            ? UserData.Resolve(where)
            : where;

        Value = Read();
    }

    /// <summary>What it is called.</summary>
    public string Name { get; }

    /// <summary>The file it is kept in.</summary>
    public string FullPath { get; }

    /// <summary>The value as it is now, which may hold changes not yet written.</summary>
    public T Value { get; private set; }

    /// <summary>Why the file could not be read the last time it was, or nothing when it was or was not there.</summary>
    public string? Problem { get; private set; }

    /// <summary>Whether the value has changed since it was last read or written.</summary>
    public bool Changed { get; private set; }

    /// <summary>Replaces the value, without writing it.</summary>
    public void Set(T value)
    {
        Value = value;
        Changed = true;
    }

    /// <summary>Changes the value through a function of the old one, without writing it.</summary>
    public void Update(Func<T, T> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Set(change(Value));
    }

    /// <inheritdoc/>
    void IPersistentValue.Write(Utf8JsonWriter json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonSerializer.Serialize(json, Value, _info);
    }

    /// <inheritdoc/>
    bool IPersistentValue.Read(JsonElement json)
    {
        try
        {
            if (Over(JsonNode.Parse(json.GetRawText())) is not { } value) return false;

            Set(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Writes the value to its file.</summary>
    public void Persist()
    {
        UserData.WriteAtomically(FullPath, JsonSerializer.SerializeToUtf8Bytes(Value, _info));
        Changed = false;
        Problem = null;
    }

    /// <summary>Throws away changes not yet written, by reading the file again.</summary>
    public void Revert() => Value = Read();

    /// <summary>Puts the default back and writes it, as a settings screen's reset does.</summary>
    public void Reset()
    {
        Value = _fallback();
        Persist();
    }

    /// <summary>The value the file holds, or the default with the reason it is not.</summary>
    private T Read()
    {
        Changed = false;
        Problem = null;

        if (!File.Exists(FullPath)) return _fallback();

        try
        {
            using var stream = File.OpenRead(FullPath);
            if (Over(JsonNode.Parse(stream)) is { } value) return value;

            Problem = $"{FullPath} holds null.";
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Problem = $"{FullPath} could not be read. {error.Message}";
        }

        return _fallback();
    }

    /// <summary>
    /// Reads a value from JSON laid over the default's own, so a field the JSON leaves out keeps
    /// the default's value.
    /// </summary>
    /// <remarks>
    /// A source-generated reader gives an init-only property the JSON leaves out the zero of its
    /// type rather than the value its initializer gives it, so a settings record that gained a
    /// field would read it as zero from every file written before. An object in both is laid over
    /// field by field the same way, and anything else the JSON holds takes the default's place
    /// whole, so a list is the file's own.
    /// </remarks>
    private T? Over(JsonNode? given)
    {
        if (given is not JsonObject fields || JsonSerializer.SerializeToNode(_fallback(), _info) is not JsonObject defaults)
            return given.Deserialize(_info);

        Lay(defaults, fields, _info.Options.PropertyNameCaseInsensitive);
        return defaults.Deserialize(_info);
    }

    /// <summary>Lays each field of <paramref name="over"/> on <paramref name="under"/>.</summary>
    private static void Lay(JsonObject under, JsonObject over, bool anyCase)
    {
        foreach (var (name, value) in over)
        {
            var key = anyCase
                ? under.Select(field => field.Key).FirstOrDefault(field => string.Equals(field, name, StringComparison.OrdinalIgnoreCase)) ?? name
                : name;

            if (value is JsonObject inner && under[key] is JsonObject beneath)
            {
                Lay(beneath, inner, anyCase);
                continue;
            }

            under[key] = value?.DeepClone();
        }
    }
}

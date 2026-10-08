using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Bevy;

namespace BevyCSharp.FeatureTest;

/// <summary>How the picture is smoothed at its edges, as the panel offers it.</summary>
public enum Smoothing
{
    /// <summary>Not at all.</summary>
    None,

    /// <summary>Fast approximate anti-aliasing, a pass over the finished picture.</summary>
    Fxaa,

    /// <summary>
    /// Subpixel morphological anti-aliasing, sharper than FXAA for a little more.
    /// </summary>
    Smaa,

    /// <summary>Temporal anti-aliasing, the smoothest, from the frames before.</summary>
    Taa,

    /// <summary>Four samples a pixel, on the geometry's edges alone.</summary>
    Msaa,
}

/// <summary>What is drawn behind the scene, as the effects page offers it.</summary>
public enum Backdrop
{
    /// <summary>The sky the air scatters from the sun, which tints the distance.</summary>
    Atmosphere,

    /// <summary>A cubemap of a dusk sky with stars, drawn in code, behind everything.</summary>
    Dusk,
}

/// <summary>A tier the graphics page sets several settings from at once.</summary>
public enum Quality
{
    /// <summary>No shadows, no bloom, FXAA.</summary>
    Low,

    /// <summary>Shadows and FXAA, no bloom.</summary>
    Medium,

    /// <summary>Shadows, bloom and SMAA.</summary>
    High,

    /// <summary>Shadows, bloom and TAA.</summary>
    Ultra,
}

/// <summary>Everything the admin panel sets, kept between runs.</summary>
/// <remarks>
/// A record read whole from the player's settings file as the program starts and written whole
/// each time the panel changes a field, through <see cref="Persistent{T}"/>, so a setting a tester
/// chose is the one the next run starts with. A field added later reads as its default from a file
/// written before it, since the file is read as far as its fields match.
/// </remarks>
public sealed record FeatureSettings
{
    /// <summary>The window's mode.</summary>
    public WindowMode Window { get; init; } = WindowMode.Windowed;

    /// <summary>
    /// Whether a frame waits for the display, which takes effect at the next start.
    /// </summary>
    public bool Vsync { get; init; } = true;

    /// <summary>How the edges are smoothed.</summary>
    public Smoothing Smoothing { get; init; } = Smoothing.Smaa;

    /// <summary>Whether the sun casts shadows.</summary>
    public bool Shadows { get; init; } = true;

    /// <summary>Whether bright things bleed light around them.</summary>
    public bool Bloom { get; init; } = true;

    /// <summary>
    /// Whether the app asks for Bevy's ray-traced lighting, Solari, which takes effect at the next
    /// start, since it makes every material deferred for the whole app.
    /// </summary>
    public bool RayTraced { get; init; }

    /// <summary>
    /// Whether the app asks for Bevy's meshlets, which takes effect at the next start, since the
    /// room the GPU keeps for their clusters is given as the app is made.
    /// </summary>
    public bool Meshlets { get; init; }

    /// <summary>The tier last chosen, which set the four above.</summary>
    public Quality Quality { get; init; } = Quality.High;

    /// <summary>How the picture's light is brought into what a screen can show.</summary>
    public Tonemapper Tonemapper { get; init; } = Tonemapper.TonyMcMapface;

    /// <summary>
    /// Whether corners and creases are darkened where ambient light reaches less, which needs the
    /// picture drawn once a pixel, so MSAA gives way to it.
    /// </summary>
    public bool AmbientOcclusion { get; init; }

    /// <summary>
    /// Whether smooth surfaces reflect what is on screen, which draws every material deferred and
    /// once a pixel, so MSAA gives way to it.
    /// </summary>
    public bool Reflections { get; init; }

    /// <summary>
    /// How what is out of focus is blurred, the focus on whatever the middle of the view rests on.
    /// </summary>
    public DepthOfFieldMode DepthOfField { get; init; } = DepthOfFieldMode.None;

    /// <summary>Whether what moves across the view is smeared along its way.</summary>
    public bool MotionBlur { get; init; }

    /// <summary>
    /// Whether the colors split toward the picture's edges, as a cheap lens splits them.
    /// </summary>
    public bool Aberration { get; init; }

    /// <summary>Whether the picture darkens toward its corners.</summary>
    public bool Vignette { get; init; } = true;

    /// <summary>Whether the exposure follows how bright the view is, as an eye adjusts.</summary>
    public bool AutoExposure { get; init; }

    /// <summary>
    /// How much the finished picture is sharpened, from none at zero to the most at one.
    /// </summary>
    public float Sharpen { get; init; }

    /// <summary>What is drawn behind the scene.</summary>
    public Backdrop Backdrop { get; init; } = Backdrop.Atmosphere;

    /// <summary>Every sound's loudness, from silent at zero to as recorded at one.</summary>
    public float Master { get; init; } = 1f;

    /// <summary>The music's loudness, under the master's.</summary>
    public float Music { get; init; } = 0.8f;

    /// <summary>The effects' loudness, under the master's.</summary>
    public float Effects { get; init; } = 1f;

    /// <summary>How far the view turns for the mouse or the stick, against the usual.</summary>
    public float LookSpeed { get; init; } = 1f;

    /// <summary>Whether moving the mouse up looks down.</summary>
    public bool InvertY { get; init; }

    /// <summary>
    /// Whether the frame time, the place and what the program holds are shown (F3).
    /// </summary>
    public bool Overlay { get; init; }

    /// <summary>Whether each collider is drawn as lines.</summary>
    public bool Colliders { get; init; }

    /// <summary>Whether the gizmos the zones draw are shown, the orbits among them.</summary>
    public bool Gizmos { get; init; } = true;

    /// <summary>Whether each mesh is drawn as its edges.</summary>
    public bool Wireframe { get; init; }

    /// <summary>How fast the game's clock runs, one being real time.</summary>
    public float TimeScale { get; init; } = 1f;

    /// <summary>The settings a tier gives, from these.</summary>
    public FeatureSettings At(Quality tier) => tier switch
    {
        Quality.Low => this with { Quality = tier, Shadows = false, Bloom = false, Smoothing = Smoothing.Fxaa },
        Quality.Medium => this with { Quality = tier, Shadows = true, Bloom = false, Smoothing = Smoothing.Fxaa },
        Quality.High => this with { Quality = tier, Shadows = true, Bloom = true, Smoothing = Smoothing.Smaa },
        _ => this with { Quality = tier, Shadows = true, Bloom = true, Smoothing = Smoothing.Taa },
    };
}

/// <summary>How the settings are read and written, generated so nothing reflects.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(FeatureSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;

/// <summary>The settings as the program holds them, read before the window opens.</summary>
/// <remarks>
/// Read before the app is made, since vsync and the window's mode are the window's from its
/// start, so the player's folder is named here first, under the name the app gives it too.
/// <see cref="Change"/> writes the file and tells whoever applies the setting.
/// </remarks>
public static class Settings
{
    /// <summary>The name the player's folder and the window go under.</summary>
    public const string GameName = "BevyCSharp Feature Test";

    private static Persistent<FeatureSettings>? _store;

    /// <summary>The settings as they are now.</summary>
    public static FeatureSettings Current => Store.Value;

    /// <summary>Told after each change, with the settings before it.</summary>
    public static event Action<FeatureSettings>? Changed;

    private static Persistent<FeatureSettings> Store
    {
        get
        {
            if (_store is not null) return _store;

            UserData.Name = GameName;
            _store = new Persistent<FeatureSettings>("feature-test", SettingsJson.Default.FeatureSettings, static () => new FeatureSettings());
            if (_store.Problem is { } problem) Console.WriteLine($"[Settings] {problem} The defaults are used.");
            return _store;
        }
    }

    /// <summary>Changes the settings, writes them, and tells whoever applies them.</summary>
    public static void Change(Func<FeatureSettings, FeatureSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var was = Current;
        Store.Update(change);
        if (Current == was) return;

        Store.Persist();
        Changed?.Invoke(was);
    }

    /// <summary>
    /// Reads one setting, or changes it as the panel would, for a tester at the console and for
    /// the drive script.
    /// </summary>
    /// <remarks>
    /// The settings go to JSON and back through the same generated context the file is kept with,
    /// so a setting is named as the file names it, in any case, and a value is given as the file
    /// holds it, a number, true or false, or an option's name, which is taken as a string when it
    /// is not JSON of its own.
    /// </remarks>
    [Command("setting", "Reads or changes one of the panel's settings, kept as the panel keeps it: setting <name> [value]")]
    internal static string Setting(string words)
    {
        const string Usage = "setting <name> [value]";
        var parts = words.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var fields = JsonSerializer.SerializeToNode(Current, SettingsJson.Default.FeatureSettings)!.AsObject();
        if (parts.Length == 0) return string.Join(", ", fields.Select(field => field.Key));

        var name = fields.Select(field => field.Key).FirstOrDefault(key => key.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            ConsoleHost.Fail("NO_SUCH_SETTING", $"There is no setting called {parts[0]}. The settings are {string.Join(", ", fields.Select(field => field.Key))}.");
            return Usage;
        }

        if (parts.Length == 1) return $"{name} = {fields[name]?.ToJsonString()}";

        JsonNode? value;
        try
        {
            value = JsonNode.Parse(parts[1]);
        }
        catch (JsonException)
        {
            value = JsonValue.Create(parts[1]);
        }

        fields[name] = value;
        FeatureSettings? changed;
        try
        {
            changed = fields.Deserialize(SettingsJson.Default.FeatureSettings);
        }
        catch (JsonException error)
        {
            ConsoleHost.Fail("BAD_VALUE", $"{parts[1]} is not a value {name} can take. {error.Message}");
            return Usage;
        }

        if (changed is null) return Usage;

        Change(_ => changed);
        return $"{name} = {JsonSerializer.SerializeToNode(Current, SettingsJson.Default.FeatureSettings)![name]?.ToJsonString()}";
    }
}

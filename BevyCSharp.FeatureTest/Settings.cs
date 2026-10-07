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

    /// <summary>The tier last chosen, which set the four above.</summary>
    public Quality Quality { get; init; } = Quality.High;

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
}

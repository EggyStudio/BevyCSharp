using Bevy;
using Diagnostics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The scene's distance field around the player, from the panel's debug page or the
/// <c>feature.field</c> command, in four cascades of a quarter meter, with the engine's window of
/// it beside the panel, its slice through the eye showing the hub's blocks and walls as the field
/// holds them.
/// </summary>
/// <remarks>
/// The field darkens the sun past its shadow map's reach, the engine's default, so the ground far
/// off keeps the shadows of the walls and trees beyond the map's last cascade. Global illumination,
/// from the panel's graphics page, traces its probes through the field, which it turns on where the
/// debug page left it off. While a scene pack stands, global illumination traces the field the
/// scene names (<see cref="Packs.Light"/>), which is asked again only when the scene or the
/// probes change, so a <c>field.set</c> typed between holds.
/// </remarks>
[Behavior]
public partial struct Field
{
    /// <summary>Whether the field is built and its window shown.</summary>
    internal static bool On { get; private set; }

    /// <summary>The scene's field last asked for, none where the feature test's own was.</summary>
    private static PackLight? _followed;

    /// <summary>Starts a run with the feature test's own field asked for.</summary>
    [OnStartup]
    public static void Start(BehaviorContext ctx)
    {
        On = false;
        _followed = null;
    }

    /// <summary>Builds the field and shows its window, or turns both off.</summary>
    internal static void Set(bool on)
    {
        On = on;
        Ask(_followed);
    }

    /// <summary>
    /// Asks for the field of the scene standing where global illumination is traced through it, or
    /// else the feature test's own, once each time that changes.
    /// </summary>
    internal static void Follow()
    {
        var fitted = Applied.Probed ? Packs.Light : null;
        if (fitted == _followed) return;
        _followed = fitted;
        Ask(fitted);
    }

    /// <summary>
    /// The field of four cascades a scene names, or the feature test's own of a quarter meter with
    /// none far, built while the debug page shows it or a scene's probes trace it.
    /// </summary>
    private static void Ask(PackLight? fitted) =>
        Render.SetSceneField(On || fitted is not null ? 4 : 0, fitted?.Cell ?? 0.25f, 1, fitted?.Far ?? 0f);

    /// <summary>Draws the field's window while it is on, inside the interface's frame.</summary>
    internal static void Draw()
    {
        if (On) Render.DrawSceneFieldWindow();
    }

    [Command("feature.field", "The scene's distance field in four cascades of a quarter meter with its window, or neither: feature.field <on|off>")]
    internal static string Command(string state)
    {
        Set(state is "on" or "1" or "true");
        return On ? "the scene's distance field is built in four cascades and its window shown" : "the scene's distance field is off";
    }
}

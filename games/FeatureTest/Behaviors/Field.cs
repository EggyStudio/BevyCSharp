using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The scene's distance field around the player, from the panel's debug page or the
/// <c>feature.field</c> command, in four cascades of a quarter meter, with the engine's window of
/// it beside the panel, its slice through the eye showing the hub's blocks and walls as the field
/// holds them.
/// </summary>
/// <remarks>
/// The field darkens the sun past its shadow map's reach, the engine's default, so the ground far
/// off keeps the shadows of the walls and trees beyond the map's last cascade.
/// </remarks>
[Behavior]
public partial struct Field
{
    /// <summary>Whether the field is built and its window shown.</summary>
    internal static bool On { get; private set; }

    /// <summary>Builds the field and shows its window, or turns both off.</summary>
    internal static void Set(bool on)
    {
        On = on;
        Render.SetSceneField(on ? 4 : 0, 0.25f, 1);
    }

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

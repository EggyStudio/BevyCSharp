using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The scenes yard's floor under the site a scene stands at, a brush entity of its own in the level
/// drawn to Sponza's outline, hidden while a scene that covers it stands and shown again when the
/// scene goes.
/// </summary>
/// <remarks>
/// <para>
/// A scene's ground stands two centimeters over the yard's floor (<c>Packs.Lift</c>), within
/// one of the scene field's cells, and the field paints a cell by its nearest surface whichever of
/// the two that is. Under Sponza the yard's white floor painted the cells of Sponza's stone floor
/// near white, and the light the probes took from it lit the walls under the arcades at twice the
/// reference's. A hidden entity stands out of the field as it stands out of the picture, so the
/// floor a scene covers is hidden while it stands, and its collider, two centimeters under the
/// scene's own floor, holds nothing the scene's does not.
/// </para>
/// <para>
/// The file uses nothing else of the game, since the test suite compiles it in for its test.
/// </para>
/// </remarks>
internal static class SceneFloor
{
    /// <summary>
    /// The class of the level's brush entity under the scenes' site, which the level's generator
    /// writes.
    /// </summary>
    internal const string Class = "func_scene_floor";

    /// <summary>
    /// Shows the floor under the scenes' site or hides it, answering how many entities of its class
    /// the level holds.
    /// </summary>
    internal static int Show(EcsWorld ecs, bool shown)
    {
        var floors = new List<Entity>();
        foreach (var row in ecs.Query<LevelKeys>(markChanged: false))
            if (row.Component.ClassName.Value == Class) floors.Add(row.Entity);

        foreach (var floor in floors) ecs.GetRef<Visibility>(floor) = shown ? Visibility.Inherited : Visibility.Hidden;
        return floors.Count;
    }
}

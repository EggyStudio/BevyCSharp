namespace Bevy;

/// <summary>
/// Writes a resource of the game's own into every scene file saved while the world holds it, and
/// puts it back as the scene loads, as Bevy writes a world's resources beside its entities.
/// </summary>
/// <remarks>
/// <para>
/// On a struct or a class the app's <see cref="World"/> holds as a resource, a score or the state of
/// a level, which a scene saves with the entities it belongs with. Its fields are written as a data
/// asset's are, a string, a list and a dictionary among them, under the scene's
/// <c>worldResources</c>, and <see cref="SceneFile.Load"/> sets it into the world, over the one it
/// held. A scene of a world holding none is written as before.
/// </para>
/// <para>
/// <see cref="FormerNameAttribute"/> and <see cref="DataVersionAttribute"/> work on it as on a
/// component, and <see cref="NotSavedAttribute"/> leaves a field out.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class SceneResourceAttribute : Attribute;

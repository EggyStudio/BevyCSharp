namespace Bevy;

/// <summary>
/// Leaves a field out of every file a component or a data asset is written to, as Bevy's
/// <c>skip_serializing</c> leaves one out of a scene.
/// </summary>
/// <remarks>
/// For a value worked out as the game runs rather than chosen, such as how long since something
/// started, which a scene saved now and loaded later would only restore wrong. A scene or a save
/// leaves the field out as it writes and leaves it at the value the type starts with as it loads,
/// a file that holds it from before included. The inspector still draws and edits it, and a script
/// reload keeps it, since the game runs on.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class NotSavedAttribute : Attribute;

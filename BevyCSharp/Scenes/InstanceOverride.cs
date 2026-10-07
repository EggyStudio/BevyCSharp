namespace Bevy;

/// <summary>
/// One change an instance makes to the scene it places.
/// </summary>
/// <param name="At">The node, as a path of names from the instance's root.</param>
/// <param name="Kind">What is done to it.</param>
/// <param name="Component">
/// The component's full name, as a scene file keys it, or nothing for <see cref="OverrideKind.Delete"/>.
/// </param>
/// <param name="Fields">
/// The fields written, as the JSON object a scene file holds for the component, or nothing where
/// the kind writes none. For <see cref="OverrideKind.Child"/>, the child, by its id in the file as
/// <c>#8</c> until the file's entities are spawned, then the entity's bits.
/// </param>
public sealed record InstanceOverride(string At, OverrideKind Kind, string? Component = null, string? Fields = null);

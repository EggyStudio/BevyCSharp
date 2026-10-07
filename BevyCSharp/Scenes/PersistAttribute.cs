namespace Bevy;

/// <summary>
/// Puts a component's fields in a save game.
/// </summary>
/// <remarks>
/// A save holds what changed while playing, so it writes only what is marked, the fields of a
/// component with this attribute, on an entity with a <see cref="SaveId"/>. A level's walls and
/// lights stay the scene's, so a save is small and a level fixed in an update reaches players with
/// saves from before it.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class PersistAttribute : Attribute;

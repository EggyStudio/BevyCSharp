namespace Bevy;

/// <summary>
/// A box the fog is thicker inside, a bank of mist in a hollow or smoke filling a room, by how far it
/// takes away 95% of a point's light inside it on top of the layer's.
/// </summary>
/// <param name="Center">Its middle, in world units.</param>
/// <param name="Size">How large it is along each axis, in world units.</param>
/// <param name="Visibility">How far inside it 95% of a point's light is taken away, in world units.</param>
/// <param name="Edge">Over how far inside its faces it thickens to that, in world units, so its sides do not show.</param>
public readonly record struct FogBox(Vec3 Center, Vec3 Size, float Visibility, float Edge = 2f);

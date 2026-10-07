namespace Bevy;

/// <summary>
/// Sent the frame after <see cref="SaveGame.Load"/> brought a save back, for a game to build what a
/// save does not hold.
/// </summary>
/// <param name="Path">The save that was loaded, as <see cref="SaveGame.Load"/> was given it.</param>
/// <param name="Entities">Everything the load spawned, the scenes' and the save's.</param>
/// <remarks>
/// <para>
/// A load ends the game in progress and spawns its scenes again with the save laid over them, and
/// enters no state, so what a game builds on entering one, such as the player's body or a camera
/// following it, is not built for what came back. A system reading this builds it once, where it
/// reads it, rather than looking every frame for what is missing.
/// </para>
/// <para>
/// The frame after rather than the frame of the load, as every message the engine sends arrives at
/// the top of a frame, so every system of that frame reads it and none reads it twice.
/// </para>
/// </remarks>
public readonly record struct SaveLoaded(string Path, IReadOnlyList<Entity> Entities);

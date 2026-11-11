namespace Bevy;

/// <summary>A strike of lightning, as <see cref="ThunderSettings.Struck"/> is told of it.</summary>
/// <param name="Position">Where the bolt landed, or for a strike in the cloud, the middle of its glow.</param>
/// <param name="Distance">How far from the camera, in meters.</param>
/// <param name="ThunderDelay">How many seconds until its thunder is heard, the distance over sound's 343 meters a second.</param>
/// <param name="ToGround">Whether it came down to the ground as a bolt rather than staying in the cloud.</param>
public readonly record struct LightningStrike(Vec3 Position, float Distance, float ThunderDelay, bool ToGround);

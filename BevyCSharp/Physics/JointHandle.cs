namespace Bevy.Physics;

/// <summary>A joint between two bodies, from <see cref="PhysicsWorld.Connect"/>.</summary>
/// <param name="Id">What the joint is known by.</param>
public readonly record struct JointHandle(int Id);

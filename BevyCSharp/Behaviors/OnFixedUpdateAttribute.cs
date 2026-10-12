namespace Bevy;

/// <summary>
/// Runs on Bevy's fixed timestep, zero or more times a frame, each covering the same slice of time.
/// </summary>
/// <remarks>
/// For anything whose results should not depend on the frame rate, which is most physical
/// behavior. Integrate with <see cref="Time.FixedDelta"/> rather than <see cref="Time.Delta"/>;
/// the rate is set by <see cref="Config.FixedHz"/> and defaults to 64 Hz.
/// </remarks>
/// <example>
/// <code>
/// [OnFixedUpdate]
/// public void Step(BehaviorContext ctx)
/// {
///     Velocity.Y -= 9.81f * ctx.Time.FixedDelta;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnFixedUpdateAttribute : Attribute;

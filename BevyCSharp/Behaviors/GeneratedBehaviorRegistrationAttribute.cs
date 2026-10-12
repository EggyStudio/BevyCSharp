namespace Bevy;

/// <summary>
/// Marks a generated method that registers behaviors, so
/// <see cref="BehaviorsPlugin"/> can find it by reflection.
/// </summary>
/// <remarks>Emitted by the generator. There is no reason to apply it by hand.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GeneratedBehaviorRegistrationAttribute : Attribute;

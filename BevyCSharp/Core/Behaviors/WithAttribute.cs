namespace Bevy;

/// <summary>
/// Restricts an instance method to entities that also carry all the listed components.
/// </summary>
/// <remarks>
/// Resolved per archetype rather than per entity, so the filter costs nothing in the loop.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// [With(typeof(Alive), typeof(Visible))]
/// public void Tick(BehaviorContext ctx) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class WithAttribute : Attribute
{
    /// <summary>The components that must be present.</summary>
    public Type[] Types { get; }

    /// <summary>Requires all of <paramref name="types"/>.</summary>
    public WithAttribute(params Type[] types) => Types = types;
}

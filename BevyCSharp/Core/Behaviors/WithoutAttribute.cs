namespace Bevy;

/// <summary>Skips entities carrying any of the listed components.</summary>
/// <remarks>Resolved per archetype, like <see cref="WithAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class WithoutAttribute : Attribute
{
    /// <summary>The components that must be absent.</summary>
    public Type[] Types { get; }

    /// <summary>Excludes any of <paramref name="types"/>.</summary>
    public WithoutAttribute(params Type[] types) => Types = types;
}

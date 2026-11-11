namespace Bevy;

/// <summary>
/// Runs the system before another system in its stage, named as a behavior's system is named,
/// <c>Behavior.Method</c>.
/// </summary>
/// <remarks>The other side of <see cref="AfterAttribute"/>, which says how the name is found.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class BeforeAttribute : Attribute
{
    /// <summary>The name of the system this one runs before.</summary>
    public string System { get; }

    /// <summary>Names the system this one runs before.</summary>
    public BeforeAttribute(string system) => System = system;
}

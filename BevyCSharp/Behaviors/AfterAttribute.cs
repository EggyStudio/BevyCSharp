namespace Bevy;

/// <summary>
/// Runs the system after another system in its stage, named as a behavior's system is named,
/// <c>Behavior.Method</c>.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's <c>.after</c>, through <see cref="SystemDescriptor.After"/>, for a system that reads what
/// another wrote this frame. Systems in one stage otherwise run in an order Bevy picks, which is
/// not the order they were written in and may change between runs. The name is looked up as the
/// app starts, and one that names nothing in the stage stops it with a message saying which, since
/// a misspelled name is otherwise an order that silently does not hold. Several may be given.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// [After("Gravity.Apply")]
/// public void Land(BehaviorContext ctx, ref Transform transform) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class AfterAttribute : Attribute
{
    /// <summary>The name of the system this one runs after.</summary>
    public string System { get; }

    /// <summary>Names the system this one runs after.</summary>
    public AfterAttribute(string system) => System = system;
}

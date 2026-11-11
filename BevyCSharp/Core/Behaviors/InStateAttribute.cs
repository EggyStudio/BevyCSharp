namespace Bevy;

/// <summary>
/// Restricts a method to frames where a state machine holds a given value.
/// </summary>
/// <remarks>
/// The state must have been added with <see cref="App.AddState{TState}"/>. This is a run
/// condition like <see cref="RunIfAttribute"/>, so it composes with the stage attributes rather
/// than replacing them, and the method does not run while the state is something else.
/// </remarks>
/// <example>
/// <code>
/// public enum Screen { Menu, Playing, Paused }
///
/// [OnUpdate]
/// [InState(Screen.Playing)]
/// public void Tick(BehaviorContext ctx) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InStateAttribute : Attribute
{
    /// <summary>The value the state must hold, as an enum member.</summary>
    public object State { get; }

    /// <summary>Restricts the method to <paramref name="state"/>.</summary>
    public InStateAttribute(object state) => State = state;
}

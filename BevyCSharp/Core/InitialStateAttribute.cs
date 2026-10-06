namespace Bevy;

/// <summary>
/// Declares an enum to be a state and where it starts, so an app that uses it adds it by itself.
/// </summary>
/// <remarks>
/// <para>
/// What <c>app.AddState(Mode.Menu)</c> says, said on the enum instead, for a state declared in a
/// behavior script, which has no <c>Program.cs</c> to add it from and is compiled by a host the
/// game, the editor's Play and the player all differ in. An app adds each declared state that one
/// of its systems uses, through <c>[InState]</c>, <c>[OnEnter]</c>, <c>[OnExit]</c> or
/// <c>[OnTransition]</c>, as it starts to run, and leaves one it already added alone. A sub-state
/// (<see cref="SubStateOfAttribute"/>) is added after its parents, starting at the value given each
/// time it comes into existence.
/// </para>
/// <para>
/// Registered by the source generator from a module initializer, so nothing reflects. A computed
/// state is not declared this way, since it needs its table or its rule.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [InitialState(Mode.Menu)]
/// public enum Mode { Menu, Playing }
/// </code>
/// </example>
/// <param name="value">Where the state starts, a value of the enum it is on.</param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class InitialStateAttribute(object value) : Attribute
{
    /// <summary>Where the state starts.</summary>
    public object Value { get; } = value;
}

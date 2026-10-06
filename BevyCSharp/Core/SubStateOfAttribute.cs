namespace Bevy;

/// <summary>
/// Declares an enum to be a sub-state of another, existing only while that one holds a value.
/// </summary>
/// <remarks>
/// <para>
/// A pause that only means anything during a run is a sub-state of the run, because leaving the run
/// should take the pause with it rather than leave a state nothing is looking at. While the parent
/// holds any other value the sub-state does not exist at all, and
/// <see cref="App.TryState{TState}"/> says so rather than answering with a default.
/// </para>
/// <para>
/// On the type rather than on the call that adds it, because which state a sub-state belongs to is
/// a property of the type in Bevy too, and because a behavior's <c>[OnEnter]</c> systems are
/// registered before an app has had the chance to add anything. A relationship declared here is
/// known whichever happens first.
/// </para>
/// <para>
/// A state carries a fixed number of sub-states, because Bevy names the parent as an associated
/// type and the types exist when the bridge is built. <see cref="StateRegistry.SubsPerSlot"/>
/// reports how many, and one past that is refused rather than half-worked.
/// </para>
/// <para>
/// Put on an enum more than once, it makes a sub-state of every state it names, which exists only
/// while each of them holds the value named, such as a pause that means something only while
/// playing online. Those are kept apart from the sub-states of one state, in a fixed set of their
/// own (<see cref="StateRegistry.JointSubCount"/>), each fed every state, so which states one
/// lives inside is chosen by the game.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public enum Screen { Menu, Playing }
///
/// [SubStateOf(typeof(Screen), Screen.Playing)]
/// public enum Pause { Off, On }
/// </code>
/// </example>
/// <param name="parent">The state enum this one lives inside.</param>
/// <param name="whileIn">The value of that state which this one exists under.</param>
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
public sealed class SubStateOfAttribute(Type parent, object whileIn) : Attribute
{
    /// <summary>The state enum this one lives inside.</summary>
    public Type Parent { get; } = parent;

    /// <summary>The value of that state which this one exists under.</summary>
    public object WhileIn { get; } = whileIn;
}

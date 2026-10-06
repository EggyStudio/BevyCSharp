namespace Bevy;

/// <summary>
/// Declares an enum as a state worked out from another rather than set.
/// </summary>
/// <remarks>
/// <para>
/// The third shape of state. A plain state is set, a sub-state exists while its parent holds one
/// value, and a computed state takes a value of its own from whatever its source holds. What that
/// is for is a fact that follows from another fact, such as whether the interface is up, which is
/// true on three screens and false on the rest. Writing it as a plain state leaves two facts to
/// keep in step, and one of them eventually lies.
/// </para>
/// <para>
/// The table is stated at <c>app.AddComputedState</c> rather than here, because it is a list of
/// pairs and an attribute carrying one would be a worse way to read the same thing. A source value
/// the table says nothing about means the computed state does not exist at all, so a system scoped
/// to it does not run and <c>App.TryState</c> answers false.
/// </para>
/// <para>
/// Setting one is always refused, because there is nothing to set. It changes when its source
/// does.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public enum Screen { Menu, Playing, Paused, Cutscene }
///
/// [ComputedFrom(typeof(Screen))]
/// public enum Hud { Hidden, Shown }
///
/// app.AddComputedState((Screen.Playing, Hud.Shown), (Screen.Paused, Hud.Shown));
/// </code>
/// </example>
/// <param name="source">The state enum this one is worked out from.</param>
/// <param name="more">
/// Further states it is worked out from at once, which makes it a joint state, worked out by a
/// rule of the game's own over all of them (<c>app.AddComputedState&lt;T, A, B&gt;(rule)</c>).
/// </param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class ComputedFromAttribute(Type source, params Type[] more) : Attribute
{
    /// <summary>The state enum this one is worked out from, the first of several for a joint.</summary>
    public Type Source { get; } = source;

    /// <summary>Every state enum this one is worked out from, in the order they were named.</summary>
    public IReadOnlyList<Type> Sources { get; } = [source, .. more];

    /// <summary>Whether this is worked out from more than one state, as a joint state is.</summary>
    public bool IsJoint => Sources.Count > 1;
}

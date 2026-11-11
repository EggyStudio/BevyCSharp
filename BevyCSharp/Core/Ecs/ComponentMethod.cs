namespace Bevy;

/// <summary>
/// One thing a component can be told to do.
/// </summary>
/// <param name="Name">The method's name, which the button shows.</param>
/// <param name="Run">Calls it on an entity's copy of the component and writes the result back.</param>
/// <remarks>
/// A method with no arguments on a component struct. Anything else has no obvious button, because
/// a method that needs values needs a form, and a method that needs the world is a system rather
/// than something a person presses once.
/// </remarks>
public sealed record ComponentMethod(string Name, Action<EcsWorld, Entity> Run)
{
    /// <summary>What the method's attributes asked for.</summary>
    public MethodHints Hints { get; init; } = MethodHints.None;

    /// <summary>What the button says, which is its label when it has one.</summary>
    public string Title => Hints.Label is { Length: > 0 } label ? label : Name;
}

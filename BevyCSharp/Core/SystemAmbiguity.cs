namespace Bevy;

/// <summary>
/// Two systems of one stage whose declared access conflicts and whose order nothing states, from
/// <see cref="App.AmbiguitiesIn"/>.
/// </summary>
/// <param name="First">The one added first.</param>
/// <param name="Second">The other.</param>
/// <param name="Conflicts">
/// The resource types one writes and the other reads or writes, as
/// <see cref="SystemDescriptor.Read{T}"/> and <see cref="SystemDescriptor.Write{T}"/> declared them.
/// </param>
public readonly record struct SystemAmbiguity(string First, string Second, IReadOnlyList<Type> Conflicts);

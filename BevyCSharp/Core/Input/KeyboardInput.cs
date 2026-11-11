namespace Bevy;

/// <summary>A key going down or coming up, as Bevy's <c>KeyboardInput</c> message carries it.</summary>
/// <param name="KeyCode">Where the key is, or null for one outside <see cref="Key"/>'s table.</param>
/// <param name="LogicalKey">What the key reads as in the keyboard's layout.</param>
/// <param name="State">Whether it went down or came up.</param>
/// <param name="Text">What it typed, or null where it typed nothing, as a release never does.</param>
/// <param name="Repeat">Whether the platform repeats a key held down rather than a hand pressing it again.</param>
/// <remarks>
/// Reached as the <c>Input</c> of a <see cref="FocusedInput{TInput}"/>, a key handed to the entity
/// with the input focus. A logical key the platform could not identify reads as a named key with
/// an empty name.
/// </remarks>
public readonly record struct KeyboardInput(Key? KeyCode, LogicalKey LogicalKey, ButtonState State, string? Text, bool Repeat);

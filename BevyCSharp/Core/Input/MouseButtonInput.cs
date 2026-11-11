namespace Bevy;

/// <summary>A mouse button going down or coming up, as Bevy's <c>MouseButtonInput</c> message carries it.</summary>
/// <param name="Button">Which button.</param>
/// <param name="State">Whether it went down or came up.</param>
/// <param name="Window">The window it happened over.</param>
/// <remarks>
/// Read with <c>ctx.Read&lt;MouseButtonInput&gt;()</c>, one for each change in the order it came,
/// where <see cref="Input.MousePressed"/> says only what went down this frame. A button beyond
/// <see cref="MouseButton"/>'s five sends none.
/// </remarks>
public readonly record struct MouseButtonInput(MouseButton Button, ButtonState State, Entity Window);

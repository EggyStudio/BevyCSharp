namespace Bevy;

/// <summary>
/// The part of a text field's text it shows, in the text's own layout units, for
/// <see cref="Ui.TextViewportOf"/>.
/// </summary>
/// <param name="Offset">Where its top left corner is, across and down from the text's.</param>
/// <param name="Size">How wide and how tall it is.</param>
public readonly record struct TextViewport(Vec2 Offset, Vec2 Size);

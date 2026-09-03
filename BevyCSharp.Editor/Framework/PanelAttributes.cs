namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Marks a class as a panel backed by an HTML document.
/// </summary>
/// <remarks>
/// <para>
/// A panel is three files. The structure is HTML, the appearance is CSS, and this class is the
/// behavior: what the elements are bound to and what the buttons do. The class has to be
/// <see langword="partial"/>, because the wiring between the three is generated into it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EditorPanel("panels/settings.html")]
/// public sealed partial class SettingsPanel
/// {
///     [Bind("#bloom")]     public bool Bloom;
///     [Bind("#intensity")] public float Intensity;
///     [Command("#apply")]  public void Apply() { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EditorPanelAttribute(string document) : Attribute
{
    /// <summary>The document's path, relative to the asset root.</summary>
    public string Document { get; } = document;
}

/// <summary>Which way a binding carries a value.</summary>
public enum BindMode
{
    /// <summary>
    /// The field follows the element and the element follows the field.
    /// </summary>
    /// <remarks>The default wherever the member can be written to.</remarks>
    TwoWay = 0,

    /// <summary>The element follows the field, and an edit on screen is overwritten.</summary>
    /// <remarks>What a readout wants: a frame counter, a status line, a computed total.</remarks>
    OneWay = 1,
}

/// <summary>
/// Ties a field or property to the element carrying a CSS id.
/// </summary>
/// <remarks>
/// <para>
/// Which of the element's values is used follows the member's type: a <see cref="bool"/> ticks a
/// checkbox, a number moves a slider, and a string is text. Anything else is refused when the
/// panel is compiled rather than ignored when it runs.
/// </para>
/// <para>
/// A get-only property is one way whatever the mode says, since there is nowhere to put an edit.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BindAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;

    /// <summary>Which way the value travels.</summary>
    public BindMode Mode { get; init; } = BindMode.TwoWay;
}

/// <summary>
/// Runs a method when the element carrying a CSS id is clicked.
/// </summary>
/// <remarks>
/// The method takes no arguments and returns nothing. Which element was clicked is already known,
/// since it is the one the attribute names.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;
}

using System.Collections.Generic;

namespace Bevy.Generator;

/// <summary>Which of an element's values a binding uses.</summary>
internal enum BindKind
{
    /// <summary>Ticked or not: a checkbox, a switch, a toggle.</summary>
    Flag,

    /// <summary>A number: a slider.</summary>
    Number,

    /// <summary>Text: an input's value, or an element's inner text.</summary>
    Text,

    /// <summary>Whether the element is on screen at all.</summary>
    /// <remarks>
    /// Not a value the widget carries but something done to it, which is why it has its own
    /// attribute. It is what makes a document with a fixed set of elements show a list of a
    /// length nobody knew when the document was written.
    /// </remarks>
    Visible,
}

/// <summary>One member tied to one element.</summary>
/// <param name="Element">The CSS id, without its leading hash.</param>
/// <param name="Member">The field or property name.</param>
/// <param name="Kind">Which of the element's values it uses.</param>
/// <param name="TwoWay">Whether an edit on screen travels back to the member.</param>
/// <param name="NumericType">
/// The member's type when <paramref name="Kind"/> is <see cref="BindKind.Number"/>, so the
/// emitted code can convert back to it. An element's number is always a float.
/// </param>
/// <param name="Count">
/// How many elements the id stands for. Zero is the ordinary case, one member and one element.
/// Anything higher means the id is a prefix: <c>row</c> with a count of eight is <c>row-0</c>
/// through <c>row-7</c>, and the member is an array with a value for each.
/// </param>
internal sealed record PanelBindingModel(
    string Element,
    string Member,
    BindKind Kind,
    bool TwoWay,
    string? NumericType,
    int Count = 0);

/// <summary>One method tied to a click on one element.</summary>
/// <param name="Element">The CSS id, without its leading hash.</param>
/// <param name="Method">The method to call.</param>
/// <param name="Count">
/// How many elements the id stands for, as on a binding. When it is not zero the method is
/// handed the index of the one that was clicked.
/// </param>
internal sealed record PanelCommandModel(string Element, string Method, int Count = 0);

/// <summary>Which button a bound method answers.</summary>
internal enum ClickButton
{
    /// <summary>The ordinary one: doing the thing.</summary>
    Primary,

    /// <summary>The secondary one: asking what can be done.</summary>
    Secondary,
}

/// <summary>What a panel declared about where it sits and what dismisses it.</summary>
/// <param name="Root">The CSS id of the panel's outermost element, or null.</param>
/// <param name="Handle">The CSS id of the element it is dragged by, or null.</param>
/// <param name="Dock">The <c>EditorDock</c> it belongs to, as its enum value.</param>
/// <param name="X">Its offset within that dock, or its left edge when floating.</param>
/// <param name="Y">The same, vertically.</param>
/// <param name="Width">How wide, or <c>NaN</c> for whatever the stylesheet says.</param>
/// <param name="Height">How tall, or <c>NaN</c> for as tall as its contents.</param>
/// <param name="Order">Where it sits among its dock's other panels.</param>
/// <param name="Fill">Whether it takes what its dock has left over.</param>
/// <param name="Dismiss">The <c>PanelDismiss</c> value it asked for.</param>
/// <param name="Layer">Which panels it draws in front of.</param>
internal sealed record PanelChromeModel(
    string? Root,
    string? Handle,
    int Dock,
    float X,
    float Y,
    float Width,
    float Height,
    int Order,
    bool Fill,
    int Dismiss,
    int Layer);

/// <summary>Everything the emitter needs about one panel.</summary>
internal sealed record PanelModel(
    string? Namespace,
    string Name,
    string Document,
    PanelChromeModel Chrome,
    IReadOnlyList<PanelBindingModel> Bindings,
    IReadOnlyList<PanelCommandModel> Commands,
    IReadOnlyList<PanelCommandModel> ContextCommands,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Refreshed)
{
    /// <summary>The panel's fully qualified name.</summary>
    internal string QualifiedName => Namespace is null ? Name : $"{Namespace}.{Name}";

    /// <summary>A name unique enough to key a generated file by.</summary>
    internal string UniqueKey => QualifiedName.Replace('.', '_');
}

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
internal sealed record PanelBindingModel(
    string Element,
    string Member,
    BindKind Kind,
    bool TwoWay,
    string? NumericType);

/// <summary>One method tied to a click on one element.</summary>
internal sealed record PanelCommandModel(string Element, string Method);

/// <summary>Everything the emitter needs about one panel.</summary>
internal sealed record PanelModel(
    string? Namespace,
    string Name,
    string Document,
    IReadOnlyList<PanelBindingModel> Bindings,
    IReadOnlyList<PanelCommandModel> Commands)
{
    /// <summary>The panel's fully qualified name.</summary>
    internal string QualifiedName => Namespace is null ? Name : $"{Namespace}.{Name}";

    /// <summary>A name unique enough to key a generated file by.</summary>
    internal string UniqueKey => QualifiedName.Replace('.', '_');
}

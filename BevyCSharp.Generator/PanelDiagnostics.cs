using Microsoft.CodeAnalysis;

namespace Bevy.Generator;

/// <summary>
/// The diagnostics the panel generator reports.
/// </summary>
/// <remarks>
/// A panel binds a name in C# to an id in a file the compiler never sees, so the compiler cannot
/// check that the id exists. What it can check is everything on this side: that the class can be
/// generated into, that a bound member has a type a widget can hold, and that a command looks
/// like something callable. Each of those would otherwise surface as a panel that opens and does
/// nothing.
/// </remarks>
internal static class PanelDiagnostics
{
    private const string Category = "BevyCSharp";

    /// <summary>BCS020: the class needs <c>partial</c>.</summary>
    internal static readonly DiagnosticDescriptor NotPartial = new(
        id: "BCS020",
        title: "Panel class must be partial",
        messageFormat:
        "'{0}' is marked [EditorPanel] but is not partial, so its bindings cannot be generated "
        + "into it. Add the 'partial' modifier.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS021: nothing is bound, so the panel is a document and no behavior.</summary>
    internal static readonly DiagnosticDescriptor NothingBound = new(
        id: "BCS021",
        title: "Panel binds nothing",
        messageFormat:
        "'{0}' is marked [EditorPanel] but has no [Bind] or [Command] members, so it will open "
        + "its document and do nothing else. Add a [Bind] member or a [Command] method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>BCS022: the member's type is not one a widget carries.</summary>
    internal static readonly DiagnosticDescriptor UnsupportedBindType = new(
        id: "BCS022",
        title: "Bound member has a type no widget can hold",
        messageFormat:
        "'{0}' is bound to '{1}' but has type '{2}'. An element holds a bool, a floating point "
        + "or integer number, or a string.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS023: the method cannot be called with what the panel has.</summary>
    internal static readonly DiagnosticDescriptor UnsupportedCommand = new(
        id: "BCS023",
        title: "Panel method cannot be called",
        messageFormat:
        "'{0}' does not take what the panel has to give it. A method bound to a click, a change "
        + "or a refresh takes nothing, because everything it needs is already on the panel. The "
        + "one exception is a command over a repeated element, which takes a single int for "
        + "which of them was clicked.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS024: two members claim the same element.</summary>
    internal static readonly DiagnosticDescriptor DuplicateElement = new(
        id: "BCS024",
        title: "Two members bind the same element",
        messageFormat:
        "'{0}' and '{1}' both bind '{2}'. One element carries one value, so the second binding "
        + "would overwrite the first every frame.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS025: a repeated binding needs somewhere to keep a value per element.</summary>
    internal static readonly DiagnosticDescriptor RepeatedNeedsArray = new(
        id: "BCS025",
        title: "Repeated binding must be an array",
        messageFormat:
        "'{0}' binds '{1}' with Count = {2}, so it stands for that many elements and needs an "
        + "array to keep a value for each. Give it an array type, or drop Count.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

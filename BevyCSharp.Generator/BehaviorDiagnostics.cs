using Microsoft.CodeAnalysis;

namespace Bevy.Generator;

/// <summary>
/// The diagnostics the generator reports.
/// </summary>
/// <remarks>
/// These exist because the failure modes here are otherwise baffling. A behavior struct that
/// silently never runs, or a <c>[RunIf]</c> pointing at a renamed member, would surface as
/// "nothing happens" at runtime. Catching them at compile time turns each into a squiggle on
/// the exact line that is wrong.
/// </remarks>
internal static class BehaviorDiagnostics
{
    private const string Category = "BevyCSharp";

    /// <summary>BCS001: the struct needs <c>partial</c>.</summary>
    internal static readonly DiagnosticDescriptor NotPartial = new(
        id: "BCS001",
        title: "Behavior struct must be partial",
        messageFormat:
        "'{0}' is marked [Behavior] but is not partial, so the generated runner cannot be "
        + "attached. Add the 'partial' modifier.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS002: no stage methods, so the behavior does nothing.</summary>
    internal static readonly DiagnosticDescriptor NoStageMethods = new(
        id: "BCS002",
        title: "Behavior has no stage methods",
        messageFormat:
        "'{0}' is marked [Behavior] but has no methods with a stage attribute, so nothing will "
        + "be scheduled. Add [OnUpdate] (or another stage attribute) to a method.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>BCS003: the method signature does not match what a system needs.</summary>
    internal static readonly DiagnosticDescriptor BadSignature = new(
        id: "BCS003",
        title: "Behavior method has the wrong signature",
        messageFormat:
        "'{0}' carries a stage attribute, so it must return void and take exactly one "
        + "BehaviorContext parameter. Change the signature to 'void {0}(BehaviorContext ctx)'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS004: <c>[RunIf]</c> names something that is not there.</summary>
    internal static readonly DiagnosticDescriptor UnknownCondition = new(
        id: "BCS004",
        title: "RunIf condition not found",
        messageFormat:
        "[RunIf(\"{0}\")] on '{1}' does not match any static bool member on the behavior "
        + "struct. Use nameof(...) so a rename cannot break it.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS005: a filter names a type that cannot be a Bevy component.</summary>
    internal static readonly DiagnosticDescriptor FilterNotUnmanaged = new(
        id: "BCS005",
        title: "Component filter type must be an unmanaged struct",
        messageFormat:
        "'{0}' is used as a component filter but is not an unmanaged struct. Components are "
        + "stored in Bevy's tables, so they must contain no references.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS006: the struct itself cannot be stored as a component.</summary>
    internal static readonly DiagnosticDescriptor BehaviorNotUnmanaged = new(
        id: "BCS006",
        title: "Behavior struct must be unmanaged",
        messageFormat:
        "'{0}' has instance stage methods, so it is stored as a Bevy component, but it is not "
        + "an unmanaged struct. Remove reference-typed fields, or make the stage methods static.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>BCS007: two stage attributes on one method.</summary>
    internal static readonly DiagnosticDescriptor MultipleStages = new(
        id: "BCS007",
        title: "Behavior method has more than one stage attribute",
        messageFormat:
        "'{0}' carries more than one stage attribute. A method runs in exactly one stage; split "
        + "it if you need it in two.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

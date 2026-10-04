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

    // BCS002 warned about a behavior with neither stage methods nor fields. It is retired, since
    // such a behavior is a marker an entity carries, and its id is not given to another.

    /// <summary>BCS003: the method signature does not match what a system needs.</summary>
    internal static readonly DiagnosticDescriptor BadSignature = new(
        id: "BCS003",
        title: "Behavior method has the wrong signature",
        messageFormat:
        "'{0}' carries a stage attribute, so it must return void and take a BehaviorContext, "
        + "followed in an instance method by up to two other components of the entity, each ref or "
        + "in and each a different struct, as in 'void {0}(BehaviorContext ctx, ref Transform transform)'.",
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

    /// <summary>BCS008: a version with nothing to bring an older file up to it.</summary>
    internal static readonly DiagnosticDescriptor VersionWithoutMigrate = new(
        id: "BCS008",
        title: "A [DataVersion] type has no Migrate method",
        messageFormat:
        "'{0}' has [DataVersion({1})] but no public or internal 'static JsonObject Migrate(int from, "
        + "JsonObject value)', so a file written at an earlier version is read as it is",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>BCS009: a transition between values of two different enums.</summary>
    internal static readonly DiagnosticDescriptor TransitionAcrossStates = new(
        id: "BCS009",
        title: "OnTransition names values of two different states",
        messageFormat:
        "[OnTransition] on '{0}' names a value of {1} and a value of {2}, and a state moves only "
        + "between its own values, so it would never run. Name two values of the same enum.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

using System.Collections.Generic;
using System.Linq;

namespace Bevy.Generator;

/// <summary>The scheduling stage a method asked for. Mirrors <c>Bevy.Stage</c>.</summary>
internal enum BehaviorStage
{
    Startup,
    First,
    PreUpdate,
    FixedUpdate,
    Update,
    PostUpdate,
    Render,
    Last,
    Cleanup,
}

/// <summary>How a <c>[RunIf]</c> target is reached.</summary>
internal enum ConditionKind
{
    /// <summary>A static method taking a <c>World</c> and returning bool.</summary>
    Method,

    /// <summary>A static bool property.</summary>
    Property,

    /// <summary>A static bool field.</summary>
    Field,
}

/// <summary>Component filters extracted from a method's attributes.</summary>
/// <param name="With">Fully qualified component types the entity must carry.</param>
/// <param name="Without">Fully qualified component types the entity must not carry.</param>
/// <param name="Changed">Fully qualified component types of which one must have changed.</param>
internal sealed record BehaviorFilters(
    IReadOnlyList<string> With,
    IReadOnlyList<string> Without,
    IReadOnlyList<string> Changed)
{
    /// <summary>An empty filter set.</summary>
    public static readonly BehaviorFilters None = new([], [], []);

    /// <summary>True when no filter is declared.</summary>
    public bool IsEmpty => With.Count == 0 && Without.Count == 0 && Changed.Count == 0;

    /// <summary>Value equality over the contents, so incremental caching works.</summary>
    public bool Equals(BehaviorFilters? other) =>
        other is not null
        && With.SequenceEqual(other.With)
        && Without.SequenceEqual(other.Without)
        && Changed.SequenceEqual(other.Changed);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var value in With.Concat(Without).Concat(Changed))
            hash = hash * 31 + value.GetHashCode();

        return hash;
    }
}

/// <summary>A keyboard toggle declared with <c>[ToggleKey]</c>.</summary>
/// <param name="Key">The toggling key's enum value.</param>
/// <param name="Modifiers">
/// The combined <c>KeyModifier</c> flags. A combination such as <c>Ctrl | Shift</c> reaches the
/// generator already folded into one constant, so several modifiers need no special handling.
/// </param>
/// <param name="DefaultEnabled">Whether the system starts enabled.</param>
internal sealed record ToggleKeyInfo(int Key, int Modifiers, bool DefaultEnabled);

/// <summary>A <c>[RunIf]</c> condition.</summary>
/// <param name="MemberName">Name of the member on the behavior struct.</param>
/// <param name="Kind">How to reach it.</param>
internal sealed record ConditionInfo(string MemberName, ConditionKind Kind);

/// <summary>One stage-annotated method on a behavior struct.</summary>
internal sealed record StageMethod
{
    /// <summary>The stage the method runs in.</summary>
    public BehaviorStage Stage { get; init; }

    /// <summary>The method's name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// True for a static method (one system per frame); false for an instance method
    /// (one invocation per entity carrying the behavior).
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>Component filters; only meaningful for instance methods.</summary>
    public BehaviorFilters Filters { get; init; } = BehaviorFilters.None;

    /// <summary>An optional run condition.</summary>
    public ConditionInfo? Condition { get; init; }

    /// <summary>An optional keyboard toggle.</summary>
    public ToggleKeyInfo? Toggle { get; init; }
}

/// <summary>A <c>[Behavior]</c> struct and everything the generator needs to emit for it.</summary>
internal sealed record BehaviorModel
{
    /// <summary>The struct's namespace, or <c>null</c> for the global namespace.</summary>
    public string? Namespace { get; init; }

    /// <summary>The struct's simple name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The fully qualified struct name, with a <c>global::</c> prefix.</summary>
    public string QualifiedName { get; init; } = string.Empty;

    /// <summary>Name of the generated runner class.</summary>
    public string RunnerName => Name + "_Behavior";

    /// <summary>A globally unique, identifier-safe key for this behavior.</summary>
    public string UniqueKey =>
        (Namespace is null ? Name : Namespace + "." + Name).Replace('.', '_');

    /// <summary>The stage methods found on the struct.</summary>
    public IReadOnlyList<StageMethod> Methods { get; init; } = [];

    /// <summary>Value equality over the contents, so incremental caching works.</summary>
    public bool Equals(BehaviorModel? other) =>
        other is not null
        && Namespace == other.Namespace
        && Name == other.Name
        && QualifiedName == other.QualifiedName
        && Methods.SequenceEqual(other.Methods);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = QualifiedName.GetHashCode();
        foreach (var method in Methods)
            hash = hash * 31 + method.GetHashCode();

        return hash;
    }
}

namespace Bevy;

/// <summary>
/// Puts a field inside a fold, which can be opened and shut.
/// </summary>
/// <remarks>
/// <para>
/// A component with twenty fields is unreadable however well it is ordered, and the answer every
/// editor arrives at is the same. The eight that are wanted go at the top and the rest fold away.
/// Consecutive fields naming the same fold share it.
/// </para>
/// <para>
/// Folds nest, written as a path, so <c>[Foldout("Advanced/Debug")]</c> is a fold inside a fold
/// and it goes as deep as somebody writes slashes. Whether a fold is open is remembered per
/// component rather than per entity, because somebody who shut one meant it about the component.
/// </para>
/// </remarks>
/// <param name="path">What the fold is called, with slashes between the levels.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class FoldoutAttribute(string path) : Attribute
{
    /// <summary>What the fold is called, with slashes between the levels.</summary>
    public string Path { get; } = path;

    /// <summary>Whether it starts open the first time it is seen.</summary>
    public bool Open { get; init; } = true;
}

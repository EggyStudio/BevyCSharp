namespace Bevy;

/// <summary>
/// A wrapper over one of Bevy's components, as code that knows only its type argument finds the
/// component it names and makes one over an entity.
/// </summary>
/// <typeparam name="TSelf">The wrapper itself.</typeparam>
/// <remarks>
/// <see cref="IReflectedComponent{TSelf}"/> says the same through static members, which only a
/// generic constrained to it reaches. An observer of <see cref="Add{T}"/> and its kin takes a C#
/// component or a wrapper alike, so it is constrained to neither, and asks a default value whether
/// it is a wrapper instead. The generated wrappers implement both.
/// </remarks>
internal interface IReflectedWrapper<TSelf> where TSelf : struct
{
    /// <summary>The component's full Rust type path.</summary>
    string ComponentPath { get; }

    /// <summary>Makes a wrapper over the component on <paramref name="entity"/>.</summary>
    TSelf Over(EcsWorld world, Entity entity);
}

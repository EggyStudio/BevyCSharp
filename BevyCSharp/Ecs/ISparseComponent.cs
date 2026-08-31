namespace Bevy;

/// <summary>
/// Marks a component as one Bevy should keep in a sparse set rather than in a table.
/// </summary>
/// <remarks>
/// <para>
/// A table-stored component sits in a contiguous column, which is what makes iteration fast: a
/// query walks it with no indirection. The cost is paid on insertion and removal, because both
/// move the entity to a different archetype and copy every one of its other components along
/// with it.
/// </para>
/// <para>
/// A sparse set inverts that trade. Adding or removing costs nothing but an index write, and
/// nothing else about the entity moves; in exchange the values are not laid out for a fast walk.
/// It suits a tag that is toggled far more often than it is iterated: a per-frame
/// <c>Stunned</c> or <c>Colliding</c> that is filtered on rather than read.
/// </para>
/// <para>
/// <b>Such a component cannot be the one iterated.</b> Bevy exposes no way to reach the dense
/// storage behind a sparse set, only a per-entity lookup, so there is nothing for a chunk to
/// point at. <see cref="EcsWorld.Query{T}"/> and <see cref="EcsWorld.Chunks{T}(ReadOnlySpan{int},
/// ReadOnlySpan{int}, bool)"/> refuse it. Everything else works:
/// <see cref="EcsWorld.Add{T}"/>, <see cref="EcsWorld.Remove{T}"/>,
/// <see cref="EcsWorld.Has{T}"/>, <see cref="EcsWorld.GetRef{T}"/>,
/// <see cref="EcsWorld.Count{T}"/>, and naming it in a <c>[With]</c> or <c>[Without]</c> filter
/// on some other query, which is what it is for.
/// </para>
/// <para>
/// Storage is fixed when the type is first registered, so implementing this later in a run has
/// no effect until the next <see cref="App"/>. It is ignored on a type that also implements
/// <see cref="INativeComponent"/>, because Bevy already chose the storage for its own components.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Added and removed constantly, read only as a filter.
/// public struct Colliding : ISparseComponent;
///
/// [OnUpdate]
/// [Without(typeof(Colliding))]
/// public void Fall(BehaviorContext ctx) { }
/// </code>
/// </example>
public interface ISparseComponent;

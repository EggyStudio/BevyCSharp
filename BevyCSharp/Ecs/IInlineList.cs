using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A list held inside a component's own bytes, of up to a fixed number of items.
/// </summary>
/// <typeparam name="T">What the list holds.</typeparam>
/// <remarks>
/// <para>
/// A component lives in Bevy's storage as bytes, so it cannot hold a <see cref="List{T}"/>, whose
/// memory the garbage collector moves and frees. An inline list is a count and a fixed run of
/// items, all of it the component's own bytes, so it iterates in chunks with everything else, costs
/// nothing to keep, and needs nothing freed when the entity goes. It suits a list with a bound
/// known in advance: waypoints, inventory slots, the last few hits.
/// </para>
/// <para>
/// The capacity is part of the type (<see cref="InlineList4{T}"/>, <see cref="InlineList8{T}"/> and
/// on to <see cref="InlineList64{T}"/>), because C# fixes an inline array's length at compile time
/// and cannot take it as a type parameter. The inspector draws any of them as a list, and a scene
/// writes one as an array.
/// </para>
/// </remarks>
public interface IInlineList<T> where T : unmanaged
{
    /// <summary>How many items the list holds.</summary>
    int Count { get; }

    /// <summary>How many it can hold.</summary>
    int Capacity { get; }

    /// <summary>One item, by its place in the list, as a copy.</summary>
    /// <remarks>
    /// A copy rather than the span the concrete types offer as <c>Items</c>, because a span into a
    /// struct reached through an interface would point into a boxed copy of it.
    /// </remarks>
    T ItemAt(int index);

    /// <summary>Adds an item at the end, reporting whether there was room.</summary>
    bool TryAdd(T item);

    /// <summary>Empties the list.</summary>
    void Clear();
}

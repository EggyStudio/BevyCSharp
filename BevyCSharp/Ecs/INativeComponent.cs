namespace Bevy;

/// <summary>
/// Marks a struct as a handle on one of Bevy's own components rather than a C# one.
/// </summary>
/// <remarks>
/// <para>
/// A struct C# declares is registered with Bevy from its layout, because Bevy has never heard of
/// it. Bevy's own components are the opposite problem: they are Rust types the managed side has
/// no handle on, so they are asked for by name and come back as the same kind of id. This
/// interface carries that name, and the name is the only thing that differs between the two
/// cases. A type implementing it resolves to the engine's component id instead of registering a
/// new one, so every ordinary operation reaches Bevy's real component with no separate API:
/// </para>
/// <code>
/// ctx.Ecs.Add(entity, Transform.At(0f, 5f, 0f));   // Bevy's Transform, not a copy
/// ref var t = ref ctx.Ecs.GetRef&lt;Transform&gt;(entity);
/// foreach (var row in ctx.Ecs.Query&lt;Transform&gt;()) { }
/// </code>
/// <para>
/// Implement it explicitly, so the members stay off the value's own surface: they answer a
/// question about the type, and nothing that holds one of these values wants to see them.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [StructLayout(LayoutKind.Sequential)]
/// public struct Visibility : INativeComponent
/// {
///     public byte Mode;
///     readonly string INativeComponent.NativeName => "Visibility";
/// }
/// </code>
/// </example>
public interface INativeComponent
{
    /// <summary>The Bevy type's name, as the bridge knows it.</summary>
    /// <remarks>
    /// The bridge resolves a fixed set of names; asking for one it does not expose fails when
    /// the id is first resolved rather than quietly registering an unrelated component.
    /// </remarks>
    string NativeName { get; }

    /// <summary>
    /// Whether this struct reproduces the engine type's bytes, and so may be read and written.
    /// </summary>
    /// <remarks>
    /// Override to <see langword="false"/> for a type that exists only to name a component in a
    /// filter, a <see cref="EcsWorld.Has{T}"/> or a <see cref="EcsWorld.Count{T}"/>. Bevy's
    /// <c>Children</c> owns a <c>Vec</c> and its <c>GlobalTransform</c> is an affine matrix with
    /// no C# mirror, so reading either as raw bytes would be wrong and writing one would corrupt
    /// the world. Those operations are refused for such a type; the id-shaped ones still work,
    /// because they never touch the value.
    /// </remarks>
    bool MirrorsLayout => true;
}

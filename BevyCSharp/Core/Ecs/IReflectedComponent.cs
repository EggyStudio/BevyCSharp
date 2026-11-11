namespace Bevy;

/// <summary>
/// A typed wrapper over one of Bevy's components, generated from the checked-in description of
/// Bevy's reflected components.
/// </summary>
/// <typeparam name="TSelf">The wrapper itself.</typeparam>
/// <remarks>
/// <para>
/// The wrappers live in <c>Bevy.Reflected</c>, one per component, such as <c>PointLightRef</c>
/// over <c>bevy_light::point_light::PointLight</c>, with a property per field. They read and write
/// through the same reflection as <see cref="EcsWorld.GetReflected"/>, so they cost what a string
/// path costs. What they add is the compiler, because a field Bevy renames stops compiling once the
/// description is regenerated rather than failing on the day the line runs.
/// </para>
/// <para>
/// A wrapper holds the world and an entity, not a copy of the component, so every read is the
/// component as it is now. It is obtained through <see cref="EcsWorld.Get{T}"/> or
/// <see cref="EcsWorld.Insert{T}"/>, which make one through its static members.
/// </para>
/// </remarks>
public interface IReflectedComponent<TSelf> where TSelf : struct, IReflectedComponent<TSelf>
{
    /// <summary>The component's full Rust type path.</summary>
    static abstract string TypePath { get; }

    /// <summary>Makes a wrapper over the component on <paramref name="entity"/>.</summary>
    static abstract TSelf Create(EcsWorld world, Entity entity);
}

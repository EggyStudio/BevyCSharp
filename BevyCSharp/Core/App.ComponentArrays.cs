namespace Bevy;

public sealed unsafe partial class App
{
    /// <summary>
    /// Keeps every entity's <typeparamref name="T"/> in a storage buffer a shader reads by the
    /// entity's mesh tag, as Bevy's <c>GpuComponentArrayBufferPlugin</c> does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The array is brought up to date at the end of every frame (<see cref="Stage.Last"/>), so a
    /// component added, changed or taken off in the frame is in the buffer the frame draws. What it
    /// keeps and how a shader reads it is in <see cref="ComponentArray{T}"/>.
    /// </para>
    /// <para>
    /// Bevy's plugin converts its component into a GPU struct of its own, and here the component is
    /// copied as C# lays it out, so a <typeparamref name="T"/> meant for a structured buffer spells
    /// its padding out as that struct would.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tints = app.AddComponentArray&lt;Tint&gt;();
    /// app.Startup(ctx => material = Shaders.CreateMaterial(program).SetBuffer("tints", tints.Buffer));
    /// </code>
    /// </example>
    public ComponentArray<T> AddComponentArray<T>() where T : unmanaged
    {
        var array = new ComponentArray<T>();
        AddSystem(Stage.Last, new SystemDescriptor(world => array.Update(world.Resource<EcsWorld>()), $"ComponentArray.{typeof(T).Name}"));
        return array;
    }
}

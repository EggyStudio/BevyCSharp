using System.Runtime.CompilerServices;
using Bevy.Reflected;

namespace Bevy;

/// <summary>
/// Every entity's <typeparamref name="T"/> kept in one storage buffer, each entity's mesh tag its
/// place there, as Bevy's <c>GpuComponentArrayBuffer</c> keeps a component for a shader.
/// </summary>
/// <typeparam name="T">The component, copied into the buffer as C# lays it out.</typeparam>
/// <remarks>
/// <para>
/// Made by <see cref="App.AddComponentArray{T}"/>, which brings it up to date at the end of every
/// frame. An entity given a <typeparamref name="T"/> takes the next place and a mesh tag holding
/// it, one whose component changed has its place written again, and one that lost the component or
/// was despawned gives its place to the last entry, whose entity takes the place as its tag, so the
/// array stays packed and a shader never reads a place nobody holds.
/// </para>
/// <para>
/// A material binds it by name with <see cref="Buffer"/>, and its shader reads it as a
/// <c>StructuredBuffer</c> at <c>bcs::tag(mesh.instance_index)</c>, or <c>bcs2d::tag</c> on a 2D
/// mesh. A structured buffer is laid out with the storage rules, where a <c>float3</c> takes sixteen
/// bytes, so a struct shared with one spells its padding out. Every material drawing the entities
/// can share the one buffer, as Bevy's example shares it between two, and an entity drawn by any of
/// them finds its own entry.
/// </para>
/// <para>
/// The mesh tag is the array's, so an entity in it cannot use a tag of its own, and an entity in
/// two arrays has the tag of whichever wrote last, as in Bevy, where one entity with two of these
/// components is not supported either.
/// </para>
/// </remarks>
public sealed class ComponentArray<T> where T : unmanaged
{
    private readonly Dictionary<Entity, int> _places = [];
    private readonly List<Entity> _entities = [];
    private readonly HashSet<Entity> _present = [];
    private T[] _items = new T[1];
    private AssetHandle _buffer = AssetHandle.None;

    internal ComponentArray()
    {
    }

    /// <summary>
    /// The buffer the entries are in, made the first time it is asked for. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// The buffer grows as entities join, every material holding it bound to the larger one
    /// (<see cref="Shaders.GrowBuffer"/>), so the handle stays the same.
    /// </remarks>
    public AssetHandle Buffer
    {
        get
        {
            if (!_buffer.IsValid) _buffer = Shaders.CreateBuffer(Unsafe.SizeOf<T>());
            return _buffer;
        }
    }

    /// <summary>How many entities hold a place, as of the end of the last frame.</summary>
    public int Count => _entities.Count;

    /// <summary>Where an entity's entry is, its mesh tag, or -1 where it has none.</summary>
    public int PlaceOf(Entity entity) => _places.TryGetValue(entity, out var place) ? place : -1;

    /// <summary>Brings the array up to date with the world, as the frame ends.</summary>
    internal void Update(EcsWorld ecs)
    {
        var written = false;
        _present.Clear();

        foreach (var entity in ecs.EntitiesWith<T>())
        {
            _present.Add(entity);

            if (_places.TryGetValue(entity, out var place))
            {
                if (!ecs.Changed<T>(entity)) continue;
                _items[place] = ecs.GetOrDefault<T>(entity);
            }
            else
            {
                place = _entities.Count;
                if (place == _items.Length) Array.Resize(ref _items, Grown(place + 1));

                _items[place] = ecs.GetOrDefault<T>(entity);
                _places[entity] = place;
                _entities.Add(entity);
                ecs.Insert<MeshTagRef>(entity).Value = (uint)place;
            }

            written = true;
        }

        // From the end, so a place given up is filled by an entry already looked at.
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            var entity = _entities[i];
            if (_present.Contains(entity)) continue;

            Remove(ecs, entity);
            written = true;
        }

        if (!written) return;

        var bytes = Math.Max(_entities.Count, 1) * Unsafe.SizeOf<T>();
        if (bytes > Shaders.BufferSize(Buffer)) Shaders.GrowBuffer(Buffer, Grown(_entities.Count) * Unsafe.SizeOf<T>());

        Shaders.WriteBuffer<T>(Buffer, _items.AsSpan(0, _entities.Count));
    }

    /// <summary>
    /// Takes an entity's entry out, the last entry moved into its place and that entity's tag
    /// changed to say so.
    /// </summary>
    private void Remove(EcsWorld ecs, Entity entity)
    {
        var place = _places[entity];
        var last = _entities.Count - 1;

        _places.Remove(entity);

        if (place != last)
        {
            var moved = _entities[last];
            _entities[place] = moved;
            _items[place] = _items[last];
            _places[moved] = place;
            ecs.Wrap<MeshTagRef>(moved).Value = (uint)place;
        }

        _entities.RemoveAt(last);

        // An entity that lost the component and lives on loses the tag that pointed into the array.
        if (ecs.IsAlive(entity)) ecs.Get<MeshTagRef>(entity)?.Remove();
    }

    /// <summary>
    /// Room for at least <paramref name="count"/> entries, half as much again as was needed each
    /// time it grows, as Bevy grows its own, so a game spawning one entity at a time does not make
    /// a buffer every frame.
    /// </summary>
    private static int Grown(int count) => Math.Max(1, (int)Math.Ceiling(count * 1.5));
}

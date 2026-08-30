using System.Collections.Concurrent;

namespace Bevy;

/// <summary>
/// The managed side of the world: a typed bag of resources shared by every system.
/// </summary>
/// <remarks>
/// <para>
/// Entities and components live in Bevy, reached through <see cref="EcsWorld"/>. Resources
/// live here, as ordinary C# objects. That split is deliberate: components are hot, blittable
/// and iterated by the million, so they belong in Bevy's tables; resources are singletons your
/// gameplay code reads by type, so keeping them managed avoids marshalling on every access and
/// lets them be any C# type at all - not just a blittable struct.
/// </para>
/// <para>
/// <see cref="Time"/> and <see cref="Input"/> are resources like any other, refreshed from
/// Bevy once per frame before user systems run.
/// </para>
/// </remarks>
public sealed class World : IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();
    private bool _disposed;

    /// <summary>Number of resources currently registered.</summary>
    public int ResourceCount => _resources.Count;

    /// <summary>The types of every registered resource.</summary>
    public IReadOnlyCollection<Type> ResourceTypes => _resources.Keys.ToArray();

    /// <summary>Adds or replaces the resource of type <typeparamref name="T"/>.</summary>
    public void InsertResource<T>(T value) where T : notnull => _resources[typeof(T)] = value;

    /// <summary>Returns the existing resource, or inserts and returns <paramref name="value"/>.</summary>
    public T GetOrInsertResource<T>(T value) where T : notnull =>
        (T)_resources.GetOrAdd(typeof(T), value);

    /// <summary>Returns the existing resource, or inserts one built by <paramref name="factory"/>.</summary>
    public T GetOrInsertResource<T>(Func<T> factory) where T : notnull =>
        (T)_resources.GetOrAdd(typeof(T), _ => factory());

    /// <summary>Returns the existing resource, or inserts a default-constructed one.</summary>
    public T InitResource<T>() where T : notnull, new() =>
        (T)_resources.GetOrAdd(typeof(T), _ => new T());

    /// <summary>Removes the resource of type <typeparamref name="T"/>.</summary>
    public bool RemoveResource<T>() where T : notnull => _resources.TryRemove(typeof(T), out _);

    /// <summary>True when a resource of type <typeparamref name="T"/> is registered.</summary>
    public bool ContainsResource<T>() where T : notnull => _resources.ContainsKey(typeof(T));

    /// <summary>Gets a required resource.</summary>
    /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
    public T Resource<T>() where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var value) && value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Resource '{typeof(T).Name}' is not registered. Insert it with "
            + $"world.InsertResource(...) - usually from a plugin's Build method, or an "
            + $"[OnStartup] behaviour method.");
    }

    /// <summary>Gets a resource, or <see langword="null"/> if it is not registered.</summary>
    public T? TryResource<T>() where T : class => _resources.GetValueOrDefault(typeof(T)) as T;

    /// <summary>Gets a resource, reporting whether it was found.</summary>
    public bool TryGetResource<T>(out T value) where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var found) && found is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Disposes every disposable resource and clears the world.</summary>
    public void Clear()
    {
        foreach (var pair in _resources)
        {
            if (pair.Value is not IDisposable disposable) continue;
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[BevyCSharp] Failed to dispose resource {pair.Key.Name}: {ex.Message}");
            }
        }

        _resources.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Clear();
    }
}

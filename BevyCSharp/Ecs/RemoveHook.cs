namespace Bevy;

/// <summary>Reads a component's value as it leaves an entity.</summary>
/// <typeparam name="T">The component.</typeparam>
public delegate void RemoveHook<T>(in T component) where T : unmanaged;

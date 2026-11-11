namespace Bevy;

/// <summary>Rewrites the copy of a component an entity clone is about to receive.</summary>
/// <typeparam name="T">The component.</typeparam>
public delegate void CloneHook<T>(ref T component) where T : unmanaged;

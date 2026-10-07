namespace Bevy;

/// <summary>
/// What runs as a component goes on an entity or comes off it, with the component's value as it is
/// then, Bevy's <c>ComponentHook</c>.
/// </summary>
/// <param name="context">The entity, and the app's managed side the hook may reach.</param>
/// <param name="component">The component's value, read for the call alone.</param>
/// <typeparam name="T">The component.</typeparam>
public delegate void ComponentHook<T>(HookContext context, in T component) where T : unmanaged;

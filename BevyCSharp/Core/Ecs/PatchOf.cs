namespace Bevy;

/// <summary>
/// What <see cref="EcsWorld.Patch{T}"/> does to a component.
/// </summary>
/// <remarks>
/// By reference, so a patch names the fields it cares about and leaves the rest as they were, which
/// is the whole difference between patching a component and replacing it.
/// </remarks>
/// <typeparam name="T">The component being changed.</typeparam>
/// <param name="component">The component as it stands, to be changed in place.</param>
public delegate void PatchOf<T>(ref T component) where T : unmanaged;

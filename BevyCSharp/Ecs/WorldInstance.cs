using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Marks an entity whose scene has finished spawning.
/// </summary>
/// <remarks>
/// A name-only handle, and a weaker signal than it looks. It can appear a frame before the
/// entities the scene produced are visible, so it says "the spawn has been done" rather than
/// "the scene is there". Wait on <see cref="EcsWorld.ChildrenOf"/> instead. Its contents are an
/// internal instance id with no C# equivalent.
/// </remarks>
public readonly struct WorldInstance : INativeComponent
{
    readonly string INativeComponent.NativeName => "WorldInstance";
    readonly bool INativeComponent.MirrorsLayout => false;
}

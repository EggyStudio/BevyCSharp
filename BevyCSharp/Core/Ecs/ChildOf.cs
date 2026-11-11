using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's <c>ChildOf</c> relationship, naming an entity's parent.
/// </summary>
/// <remarks>
/// A name-only handle, for filtering on "has a parent". Use <see cref="EcsWorld.SetParent"/> to
/// change it and <see cref="EcsWorld.ParentOf"/> to read it, because writing the bytes directly
/// would set the field without maintaining the matching <see cref="Children"/> list.
/// </remarks>
public readonly struct ChildOf : INativeComponent
{
    readonly string INativeComponent.NativeName => "ChildOf";
    readonly bool INativeComponent.MirrorsLayout => false;
}

using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's <c>Children</c> list.
/// </summary>
/// <remarks>
/// A name-only handle, for filtering on "has children". The component owns a <c>Vec</c>, which
/// is not safe to read as raw bytes; use <see cref="EcsWorld.ChildrenOf"/> for the contents.
/// </remarks>
public readonly struct Children : INativeComponent
{
    readonly string INativeComponent.NativeName => "Children";
    readonly bool INativeComponent.MirrorsLayout => false;
}

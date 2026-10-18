using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Whether the hierarchy leaves this entity visible, the propagated half of
/// <see cref="Visibility"/>.
/// </summary>
/// <remarks>
/// Written by Bevy during <see cref="Stage.PostUpdate"/> and overwritten every frame, so read it
/// and write <see cref="Visibility"/>. It answers "is this entity hidden by itself or by an
/// ancestor", which is not the same question as whether anything actually drew it; that is
/// <see cref="ViewVisibility"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct InheritedVisibility : INativeComponent, IMirroredLayout
{
    /// <summary>The raw byte Bevy stores, which is 0 or 1.</summary>
    public byte Value;

    /// <summary>True when no ancestor hides this entity.</summary>
    public readonly bool IsVisible => Value != 0;

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "bevy_camera::visibility::InheritedVisibility";

    /// <summary>
    /// Where Bevy puts each field, as the description holds it, which the bridge's own reading of
    /// the component is held to the first time the type is used in a world.
    /// </summary>
    readonly string IMirroredLayout.Layout => DescribedLayouts.InheritedVisibility;

    /// <inheritdoc/>
    public readonly override string ToString() => $"InheritedVisibility({IsVisible})";
}

using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Whether an entity should be drawn.
/// </summary>
/// <remarks>
/// <para>
/// This is the request. Bevy answers it in <see cref="Stage.PostUpdate"/> by walking the
/// hierarchy into <see cref="InheritedVisibility"/>, and then culling into
/// <see cref="ViewVisibility"/>, so hiding a parent hides its children without touching them.
/// </para>
/// <para>
/// Needs a render build; a headless one has no such component and says so when the id is resolved.
/// Set it on an entity that is already drawable, because adding it writes the component but does
/// not pull in the two Bevy computes from it, which arrive with the mesh.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// ctx.Ecs.Add(entity, Visibility.Hidden);
/// ctx.Ecs.GetRef&lt;Visibility&gt;(entity).Mode = VisibilityMode.Inherited;
/// </code>
/// </example>
[StructLayout(LayoutKind.Sequential)]
public struct Visibility : INativeComponent, IMirroredLayout
{
    /// <summary>What this entity asks for.</summary>
    public VisibilityMode Mode;

    /// <summary>Creates a visibility from a mode.</summary>
    public Visibility(VisibilityMode mode) => Mode = mode;

    /// <summary>Defer to the parent, which is Bevy's default.</summary>
    public static Visibility Inherited => new(VisibilityMode.Inherited);

    /// <summary>Hide this entity and everything below it.</summary>
    public static Visibility Hidden => new(VisibilityMode.Hidden);

    /// <summary>Show this entity even if an ancestor is hidden.</summary>
    public static Visibility Visible => new(VisibilityMode.Visible);

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "bevy_camera::visibility::Visibility";

    /// <summary>
    /// Where Bevy puts each field, as the description holds it, which the bridge's own reading of
    /// the component is held to the first time the type is used in a world.
    /// </summary>
    readonly string IMirroredLayout.Layout => DescribedLayouts.Visibility;

    /// <inheritdoc/>
    public readonly override string ToString() => $"Visibility({Mode})";
}

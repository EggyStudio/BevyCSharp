using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>What an entity's <see cref="Visibility"/> asks for.</summary>
/// <remarks>
/// The numbers are Bevy's own discriminants, checked against the engine the first time
/// <see cref="Visibility"/> is resolved.
/// </remarks>
public enum VisibilityMode : byte
{
    /// <summary>Take the parent's answer. A root entity set to this is visible.</summary>
    Inherited = 0,

    /// <summary>Hidden, and so is everything below it in the hierarchy.</summary>
    Hidden = 1,

    /// <summary>Visible, even if an ancestor is hidden.</summary>
    Visible = 2,
}

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
/// Needs a render build; a headless one has no such component and says so when the id is
/// resolved. Set it on an entity that is already drawable: adding it writes the component but
/// does not pull in the two Bevy computes from it, which arrive with the mesh.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// ctx.Ecs.Add(entity, Visibility.Hidden);
/// ctx.Ecs.GetRef&lt;Visibility&gt;(entity).Mode = VisibilityMode.Inherited;
/// </code>
/// </example>
[StructLayout(LayoutKind.Sequential)]
public struct Visibility : INativeComponent
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
    readonly string INativeComponent.NativeName => "Visibility";

    /// <inheritdoc/>
    public readonly override string ToString() => $"Visibility({Mode})";
}

/// <summary>
/// Whether the hierarchy leaves this entity visible: the propagated half of
/// <see cref="Visibility"/>.
/// </summary>
/// <remarks>
/// Written by Bevy during <see cref="Stage.PostUpdate"/> and overwritten every frame, so read it
/// and write <see cref="Visibility"/>. It answers "is this entity hidden by itself or by an
/// ancestor", which is not the same question as whether anything actually drew it; that is
/// <see cref="ViewVisibility"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct InheritedVisibility : INativeComponent
{
    /// <summary>The raw byte Bevy stores, which is 0 or 1.</summary>
    public byte Value;

    /// <summary>True when no ancestor hides this entity.</summary>
    public readonly bool IsVisible => Value != 0;

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "InheritedVisibility";

    /// <inheritdoc/>
    public readonly override string ToString() => $"InheritedVisibility({IsVisible})";
}

/// <summary>
/// Whether any camera actually saw this entity last frame: the culled half of
/// <see cref="Visibility"/>.
/// </summary>
/// <remarks>
/// <para>
/// Written by Bevy after culling and overwritten every frame. An entity can be visible in the
/// hierarchy and still be false here, because it is off-screen or behind the camera, which makes
/// this the one to read before doing work that only matters when something is on screen.
/// </para>
/// <para>
/// The byte packs two frames: bit 0 is this frame and bit 1 the previous one. Bevy keeps the
/// older bit as scratch space so it can tell "no longer seen by anything" from "was never seen",
/// and only then trip change detection.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct ViewVisibility : INativeComponent
{
    /// <summary>The raw byte Bevy stores: bit 0 this frame, bit 1 the previous frame.</summary>
    public byte Bits;

    /// <summary>True when some view rendered this entity this frame.</summary>
    public readonly bool IsVisible => (Bits & 1) != 0;

    /// <summary>True when some view rendered this entity the frame before.</summary>
    public readonly bool WasVisible => (Bits & 2) != 0;

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "ViewVisibility";

    /// <inheritdoc/>
    public readonly override string ToString() => $"ViewVisibility({IsVisible})";
}

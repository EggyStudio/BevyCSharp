using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Where an entity sits: position, rotation and scale, relative to its parent.
/// </summary>
/// <remarks>
/// <para>
/// This is Bevy's own <c>Transform</c>, not a copy kept in sync. Writing to one through a query
/// updates the component Bevy's own systems read, so transform propagation, rendering and
/// anything else built on it all see the change.
/// </para>
/// <para>
/// The field order and padding here reproduce Bevy's memory layout exactly, which is not the
/// order its source declares. <see cref="NativeComponents"/> checks every offset against the
/// engine the first time the component is resolved, so a layout that drifts fails loudly rather
/// than reading each value from the wrong place.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Transform : INativeComponent
{
    /// <summary>Rotation relative to the parent.</summary>
    public Quat Rotation;

    /// <summary>Position relative to the parent.</summary>
    public Vec3 Translation;

    /// <summary>Scale relative to the parent.</summary>
    public Vec3 Scale;

    /// <summary>
    /// The engine's name for this component, which makes the ordinary generic API land on Bevy's
    /// own <c>Transform</c> rather than register a second one that merely shares the name.
    /// </summary>
    /// <remarks>
    /// Implemented explicitly, because it answers a question about the type and nothing holding a
    /// transform needs it on the value's surface.
    /// </remarks>
    readonly string INativeComponent.NativeName => "Transform";

    private readonly float _tailPaddingA;
    private readonly float _tailPaddingB;

    /// <summary>The layout Bevy gives this struct, which the field order above reproduces.</summary>
    /// <remarks>
    /// Rotation first is not a stylistic choice. Bevy's <c>Transform</c> uses Rust's default
    /// representation, which permits the compiler to reorder fields, and it does. The
    /// sixteen-byte-aligned <c>Quat</c> is moved ahead of the two vectors to save padding.
    /// Declaring the fields in source order here would compile, pass a size check, and read
    /// every value from the wrong place.
    /// </remarks>
    internal const int NativeSize = 48;

    /// <summary>Offset Bevy places the rotation at.</summary>
    internal const int RotationOffset = 0;

    /// <summary>Offset Bevy places the translation at.</summary>
    internal const int TranslationOffset = 16;

    /// <summary>Offset Bevy places the scale at.</summary>
    internal const int ScaleOffset = 28;

    /// <summary>Creates a transform at <paramref name="translation"/> with no rotation.</summary>
    public Transform(Vec3 translation)
    {
        Rotation = Quat.Identity;
        Translation = translation;
        Scale = Vec3.One;
        _tailPaddingA = 0f;
        _tailPaddingB = 0f;
    }

    /// <summary>Creates a transform from all three parts.</summary>
    public Transform(Vec3 translation, Quat rotation, Vec3 scale)
    {
        Rotation = rotation;
        Translation = translation;
        Scale = scale;
        _tailPaddingA = 0f;
        _tailPaddingB = 0f;
    }

    /// <summary>The identity transform: at the origin, unrotated, unscaled.</summary>
    public static Transform Identity => new(Vec3.Zero, Quat.Identity, Vec3.One);

    /// <summary>A transform at <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>.</summary>
    public static Transform At(float x, float y, float z) => new(new Vec3(x, y, z));

    /// <summary>
    /// A transform at <paramref name="eye"/> oriented so that its forward axis points at
    /// <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// Forward is negative Z, matching Bevy's convention, so this aims a camera or a directional
    /// light. The maths is done here rather than in the engine because it needs no world state, and
    /// a call across the boundary for arithmetic would be waste.
    /// </remarks>
    /// <param name="eye">Where the transform sits.</param>
    /// <param name="target">What it points at.</param>
    /// <param name="up">Which way is up, used to settle the roll.</param>
    public static Transform LookingAt(Vec3 eye, Vec3 target, Vec3 up)
    {
        var back = (eye - target).Normalized;         // negative Z, so back rather than forward
        var right = Vec3.Cross(up, back).Normalized;
        var trueUp = Vec3.Cross(back, right);

        return new Transform(eye, Quat.FromBasis(right, trueUp, back), Vec3.One);
    }

    /// <inheritdoc/>
    public readonly override string ToString() =>
        $"Transform(at {Translation}, scale {Scale})";
}

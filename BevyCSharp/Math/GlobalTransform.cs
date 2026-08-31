using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Where an entity actually is: the world-space result of transform propagation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Transform"/> is relative to the parent, so for anything parented it answers a
/// different question from the one gameplay usually asks. Bevy combines each ancestor's transform
/// during <see cref="Stage.PostUpdate"/> and writes the result here, as an affine matrix rather
/// than the position/rotation/scale triple, because a chain of arbitrary transforms cannot always
/// be expressed as one of those.
/// </para>
/// <para>
/// <b>Read this, write <see cref="Transform"/>.</b> Propagation overwrites it every frame, so a
/// write lands and then disappears. It is also a frame behind a <see cref="Transform"/> written
/// during <see cref="Stage.PostUpdate"/> or later, which is when propagation has already run.
/// </para>
/// <para>
/// The layout mirrors glam's <c>Affine3A</c>: three basis columns and a translation, each a
/// sixteen-byte-aligned <c>Vec3A</c> occupying four floats rather than three.
/// <see cref="NativeComponents"/> checks every offset against the engine the first time the
/// component is resolved, because packing them tightly would read three of the four from the
/// wrong place while passing a check on the total size.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = ctx.Ecs.GetRef&lt;GlobalTransform&gt;(ctx.Entity).Translation;
/// </code>
/// </example>
[StructLayout(LayoutKind.Sequential)]
public struct GlobalTransform : INativeComponent
{
    /// <summary>The entity's local X axis in world space, scaled by its world scale.</summary>
    public Vec3 XAxis;

    private float _padX;

    /// <summary>The entity's local Y axis in world space, scaled by its world scale.</summary>
    public Vec3 YAxis;

    private float _padY;

    /// <summary>The entity's local Z axis in world space, scaled by its world scale.</summary>
    public Vec3 ZAxis;

    private float _padZ;

    /// <summary>Position in world space.</summary>
    public Vec3 Translation;

    private float _padTranslation;

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "GlobalTransform";

    /// <summary>The size Bevy gives this struct, which the padding above reproduces.</summary>
    internal const int NativeSize = 64;

    /// <summary>Offset Bevy places the X basis column at.</summary>
    internal const int XAxisOffset = 0;

    /// <summary>Offset Bevy places the Y basis column at.</summary>
    internal const int YAxisOffset = 16;

    /// <summary>Offset Bevy places the Z basis column at.</summary>
    internal const int ZAxisOffset = 32;

    /// <summary>Offset Bevy places the translation at.</summary>
    internal const int TranslationOffset = 48;

    /// <summary>Creates one from its basis columns and translation.</summary>
    public GlobalTransform(Vec3 xAxis, Vec3 yAxis, Vec3 zAxis, Vec3 translation)
    {
        XAxis = xAxis;
        YAxis = yAxis;
        ZAxis = zAxis;
        Translation = translation;
        _padX = 0f;
        _padY = 0f;
        _padZ = 0f;
        _padTranslation = 0f;
    }

    /// <summary>The transform that does nothing: at the origin, unrotated, unscaled.</summary>
    public static GlobalTransform Identity =>
        new(Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ, Vec3.Zero);

    /// <summary>World-space scale, taken from the lengths of the three basis columns.</summary>
    /// <remarks>
    /// The X component carries the sign of the determinant, so a transform mirrored an odd number
    /// of times reports a negative scale rather than a rotation that cannot exist.
    /// </remarks>
    public readonly Vec3 Scale
    {
        get
        {
            var determinant = Vec3.Dot(XAxis, Vec3.Cross(YAxis, ZAxis));
            return new Vec3(
                XAxis.Length * (determinant < 0f ? -1f : 1f),
                YAxis.Length,
                ZAxis.Length);
        }
    }

    /// <summary>World-space rotation, with the scale divided back out of the basis.</summary>
    /// <remarks>
    /// Only meaningful for a transform built from rotations, translations and scales. A chain
    /// containing a shear has no equivalent rotation, and this returns the nearest thing the
    /// columns suggest rather than failing.
    /// </remarks>
    public readonly Quat Rotation
    {
        get
        {
            var scale = Scale;
            return Quat.FromBasis(
                Divide(XAxis, scale.X),
                Divide(YAxis, scale.Y),
                Divide(ZAxis, scale.Z));
        }
    }

    /// <summary>The direction the entity faces, which is its negative Z axis.</summary>
    public readonly Vec3 Forward => (-ZAxis).Normalized;

    /// <summary>The entity's local right, in world space.</summary>
    public readonly Vec3 Right => XAxis.Normalized;

    /// <summary>The entity's local up, in world space.</summary>
    public readonly Vec3 Up => YAxis.Normalized;

    /// <summary>
    /// The same transform expressed as a position, rotation and scale.
    /// </summary>
    /// <remarks>
    /// Matches Bevy's own decomposition. Exact for any chain of rotations, translations and
    /// uniform or axis-aligned scales, which is every hierarchy that does not deliberately shear.
    /// </remarks>
    public readonly Transform ToTransform() => new(Translation, Rotation, Scale);

    /// <summary>Maps a point in the entity's local space into world space.</summary>
    /// <example>
    /// <code>
    /// // Where the muzzle of a gun modelled at (0, 0, -1) actually is.
    /// var muzzle = global.TransformPoint(new Vec3(0f, 0f, -1f));
    /// </code>
    /// </example>
    public readonly Vec3 TransformPoint(Vec3 point) =>
        XAxis * point.X + YAxis * point.Y + ZAxis * point.Z + Translation;

    /// <summary>Maps a direction in the entity's local space into world space.</summary>
    /// <remarks>The translation is not applied, so this rotates and scales but does not move.</remarks>
    public readonly Vec3 TransformDirection(Vec3 direction) =>
        XAxis * direction.X + YAxis * direction.Y + ZAxis * direction.Z;

    /// <summary>Divides an axis by its scale, leaving a degenerate axis alone.</summary>
    private static Vec3 Divide(Vec3 axis, float scale) =>
        scale != 0f ? axis * (1f / scale) : axis;

    /// <inheritdoc/>
    public readonly override string ToString() => $"GlobalTransform(at {Translation})";
}

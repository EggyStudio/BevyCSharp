using System.Numerics;

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where the orientation cross is drawn, as reported by the element that reserves the space.
/// </summary>
/// <remarks>
/// <para>
/// The cross is a thing in the scene that has to appear in a corner of the interface, which is two
/// coordinate systems meeting. Rendering it to a texture and showing that in a panel would be one
/// answer; this is the cheaper one: the layout puts an empty square wherever the bar ends up, the
/// panel reads back where that square landed, and the lines are drawn along the ray through its
/// centre. Nothing tracks a screen position, because the interface already knows one.
/// </para>
/// <para>
/// A static because a behavior and a panel are two things that never see each other, and this is
/// one number passing between them once a frame.
/// </para>
/// </remarks>
public static class EditorGizmoSlot
{
    /// <summary>Where the square is, or a zero-width one when there is none.</summary>
    public static (float Left, float Top, float Width, float Height) Rect { get; private set; }

    /// <summary>The frame the square was last reported on.</summary>
    private static ulong _said;

    /// <summary>
    /// Whether anything has said where the cross goes lately.
    /// </summary>
    /// <remarks>
    /// Lately rather than this frame, because the answer goes stale on its own: closing the bar
    /// stops it being reported, and a couple of frames later the cross is back in the corner
    /// without anything having to tell it so.
    /// </remarks>
    public static bool Known => Rect.Width > 1f && Frame - _said < 4;

    /// <summary>The middle of the square, which is what the cross is drawn at.</summary>
    public static (float X, float Y) Centre =>
        (Rect.Left + (Rect.Width * 0.5f), Rect.Top + (Rect.Height * 0.5f));

    /// <summary>How wide the square is, which is how big the cross should be drawn.</summary>
    public static float Size => Rect.Width;

    /// <summary>Says where the reserved square ended up, in logical pixels.</summary>
    public static void Report(Vector2 at, float size, ulong frame)
    {
        if (size <= 1f)
        {
            Forget();
            return;
        }

        Rect = (at.X, at.Y, size, size);
        _said = frame;
    }

    /// <summary>What frame it is, as far as the cross is concerned.</summary>
    public static ulong Frame { get; set; }

    /// <summary>Says there is nowhere to draw, which puts the cross back in the corner.</summary>
    public static void Forget() => Rect = default;
}

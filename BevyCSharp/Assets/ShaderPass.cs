namespace Bevy;

/// <summary>One full-screen pass a camera runs over what it drew. See <see cref="Shaders.SetPasses"/>.</summary>
/// <param name="Instance">An instance of a program with a pass stage.</param>
/// <param name="AfterTonemapping">
/// Whether the pass runs on the picture as the screen will show it rather than on the linear one.
/// Before is right for anything about light, such as a glow or an exposure, because the numbers
/// are still proportional to it. After is right for anything about the picture as a picture, such
/// as scan lines, a palette or dithering.
/// </param>
/// <param name="At">
/// A point of the frame to run at instead, which is <see cref="FramePoint.AfterOpaque"/>: on the lit
/// opaque geometry, before transparent geometry is drawn over it, which is where something about
/// the lit surfaces goes (a screen-space reflection or GI composite, a fog glass should not be
/// under). It needs a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of one), since a
/// multisampled picture is not resolved until transparent geometry is drawn. Null runs the pass by
/// <paramref name="AfterTonemapping"/>, and the tonemapping points may be named here as well.
/// </param>
public readonly record struct ShaderPass(ShaderInstance Instance, bool AfterTonemapping = false, FramePoint? At = null)
{
    /// <summary>Where the bridge runs it, as the number it reads.</summary>
    internal int Place => At switch
    {
        null => AfterTonemapping ? 1 : 0,
        FramePoint.BeforeTonemapping => 0,
        FramePoint.AfterTonemapping => 1,
        FramePoint.AfterOpaque => 2,
        _ => throw new ArgumentException(
            "A pass runs on the picture, which does not exist yet after the prepass.", nameof(At)),
    };

    /// <summary>A pass that runs before tonemapping.</summary>
    public static implicit operator ShaderPass(ShaderInstance instance) => new(instance);
}

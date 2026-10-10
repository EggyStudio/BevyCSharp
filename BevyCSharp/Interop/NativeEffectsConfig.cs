using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The lens effects a camera draws through, beside <see cref="NativePostConfig"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeEffectsConfig
{
    /// <summary>Pairs of luminance and compensation the flat config carries.</summary>
    public const int CompensationPoints = 8;

    /// <summary>0 no depth of field, 1 gaussian, 2 bokeh.</summary>
    public int DofMode;

    /// <summary>Distance in meters to what is in focus.</summary>
    public float FocalDistance;

    /// <summary>Aperture in f-stops.</summary>
    public float ApertureFStops;

    /// <summary>Sensor height in meters; 0 for Bevy's own.</summary>
    public float SensorHeight;

    /// <summary>Widest a blur may be, in pixels; 0 for Bevy's own.</summary>
    public float MaxBlurDiameter;

    /// <summary>Distance past which nothing blurs further; 0 leaves it unbounded.</summary>
    public float MaxDepth;

    /// <summary>Fraction of a frame the shutter is open; 0 for no motion blur.</summary>
    public float ShutterAngle;

    /// <summary>Samples either side of a pixel along its motion; 0 also turns it off.</summary>
    public uint MotionBlurSamples;

    /// <summary>Width of the colored fringe, as a fraction of the window; 0 for none.</summary>
    public float Aberration;

    /// <summary>Cap on the samples the fringe is built from; 0 for Bevy's own.</summary>
    public uint AberrationSamples;

    /// <summary>Asset key of the image the fringe takes its colors from, or -1.</summary>
    public int AberrationLut;

    /// <summary>Strength of the lens warp; 0 for a straight picture.</summary>
    public float Distortion;

    /// <summary>Zoom applied after warping.</summary>
    public float DistortionScale;

    /// <summary>How much of the warp lands on X.</summary>
    public float DistortionAxisX;

    /// <summary>How much of the warp lands on Y.</summary>
    public float DistortionAxisY;

    /// <summary>Point the warp radiates from, horizontally.</summary>
    public float DistortionCenterX;

    /// <summary>Point the warp radiates from, vertically.</summary>
    public float DistortionCenterY;

    /// <summary>How sharply the warp bends at the edges.</summary>
    public float DistortionEdgeCurvature;

    /// <summary>How dark the corners go; 0 for no vignette.</summary>
    public float Vignette;

    /// <summary>How much of the picture is left untouched.</summary>
    public float VignetteRadius;

    /// <summary>Width of the edge between the clear center and the dark corners.</summary>
    public float VignetteSmoothness;

    /// <summary>Shape of that edge; 1 for a circle.</summary>
    public float VignetteRoundness;

    /// <summary>Point the vignette is centered on, horizontally.</summary>
    public float VignetteCenterX;

    /// <summary>Point the vignette is centered on, vertically.</summary>
    public float VignetteCenterY;

    /// <summary>How far the vignette is stretched to fit a window that is not square.</summary>
    public float VignetteEdgeCompensation;

    /// <summary>Vignette color red, linear.</summary>
    public float VignetteColorR;

    /// <summary>Vignette color green, linear.</summary>
    public float VignetteColorG;

    /// <summary>Vignette color blue, linear.</summary>
    public float VignetteColorB;

    /// <summary>Vignette color alpha.</summary>
    public float VignetteColorA;

    /// <summary>Non-zero to let the camera find its own exposure.</summary>
    public int AutoExposure;

    /// <summary>Darkest luminance the metering counts, in EV-100.</summary>
    public float MeteringMin;

    /// <summary>Brightest luminance the metering counts, in EV-100.</summary>
    public float MeteringMax;

    /// <summary>Fraction of the darkest samples thrown away.</summary>
    public float MeteringLow;

    /// <summary>Fraction below which the brightest samples are kept.</summary>
    public float MeteringHigh;

    /// <summary>How fast the exposure opens, in f-stops per second.</summary>
    public float SpeedBrighten;

    /// <summary>How fast it closes, in f-stops per second.</summary>
    public float SpeedDarken;

    /// <summary>How near the target adaptation stops being linear, in f-stops.</summary>
    public float ExposureTransition;

    /// <summary>Asset key of the metering weight image, or -1.</summary>
    public int MeteringMask;

    /// <summary>How many entries of <see cref="CompensationCurve"/> are used.</summary>
    public uint CompensationCount;

    /// <summary>Luminance in EV-100 and compensation in f-stops, in pairs, rising.</summary>
    public fixed float CompensationCurve[CompensationPoints * 2];
}

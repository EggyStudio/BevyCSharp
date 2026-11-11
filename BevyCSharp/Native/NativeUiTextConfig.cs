using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a run of UI text is set.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeUiTextConfig
{
    /// <summary>Asset key of the font, or negative for the one built into Bevy.</summary>
    public int Font;

    /// <summary>Glyph height in logical pixels.</summary>
    public float FontSize;

    /// <summary>0 left, 1 centered, 2 right, 3 justified, 4 start, 5 end.</summary>
    public int Justify;

    /// <summary>0 word boundaries, 1 any character, 2 word then character, 3 never.</summary>
    public int LineBreak;

    /// <summary>How far apart the lines sit, or zero for the font's own spacing.</summary>
    public float LineHeight;

    /// <summary>0 multiples of the font size, 1 logical pixels.</summary>
    public int LineHeightUnit;

    /// <summary>Room added between the letters, or zero for the font's own fit.</summary>
    public float LetterSpacing;

    /// <summary>0 multiples of the font size, 1 logical pixels.</summary>
    public int LetterSpacingUnit;

    /// <summary>0 antialiased, 1 not.</summary>
    public int FontSmoothing;

    /// <summary>How far a shadow is cast, across.</summary>
    public float ShadowOffsetX;

    /// <summary>How far a shadow is cast, down.</summary>
    public float ShadowOffsetY;

    /// <summary>Shadow color, red.</summary>
    public float ShadowColorR;

    /// <summary>Shadow color, green.</summary>
    public float ShadowColorG;

    /// <summary>Shadow color, blue.</summary>
    public float ShadowColorB;

    /// <summary>Shadow color, alpha. Zero is no shadow.</summary>
    public float ShadowColorA;
}

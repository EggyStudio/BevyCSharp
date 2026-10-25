namespace Bevy;

/// <summary>
/// The outline of a leaf the generator makes where a species names no leaf set, the paper's leaf
/// shapes, its length along its midrib from the stalk to the tip.
/// </summary>
public enum TreeLeafShape
{
    /// <summary>Widest a little below its middle, rounding to a point, as an aspen's or a birch's.</summary>
    Oval,

    /// <summary>Widest at its foot and narrowing straight to its tip.</summary>
    Triangle,

    /// <summary>Three rounded lobes down each side and one at the tip, as an oak's.</summary>
    ThreeLobeOak,

    /// <summary>Three pointed lobes spreading from the stalk, as a maple's.</summary>
    ThreeLobeMaple,

    /// <summary>Five pointed lobes spreading from the stalk, as a sugar maple's.</summary>
    FiveLobeMaple,

    /// <summary>Three small leaflets on one stalk, the middle one the longest.</summary>
    ThreeLeaflets,
}

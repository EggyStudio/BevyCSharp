namespace Bevy;

/// <summary>How bloom is mixed back into the picture.</summary>
public enum BloomMode
{
    /// <summary>
    /// The scattered light is taken out of the source, so the picture keeps its brightness.
    /// </summary>
    EnergyConserving = 0,

    /// <summary>The scattered light is added on top, which is brighter and more obvious.</summary>
    Additive = 1,
}

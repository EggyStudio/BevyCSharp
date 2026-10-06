namespace Bevy;

/// <summary>Where in a camera's frame a compute shader on it runs.</summary>
public enum FramePoint
{
    /// <summary>Once depth, normals and motion are drawn, before anything is lit.</summary>
    AfterPrepass = 0,

    /// <summary>Once opaque geometry is drawn, before transparent geometry.</summary>
    AfterOpaque = 1,

    /// <summary>On the linear picture, before the passes that run before tonemapping.</summary>
    BeforeTonemapping = 2,

    /// <summary>On the picture as the screen will show it, before the passes that run after it.</summary>
    AfterTonemapping = 3,
}

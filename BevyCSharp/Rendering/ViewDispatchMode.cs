namespace Bevy;

/// <summary>How a <see cref="ViewDispatch"/> counts its workgroups.</summary>
public enum ViewDispatchMode
{
    /// <summary>Enough workgroups of a size in pixels to cover a fraction of the picture.</summary>
    PerPixel = 0,

    /// <summary>A fixed number of workgroups.</summary>
    Fixed = 1,

    /// <summary>As many as a buffer says, written on the GPU.</summary>
    Indirect = 2,
}

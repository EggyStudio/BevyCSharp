using Bevy.Interop;

namespace Bevy;

/// <summary>An edge or corner of the window, counted clockwise from the top.</summary>
/// <remarks>What <see cref="Window.StartDragResize"/> resizes from.</remarks>
public enum WindowEdge
{
    /// <summary>The top edge.</summary>
    Top = 0,

    /// <summary>The top right corner.</summary>
    TopRight = 1,

    /// <summary>The right edge.</summary>
    Right = 2,

    /// <summary>The bottom right corner.</summary>
    BottomRight = 3,

    /// <summary>The bottom edge.</summary>
    Bottom = 4,

    /// <summary>The bottom left corner.</summary>
    BottomLeft = 5,

    /// <summary>The left edge.</summary>
    Left = 6,

    /// <summary>The top left corner.</summary>
    TopLeft = 7,
}

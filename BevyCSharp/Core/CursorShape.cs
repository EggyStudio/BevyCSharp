using Bevy.Interop;

namespace Bevy;

/// <summary>The shape of the pointer while it is over the window.</summary>
/// <remarks>The shapes an interface asks for, taken from the platform's own set.</remarks>
public enum CursorShape
{
    /// <summary>The platform's arrow.</summary>
    Default = 0,

    /// <summary>A text caret, over something to type in.</summary>
    Text = 1,

    /// <summary>A hand, over something to press.</summary>
    Pointer = 2,

    /// <summary>Four arrows, over something to move.</summary>
    Move = 3,

    /// <summary>A no-entry sign, over something that cannot be used now.</summary>
    NotAllowed = 4,

    /// <summary>Left and right, over a vertical edge to drag.</summary>
    ResizeHorizontal = 5,

    /// <summary>Up and down, over a horizontal edge to drag.</summary>
    ResizeVertical = 6,

    /// <summary>From the bottom left to the top right, over that pair of corners.</summary>
    ResizeRising = 7,

    /// <summary>From the top left to the bottom right, over that pair of corners.</summary>
    ResizeFalling = 8,

    /// <summary>An open hand, over something to grab.</summary>
    Grab = 9,

    /// <summary>A closed hand, while something is held.</summary>
    Grabbing = 10,

    /// <summary>The platform's sign to wait, while nothing can be done.</summary>
    Wait = 11,

    /// <summary>The platform's sign of work going on that still takes clicks, such as a file being saved.</summary>
    Progress = 12,
}

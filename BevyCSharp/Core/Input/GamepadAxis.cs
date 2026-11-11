using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>A gamepad's analog inputs.</summary>
public enum GamepadAxis
{
    /// <summary>The left stick across, minus one to one, right positive.</summary>
    LeftX,

    /// <summary>The left stick up and down, minus one to one, up positive.</summary>
    LeftY,

    /// <summary>The right stick across, minus one to one, right positive.</summary>
    RightX,

    /// <summary>The right stick up and down, minus one to one, up positive.</summary>
    RightY,

    /// <summary>How far the left trigger is down, zero to one.</summary>
    LeftTrigger,

    /// <summary>How far the right trigger is down, zero to one.</summary>
    RightTrigger,
}

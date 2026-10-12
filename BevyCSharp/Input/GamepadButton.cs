using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>A gamepad's buttons, as Bevy names them.</summary>
/// <remarks>
/// In the bridge's order, which is Bevy's list of standard buttons, so a button's bit means the same
/// on both sides. The face buttons are named by where they sit, since a pad's labels differ
/// by maker, so <see cref="South"/> is A on an Xbox pad and Cross on a PlayStation one.
/// </remarks>
public enum GamepadButton
{
    /// <summary>The bottom face button, A or Cross.</summary>
    South,

    /// <summary>The right face button, B or Circle.</summary>
    East,

    /// <summary>The top face button, Y or Triangle.</summary>
    North,

    /// <summary>The left face button, X or Square.</summary>
    West,

    /// <summary>The C button, on the pads that have one.</summary>
    C,

    /// <summary>The Z button, on the pads that have one.</summary>
    Z,

    /// <summary>The left shoulder button, LB or L1.</summary>
    LeftTrigger,

    /// <summary>The left trigger, LT or L2, down past three quarters of its travel.</summary>
    LeftTrigger2,

    /// <summary>The right shoulder button, RB or R1.</summary>
    RightTrigger,

    /// <summary>The right trigger, RT or R2, down past three quarters of its travel.</summary>
    RightTrigger2,

    /// <summary>The select or back button.</summary>
    Select,

    /// <summary>The start or menu button.</summary>
    Start,

    /// <summary>The mode or home button.</summary>
    Mode,

    /// <summary>The left stick pressed in.</summary>
    LeftThumb,

    /// <summary>The right stick pressed in.</summary>
    RightThumb,

    /// <summary>Up on the directional pad.</summary>
    DPadUp,

    /// <summary>Down on the directional pad.</summary>
    DPadDown,

    /// <summary>Left on the directional pad.</summary>
    DPadLeft,

    /// <summary>Right on the directional pad.</summary>
    DPadRight,
}

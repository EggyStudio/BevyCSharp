using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>What a drag in the viewport does.</summary>
public enum EditorTool
{
    /// <summary>Picks what is under the pointer and nothing else.</summary>
    Select,

    /// <summary>Drags the selection along an axis.</summary>
    Move,

    /// <summary>Turns the selection about an axis.</summary>
    Rotate,

    /// <summary>Stretches the selection along an axis.</summary>
    Scale,
}

/// <summary>Which axes a handle is drawn along.</summary>
public enum ToolSpace
{
    /// <summary>The world's own axes, whichever way the thing is facing.</summary>
    Global,

    /// <summary>The thing's own axes, which is what a drag along its length means.</summary>
    Local,
}

/// <summary>What several selected things turn and stretch about.</summary>
/// <remarks>
/// Every editor offers both, and the reason is that both are right for different work. Turning
/// three lamps about their own origins aims each of them; turning them about the middle of the
/// three swings them around the room. A first drag should do the first, which is why that is the
/// default, and the second is a toggle away.
/// </remarks>
public enum ToolPivot
{
    /// <summary>Each about its own origin.</summary>
    Origins,

    /// <summary>All about the middle of what is selected.</summary>
    Centre,
}

/// <summary>
/// Which tool the viewport is in, and what the tools agree on.
/// </summary>
/// <remarks>
/// <para>
/// One place, because the toolbar shows it, the gizmo obeys it, the key list describes it and the
/// keys change it. A tool is a mode of the viewport rather than a mode of any panel.
/// </para>
/// <para>
/// The keys are Unity's: Q, W, E, R along the top row of the keyboard, in the order a hand finds
/// them. Q and E do something else while the right button is held, because that is the camera
/// flying and a person flying is not choosing a tool.
/// </para>
/// </remarks>
public static class EditorTools
{
    /// <summary>The tool in force.</summary>
    public static EditorTool Current { get; set; } = EditorTool.Select;

    /// <summary>Whether a drag lands on round numbers.</summary>
    public static bool Snap { get; set; }

    /// <summary>
    /// Whether the handles follow the world's axes or the thing's own.
    /// </summary>
    /// <remarks>
    /// Both are needed and neither is a default anyone agrees on. Moving a thing along a floor
    /// wants the world; sliding a drawer out of a cabinet that is not square to the world wants
    /// the cabinet's. What matters is that the handle drawn and the drag applied use the same one,
    /// so it is asked here and nowhere else.
    /// </remarks>
    public static ToolSpace Space { get; set; } = ToolSpace.Global;

    /// <summary>What several selected things turn and stretch about.</summary>
    /// <remarks>
    /// Origins, until somebody says otherwise. A drag that swings the selection across the level
    /// is a surprise when it was not asked for, and asking for it is one press.
    /// </remarks>
    public static ToolPivot Pivot { get; set; } = ToolPivot.Origins;

    /// <summary>The three axes a handle is drawn along, for a thing with this rotation.</summary>
    public static Vec3[] AxesFor(Quat rotation)
    {
        if (Space == ToolSpace.Global) return ViewportAxes;

        return
        [
            (rotation * ViewportAxes[0]).Normalized,
            (rotation * ViewportAxes[1]).Normalized,
            (rotation * ViewportAxes[2]).Normalized,
        ];
    }

    /// <summary>The world's axes, which are what a global handle is drawn along.</summary>
    private static readonly Vec3[] ViewportAxes = [Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ];

    /// <summary>Metres a snapped move lands on.</summary>
    public static float MoveStep { get; set; } = 0.25f;

    /// <summary>Degrees a snapped turn lands on.</summary>
    public static float RotateStep { get; set; } = 15f;

    /// <summary>Fraction a snapped scale lands on.</summary>
    public static float ScaleStep { get; set; } = 0.1f;

    /// <summary>Which key chooses which tool.</summary>
    public static readonly (Key Key, EditorTool Tool, string Name)[] Keys =
    [
        (Key.Q, EditorTool.Select, "Q"),
        (Key.W, EditorTool.Move, "W"),
        (Key.E, EditorTool.Rotate, "E"),
        (Key.R, EditorTool.Scale, "R"),
    ];

    /// <summary>Rounds a value to the step, when snapping is on.</summary>
    public static float Snapped(float value, float step) =>
        Snap && step > 0f ? MathF.Round(value / step) * step : value;
}

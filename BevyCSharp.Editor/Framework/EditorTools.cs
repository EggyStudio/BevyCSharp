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

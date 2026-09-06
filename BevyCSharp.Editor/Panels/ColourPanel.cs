using Bevy;
using BevyCSharp.Editor.Drawers;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// Asks for a colour.
/// </summary>
/// <remarks>
/// <para>
/// What pressing a patch of colour opens, and not only in the inspector: it is asked for a colour
/// and it answers with one, so anything that has a colour to set can use it without knowing where
/// the colour is kept.
/// </para>
/// <para>
/// It reports every change rather than only the last one, so the thing being coloured changes while
/// the bar is moving. Picking a colour is a thing somebody does by looking at the result, and a
/// picker that only tells you at the end is one where every choice is a guess.
/// </para>
/// </remarks>
[UiPanel(
    "panels/colour.html",
    Root = "#colour",
    Dismiss = UiDismiss.OnOutsideClick,
    Layer = 100)]
public sealed partial class ColourPanel
{
    /// <summary>What is being coloured.</summary>
    [Bind("#c-name", Mode = BindMode.OneWay)]
    public string Title { get; private set; } = "Colour";

    /// <summary>The colour as the six digits everybody reads.</summary>
    [Bind("#c-hex", Mode = BindMode.OneWay)]
    public string Digits { get; private set; } = "#000000";

    /// <summary>How much red, from nothing to a thousand.</summary>
    [Bind("#c-red")]
    public float Red;

    /// <summary>How much green.</summary>
    [Bind("#c-green")]
    public float Green;

    /// <summary>How much blue.</summary>
    [Bind("#c-blue")]
    public float Blue;

    /// <summary>Who to tell when the colour changes.</summary>
    private Action<Vec3>? _tell;

    /// <summary>What was last reported, so the same colour is not reported twice.</summary>
    private Vec3 _told;

    /// <summary>
    /// Opens the picker over a colour, and answers with every colour chosen after it.
    /// </summary>
    /// <param name="title">What is being coloured.</param>
    /// <param name="colour">What it is now.</param>
    /// <param name="x">Where to open, across.</param>
    /// <param name="y">And down.</param>
    /// <param name="picked">Told each time the colour changes.</param>
    public static void Ask(string title, Vec3 colour, float x, float y, Action<Vec3> picked)
    {
        ArgumentNullException.ThrowIfNull(picked);

        var panel = EditorShell.Find<ColourPanel>() ?? new ColourPanel();

        panel.Title = title;
        panel._tell = picked;
        panel._told = colour;
        panel.Red = Bar(colour.X);
        panel.Green = Bar(colour.Y);
        panel.Blue = Bar(colour.Z);
        panel.Digits = ColourDrawer.Digits(colour);

        EditorShell.ShowAt(panel, x, y, pinned: true);
        EditorShell.Reveal(panel);
    }

    /// <summary>Reports the colour while it is being chosen.</summary>
    [OnChange]
    public void Chosen()
    {
        var colour = new Vec3(Channel(Red), Channel(Green), Channel(Blue));

        Digits = ColourDrawer.Digits(colour);

        if (_tell is not { } tell) return;
        if (Near(colour, _told)) return;

        _told = colour;
        tell(colour);
    }

    /// <summary>Keeps the patch showing what the bars come to.</summary>
    [OnRefresh]
    public void Show()
    {
        if (Window is not { IsOpen: true } window) return;

        var colour = new Vec3(Channel(Red), Channel(Green), Channel(Blue));
        var packed = ((uint)Byte(colour.X) << 24)
            | ((uint)Byte(colour.Y) << 16)
            | ((uint)Byte(colour.Z) << 8)
            | 0xFFu;

        if (_painted == packed) return;

        var patch = window.Element("c-patch");
        if (patch.IsNone) return;

        Xui.SetColour(patch, packed);
        _painted = packed;
    }

    /// <summary>What the patch was last painted, so it is written once.</summary>
    private uint _painted;

    /// <summary>Shuts the picker.</summary>
    [Command("#c-done")]
    public void Done() => EditorShell.Conceal(this);

    /// <summary>Where a channel sits on a bar of a thousand steps.</summary>
    /// <remarks>
    /// Clamped, because a colour used as a light's tint can be brighter than white and a bar
    /// cannot say so. Touching the bar of such a colour brings it back into the range the bar has,
    /// which is the honest thing for a bar to do: what it shows is what it will set.
    /// </remarks>
    private static float Bar(float channel) => Math.Clamp(channel, 0f, 1f) * 1000f;

    /// <summary>And back again.</summary>
    private static float Channel(float bar) => Math.Clamp(bar / 1000f, 0f, 1f);

    /// <summary>One channel as a byte, as a screen would show it.</summary>
    private static int Byte(float channel) =>
        (int)Math.Round(Math.Clamp(channel, 0f, 1f) * 255f);

    /// <summary>Whether two colours are the same as far as a screen is concerned.</summary>
    private static bool Near(Vec3 left, Vec3 right) =>
        Byte(left.X) == Byte(right.X)
        && Byte(left.Y) == Byte(right.Y)
        && Byte(left.Z) == Byte(right.Z);
}

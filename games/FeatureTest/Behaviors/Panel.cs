using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// One row of the panel, which says its value and is stepped by the keys or the pad.
/// </summary>
/// <param name="Label">What it is called.</param>
internal abstract record Row(string Label)
{
    /// <summary>What it is set to, said at the right of the row.</summary>
    public virtual string Value => string.Empty;

    /// <summary>How far something it started has come, drawn as a bar under it, or nothing.</summary>
    public virtual float? Progress => null;

    /// <summary>
    /// Steps it, one forward for Enter, the right arrow or the pad's south button, and one back for
    /// the left arrow.
    /// </summary>
    public abstract void Step(BehaviorContext ctx, int direction);
}

/// <summary>A row that opens a page of its own.</summary>
internal sealed record PageRow(string Label, Func<Page> Opens) : Row(Label)
{
    public override string Value => ">";

    public override void Step(BehaviorContext ctx, int direction)
    {
        if (direction > 0) Panel.Open(Opens());
    }
}

/// <summary>A row that is on or off.</summary>
internal sealed record ToggleRow(string Label, Func<bool> Get, Action<bool> Set) : Row(Label)
{
    public override string Value => Get() ? "on" : "off";

    public override void Step(BehaviorContext ctx, int direction) => Set(!Get());
}

/// <summary>
/// A row that is one value of an enum, stepped through them in order and round again.
/// </summary>
internal sealed record ChoiceRow<T>(string Label, Func<T> Get, Action<T> Set) : Row(Label)
    where T : struct, Enum
{
    public override string Value => Get().ToString();

    public override void Step(BehaviorContext ctx, int direction)
    {
        var values = Enum.GetValues<T>();
        var at = Array.IndexOf(values, Get());
        Set(values[((at + direction) % values.Length + values.Length) % values.Length]);
    }
}

/// <summary>
/// A row that is a number between two ends, stepped by a share of the way between them.
/// </summary>
internal sealed record SliderRow(string Label, Func<float> Get, Action<float> Set, float Min, float Max, float By, string Format = "0.##")
    : Row(Label)
{
    public override string Value => Get().ToString(Format, CultureInfo.InvariantCulture);

    public override void Step(BehaviorContext ctx, int direction) =>
        Set(MathF.Round(Math.Clamp(Get() + (direction * By), Min, Max) / By) * By);
}

/// <summary>A row that does something once.</summary>
internal sealed record ActionRow(string Label, Action<BehaviorContext> Do) : Row(Label)
{
    public override void Step(BehaviorContext ctx, int direction)
    {
        if (direction > 0) Do(ctx);
    }
}

/// <summary>A line of text, such as a scene's attribution, which stepping does nothing to.</summary>
internal sealed record TextRow(string Label) : Row(Label)
{
    public override void Step(BehaviorContext ctx, int direction)
    {
    }
}

/// <summary>A page of rows, under its title.</summary>
internal sealed record Page(string Title, IReadOnlyList<Row> Rows);

/// <summary>
/// The admin panel, a list menu over the program steered by keyboard and pad as a mod menu is.
/// </summary>
/// <remarks>
/// <para>
/// F1 or the pad's start button opens and closes it. The arrows or the pad's cross move through
/// the rows, Enter, the right arrow or the pad's south button opens a page or steps a value on,
/// the left arrow steps it back, and Escape, Backspace or the pad's east button goes back a page,
/// closing the panel from the first. The pointer works too, a click being Enter.
/// </para>
/// <para>
/// Read from the engine's own input rather than through ImGui's keyboard navigation, which moves
/// between widgets and has no idea of a row stepped left and right, so the window takes no
/// navigation of its own. Each setting a row changes goes through <see cref="Settings.Change"/>,
/// which writes it and has it applied (<see cref="Applied"/>).
/// </para>
/// </remarks>
public static class Panel
{
    private static readonly Stack<(Page Page, int Selected)> Trail = new();
    private static Page? _page;
    private static int _selected;

    /// <summary>Whether it is open.</summary>
    public static bool IsOpen { get; set; }

    /// <summary>The title of the page shown, for a check that a page opened.</summary>
    public static string? Title => IsOpen ? _page?.Title : null;

    /// <summary>Opens a page over the one shown, which going back returns to.</summary>
    internal static void Open(Page page)
    {
        if (_page is not null) Trail.Push((_page, _selected));
        _page = page;
        _selected = 0;
    }

    /// <summary>Opens or closes it on its keys, steers it, and draws it while it is open.</summary>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var input = ctx.Input;

        if (input.KeyPressed(Key.F1) || Pad(ctx, GamepadButton.Start))
        {
            IsOpen = !IsOpen;
            Trail.Clear();
            _page = IsOpen ? Pages.First() : null;
            _selected = 0;
        }

        if (!IsOpen || _page is not { } page) return;

        if (!ImGuiConsole.IsOpen) Steer(ctx, page);
        if (_page is not { } shown) return;

        ImGui.SetNextWindowPos(new Vector2(16f, 16f), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(380f, 0f), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.9f);

        const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize;

        if (ImGui.Begin("Panel##feature-test", Flags))
        {
            ImGui.TextDisabled(string.Join("  >  ", Trail.Reverse().Select(step => step.Page.Title).Append(shown.Title)));
            ImGui.Separator();

            for (var index = 0; index < shown.Rows.Count; index++)
            {
                var row = shown.Rows[index];
                ImGui.PushID(index);
                if (ImGui.Selectable(row.Label, index == _selected))
                {
                    _selected = index;
                    row.Step(ctx, 1);
                }

                if (row.Value.Length > 0)
                {
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(row.Value).X + ImGui.GetCursorPosX() - 8f);
                    ImGui.TextUnformatted(row.Value);
                }

                if (row.Progress is { } progress) ImGui.ProgressBar(progress, new Vector2(-1f, 6f), string.Empty);

                ImGui.PopID();
            }

            ImGui.Separator();
            ImGui.TextDisabled("Arrows or the pad move, Enter steps on,");
            ImGui.TextDisabled("Esc goes back and F1 closes.");
        }

        ImGui.End();
    }

    /// <summary>
    /// Moves through the rows and steps the one chosen, from the keyboard and every pad.
    /// </summary>
    private static void Steer(BehaviorContext ctx, Page page)
    {
        var input = ctx.Input;
        if (page.Rows.Count == 0) return;

        if (input.KeyPressed(Key.ArrowUp) || Pad(ctx, GamepadButton.DPadUp)) _selected = (_selected + page.Rows.Count - 1) % page.Rows.Count;
        if (input.KeyPressed(Key.ArrowDown) || Pad(ctx, GamepadButton.DPadDown)) _selected = (_selected + 1) % page.Rows.Count;
        _selected = Math.Clamp(_selected, 0, page.Rows.Count - 1);

        var row = page.Rows[_selected];
        if (input.KeyPressed(Key.Enter) || input.KeyPressed(Key.ArrowRight) || Pad(ctx, GamepadButton.South) || Pad(ctx, GamepadButton.DPadRight))
            row.Step(ctx, 1);
        else if (input.KeyPressed(Key.ArrowLeft) || Pad(ctx, GamepadButton.DPadLeft))
            row.Step(ctx, -1);
        else if (input.KeyPressed(Key.Escape) || input.KeyPressed(Key.Backspace) || Pad(ctx, GamepadButton.East))
            Back();
    }

    /// <summary>Back a page, or closed from the first.</summary>
    internal static void Back()
    {
        if (Trail.Count == 0)
        {
            IsOpen = false;
            _page = null;
            return;
        }

        (_page, _selected) = Trail.Pop();
    }

    private static bool Pad(BehaviorContext ctx, GamepadButton button) =>
        ctx.Input.Gamepads.Any(pad => pad.Pressed(button));
}

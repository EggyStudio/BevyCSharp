using System.Numerics;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// A console a game opens over itself with the key under Escape, drawn with Dear ImGui, the log
/// filtered by level and searched over a line to type a command into, with history and completion.
/// </summary>
/// <remarks>
/// <para>
/// One call a frame, between <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/>,
/// gives a game the console the editor has, over the same two lists, the lines
/// <see cref="ConsoleLog"/> keeps, the program's own and Bevy's, and every <c>[Command]</c> the
/// game and the engine declare (<see cref="ConsoleCommands"/>). The key opens and closes it, and
/// it draws nothing while closed, so a game draws it always and decides nothing.
/// </para>
/// <code>
/// ImGuiRuntime.Begin(ctx);
/// DrawTheGamesOwnWindows();
/// ImGuiConsole.Draw(ctx);
/// ImGuiRuntime.End();
/// </code>
/// <para>
/// It lies along the top of the window, the way a game's console has since Quake, over a share of
/// its height (<see cref="Height"/>). The up and down arrows walk back through what was typed, the
/// tab key takes the command a half-typed name completes to, and the hint under the line says what
/// the command takes. A command is run with the frame's world lent to it, so one that reads or
/// changes an entity works as it does in the editor, and one whose answer waits on the GPU is
/// answered in a later frame. While it is open the interface takes the keyboard, which a game's
/// bindings read from <see cref="ImGuiRuntime.Typing"/>.
/// </para>
/// </remarks>
public static unsafe class ImGuiConsole
{
    private static readonly ConsoleView Shown = new();
    private static string _typed = string.Empty;
    private static int _seen = -1;
    private static int _recalls;
    private static bool _focus;

    /// <summary>Whether it is open, which the key toggles and a game may set.</summary>
    public static bool IsOpen { get; set; }

    /// <summary>The key that opens and closes it, the one under Escape by default.</summary>
    public static Key Toggle { get; set; } = Key.Backquote;

    /// <summary>How much of the window's height it takes, from a tenth to the whole.</summary>
    public static float Height { get; set; } = 0.4f;

    /// <summary>
    /// What it shows and runs, for a game that sets its filters or runs a line itself.
    /// </summary>
    public static ConsoleView View => Shown;

    /// <summary>Opens or closes it on its key, and draws it while it is open.</summary>
    /// <param name="ctx">The frame's context, whose world a command is lent.</param>
    /// <remarks>
    /// Called between <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/>, and
    /// does nothing where the interface is not running, as in a build without it or a run that did
    /// not ask for it (<see cref="Config.Gui"/>).
    /// </remarks>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!ImGuiRuntime.IsRunning) return;

        ConsoleView.AnswerLater(ctx.World);

        // The key closes it while a line is being typed as well, and the character it typed is
        // taken back out, as the console it opens with would otherwise fill with them.
        if (ctx.Input.KeyPressed(Toggle) && (IsOpen || !ImGuiRuntime.Typing))
        {
            IsOpen = !IsOpen;
            _focus = IsOpen;
            _typed = _typed.Replace("`", string.Empty, StringComparison.Ordinal);
            _recalls++;
        }

        if (!IsOpen) return;

        var window = ImGuiRuntime.Size;
        ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(window.X, MathF.Round(window.Y * Math.Clamp(Height, 0.1f, 1f))), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.92f);

        const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar;

        if (ImGui.Begin("Console##bcs", Flags))
        {
            Filters();
            Lines();
            Entry(ctx);
        }

        ImGui.End();
    }

    /// <summary>A box to search the log in and a box for each level shown.</summary>
    private static void Filters()
    {
        var search = View.Search;
        ImGui.SetNextItemWidth(MathF.Max(120f, ImGui.GetContentRegionAvail().X * 0.4f));
        if (ImGui.InputTextWithHint("##search", "Search the log", ref search, 128)) View.Search = search;

        var info = View.ShowInfo;
        ImGui.SameLine();
        if (ImGui.Checkbox("info", ref info)) View.ShowInfo = info;

        var warnings = View.ShowWarnings;
        ImGui.SameLine();
        if (ImGui.Checkbox("warnings", ref warnings)) View.ShowWarnings = warnings;

        var errors = View.ShowErrors;
        ImGui.SameLine();
        if (ImGui.Checkbox("errors", ref errors)) View.ShowErrors = errors;
    }

    /// <summary>
    /// The lines shown, each on one row in its level's color, following what is written unless
    /// somebody scrolled up to read.
    /// </summary>
    /// <remarks>
    /// One row a line, cut where it is very long (<see cref="ConsoleView.Written"/>), so ImGui's
    /// clipper lays out the rows in view alone of the two thousand a log keeps.
    /// </remarks>
    private static void Lines()
    {
        // The room left once the line to type into and the hint under it have theirs, so the
        // window holds the whole of it and nothing scrolls the filters out of sight.
        var room = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() - ImGui.GetTextLineHeightWithSpacing();
        if (ImGui.BeginChild("##lines", new Vector2(0f, MathF.Max(room, ImGui.GetTextLineHeight())), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            var lines = View.Lines();
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            clipper.Begin(lines.Length);
            while (clipper.Step())
            {
                for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColorOf(lines[index].Level));
                    ImGui.TextUnformatted(ConsoleView.Written(lines[index]));
                    ImGui.PopStyleColor();
                }
            }

            clipper.End();
            clipper.Destroy();

            if (_seen != ConsoleLog.Written && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f) ImGui.SetScrollHereY(1f);
            _seen = ConsoleLog.Written;
        }

        ImGui.EndChild();
    }

    /// <summary>The line to type into, and the hint under it.</summary>
    private static void Entry(BehaviorContext ctx)
    {
        // Under an id that changes whenever the text is put there rather than typed, since ImGui
        // reads the string it was given when a box becomes active and not while it is.
        ImGui.PushID(_recalls);
        ImGui.SetNextItemWidth(-1f);
        if (_focus)
        {
            ImGui.SetKeyboardFocusHere();
            _focus = false;
        }

        // The tab key is given to the box as a completion it asks for, rather than moving the
        // keyboard to the next control, and taken below.
        const ImGuiInputTextFlags Flags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackCompletion;
        if (ImGui.InputTextWithHint("##typed", "Type a command", ref _typed, 512, Flags, NoChange))
        {
            View.Run(_typed, ctx.World);
            _typed = string.Empty;
            _focus = true;
        }

        Recall();
        ImGui.PopID();

        ImGui.TextDisabled(View.Hint(_typed) is { Length: > 0 } hint ? hint : " ");
    }

    /// <summary>
    /// An earlier command back in the box on the arrows, and the command a half-typed name
    /// completes to on the tab key, put there as the editor's console does.
    /// </summary>
    private static void Recall()
    {
        if (!ImGui.IsItemActive()) return;

        var was = _typed;
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) _typed = View.Back(_typed);
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) _typed = View.Forward(_typed);
        else if (ImGui.IsKeyPressed(ImGuiKey.Tab) && View.Completion(_typed) is { } completion) _typed = completion + " ";
        else return;

        if (_typed == was) return;

        _recalls++;
        _focus = true;
    }

    /// <summary>
    /// What the box's completion asks for, which changes nothing, the tab key being read after it.
    /// </summary>
    private static int NoChange(ImGuiInputTextCallbackData* data) => 0;

    private static Vector4 ColorOf(LogLevel level) => level switch
    {
        LogLevel.Warning => new Vector4(1f, 0.78f, 0.35f, 1f),
        LogLevel.Error => new Vector4(1f, 0.42f, 0.42f, 1f),
        LogLevel.Echo => new Vector4(0.55f, 0.85f, 1f, 1f),
        _ => new Vector4(0.82f, 0.82f, 0.82f, 1f),
    };
}

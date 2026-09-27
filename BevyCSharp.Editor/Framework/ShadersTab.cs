using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The shader programs the running app has made, as a tab along the bottom.
/// </summary>
/// <remarks>
/// <para>
/// A shader being edited compiles in the background and reloads on its own, so the question while
/// working on one is not how to reload it but whether it took. Each program is a row saying
/// whether it compiled, and the selected one shows what the compiler said, which is the part the
/// log scrolls away, and what the shader declares, which is every name a value is set by.
/// </para>
/// <para>
/// The same answers the console gives through <c>shader.list</c>, <c>shader.errors</c> and
/// <c>shader.layout</c>, drawn where they can be watched while a file is saved.
/// </para>
/// </remarks>
public static class ShadersTab
{
    /// <summary>The program whose messages are shown, by number, or less than zero for none.</summary>
    private static int _selected = -1;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        if (!App.HasRenderer)
        {
            EditorSurface.Empty("No shaders", "This bridge draws nothing. Build it with the render or editor profile.");
            return;
        }

        var programs = Shaders.Programs.ToList();

        Status(programs);

        if (programs.Count == 0)
        {
            EditorSurface.Pane("##noPrograms", Vector2.Zero, () => EditorSurface.Empty(
                "No shader programs yet",
                "Shaders.CreateProgram makes one, and it is listed here as it compiles."));
            return;
        }

        if (_selected >= programs.Count) _selected = -1;

        // The list beside what the chosen one says, as the frame tab lays out its images beside the
        // picture, since a compiler's message is read while looking at which program it is about.
        var room = ImGui.GetContentRegionAvail();

        EditorSurface.Pane("##programs", new Vector2(MathF.Min(420f, room.X * 0.4f), 0f), () => List(programs));

        ImGui.SameLine();

        EditorSurface.Pane("##said", Vector2.Zero, () =>
        {
            if (_selected < 0)
            {
                EditorSurface.Empty("Nothing chosen", "Pick a program to see what the compiler said about it.");
                return;
            }

            Said(programs[_selected]);
        });
    }

    /// <summary>
    /// Whether shaders compile as they are saved, as a dot and a line, with the button that
    /// compiles every program again at the end of the row.
    /// </summary>
    private static void Status(List<ShaderProgram> programs)
    {
        var theme = EditorTheme.Current;
        var found = Shaders.SlangAvailable;

        ImGui.AlignTextToFramePadding();

        var at = ImGui.GetCursorScreenPos();
        var line = ImGui.GetTextLineHeight();

        ImGui.GetWindowDrawList().AddCircleFilled(
            at + new Vector2(5f, ImGui.GetStyle().FramePadding.Y + (line * 0.5f)),
            4f,
            ImGui.GetColorU32(found ? EditorTheme.LiveAccent : theme.Warn));

        ImGui.Dummy(new Vector2(12f, line));
        ImGui.SameLine();

        ImGui.TextDisabled(found
            ? "slangc found, so Slang shaders compile as they are saved"
            : "slangc not found, so Slang shaders load from the cache and edits are not compiled");

        if (programs.Count > 0)
        {
            var label = "reload all";
            var width = ImGui.CalcTextSize(label).X + (EditorSurface.Sides * 2f);

            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, ImGui.GetContentRegionAvail().X - width));

            if (EditorWidgets.Pill(label, false))
            {
                foreach (var program in programs) program.Reload();
            }
        }

        ImGui.Spacing();
    }

    /// <summary>Every program, as a row saying whether it compiled, and which is chosen.</summary>
    private static void List(List<ShaderProgram> programs)
    {
        var theme = EditorTheme.Current;

        RoundedRows.Rows(() =>
        {
            for (var index = 0; index < programs.Count; index++)
            {
                var program = programs[index];

                var (word, color) = program.State switch
                {
                    ShaderProgramState.Ready => ("ready", theme.Dim),
                    ShaderProgramState.Failed => ("failed", theme.Bad),
                    _ => ("compiling", theme.Warn),
                };

                var picked = _selected == index;

                if (ImGui.Selectable($"##program{index}", picked)) _selected = picked ? -1 : index;

                RoundedRows.Row(picked);

                // The state as a word in its color, then the number and the files, over the row.
                var min = ImGui.GetItemRectMin();
                var draw = ImGui.GetWindowDrawList();
                var gap = ImGui.GetStyle().ItemSpacing.X;

                draw.AddText(min + new Vector2(gap, 0f), ImGui.GetColorU32(color), word);
                draw.AddText(
                    min + new Vector2(gap + ImGui.CalcTextSize("compiling  ").X, 0f),
                    ImGui.GetColorU32(EditorTheme.Ink(picked)),
                    $"#{index}  {program.Files}");
            }
        });
    }

    /// <summary>What the compiler said about one program, what it declares, and what the renderer last reported.</summary>
    private static void Said(ShaderProgram chosen)
    {
        var theme = EditorTheme.Current;

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted($"#{_selected}  {chosen.Files}");
        ImGui.SameLine();

        if (EditorWidgets.Pill("reload", false)) chosen.Reload();

        ImGui.Spacing();
        ImGui.PushTextWrapPos(0f);

        var said = chosen.Diagnostics;

        if (said.Length == 0)
        {
            ImGui.TextDisabled("The compiler said nothing");
        }
        else
        {
            ImGui.PushStyleColor(
                ImGuiCol.Text,
                chosen.State == ShaderProgramState.Failed ? theme.Bad : theme.Warn);
            ImGui.TextUnformatted(said);
            ImGui.PopStyleColor();
        }

        // What the shader declares, which is every name a value can be set by, and where each
        // is, which a struct set as bytes is laid out against.
        var layout = chosen.Layout;

        if (layout.Length > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("It declares:");
            ImGui.TextUnformatted(layout);
        }

        // What the renderer last complained of, which is where a shader that compiled but
        // disagrees with its pipeline shows up, since that is not the compiler's to say.
        var rendered = Shaders.LastRenderError;

        if (rendered.Length > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("The renderer last reported:");
            ImGui.TextUnformatted(rendered);
        }

        ImGui.PopTextWrapPos();
    }
}

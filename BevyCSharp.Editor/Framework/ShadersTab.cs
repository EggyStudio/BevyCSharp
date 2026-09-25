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
/// log scrolls away.
/// </para>
/// <para>
/// The same answers the console gives through <c>shader.list</c> and <c>shader.errors</c>, drawn
/// where they can be watched while a file is saved.
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
            ImGui.TextDisabled("This bridge draws nothing, so there are no shaders");
            return;
        }

        var theme = EditorTheme.Current;
        var programs = Shaders.Programs.ToList();

        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(Shaders.SlangAvailable
            ? "slangc found, so Slang shaders compile as they are saved"
            : "slangc not found, so Slang shaders load from the cache and edits are not compiled");

        if (programs.Count > 0)
        {
            ImGui.SameLine();

            if (EditorWidgets.Pill("reload all", false))
            {
                foreach (var program in programs) program.Reload();
            }
        }

        EditorTheme.Divide();

        if (programs.Count == 0)
        {
            ImGui.TextDisabled("No shader programs yet. Shaders.CreateProgram makes one.");
            return;
        }

        if (_selected >= programs.Count) _selected = -1;

        // The list takes what the messages leave, and the messages take a third of the tab when a
        // program is selected, because a compiler error is a few lines and a list is many.
        var room = ImGui.GetContentRegionAvail();
        var listHeight = _selected >= 0 ? room.Y * 0.62f : 0f;

        if (EditorSurface.Region("##programs", new Vector2(0f, listHeight)))
        {
            foreach (var program in programs)
            {
                var state = program.State;

                var (word, color) = state switch
                {
                    ShaderProgramState.Ready => ("ready", theme.Dim),
                    ShaderProgramState.Failed => ("failed", theme.Bad),
                    _ => ("compiling", theme.Warn),
                };

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextUnformatted(word.PadRight(10));
                ImGui.PopStyleColor();

                ImGui.SameLine();

                var index = programs.IndexOf(program);

                if (ImGui.Selectable($"#{index}  {program.Files}##program{index}", _selected == index))
                {
                    _selected = _selected == index ? -1 : index;
                }
            }
        }

        EditorSurface.EndRegion();

        if (_selected < 0) return;

        var chosen = programs[_selected];

        EditorTheme.Divide();

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted($"#{_selected}");
        ImGui.SameLine();

        if (EditorWidgets.Pill("reload", false)) chosen.Reload();

        if (EditorSurface.Region("##messages", new Vector2(0f, 0f)))
        {
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

        EditorSurface.EndRegion();
    }
}

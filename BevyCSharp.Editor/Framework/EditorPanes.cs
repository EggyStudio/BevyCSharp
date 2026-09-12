using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The panel that holds the world and the details, and the handles that move it.
/// </summary>
/// <remarks>
/// One window with two cards in it, side by side or stacked depending on how wide it is. Where
/// the window goes and how wide it is are <see cref="EditorShell"/>'s to say; this draws it.
/// </remarks>
public static class EditorPanes
{
    /// <summary>How much of the panel's height its own header row takes.</summary>
    private const float Header = 28f;

    /// <summary>The world beside the data, or above it when there is no room for two columns.</summary>
    internal static void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(EditorShell.Panel.X, EditorShell.Panel.Y));
        ImGui.SetNextWindowSize(new Vector2(EditorShell.Panel.Width, EditorShell.Panel.Height));

        // Seen through, so the scene is behind the panel rather than cut off by it. Thinner than
        // the cards inside it: this is the layer against the scene.
        //
        // Docked there is no scene behind it at all, so it is solid whatever the alpha says, and
        // what it is solid in is the ground: the darkest there is, and the same thing the corners
        // taken off the viewport are painted in, so the frame round the scene is one colour.
        ImGui.SetNextWindowBgAlpha(EditorShell.Docked ? 1f : EditorTheme.Current.WindowAlpha);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, EditorSurface.Chrome());

        // Square against the window's edge when it is docked. A rounded corner is what says a
        // thing is floating, and a docked panel is not.
        //
        // Floating, whatever the style says: a value taken from the theme would be written over
        // the style editor's every frame, and nothing dragged there would ever hold.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowRounding,
            EditorShell.Docked ? 0f : ImGui.GetStyle().WindowRounding);

        // One inset rather than two. The panel keeps a gutter wide enough to read as a gap between
        // its cards, and the padding a person sees is the one inside each card; the panel's own
        // padding on top of that doubles the air round everything. The stock look has no card fill
        // at all, so there the window's padding is the only one there is.
        ImGui.PushStyleVar(
            ImGuiStyleVar.WindowPadding,
            EditorTheme.Current.Stock ? ImGui.GetStyle().WindowPadding : new Vector2(EditorSurface.Gutter, EditorSurface.Gutter));

        // Never raised over the rest: the panel is where it is, and clicking it should not put it
        // in front of a menu that was opened over it.
        var flags = EditorSurface.Panel | ImGuiWindowFlags.NoBringToFrontOnFocus;

        if (!ImGui.Begin("##panel", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
            return;
        }

        Handle();

        var body = ImGui.GetContentRegionAvail();

        if (EditorShell.Stacked)
        {
            var world = MathF.Max(120f, body.Y * EditorShell.WorldShare);

            EditorSurface.Card("##world", new Vector2(0f, world), WorldPanel.Draw);
            Splitter(body.X);
            EditorSurface.Card("##data", new Vector2(0f, 0f), DetailsPanel.Draw);
        }
        else
        {
            // ImGui's own columns, which come with the grip between them: dragging one edge is
            // something the table already knows how to do, and nothing here has to work out where
            // the pointer went.
            var table = ImGuiTableFlags.Resizable
                | ImGuiTableFlags.NoBordersInBody
                | ImGuiTableFlags.SizingStretchProp
                | ImGuiTableFlags.NoSavedSettings;

            if (ImGui.BeginTable("##split", 2, table, body))
            {
                ImGui.TableSetupColumn("##worldcol", ImGuiTableColumnFlags.WidthStretch, EditorShell.WorldShare);
                ImGui.TableSetupColumn("##datacol", ImGuiTableColumnFlags.WidthStretch, 1f - EditorShell.WorldShare);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                EditorSurface.Card("##world", new Vector2(0f, body.Y), WorldPanel.Draw);

                ImGui.TableNextColumn();
                EditorSurface.Card("##data", new Vector2(0f, body.Y), DetailsPanel.Draw);

                ImGui.EndTable();
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    /// <summary>The bar between the world and the data, which a drag moves.</summary>
    /// <remarks>
    /// A short pill in the middle rather than a line across: it says where to take hold without
    /// drawing a border, which is the one thing this look does not do.
    /// </remarks>
    internal static void Splitter(float width)
    {
        ImGui.InvisibleButton("##split", new Vector2(width, 10f));

        var held = ImGui.IsItemActive();
        var over = ImGui.IsItemHovered();

        if (over || held) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);

        if (held)
        {
            var body = ImGui.GetWindowHeight() - Header;
            if (body > 1f) EditorShell.WorldShare = Math.Clamp(EditorShell.WorldShare + (ImGui.GetIO().MouseDelta.Y / body), 0.15f, 0.85f);
        }

        EditorSurface.Grab(EditorSurface.Pill, over, held);
    }

    /// <summary>The panel's left edge, which a drag widens.</summary>
    internal static void Handle()
    {
        var at = ImGui.GetWindowPos();
        var height = ImGui.GetWindowHeight();

        // Wholly inside the panel, in the gutter its cards already leave empty. Straddling the
        // edge puts half the handle outside the window, where it is clipped away: what is left is
        // a pill cut down the middle, which is what the tab strip's grip avoids by sitting inside
        // the strip rather than on its edge.
        ImGui.SetCursorScreenPos(at);
        ImGui.InvisibleButton("##width", new Vector2(EditorSurface.Gutter, height));

        var over = ImGui.IsItemHovered();
        var held = ImGui.IsItemActive();

        if (over || held) ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);

        if (held) EditorShell.PanelWidth -= ImGui.GetIO().MouseDelta.X;

        // Standing up, because this edge moves sideways, and otherwise the same handle as the one
        // between the world and the data: out of the way while the panel floats over the scene,
        // and always there once it is docked.
        //
        // Drawn on the front of everything and centred on the panel's edge rather than inside it.
        // What a person sees a gap between is the scene and the card, and the middle of that gap
        // is the edge itself; a pill centred in the half of it that happens to be inside the
        // window sits visibly off to one side.
        EditorSurface.Grab(
            new Vector2(EditorSurface.Pill.Y, EditorSurface.Pill.X),
            over,
            held,
            ImGui.GetForegroundDrawList(),
            at.X);

        ImGui.SetCursorScreenPos(at + ImGui.GetStyle().WindowPadding);
    }

}

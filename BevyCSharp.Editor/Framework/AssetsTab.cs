using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The files the editor is running out of, as a tab along the bottom.
/// </summary>
/// <remarks>
/// A browser over the asset directory itself, where what is listed is exactly what a path in a
/// script or a component would find, because nothing here imports or catalogs anything. What a
/// row shows is
/// <see cref="EditorAssets"/>'s to answer; this draws it.
/// </remarks>
public static class AssetsTab
{
    /// <summary>Which folders are unfolded in the tree.</summary>
    private static readonly HashSet<string> Unfolded = [string.Empty];

    /// <summary>What a drag carries, by name, so a drop target takes only tiles.</summary>
    private const string Dragged = "asset";

    /// <summary>The tile being dragged, kept here rather than handed through ImGui as a pointer.</summary>
    private static string? _dragging;

    /// <summary>The file or folder whose name is being typed, or nothing.</summary>
    private static string? _renaming;

    /// <summary>The name typed so far.</summary>
    private static string _typed = string.Empty;

    /// <summary>The file or folder a delete is waiting on an answer for, or nothing.</summary>
    private static string? _deleting;

    /// <summary>What the last move or delete was refused for, said under the tiles until the next.</summary>
    private static string? _refused;

    /// <summary>
    /// Draws the folders down the left, and what is in the chosen one as tiles on the right.
    /// </summary>
    /// <remarks>
    /// The shape every asset browser has, and the reason is navigation. A single pane with a way
    /// back out means every move between two directories goes up through their parent, and where
    /// something is has to be held in the head instead of being on the screen.
    /// </remarks>
    public static void Draw()
    {
        // A model is geometry rather than pixels, so the only way to show one is to draw it. The
        // picture is asked for here, once a frame, and drawn in the column below once it exists.
        if (EditorShell.Context is { } ctx)
        {
            EditorPreview.Show(ctx, Previewable(EditorAssets.Selected));
        }

        var split = ImGuiTableFlags.Resizable
            | ImGuiTableFlags.NoBordersInBody
            | ImGuiTableFlags.NoSavedSettings;

        var columns = EditorPreview.Ready ? 3 : 2;

        if (!ImGui.BeginTable("##assets", columns, split)) return;

        ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("##tiles", ImGuiTableColumnFlags.WidthStretch);

        if (columns == 3)
        {
            ImGui.TableSetupColumn("##preview", ImGuiTableColumnFlags.WidthFixed, Preview);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        Tree();

        ImGui.TableNextColumn();

        Tiles();

        if (columns == 3)
        {
            ImGui.TableNextColumn();
            EditorPreview.Draw(Preview);
        }

        ImGui.EndTable();
    }

    /// <summary>How large the preview is drawn, in logical pixels.</summary>
    private const float Preview = 220f;

    /// <summary>
    /// The file to draw a picture of, or null for one nothing can be drawn of.
    /// </summary>
    /// <remarks>
    /// Models only. An image already shows itself on its tile, and a sound or a script has nothing
    /// to look at, so drawing a scene for one would be a pass a frame spent on an empty picture.
    /// </remarks>
    private static string? Previewable(string? selected) =>
        selected is { Length: > 0 } && EditorAssets.KindOf(selected) == "model" ? selected : null;

    /// <summary>The folders, from the asset root down.</summary>
    private static void Tree()
    {
        if (EditorSurface.Region("##tree", new Vector2(0f, 0f)))
        {
            RoundedRows.Rows(() => Branch(string.Empty, "assets"));
        }

        EditorSurface.EndRegion();
    }

    /// <summary>One folder and, when it is unfolded, the folders under it.</summary>
    /// <param name="path">Its path under the asset root, empty for the root itself.</param>
    /// <param name="name">What to call it.</param>
    private static void Branch(string path, string name)
    {
        var children = EditorAssets.Directories(path);
        var here = EditorAssets.Directory == path;
        var unfolded = Unfolded.Contains(path);

        var flags = ImGuiTreeNodeFlags.OpenOnArrow
            | ImGuiTreeNodeFlags.SpanAvailWidth
            | ImGuiTreeNodeFlags.FramePadding;

        if (here) flags |= ImGuiTreeNodeFlags.Selected;
        if (children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf;
        if (unfolded) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        ImGui.SetNextItemOpen(unfolded, ImGuiCond.Always);

        var shown = ImGui.TreeNodeEx($"{name}##{path}", flags);

        RoundedRows.Row(here);
        Target(path);

        // The arrow folds, the word walks. Clicking a folder's name is how somebody says they want
        // to look inside it, and the arrow folds it.
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
        {
            EditorAssets.Enter(path);
            Unfolded.Add(path);
        }
        else if (ImGui.IsItemToggledOpen())
        {
            if (shown) Unfolded.Add(path);
            else Unfolded.Remove(path);
        }

        if (!shown) return;

        foreach (var (child, called) in children) Branch(child, called);

        ImGui.TreePop();
    }

    /// <summary>What is in the chosen folder.</summary>
    private static void Tiles()
    {
        if (!EditorSurface.Region("##files", new Vector2(0f, 0f)))
        {
            EditorSurface.EndRegion();
            return;
        }

        var entries = EditorAssets.List();

        if (entries.Count == 0)
        {
            ImGui.TextDisabled("Nothing here");
            EditorSurface.EndRegion();
            return;
        }

        // A grid of tiles rather than a list of names, because most of what is in here is a picture
        // or a mesh, and a name in a column says nothing about which one it is.
        var across = Math.Max(
            1,
            (int)(ImGui.GetContentRegionAvail().X / (Size + ImGui.GetStyle().ItemSpacing.X)));

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];

            if (index % across != 0) ImGui.SameLine();

            Tile(entry, Size);
        }

        if (_refused is { } why) ImGui.TextDisabled(why);

        Renaming();
        Deleting();

        EditorSurface.EndRegion();
    }

    /// <summary>
    /// Offers what a tile can have done to it: renamed, or deleted.
    /// </summary>
    /// <remarks>
    /// Both open a small box of their own rather than acting from the menu, because a rename needs
    /// a name typed and a delete cannot be taken back, so each asks before it does anything.
    /// </remarks>
    private static void Menu(AssetEntry entry)
    {
        if (!EditorWidgets.FlyoutHere($"##asset{entry.Path}")) return;

        RoundedRows.Rows(() =>
        {
            // A model or a scene goes into the scene as an instance, which keeps the edits made over it.
            if (!entry.IsDirectory && EditorAssets.KindOf(entry.Path) is "model" or "scene" && EditorShell.Context is { } ctx)
            {
                if (ImGui.MenuItem("Place in the scene")) EditorCommands.Place(ctx.Ecs, entry.Path);
                RoundedRows.Row();
            }

            if (ImGui.MenuItem("Rename"))
            {
                _renaming = entry.Path;
                _typed = entry.Name;
                _refused = null;
            }

            RoundedRows.Row();

            if (ImGui.MenuItem("Delete"))
            {
                _deleting = entry.Path;
                _refused = null;
            }

            RoundedRows.Row();
        });

        EditorWidgets.EndFlyout();
    }

    /// <summary>The box a new name is typed into, open while a rename waits for one.</summary>
    /// <remarks>
    /// The name only, kept in the same folder, since moving somewhere else is a drag onto that
    /// folder. Enter renames, and Escape or a click elsewhere leaves it as it was.
    /// </remarks>
    private static void Renaming()
    {
        const string Box = "##rename-asset";

        if (_renaming is not null && !ImGui.IsPopupOpen(Box)) ImGui.OpenPopup(Box);
        if (!EditorWidgets.Flyout(Box))
        {
            _renaming = null;
            return;
        }

        ImGui.TextUnformatted("Rename to");
        ImGui.SetNextItemWidth(240f);
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();

        var done = ImGui.InputText(
            "##asset-name",
            ref _typed,
            256,
            ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);

        if (done && _renaming is { } from && _typed.Trim() is { Length: > 0 } name)
        {
            var folder = EditorAssets.Parent(from);
            _refused = EditorAssets.Move(from, folder.Length == 0 ? name : folder + "/" + name);
            _renaming = null;
            ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            _renaming = null;
            ImGui.CloseCurrentPopup();
        }

        EditorWidgets.EndFlyout();
    }

    /// <summary>The question a delete asks before it deletes, open while it waits for an answer.</summary>
    private static void Deleting()
    {
        const string Box = "##delete-asset";

        if (_deleting is not null && !ImGui.IsPopupOpen(Box)) ImGui.OpenPopup(Box);
        if (!EditorWidgets.Flyout(Box))
        {
            _deleting = null;
            return;
        }

        var name = _deleting is { } path ? path[(path.LastIndexOf('/') + 1)..] : string.Empty;
        ImGui.TextUnformatted($"Delete {name}? It cannot be undone.");

        if (ImGui.Button("Delete") && _deleting is { } doomed)
        {
            _refused = EditorAssets.Delete(doomed);
            _deleting = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Keep") || ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            _deleting = null;
            ImGui.CloseCurrentPopup();
        }

        EditorWidgets.EndFlyout();
    }

    /// <summary>
    /// Takes a model or a scene dropped on the scene view, placing it as an instance where the
    /// pointer meets the ground.
    /// </summary>
    /// <remarks>
    /// The scene view is the engine's picture with nothing of ImGui's over it, so there is no item
    /// there to drop on. While a tile that can be placed is held, a bare window the size of the
    /// part of the scene the panels leave free is put over it to take the drop, and it is gone
    /// again when the drag ends, so it never takes a click meant for the scene.
    /// </remarks>
    internal static void SceneDrop(BehaviorContext ctx)
    {
        if (_dragging is not { } dragged || EditorAssets.KindOf(dragged) is not ("model" or "scene")) return;

        // ImGui's own word on whether a drag is under way, since the tile last picked up is still
        // remembered after a drag ends somewhere that took nothing.
        var held = ImGui.GetDragDropPayload();
        if (System.Runtime.CompilerServices.Unsafe.As<ImGuiPayloadPtr, IntPtr>(ref held) == IntPtr.Zero)
        {
            _dragging = null;
            return;
        }

        var (x, y, width, height) = EditorShell.Docked
            ? EditorShell.Scene
            : (0f, 0f, EditorShell.Free.Right, EditorShell.Free.Bottom);

        ImGui.SetNextWindowPos(new Vector2(x, y));
        ImGui.SetNextWindowSize(new Vector2(width, height));

        var flags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##scene-drop", flags))
        {
            ImGui.InvisibleButton("##drop", new Vector2(width, height));

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(Dragged);
                if (System.Runtime.CompilerServices.Unsafe.As<ImGuiPayloadPtr, IntPtr>(ref payload) != IntPtr.Zero)
                {
                    var (pointerX, pointerY) = ctx.Input.MousePosition;
                    EditorCommands.Place(ctx.Ecs, dragged, Ground(pointerX, pointerY));
                    _dragging = null;
                }

                ImGui.EndDragDropTarget();
            }
        }

        ImGui.End();
    }

    /// <summary>Where the pointer's ray meets the ground, or nothing when it points above the horizon.</summary>
    private static Vec3? Ground(float x, float y)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone || !Render.TryRay(camera, x, y, out var origin, out var direction)) return null;
        if (direction.Y >= -1e-4f) return null;

        var along = -origin.Y / direction.Y;
        return origin + (direction * along);
    }

    /// <summary>Lets the tile drawn last be picked up and dropped on a folder.</summary>
    private static void Source(AssetEntry entry)
    {
        if (!ImGui.BeginDragDropSource()) return;

        _dragging = entry.Path;
        ImGui.SetDragDropPayload(Dragged, IntPtr.Zero, 0);
        ImGui.TextUnformatted(entry.Name);

        ImGui.EndDragDropSource();
    }

    /// <summary>Takes a tile dropped on the item drawn last, moving it into <paramref name="folder"/>.</summary>
    /// <remarks>
    /// A tile dropped where it already is, or a folder dropped into itself, is refused before the
    /// drop, so nothing lights up for a move that would do nothing.
    /// </remarks>
    private static void Target(string folder)
    {
        if (_dragging is not { } dragged || !ImGui.BeginDragDropTarget()) return;

        var fits = EditorAssets.Parent(dragged) != folder
            && dragged != folder
            && !folder.StartsWith(dragged + "/", StringComparison.Ordinal);

        if (fits)
        {
            var payload = ImGui.AcceptDragDropPayload(Dragged);

            if (System.Runtime.CompilerServices.Unsafe.As<ImGuiPayloadPtr, IntPtr>(ref payload) != IntPtr.Zero)
            {
                var name = dragged[(dragged.LastIndexOf('/') + 1)..];
                _refused = EditorAssets.Move(dragged, folder.Length == 0 ? name : folder + "/" + name);
                _dragging = null;
            }
        }

        ImGui.EndDragDropTarget();
    }

    /// <summary>How wide and how tall one tile is.</summary>
    private const float Size = 96f;

    /// <summary>One file or directory, as a tile.</summary>
    private static void Tile(AssetEntry entry, float size)
    {
        var picked = EditorAssets.Selected == entry.Path;

        ImGui.BeginGroup();

        var at = ImGui.GetCursorScreenPos();
        var line = ImGui.GetTextLineHeight();

        var pressed = ImGui.InvisibleButton($"##tile{entry.Path}", new Vector2(size, size));
        var over = ImGui.IsItemHovered();

        Source(entry);
        if (entry.IsDirectory) Target(entry.Path);
        Menu(entry);

        if (pressed)
        {
            if (entry.IsDirectory) EditorAssets.Enter(entry.Path);
            else EditorAssets.Select(picked ? null : entry.Path);
        }

        var draw = ImGui.GetWindowDrawList();

        // Cut to the region rather than run under its edge, so a tile scrolled half out of sight
        // ends in a rounded corner instead of a square one.
        var top = at;
        var bottom = at + new Vector2(size, size);

        if (EditorSurface.Clipped(ref top, ref bottom))
        {
            EditorDraw.Rounded(
                top,
                bottom,
                ImGui.GetStyle().ChildRounding,
                ImGui.GetColorU32(picked
                    ? EditorTheme.LiveAccent
                    : over ? EditorTheme.LiveLift : EditorTheme.LiveGroup),
                draw);
        }

        const float Mark = 34f;

        var middle = at + new Vector2((size - Mark) * 0.5f, (size - Mark) * 0.5f - (line * 0.6f));

        // An image tile wears the image, because the interface loads a picture from a path and
        // that is all it takes. Everything else wears the picture of its kind, since what a model
        // or a sound looks like is a thumbnail somebody has to render.
        var shown = !entry.IsDirectory
                    && EditorAssets.KindOf(entry.Path) == "image"
                    && EditorDraw.Picture(
                        draw,
                        entry.Path,
                        at + new Vector2(EditorSurface.Air, EditorSurface.Air),
                        size - (EditorSurface.Air * 2f) - line);

        if (!shown)
        {
            var icon = entry.IsDirectory ? EditorIcons.Folder : EditorAssets.IconOf(entry.Path);

            EditorDraw.Icon(draw, icon, middle, Mark, picked);
        }

        // And its name under it, cut to what fits rather than spilling into the next tile.
        var name = EditorText.Fit(entry.Name, size - EditorSurface.Sides);
        var width = ImGui.CalcTextSize(name).X;

        draw.AddText(
            at + new Vector2((size - width) * 0.5f, size - line - EditorSurface.Air),
            ImGui.GetColorU32(EditorTheme.Ink(picked)),
            name);

        ImGui.EndGroup();

        if (over) EditorWidgets.Tip(entry.IsDirectory ? entry.Name : $"{entry.Name}  ({Say(entry.Size)})");
    }

    /// <summary>How large a file is, in the units a person reads.</summary>
    private static string Say(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024f:0.#} KB",
        _ => $"{bytes / (1024f * 1024f):0.#} MB",
    };
}

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
    /// <remarks>
    /// What a picked file is like is the details panel's to show, as an entity's is, so the tab is
    /// the folders and the tiles and nothing beside them.
    /// </remarks>
    public static void Draw()
    {
        var split = ImGuiTableFlags.Resizable
            | ImGuiTableFlags.NoBordersInBody
            | ImGuiTableFlags.NoSavedSettings;

        if (!ImGui.BeginTable("##assets", 2, split)) return;

        ImGui.TableSetupColumn("##tree", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("##tiles", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        Tree();

        ImGui.TableNextColumn();

        Tiles();

        ImGui.EndTable();
    }

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

        Toolbar();

        var entries = Filtered(EditorAssets.List());

        if (entries.Count == 0)
        {
            ImGui.TextDisabled(_search.Length > 0 || _kind is not null ? "Nothing matches" : "Nothing here");
            EditorSurface.EndRegion();
            return;
        }

        // A grid of tiles rather than a list of names, because most of what is in here is a picture
        // or a mesh, and a name in a column says nothing about which one it is.
        var across = Math.Max(
            1,
            (int)(ImGui.GetContentRegionAvail().X / (_size + ImGui.GetStyle().ItemSpacing.X)));

        // Only the rows on screen are drawn, through ImGui's clipper, as the world panel's are. A
        // row is as tall as a tile and the spacing under it, the same for every row, so the
        // clipper knows where each is without drawing those above it. A tile out of
        // view asks for no thumbnail either, so a folder of models is pictured from where it is
        // being looked at.
        var rows = (entries.Count + across - 1) / across;

        unsafe
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            clipper.Begin(rows, _size + ImGui.GetStyle().ItemSpacing.Y);

            while (clipper.Step())
            {
                for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                {
                    for (var index = row * across; index < Math.Min(entries.Count, (row + 1) * across); index++)
                    {
                        if (index % across != 0) ImGui.SameLine();

                        Tile(entries[index], _size);
                    }
                }
            }

            clipper.End();
            clipper.Destroy();
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
                    EditorCommands.Place(ctx.Ecs, dragged, Under(pointerX, pointerY));
                    _dragging = null;
                }

                ImGui.EndDragDropTarget();
            }
        }

        ImGui.End();
    }

    /// <summary>
    /// Where a model dropped at a point on the scene goes: on the surface under the pointer, or on
    /// the ground plane where the pointer is over nothing, or nowhere in particular above the horizon.
    /// </summary>
    /// <remarks>
    /// On what is under the pointer, as Unity and Godot place a dragged model, so a lamp dropped on
    /// a table stands on the table rather than under it on the floor. The surface is met by its
    /// triangles (<see cref="Picking.TryCast"/>), and the model's own origin is put there.
    /// </remarks>
    private static Vec3? Under(float x, float y)
    {
        var camera = EditorSelection.Camera;
        if (camera.IsNone || !Render.TryRay(camera, x, y, out var origin, out var direction)) return null;

        return Picking.TryCast(origin, direction, out _, out var point, out _) ? point : Ground(origin, direction);
    }

    /// <summary>Where a ray meets the ground, or nothing when it points above the horizon.</summary>
    private static Vec3? Ground(Vec3 origin, Vec3 direction)
    {
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

    /// <summary>How wide and how tall one tile is, which the slider over the tiles sets.</summary>
    private static float _size = 96f;

    /// <summary>What the search box holds.</summary>
    private static string _search = string.Empty;

    /// <summary>The kind of file shown, or nothing for every kind.</summary>
    private static string? _kind;

    /// <summary>The kinds a chip narrows the tiles to, by what a chip says and the kind it means.</summary>
    private static readonly (string Chip, string Kind)[] Kinds =
    [
        ("Models", "model"),
        ("Meshes", "mesh"),
        ("Materials", "material"),
        ("Images", "image"),
        ("Scenes", "scene"),
        ("Data", "data"),
        ("Sounds", "sound"),
        ("Scripts", "behavior script"),
        ("Shaders", "shader"),
    ];

    /// <summary>
    /// The search box, a chip per kind of file and the tile size, above the tiles.
    /// </summary>
    /// <remarks>
    /// A search reads the folder being looked at, as the tiles do, and folders stay listed only
    /// while nothing narrows the tiles, since a folder is a way somewhere rather than an answer.
    /// </remarks>
    private static void Toolbar()
    {
        ImGui.SetNextItemWidth(MathF.Min(220f, ImGui.GetContentRegionAvail().X));
        ImGui.InputTextWithHint("##asset-search", "Search", ref _search, 128);

        ImGui.SameLine();
        if (EditorWidgets.Pill("All", _kind is null)) _kind = null;

        foreach (var (chip, kind) in Kinds)
        {
            ImGui.SameLine();
            if (EditorWidgets.Pill(chip, _kind == kind)) _kind = _kind == kind ? null : kind;
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(110f);
        var fraction = (_size - 64f) / (160f - 64f);
        EditorWidgets.Sliding("##tile-size", fraction, () => ImGui.SliderFloat("##tile-size", ref _size, 64f, 160f, string.Empty));

        ImGui.Spacing();
    }

    /// <summary>The entries the search and the chosen kind leave.</summary>
    private static List<AssetEntry> Filtered(IReadOnlyList<AssetEntry> entries)
    {
        var wanted = _search.Trim();
        var narrowed = wanted.Length > 0 || _kind is not null;

        return [.. entries.Where(entry =>
            entry.IsDirectory
                ? !narrowed
                : (wanted.Length == 0 || entry.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
                    && (_kind is null || EditorAssets.KindOf(entry.Path) == _kind))];
    }

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

        // Twice on a model goes into it, as into a folder, where its meshes and materials are
        // tiles of their own to pick, preview and place one at a time.
        if (over && !entry.IsDirectory && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && EditorAssets.IsModel(entry.Path))
            EditorAssets.Enter(entry.Path);

        AssetGrid.Face(
            at,
            size,
            entry.Name,
            entry.IsDirectory ? null : AssetGrid.PictureOf(entry.Path),
            entry.IsDirectory ? EditorIcons.Folder : EditorAssets.IconOf(entry.Path),
            picked,
            over,
            entry.IsDirectory ? null : AssetGrid.BadgeOf(entry.Path));

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

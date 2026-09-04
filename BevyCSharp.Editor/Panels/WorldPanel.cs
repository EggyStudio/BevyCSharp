using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What is in the world, as a tree, and the commands that act on all of it.
/// </summary>
/// <remarks>
/// <para>
/// Called the world rather than the hierarchy because that is what Bevy calls the thing being
/// listed. The rows are its entities, nested by the parent relationship the engine already keeps,
/// so what is shown is the world's own structure rather than a second one the editor maintains.
/// </para>
/// <para>
/// The rows are a pool. The document declares a fixed number of them and this decides what each
/// one stands for, which is what a list of ten thousand entities wants anyway: only the rows on
/// screen are ever drawn, and scrolling moves what the pool points at.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/world.html",
    Root = "#world",
    Handle = "#world-title",
    Dock = EditorDock.Left,
    Fill = true)]
public sealed partial class WorldPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 24;

    /// <summary>
    /// Whether the editor's own widgets are listed alongside the world's entities.
    /// </summary>
    /// <remarks>
    /// Off. The interface is built out of entities in the same world as the scene and names every
    /// one of them, so listing them buries what a person spawned under a thousand spans of text.
    /// It is a menu toggle rather than a control on the panel, because it is a question about what
    /// the editor shows rather than about the world, and those belong together in one menu.
    /// </remarks>
    public static bool ShowInterface { get; set; }

    /// <summary>
    /// Whether entities that are in no place and have no name are listed.
    /// </summary>
    /// <remarks>
    /// Off. Most of a Bevy world is bookkeeping: an observer per event, a handful of entities per
    /// window and per monitor, one per gizmo layer. None of it is in the scene, none of it has a
    /// name, and a list of a thousand of them buries the six things a person put there. What is
    /// left when this is off is what has a name or a place, which is what "in the world" means.
    /// </remarks>
    public static bool ShowAll { get; set; }

    /// <summary>What each row says.</summary>
    [Bind("#wrow", Count = Rows)]
    public string[] Labels = new string[Rows];

    /// <summary>Which rows stand for an entity at all.</summary>
    [Show("#wrow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Narrows the list to entities whose name contains this.</summary>
    [Bind("#w-filter")]
    public string Filter = string.Empty;

    /// <summary>How much of the world is being shown.</summary>
    [Bind("#w-count", Mode = BindMode.OneWay)]
    public string Summary { get; private set; } = string.Empty;

    /// <summary>The hamburger, which is where everything the editor can do lives.</summary>
    [Bind("#w-menu", Mode = BindMode.OneWay)]
    public string MenuIcon => EditorIcons.Menu;

    /// <summary>Takes back the last change.</summary>
    [Bind("#w-undo", Mode = BindMode.OneWay)]
    public string UndoIcon => EditorIcons.Undo;

    /// <summary>Puts it back.</summary>
    [Bind("#w-redo", Mode = BindMode.OneWay)]
    public string RedoIcon => EditorIcons.Redo;

    /// <summary>Writes the world's edits and the layout.</summary>
    [Bind("#w-save", Mode = BindMode.OneWay)]
    public string SaveIcon => EditorIcons.Save;

    /// <summary>Adds something to the world.</summary>
    [Bind("#w-add", Mode = BindMode.OneWay)]
    public string AddIcon => EditorIcons.Add;

    /// <summary>What the rows currently stand for, so a click can be turned into an entity.</summary>
    private readonly Entity[] _entities = new Entity[Rows];

    /// <summary>The whole tree, flattened, with how deep each entity sits.</summary>
    private readonly List<TreeRow> _tree = [];

    /// <summary>Which entities are collapsed, so their children are not listed.</summary>
    private readonly HashSet<ulong> _collapsed = [];

    /// <summary>How far down the list the pool is looking.</summary>
    private int _scroll;

    /// <summary>The frame the tree was last built on, so it is not rebuilt every one.</summary>
    private ulong _built;

    /// <summary>How many entities there were then, which is the cheapest sign of a change.</summary>
    private int _population;

    /// <summary>The row a drag started on, for dropping one entity onto another.</summary>
    private int _dragging = -1;

    /// <summary>One entity in the flattened tree.</summary>
    private readonly record struct TreeRow(Entity Entity, string Name, int Depth, bool HasChildren);

    /// <summary>Builds the tree when it is stale, then fills the rows from it.</summary>
    /// <remarks>
    /// The tree is rebuilt a few times a second rather than every frame: walking a thousand
    /// entities and asking each for its name and its parent is not free, and a list of names does
    /// not need to be newer than that. What is drawn from it, including which row is selected, is
    /// written every frame.
    /// </remarks>
    [OnRefresh]
    public void Fill()
    {
        var world = EditorShell.Ecs;

        Roll();
        Rebuild(world);

        var wanted = Filter.Trim();
        var visible = new List<(Entity Entity, string Label)>();

        foreach (var row in _tree)
        {
            if (wanted.Length > 0)
            {
                // A search flattens the tree: what matters is finding the thing, not where it
                // sits, and an indented match under a collapsed parent would not be shown at all.
                if (!row.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase)) continue;

                visible.Add((row.Entity, "  " + row.Name));
                continue;
            }

            if (Folded(row.Entity)) continue;

            var mark = row.HasChildren
                ? (_collapsed.Contains(row.Entity.Bits) ? "+ " : "- ")
                : "  ";

            visible.Add((row.Entity, new string(' ', row.Depth * 2) + mark + row.Name));
        }

        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, visible.Count - Rows));

        var written = 0;
        for (var i = _scroll; i < visible.Count && written < Rows; i++)
        {
            var (entity, label) = visible[i];

            Labels[written] = entity == EditorSelection.Current
                ? EditorIcons.Selected + label[1..]
                : label;

            _entities[written] = entity;
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Rows; i++)
        {
            Labels[i] = string.Empty;
            _entities[i] = Entity.None;
            Shown[i] = false;
        }

        Summary = $"{Math.Min(_scroll + written, visible.Count)}/{visible.Count}";
    }

    /// <summary>Whether an entity is inside something that is collapsed.</summary>
    private bool Folded(Entity entity)
    {
        if (_collapsed.Count == 0) return false;

        var depth = -1;

        foreach (var row in _tree)
        {
            if (row.Entity == entity) return depth >= 0;

            // Walking the flattened tree rather than asking the world for parents again: a row
            // is hidden when a collapsed row above it is shallower and nothing shallower still
            // has closed the branch since.
            if (depth >= 0 && row.Depth <= depth) depth = -1;
            if (depth < 0 && _collapsed.Contains(row.Entity.Bits)) depth = row.Depth;
        }

        return false;
    }

    /// <summary>Walks the world into a flat list of rows with their depths.</summary>
    private void Rebuild(EcsWorld world)
    {
        var frame = EditorShell.Context?.Time.FrameCount ?? 0;
        var all = world.All();

        if (_tree.Count > 0 && all.Length == _population && frame - _built < 10) return;

        _built = frame;
        _population = all.Length;
        _tree.Clear();

        // One pass for the names and parents, so the tree is built from a table rather than by
        // asking the world about each entity again as it is reached.
        var names = new Dictionary<ulong, string>();
        var parents = new Dictionary<ulong, Entity>();

        var transform = EcsWorld.ComponentId<Transform>();

        foreach (var entity in all)
        {
            if (!ShowInterface && EditorEntity.IsInterface(world, entity)) continue;

            var name = world.NameOf(entity);
            if (!ShowAll && name is null && !world.HasById(entity, transform)) continue;

            names[entity.Bits] = name ?? $"Entity {entity.Index}";
            parents[entity.Bits] = world.ParentOf(entity);
        }

        var children = new Dictionary<ulong, List<Entity>>();
        var roots = new List<Entity>();

        foreach (var (bits, parent) in parents)
        {
            var entity = new Entity(bits);

            // A parent that is not itself listed, which is what the interface filter leaves
            // behind, makes its child a root: the alternative is a row nothing can reach.
            if (parent.IsNone || !names.ContainsKey(parent.Bits))
            {
                roots.Add(entity);
                continue;
            }

            if (!children.TryGetValue(parent.Bits, out var list))
            {
                list = [];
                children[parent.Bits] = list;
            }

            list.Add(entity);
        }

        // Named things first, because a name is what somebody meant. Everything the engine
        // spawned for itself is unnamed and sorts below.
        roots.Sort((a, b) => Compare(names, a, b));
        foreach (var list in children.Values) list.Sort((a, b) => Compare(names, a, b));

        foreach (var root in roots) Walk(root, 0);

        void Walk(Entity entity, int depth)
        {
            var mine = children.GetValueOrDefault(entity.Bits);

            _tree.Add(new TreeRow(
                entity,
                names.GetValueOrDefault(entity.Bits, $"Entity {entity.Index}"),
                depth,
                mine is { Count: > 0 }));

            if (mine is null) return;
            foreach (var child in mine) Walk(child, depth + 1);
        }
    }

    /// <summary>Orders two entities: named first, then alphabetically.</summary>
    private static int Compare(Dictionary<ulong, string> names, Entity a, Entity b)
    {
        var left = names.GetValueOrDefault(a.Bits, string.Empty);
        var right = names.GetValueOrDefault(b.Bits, string.Empty);

        var named = left.StartsWith("Entity ", StringComparison.Ordinal)
            .CompareTo(right.StartsWith("Entity ", StringComparison.Ordinal));

        return named != 0 ? named : string.CompareOrdinal(left, right);
    }

    /// <summary>Scrolls the list when the wheel is rolled over it.</summary>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        _scroll = Math.Max(0, _scroll - ((int)wheel * 3));
    }

    /// <summary>Selects a row, or folds it when the mark was clicked.</summary>
    /// <remarks>
    /// The fold mark is part of the row's text rather than a control of its own, because a row is
    /// one button and nothing can put a second one inside it. Where the click landed decides
    /// which of the two it was, which is the same rule a tree view uses anyway.
    /// </remarks>
    [Command("#wrow", Count = Rows)]
    public void Choose(int row)
    {
        var entity = _entities[row];
        if (entity.IsNone) return;

        if (EditorShell.Context is { } ctx
            && Window is { } window
            && Xui.TryRect(window.Element($"wrow-{row}"), out var rect)
            && ctx.Input.MouseX < rect.X + 10f + (DepthOf(entity) * 7f)
            && HasChildren(entity))
        {
            if (!_collapsed.Remove(entity.Bits)) _collapsed.Add(entity.Bits);
            return;
        }

        EditorSelection.Select(entity);
    }

    /// <summary>How deep a row sits, for working out where its fold mark is.</summary>
    private int DepthOf(Entity entity)
    {
        foreach (var row in _tree)
        {
            if (row.Entity == entity) return row.Depth;
        }

        return 0;
    }

    /// <summary>Whether a row has anything to fold.</summary>
    private bool HasChildren(Entity entity)
    {
        foreach (var row in _tree)
        {
            if (row.Entity == entity) return row.HasChildren;
        }

        return false;
    }

    /// <summary>Selects a row and offers what can be done to it.</summary>
    [Context("#wrow", Count = Rows)]
    public void RowMenu(int row)
    {
        var entity = _entities[row];
        if (entity.IsNone) return;

        EditorSelection.Select(entity);

        var (x, y) = EditorShell.Context?.Input.MousePosition ?? (0f, 0f);
        EditorShell.ShowMenu("Entity", x, y, EditorShell.Ecs.NameOf(entity) ?? "Entity");
    }

    /// <summary>Offers what can be done to the world.</summary>
    [Context("#world")]
    public void PanelMenu()
    {
        var (x, y) = EditorShell.Context?.Input.MousePosition ?? (0f, 0f);
        EditorShell.ShowMenu("Spawn", x, y, "Spawn");
    }

    /// <summary>Opens the editor's menu under the hamburger.</summary>
    [Command("#w-menu")]
    public void Menu() => OpenUnder("w-menu", string.Empty, null);

    /// <summary>Takes back the last change.</summary>
    [Command("#w-undo")]
    public void Undo() => EditorHistory.Undo(EditorShell.Ecs);

    /// <summary>Puts it back.</summary>
    [Command("#w-redo")]
    public void Redo() => EditorHistory.Redo(EditorShell.Ecs);

    /// <summary>Writes the world's edits and the arrangement of the panels.</summary>
    [Command("#w-save")]
    public void Save() => EditorProject.Save(EditorShell.Ecs);

    /// <summary>Offers what can be added to the world, under the button that asks.</summary>
    [Command("#w-add")]
    public void Add() => OpenUnder("w-add", "Spawn", "Spawn");

    /// <summary>Opens a menu under one of this panel's buttons.</summary>
    private void OpenUnder(string element, string path, string? title)
    {
        var below = Window is { } window && Xui.TryRect(window.Element(element), out var button)
            ? (button.X, button.Bottom + 6f)
            : (12f, 44f);

        EditorShell.ShowMenu(path, below.Item1, below.Item2, title);
    }

    /// <summary>Starts and finishes dragging one row onto another, which parents it.</summary>
    /// <remarks>
    /// Dropping a row on another makes it that one's child; dropping it past the last row detaches
    /// it. Both go through Bevy's own relationship, so transform propagation and the child lists
    /// follow, and the panel is showing the world rather than a picture of one.
    /// </remarks>
    [OnRefresh]
    public void DragRows()
    {
        if (EditorShell.Context is not { } ctx) return;
        if (Window is not { IsOpen: true } window) return;

        var (x, y) = ctx.Input.MousePosition;

        if (ctx.Input.MousePressed(MouseButton.Left)) _dragging = RowAt(window, x, y);

        if (!ctx.Input.MouseReleased(MouseButton.Left)) return;

        var from = _dragging;
        _dragging = -1;

        if (from < 0 || _entities[from].IsNone) return;

        var onto = RowAt(window, x, y);
        if (onto == from) return;

        var child = _entities[from];
        var world = EditorShell.Ecs;
        var previous = world.ParentOf(child);

        if (onto < 0)
        {
            // Dropped past the end of the list, which is the gesture for taking something out of
            // whatever it was in.
            if (!window.Covers(x, y) || previous.IsNone) return;

            world.ClearParent(child);
            EditorHistory.Record(
                "unparent",
                undo => undo.SetParent(child, previous),
                redo => redo.ClearParent(child));

            _built = 0;
            return;
        }

        var parent = _entities[onto];
        if (parent.IsNone || parent == child) return;

        world.SetParent(child, parent);
        EditorHistory.Record(
            "parent",
            undo =>
            {
                if (previous.IsNone) undo.ClearParent(child);
                else undo.SetParent(child, previous);
            },
            redo => redo.SetParent(child, parent));

        _built = 0;
    }

    /// <summary>Which row a point is over, or -1.</summary>
    private int RowAt(EditorWindow window, float x, float y)
    {
        for (var i = 0; i < Rows; i++)
        {
            if (!Shown[i]) continue;

            var element = window.Element($"wrow-{i}");
            if (element.IsNone) continue;
            if (!Xui.TryRect(element, out var rect)) continue;
            if (rect.Contains(x, y)) return i;
        }

        return -1;
    }
}

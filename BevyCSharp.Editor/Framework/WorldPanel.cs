using System.Numerics;
using Bevy;
using Bevy.Interop;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What is in the world, as a tree.
/// </summary>
/// <remarks>
/// Bevy's own word for the thing being listed, so the panel is called that. The tree is walked
/// again only when the population changes or enough frames have gone by that a rename would
/// otherwise never show, because a query and a sort per frame for a list that is the same list is
/// work for nothing, even in immediate mode.
/// </remarks>
public static class WorldPanel
{
    private static readonly List<Row> Rows = [];

    /// <summary>What has been folded away, by the entity that holds it.</summary>
    /// <remarks>
    /// What is folded rather than what is open, so a thing spawned into the world arrives visible.
    /// A list that remembered what was open would hide everything it has not seen before.
    /// </remarks>
    private static readonly HashSet<ulong> Folded = [];

    private static ulong _built;
    private static int _population;
    private static string _search = string.Empty;

    /// <summary>What is being renamed in place, and what has been typed so far.</summary>
    private static ulong _renaming;
    private static string _typed = string.Empty;

    /// <summary>Which rename has already been given the keyboard.</summary>
    private static ulong _started;

    /// <summary>The last row picked, which a range is measured from.</summary>
    private static ulong _anchor;

    /// <summary>How wide the eye's end of a row is.</summary>
    /// <remarks>
    /// Wide enough to press without aiming, and the room the name stops short of so that a long
    /// one does not run under the eye.
    /// </remarks>
    private const float EyeRoom = 24f;

    /// <summary>
    /// The room a row's fold arrow takes, whether or not the row has one.
    /// </summary>
    /// <remarks>
    /// One number, because it decides three things that have to agree, which are how far the arrow
    /// is drawn, how far a row without one indents so the names still line up, and how much of the
    /// front of a row a click folds rather than selects.
    /// </remarks>
    private static float Arrow => ImGui.GetTextLineHeight() + 2f;

    /// <summary>Whether this build has Bevy's <c>Visibility</c> at all.</summary>
    /// <remarks>
    /// Asked once. Resolving a component the bridge was compiled without throws rather than
    /// answering no, and a list that asked per row per frame would throw as often.
    /// </remarks>
    private static bool _eyes = true;

    /// <summary>
    /// Starts renaming a row, which is a box to type in where its name was.
    /// </summary>
    /// <remarks>
    /// The same state a double click on the name sets, so a rename started from the menu and one
    /// started by pointing at it are the same thing happening.
    /// </remarks>
    /// <param name="entity">Which row.</param>
    /// <param name="name">What it is called now, which the box opens holding.</param>
    public static void Rename(Entity entity, string name)
    {
        _renaming = entity.Bits;
        _typed = name;
    }

    /// <summary>Walks the world again on the next frame, after something changed where things sit.</summary>
    /// <remarks>
    /// The list is walked again when the number of entities changes, and a change of parent leaves
    /// that alone, so whatever moves one says so here. Marked rather than cleared, since the move
    /// usually comes from a drop on a row while the list is being drawn, and clearing it then pulls
    /// the rows out from under the loop drawing them.
    /// </remarks>
    public static void Invalidate() => _stale = true;

    /// <summary>Whether the list has to be walked again whatever the count says.</summary>
    private static bool _stale;

    /// <summary>Opens a row that was folded away, so what was just put under it can be seen.</summary>
    /// <param name="entity">Which row.</param>
    public static void Unfold(Entity entity) => Folded.Remove(entity.Bits);

    /// <summary>What is being dragged, fixed when the drag began.</summary>
    /// <remarks>
    /// The row under the pointer, and with it the rest of the selection when that row is part of
    /// it, as dragging one of several chosen files drags them all.
    /// </remarks>
    private static Entity[] _dragging = [];

    /// <summary>What ImGui calls a drag of rows, which only a row accepts.</summary>
    private const string Dragged = "bcs.entities";

    /// <summary>How far in a row's picture sits, which makes the list a tree.</summary>
    private static float Indent(Row row) =>
        EditorSurface.Air + (row.Depth * ImGui.GetStyle().IndentSpacing);

    /// <summary>
    /// What an entity asks to be drawn, and whether it asks at all.
    /// </summary>
    /// <remarks>
    /// Only the ones carrying the component get an eye. Adding one to an entity that is not drawn
    /// would put a control on a row where it means nothing, and everything Bevy draws carries it
    /// already.
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">Which entity.</param>
    /// <param name="sight">What it asks for, when it asks.</param>
    private static bool Sighted(BehaviorContext ctx, Entity entity, out Visibility sight)
    {
        sight = default;
        if (!_eyes) return false;

        try
        {
            return ctx.Ecs.TryGet(entity, out sight);
        }
        catch (BevyNativeException)
        {
            _eyes = false;
            return false;
        }
    }

    /// <summary>One line of the list.</summary>
    /// <param name="Entity">What the row stands for.</param>
    /// <param name="Name">What it is called.</param>
    /// <param name="Depth">How far in it sits, which makes a list read as a tree.</param>
    /// <param name="HasChildren">Whether anything hangs under it.</param>
    /// <param name="Icon">The picture it wears, under the asset root.</param>
    private readonly record struct Row(
        Entity Entity, string Name, int Depth, bool HasChildren, string Icon);

    /// <summary>Draws the list.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        // No heading, since a list of names says what it is. The search box takes the whole row.
        EditorSurface.FullWidth();
        ImGui.InputTextWithHint("##search", "Search", ref _search, 128);

        ImGui.Spacing();

        Walk(ctx);

        var wanted = _search.Trim();

        // The room the button at the bottom keeps for itself, taken out of the scrolling list rather
        // than scrolled with it, as the button that adds a component keeps its row in the panel
        // below. Adding something is not a thing to go looking for past everything already there.
        var button = ImGui.GetFrameHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2f);
        var room = ImGui.GetContentRegionAvail().Y - button;

        if (!EditorSurface.Region("##rows", new Vector2(0f, MathF.Max(1f, room))))
        {
            // Ended whether or not it opened, as a child window requires.
            EditorSurface.EndRegion();
            Add();
            return;
        }

        // A fold reaches everything under a folded row, until something at its own
        // depth or shallower comes along.
        var hidden = -1;

        foreach (var row in Rows)
        {
            if (hidden >= 0 && row.Depth > hidden) continue;

            hidden = -1;

            if (wanted.Length > 0 && !row.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Line(ctx, row);

            // A search shows what matches wherever it is, so nothing is folded while one is on.
            if (wanted.Length == 0 && row.HasChildren && Folded.Contains(row.Entity.Bits))
            {
                hidden = row.Depth;
            }
        }

        // The room under the last row, which is part of the list and means nothing is chosen. A
        // list somebody can add to a selection with but never clear it from is a list they have to
        // leave to start again.
        var rest = ImGui.GetContentRegionAvail();

        if (rest.Y > 1f)
        {
            ImGui.InvisibleButton("##empty", new Vector2(MathF.Max(1f, rest.X), rest.Y));

            if (ImGui.IsItemClicked()) EditorSelection.Clear();

            // And dropping a row there takes it out from under whatever it was in, which is the
            // one place in the list that stands for the top of the world.
            _ = Target(ctx, Entity.None);
        }

        EditorSurface.EndRegion();

        Add();
    }

    /// <summary>The button that opens the window that adds an entity, across the bottom.</summary>
    private static void Add()
    {
        ImGui.Spacing();

        if (ImGui.Button("Add Entity", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight())))
        {
            AddEntityWindow.Open();
        }

        if (ImGui.IsItemHovered()) EditorWidgets.Tip("Ctrl+A");
    }

    /// <summary>
    /// A row that is being renamed, a box to type in and nothing else until Enter or Escape.
    /// </summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="row">Which row is being renamed.</param>
    /// <param name="width">How wide the list is.</param>
    private static void Rename(BehaviorContext ctx, Row row, float width)
    {
        ImGui.SetNextItemWidth(width);

        // The keyboard goes to the box the frame it appears, so a name can be typed without
        // clicking the thing that was just double-clicked.
        if (_started != row.Entity.Bits)
        {
            _started = row.Entity.Bits;
            ImGui.SetKeyboardFocusHere();
        }

        var done = ImGui.InputText(
            $"##rename{row.Entity.Bits}",
            ref _typed,
            128,
            ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);

        if (done && _typed.Trim() is { Length: > 0 } name)
        {
            EditorEntity.Rename(ctx.Ecs, row.Entity, name.Trim());
            _renaming = 0;
            _started = 0;

            // Walked again, because the list is sorted by name.
            _built = 0;
        }

        // Let go of it by pressing Escape or by clicking somewhere else.
        if (ImGui.IsKeyPressed(ImGuiKey.Escape)
            || (_started == row.Entity.Bits && !ImGui.IsItemActive() && !ImGui.IsItemFocused()))
        {
            _renaming = 0;
            _started = 0;
        }
    }

    /// <summary>
    /// One row, a pill the width of the list with a picture and a name in it.
    /// </summary>
    /// <remarks>
    /// Drawn rather than asked for, because a selectable is a rectangle with square corners in this
    /// version of ImGui and everything else in the editor is rounded. Besides the shape, that buys
    /// the fill running the whole width behind the indent, which makes a list of nested things read
    /// as rows rather than as ragged text.
    /// </remarks>
    private static void Line(BehaviorContext ctx, Row row)
    {
        var picked = EditorSelection.All.Contains(row.Entity);
        var theme = EditorTheme.Current;

        var height = ImGui.GetFrameHeight();
        var width = ImGui.GetContentRegionAvail().X;
        var at = ImGui.GetCursorScreenPos();

        if (_renaming == row.Entity.Bits)
        {
            Rename(ctx, row, width);
            return;
        }

        ImGui.InvisibleButton($"##row{row.Entity.Bits}", new Vector2(width, height));

        // Picked up and put down on another row to go under it, before anything else asks about
        // the row, since a drag and a drop both belong to the button just made.
        Source(row);
        var dropping = Target(ctx, row.Entity);

        var over = ImGui.IsItemHovered();

        var eyed = Sighted(ctx, row.Entity, out var sight);
        var onEye = eyed && over && ImGui.GetIO().MousePos.X >= at.X + width - EyeRoom;

        // The arrow at the front of a row that has children is its own target, as wide as the
        // picture it stands beside.
        var onArrow = row.HasChildren
            && over
            && ImGui.GetIO().MousePos.X < at.X + Indent(row) + Arrow;

        // Twice on a name is how a name is changed, in every list of things there is.
        if (over && !onEye && !onArrow && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            Rename(row.Entity, row.Name);
            return;
        }

        if (ImGui.IsItemClicked())
        {
            // The eye takes the click that lands on it, so putting something away never also picks
            // it. Back to inherited rather than to visible, because its parent says what a thing
            // was before it was hidden, and forcing it on outlives the hierarchy.
            if (onEye)
            {
                var was = sight;
                var now = sight.Mode == VisibilityMode.Hidden
                    ? Visibility.Inherited
                    : Visibility.Hidden;

                var which = row.Entity;

                ctx.Ecs.Set(which, now);

                // Taken back like anything else. Putting something away by accident is the easiest
                // mistake to make in a list, and an editor where one of the things a click does
                // cannot be undone is one nobody trusts the rest of.
                //
                // No key, so each click is its own entry rather than the last one continued,
                // because two clicks on an eye are two changes of mind rather than one edit still
                // being made.
                EditorHistory.Record(
                    now.Mode == VisibilityMode.Hidden ? $"hide {row.Name}" : $"show {row.Name}",
                    undo => undo.Set(which, was),
                    redo => redo.Set(which, now));
            }

            // And the arrow takes its own, so folding a branch away leaves the selection alone.
            else if (onArrow)
            {
                if (!Folded.Add(row.Entity.Bits)) Folded.Remove(row.Entity.Bits);
            }

            // Control adds one, shift takes everything between this and the last one picked, and
            // neither replaces what was chosen, as every list of things anywhere behaves.
            else if (ctx.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]))
            {
                EditorSelection.Toggle(row.Entity);
                _anchor = row.Entity.Bits;
            }
            else if (ctx.Input.AnyKeyDown([Key.ShiftLeft, Key.ShiftRight]) && _anchor != 0)
            {
                Range(row.Entity);
            }
            else
            {
                EditorSelection.Select(row.Entity);
                _anchor = row.Entity.Bits;
            }
        }

        if (EditorWidgets.FlyoutHere($"##menu{row.Entity.Bits}"))
        {
            EditorSelection.Select(row.Entity);

            RoundedRows.Rows(() =>
            {
                // What a row itself offers, which a double click and a key are otherwise the only
                // way to reach.
                if (ImGui.MenuItem("Rename")) Rename(row.Entity, row.Name);

                RoundedRows.Row();

                if (ImGui.MenuItem("Duplicate", "Ctrl+D"))
                    EditorMenu.Find("Entity/Duplicate")?.Run?.Invoke(ctx.Ecs);

                RoundedRows.Row();

                if (ImGui.MenuItem("Delete", "Del")) EditorMenu.Find("Entity/Delete")?.Run?.Invoke(ctx.Ecs);

                RoundedRows.Row();
            });

            EditorWidgets.EndFlyout();
        }

        Paint(row, at, width, height, picked, over, eyed);

        // Over the fill, so a row that is also selected still shows it would take the drop.
        if (dropping)
        {
            ImGui.GetWindowDrawList().AddRect(
                at,
                at + new Vector2(width, height),
                ImGui.GetColorU32(EditorTheme.LiveAccent),
                ImGui.GetStyle().FrameRounding,
                ImDrawFlags.None,
                1.5f);
        }

        // On the row while the pointer is on it, and on a hidden one whether or not, because what
        // says a thing has been put away has to still be there once the hand has moved on.
        if (eyed && (over || sight.Mode == VisibilityMode.Hidden))
        {
            EditorDraw.Eye(
                ImGui.GetWindowDrawList(),
                new Vector2(at.X + width - (EyeRoom * 0.5f), at.Y + (height * 0.5f)),
                ImGui.GetTextLineHeight() * 0.95f,
                sight.Mode != VisibilityMode.Hidden,
                ImGui.GetColorU32(EditorTheme.Alpha(
                    EditorTheme.LiveText,
                    sight.Mode == VisibilityMode.Hidden ? 0.85f : 0.7f)));
        }
    }

    /// <summary>
    /// What a row looks like: its fill, its fold arrow, its picture and its name.
    /// </summary>
    /// <remarks>
    /// Drawn rather than laid out. The invisible button is the row as far as ImGui is concerned,
    /// and moving the cursor to put things inside it is how a window ends up believing it is
    /// taller than it is.
    /// </remarks>
    /// <param name="row">What to draw.</param>
    /// <param name="at">Where the row starts, on the screen.</param>
    /// <param name="width">How wide it is.</param>
    /// <param name="height">How tall it is.</param>
    /// <param name="picked">Whether it is one of the selected.</param>
    /// <param name="over">Whether the pointer is on it.</param>
    /// <param name="eyed">Whether the row ends in an eye, which the name stops short of.</param>
    private static void Paint(
        Row row, Vector2 at, float width, float height, bool picked, bool over, bool eyed)
    {
        var theme = EditorTheme.Current;

        // Everything after the button is drawn rather than laid out, because the button is the row
        // as far as ImGui is concerned, and moving the cursor to put things inside it is how a
        // window ends up believing it is taller than it is.
        var draw = ImGui.GetWindowDrawList();

        if (picked || over)
        {
            // The one the details panel is showing wears the accent outright; the rest of a
            // selection wears it at half strength. A dozen rows all at full accent says which
            // twelve were picked and not which one is being looked at, which is the thing somebody
            // is about to edit.
            //
            // Under the stock look the row wears what ImGui says a chosen row wears instead. A
            // theme taken whole is taken whole.
            var current = row.Entity == EditorSelection.Current;

            var fill = theme.Stock
                ? ImGui.GetColorU32(picked ? ImGuiCol.Header : ImGuiCol.HeaderHovered)
                : ImGui.GetColorU32(picked
                    ? current ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveAccent, 0.45f)
                    : EditorTheme.LiveLift);

            // Cut to the list rather than run under its edge, so a row scrolled half out of sight
            // ends in a rounded corner instead of a square one.
            var top = at;
            var bottom = at + new Vector2(width, height);

            if (EditorSurface.Clipped(ref top, ref bottom))
            {
                EditorDraw.Rounded(top, bottom, ImGui.GetStyle().FrameRounding, fill, draw);
            }
        }

        var line = ImGui.GetTextLineHeight();
        var indent = Indent(row);
        var middle = at.Y + ((height - line) * 0.5f);

        // A triangle for anything with things under it, pointing down when they are showing and
        // right when they are folded away. Drawn where an arrow goes and hit where it is drawn.
        if (row.HasChildren)
        {
            var folded = Folded.Contains(row.Entity.Bits);
            var arrow = new Vector2(at.X + indent, middle);
            var mark = line * 0.34f;

            var color = ImGui.GetColorU32(
                EditorTheme.Alpha(EditorTheme.LiveText, over || picked ? 0.9f : 0.6f));
            var center = arrow + new Vector2(line * 0.5f, line * 0.5f);

            if (folded)
            {
                draw.AddTriangleFilled(
                    center + new Vector2(-mark * 0.6f, -mark),
                    center + new Vector2(-mark * 0.6f, mark),
                    center + new Vector2(mark * 0.8f, 0f),
                    color);
            }
            else
            {
                draw.AddTriangleFilled(
                    center + new Vector2(-mark, -mark * 0.6f),
                    center + new Vector2(mark, -mark * 0.6f),
                    center + new Vector2(0f, mark * 0.8f),
                    color);
            }

            indent += Arrow;
        }
        else if (Rows.Exists(other => other.HasChildren))
        {
            // Kept in step with the rows that do have an arrow, so names line up in a column.
            indent += Arrow;
        }

        EditorDraw.Icon(draw, row.Icon, new Vector2(at.X + indent, middle), line, picked);

        // Cut where the eye begins rather than where the row ends, so a long name runs out of
        // room instead of running under the thing that hides it.
        draw.PushClipRect(
            at,
            new Vector2(at.X + width - (eyed ? EyeRoom : 0f), at.Y + height),
            true);

        draw.AddText(
            new Vector2(at.X + indent + line + ImGui.GetStyle().ItemSpacing.X, middle),
            ImGui.GetColorU32(EditorTheme.Ink(picked)),
            row.Name);

        draw.PopClipRect();
    }

    /// <summary>Starts dragging a row, and with it the rest of the selection if it is part of it.</summary>
    /// <remarks>
    /// The payload is empty and says only that rows are being dragged. Which rows is kept here,
    /// since the thing dropped on is always this list and a pointer to managed memory handed
    /// through ImGui would be one that outlives what it points at.
    /// </remarks>
    private static void Source(Row row)
    {
        if (!ImGui.BeginDragDropSource()) return;

        // Every frame of the drag, which gives the same answer each time, since nothing changes
        // the selection while the button is held on a row.
        _dragging = EditorSelection.All.Contains(row.Entity)
            ? [.. EditorSelection.All]
            : [row.Entity];

        ImGui.SetDragDropPayload(Dragged, IntPtr.Zero, 0);

        ImGui.TextUnformatted(_dragging.Length > 1 ? $"{row.Name} and {_dragging.Length - 1} more" : row.Name);

        ImGui.EndDragDropSource();
    }

    /// <summary>
    /// Takes rows dropped on the item just made, which puts them under
    /// <paramref name="parent"/>, or at the top of the world for none.
    /// </summary>
    /// <remarks>
    /// A row that cannot go there, because it is the row dropped on or above it, is refused
    /// before the drop, so the row under the pointer does not light up for a drop that would do
    /// nothing. The row is lit with the accent round its edge rather than ImGui's square box,
    /// since every other row here is rounded.
    /// </remarks>
    /// <returns>Whether rows are held over it that it would take, which lights it.</returns>
    private static bool Target(BehaviorContext ctx, Entity parent)
    {
        if (_dragging.Length == 0 || !ImGui.BeginDragDropTarget()) return false;

        var lit = false;
        var fits = _dragging.Any(child => child != parent && EditorHierarchy.CanParent(ctx.Ecs, child, parent));

        if (fits)
        {
            // Before the button is let go as well as on it, which lights the row while
            // the rows are held over it rather than only once they have landed.
            var payload = ImGui.AcceptDragDropPayload(
                Dragged,
                ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery);

            if (Held(payload))
            {
                lit = true;

                if (payload.IsDelivery())
                {
                    EditorHierarchy.Reparent(ctx.Ecs, _dragging, parent);
                    if (!parent.IsNone) Unfold(parent);

                    _dragging = [];
                }
            }
        }

        ImGui.EndDragDropTarget();

        return lit;
    }

    /// <summary>Whether ImGui handed a payload back rather than none.</summary>
    /// <remarks>
    /// The wrapper is a pointer and nothing else, read as one here so the editor needs no unsafe
    /// code to ask whether it is null.
    /// </remarks>
    private static bool Held(ImGuiPayloadPtr payload) =>
        System.Runtime.CompilerServices.Unsafe.As<ImGuiPayloadPtr, IntPtr>(ref payload) != IntPtr.Zero;

    /// <summary>
    /// Chooses everything between the last row picked and this one.
    /// </summary>
    /// <remarks>
    /// Measured down the list as it is drawn rather than through the tree, because "everything
    /// between these two" means what somebody can see between them.
    /// </remarks>
    private static void Range(Entity to)
    {
        var first = Rows.FindIndex(row => row.Entity.Bits == _anchor);
        var last = Rows.FindIndex(row => row.Entity == to);

        if (first < 0 || last < 0) return;

        if (first > last) (first, last) = (last, first);

        EditorSelection.Clear();

        for (var index = first; index <= last; index++)
        {
            EditorSelection.Toggle(Rows[index].Entity);
        }
    }

    /// <summary>Walks the world into a flat list of rows carrying their depth.</summary>
    private static void Walk(BehaviorContext ctx)
    {
        var all = ctx.Ecs.All();

        if (!_stale && Rows.Count > 0 && all.Length == _population && EditorShell.Frame - _built < 30) return;

        _stale = false;
        _built = EditorShell.Frame;
        _population = all.Length;

        Rows.Clear();

        var names = new Dictionary<ulong, string>();
        var parents = new Dictionary<ulong, Entity>();

        foreach (var entity in all)
        {
            if (EditorEntity.IsInterface(ctx.Ecs, entity)) continue;

            // The scene view's camera is the editor's, not the world's, and is set from its own
            // button in the scene's corner, so it is not listed with what the world holds.
            if (entity == EditorSelection.Camera) continue;
            if (EditorEntity.IsBookkeeping(ctx.Ecs, entity)) continue;

            // Named, or drawn. A thing with a mesh is in the world whether or not anybody called
            // it anything, and leaving it out of the list is how an object ends up visible in the
            // viewport, selectable by clicking it, and absent from the one place that lists what
            // is there. Everything else without a name is the engine's own.
            if (ctx.Ecs.NameOf(entity) is not { Length: > 0 } name)
            {
                if (!Render.TryGetBounds(entity, out _, out _)) continue;

                name = $"Entity {entity.Index}";
            }

            names[entity.Bits] = name;
            parents[entity.Bits] = ctx.Ecs.ParentOf(entity);
        }

        var children = new Dictionary<ulong, List<Entity>>();
        var roots = new List<Entity>();

        foreach (var (bits, parent) in parents)
        {
            var entity = new Entity(bits);

            // A parent that is not itself listed makes its child a root, because the alternative is
            // a row nothing can reach.
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

        roots.Sort(Compare);
        foreach (var list in children.Values) list.Sort(Compare);

        foreach (var root in roots) Add(root, 0);

        // A fold on something that is no longer there is a fold that hides the next thing to take
        // its place in storage.
        Folded.RemoveWhere(bits => !names.ContainsKey(bits));

        int Compare(Entity a, Entity b) => EditorSort.Naturally(
            names.GetValueOrDefault(a.Bits), names.GetValueOrDefault(b.Bits));

        void Add(Entity entity, int depth)
        {
            var mine = children.GetValueOrDefault(entity.Bits);

            Rows.Add(new Row(
                entity,
                names.GetValueOrDefault(entity.Bits, $"Entity {entity.Index}"),
                depth,
                mine is { Count: > 0 },
                EditorKinds.IconFor(ctx.Ecs, entity)));

            if (mine is null) return;

            foreach (var child in mine) Add(child, depth + 1);
        }
    }
}

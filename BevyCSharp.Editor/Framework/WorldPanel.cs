using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What is in the world, as a tree.
/// </summary>
/// <remarks>
/// Bevy's own word for the thing being listed, so that is what the panel is called. The tree is
/// walked again only when the population changes or enough frames have gone by that a rename would
/// otherwise never show: a query and a sort per frame for a list that is the same list is work for
/// nothing, even in immediate mode.
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

    /// <summary>The last row picked, which is what a range is measured from.</summary>
    private static ulong _anchor;

    /// <summary>One line of the list.</summary>
    /// <param name="Entity">What the row stands for.</param>
    /// <param name="Name">What it is called.</param>
    /// <param name="Depth">How far in it sits, which is what makes a list read as a tree.</param>
    /// <param name="HasChildren">Whether anything hangs under it.</param>
    /// <param name="Icon">The picture it wears, under the asset root.</param>
    private readonly record struct Row(
        Entity Entity, string Name, int Depth, bool HasChildren, string Icon);

    /// <summary>Draws the list.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        ImGui.TextDisabled("WORLD");
        ImGui.SameLine();
        ImGui.TextDisabled($"({Rows.Count})");

        // Short of the dock button when it happens to float over this corner.
        ImGui.SetNextItemWidth(-1f - EditorShell.DockRoom());
        ImGui.InputTextWithHint("##search", "Search", ref _search, 128);

        ImGui.Spacing();

        Walk(ctx);

        var wanted = _search.Trim();

        // No fill of its own. The card this panel is drawn in is the surface, and a second one
        // filling it edge to edge with no padding is the box inside a box this look does without.
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        var open = ImGui.BeginChild("##rows", new Vector2(0f, 0f));

        ImGui.PopStyleColor();

        if (!open) return;

        // How deep a fold reaches: everything under a folded row, until something at its own
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
        }

        ImGui.EndChild();
    }

    /// <summary>
    /// One row: a pill the width of the list, with a picture and a name in it.
    /// </summary>
    /// <remarks>
    /// Drawn rather than asked for, because a selectable is a rectangle with square corners in this
    /// version of ImGui and everything else in the editor is rounded. What that buys besides the
    /// shape is the fill running the whole width behind the indent, which is what makes a list of
    /// nested things read as rows rather than as ragged text.
    /// </remarks>
    private static void Line(BehaviorContext ctx, Row row)
    {
        var picked = EditorSelection.All.Contains(row.Entity);
        var theme = EditorTheme.Current;

        var height = ImGui.GetFrameHeight();
        var width = ImGui.GetContentRegionAvail().X;
        var at = ImGui.GetCursorScreenPos();

        // Being renamed: the row is a box to type in and nothing else, until Enter or Escape.
        if (_renaming == row.Entity.Bits)
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
                ctx.Ecs.SetName(row.Entity, name.Trim());
                _renaming = 0;
                _started = 0;

                // Walked again, because a name is what the list is sorted by.
                _built = 0;
            }

            // Let go of it by pressing Escape or by clicking somewhere else.
            if (ImGui.IsKeyPressed(ImGuiKey.Escape)
                || (_started == row.Entity.Bits && !ImGui.IsItemActive() && !ImGui.IsItemFocused()))
            {
                _renaming = 0;
                _started = 0;
            }

            return;
        }

        ImGui.InvisibleButton($"##row{row.Entity.Bits}", new Vector2(width, height));

        var over = ImGui.IsItemHovered();

        // Twice on a name is how a name is changed, in every list of things there is.
        if (over && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            _renaming = row.Entity.Bits;
            _typed = row.Name;
            return;
        }

        if (ImGui.IsItemClicked())
        {
            // Control adds one, shift takes everything between this and the last one picked, and
            // neither replaces what was chosen. Which is what every list of things anywhere does.
            if (ctx.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]))
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

        if (ImGui.BeginPopupContextItem($"##menu{row.Entity.Bits}"))
        {
            EditorSelection.Select(row.Entity);

            if (ImGui.MenuItem("Delete")) EditorMenu.Find("Entity/Delete")?.Run?.Invoke(ctx.Ecs);
            if (ImGui.MenuItem("Duplicate")) EditorMenu.Find("Entity/Duplicate")?.Run?.Invoke(ctx.Ecs);

            ImGui.EndPopup();
        }

        // Everything after the button is drawn rather than laid out: the button is the row as far
        // as ImGui is concerned, and moving the cursor to put things inside it is how a window ends
        // up believing it is taller than it is.
        var draw = ImGui.GetWindowDrawList();

        if (picked || over)
        {
            // Under the stock look the row wears what ImGui says a chosen row wears, rather than
            // the editor's own accent: a theme taken whole is taken whole.
            // The one the details panel is showing wears the accent outright; the rest of a
            // selection wears it at half strength. A dozen rows all at full accent says which
            // twelve were picked and not which one is being looked at, which is the thing somebody
            // is about to edit.
            var current = row.Entity == EditorSelection.Current;

            var fill = theme.Stock
                ? ImGui.GetColorU32(picked ? ImGuiCol.Header : ImGuiCol.HeaderHovered)
                : ImGui.GetColorU32(picked
                    ? current ? EditorTheme.LiveAccent : EditorTheme.Alpha(EditorTheme.LiveAccent, 0.45f)
                    : EditorTheme.LiveHover);

            draw.AddRectFilled(at, at + new Vector2(width, height), fill, ImGui.GetStyle().FrameRounding);
        }

        var line = ImGui.GetTextLineHeight();
        var indent = 6f + (row.Depth * ImGui.GetStyle().IndentSpacing);
        var middle = at.Y + ((height - line) * 0.5f);

        // A triangle for anything with things under it, pointing down when they are showing and
        // right when they are folded away. Drawn where an arrow goes and hit where it is drawn.
        if (row.HasChildren)
        {
            var folded = Folded.Contains(row.Entity.Bits);
            var arrow = new Vector2(at.X + indent, middle);
            var mark = line * 0.34f;

            var colour = ImGui.GetColorU32(
                EditorTheme.Alpha(EditorTheme.LiveText, over || picked ? 0.9f : 0.6f));
            var centre = arrow + new Vector2(line * 0.5f, line * 0.5f);

            if (folded)
            {
                draw.AddTriangleFilled(
                    centre + new Vector2(-mark * 0.6f, -mark),
                    centre + new Vector2(-mark * 0.6f, mark),
                    centre + new Vector2(mark * 0.8f, 0f),
                    colour);
            }
            else
            {
                draw.AddTriangleFilled(
                    centre + new Vector2(-mark, -mark * 0.6f),
                    centre + new Vector2(mark, -mark * 0.6f),
                    centre + new Vector2(0f, mark * 0.8f),
                    colour);
            }

            // The arrow is its own target: clicking it folds, clicking the rest of the row picks.
            if (over
                && ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                && ImGui.GetIO().MousePos.X < arrow.X + line + 4f)
            {
                if (folded) Folded.Remove(row.Entity.Bits);
                else Folded.Add(row.Entity.Bits);
            }

            indent += line + 2f;
        }
        else if (Rows.Exists(other => other.HasChildren))
        {
            // Kept in step with the rows that do have an arrow, so names line up in a column.
            indent += line + 2f;
        }

        if (ImGuiTextures.Load(row.Icon) is var picture && picture != 0)
        {
            draw.AddImage(
                (IntPtr)picture,
                new Vector2(at.X + indent, middle),
                new Vector2(at.X + indent + line, middle + line),
                Vector2.Zero,
                Vector2.One,
                ImGui.GetColorU32(EditorTheme.IconTint(picked)));
        }

        draw.AddText(
            new Vector2(at.X + indent + line + 6f, middle),
            ImGui.GetColorU32(picked ? EditorTheme.LiveText : EditorTheme.Alpha(EditorTheme.LiveText, 0.88f)),
            row.Name);
    }

    /// <summary>
    /// Chooses everything between the last row picked and this one.
    /// </summary>
    /// <remarks>
    /// Measured down the list as it is drawn rather than through the tree, because what somebody
    /// means by "everything between these two" is what they can see between them.
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

        if (Rows.Count > 0 && all.Length == _population && EditorShell.Frame - _built < 30) return;

        _built = EditorShell.Frame;
        _population = all.Length;

        Rows.Clear();

        var names = new Dictionary<ulong, string>();
        var parents = new Dictionary<ulong, Entity>();

        foreach (var entity in all)
        {
            if (EditorEntity.IsInterface(ctx.Ecs, entity)) continue;
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

            // A parent that is not itself listed makes its child a root: the alternative is a row
            // nothing can reach.
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

        int Compare(Entity a, Entity b) => string.Compare(
            names.GetValueOrDefault(a.Bits), names.GetValueOrDefault(b.Bits),
            StringComparison.OrdinalIgnoreCase);

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

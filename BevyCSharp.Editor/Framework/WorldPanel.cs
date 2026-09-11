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

    private static ulong _built;
    private static int _population;
    private static string _search = string.Empty;

    /// <summary>One line of the list.</summary>
    /// <param name="Entity">What the row stands for.</param>
    /// <param name="Name">What it is called.</param>
    /// <param name="Depth">How far in it sits, which is what makes a list read as a tree.</param>
    /// <param name="HasChildren">Whether anything hangs under it.</param>
    private readonly record struct Row(
        Entity Entity, string Name, int Depth, bool HasChildren, string Icon);

    /// <summary>Draws the list.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        ImGui.TextDisabled("WORLD");
        ImGui.SameLine();
        ImGui.TextDisabled($"({Rows.Count})");

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##search", "Search", ref _search, 128);

        ImGui.Separator();

        Walk(ctx);

        var wanted = _search.Trim();

        if (!ImGui.BeginChild("##rows", new Vector2(0f, 0f))) return;

        foreach (var row in Rows)
        {
            if (wanted.Length > 0 && !row.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var picked = EditorSelection.All.Contains(row.Entity);

            // The accent means one thing: this is what is chosen. Pushed here rather than set on
            // the theme, because the same colour draws every collapsing header in the editor.
            // Under the stock look ImGui already draws a selected row as it thinks best, and a
            // colour of ours over it would be the editor disagreeing with the theme it was asked
            // to wear.
            var mark = picked && !EditorTheme.Current.Stock;

            if (mark)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, EditorTheme.Current.Accent);
                ImGui.PushStyleColor(
                    ImGuiCol.HeaderHovered,
                    EditorTheme.Alpha(EditorTheme.Current.Accent, 0.9f));
            }

            ImGui.Indent(row.Depth * ImGui.GetStyle().IndentSpacing);

            // The picture first, then the row it belongs to, on one line. Drawn before the
            // selectable so the highlight runs the whole width behind both.
            ImGuiTextures.Draw(row.Icon, 14f, EditorTheme.IconTint(picked));
            ImGui.SameLine(0f, 6f);

            if (ImGui.Selectable($"{row.Name}##{row.Entity.Bits}", picked))
            {
                // Holding control adds to what is chosen rather than replacing it, which is what
                // every list of things anywhere does.
                if (ctx.Input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]))
                {
                    EditorSelection.Toggle(row.Entity);
                }
                else
                {
                    EditorSelection.Select(row.Entity);
                }
            }

            if (ImGui.BeginPopupContextItem($"##menu{row.Entity.Bits}"))
            {
                EditorSelection.Select(row.Entity);

                if (ImGui.MenuItem("Delete")) EditorMenu.Find("Entity/Delete")?.Run?.Invoke(ctx.Ecs);
                if (ImGui.MenuItem("Duplicate")) EditorMenu.Find("Entity/Duplicate")?.Run?.Invoke(ctx.Ecs);

                ImGui.EndPopup();
            }

            ImGui.Unindent(row.Depth * ImGui.GetStyle().IndentSpacing);

            if (mark) ImGui.PopStyleColor(2);
        }

        ImGui.EndChild();
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
            if (ctx.Ecs.NameOf(entity) is not { Length: > 0 } name) continue;

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

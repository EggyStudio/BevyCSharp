using System.Text;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor puts in its page, and when.
/// </summary>
/// <remarks>
/// <para>
/// Each view writes markup into a part of the page and says what a click on it means. There is no
/// widget to make, no size to work out and no order to keep: an element carries the state it is in
/// as an attribute, and the stylesheet decides what being in that state looks like.
/// </para>
/// <para>
/// Markup is written again only when what it says has changed, because writing it parses it. What
/// has changed is a short string per view rather than a comparison of trees.
/// </para>
/// </remarks>
public static class EditorViews
{
    private static readonly Dictionary<string, string> Written = [];
    private static readonly List<TreeRow> Rows = [];

    private static ulong _built;
    private static int _population;
    private static string _search = string.Empty;

    /// <summary>One line of the world list.</summary>
    /// <param name="Entity">What the row stands for.</param>
    /// <param name="Name">What it is called.</param>
    /// <param name="Icon">The picture it wears, under the asset root.</param>
    /// <param name="Depth">How far in it sits, which is what makes a list read as a tree.</param>
    private readonly record struct TreeRow(Entity Entity, string Name, string Icon, int Depth);

    /// <summary>What the world list is filtered by.</summary>
    public static string Search
    {
        get => _search;
        set
        {
            _search = value ?? string.Empty;

            // Written again on the next frame, since what is shown is not what it was.
            Written.Remove("hierarchy");
        }
    }

    /// <summary>Says what the fixed parts of the page do.</summary>
    public static void Install()
    {
        // What a field reported. The console is the one that has an Enter to act on today.
        EditorShell.Accepted = static id =>
        {
            if (id == "console-entry")
            {
                ConsolePage.Submit();
                return;
            }

            if (id == "search")
            {
                Search = Dom.GetValue(EditorShell.Part("search"));
                return;
            }

            if (EditorShell.Context is { } ctx) InspectorPage.Accepted(ctx, id);
        };

        // The menu, which is the way to everything the editor can be told to do.
        EditorShell.OnClick("menu", static () =>
        {
            var at = EditorShell.Part("menu");

            if (Dom.TryRect(at, out var rect)) EditorShell.ToggleMenu(string.Empty, rect.Left, rect.Bottom + 4f);
        });

        EditorShell.OnClick("tool-select", static () => EditorTools.Current = EditorTool.Select);
        EditorShell.OnClick("tool-move", static () => EditorTools.Current = EditorTool.Move);
        EditorShell.OnClick("tool-rotate", static () => EditorTools.Current = EditorTool.Rotate);
        EditorShell.OnClick("tool-scale", static () => EditorTools.Current = EditorTool.Scale);
    }

    /// <summary>Draws whatever has changed since the last frame.</summary>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!EditorShell.IsOpen) return;

        Tools();
        Hierarchy(ctx);
        InspectorPage.Draw(ctx);
        Status(ctx);
    }

    /// <summary>Which tool is on, as a state on the buttons.</summary>
    /// <remarks>
    /// An attribute rather than a class list, because the stylesheet reads it with an attribute
    /// selector and nothing here has to know what being on looks like.
    /// </remarks>
    private static void Tools()
    {
        foreach (var (id, tool) in new[]
        {
            ("tool-select", EditorTool.Select),
            ("tool-move", EditorTool.Move),
            ("tool-rotate", EditorTool.Rotate),
            ("tool-scale", EditorTool.Scale),
        })
        {
            var element = EditorShell.Part(id);
            if (element.Exists) Dom.SetAttribute(element, "data-on", EditorTools.Current == tool ? "1" : "0");
        }
    }

    /// <summary>The world, as a tree of what is in it.</summary>
    /// <remarks>
    /// Walked again only when the population changed or enough frames have gone by that a rename
    /// would otherwise never show. Walking it every frame would be a query, a sort and a parse for
    /// a list that is the same list.
    /// </remarks>
    private static void Hierarchy(BehaviorContext ctx)
    {
        var all = ctx.Ecs.All();

        if (Rows.Count == 0 || all.Length != _population || EditorShell.Frame - _built > 30)
        {
            _built = EditorShell.Frame;
            _population = all.Length;

            Walk(ctx, all);
        }

        var markup = new StringBuilder();
        var wanted = _search.Trim();

        foreach (var row in Rows)
        {
            if (wanted.Length > 0
                && !row.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var picked = EditorSelection.All.Contains(row.Entity) ? "1" : "0";

            // What the thing is, from the components it carries. A picture is read from the assets
            // the page was opened with, which is the only place it may read from.
            markup.Append(
                $"<div class=\"tree-item\" id=\"entity-{row.Entity.Bits}\""
                + $" data-selected=\"{picked}\" style=\"padding-left:{6 + (row.Depth * 12)}px\">"
                + $"<img class=\"icon\" src=\"{EditorShell.Escape(row.Icon)}\" />"
                + EditorShell.Escape(row.Name)
                + "</div>");
        }

        if (markup.Length == 0)
        {
            markup.Append("<div class=\"empty\">")
                .Append(wanted.Length > 0 ? "Nothing matches" : "Nothing in the world")
                .Append("</div>");
        }

        if (Fill("hierarchy", markup.ToString()))
        {
            // Said again after the markup, because the elements a click lands on are the ones just
            // written and the ones before them are gone.
            foreach (var row in Rows)
            {
                var picked = row.Entity;
                EditorShell.OnClick($"entity-{picked.Bits}", () => EditorSelection.Select(picked));
            }
        }
    }

    /// <summary>Walks the world into a flat list of rows carrying their depth.</summary>
    /// <remarks>
    /// Flattened rather than nested, because a row's depth is all a list needs to look like a tree
    /// and a flat list is what a filter can be applied to without losing anything.
    /// </remarks>
    private static void Walk(BehaviorContext ctx, Entity[] all)
    {
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
            Rows.Add(new TreeRow(
                entity,
                names.GetValueOrDefault(entity.Bits, $"Entity {entity.Index}"),
                EditorKinds.IconFor(ctx.Ecs, entity),
                depth));

            if (!children.TryGetValue(entity.Bits, out var mine)) return;

            foreach (var child in mine) Add(child, depth + 1);
        }
    }

    /// <summary>The strip along the bottom.</summary>
    private static void Status(BehaviorContext ctx)
    {
        var selection = EditorSelection.Any
            ? $"{EditorSelection.Count} selected"
            : "No selection";

        Text("status-selection", selection);
        Text("status-frame", $"frame {ctx.Time.FrameCount}");
    }

    /// <summary>Writes markup into a part of the page, if it is not already what is there.</summary>
    private static bool Fill(string id, string markup)
    {
        if (Written.TryGetValue(id, out var before) && before == markup) return false;

        Written[id] = markup;
        EditorShell.Fill(id, markup);
        return true;
    }

    /// <summary>Writes text into a part of the page, if it has changed.</summary>
    private static void Text(string id, string text)
    {
        if (Written.TryGetValue(id, out var before) && before == text) return;

        Written[id] = text;

        var element = EditorShell.Part(id);
        if (element.Exists) Dom.SetText(element, text);
    }
}

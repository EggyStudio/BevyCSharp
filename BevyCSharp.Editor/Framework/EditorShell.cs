using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The editor's page, and the frame it runs in.
/// </summary>
/// <remarks>
/// <para>
/// One document holds the whole editor. A panel is a piece of markup inside it, so there is no
/// list of open documents to order, no placement to compute and nothing to rebuild when something
/// opens: showing a panel writes markup into an element, and hiding one writes nothing there.
/// </para>
/// <para>
/// What that leaves this class doing is small on purpose: load the page, hand out the parts of it
/// other code fills, say where the viewport ended up, and pass on what the page reported.
/// </para>
/// </remarks>
public static class EditorShell
{
    private static readonly DomEvent[] Reports = new DomEvent[64];
    private static readonly Dictionary<string, Action> Clicks = [];

    private static string _markup = string.Empty;
    private static string _theme = string.Empty;

    /// <summary>The frame the editor is on.</summary>
    public static ulong Frame { get; private set; }

    /// <summary>What is going on this frame, for anything that needs the engine mid-callback.</summary>
    public static BehaviorContext? Context { get; private set; }

    /// <summary>The world, as of this frame.</summary>
    /// <remarks>
    /// For the things a person asks for rather than the things a system does: a console command, a
    /// menu row. Both happen inside the frame, so the world they mean is this one.
    /// </remarks>
    public static EcsWorld Ecs =>
        Context?.Ecs ?? throw new InvalidOperationException("the editor has not started a frame");

    /// <summary>Where the scene shows through, in physical pixels.</summary>
    /// <remarks>
    /// Read off the page rather than decided here. The viewport is a box in a grid with no
    /// background, so the layout says where the scene appears and the camera follows it.
    /// </remarks>
    public static Rect Viewport { get; private set; }

    /// <summary>Whether the page is up.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>Opens the page from the editor's assets.</summary>
    /// <remarks>
    /// The stylesheet is spliced into the markup rather than linked, because a link would be a
    /// fetch and the page has no network. It stays a file on disk either way, which is what lets
    /// it be edited while the editor runs.
    /// </remarks>
    public static void Load(string assets)
    {
        ArgumentException.ThrowIfNullOrEmpty(assets);

        _markup = File.ReadAllText(Path.Combine(assets, "ui", "editor.html"));
        _theme = File.ReadAllText(Path.Combine(assets, "ui", "editor.css"));

        // The assets are where the page's pictures come from, and the only place it may read from.
        Dom.Open(Themed(), assets);
        IsOpen = true;
    }

    /// <summary>Reads the page and the stylesheet again, keeping what is on screen.</summary>
    /// <remarks>
    /// Hot reload. The whole page is opened again rather than patched, which loses nothing,
    /// because every panel is written into it from C# and is written again on the next frame.
    /// </remarks>
    public static void Reload(string assets)
    {
        Load(assets);
    }

    private static string Themed() =>
        _markup.Replace("<style id=\"theme\"></style>", $"<style id=\"theme\">{_theme}</style>");

    /// <summary>The element carrying an id.</summary>
    public static Element Part(string id) => Dom.Element(id);

    /// <summary>Puts markup into a part of the page, which is what showing a panel is.</summary>
    public static void Fill(string id, string html)
    {
        var host = Dom.Element(id);
        if (host.Exists) Dom.SetHtml(host, html);
    }

    /// <summary>Says what to do when an element is clicked.</summary>
    /// <remarks>
    /// By id rather than by handle, because markup written into the page replaces the elements in
    /// it and a handle taken before that names something that is gone. An id survives being
    /// written again, which is the point of having one.
    /// </remarks>
    public static void OnClick(string id, Action action)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(action);

        Clicks[id] = action;
    }

    /// <summary>Whether the pointer is over the interface rather than over the scene.</summary>
    /// <remarks>
    /// Asked of the page: whatever is under the pointer either is the viewport, which is a hole,
    /// or is part of the interface. Nothing here keeps a list of rectangles to test against.
    /// </remarks>
    public static bool PointerOverPanel(float x, float y)
    {
        if (!IsOpen) return false;

        var over = Dom.Hit(x, y);
        if (!over.Exists) return false;

        var viewport = Dom.Element("viewport");
        return over.Value != viewport.Value;
    }

    /// <summary>Shows a menu at a point, and calls back with what was picked.</summary>
    /// <remarks>
    /// A menu is markup put into the overlay element and taken out again. It sits over everything
    /// because it is last in the page, which is a rule of the document rather than a layer number
    /// somebody had to choose.
    /// </remarks>
    public static void ShowMenu(
        string title, IReadOnlyList<MenuItem> items, float x, float y, bool beside = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        _ = title;
        _ = beside;

        var markup = new System.Text.StringBuilder();
        markup.Append($"<div class=\"menu\" id=\"menu\" style=\"left:{x:0}px;top:{y:0}px\">");

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];

            if (item.Kind == MenuKind.Separator)
            {
                markup.Append("<div class=\"rule\"></div>");
                continue;
            }

            var ticked = item.Checked?.Invoke() == true ? " data-on=\"1\"" : string.Empty;
            var deeper = item.Kind == MenuKind.Submenu ? "<span class=\"more\">&gt;</span>" : string.Empty;

            markup.Append($"<div class=\"item\" id=\"menu-{index}\"{ticked}>")
                .Append(Escape(item.Label))
                .Append(deeper)
                .Append("</div>");
        }

        markup.Append("</div>");

        Fill("overlays", markup.ToString());

        _menuItems = items;
        _menuAt = (x, y);
    }

    /// <summary>Shows the menu a whole branch of the editor's menu holds.</summary>
    public static void ShowMenu(string branch, float x, float y) =>
        ShowMenu(branch, EditorMenu.Level(branch), x, y);

    /// <summary>Shows a menu branch, or puts it away if that branch is what is up.</summary>
    public static void ToggleMenu(string branch, float x, float y)
    {
        // Already put away by the press this click came from, which is what closing it means.
        if (_dismissed == Frame) return;

        if (Dismiss()) return;

        ShowMenu(branch, x, y);
    }

    /// <summary>What the open menu offers, so a pick can be run.</summary>
    private static IReadOnlyList<MenuItem> _menuItems = [];

    /// <summary>Where it was put, so a level below it opens beside rather than at the pointer.</summary>
    private static (float X, float Y) _menuAt;

    /// <summary>Called with what a menu pick was, if anything is listening.</summary>
    public static Action<MenuItem>? MenuPicked { get; set; }

    /// <summary>Called when Enter is pressed in a field, with the field's id.</summary>
    public static Action<string>? Accepted { get; set; }

    /// <summary>Called when the scene itself is right clicked.</summary>
    public static Action<float, float>? ViewportMenu { get; set; }

    /// <summary>Takes whatever overlay is up back off the page.</summary>
    public static bool Dismiss()
    {
        if (!IsOpen || !Dom.Select("#overlays > *").Exists) return false;

        Fill("overlays", string.Empty);

        _menuItems = [];
        _dismissed = Frame;
        return true;
    }

    /// <summary>The frame an overlay was last put away on.</summary>
    /// <remarks>
    /// A click anywhere else puts a menu away, and the button that opened it is somewhere else.
    /// Without remembering this, that button would put its menu away and then, finding it away,
    /// open it again, so the button that opened it could never close it.
    /// </remarks>
    private static ulong _dismissed;

    /// <summary>Does the page's frame: what happened, and where the viewport ended up.</summary>
    public static void Tick(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        Context = ctx;
        Frame = ctx.Time.FrameCount;

        if (!IsOpen) return;

        if (Dom.TryRect(Dom.Element("viewport"), out var viewport)) Viewport = viewport;

        var count = Dom.Drain(Reports);

        for (var index = 0; index < count; index++)
        {
            var report = Reports[index];

            switch (report.Kind)
            {
                case DomEventKind.Click:
                    Clicked(report.Target);
                    break;

                case DomEventKind.Accepted:
                    Accepted?.Invoke(Dom.ClosestId(report.Target));
                    break;
            }
        }
    }

    private static void Clicked(Element target)
    {
        // From the innermost thing under the pointer up to the nearest thing with a name, because
        // a click on a button with a picture in it lands on the picture.
        var id = Dom.ClosestId(target);

        // Anything that is not the menu puts the menu away. That is what makes it a flyout, and
        // with one document it costs nothing: no rebuild, and nothing else on screen moves.
        if (!id.StartsWith("menu-", StringComparison.Ordinal)) Dismiss();

        if (id.Length == 0) return;

        if (id.StartsWith("menu-", StringComparison.Ordinal)
            && int.TryParse(id.AsSpan(5), out var index)
            && index >= 0 && index < _menuItems.Count)
        {
            var picked = _menuItems[index];
            var at = _menuAt;

            Dismiss();

            // A branch opens the level below it, offset so that the one it came from is still
            // readable behind it.
            if (picked.Kind == MenuKind.Submenu)
            {
                ShowMenu(picked.Path, at.X + 24f, at.Y + 12f);
                return;
            }

            // What a menu item does is the item's own, and the world it does it to is the frame's.
            if (Context is { } ctx) picked.Run?.Invoke(ctx.Ecs);

            MenuPicked?.Invoke(picked);
            return;
        }

        if (Context is { } frame && InspectorPage.Clicked(frame, id)) return;

        if (Clicks.TryGetValue(id, out var action)) action();
    }

    /// <summary>Makes text safe to put in markup.</summary>
    public static string Escape(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}

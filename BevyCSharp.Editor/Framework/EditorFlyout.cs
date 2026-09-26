using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The editor's menu: which branch is up, where it was asked for, and the rows in it.
/// </summary>
/// <remarks>
/// A popup belongs to the frame it is opened in, so what somebody asks for is remembered here and
/// put up on the next pass through the interface.
/// </remarks>
public static class EditorFlyout
{
    /// <summary>Which branch of the menu is up, if any.</summary>
    private static string? _menu;

    /// <summary>Whether it still has to be handed to ImGui, which happens once per opening.</summary>
    internal static bool _opening;

    /// <summary>Whether a menu is up, for anything asking outside the interface's own frame.</summary>
    /// <remarks>
    /// Asked rather than looked up: ImGui answers questions about its windows only between the
    /// beginning and the end of a frame, and a system that runs before the interface does would be
    /// asking a context that is not in one.
    /// </remarks>
    public static bool MenuOpen => _menu is not null;

    /// <summary>Shows a branch of the editor's menu at a point.</summary>
    /// <remarks>
    /// A menu is a popup, and a popup belongs to the frame it is opened in, so what is asked for
    /// here is remembered and put up on the next pass through the interface.
    /// </remarks>
    public static void ShowMenu(string branch, float x, float y)
    {
        _menu = branch ?? string.Empty;
        _opening = true;
        MenuAt = new Vector2(x, y);
    }

    /// <summary>Shows a branch, or puts it away if it is the one that is up.</summary>
    public static void ToggleMenu(string branch, float x, float y)
    {
        if (_menu is not null)
        {
            _menu = null;
            return;
        }

        ShowMenu(branch, x, y);
    }

    /// <summary>Where the menu was asked for.</summary>
    private static Vector2 MenuAt { get; set; }

    /// <summary>
    /// Whatever branch of the menu was asked for, where it was asked for.
    /// </summary>
    /// <remarks>
    /// Opened once, when it is asked for. Opening it whenever it is not open is how a menu becomes
    /// one that cannot be dismissed, because the click that closes it is followed by a frame that
    /// finds it closed and opens it again.
    /// </remarks>
    internal static void Draw(BehaviorContext ctx)
    {
        if (_menu is null) return;

        const string Name = "##menu";

        if (_opening)
        {
            _opening = false;

            ImGui.SetNextWindowPos(MenuAt);
            ImGui.OpenPopup(Name);
        }

        if (!EditorWidgets.Flyout(Name))
        {
            // Dismissed by a click somewhere else, as a menu is.
            _menu = null;
            return;
        }

        Branch(ctx, _menu);

        EditorWidgets.EndFlyout();
    }

    /// <summary>
    /// A label with room at its left for the row's picture.
    /// </summary>
    /// <remarks>
    /// Spaces rather than a widget, because what draws the row is ImGui's own menu item and it
    /// draws its label where it likes. Every row is padded whether or not it has a picture, so the
    /// labels line up down the menu instead of stepping in and out with whatever has one.
    /// </remarks>
    /// <param name="label">What the row says.</param>
    private static string Padded(string label)
    {
        var room = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.X;
        var space = MathF.Max(1f, ImGui.CalcTextSize(" ").X);

        return new string(' ', (int)MathF.Ceiling(room / space)) + label;
    }

    /// <summary>
    /// The picture for the row just drawn, in the room its label left at the front.
    /// </summary>
    /// <remarks>
    /// Drawn over the row rather than laid out before it, so the fill behind a row under the
    /// pointer still runs the whole width of the menu.
    /// </remarks>
    /// <param name="item">The row that was drawn.</param>
    private static void Picture(MenuItem item)
    {
        if (item.Icon is not { Length: > 0 } icon) return;

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var line = ImGui.GetTextLineHeight();

        EditorDraw.Icon(
            ImGui.GetWindowDrawList(),
            icon,
            new Vector2(min.X, ((min.Y + max.Y) * 0.5f) - (line * 0.5f)),
            line,
            false);
    }

    /// <summary>One level of the menu, with a submenu per branch under it.</summary>
    internal static void Branch(BehaviorContext ctx, string path) => RoundedRows.Rows(() =>
    {
        foreach (var item in EditorMenu.Level(path))
        {
            switch (item.Kind)
            {
                case MenuKind.Separator:
                    EditorTheme.Divide();
                    continue;

                case MenuKind.Submenu:
                    var opened = ImGui.BeginMenu(Padded(item.Label));

                    Picture(item);
                    RoundedRows.Row(opened);

                    if (opened)
                    {
                        Branch(ctx, item.Path);
                        ImGui.EndMenu();
                    }

                    break;

                case MenuKind.Toggle:
                    var ticked = item.Checked?.Invoke() == true;

                    if (ImGui.MenuItem(Padded(item.Label), item.Keys ?? string.Empty, ticked))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    Picture(item);
                    RoundedRows.Row();

                    break;

                default:
                    if (ImGui.MenuItem(
                            Padded(item.Label),
                            item.Keys ?? string.Empty,
                            false,
                            item.Enabled?.Invoke() != false))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    Picture(item);
                    RoundedRows.Row();

                    break;
            }
        }
    });


}

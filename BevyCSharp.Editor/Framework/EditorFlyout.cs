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
    /// one that cannot be dismissed: the click that closes it is followed by a frame that finds it
    /// closed and opens it again.
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

        if (!ImGui.BeginPopup(Name))
        {
            // Dismissed by a click somewhere else, which is what a menu is for.
            _menu = null;
            return;
        }

        Branch(ctx, _menu);

        ImGui.EndPopup();
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
                    var opened = ImGui.BeginMenu(item.Label);

                    RoundedRows.Row(opened);

                    if (opened)
                    {
                        Branch(ctx, item.Path);
                        ImGui.EndMenu();
                    }

                    break;

                case MenuKind.Toggle:
                    var ticked = item.Checked?.Invoke() == true;

                    if (ImGui.MenuItem(item.Label, string.Empty, ticked)) item.Run?.Invoke(ctx.Ecs);

                    RoundedRows.Row();

                    break;

                default:
                    if (ImGui.MenuItem(item.Label, string.Empty, false, item.Enabled?.Invoke() != false))
                    {
                        item.Run?.Invoke(ctx.Ecs);
                    }

                    RoundedRows.Row();

                    break;
            }
        }
    });


}

// Bevy's directional_navigation_overrides example,
// examples/ui/navigation/directional_navigation_overrides.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates automatic directional navigation with manual navigation overrides. Three pages of
// buttons, each reached from its neighbors by where it is on the screen, and edges drawn by hand
// where that is not the way meant: rows that wrap to the next, a far button joined to the others,
// a grid whose up and down are blocked and one whose up and down are turned over, and the way from
// each page to the next. Moving the focus onto another page shows that page and hides the last.
internal static class DirectionalNavigationOverrides
{
    // A grid page's buttons, four rows of three.
    private static readonly (float Left, float Top)[] GridPositions =
    [
        (450f, 80f), (650f, 80f), (850f, 80f),
        (450f, 215f), (650f, 215f), (850f, 215f),
        (450f, 350f), (650f, 350f), (850f, 350f),
        (450f, 485f), (650f, 485f), (850f, 485f),
    ];

    // The second page's buttons, top left, top right, middle and the far one at the bottom right.
    private static readonly (float Left, float Top)[] TrianglePositions = [(450f, 80f), (700f, 80f), (575f, 215f), (1050f, 350f)];

    public static void Build(App app)
    {
        app.Startup(SetupPagedUi, "directional_navigation_overrides.SetupPagedUi");
        NavigationInput.AddSystems(app, "directional_navigation_overrides", ShowPageOf);
    }

    // Bevy's setup_paged_ui.
    private static void SetupPagedUi(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        NavigationInput.Setup(ecs, minAlignment: 0.1f, maxDistance: 200f);
        Render2d.SpawnCamera2d();

        Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
        NavigationInput.SpawnPanels(ecs, """
            Directional Navigation Overrides Demo

            Use arrow keys or D-pad to navigate.
            Press Enter or A button to interact.

            Navigation on each page is a combination of both automatic and manual navigation.
            """);

        // Each page a node filling the window, the buttons and texts its children, the first alone shown.
        var pages = new List<Entity>[3];
        for (var page = 0; page < 3; page++)
        {
            var node = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
            ecs.Add(node, page == 0 ? Visibility.Visible : Visibility.Hidden);

            var (buttons, texts) = page == 1 ? TrianglePage(ecs, page) : GridPage(ecs, page);
            foreach (var child in buttons.Concat(texts)) ecs.SetParent(child, node);
            pages[page] = buttons;
        }

        // On the grid pages, the end of each row leads on to the start of the next, and back.
        foreach (var grid in new[] { pages[0], pages[2] })
        {
            for (var row = 0; row < 3; row++) Navigation.AddEdge(grid[row * 3 + 2], grid[(row + 1) * 3], CompassOctant.East, bothWays: true);
        }

        // The search does not join the middle button of the second page to the far one with the
        // settings above, so the edges are drawn by hand, east, south and south-east.
        foreach (var way in new[] { CompassOctant.East, CompassOctant.South, CompassOctant.SouthEast })
            Navigation.AddEdge(pages[1][2], pages[1][3], way, bothWays: true);

        // The first page moves along its rows alone, up and down blocked from every button.
        foreach (var button in pages[0])
        {
            Navigation.BlockEdge(button, CompassOctant.South);
            Navigation.BlockEdge(button, CompassOctant.North);
        }

        // The third page's columns turned over, north going down each and round from the bottom.
        for (var column = 0; column < 3; column++)
            Navigation.AddEdges([.. Enumerable.Range(0, 4).Select(row => pages[2][row * 3 + column])], CompassOctant.North, looping: true);

        // Between the pages. East from the first page's last button to the second's first and back,
        // south from the second's last to the third's first and west back again, each one way, and
        // east from the third's last round to the first page's first and back.
        Navigation.AddEdge(pages[0][11], pages[1][0], CompassOctant.East, bothWays: true);
        Navigation.AddEdge(pages[1][3], pages[2][0], CompassOctant.South);
        Navigation.AddEdge(pages[2][0], pages[1][3], CompassOctant.West);
        Navigation.AddEdge(pages[2][11], pages[0][0], CompassOctant.East, bothWays: true);

        Ui.Focus(pages[0][0]);
    }

    // Bevy's setup_buttons_for_grid_page, its buttons and the texts saying where each edge leads.
    private static (List<Entity> Buttons, List<Entity> Texts) GridPage(EcsWorld ecs, int page)
    {
        var buttons = GridPositions.Select((at, i) =>
            NavigationInput.SpawnButton(ecs, $"Btn {i / 3 + 1}-{i % 3 + 1}", at.Left, at.Top, 140f, 100f, page)).ToList();

        var previous = page == 0 ? 3 : page;
        List<Entity> texts =
        [
            SmallText($"Currently on Page {page + 1}", 650, 20, TextJustify.Center),
            SmallText($"Page {previous} << ", 310, 120, TextJustify.Right),
            SmallText($">> Page {(page + 1) % 3 + 1}", 1000, 525, TextJustify.Left),
            SmallText("> Btn 2-1", 1000, 120, TextJustify.Left),
            SmallText("> Btn 3-1", 1000, 255, TextJustify.Left),
            SmallText("> Btn 4-1", 1000, 390, TextJustify.Left),
            SmallText("Btn 1-3 < ", 310, 255, TextJustify.Right),
            SmallText("Btn 2-3 < ", 310, 390, TextJustify.Right),
            SmallText("Btn 3-3 < ", 310, 525, TextJustify.Right),
        ];

        var footer = page switch
        {
            0 => "Vertical movements disabled on each button, but you can still navigate between rows by going off the left or right sides.",
            2 => "Vertical Navigation has been manually overridden to be inverted! ^ moves down, and v (down) moves up.",
            _ => "",
        };
        texts.Add(Ui.SpawnText(footer, new UiSettings { Absolute = true, Left = Length.Px(450f), Top = Length.Px(600f), Width = Length.Px(540f), Padding = Length.Px(12f) }));
        return (buttons, texts);
    }

    // Bevy's setup_buttons_for_triangle_page.
    private static (List<Entity> Buttons, List<Entity> Texts) TrianglePage(EcsWorld ecs, int page)
    {
        var buttons = TrianglePositions.Select((at, i) =>
            NavigationInput.SpawnButton(ecs, $"Btn {i + 1}", at.Left, at.Top, 140f, 100f, page)).ToList();

        List<Entity> texts =
        [
            SmallText($"Currently on Page {page + 1}", 650, 20, TextJustify.Center),
            SmallText($"Page {page} << ", 310, 120, TextJustify.Right),
            SmallText("v\nBtn 4", 575, 325, TextJustify.Center),
            SmallText("> Btn 4", 735, 255, TextJustify.Left),
            SmallText("Btn 3\n^", 1050, 300, TextJustify.Center),
            SmallText("Btn 3 < ", 910, 390, TextJustify.Right),
            SmallText($"V\nV\nPage {(page + 1) % 3 + 1}", 1050, 460, TextJustify.Center),
        ];
        return (buttons, texts);
    }

    // Bevy's spawn_small_text_node.
    private static Entity SmallText(string text, float left, float top, TextJustify justify) =>
        Ui.SpawnText(text,
            new UiSettings { Absolute = true, Left = Length.Px(left), Top = Length.Px(top), Width = Length.Px(140f), Padding = Length.Px(12f) },
            new UiTextSettings { FontSize = 20f, Justify = justify });

    // The part of Bevy's navigate that follows the focus, the page of the button it moved to shown
    // and the page it left hidden where the two differ.
    private static void ShowPageOf(EcsWorld ecs, Entity? previous, Entity next)
    {
        var page = ecs.ParentOf(next);
        if (page.IsNone) return;
        ecs.Set(page, Visibility.Visible);

        if (previous is { } left && ecs.ParentOf(left) is var leftPage && !leftPage.IsNone && leftPage != page)
            ecs.Set(leftPage, Visibility.Hidden);
    }
}

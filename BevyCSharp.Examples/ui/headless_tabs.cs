// Bevy's headless_tabs example, examples/ui/widgets/headless_tabs.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates the behavior-only tab widgets in bevy_ui_widgets, four tab strips, three that keep
// their own selection and one whose selection an observer keeps in state of the game's own, each
// tab colored as it is selected, hovered or disabled and outlined while it holds the focus Tab
// moves.
//
// Bevy marks the selected tab with a Selected component, which it does not reflect. Here a tab is
// selected where its list's SelectedTab names it, the list being the tab's parent.
internal static class HeadlessTabs
{
    private static readonly Color TextLabel = Color.FromSrgb(0.72f, 0.76f, 0.84f);
    private static readonly Color TabText = Color.FromSrgb(0.88f, 0.89f, 0.92f);
    private static readonly Color Strip = Color.FromSrgb(0.10f, 0.11f, 0.14f);
    internal static readonly Color Idle = Color.FromSrgb(0.13f, 0.14f, 0.18f);
    internal static readonly Color Edge = Color.FromSrgb(0.24f, 0.25f, 0.30f);

    // Bevy's ControlledSelection resource, the controlled strip's selection, kept by the game
    // rather than the list.
    private static Entity? _controlledSelection;

    public static void Build(App app) => app.Startup(Setup, "headless_tabs.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _controlledSelection = null;
        Render2d.SpawnCamera2d();

        var page = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Padding = Sides.All(Length.Px(24f)),
            Direction = UiDirection.Column,
            Align = UiAlign.Start,
            RowGap = Length.Px(18f),
            OverflowY = UiOverflow.Scroll,
            Color = Color.FromSrgb(0.06f, 0.07f, 0.09f),
        });
        ecs.Insert<TabGroupRef>(page);

        Section(ecs, page, "Horizontal automatic - self-updating");
        var (automatic, automaticTabs) = TabStrip(ecs, page, horizontal: true, TabListRef.ActivationVariant.Automatic, ["General", "Rendering", "Disabled"]);
        ecs.Insert<InteractionDisabledRef>(automaticTabs[2]);
        Ui.SelfUpdate(automatic, UiWidgetKind.TabList);

        Section(ecs, page, "Horizontal manual - focus and selection are separate");
        Ui.SelfUpdate(TabStrip(ecs, page, horizontal: true, TabListRef.ActivationVariant.Manual, ["Scene", "Assets", "Inspector"]).Strip, UiWidgetKind.TabList);

        Section(ecs, page, "Vertical manual");
        Ui.SelfUpdate(TabStrip(ecs, page, horizontal: false, TabListRef.ActivationVariant.Manual, ["Transform", "Visibility", "Metadata"]).Strip, UiWidgetKind.TabList);

        // The game keeps the selection and writes it back, where it could as well refuse it.
        Section(ecs, page, "Controlled - observer updates external state");
        var (controlled, _) = TabStrip(ecs, page, horizontal: true, TabListRef.ActivationVariant.Manual, ["External A", "External B"]);
        ecs.Observe<ValueChange<Entity?>>(controlled, on =>
        {
            _controlledSelection = on.Event.Value;
            on.Ecs.Wrap<SelectedTabRef>(on.Event.Source).Value = _controlledSelection;
        });
    }

    private static void Section(EcsWorld ecs, Entity page, string label) =>
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextLabel }, new UiTextSettings { FontSize = 17f }), page);

    // A strip of tabs with its first selected, which is Bevy's @selected_tab of the first.
    private static (Entity Strip, Entity[] Tabs) TabStrip(EcsWorld ecs, Entity page, bool horizontal, TabListRef.ActivationVariant activation, string[] labels)
    {
        var strip = Ui.SpawnNode(new UiSettings
        {
            Direction = horizontal ? UiDirection.Row : UiDirection.Column,
            Align = UiAlign.Stretch,
            Color = Strip,
        });
        var list = ecs.Insert<TabListRef>(strip);
        list.Orientation = horizontal ? TabListRef.OrientationVariant.Horizontal : TabListRef.OrientationVariant.Vertical;
        list.Activation = activation;
        ecs.SetParent(strip, page);

        var tabs = new Entity[labels.Length];
        for (var i = 0; i < labels.Length; i++)
        {
            var tab = Ui.SpawnNode(new UiSettings
            {
                MinWidth = Length.Px(112f),
                MinHeight = Length.Px(36f),
                Padding = new Sides(Length.Px(12f), Length.Px(8f), Length.Px(12f), Length.Px(8f)),
                Border = Sides.All(Length.Px(1f)),
                Align = UiAlign.Center,
                Justify = UiJustify.Center,
                Color = Idle,
                BorderColor = Edge,
            });
            ecs.Add(tab, new ShowcaseTab());
            ecs.Insert<TabRef>(tab);
            ecs.Insert<HoveredRef>(tab);
            ecs.SetParent(Ui.SpawnText(labels[i], new UiSettings { Color = TabText }, new UiTextSettings { FontSize = 16f }), tab);
            ecs.SetParent(tab, strip);
            tabs[i] = tab;
        }

        ecs.Insert<SelectedTabRef>(strip).Value = tabs[0];
        return (strip, tabs);
    }
}

/// <summary>A tab of the showcase's strips, Bevy's example's <c>ShowcaseTab</c>.</summary>
[Behavior]
public partial struct ShowcaseTab
{
    /// <summary>
    /// Bevy's update_tab_styles, the tab colored as it is disabled, selected or hovered, and
    /// outlined while it holds the focus Tab moves.
    /// </summary>
    [OnUpdate]
    public void UpdateTabStyles(BehaviorContext ctx)
    {
        var (ecs, tab) = (ctx.Ecs, ctx.Entity);
        var focus = ecs.Resource<InputFocusRef>()?.CurrentFocus;
        var focusVisible = ecs.Resource<InputFocusVisibleRef>()?.Value == true;

        var disabled = ecs.Get<InteractionDisabledRef>(tab) is not null;
        var selected = ecs.Get<SelectedTabRef>(ecs.ParentOf(tab))?.Value == tab;
        var hovered = ecs.Get<HoveredRef>(tab)?.Value == true;

        ecs.Wrap<BackgroundColorRef>(tab).Value = (disabled, selected, hovered) switch
        {
            (true, _, _) => Color.FromSrgb(0.10f, 0.10f, 0.11f),
            (false, true, _) => Color.FromSrgb(0.18f, 0.34f, 0.52f),
            (false, false, true) => Color.FromSrgb(0.20f, 0.21f, 0.26f),
            _ => HeadlessTabs.Idle,
        };

        var edge = ecs.Wrap<BorderColorRef>(tab);
        edge.Top = edge.Right = edge.Bottom = edge.Left = focusVisible && focus == tab ? Color.FromSrgb(0.45f, 0.78f, 1f) : HeadlessTabs.Edge;
    }
}

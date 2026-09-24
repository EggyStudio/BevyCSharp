using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What is selected, and everything known about it.
/// </summary>
/// <remarks>
/// <para>
/// A component's fields come from the schema the generator emitted, so nothing here names a type
/// and nothing reflects at runtime. What a field is drawn as follows from its kind and from the
/// hints its attributes declared.
/// </para>
/// <para>
/// Bevy's own components are not shown as rows, because an entity carries a dozen of them and not
/// one is something a person edits. The ones worth knowing about are named as tags under the
/// entity, which is also where a component of this project's own with no fields at all ends up.
/// </para>
/// </remarks>
public static class DetailsPanel
{
    /// <summary>Draws the selection.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        // How many were picked, when it is more than one. The rest of the panel is about the last
        // of them, and without this a drag that took a dozen things looks like a click that took
        // one, since the other eleven are only visible in the list, which may not be on screen.
        EditorSurface.Title(
            "DETAILS",
            EditorSelection.Count > 1 ? $"({EditorSelection.Count} selected)" : null);

        ImGui.Spacing();

        if (!EditorSelection.Any)
        {
            ImGui.TextDisabled("Nothing selected");
            return;
        }

        var entity = EditorSelection.Current;
        var name = ctx.Ecs.NameOf(entity) ?? $"Entity {entity.Index}";

        EditorSurface.FullWidth();

        var renamed = name;

        if (ImGui.InputText("##name", ref renamed, 128, ImGuiInputTextFlags.EnterReturnsTrue)
            && renamed.Length > 0)
        {
            EditorEntity.Rename(ctx.Ecs, entity, renamed);
        }

        ImGui.Spacing();

        // One scrolling region and nothing nested inside it.
        //
        // A child window whose height is worked out from its contents, inside a region that
        // scrolls, makes the region jump while it is being scrolled, because the height it
        // reports depends on what is visible, and what is visible depends on the height. A card is
        // a rectangle drawn behind a group instead, so this is one region with one scroll and no
        // argument.
        // The room the button at the bottom keeps for itself, taken out of the scrolling region
        // rather than scrolled with it, because adding a component is not something to go looking
        // for at the end of a long list of what is already there.
        var button = ImGui.GetFrameHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2f);
        var room = ImGui.GetContentRegionAvail().Y - button;

        var open = EditorSurface.Region(
            "##components",
            new Vector2(0f, MathF.Max(1f, room)),
            ImGuiChildFlags.NavFlattened);

        if (!open)
        {
            EditorSurface.EndRegion();
            return;
        }

        // Back to the top whenever the selection changes. Left where it was, a panel opened on
        // something with a dozen components shows the middle of the last thing that was looked at.
        if (EditorSelection.ChangedOn == EditorShell.Frame) ImGui.SetScrollY(0f);

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (EditorEntity.IsDerived(ctx.Ecs, id)) continue;
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (schema.Fields.Count == 0) continue;
            if (Elsewhere.Contains(schema.Name)) continue;

            Component(ctx, entity, schema);
        }

        // What it is drawn with, which is the engine's own components rather than this project's
        // and so has no schema to be drawn from.
        EditorDrawn.Draw(ctx, entity);

        // What the thing is, under what can be edited about it. A tag says what something carries,
        // which is worth knowing and never worth the room at the top.
        Tags(ctx, entity);

        EditorSurface.EndRegion();

        Add(ctx, entity);
    }

    /// <summary>How much air a component's card keeps inside its own edge.</summary>
    /// <remarks>The air every card keeps, because a component's card is one.</remarks>
    internal const float Inset = EditorSurface.Air;

    /// <summary>
    /// What this panel leaves to something else to draw.
    /// </summary>
    /// <remarks>
    /// Visibility is one enum, and the world list draws it as an eye on the entity's own line,
    /// where things are shown and hidden while looking at the list of them. A card here as well
    /// would be a second place to set one value, and the list is the better of the two. It is
    /// still on the list of what can be added, so an entity that has no eye can be given one.
    /// </remarks>
    private static readonly HashSet<string> Elsewhere = ["Visibility"];

    /// <summary>One component, as a card with its fields in it.</summary>
    /// <remarks>
    /// The fill is drawn behind the group rather than around it, through a split in the draw list:
    /// the content goes on the upper channel, the rectangle on the lower one, and the merge puts
    /// the rectangle underneath. That is how a card is drawn without a window to hold it.
    /// </remarks>
    private static void Component(BehaviorContext ctx, Entity entity, ComponentSchema schema)
    {
        var theme = EditorTheme.Current;
        var draw = ImGui.GetWindowDrawList();

        draw.ChannelsSplit(2);
        draw.ChannelsSetCurrent(1);

        ImGui.BeginGroup();

        // The header wears no fill of its own, and neither does the card, because both are drawn
        // behind, so the card can be exactly the header grown downwards. Nothing is indented round
        // it, so a component that is closed is the header and nothing else, at the header's size.
        if (!theme.Stock)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, 0u);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0u);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0u);
        }

        var open = ImGui.CollapsingHeader(schema.Name, ImGuiTreeNodeFlags.DefaultOpen);

        if (!theme.Stock) ImGui.PopStyleColor(3);

        var head = ImGui.GetItemRectMin();
        var headTo = ImGui.GetItemRectMax();
        var over = ImGui.IsItemHovered();

        // A component's own menu, where taking it off lives. On the header, because that is the
        // thing the component is.
        if (EditorWidgets.FlyoutHere($"##menu{schema.Name}"))
        {
            RoundedRows.Rows(() =>
            {
                if (ImGui.MenuItem("Remove", string.Empty, false, schema.CanAdd))
                {
                    EditorEntity.Drop(ctx.Ecs, entity, schema);
                }

                RoundedRows.Row();
            });

            EditorWidgets.EndFlyout();
        }

        if (open)
        {
            ImGui.Indent(Inset);

            // In the order the fields asked for, which is declaration order for anything that
            // did not ask, and inside whatever fold each one named.
            var fold = new FoldStack(schema.Name);

            foreach (var field in Ordered(schema))
            {
                if (fold.Enter(field.Hints.Foldout, field.Hints.FoldoutOpen))
                {
                    ComponentFields.Row(ctx, entity, schema, field);
                }
            }

            fold.Leave();

            // Wrapped rather than run off the edge, because a row of buttons as wide as the
            // panel is a row whose last button cannot be pressed.
            var room = ImGui.GetContentRegionAvail().X - Inset;
            var used = 0f;

            foreach (var method in schema.Methods)
            {
                var width = EditorWidgets.PillWidth(method.Title);

                if (used > 0f && used + width < room) ImGui.SameLine();
                else used = 0f;

                used += width + ImGui.GetStyle().ItemSpacing.X;

                if (EditorWidgets.Pill(method.Title, false)) method.Run(ctx.Ecs, entity);
            }

            ImGui.Unindent(Inset);
            ImGui.Dummy(new Vector2(0f, Inset * 0.5f));
        }

        ImGui.EndGroup();

        if (!theme.Stock)
        {
            var to = ImGui.GetItemRectMax();

            // Rounded by half the header's height, so a component that is closed is a capsule and
            // one that is open is that capsule with its bottom pulled down. Anything larger is the
            // same shape, because that is as round as a rectangle this tall can be.
            var round = (headTo.Y - head.Y) * 0.5f;

            draw.ChannelsSetCurrent(0);

            // Cut to the region rather than run under its edge, so a card scrolled half out of
            // sight ends in a rounded corner instead of a square one.
            var top = head;
            var bottom = new Vector2(headTo.X, MathF.Max(headTo.Y, to.Y));

            if (EditorSurface.Clipped(ref top, ref bottom))
            {
                EditorDraw.Rounded(
                    top, bottom, round, ImGui.GetColorU32(EditorTheme.LiveGroup), draw);
            }

            // And the header on top of it when the pointer is there, which is the one thing that
            // says a header is something to press.
            var lit = head;
            var litTo = headTo;

            if (over && EditorSurface.Clipped(ref lit, ref litTo))
            {
                EditorDraw.Rounded(
                    lit, litTo, round, ImGui.GetColorU32(EditorTheme.LiveLift), draw);
            }
        }

        draw.ChannelsMerge();

        ImGui.Spacing();
    }

    /// <summary>
    /// Opens and closes the folds a run of fields names, one field at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A fold is written on each field rather than around a group of them, because an attribute
    /// can only be put on the thing it is about. Consecutive fields naming the same fold share it,
    /// which means the run of fields is what says where a fold begins and ends, and something has
    /// to keep that as it walks them.
    /// </para>
    /// <para>
    /// Whether a fold is open is remembered per component rather than per entity, because somebody
    /// who shut one meant it about the component. Nested folds are a path with slashes in it, so
    /// entering <c>Advanced/Debug</c> from <c>Advanced</c> opens one level and entering it from
    /// nothing opens two.
    /// </para>
    /// </remarks>
    /// <param name="component">Which component's folds these are.</param>
    private sealed class FoldStack(string component)
    {
        /// <summary>Which folds are open, by component and path, for as long as the editor runs.</summary>
        private static readonly Dictionary<string, bool> Shown = [];

        /// <summary>The levels currently entered, outermost first.</summary>
        private readonly List<string> _open = [];

        /// <summary>Whether every level entered so far is open, so the fields inside show.</summary>
        private bool _visible = true;

        /// <summary>
        /// Moves to the fold a field named, and says whether the field should be drawn.
        /// </summary>
        /// <param name="path">The fold, or null for none.</param>
        /// <param name="starts">Whether a fold seen for the first time starts open.</param>
        internal bool Enter(string? path, bool starts)
        {
            var wanted = path is { Length: > 0 } ? path.Split('/') : [];

            // Everything the two have in common stays as it is, which is what lets consecutive
            // fields share a fold rather than closing and reopening it each time.
            var shared = 0;

            while (shared < wanted.Length
                   && shared < _open.Count
                   && wanted[shared] == _open[shared])
            {
                shared++;
            }

            Close(shared);

            for (var level = shared; level < wanted.Length; level++)
            {
                _open.Add(wanted[level]);

                // Named by the whole path, so two folds called Debug under different parents are
                // two folds rather than one remembered in both places.
                var key = component + "/" + string.Join("/", _open);

                if (!Shown.TryGetValue(key, out var open))
                {
                    open = starts;
                    Shown[key] = open;
                }

                // A shut fold still has its levels pushed, so the next field's path is compared
                // against where it actually is rather than against where it would have been.
                if (!_visible) continue;

                ImGui.SetNextItemOpen(open, ImGuiCond.Always);

                if (ImGui.TreeNodeEx(
                        wanted[level],
                        ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen))
                {
                    _visible = true;
                }
                else
                {
                    _visible = false;
                }

                // ImGui reports the state it drew, and a click on the arrow is what changes it.
                if (ImGui.IsItemToggledOpen()) Shown[key] = !open;
            }

            return _visible;
        }

        /// <summary>Closes everything still open, at the end of a component.</summary>
        internal void Leave() => Close(0);

        /// <summary>Leaves every level past <paramref name="depth"/>.</summary>
        private void Close(int depth)
        {
            while (_open.Count > depth) _open.RemoveAt(_open.Count - 1);

            // Visibility is a property of what is left open, so it is worked out again rather than
            // remembered, and leaving every level puts it back to showing. The key is the whole
            // path, the same way it was when the level was entered.
            _visible = true;

            for (var level = 1; level <= _open.Count; level++)
            {
                var key = component + "/" + string.Join("/", _open.Take(level));

                if (Shown.TryGetValue(key, out var open) && !open)
                {
                    _visible = false;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// A component's fields, in the order they are drawn.
    /// </summary>
    /// <remarks>
    /// Sorted rather than reordered in place, and stable, so fields that asked for nothing stay in
    /// the order they were written in and the ones that asked move around them.
    /// </remarks>
    /// <param name="schema">The component whose fields to order.</param>
    private static IEnumerable<ComponentField> Ordered(ComponentSchema schema)
    {
        var ordered = new List<ComponentField>(schema.Fields);

        for (var i = 1; i < ordered.Count; i++)
        {
            var field = ordered[i];
            var j = i - 1;

            while (j >= 0 && ordered[j].Hints.Order > field.Hints.Order)
            {
                ordered[j + 1] = ordered[j];
                j--;
            }

            ordered[j + 1] = field;
        }

        return ordered;
    }

    /// <summary>
    /// What can be put on the entity that is not on it already.
    /// </summary>
    /// <remarks>
    /// Only what the generator emitted a way to add, which is every <c>[Behavior]</c> struct. The
    /// engine's own components need a byte-compatible mirror on this side and cannot be built from
    /// a name.
    /// </remarks>
    private static void Add(BehaviorContext ctx, Entity entity)
    {
        ImGui.Spacing();

        var room = ImGui.GetContentRegionAvail().X;

        if (ImGui.Button("Add Component", new Vector2(room, ImGui.GetFrameHeight())))
        {
            ImGui.OpenPopup("##add");
        }

        if (!EditorWidgets.Flyout("##add")) return;

        var carried = new HashSet<string>();

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is { } schema) carried.Add(schema.Name);
        }

        RoundedRows.Rows(() =>
        {
            var any = false;

            foreach (var schema in ComponentSchemas.All)
            {
                if (!schema.CanAdd || carried.Contains(schema.Name)) continue;

                if (ImGui.MenuItem(schema.Name)) EditorEntity.Carry(ctx.Ecs, entity, schema);

                RoundedRows.Row();
                any = true;
            }

            // Said rather than left blank, because an empty flyout reads as one that failed to
            // open. Everything this project generates a way to add is already on the entity.
            if (!any) ImGui.TextDisabled("Nothing left to add");
        });

        EditorWidgets.EndFlyout();
    }

    /// <summary>
    /// What the entity is, as tags.
    /// </summary>
    /// <remarks>
    /// A component with nothing to edit still says something about the thing carrying it, and a row
    /// with no value in it says it badly. A tag is the shape that fits: a word, wrapped into as many
    /// lines as it takes.
    /// </remarks>
    private static void Tags(BehaviorContext ctx, Entity entity)
    {
        var room = ImGui.GetContentRegionAvail().X;
        var used = 0f;
        var any = false;

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (EditorEntity.IsDerived(ctx.Ecs, id)) continue;

            var schema = ComponentSchemas.For(id);

            // Something of this project's own with no fields to edit. Not the engine's, because
            // a mesh, a material and a visibility are on everything that is drawn, so naming them
            // says nothing about the thing being looked at and crowds out what does.
            if (schema is null) continue;
            if (schema.Fields.Count > 0) continue;

            var label = schema.Name;
            if (label.Length == 0) continue;

            var width = EditorWidgets.PillWidth(label);

            if (used > 0f && used + width < room) ImGui.SameLine();
            else used = 0f;

            used += width + ImGui.GetStyle().ItemSpacing.X;

            if (!any)
            {
                any = true;

                EditorSurface.Heading("Also carries", Inset);
                ImGui.Spacing();

                // Said before the first one, so a thing carrying nothing extra says nothing at all
                // rather than showing a heading over an empty row.
                used = width + ImGui.GetStyle().ItemSpacing.X;
            }

            // Drawn rather than asked for, because a tag is a word on a pill and not something to
            // press. A button that answers a click by doing nothing is a button that says it will.
            var at = ImGui.GetCursorScreenPos();
            var word = ImGui.CalcTextSize(label);
            var size = new Vector2(width, ImGui.GetFrameHeight());

            ImGui.Dummy(size);

            var draw = ImGui.GetWindowDrawList();

            // The shape a pill is, since that is what a word on a plate is everywhere else here,
            // in the fill a field wears rather than the one a button does, because this is a
            // label and not something to press.
            EditorDraw.Capsule(at, at + size, ImGui.GetColorU32(ImGuiCol.FrameBg), draw);

            draw.AddText(
                at + new Vector2(EditorSurface.Sides, (size.Y - word.Y) * 0.5f),
                ImGui.GetColorU32(ImGuiCol.TextDisabled),
                label);
        }
    }
}

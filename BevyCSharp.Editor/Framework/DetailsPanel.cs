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
            ctx.Ecs.SetName(entity, renamed);
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

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (EditorEntity.IsDerived(ctx.Ecs, id)) continue;
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (schema.Fields.Count == 0) continue;
            if (Elsewhere.Contains(schema.Name)) continue;

            Component(ctx, entity, schema);
        }

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
                    schema.Remove(ctx.Ecs, entity);
                }

                RoundedRows.Row();
            });

            EditorWidgets.EndFlyout();
        }

        if (open)
        {
            ImGui.Indent(Inset);

            foreach (var field in schema.Fields)
            {
                if (field.Hints.Hidden) continue;

                ComponentFields.Row(ctx, entity, schema, field);
            }

            // Wrapped rather than run off the edge, because a row of buttons as wide as the
            // panel is a row whose last button cannot be pressed.
            var room = ImGui.GetContentRegionAvail().X - Inset;
            var used = 0f;

            foreach (var method in schema.Methods)
            {
                var width = ImGui.CalcTextSize(method.Title).X + (ImGui.GetStyle().FramePadding.X * 2f);

                if (used > 0f && used + width < room) ImGui.SameLine();
                else used = 0f;

                used += width + ImGui.GetStyle().ItemSpacing.X;

                if (ImGui.Button(method.Title)) method.Run(ctx.Ecs, entity);
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

                if (ImGui.MenuItem(schema.Name)) schema.Add(ctx.Ecs, entity);

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

            var width = ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f) + 8f;

            if (used > 0f && used + width < room) ImGui.SameLine();
            else used = 0f;

            used += width + ImGui.GetStyle().ItemSpacing.X;

            if (!any)
            {
                any = true;

                ImGui.Spacing();
                ImGui.TextDisabled("ALSO CARRIES");
                ImGui.Spacing();

                // Said before the first one, so a thing carrying nothing extra says nothing at all
                // rather than showing a heading over an empty row.
                used = width + ImGui.GetStyle().ItemSpacing.X;
            }

            // Drawn rather than asked for, because a tag is a word on a pill and not something to
            // press. A button that answers a click by doing nothing is a button that says it will.
            var at = ImGui.GetCursorScreenPos();
            var word = ImGui.CalcTextSize(label);
            var air = ImGui.GetStyle().FramePadding;

            var size = new Vector2(word.X + (air.X * 2f), word.Y + (air.Y * 2f));

            ImGui.Dummy(size);

            var draw = ImGui.GetWindowDrawList();

            EditorDraw.Rounded(
                at,
                at + size,
                ImGui.GetStyle().FrameRounding,
                ImGui.GetColorU32(ImGuiCol.FrameBg),
                draw);

            draw.AddText(at + air, ImGui.GetColorU32(ImGuiCol.TextDisabled), label);
        }
    }
}

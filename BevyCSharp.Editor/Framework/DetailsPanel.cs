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
/// Bevy's own components are not shown as rows: an entity carries a dozen of them and not one is
/// something a person edits. The ones worth knowing about are named as tags under the entity, which
/// is also where a component of this project's own with no fields at all ends up.
/// </para>
/// </remarks>
public static class DetailsPanel
{
    /// <summary>Draws the selection.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        ImGui.TextDisabled("DETAILS");

        // How many were picked, when it is more than one. The rest of the panel is about the last
        // of them, and without this a drag that took a dozen things looks like a click that took
        // one: the other eleven are only visible in the list, which may not be on screen.
        if (EditorSelection.Count > 1)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"({EditorSelection.Count} selected)");
        }

        ImGui.Spacing();

        if (!EditorSelection.Any)
        {
            ImGui.TextDisabled("Nothing selected");
            return;
        }

        var entity = EditorSelection.Current;
        var name = ctx.Ecs.NameOf(entity) ?? $"Entity {entity.Index}";

        ImGui.SetNextItemWidth(-1f - EditorSceneFrame.DockRoom());
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
        // scrolls, makes the region jump while it is being scrolled: the height it reports depends
        // on what is visible, and what is visible depends on the height. A card is a rectangle
        // drawn behind a group instead, so this is one region with one scroll and no argument.
        // The room the button at the bottom keeps for itself, taken out of the scrolling region
        // rather than scrolled with it: adding a component is not something to go looking for at
        // the end of a long list of what is already there.
        var button = ImGui.GetFrameHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2f);
        var room = ImGui.GetContentRegionAvail().Y - button;

        var open = EditorSurface.Region(
            "##components",
            new Vector2(0f, MathF.Max(1f, room)),
            ImGuiChildFlags.NavFlattened);

        if (!open)
        {
            ImGui.EndChild();
            return;
        }

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (schema.Fields.Count == 0) continue;

            Component(ctx, entity, schema);
        }

        // What the thing is, under what can be edited about it: a tag says what something carries,
        // which is worth knowing and never worth the room at the top.
        Tags(ctx, entity);

        ImGui.EndChild();

        Add(ctx, entity);
    }

    /// <summary>How much air a component's card keeps inside its own edge.</summary>
    internal const float Inset = 6f;

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

        // The header wears no fill of its own, and neither does the card: both are drawn behind,
        // so the card can be exactly the header grown downwards. Nothing is indented round it, so
        // a component that is closed is the header and nothing else, at the header's own size.
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
        if (ImGui.BeginPopupContextItem($"##menu{schema.Name}"))
        {
            RoundedRows.Rows(() =>
            {
                if (ImGui.MenuItem("Remove", string.Empty, false, schema.CanAdd))
                {
                    schema.Remove(ctx.Ecs, entity);
                }

                RoundedRows.Row();
            });

            ImGui.EndPopup();
        }

        if (open)
        {
            ImGui.Indent(Inset);

            foreach (var field in schema.Fields)
            {
                if (field.Hints.Hidden) continue;

                ComponentFields.Row(ctx, entity, schema, field);
            }

            // Wrapped rather than run off the edge: a row of buttons as wide as the panel is a row
            // whose last button cannot be pressed.
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

            draw.AddRectFilled(
                head,
                new Vector2(headTo.X, MathF.Max(headTo.Y, to.Y)),
                ImGui.GetColorU32(EditorTheme.LiveGroup),
                round);

            // And the header on top of it when the pointer is there, which is the one thing that
            // says a header is something to press.
            if (over)
            {
                draw.AddRectFilled(head, headTo, ImGui.GetColorU32(EditorTheme.LiveHover), round);
            }
        }

        draw.ChannelsMerge();

        ImGui.Spacing();
    }

    /// <summary>
    /// What can be put on the entity that is not on it already.
    /// </summary>
    /// <remarks>
    /// Only what the generator emitted a way to add, which is every <c>[Behavior]</c> struct: the
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

        if (!ImGui.BeginPopup("##add")) return;

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
            if (!any) ImGui.TextDisabled("nothing left to add");
        });

        ImGui.EndPopup();
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
            var schema = ComponentSchemas.For(id);

            // Something of this project's own with no fields to edit. Not the engine's: a mesh, a
            // material and a visibility are on everything that is drawn, so naming them says
            // nothing about the thing being looked at and crowds out what does.
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

            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGui.SmallButton(label);
            ImGui.PopStyleColor(2);
        }
    }
}

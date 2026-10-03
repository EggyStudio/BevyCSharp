using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Data assets in the editor: making one, picking one for a field, and writing back what was
/// changed.
/// </summary>
/// <remarks>
/// <para>
/// A data asset is edited where an entity is, in the details panel, when its file is the last
/// thing chosen in the asset browser. Its fields are the rows a component's are, drawn from the
/// schema the generator emitted for its type, and a change is written to the file at the end of
/// the frame, so dragging a slider writes once a frame rather than once per step.
/// </para>
/// <para>
/// A field holding a <see cref="DataRef{T}"/> offers the files whose type matches, so a weapon
/// field lists weapons and not every data file in the project. The list is read when it opens,
/// since knowing a file's type means opening it.
/// </para>
/// </remarks>
public static class EditorDataAssets
{
    /// <summary>Adds the command that makes a new data asset.</summary>
    internal static void Register()
    {
        EditorMenu.Command("Project/New data asset", static _ => Choose(), 5, EditorIcons.Data);
    }

    /// <summary>Offers every data asset type to make one of, asked for each time it opens.</summary>
    /// <remarks>
    /// Asked rather than listed once, because a type a reloaded script declares is registered after
    /// the menu was built.
    /// </remarks>
    private static void Choose() =>
        PickerWindow.Open(
            "New data asset",
            static () => [.. DataAssets.Types.Select(type => new PickerItem(Short(type), EditorIcons.Data, _ => Make(type)))],
            "No [DataAsset] types in this project");

    /// <summary>
    /// Makes a data asset of a type in the directory the asset browser is looking at, and selects it
    /// so the details panel shows it.
    /// </summary>
    private static void Make(string type)
    {
        var folder = EditorAssets.Directory;
        var stem = Short(type);
        var path = Join(folder, stem + DataAssets.Extension);

        for (var n = 2; File.Exists(EditorAssets.Absolute(path)); n++)
            path = Join(folder, $"{stem} {n}{DataAssets.Extension}");

        DataAssets.Create(type, path);
        EditorAssets.Select(path);
        Console.WriteLine($"[editor] made {path}");
    }

    /// <summary>
    /// A field holding a reference to a data asset, chosen from the files of its type.
    /// </summary>
    internal static void Picker(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        var held = value is IDataRef { Id: not 0 } reference
            ? AssetIds.PathOf(reference.Id) ?? "missing file"
            : "Nothing";

        EditorWidgets.Picking(id, held, () =>
        {
            if (ImGui.Selectable("Nothing")) field.Write(ctx.Ecs, entity, 0UL);

            RoundedRows.Row();

            foreach (var file in Of(field.Hints.Asset))
            {
                var chosen = ImGui.Selectable(file, file == held);

                RoundedRows.Row(file == held);

                // Given an id as it is chosen, so a file never referred to before is referred to
                // by the id that keeps the reference through a rename.
                if (chosen) field.Write(ctx.Ecs, entity, AssetIds.IdOf(file, create: true));
            }
        });
    }

    /// <summary>
    /// The asset a reference field names, as a fold under the field's row holding the asset's own
    /// fields, with how many entities share it and a way to stop sharing it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shut by default, because the fields are shared, and an edit here changes every entity that
    /// refers to the asset, which the fold's name says by counting them. Open, the rows are the
    /// asset's, drawn and recorded as when its file is selected in the asset browser.
    /// </para>
    /// <para>
    /// "Make unique" copies the asset to a file of its own beside it and points this field at the
    /// copy, for the one chest whose loot differs. It is offered only while something else shares
    /// the asset, since a copy of one used once changes nothing.
    /// </para>
    /// </remarks>
    internal static void Opened(BehaviorContext ctx, Entity entity, ComponentField field, ulong id)
    {
        if (DataAssets.SchemaOf(id) is not { } schema) return;

        var users = Users(ctx.Ecs, id);
        var shared = users switch
        {
            0 or 1 => "used here only",
            2 => "shared with one other",
            _ => $"shared with {users - 1} others",
        };

        ImGui.Indent();
        var open = ImGui.TreeNodeEx(
            $"{Short(schema.QualifiedName)}, {shared}##shared",
            ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding);

        if (ImGui.IsItemHovered())
            EditorWidgets.Tip("The asset's own fields. A change here reaches everything that refers to it.");

        if (open)
        {
            if (users > 1 && AssetIds.PathOf(id) is { } path && ImGui.Button("Make unique"))
            {
                var copy = DataAssets.Copy(id, Unused(path));
                field.Write(ctx.Ecs, entity, copy);
                Console.WriteLine($"[editor] copied {path} to {AssetIds.PathOf(copy)} for this one");
            }

            foreach (var part in schema.Fields) ComponentFields.Row(ctx, Entity.None, schema, part);
            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

    /// <summary>How many entities have a field referring to a data asset.</summary>
    private static int Users(EcsWorld world, ulong id)
    {
        var users = 0;
        foreach (var entity in world.All())
        {
            var uses = world.ComponentsOf(entity)
                .Select(ComponentSchemas.For)
                .OfType<ComponentSchema>()
                .SelectMany(schema => schema.Fields)
                .Any(field => field.Kind == FieldKind.Data && field.Read(world, entity) is IDataRef reference && reference.Id == id);

            if (uses) users++;
        }

        return users;
    }

    /// <summary>A file name beside a data file that nothing is called, for its copy.</summary>
    private static string Unused(string path)
    {
        var folder = EditorAssets.Parent(path);
        var stem = Path.GetFileName(path)[..^DataAssets.Extension.Length];

        for (var n = 2; ; n++)
        {
            var tried = Join(folder, $"{stem} {n}{DataAssets.Extension}");
            if (!File.Exists(EditorAssets.Absolute(tried))) return tried;
        }
    }

    /// <summary>The data asset files holding a type, or every one when no type is named.</summary>
    private static IEnumerable<string> Of(string? type) =>
        EditorAssets.Every([".json"])
            .Where(file => file.EndsWith(DataAssets.Extension, StringComparison.OrdinalIgnoreCase))
            .Where(file => type is null || DataAssets.TypeOf(file) == type);

    private static string Short(string type) => type[(type.LastIndexOf('.') + 1)..];

    private static string Join(string folder, string name) => folder.Length == 0 ? name : folder + "/" + name;
}

/// <summary>Writes the data assets changed through the editor, once a frame.</summary>
[Behavior]
public partial struct EditorDataSave
{
    /// <summary>Writes every data asset changed this frame.</summary>
    [OnLast]
    public static void Save(BehaviorContext ctx)
    {
        if (!App.HasEditor) return;

        DataAssets.SaveChanged();
    }
}

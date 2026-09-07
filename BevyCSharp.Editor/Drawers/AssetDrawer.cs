using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// A reference to an asset: the file it points at, and a way to point it somewhere else.
/// </summary>
/// <remarks>
/// <para>
/// A handle is a key into a table on the engine's side, so what a field holds is a number that
/// means nothing to anybody. What it points at is a file, and that is what is shown: the path,
/// short enough to read, with the whole of it in the row's own hint.
/// </para>
/// <para>
/// Pressing it offers the files under the asset root that suit the field. Which those are is the
/// field's own business, said with an attribute, because a handle to a mesh and a handle to a
/// sound are the same type and nothing else can tell them apart.
/// </para>
/// </remarks>
public sealed class AssetDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Asset;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Title);
        row.Button(Named(target.Read() as AssetHandle? ?? AssetHandle.None));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
    }

    /// <inheritdoc/>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var field = target.Field;
        var entity = target.Entity;
        var kind = field.Hints.Asset ?? AssetKind.Mesh;
        var items = new List<MenuItem>
        {
            new("Nothing", MenuKind.Command, world =>
                EditorFields.Change(world, entity, field, AssetHandle.None)),
        };

        foreach (var file in EditorAssets.Every(Suits(field)))
        {
            var chosen = file;

            items.Add(new MenuItem(
                Short(file),
                MenuKind.Command,
                world =>
                {
                    // Loaded when it is chosen rather than when the menu is built: a menu that
                    // loaded every file it offered would load the whole project to ask a question.
                    var handle = AssetServer.Load(kind, chosen);
                    EditorFields.Change(world, entity, field, handle);
                },
                Icon: EditorAssets.IconOf(file)));
        }

        if (items.Count == 1) items.Add(new MenuItem("no files of that kind", MenuKind.Separator));

        var (x, y) = row.Below;
        // At the row it was opened from, like every other list the inspector offers. Which field a
        // list of values belongs to is said by where it appeared.
        EditorShell.ShowMenu(field.Title, items, x, y, beside: false);
    }

    /// <summary>Which files suit a field: what it asked for, or what its kind usually is.</summary>
    private static IReadOnlyCollection<string> Suits(ComponentField field)
    {
        if (field.Hints.Extensions is { Length: > 0 } asked)
        {
            return [.. asked.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.StartsWith('.') ? part : "." + part)
                .Select(part => part.ToLowerInvariant())];
        }

        return [.. EditorAssets.ExtensionsFor(field.Hints.Asset ?? AssetKind.Mesh)];
    }

    /// <summary>What an asset field says: the file's name, or that it holds nothing.</summary>
    private static string Named(AssetHandle handle)
    {
        if (!handle.IsValid) return "none";

        return AssetServer.PathOf(handle) is { Length: > 0 } path ? Short(path) : "loaded";
    }

    /// <summary>The end of a path, which is what fits in a row.</summary>
    private static string Short(string path)
    {
        var cut = path.LastIndexOf('/');
        return cut < 0 ? path : path[(cut + 1)..];
    }
}

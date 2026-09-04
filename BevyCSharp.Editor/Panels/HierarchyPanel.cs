using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The world as a list of entities, and where a selection comes from.
/// </summary>
/// <remarks>
/// <para>
/// The rows are a pool rather than a list: the document declares a fixed number of them and this
/// decides what each one stands for. That is not a workaround for a document being static, it is
/// what a list of ten thousand entities wants anyway, since only the rows on screen are ever
/// drawn. Scrolling moves what the pool points at, not the pool.
/// </para>
/// <para>
/// Nothing here knows what an inspector is. It writes <see cref="EditorSelection"/>, and whatever
/// cares reads it.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/hierarchy.html",
    Root = "#hierarchy",
    Handle = "#hierarchy-title",
    Region = EditorRegion.TopLeft,
    Y = 34f)]
public sealed partial class HierarchyPanel
{
    /// <summary>How many rows the document declares.</summary>
    /// <remarks>
    /// The one number that has to agree with the HTML, because the elements are named in it. A
    /// document with more rows than this shows blank ones; with fewer, the extra bindings find
    /// nothing and do nothing.
    /// </remarks>
    public const int Rows = 18;

    /// <summary>What each row says.</summary>
    [Bind("#hrow", Count = Rows)]
    public string[] Labels = new string[Rows];

    /// <summary>Which rows stand for an entity at all.</summary>
    [Show("#hrow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Narrows the list to entities whose name contains this.</summary>
    [Bind("#hier-filter")]
    public string Filter = string.Empty;

    /// <summary>
    /// Whether the editor's own widgets are listed alongside the world's entities.
    /// </summary>
    /// <remarks>
    /// Off, because the interface is built out of entities in the same world as the scene and
    /// names every one of them, so listing them buries the four things a person spawned under a
    /// thousand spans of text. It can be turned on, because those entities are a real part of
    /// this world and an editor that cannot show part of its own world is hiding something.
    /// </remarks>
    [Bind("#hier-interface")]
    public bool ShowInterface;

    /// <summary>How much of the world is being shown.</summary>
    [Bind("#hier-count", Mode = BindMode.OneWay)]
    public string Summary { get; private set; } = string.Empty;

    /// <summary>What the rows currently stand for, so a click can be turned into an entity.</summary>
    private readonly Entity[] _entities = new Entity[Rows];

    /// <summary>How far down the list the pool is looking.</summary>
    private int _scroll;

    /// <summary>How many entities matched the filter, which is what bounds the scroll.</summary>
    private int _matched;

    /// <summary>Fills the rows from the world.</summary>
    /// <remarks>
    /// Every frame, because entities are spawned and despawned by anything and nothing reports
    /// it. Walking a few thousand entities and asking each for its name is cheap enough to do
    /// that; if it ever stops being, the fix is to notice the world's change tick rather than to
    /// refresh less often and show something stale.
    /// </remarks>
    [OnRefresh]
    public void Fill()
    {
        Roll();

        var world = EditorShell.Ecs;
        var all = world.All();
        var wanted = Filter.Trim();

        var written = 0;
        var seen = 0;

        // Named entities first, then the rest. A world has a thousand entities the engine spawned
        // for itself and a handful somebody meant, and the handful are the ones that were given
        // names. Ordering by that puts what a person is looking for on the first screen instead of
        // somewhere down a list of a thousand.
        foreach (var (entity, label) in Listed(world, all))
        {
            if (!ShowInterface && EditorEntity.IsInterface(world, entity)) continue;

            if (wanted.Length > 0
                && !label.Contains(wanted, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            seen++;
            if (seen <= _scroll) continue;
            if (written >= Rows) continue;

            // The selected row says so in its text. A row cannot be given a class while the
            // editor runs, and marking it in the one channel a row does have is honest about
            // that rather than pretending the highlight exists.
            Labels[written] = entity == EditorSelection.Current ? "> " + label : "  " + label;
            _entities[written] = entity;
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Rows; i++)
        {
            Labels[i] = string.Empty;
            _entities[i] = Entity.None;
            Shown[i] = false;
        }

        _matched = seen;
        Summary = wanted.Length > 0
            ? $"{seen}/{all.Length}"
            : $"{Math.Min(_scroll + written, seen)}/{seen}";
    }

    /// <summary>Scrolls the list when the wheel is rolled over it.</summary>
    /// <remarks>
    /// Three rows a notch, which is what a list of eighteen wants: a screenful takes two rolls,
    /// and a single notch does not lose the reader's place.
    /// </remarks>
    private void Roll()
    {
        if (EditorShell.Context is not { } ctx) return;

        var wheel = ctx.Input.WheelY;
        if (wheel == 0f) return;
        if (Window?.Covers(ctx.Input.MouseX, ctx.Input.MouseY) != true) return;

        var moved = _scroll - ((int)wheel * 3);
        _scroll = Math.Clamp(moved, 0, Math.Max(0, _matched - Rows));
    }

    /// <summary>Every entity with the label it is shown under, named ones first.</summary>
    /// <remarks>
    /// The order is the panel's, not the world's: Bevy hands entities back in whatever order its
    /// storage holds them, which is an answer about archetypes rather than about anything a
    /// person cares to see.
    /// </remarks>
    private static IEnumerable<(Entity Entity, string Label)> Listed(EcsWorld world, Entity[] all)
    {
        var unnamed = new List<(Entity, string)>();

        foreach (var entity in all)
        {
            if (world.NameOf(entity) is { } name)
                yield return (entity, name);
            else
                unnamed.Add((entity, $"Entity {entity.Index}"));
        }

        foreach (var entry in unnamed) yield return entry;
    }

    /// <summary>Selects whatever a row stands for.</summary>
    [Command("#hrow", Count = Rows)]
    public void Choose(int row)
    {
        var entity = _entities[row];
        if (entity.IsNone) return;

        EditorSelection.Select(entity);
    }

    /// <summary>Spawns an entity, names it and selects it.</summary>
    /// <remarks>
    /// Named because the hierarchy is a list of names and an unnamed entity is a number in it,
    /// and because the world file matches entities up by name: something spawned here and left
    /// unnamed could not be saved.
    /// </remarks>
    [Command("#hier-new")]
    public void New()
    {
        var world = EditorShell.Ecs;
        var entity = world.Spawn();

        world.Add(entity, Transform.Identity);
        world.SetName(entity, $"Entity {entity.Index}");

        EditorSelection.Select(entity);
        _scroll = 0;

        // Spawning is undoable exactly: taking it back is despawning what was just made, and
        // nothing else in the world knows about it yet. Deleting is not, which is why there is
        // nothing recorded below.
        EditorHistory.Record(
            "new entity",
            undo => undo.Despawn(entity),
            redo =>
            {
                var again = redo.Spawn();
                redo.Add(again, Transform.Identity);
                redo.SetName(again, $"Entity {again.Index}");
            });
    }

    /// <summary>Despawns whatever is selected.</summary>
    [Command("#hier-delete")]
    public void Delete()
    {
        if (!EditorSelection.Any) return;

        EditorShell.Ecs.Despawn(EditorSelection.Current);
        EditorSelection.Clear();
    }

    /// <summary>Moves the pool up one screen.</summary>
    [Command("#hier-up")]
    public void PageUp() => _scroll = Math.Max(0, _scroll - Rows);

    /// <summary>Moves it down one screen, stopping at the end of the list.</summary>
    [Command("#hier-down")]
    public void PageDown() => _scroll = Math.Min(Math.Max(0, _matched - Rows), _scroll + Rows);
}

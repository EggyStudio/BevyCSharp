// Bevy's observers example, examples/ecs/observers.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Observes events, both a component's addition and removal and events of the example's own. A
// field of mines is kept in a spatial index by observers of Mine coming and going, a click
// explodes the mines under it, and each mine that explodes sets off the ones it overlaps.
internal static class Observers
{
    // An ordinary event, which every observer of it runs for.
    internal readonly record struct ExplodeMines(float X, float Y, float Radius);

    // An entity event, which runs the observers of every Explode and then the ones of its entity.
    internal readonly record struct Explode(Entity Entity) : IEntityEvent;

    // The cell size has to be bigger than any explosion's radius.
    private const float CellSize = 64f;

    private static readonly Dictionary<(int, int), HashSet<Entity>> Index = [];
    private static bool _explosionsEnabled;
    private static Entity _camera;

    public static void Build(App app)
    {
        Index.Clear();
        _explosionsEnabled = true;

        app.Startup(Setup, "observers.Setup");

        // Runs whenever ExplodeMines is triggered. Bevy gives the observer a run condition, that
        // explosions are enabled, which here is the observer's first line. Space toggles it.
        app.AddObserver<ExplodeMines>(explode =>
        {
            if (!_explosionsEnabled) return;
            foreach (var entity in Nearby(explode.Event.X, explode.Event.Y))
            {
                // An explosion earlier in this loop may have set this one off already.
                if (!explode.Ecs.Has<Mine>(entity)) continue;
                var mine = explode.Ecs.GetOrDefault<Mine>(entity);
                var distance = MathF.Sqrt((mine.X - explode.Event.X) * (mine.X - explode.Event.X) + (mine.Y - explode.Event.Y) * (mine.Y - explode.Event.Y));
                if (distance < mine.Size + explode.Event.Radius) explode.Ecs.Trigger(new Explode(entity));
            }
        });

        // Places each mine in the spatial index as Mine is added to it.
        app.AddObserver<Add<Mine>>(add =>
        {
            var cell = Cell(add.Event.Value.X, add.Event.Value.Y);
            if (!Index.TryGetValue(cell, out var mines)) Index[cell] = mines = [];
            mines.Add(add.Event.Entity);
        });

        // Takes each mine out of the index as Mine leaves it, despawned included, from the value
        // the removal is handed, since the component itself is already gone.
        app.AddObserver<Remove<Mine>>(remove =>
        {
            if (Index.TryGetValue(Cell(remove.Event.Value.X, remove.Event.Value.Y), out var cell)) cell.Remove(remove.Event.Entity);
        });

        app.Update(ctx =>
        {
            // Explodes the mines at the point clicked.
            var (x, y) = ctx.Input.MousePosition;
            if (ctx.Input.MousePressed(MouseButton.Left) && Render.TryRay(_camera, x, y, out var origin, out _))
                ctx.Ecs.Trigger(new ExplodeMines(origin.X, origin.Y, 1f));
        }, "observers.HandleClick");

        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.Space)) return;
            _explosionsEnabled = !_explosionsEnabled;
            Console.WriteLine($"Explosions {(_explosionsEnabled ? "ENABLED" : "DISABLED")}");
        }, "observers.ToggleExplosions");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _camera = Render2d.SpawnCamera2d();
        Ui.SpawnText(
            "Click on a \"Mine\" to trigger it.\nWhen it explodes it will trigger all overlapping mines.\nPress Space to toggle explosions (demonstrates observer run conditions).",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) },
            20f);

        // Seeded, so the field is the same each run, as Bevy's is from its seeded generator.
        var random = new Random(1987836746);

        // An observer of one entity, run whenever Explode is triggered for it.
        var first = ecs.Spawn();
        ecs.Add(first, Mine.Random(random));
        ecs.Observe<Explode>(first, ExplodeMine);

        // Bevy makes one observer watch every other mine, where here each mine is given the same
        // handler, which comes to the same.
        for (var i = 0; i < 1000; i++)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Mine.Random(random));
            ecs.Observe<Explode>(entity, ExplodeMine);
        }
    }

    private static void ExplodeMine(On<Explode> explode)
    {
        var ecs = explode.Ecs;
        var entity = explode.Event.Entity;
        if (!ecs.IsAlive(entity)) return;

        Console.WriteLine($"Boom! {entity} exploded.");
        var mine = ecs.GetOrDefault<Mine>(entity);
        ecs.Despawn(entity);

        // Sets off a cascade.
        ecs.Trigger(new ExplodeMines(mine.X, mine.Y, mine.Size));
    }

    private static (int, int) Cell(float x, float y) => ((int)MathF.Floor(x / CellSize), (int)MathF.Floor(y / CellSize));

    // Every mine in the cells around the point, copied, since exploding them changes the index.
    private static List<Entity> Nearby(float x, float y)
    {
        var (cx, cy) = Cell(x, y);
        var nearby = new List<Entity>();
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
            if (Index.TryGetValue((cx + dx, cy + dy), out var mines)) nearby.AddRange(mines);
        return nearby;
    }
}

/// <summary>A mine, a circle at a place that explodes when clicked on or when an explosion reaches it.</summary>
[Behavior]
public partial struct Mine
{
    /// <summary>Where it is across.</summary>
    public float X;

    /// <summary>Where it is up.</summary>
    public float Y;

    /// <summary>Its radius.</summary>
    public float Size;

    /// <summary>A mine somewhere in the field, of a size between four and twenty.</summary>
    public static Mine Random(Random random) => new()
    {
        X = (random.NextSingle() - 0.5f) * 1200f,
        Y = (random.NextSingle() - 0.5f) * 600f,
        Size = 4f + random.NextSingle() * 16f,
    };

    /// <summary>Drawn as a circle, its hue by its size.</summary>
    [OnUpdate]
    public void DrawShapes(BehaviorContext ctx) =>
        Gizmos.Circle2d((X, Y), Size, Color.FromHsl((Size - 4f) / 16f * 360f, 1f, 0.8f));
}

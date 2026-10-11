// Bevy's world_serialization example, examples/scene/world_serialization.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Scenes;

// A world saved to a scene file and a scene file loaded into the world. At startup an entity named
// joe, carrying a component of two numbers and one of text with the helmet placed on it, another
// entity of two numbers and a resource of a score are saved under the user's folder and the scene's
// JSON printed, the time since startup left out as Bevy's skips it. Then the scene checked in beside the example
// is loaded, its helmet drawn, and each entity it brings and its resource logged as they arrive.
// Bevy's checked-in scene is RON naming Rust types, so the one here is the same scene in the scene
// file's JSON, naming these.
internal static class WorldSerialization
{
    private const string WorldFilePath = "serialized_worlds/load_scene_example.scene.json";
    private const string NewWorldFilePath = "user://serialized_worlds/load_scene_example-new.scene.json";

    public static void Build(App app)
    {
        // Bevy's two run in either order, since its save writes a world of its own. Here both use the
        // app's, so the save runs first and has its entities gone before the load brings the scene's.
        app.Startup(ctx =>
        {
            SaveWorldSystem(ctx);
            LoadWorldSystem(ctx);
        }, "world_serialization.Setup");
        app.Startup(InfotextSystem, "world_serialization.InfotextSystem");
        app.Update(LogSystem, "world_serialization.LogSystem");
    }

    // Bevy's save_world_system, its world of entities made, saved and printed. Bevy builds them in a
    // world of their own, so here they are spawned, saved alone and taken away again.
    private static void SaveWorldSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        var joe = SceneInstances.Spawn(ecs, "models/FlightHelmet/FlightHelmet.gltf");
        ecs.SetName(joe, "joe");
        ecs.Set(joe, Transform.Identity);
        ecs.Add(joe, new ComponentA { X = 1f, Y = 2f });
        ecs.Add(joe, new ComponentB { Value = "hello", TimeSinceStartup = ctx.Time.Elapsed });

        var other = ecs.Spawn();
        ecs.Add(other, new ComponentA { X = 3f, Y = 4f });

        ctx.World.InsertResource(new ResourceA { Score = 1 });

        var saved = new HashSet<Entity> { joe, other };
        SceneFile.Save(ecs, NewWorldFilePath, saved.Contains);
        Log.Info(File.ReadAllText(SceneFile.Resolve(NewWorldFilePath)));

        ecs.Despawn(joe);
        ecs.Despawn(other);
        ctx.World.RemoveResource<ResourceA>();
    }

    // Bevy's load_world_system, the scene loaded with a camera and a light to see the helmet by.
    private static void LoadWorldSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        SceneFile.Load(ecs, WorldFilePath);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(1f, 1f, 1f), new Vec3(0f, 0.25f, 0f), Vec3.UnitY));

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(light, Transform.LookingAt(Vec3.Zero, new Vec3(0f, -1f, -1f), Vec3.UnitY));
    }

    // Bevy's log_system, each entity whose ComponentA arrived or changed since the last frame, and the
    // resource the frame it was put in. The query leaves the rows unmarked, so a row is logged once
    // for each change rather than every frame after.
    private static void LogSystem(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<ComponentA>(markChanged: false))
        {
            if (!ctx.Ecs.Changed<ComponentA>(row.Entity)) continue;

            Log.Info($"  Entity({row.Entity.Index})");
            Log.Info($"    ComponentA: {{ x: {row.Component.X} y: {row.Component.Y} }}\n");
        }

        if (ctx.World.TryGetResource<ResourceA>(out var resource) && ctx.World.IsAdded<ResourceA>())
            Log.Info($"  New ResourceA: {{ score: {resource.Score} }}\n");
    }

    // Bevy's infotext_system.
    private static void InfotextSystem(BehaviorContext ctx) =>
        Ui.SpawnText("Check the console output for more!", new UiSettings { AlignSelf = UiAlignSelf.FlexEnd }, 42f);
}

/// <summary>Bevy's ComponentA, two numbers.</summary>
[Behavior]
public partial struct ComponentA
{
    public float X;
    public float Y;
}

/// <summary>Bevy's ComponentB, text, and a time a scene leaves out.</summary>
[Behavior]
public partial struct ComponentB
{
    public EcsText Value;

    /// <summary>How long the app had run when it was made, which means nothing to another run, so a scene leaves it out.</summary>
    [NotSaved] public float TimeSinceStartup;
}

/// <summary>Bevy's ResourceA, a score a scene writes with its entities.</summary>
[SceneResource]
public sealed class ResourceA
{
    public int Score;
}

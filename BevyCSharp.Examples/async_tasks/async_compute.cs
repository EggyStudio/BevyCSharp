// Bevy's async_compute example, examples/async_tasks/async_compute.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.AsyncTasks;

// Shows work done off the frame, two hundred and sixteen tasks each waiting up to five seconds
// before saying where its cube goes, the cubes appearing as their tasks finish.
internal static class AsyncCompute
{
    private const int Cubes = 6;

    // Bevy's BoxMeshHandle and BoxMaterialHandle resources, and the tasks, which an entity holds by
    // its slot here, since a task is no value a component can keep.
    internal static AssetHandle BoxMesh, BoxMaterial;
    internal static readonly List<Task<Vec3>> Tasks = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Tasks.Clear();
            BoxMesh = Render.CreateMesh(MeshShape.Cuboid, 0.25f, 0.25f, 0.25f);
            BoxMaterial = Render.CreateMaterial(Color.FromSrgb(1f, 0.2f, 0.3f));

            var offset = Cubes % 2 == 0 ? Cubes / 2 - 0.5f : Cubes / 2;
            ecs.SpawnPointLight(new Vec3(4f, 12f, 15f));
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(offset, offset, 15f), new Vec3(offset, offset, 0f), Vec3.UnitY));

            // .NET's thread pool stands where Bevy's AsyncComputeTaskPool does, each task a wait
            // and then a result, and an empty entity holding each until it is done.
            for (var x = 0; x < Cubes; x++)
            for (var y = 0; y < Cubes; y++)
            for (var z = 0; z < Cubes; z++)
            {
                var at = new Vec3(x, y, z);
                Tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.05 + Random.Shared.NextDouble() * 4.95));
                    return at;
                }));
                ecs.Add(ecs.Spawn(), new ComputeTransform { Slot = Tasks.Count - 1 });
            }
        }, "async_compute.Setup");
    }
}

/// <summary>A task on its way, which gives its entity a cube where the task says once it is done.</summary>
[Behavior]
public partial struct ComputeTransform
{
    /// <summary>The task's place among the example's tasks.</summary>
    public int Slot;

    /// <summary>
    /// The task's cube put on its entity once the task is done, and this taken off, as Bevy
    /// appends the commands the task made and removes the component.
    /// </summary>
    [OnUpdate]
    public void HandleTasks(BehaviorContext ctx)
    {
        var task = AsyncCompute.Tasks[Slot];
        if (!task.IsCompleted) return;

        var (entity, at) = (ctx.Entity, task.Result);
        ctx.Cmd.Run(ecs =>
        {
            ecs.Add(entity, new Transform(at));
            Render.SetMesh(ecs, entity, AsyncCompute.BoxMesh);
            Render.SetMaterial(ecs, entity, AsyncCompute.BoxMaterial);
        });
        ctx.Cmd.Remove<ComputeTransform>(entity);
    }
}

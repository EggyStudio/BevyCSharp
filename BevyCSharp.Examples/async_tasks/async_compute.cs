// Bevy's async_compute example, examples/async_tasks/async_compute.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.AsyncTasks;

// Shows work done off the frame, two hundred and sixteen tasks each waiting up to five seconds
// before saying where its cube goes, the cubes appearing as their tasks finish.
internal static class AsyncCompute
{
    private const int Cubes = 6;

    private static readonly List<Task<Vec3>> Pending = [];
    private static AssetHandle _mesh, _material;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Pending.Clear();
            _mesh = Render.CreateMesh(MeshShape.Cuboid, 0.25f, 0.25f, 0.25f);
            _material = Scene.Material(Scene.Srgb(1f, 0.2f, 0.3f));

            var offset = Cubes % 2 == 0 ? Cubes / 2 - 0.5f : Cubes / 2;
            ecs.PointLight(new Vec3(4f, 12f, 15f));
            ecs.Camera(Transform.LookingAt(new Vec3(offset, offset, 15f), new Vec3(offset, offset, 0f), Vec3.UnitY));

            // .NET's thread pool stands where Bevy's AsyncComputeTaskPool does, each task a wait
            // and then a result.
            for (var x = 0; x < Cubes; x++)
            for (var y = 0; y < Cubes; y++)
            for (var z = 0; z < Cubes; z++)
            {
                var at = new Vec3(x, y, z);
                Pending.Add(Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.05 + Random.Shared.NextDouble() * 4.95));
                    return at;
                }));
            }
        }, "async_compute.Setup");

        // Each frame takes the tasks that have finished and gives their cubes to the world, which
        // only a system may touch.
        app.Update(ctx =>
        {
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                if (!Pending[i].IsCompleted) continue;
                ctx.Ecs.Mesh(_mesh, _material, new Transform(Pending[i].Result));
                Pending.RemoveAt(i);
            }
        }, "async_compute.HandleTasks");
    }
}

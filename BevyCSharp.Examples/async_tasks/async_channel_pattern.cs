// Bevy's async_channel_pattern example, examples/async_tasks/async_channel_pattern.rs at v0.20.0,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Threading.Channels;
using Bevy;

namespace BevyCSharp.Examples.AsyncTasks;

// Shows background tasks reporting through a channel, each of a hundred and forty-four sending its
// cube's place when its wait is over, and the frame placing whatever has arrived under a light
// circling overhead. Each waits on the game's clock (Time.Provider) where Bevy's waits on the
// machine's, which agree while the game keeps its pace, so on a set clock, as a capture runs on, a
// cube lands in the same frame on any device, and where the workflow runs, as a capture does, the
// waits are drawn from a seeded generator.
internal static class AsyncChannelPattern
{
    private const int Cubes = 6;
    private const float LightRadius = 8f;

    private static Channel<Vec3> _channel = Channel.CreateUnbounded<Vec3>();
    private static AssetHandle _mesh, _material;
    private static Entity _light;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _channel = Channel.CreateUnbounded<Vec3>();
            _mesh = Render.CreateMesh(MeshShape.Cuboid, 0.4f, 0.4f, 0.4f);
            _material = Render.CreateMaterial(Color.FromSrgb(1f, 0.2f, 0.3f));

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 1.618f * Cubes), Render.CreateMaterial((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            _light = ecs.SpawnPointLight(new Vec3(0f, LightRadius, 4f), shadows: true);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-6.5f, 5.5f, 12f), Vec3.Zero, Vec3.UnitY));

            var writer = _channel.Writer;
            var clock = ctx.Time.Provider;
            var random = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? new Random(19878367) : new Random();
            for (var x = -Cubes; x < Cubes; x++)
            for (var z = -Cubes; z < Cubes; z++)
            {
                var at = new Vec3(x, 0.5f, z);
                // The wait begun here, in the startup frame, so it is due a set time after it on
                // every run, where begun on the task's thread it could start a frame later.
                var wait = Task.Delay(TimeSpan.FromSeconds(2 + random.NextDouble() * 6), clock);
                _ = Task.Run(async () =>
                {
                    await wait;
                    writer.TryWrite(at);
                });
            }
        }, "async_channel_pattern.Setup");

        app.Update(ctx =>
        {
            while (_channel.Reader.TryRead(out var at)) ctx.Ecs.SpawnMesh(_mesh, _material, new Transform(at));

            var angle = 1.618f * ctx.Time.Elapsed;
            ctx.Ecs.Set(_light, Transform.At(LightRadius * MathF.Cos(angle), LightRadius, LightRadius * MathF.Sin(angle)));
        }, "async_channel_pattern.Update");
    }
}

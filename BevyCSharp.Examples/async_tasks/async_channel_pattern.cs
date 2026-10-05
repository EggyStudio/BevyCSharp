// Bevy's async_channel_pattern example, examples/async_tasks/async_channel_pattern.rs at v0.19.1,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Threading.Channels;
using Bevy;

namespace BevyCSharp.Examples.AsyncTasks;

// Shows background tasks reporting through a channel, each of a hundred and forty-four sending its
// cube's place when its wait is over, and the frame placing whatever has arrived under a light
// circling overhead.
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
            _material = Scene.Material(Scene.Srgb(1f, 0.2f, 0.3f));

            ecs.Mesh(Render.CreateMesh(MeshShape.Circle, 1.618f * Cubes), Scene.Material((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
            _light = ecs.PointLight(new Vec3(0f, LightRadius, 4f), shadows: true);
            ecs.Camera(Transform.LookingAt(new Vec3(-6.5f, 5.5f, 12f), Vec3.Zero, Vec3.UnitY));

            var writer = _channel.Writer;
            for (var x = -Cubes; x < Cubes; x++)
            for (var z = -Cubes; z < Cubes; z++)
            {
                var at = new Vec3(x, 0.5f, z);
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 + Random.Shared.NextDouble() * 6));
                    writer.TryWrite(at);
                });
            }
        }, "async_channel_pattern.Setup");

        app.Update(ctx =>
        {
            while (_channel.Reader.TryRead(out var at)) ctx.Ecs.Mesh(_mesh, _material, new Transform(at));

            var angle = 1.618f * ctx.Time.Elapsed;
            ctx.Ecs.Set(_light, Transform.At(LightRadius * MathF.Cos(angle), LightRadius, LightRadius * MathF.Sin(angle)));
        }, "async_channel_pattern.Update");
    }
}

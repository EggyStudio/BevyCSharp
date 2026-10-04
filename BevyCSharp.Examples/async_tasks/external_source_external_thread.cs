using System.Threading.Channels;
using Bevy;

namespace BevyCSharp.Examples.AsyncTasks;

// Shows a thread of its own feeding the app through a channel, a stream of numbers read twice a
// second on the fixed timestep and each falling down the window as text.
internal static class ExternalSourceExternalThread
{
    private const string Text2d = "bevy_sprite::text2d::Text2d";

    internal readonly record struct StreamMessage(uint Value);

    private static readonly List<Entity> Texts = [];
    private static Channel<uint> _stream = Channel.CreateBounded<uint>(1);
    private static CancellationTokenSource _stop = new();

    // Bevy's Time::<Fixed>::from_seconds(0.5), as a rate.
    public static void Configure(Config config) => config.FixedHz = 2;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            Render2d.SpawnCamera2d();
            Texts.Clear();
            _stream = Channel.CreateBounded<uint>(1);
            _stop = new CancellationTokenSource();

            // One number at a time, the thread waiting while the last is unread, as Bevy's
            // bounded channel of one has it.
            var (writer, stop) = (_stream.Writer, _stop.Token);
            new Thread(() =>
            {
                var random = new Random(19878367);
                try
                {
                    while (!stop.IsCancellationRequested) writer.WriteAsync((uint)random.Next(0, 2000), stop).AsTask().Wait(stop);
                }
                catch (OperationCanceledException)
                {
                }
            }) { IsBackground = true }.Start();
        }, "external_source_external_thread.Setup");

        app.On(Stage.FixedUpdate, ctx =>
        {
            while (_stream.Reader.TryRead(out var value)) ctx.Send(new StreamMessage(value));
        }, "external_source_external_thread.ReadStream");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var perFrame = 0;
            foreach (var message in ctx.Read<StreamMessage>())
            {
                var text = ecs.Spawn();
                ecs.Add(text, Transform.At(perFrame++ * 100f, 300f, 0f));
                ecs.InsertReflected(text, Text2d, $"\"{message.Value}\"");
                Texts.Add(text);
            }

            for (var i = Texts.Count - 1; i >= 0; i--)
            {
                var transform = ecs.GetOrDefault<Transform>(Texts[i]);
                transform.Translation -= new Vec3(0f, 100f * ctx.Time.Delta, 0f);
                if (transform.Translation.Y >= -300f)
                {
                    ecs.Set(Texts[i], transform);
                    continue;
                }

                ecs.Despawn(Texts[i]);
                Texts.RemoveAt(i);
            }
        }, "external_source_external_thread.SpawnAndMoveText");

        app.On(Stage.Cleanup, _ => _stop.Cancel(), "external_source_external_thread.Stop");
    }
}

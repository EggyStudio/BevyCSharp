// Bevy's spatial_audio_2d example, examples/audio/spatial_audio_2d.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows spatial audio in 2D, music played from a blue circle moving side to side past a listener
// whose ears are the red and green squares, the arrow keys moving the listener and Space stopping
// the circle.
internal static class SpatialAudio2d
{
    // The space between the two ears.
    private const float Gap = 400f;

    // The listener, which Bevy finds by its SpatialListener.
    private static Entity _listener;

    // A 2D camera measures a pixel to a unit, so a hundred of them make the meter a sound fades by,
    // as Bevy's AUDIO_SCALE of a hundredth does.
    public static void Configure(Config config) => config.SpatialScale = 100f;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var emitter = ecs.Spawn();
            ecs.Add(emitter, Transform.At(0f, 50f, 0f));
            ecs.Add(emitter, new Emitter());
            Render2d.SetMesh(ecs, emitter, Render.CreateMesh(MeshShape.Circle, 15f));
            Render2d.SetMaterial(ecs, emitter, Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 1f, 1f) }));
            var playing = Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg"), new AudioSettings { Mode = PlaybackMode.Loop, Spatial = true });
            ecs.SetParent(playing, emitter);

            // The listener's ears sit half the gap to either side, each a square of one color, as
            // Bevy's Sprite::from_color draws them, a white pixel tinted and sized.
            _listener = ecs.Spawn();
            ecs.Add(_listener, Transform.Identity);
            Audio.SetListener(_listener, Gap);
            var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
            foreach (var (color, x) in new[] { ((1f, 0f, 0f, 1f), -Gap / 2f), ((0f, 1f, 0f, 1f), Gap / 2f) })
            {
                var ear = ecs.Spawn();
                ecs.Add(ear, Transform.At(x, 0f, 0f));
                Render2d.SetSprite(ecs, ear, white, new SpriteSettings { Color = color, Size = (20f, 20f) });
                ecs.SetParent(ear, _listener);
            }

            Ui.SpawnText("Up/Down/Left/Right: Move Listener\nSpace: Toggle Emitter Movement",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
            Render2d.SpawnCamera2d();
        }, "spatial_audio_2d.Setup");

        app.Update(ctx =>
        {
            var (ecs, input) = (ctx.Ecs, ctx.Input);
            const float Speed = 200f;
            var step = Speed * ctx.Time.Delta;
            var listener = ecs.GetOrDefault<Transform>(_listener);
            if (input.KeyDown(Key.ArrowRight)) listener.Translation += new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowLeft)) listener.Translation -= new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowUp)) listener.Translation += new Vec3(0f, step, 0f);
            if (input.KeyDown(Key.ArrowDown)) listener.Translation -= new Vec3(0f, step, 0f);
            ecs.Set(_listener, listener);
        }, "spatial_audio_2d.UpdateListener");
    }
}

/// <summary>The circle the music plays from, and the stopwatch it moves by, which Space stops and starts.</summary>
[Behavior]
public partial struct Emitter
{
    /// <summary>How long it has run, in seconds.</summary>
    public float Elapsed;

    /// <summary>Whether it is stopped.</summary>
    public bool Paused;

    /// <summary>Moved side to side by its stopwatch while it runs, as Bevy's <c>update_emitters</c> moves it.</summary>
    [OnUpdate]
    public void UpdateEmitters(BehaviorContext ctx, ref Transform transform)
    {
        if (ctx.Input.KeyPressed(Key.Space)) Paused = !Paused;
        if (Paused) return;
        Elapsed += ctx.Time.Delta;
        transform.Translation.X = MathF.Sin(Elapsed) * 500f;
    }
}

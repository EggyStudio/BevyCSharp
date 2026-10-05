// Bevy's spatial_audio_2d example, examples/audio/spatial_audio_2d.rs at v0.19.1, by Bevy's
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

    private static Entity _emitter, _listener;
    private static float _stopwatch;
    private static bool _paused;

    // A 2D camera measures a pixel to a unit, so a hundred of them make the meter a sound fades by,
    // as Bevy's AUDIO_SCALE of a hundredth does.
    public static void Configure(Config config) => config.SpatialScale = 100f;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_stopwatch, _paused) = (0f, false);

            _emitter = ecs.Spawn();
            ecs.Add(_emitter, Transform.At(0f, 50f, 0f));
            Render2d.SetMesh(ecs, _emitter, Render.CreateMesh(MeshShape.Circle, 15f));
            Render2d.SetMaterial(ecs, _emitter, Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 1f, 1f) }));
            var playing = Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg"), new AudioSettings { Mode = PlaybackMode.Loop, Spatial = true });
            ecs.SetParent(playing, _emitter);

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

            if (input.KeyPressed(Key.Space)) _paused = !_paused;
            if (!_paused)
            {
                _stopwatch += ctx.Time.Delta;
                var emitter = ecs.GetOrDefault<Transform>(_emitter);
                ecs.Set(_emitter, emitter with { Translation = emitter.Translation with { X = MathF.Sin(_stopwatch) * 500f } });
            }

            const float Speed = 200f;
            var step = Speed * ctx.Time.Delta;
            var listener = ecs.GetOrDefault<Transform>(_listener);
            if (input.KeyDown(Key.ArrowRight)) listener.Translation += new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowLeft)) listener.Translation -= new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowUp)) listener.Translation += new Vec3(0f, step, 0f);
            if (input.KeyDown(Key.ArrowDown)) listener.Translation -= new Vec3(0f, step, 0f);
            ecs.Set(_listener, listener);
        }, "spatial_audio_2d.Update");
    }
}

// Bevy's spatial_audio_3d example, examples/audio/spatial_audio_3d.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows spatial audio in 3D, music played from a blue sphere circling a listener whose ears are
// the red and green cubes, the arrow keys moving the listener, Space stopping the sphere and M
// muting it.
internal static class SpatialAudio3d
{
    private const float Gap = 4f;

    private static Entity _emitter, _listener, _playing;
    private static float _stopwatch;
    private static bool _paused, _muted;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_stopwatch, _paused, _muted) = (0f, false, false);

            _emitter = ecs.Mesh(Render.CreateMesh(MeshShape.Sphere, 0.2f), Scene.Material(Scene.Srgb(0f, 0f, 1f)), Transform.Identity);
            _playing = Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg"), new AudioSettings { Mode = PlaybackMode.Loop, Spatial = true });
            ecs.SetParent(_playing, _emitter);

            // The listener's ears sit half the gap to either side, and a cube marks each.
            _listener = ecs.Spawn();
            ecs.Add(_listener, Transform.Identity);
            Audio.SetListener(_listener, Gap);
            var ear = Render.CreateMesh(MeshShape.Cuboid, 0.2f, 0.2f, 0.2f);
            ecs.SetParent(ecs.Mesh(ear, Scene.Material(Scene.Srgb(1f, 0f, 0f)), Transform.At(-Gap / 2f, 0f, 0f)), _listener);
            ecs.SetParent(ecs.Mesh(ear, Scene.Material(Scene.Srgb(0f, 1f, 0f)), Transform.At(Gap / 2f, 0f, 0f)), _listener);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

            Ui.SpawnText("Up/Down/Left/Right: Move Listener\nSpace: Toggle Emitter Movement\nM: Toggle Mute",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
            ecs.Camera(Transform.LookingAt(new Vec3(0f, 5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "spatial_audio_3d.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;

            if (input.KeyPressed(Key.Space)) _paused = !_paused;
            if (!_paused)
            {
                _stopwatch += ctx.Time.Delta;
                ecs.Set(_emitter, Transform.At(MathF.Sin(_stopwatch) * 3f, 0f, MathF.Cos(_stopwatch) * 3f));
            }

            const float Speed = 2f;
            var listener = ecs.GetOrDefault<Transform>(_listener);
            var step = Speed * ctx.Time.Delta;
            if (input.KeyDown(Key.ArrowRight)) listener.Translation += new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowLeft)) listener.Translation -= new Vec3(step, 0f, 0f);
            if (input.KeyDown(Key.ArrowDown)) listener.Translation += new Vec3(0f, 0f, step);
            if (input.KeyDown(Key.ArrowUp)) listener.Translation -= new Vec3(0f, 0f, step);
            ecs.Set(_listener, listener);

            if (input.KeyPressed(Key.M))
            {
                _muted = !_muted;
                Audio.SetVolume(_playing, _muted ? 0f : 1f);
            }
        }, "spatial_audio_3d.Update");
    }
}

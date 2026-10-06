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

    // The listener, which Bevy finds by its SpatialListener, and the music playing, which M mutes.
    private static Entity _listener, _playing;
    private static bool _muted;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _muted = false;

            var emitter = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 0.2f), Render.CreateMaterial(Color.FromSrgb(0f, 0f, 1f)), Transform.Identity);
            ecs.Add(emitter, new Emitter3d());
            _playing = Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg"), new AudioSettings { Mode = PlaybackMode.Loop, Spatial = true });
            ecs.SetParent(_playing, emitter);

            // The listener's ears sit half the gap to either side, and a cube marks each.
            _listener = ecs.Spawn();
            ecs.Add(_listener, Transform.Identity);
            Audio.SetListener(_listener, Gap);
            var ear = Render.CreateMesh(MeshShape.Cuboid, 0.2f, 0.2f, 0.2f);
            ecs.SetParent(ecs.SpawnMesh(ear, Render.CreateMaterial(Color.FromSrgb(1f, 0f, 0f)), Transform.At(-Gap / 2f, 0f, 0f)), _listener);
            ecs.SetParent(ecs.SpawnMesh(ear, Render.CreateMaterial(Color.FromSrgb(0f, 1f, 0f)), Transform.At(Gap / 2f, 0f, 0f)), _listener);

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
            ecs.Add(sun, Transform.LookingAt(new Vec3(4f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

            Ui.SpawnText("Up/Down/Left/Right: Move Listener\nSpace: Toggle Emitter Movement\nM: Toggle Mute",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 5f, 5f), Vec3.Zero, Vec3.UnitY));
        }, "spatial_audio_3d.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;

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

/// <summary>Bevy's <c>Emitter</c>, the sphere the music plays from, and the stopwatch it circles by, which Space stops and starts.</summary>
[Behavior]
public partial struct Emitter3d
{
    /// <summary>How long it has run, in seconds.</summary>
    public float Elapsed;

    /// <summary>Whether it is stopped.</summary>
    public bool Paused;

    /// <summary>Moved round the listener by its stopwatch while it runs, as Bevy's <c>update_positions</c> moves it.</summary>
    [OnUpdate]
    public void UpdatePositions(BehaviorContext ctx, ref Transform transform)
    {
        if (ctx.Input.KeyPressed(Key.Space)) Paused = !Paused;
        if (Paused) return;
        Elapsed += ctx.Time.Delta;
        (transform.Translation.X, transform.Translation.Z) = (MathF.Sin(Elapsed) * 3f, MathF.Cos(Elapsed) * 3f);
    }
}

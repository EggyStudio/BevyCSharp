using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>How a clip is played, for <see cref="Animation.Play"/>.</summary>
public sealed class AnimationSettings
{
    /// <summary>Whether it plays over and over, rather than once and holding its last pose.</summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// How many times it plays before holding its last pose, where it does not repeat. Zero and one
    /// are once.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>RepeatAnimation::Count</c>, for a gesture made twice or a bell rung three times.
    /// <see cref="Animation.SetRepeat"/> changes it while the clip plays, without starting it over.
    /// </remarks>
    public uint Times { get; set; }

    /// <summary>How fast, one being as it was made, two twice as fast, and below zero backwards.</summary>
    public float Speed { get; set; } = 1f;

    /// <summary>
    /// Seconds over which whatever played before fades out as this fades in, or zero for a cut.
    /// </summary>
    /// <remarks>
    /// A walk turning into a run over a fifth of a second keeps a character from snapping between
    /// poses. Both clips play during the fade, each weighted by how far it has gone.
    /// </remarks>
    public float Blend { get; set; }
}

/// <summary>What a model is playing, as <see cref="Animation.StateOf"/> reads it.</summary>
/// <param name="Clip">The clip, by name, or nothing when none is playing.</param>
/// <param name="Seconds">Seconds into it.</param>
/// <param name="Speed">How fast it plays.</param>
/// <param name="Paused">Whether it is held where it is.</param>
/// <param name="Finished">Whether a clip that plays once has reached its end.</param>
/// <param name="Completions">How many times a repeating clip has come round to its start.</param>
public readonly record struct AnimationState(
    string? Clip, float Seconds, float Speed, bool Paused, bool Finished, uint Completions);

/// <summary>
/// A clip that plays once reached its end, posted as a message the frame after it did.
/// </summary>
/// <param name="Scene">The entity the model's scene was spawned under, as it was played on.</param>
/// <param name="Clip">The clip, by name.</param>
public readonly record struct AnimationFinished(Entity Scene, string Clip);

/// <summary>
/// Plays a model's animation clips by name, on the entity its scene was spawned under.
/// </summary>
/// <remarks>
/// <para>
/// A glTF file's clips come with it, named as the artist named them, and a model spawned with
/// <see cref="EcsWorld.SpawnScene"/> plays them through the entity that call returned. The first
/// call about a model gathers its clips into what Bevy's animation player reads, so nothing has to
/// be prepared, and every call after finds them again. A skinned character's bones and a
/// propeller's spin are clips alike.
/// </para>
/// <para>
/// A model arrives a few frames after it is spawned, so <see cref="TryClips"/> and
/// <see cref="Play"/> answer false until it has, and asking again next frame is the whole of the
/// protocol. A model with nothing to animate, or an entity no scene was spawned under, is refused
/// with an exception rather than waited on.
/// </para>
/// <para>
/// One clip plays at a time, with a fade from one to the next (<see cref="AnimationSettings.Blend"/>).
/// Masks that play a clip on part of a body, additive layers and a state machine choosing between
/// clips are not here yet.
/// </para>
/// </remarks>
public static unsafe class Animation
{
    /// <summary>The names of a model's clips, or false while it has not arrived.</summary>
    /// <remarks>
    /// In the order the file lists them, and a clip the file gives no name is named as Bevy labels
    /// it, <c>Animation</c> and its number, so every clip can be played.
    /// </remarks>
    /// <param name="scene">The entity the model's scene was spawned under.</param>
    /// <param name="clips">The names, in a fixed order, when this answers true.</param>
    /// <exception cref="BevyNativeException">
    /// The entity has no scene, the scene animates nothing, or this build has no renderer.
    /// </exception>
    public static bool TryClips(Entity scene, out IReadOnlyList<string> clips)
    {
        clips = [];

        var length = Native.bcs_animation_clips(scene.Bits, null, 0);
        if (length == NativeStatus.InvalidState) return false;
        if (length == NativeStatus.Unsupported) throw NoRenderer();

        Native.Check(length, $"reading the clips of {scene}");

        var bytes = new byte[length];
        fixed (byte* buffer = bytes)
        {
            Native.Check(Native.bcs_animation_clips(scene.Bits, buffer, length), $"reading the clips of {scene}");
        }

        clips = length == 0 ? [] : Encoding.UTF8.GetString(bytes).Split('\n');
        return true;
    }

    /// <summary>Plays one of a model's clips, fading out whatever played before.</summary>
    /// <param name="scene">The entity the model's scene was spawned under.</param>
    /// <param name="clip">The clip, by the name the file gives it.</param>
    /// <param name="settings">How to play it, or nothing for once, at its own speed, with a cut.</param>
    /// <returns>Whether it is playing, false while the model has not arrived.</returns>
    /// <exception cref="ArgumentException">The model has no clip of that name.</exception>
    /// <exception cref="BevyNativeException">
    /// The entity has no scene, the scene animates nothing, or this build has no renderer.
    /// </exception>
    public static bool Play(Entity scene, string clip, AnimationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(clip);
        settings ??= new AnimationSettings();

        if (!TryClips(scene, out var clips)) return false;

        var index = IndexOf(clips, clip);
        if (index < 0)
            throw new ArgumentException(
                $"{scene} has no clip called {clip}. It has {(clips.Count == 0 ? "none" : string.Join(", ", clips))}.",
                nameof(clip));

        Native.Check(
            Native.bcs_animation_play(scene.Bits, index, settings.Repeat ? 1 : settings.Times >= 2 ? (int)settings.Times : 0, settings.Speed, settings.Blend),
            $"playing {clip} on {scene}");

        return true;
    }

    /// <summary>Stops every clip on a model, leaving it in the pose it was in.</summary>
    public static void Stop(Entity scene) => Act(Native.bcs_animation_stop(scene.Bits), "stopping", scene);

    /// <summary>Holds what a model is playing where it is.</summary>
    public static void Pause(Entity scene) => Act(Native.bcs_animation_pause(scene.Bits, 1), "pausing", scene);

    /// <summary>Lets what a model is playing go on from where it was held.</summary>
    public static void Resume(Entity scene) => Act(Native.bcs_animation_pause(scene.Bits, 0), "resuming", scene);

    /// <summary>Moves the clip a model is playing to a time from its start.</summary>
    /// <exception cref="BevyNativeException">Nothing is playing, or the model has not arrived.</exception>
    public static void Seek(Entity scene, float seconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(seconds);
        Act(Native.bcs_animation_adjust(scene.Bits, seconds, float.NaN), "seeking", scene);
    }

    /// <summary>
    /// Changes how many times the clip playing plays, counting those it has finished, without
    /// starting it over: zero for ever, one for once.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>set_repeat</c>. A clip that has already played more times than asked stops at the
    /// end of the play it is in.
    /// </remarks>
    /// <exception cref="BevyNativeException">Nothing is playing, or the entity has no scene.</exception>
    public static void SetRepeat(Entity scene, uint times) => Act(Native.bcs_animation_set_repeat(scene.Bits, times), "changing how many times the clip plays on", scene);

    /// <summary>Changes how fast the clip a model is playing goes.</summary>
    /// <exception cref="BevyNativeException">Nothing is playing, or the model has not arrived.</exception>
    public static void SetSpeed(Entity scene, float speed) =>
        Act(Native.bcs_animation_adjust(scene.Bits, -1f, speed), "changing the speed of", scene);

    /// <summary>What a model is playing, or nothing while it has not arrived.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity has no scene, the scene animates nothing, or this build has no renderer.
    /// </exception>
    public static AnimationState? StateOf(Entity scene)
    {
        NativeAnimationState read;
        var answer = Native.bcs_animation_state(scene.Bits, &read);
        if (answer == NativeStatus.InvalidState) return null;
        if (answer == NativeStatus.Unsupported) throw NoRenderer();

        Native.Check(answer, $"reading what {scene} plays");

        string? clip = null;
        if (read.Clip >= 0 && TryClips(scene, out var clips) && read.Clip < clips.Count) clip = clips[read.Clip];

        return new AnimationState(clip, read.Seconds, read.Speed, read.Paused != 0, read.Finished != 0, read.Completions);
    }

    /// <summary>Posts the clips that reached their end since the last frame.</summary>
    /// <remarks>Called once a frame as the engine moves its messages onto the bus.</remarks>
    internal static void PostFinished(MessageBus bus)
    {
        const int Capacity = 16;

        ulong* roots = stackalloc ulong[Capacity];
        int* clips = stackalloc int[Capacity];

        int count;
        while ((count = Native.bcs_animation_finished(roots, clips, Capacity)) > 0)
        {
            for (var i = 0; i < count; i++)
            {
                var scene = new Entity(roots[i]);
                var name = TryClips(scene, out var names) && clips[i] < names.Count ? names[clips[i]] : string.Empty;
                bus.Send(new AnimationFinished(scene, name));
            }

            if (count < Capacity) break;
        }
    }

    /// <summary>A clip's number among a model's, by its exact name, or -1.</summary>
    private static int IndexOf(IReadOnlyList<string> clips, string clip)
    {
        for (var index = 0; index < clips.Count; index++)
        {
            if (clips[index] == clip) return index;
        }

        return -1;
    }

    private static void Act(int status, string doing, Entity scene)
    {
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"{doing} the animation of {scene}");
    }

    private static BevyNativeException NoRenderer() =>
        new(NativeStatus.Unsupported,
            "Animation needs a bridge built with the render or editor profile, which carries "
            + "Bevy's animation player. Rebuild the bridge with build/build-native.sh --render.");
}

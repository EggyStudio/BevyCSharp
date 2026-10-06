using System.Text;
using Bevy.Interop;

namespace Bevy;

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

    /// <summary>Makes an empty clip, which <see cref="AddCurve"/> fills.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>AnimationClip</c> built in code, as its examples make one, a curve for each
    /// property each entity it moves goes through. A player plays a graph made from it with
    /// <see cref="GraphFromClip"/>, and each entity a curve is aimed at carries its
    /// <see cref="AnimationTarget"/> and the player through <see cref="Animate"/>:
    /// </para>
    /// <code>
    /// var clip = Animation.CreateClip();
    /// var planet = AnimationTarget.FromNames("planet");
    /// Animation.AddCurve(clip, planet, AnimationCurve.Translation([0f, 1f, 2f], [a, b, a]));
    /// var (graph, node) = Animation.GraphFromClip(clip);
    /// Animation.PlayGraph(sphere, graph, node, repeat: true);
    /// Animation.Animate(sphere, planet, player: sphere);
    /// </code>
    /// <para>
    /// A model's own clips are played by name with <see cref="Play"/> instead.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateClip()
    {
        var key = Native.bcs_animation_clip_create();
        if (key == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(key, "making an animation clip");
        return new AssetHandle(key);
    }

    /// <summary>Adds a curve to a clip, moving one property of the entity <paramref name="target"/> names.</summary>
    /// <remarks>
    /// Bevy's <c>add_curve_to_target</c>. A target takes a curve for each of its properties, and
    /// one curve of each property, a second replacing the first.
    /// </remarks>
    /// <param name="clip">A clip from <see cref="CreateClip"/>.</param>
    /// <param name="target">What the curve moves.</param>
    /// <param name="curve">How it moves.</param>
    /// <exception cref="BevyNativeException">The clip is gone, or this build has no renderer.</exception>
    public static void AddCurve(AssetHandle clip, AnimationTarget target, AnimationCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);

        var native = new NativeAnimationCurve
        {
            Property = (int)curve.Property,
            Kind = curve.Ease is null ? 0 : 1,
            Count = curve.Count,
            Duration = curve.Duration,
            PingPong = curve.IsPingPong ? 1u : 0u,
        };
        if (curve.Ease is { } ease)
            (native.Ease, native.EaseSteps, native.EaseJump, native.EaseOmega) = ((int)ease.Kind, ease.StepCount, (int)ease.Jump, ease.Omega);

        int status;
        fixed (float* times = curve.Times)
        fixed (float* values = curve.Values)
            status = Native.bcs_animation_clip_add_curve(clip.Key, target.High, target.Low, &native, times, values, curve.Values.Length);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"adding a {curve.Property} curve to clip {clip.Key}");
    }

    /// <summary>A graph holding the one clip, and the clip's node in it, as Bevy's <c>AnimationGraph::from_clip</c>.</summary>
    /// <param name="clip">A clip from <see cref="CreateClip"/>.</param>
    /// <exception cref="BevyNativeException">The clip is gone, or this build has no renderer.</exception>
    public static (AssetHandle Graph, uint Node) GraphFromClip(AssetHandle clip)
    {
        int graph;
        uint node;
        var status = Native.bcs_animation_graph_from_clip(clip.Key, &graph, &node);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"making a graph of clip {clip.Key}");
        return (new AssetHandle(graph), node);
    }

    /// <summary>Makes an entity a player of a graph, playing one of its nodes.</summary>
    /// <remarks>
    /// Bevy's <c>AnimationPlayer</c> and <c>AnimationGraphHandle</c> on the entity, the node played
    /// once and held at its end, or over and over where <paramref name="repeat"/> says so. The
    /// player moves the entities that name it through <see cref="Animate"/>, itself among them
    /// where it is one.
    /// </remarks>
    /// <param name="player">The entity that plays it.</param>
    /// <param name="graph">A graph from <see cref="GraphFromClip"/>.</param>
    /// <param name="node">The node to play.</param>
    /// <param name="repeat">Whether it plays over and over.</param>
    /// <exception cref="BevyNativeException">The entity or the graph is gone, or this build has no renderer.</exception>
    public static void PlayGraph(Entity player, AssetHandle graph, uint node, bool repeat = false) =>
        Act(Native.bcs_animation_play_graph(player.Bits, graph.Key, node, repeat ? 1 : 0), "playing a graph as", player);

    /// <summary>Makes an entity the target a clip's curves are aimed at, moved by a player.</summary>
    /// <remarks>Bevy's <c>AnimationTargetId</c> and <c>AnimatedBy</c> on the entity.</remarks>
    /// <param name="entity">The entity the curves move.</param>
    /// <param name="target">The target the curves are aimed at.</param>
    /// <param name="player">The entity whose player moves it, which may be itself.</param>
    /// <exception cref="BevyNativeException">An entity is gone, or this build has no renderer.</exception>
    public static void Animate(Entity entity, AnimationTarget target, Entity player) =>
        Act(Native.bcs_animation_animate(entity.Bits, target.High, target.Low, player.Bits), "aiming curves at", entity);

    /// <summary>Starts loading one of a model file's clips by its label, as <c>models/Fox.glb#Animation2</c>.</summary>
    /// <remarks>
    /// Bevy's <c>GltfAssetLabel::Animation</c>, for a clip played from a graph of its own rather than
    /// by name on the scene it came with, as one with events placed on it is. It arrives a few
    /// frames later, which <see cref="AddEvent{TEvent}(AssetHandle, float, TEvent)"/> answers false
    /// until it has.
    /// </remarks>
    /// <param name="path">The file and the clip's label, as an asset path.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle LoadClip(string path) => AssetServer.Load("AnimationClip", path);

    /// <summary>Sets how long a clip lasts, which one holding only events needs.</summary>
    /// <param name="clip">A clip from <see cref="CreateClip"/>.</param>
    /// <param name="seconds">Its length.</param>
    /// <exception cref="BevyNativeException">The clip is gone, or this build has no renderer.</exception>
    public static void SetClipDuration(AssetHandle clip, float seconds)
    {
        var status = Native.bcs_animation_clip_set_duration(clip.Key, seconds);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"setting the length of clip {clip.Key}");
    }

    /// <summary>Places an event on a clip, triggered at its player as the clip reaches the time.</summary>
    /// <remarks>Bevy's <c>add_event</c>. See <see cref="IAnimationEvent"/>.</remarks>
    /// <param name="clip">The clip, made in code or loaded.</param>
    /// <param name="time">When, in seconds into the clip.</param>
    /// <param name="value">The event, triggered as it is given here each time the clip reaches it.</param>
    /// <returns>False while a loaded clip has not arrived, for the event to be placed again later.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static bool AddEvent<TEvent>(AssetHandle clip, float time, TEvent value) where TEvent : IAnimationEvent =>
        PlaceEvent(clip, time, value, null);

    /// <summary>Places an event on a clip, triggered at the entity a target names as the clip reaches the time.</summary>
    /// <remarks>Bevy's <c>add_event_to_target</c>, as a step lands at the foot that takes it.</remarks>
    /// <param name="clip">The clip, made in code or loaded.</param>
    /// <param name="target">Where it happens.</param>
    /// <param name="time">When, in seconds into the clip.</param>
    /// <param name="value">The event, triggered as it is given here each time the clip reaches it.</param>
    /// <returns>False while a loaded clip has not arrived, for the event to be placed again later.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static bool AddEvent<TEvent>(AssetHandle clip, AnimationTarget target, float time, TEvent value) where TEvent : IAnimationEvent =>
        PlaceEvent(clip, time, value, target);

    private static bool PlaceEvent<TEvent>(AssetHandle clip, float time, TEvent value, AnimationTarget? target) where TEvent : IAnimationEvent
    {
        var at = target ?? default;
        var status = Native.bcs_animation_clip_add_event(clip.Key, time, ClipEvents.Keep(value), target is null ? 0 : 1, at.High, at.Low);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        if (status == NativeStatus.NotPresent) return false;
        Native.Check(status, $"placing a {typeof(TEvent).Name} on clip {clip.Key}");
        return true;
    }

    /// <summary>Makes an empty graph, its root a blend, for <see cref="AddBlend"/> and <see cref="AddClip"/> to fill.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>AnimationGraph</c> built node by node, as its animation_graph example builds one.
    /// Clips sit at the leaves and blends above them, each at a weight, and a player playing
    /// several of its clips at once mixes them by their weights and their blends':
    /// </para>
    /// <code>
    /// var (graph, root) = Animation.CreateGraph();
    /// var blend = Animation.AddBlend(graph, 0.5f, root);
    /// var idle = Animation.AddClip(graph, idleClip, 1f, root);
    /// var walk = Animation.AddClip(graph, walkClip, 1f, blend);
    /// Animation.SetGraph(fox, graph);
    /// Animation.PlayNode(fox, idle, repeat: true);
    /// Animation.PlayNode(fox, walk, repeat: true);
    /// Animation.SetNodeWeight(fox, walk, 0.3f);
    /// </code>
    /// <para>
    /// A clip's node can leave parts of a body out, the targets of each part put into a mask group
    /// with <see cref="AddToMaskGroup"/> and the groups a node leaves out its mask's bits.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static (AssetHandle Graph, uint Root) CreateGraph()
    {
        int graph;
        uint root;
        var status = Native.bcs_animation_graph_create(&graph, &root);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, "making an animation graph");
        return (new AssetHandle(graph), root);
    }

    /// <summary>Adds a blend to a graph under a node, answering its node.</summary>
    /// <remarks>
    /// An additive blend, Bevy's <c>add_additive_blend</c>, adds what is under it on top of the
    /// first of its children rather than mixing them, as clips masked to parts of a body are laid
    /// over one playing on the whole.
    /// </remarks>
    /// <param name="graph">A graph from <see cref="CreateGraph"/>.</param>
    /// <param name="weight">The weight what is under it is mixed in at.</param>
    /// <param name="parent">The node it sits under.</param>
    /// <param name="additive">Whether it adds rather than mixes.</param>
    /// <exception cref="BevyNativeException">The graph is gone, or this build has no renderer.</exception>
    public static uint AddBlend(AssetHandle graph, float weight, uint parent, bool additive = false)
    {
        uint node;
        var status = Native.bcs_animation_graph_add_blend(graph.Key, weight, parent, additive ? 1 : 0, &node);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"adding a blend to graph {graph.Key}");
        return node;
    }

    /// <summary>Adds a clip to a graph under a node, answering its node.</summary>
    /// <param name="graph">A graph from <see cref="CreateGraph"/>.</param>
    /// <param name="clip">The clip, made in code or loaded.</param>
    /// <param name="weight">The weight it is mixed in at.</param>
    /// <param name="parent">The node it sits under.</param>
    /// <param name="mask">The mask groups it leaves out, a bit for each, zero for none.</param>
    /// <exception cref="BevyNativeException">The graph or the clip is gone, or this build has no renderer.</exception>
    public static uint AddClip(AssetHandle graph, AssetHandle clip, float weight, uint parent, ulong mask = 0)
    {
        uint node;
        var status = Native.bcs_animation_graph_add_clip(graph.Key, clip.Key, mask, weight, parent, &node);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"adding clip {clip.Key} to graph {graph.Key}");
        return node;
    }

    /// <summary>Puts a target into one of a graph's mask groups, numbered from zero.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="target">The entity's target, a bone as often as not.</param>
    /// <param name="group">The group, which a node's mask leaves out with the bit at its number.</param>
    /// <exception cref="BevyNativeException">The graph is gone, or this build has no renderer.</exception>
    public static void AddToMaskGroup(AssetHandle graph, AnimationTarget target, uint group)
    {
        var status = Native.bcs_animation_graph_add_to_mask_group(graph.Key, target.High, target.Low, group);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"masking a target in graph {graph.Key}");
    }

    /// <summary>Sets which mask groups a graph's node leaves out, which takes as it plays.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="node">The node.</param>
    /// <param name="mask">The groups left out, a bit for each.</param>
    /// <exception cref="BevyNativeException">The graph or the node is gone, or this build has no renderer.</exception>
    public static void SetNodeMask(AssetHandle graph, uint node, ulong mask)
    {
        var status = Native.bcs_animation_graph_set_mask(graph.Key, node, mask);
        if (status == NativeStatus.Unsupported) throw NoRenderer();
        Native.Check(status, $"masking node {node} of graph {graph.Key}");
    }

    /// <summary>The target an entity is aimed at by, or null for one no clip aims at.</summary>
    /// <remarks>
    /// A model's bones each carry the target its clips aim at them by, as a game's own entity does
    /// through <see cref="Animate"/>.
    /// </remarks>
    /// <param name="entity">The entity.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static AnimationTarget? TargetOf(Entity entity)
    {
        ulong high, low;
        var status = Native.bcs_animation_target_of(entity.Bits, &high, &low);
        if (status == NativeStatus.NotPresent) return null;
        Act(status, "reading the target of", entity);
        return new AnimationTarget(high, low);
    }

    /// <summary>Gives an entity a graph to play from, and a player where it has none.</summary>
    /// <param name="player">The entity, often a player a model's scene brought.</param>
    /// <param name="graph">The graph.</param>
    /// <exception cref="BevyNativeException">The entity or the graph is gone, or this build has no renderer.</exception>
    public static void SetGraph(Entity player, AssetHandle graph) =>
        Act(Native.bcs_animation_set_graph(player.Bits, graph.Key), "giving a graph to", player);

    /// <summary>Starts a node of an entity's graph playing beside whatever it plays already.</summary>
    /// <param name="player">The entity with the graph.</param>
    /// <param name="node">The node.</param>
    /// <param name="repeat">Whether it plays over and over.</param>
    /// <exception cref="BevyNativeException">The entity is gone or plays no graph, or this build has no renderer.</exception>
    public static void PlayNode(Entity player, uint node, bool repeat = false) =>
        Act(Native.bcs_animation_play_node(player.Bits, node, repeat ? 1 : 0), "playing a node as", player);

    /// <summary>Sets the weight a playing node is mixed in at.</summary>
    /// <param name="player">The entity playing it.</param>
    /// <param name="node">The node.</param>
    /// <param name="weight">Its weight, one as it was made and zero gone.</param>
    /// <returns>False where the node is not playing, or the entity has no player.</returns>
    /// <exception cref="BevyNativeException">The entity is gone or plays no graph, or this build has no renderer.</exception>
    public static bool SetNodeWeight(Entity player, uint node, float weight)
    {
        var status = Native.bcs_animation_node_weight(player.Bits, node, weight);
        if (status == NativeStatus.NotPresent) return false;
        Act(status, "weighting a node of", player);
        return true;
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

using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for animation, clips with their curves and events, graphs, and the
/// players that play them on their targets.
/// </summary>
internal static unsafe partial class NativeAnimation
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeAnimation() => NativeLoader.Initialize();

    /// <summary>The names of a scene's clips, a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clips(ulong root, byte* buffer, int capacity);

    /// <summary>Plays a scene's clip by its number.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play(ulong root, int clip, int repeat, float speed, float blend);

    /// <summary>Stops every clip on a scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_stop(ulong root);

    /// <summary>Makes an empty clip, answering its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_create();

    /// <summary>Writes the target at the end of a path of names, given one a line.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_target(string names, ulong* high, ulong* low);

    /// <summary>Adds a described curve to a clip, aimed at a target.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_add_curve(int clip, ulong high, ulong low, NativeAnimationCurve* curve, float* times, float* values, int valueCount);

    /// <summary>
    /// Adds a described curve moving a field of a reflected component to a clip, aimed at a target,
    /// leaving the reason for a failure for <see cref="NativeCore.bcs_reflect_error"/>.
    /// </summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_add_field_curve(
        int clip, ulong high, ulong low, NativeAnimationCurve* curve, float* times, float* values, int valueCount, string typePath, string path, int kind);

    /// <summary>Makes a graph of one clip, writing its key and the clip's node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_from_clip(int clip, int* graph, uint* node);

    /// <summary>Makes an entity a player of a graph, playing a node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play_graph(ulong entity, int graph, uint node, int repeat);

    /// <summary>Makes an entity a target of a clip's curves, moved by a player.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_animate(ulong entity, ulong high, ulong low, ulong player);

    /// <summary>Sets how long a clip lasts.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_set_duration(int clip, float seconds);

    /// <summary>Places an event on a clip, at its player or at a target.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_add_event(int clip, float time, uint number, int targeted, ulong high, ulong low);

    /// <summary>Makes an empty graph, writing its key and its root's node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_create(int* graph, uint* root);

    /// <summary>Adds a blend to a graph, writing its node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_blend(int graph, float weight, uint parent, int additive, uint* node);

    /// <summary>Writes the target an entity is aimed at by, where it has one.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_target_of(ulong entity, ulong* high, ulong* low);

    /// <summary>Adds a clip to a graph, with the mask groups it leaves out, writing its node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_clip(int graph, int clip, ulong mask, float weight, uint parent, uint* node);

    /// <summary>Puts a target into one of a graph's mask groups.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_to_mask_group(int graph, ulong high, ulong low, uint group);

    /// <summary>Sets the mask groups a graph's node leaves out.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_set_mask(int graph, uint node, ulong mask);

    /// <summary>Gives an entity a graph to play from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_set_graph(ulong entity, int graph);

    /// <summary>Starts a node of an entity's graph playing beside the others.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play_node(ulong entity, uint node, int repeat);

    /// <summary>Sets the weight a playing node is mixed in at.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_node_weight(ulong entity, uint node, float weight);

    /// <summary>Changes how many times the playing clip plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_set_repeat(ulong root, uint times);

    /// <summary>Holds or lets go of every clip on a scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_pause(ulong root, int paused);

    /// <summary>Moves or changes the speed of the clip a scene plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_adjust(ulong root, float seconds, float speed);

    /// <summary>What a scene plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_state(ulong root, NativeAnimationState* state);

    /// <summary>Takes the clips that reached their end.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_finished(ulong* roots, int* clips, int capacity);
}

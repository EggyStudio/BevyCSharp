using Bevy.Interop;

namespace Bevy;

/// <summary>What a clip's curve is aimed at, an entity by the names on the path down to it.</summary>
/// <param name="High">The first half of Bevy's identifier.</param>
/// <param name="Low">The second half.</param>
/// <remarks>
/// <para>
/// Bevy's <c>AnimationTargetId</c>, made from names rather than from an entity, so a clip made once
/// moves every set of entities named the same way, as a glTF clip moves every copy of its model:
/// </para>
/// <code>
/// var satellite = AnimationTarget.FromNames("planet", "orbit_controller", "satellite");
/// </code>
/// <para>
/// The entity it moves carries it, with the player that moves it, through
/// <see cref="Animation.Animate"/>.
/// </para>
/// </remarks>
public readonly record struct AnimationTarget(ulong High, ulong Low)
{
    /// <summary>The target at the end of a path of names, the first the topmost.</summary>
    /// <param name="names">Each entity's name on the way down, the last the target's own.</param>
    /// <exception cref="ArgumentException">There are no names, or one holds a line break.</exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static unsafe AnimationTarget FromNames(params ReadOnlySpan<string> names)
    {
        if (names.Length == 0) throw new ArgumentException("A target is named by at least one name.", nameof(names));
        foreach (var name in names)
        {
            ArgumentNullException.ThrowIfNull(name);
            if (name.Contains('\n')) throw new ArgumentException($"A name holds no line break, as \"{name}\" does.", nameof(names));
        }

        ulong high, low;
        var status = Native.bcs_animation_target(string.Join('\n', names.ToArray()), &high, &low);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Naming an animation target");
        Native.Check(status, "naming an animation target");
        return new AnimationTarget(high, low);
    }
}

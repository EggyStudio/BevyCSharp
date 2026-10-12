namespace Bevy;

/// <summary>
/// Skips the system for a frame unless a static <see cref="bool"/> member says otherwise.
/// </summary>
/// <remarks>
/// The member must be declared on the same behavior struct and be a static bool field,
/// property, or a method taking a <see cref="World"/>. Use <c>nameof</c> so a rename cannot
/// silently break it.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// [RunIf(nameof(IsPlaying))]
/// public static void Tick(BehaviorContext ctx) { }
///
/// public static bool IsPlaying(World world) =&gt;
///     world.TryGetResource&lt;GameState&gt;(out var state) &amp;&amp; state.Playing;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RunIfAttribute : Attribute
{
    /// <summary>Name of the static bool member on the same struct.</summary>
    public string MemberName { get; }

    /// <summary>References a condition member by name.</summary>
    public RunIfAttribute(string memberName) => MemberName = memberName;
}

namespace Bevy;

/// <summary>An event a game places on an animation clip, triggered as the clip reaches it.</summary>
/// <remarks>
/// <para>
/// Bevy's <c>AnimationEvent</c>, which a type derives in Rust and implements here. It is placed on
/// a clip at a time with <see cref="Animation.AddEvent{TEvent}(AssetHandle, float, TEvent)"/>, and
/// as the clip passes that time it is triggered at the player, or at the entity a target names
/// where it was placed for one, where observers hear it as any of a game's events:
/// </para>
/// <code>
/// record struct Step : IAnimationEvent;
///
/// Animation.AddEvent(run, leftFoot, 0.625f, new Step());
/// ecs.Observe&lt;Step&gt;(on => Dust(on.Ecs, on.Entity));
/// </code>
/// <para>
/// <c>on.Entity</c> is where it happened, the player or the target's entity. The first observer of
/// such an event asks Bevy to report its clips' events, and a clip's events are the values given
/// to <c>AddEvent</c>, each triggered as it was given.
/// </para>
/// </remarks>
public interface IAnimationEvent;

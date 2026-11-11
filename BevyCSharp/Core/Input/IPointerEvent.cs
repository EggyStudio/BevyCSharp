namespace Bevy;

/// <summary>
/// What a pointer can do to an entity, one of the events <see cref="Pointer{TEvent}"/> carries.
/// </summary>
/// <remarks>
/// Implemented by Bevy's seventeen alone, <see cref="Over"/> to <see cref="Cancel"/>, since each is
/// reported by Bevy's picking and a kind of its own would never be.
/// </remarks>
public interface IPointerEvent;

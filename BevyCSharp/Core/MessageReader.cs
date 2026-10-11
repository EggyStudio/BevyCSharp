namespace Bevy;

/// <summary>
/// Reads the messages of type <typeparamref name="T"/> it has not read yet, as Bevy's
/// <c>MessageReader</c> does, the last frame's and this frame's sent so far. Only valid inside a
/// system, on the main thread.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
/// <remarks>
/// <para>
/// A reader keeps its own place, so one is made for the system that reads with it and kept there,
/// in a field or a local the system captures, and two readers each read the same messages once. A
/// system ordered after the one sending reads them in the same frame, and one ordered before it in
/// the next, as in Bevy.
/// </para>
/// <para>
/// <see cref="BehaviorContext.Read{TMessage}"/> reads the last frame's messages whole, with no place
/// of its own, which suits a system that reads every frame and need not run after the sender.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var damage = new MessageReader&lt;DealDamage&gt;();
/// app.Update(ctx =>
/// {
///     foreach (var dealt in damage.Read(ctx)) Log.Info($"Applying {dealt.Amount} damage");
/// }, "ApplyDamage");
/// </code>
/// </example>
public sealed class MessageReader<T> where T : notnull
{
    private long _cursor;

    /// <summary>The messages this reader has not read yet, oldest first, and the reader moved past them.</summary>
    public T[] Read(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return ctx.Messages.ReadPast<T>(ref _cursor);
    }
}

// Bevy's message example, examples/ecs/message.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to send, change and read messages, and how to order systems so that messages are dealt
// with in a set order, by a damage over time effect such as a game has.
internal static class MessageExample
{
    // Sent when something tries to deal damage to another entity.
    internal record struct DealDamage(int Amount);

    // Sent when an entity takes damage.
    internal readonly record struct DamageReceived;

    // Sent when an entity's armor blocks the damage.
    internal readonly record struct ArmorBlockedDamage;

    public static void Build(App app)
    {
        // Bevy's DamageTimer, once a second, again and again.
        var timer = GameTimer.FromSeconds(1f, TimerMode.Repeating);

        // Each system's own place among the messages, as Bevy's readers and mutators keep theirs.
        var extraDamage = new MessageReader<DealDamage>();
        var armor = new MessageMutator<DealDamage>();
        var health = new MessageReader<DealDamage>();
        var sound = new MessageReader<DamageReceived>();
        var particles = new MessageReader<DamageReceived>();

        // In order, so the armor changes the damage before it is applied, and each reads what the
        // one before it sent in the same frame.
        app.Chain(Stage.Update,
            new SystemDescriptor(world =>
            {
                // A message is sent as any other value is made.
                var ctx = new BehaviorContext(world);
                timer = timer.Tick(ctx.Time.Delta);
                if (timer.JustFinished) ctx.Send(new DealDamage(10));
            }, "message.DealDamageOverTime"),
            new SystemDescriptor(world =>
            {
                // Reads the damage dealt and sends more of it, which it reads itself the next time
                // it runs, since it sends after reading. Bevy's reads and sends one type through a
                // mutator, which Rust asks of it, and a C# system reads and sends with no such rule.
                var ctx = new BehaviorContext(world);
                var extra = extraDamage.Read(ctx).Where(dealt => dealt.Amount >= 10).Select(dealt => new DealDamage(dealt.Amount / 10)).ToList();
                foreach (var dealt in extra) ctx.Send(dealt);
            }, "message.ApplyExtraDamage"),
            new SystemDescriptor(world =>
            {
                // Takes the armor's share off each damage in place, and says when it took all of it.
                var ctx = new BehaviorContext(world);
                foreach (ref var dealt in armor.Read(ctx))
                {
                    dealt.Amount -= 1;
                    if (dealt.Amount <= 0) ctx.Send(new ArmorBlockedDamage());
                }
            }, "message.ApplyArmorToDamage"),
            new SystemDescriptor(world =>
            {
                // Reads the damage as the armor left it, and says the entity took it where any is left.
                var ctx = new BehaviorContext(world);
                foreach (var dealt in health.Read(ctx))
                {
                    Log.Info($"Applying {dealt.Amount} damage");
                    if (dealt.Amount > 0) ctx.Send(new DamageReceived());
                }
            }, "message.ApplyDamageToHealth"));

        // Two readers of the same messages, each reading every one. They are ordered neither against
        // each other nor after the chain, so either may read a frame late, as in Bevy.
        app.Update(ctx =>
        {
            foreach (var _ in sound.Read(ctx)) Log.Info("Playing a sound.");
        }, "message.PlayDamageReceivedSound");
        app.Update(ctx =>
        {
            foreach (var _ in particles.Read(ctx)) Log.Info("Playing particle effect.");
        }, "message.PlayDamageReceivedParticleEffect");
    }
}

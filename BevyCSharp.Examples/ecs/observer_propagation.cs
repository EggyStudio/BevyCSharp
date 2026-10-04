using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Propagates an event up the hierarchy through observers. A goblin wears three pieces of armor,
// each a child of it, and an attack on a piece goes on to the goblin only for the damage the armor
// did not block.
internal static class ObserverPropagation
{
    // An attack that goes on up from the armor it struck to the goblin wearing it, until a piece
    // blocks it, as Bevy's #[entity_event(propagate, auto_propagate)] does.
    internal record struct Attack(Entity Entity, int Damage) : IPropagatingEvent;

    // A name, by its place in Names, since a component here holds no string.
    internal struct Named
    {
        public int Name;
    }

    internal struct HitPoints
    {
        public int Value;
    }

    // For damage to reach the wearer, it must exceed the armor.
    internal struct Armor
    {
        public int Value;
    }

    private static readonly List<string> Names = [];

    // Seeded, so a capture plays the same fight each time, where Bevy's draws from the system.
    private static Random _random = new(19878367);

    public static void Build(App app)
    {
        Names.Clear();
        _random = new Random(19878367);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var goblin = Spawn(ecs, "Goblin");
            ecs.Add(goblin, new HitPoints { Value = 50 });
            ecs.Observe<Attack>(goblin, TakeDamage);

            foreach (var (name, armor) in new[] { ("Helmet", 5), ("Socks", 10), ("Shirt", 15) })
            {
                var piece = Spawn(ecs, name);
                ecs.Add(piece, new Armor { Value = armor });
                ecs.SetParent(piece, goblin);
                ecs.Observe<Attack>(piece, BlockAttack);
            }

            // A line whenever an attack hits an entity, as Bevy's attack_hits, which watches every
            // attack wherever it has reached.
            ecs.Observe<Attack>(on => Console.WriteLine($"Attack hit {NameOf(on.Ecs, on.Entity)}"));
        }, "observer_propagation.Setup");

        // Bevy's on_timer(200 ms), which passes once each time the interval comes round.
        var next = 0.2f;
        app.On(Stage.Update, AttackArmor, "observer_propagation.AttackArmor", world =>
        {
            if (world.Resource<Time>().Elapsed < next) return false;
            next += 0.2f;
            return true;
        });
    }

    private static void AttackArmor(BehaviorContext ctx)
    {
        var pieces = ctx.Ecs.EntitiesWith<Armor>().ToArray();
        if (pieces.Length == 0) return;

        var piece = pieces[_random.Next(pieces.Length)];
        var damage = _random.Next(1, 20);

        // Bevy queues the trigger and prints first, so the line comes before what the observers say.
        Console.WriteLine($"⚔️  Attack for {damage} damage");
        ctx.Ecs.Trigger(new Attack(piece, damage));
    }

    // Placed on each piece of armor, which lessens the attack by its rating or stops it.
    private static void BlockAttack(On<Attack> attack)
    {
        var armor = attack.Ecs.GetOrDefault<Armor>(attack.Entity).Value;
        var name = NameOf(attack.Ecs, attack.Entity);
        var damage = Math.Max(attack.Event.Damage - armor, 0);
        if (damage > 0)
        {
            Console.WriteLine($"🩸 {damage} damage passed through {name}");
            attack.Event.Damage = damage;
        }
        else
        {
            Console.WriteLine($"🛡️  {attack.Event.Damage} damage blocked by {name}");
            attack.Propagate(false);
            Console.WriteLine("(propagation halted early)\n");
        }
    }

    // Placed on the goblin, which an attack reaches when its armor did not block it.
    private static void TakeDamage(On<Attack> attack)
    {
        var ecs = attack.Ecs;
        var hp = Math.Max(ecs.GetOrDefault<HitPoints>(attack.Entity).Value - attack.Event.Damage, 0);
        ecs.Set(attack.Entity, new HitPoints { Value = hp });

        var name = NameOf(ecs, attack.Entity);
        if (hp > 0)
        {
            Console.WriteLine(FormattableString.Invariant($"{name} has {hp:0.0} HP"));
        }
        else
        {
            Console.WriteLine($"💀 {name} has died a gruesome death");
            ecs.Despawn(attack.Entity);
            attack.Context.Exit();
        }

        Console.WriteLine("(propagation reached root)\n");
    }

    private static Entity Spawn(EcsWorld ecs, string name)
    {
        Names.Add(name);
        var entity = ecs.Spawn();
        ecs.Add(entity, new Named { Name = Names.Count - 1 });
        return entity;
    }

    private static string NameOf(EcsWorld ecs, Entity entity) =>
        ecs.Has<Named>(entity) ? Names[ecs.GetOrDefault<Named>(entity).Name] : entity.ToString();
}

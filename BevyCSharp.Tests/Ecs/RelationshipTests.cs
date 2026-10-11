using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Relationships of a game's own between entities, as Bevy's relationships example makes, changes
/// and follows them.
/// </summary>
[Collection("engine")]
public sealed class RelationshipTests
{
    private readonly struct Follows;

    private readonly struct Guards;

    /// <summary>
    /// A target lists what relates to it in order, a relation moved or taken away leaves its list,
    /// two relationships keep apart, and following one reaches each target in turn.
    /// </summary>
    [Fact]
    public void ATargetListsWhatRelatesToItAsRelationsChange()
    {
        using var harness = new EngineHarness(frames: 1);
        Entity[] firstList = [], movedList = [], guards = [], afterUnrelate = [];
        Entity? unrelated = Entity.None;
        Entity[] chain = [];
        var (a, b, c) = (Entity.None, Entity.None, Entity.None);

        harness.OnContext(Stage.Update, ctx =>
        {
            var ecs = ctx.Ecs;
            (a, b, c) = (ecs.Spawn(), ecs.Spawn(), ecs.Spawn());

            ecs.Relate<Follows>(b, a);
            ecs.Relate<Follows>(c, a);
            ecs.Relate<Guards>(a, c);
            firstList = ecs.RelatedTo<Follows>(a);
            guards = ecs.RelatedTo<Guards>(c);

            ecs.Relate<Follows>(c, b);
            movedList = ecs.RelatedTo<Follows>(a);
            chain = [.. ecs.RelationAncestors<Follows>(c)];

            ecs.Unrelate<Follows>(b);
            unrelated = ecs.RelationOf<Follows>(b);
            afterUnrelate = ecs.RelatedTo<Follows>(a);
        });
        harness.Run();

        Assert.Equal([b, c], firstList);
        Assert.Equal([a], guards);
        Assert.Equal([b], movedList);
        Assert.Equal([b, a], chain);
        Assert.Null(unrelated);
        Assert.Empty(afterUnrelate);
    }

    /// <summary>
    /// Despawning a target leaves what related to it relating to nothing, and despawning what
    /// relates takes it off its target's list, as Bevy's hooks keep them.
    /// </summary>
    [Fact]
    public void DespawningEitherEndTakesTheRelationAway()
    {
        using var harness = new EngineHarness(frames: 3);
        var frame = 0;
        var (target, source, other) = (Entity.None, Entity.None, Entity.None);
        Entity? left = Entity.None;
        Entity[] listed = [];

        harness.OnContext(Stage.Update, ctx =>
        {
            var ecs = ctx.Ecs;
            switch (++frame)
            {
                case 1:
                    (target, source, other) = (ecs.Spawn(), ecs.Spawn(), ecs.Spawn());
                    ecs.Relate<Follows>(source, target);
                    ecs.Relate<Follows>(other, source);
                    ecs.Despawn(target);
                    ecs.Despawn(other);
                    break;
                case 2:
                    left = ecs.RelationOf<Follows>(source);
                    listed = ecs.RelatedTo<Follows>(source);
                    break;
            }
        });
        harness.Run();

        Assert.Null(left);
        Assert.Empty(listed);
    }
}

using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers copying an entity, and the editor's Duplicate built on it.
/// </summary>
/// <remarks>
/// Bevy clones only components that implement <c>Clone</c> or <c>Reflect</c>, which a C# component's
/// bytes do not, so these check first that a C# component is copied at all, and then that a copy
/// holding a stored list has a list of its own.
/// </remarks>
[Collection("engine")]
public sealed class CloneTests
{
    [Fact]
    public void ACloneCarriesBevysComponentsAndCSharpOnes()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, Transform.At(1f, 2f, 3f));
            ctx.Ecs.Add(source, new Sprung { Mass = 4f, Front = new Spring { Stiffness = 5f } });

            var route = new Route();
            route.Speeds.Add(1.5f);
            ctx.Ecs.Add(source, route);

            var copy = ctx.Ecs.Clone(source);

            Assert.NotEqual(source, copy);
            Assert.Equal(new Vec3(1f, 2f, 3f), ctx.Ecs.GetRef<Transform>(copy).Translation);
            Assert.Equal(4f, ctx.Ecs.GetOrDefault<Sprung>(copy).Mass);
            Assert.Equal(5f, ctx.Ecs.GetOrDefault<Sprung>(copy).Front.Stiffness);
            Assert.Equal(route.Speeds, ctx.Ecs.GetOrDefault<Route>(copy).Speeds);

            // A copy, not a second name for the same bytes.
            ctx.Ecs.GetRef<Sprung>(copy).Mass = 9f;
            Assert.Equal(4f, ctx.Ecs.GetOrDefault<Sprung>(source).Mass);

            Assert.Equal(Entity.None, ctx.Ecs.Clone(Entity.None));
        });

        harness.Run();
    }

    [Fact]
    public void ACloneHasStoredListsOfItsOwn()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = EcsStore.Live;

            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, default(Bag));
            ctx.Ecs.GetRef<Bag>(source).Names.Add("rope");
            ctx.Ecs.Add(source, default(Ledger));
            ctx.Ecs.GetRef<Ledger>(source).Counts.Set("apples", 3);

            var copy = ctx.Ecs.Clone(source);
            Assert.Equal(before + 4, EcsStore.Live);

            var copied = ctx.Ecs.GetOrDefault<Bag>(copy).Names;
            Assert.NotEqual(ctx.Ecs.GetOrDefault<Bag>(source).Names, copied);
            Assert.Equal(["rope"], copied.Items);

            // The original going frees its own lists and leaves the copy's, which a shared handle
            // would have freed for both.
            ctx.Ecs.Despawn(source);
            Assert.Equal(["rope"], ctx.Ecs.GetOrDefault<Bag>(copy).Names.Items);
            Assert.Equal(3, ctx.Ecs.GetOrDefault<Ledger>(copy).Counts["apples"]);

            ctx.Ecs.Despawn(copy);
            Assert.Equal(before, EcsStore.Live);
        });

        harness.Run();
    }

    [Fact]
    public void DuplicateNamesTheCopyAndIsUndoneByDespawningIt()
    {
        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        // Once, because the names count on from the copies a second frame would already find.
        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            EditorHistory.Clear();

            var source = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(source, "Crate");
            ctx.Ecs.Add(source, Transform.At(0f, 1f, 0f));

            var made = EditorEntity.Duplicate(ctx.Ecs, [source]);
            var copy = Assert.Single(made);
            Assert.Equal("Crate 2", ctx.Ecs.NameOf(copy));
            Assert.Equal(copy, EditorSelection.Current);

            // A copy of the copy counts on from the number it already has.
            var third = Assert.Single(EditorEntity.Duplicate(ctx.Ecs, [copy]));
            Assert.Equal("Crate 3", ctx.Ecs.NameOf(third));

            Assert.Equal("duplicate", EditorHistory.Undo(ctx.Ecs));
            Assert.False(ctx.Ecs.IsAlive(third));

            // Made again from the original, as a new entity, under the same name.
            Assert.Equal("duplicate", EditorHistory.Redo(ctx.Ecs));
            Assert.Contains(
                ctx.Ecs.All(),
                entity => ctx.Ecs.NameOf(entity) == "Crate 3" && ctx.Ecs.IsAlive(entity));

            EditorSelection.Clear();
            EditorHistory.Clear();
        });

        harness.Run();
    }
}

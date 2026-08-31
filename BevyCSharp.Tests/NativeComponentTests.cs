using System.Runtime.CompilerServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reaching Bevy's own components from C#, and the hierarchy built on them.
/// </summary>
/// <remarks>
/// These run against a real Bevy world, so the layout assertions compare against the engine
/// rather than against another copy of the same assumption.
/// </remarks>
[Collection("engine")]
public sealed class NativeComponentTests
{
    [Fact]
    public unsafe void MirroredLayoutsMatchTheEngine()
    {
        // The whole reason NativeComponents verifies layouts. Quat is SIMD-backed and sixteen
        // byte aligned on most targets, which pads Transform to 48 rather than the 40 its three
        // fields suggest. Getting this wrong would not throw; it would silently read the wrong
        // bytes and corrupt neighbours on write.
        Assert.Equal(12, Unsafe.SizeOf<Vec3>());
        Assert.Equal(16, Unsafe.SizeOf<Quat>());
        Assert.Equal(48, Unsafe.SizeOf<Transform>());

        // Size is not enough. Bevy's Transform uses Rust's default representation, so the
        // compiler reorders its fields to save padding, and the reordered layout is the same
        // total size as the source order. Only the offsets tell them apart, and getting them
        // wrong renders as stretched geometry rather than as any kind of error.
        var probe = default(Transform);
        var origin = (nint)Unsafe.AsPointer(ref probe);
        Assert.Equal(0, (int)((nint)Unsafe.AsPointer(ref probe.Rotation) - origin));
        Assert.Equal(16, (int)((nint)Unsafe.AsPointer(ref probe.Translation) - origin));
        Assert.Equal(28, (int)((nint)Unsafe.AsPointer(ref probe.Scale) - origin));

        using var harness = new EngineHarness(frames: 2);

        // Resolving the id runs the check against the engine's own layout and throws on a
        // mismatch, so simply getting an id back is the assertion.
        harness.OnContext(Stage.Startup, _ => Assert.True(NativeComponents.Transform >= 0));
        harness.Run();
    }

    [Fact]
    public void TransformRoundTripsThroughBevy()
    {
        using var harness = new EngineHarness(frames: 3);
        var read = Transform.Identity;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Transform(
                new Vec3(1f, 2f, 3f), Quat.FromRotationY(0.5f), new Vec3(2f)));

            Assert.True(ctx.Ecs.TryGet<Transform>(entity, out read));
        });

        harness.Run();

        Assert.Equal(new Vec3(1f, 2f, 3f), read.Translation);
        Assert.Equal(new Vec3(2f), read.Scale);
        Assert.Equal(Quat.FromRotationY(0.5f), read.Rotation);
    }

    [Fact]
    public void WritingThroughAReferenceUpdatesTheRealComponent()
    {
        using var harness = new EngineHarness(frames: 3);
        var final = Vec3.Zero;
        var entity = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.Identity);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            ref var transform = ref ctx.Ecs.GetRef<Transform>(entity);
            transform.Translation.X += 1f;
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            if (ctx.Ecs.TryGet<Transform>(entity, out var t))
                final = t.Translation;
        });

        harness.Run();
        Assert.Equal(3f, final.X);
    }

    [Fact]
    public void TransformsCanBeIteratedLikeAnyOtherComponent()
    {
        // The point of resolving native components to ids: everything downstream already works
        // on ids, so chunked iteration needs no special case.
        using var harness = new EngineHarness(frames: 3);
        var total = 0f;
        var count = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 16; i++)
                ctx.Ecs.Add(ctx.Ecs.Spawn(), Transform.At(i, 0f, 0f));
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            using var chunks = ctx.Ecs.Chunks<Transform>(NativeComponents.Transform);

            total = 0f;
            count = 0;
            for (var c = 0; c < chunks.Count; c++)
            {
                var transforms = chunks[c].Components<Transform>();
                for (var i = 0; i < transforms.Length; i++)
                {
                    total += transforms[i].Translation.X;
                    count++;
                }
            }
        });

        harness.Run();

        Assert.Equal(16, count);
        Assert.Equal(120f, total);   // 0 + 1 + ... + 15
    }

    [Fact]
    public void ParentingIsVisibleFromBothDirections()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            Assert.Equal(Entity.None, ctx.Ecs.ParentOf(first));
            Assert.Empty(ctx.Ecs.ChildrenOf(parent));

            ctx.Ecs.SetParent(first, parent);
            ctx.Ecs.SetParent(second, parent);

            // Going through Bevy's relationship API rather than writing the component means the
            // reverse list is maintained for free. Writing the bytes would give a hierarchy that
            // only worked in one direction.
            Assert.Equal(parent, ctx.Ecs.ParentOf(first));
            Assert.Equal(parent, ctx.Ecs.ParentOf(second));

            var children = ctx.Ecs.ChildrenOf(parent);
            Assert.Equal(2, children.Length);
            Assert.Contains(first, children);
            Assert.Contains(second, children);
        });

        harness.Run();
    }

    [Fact]
    public void ReparentingMovesTheChildRatherThanDuplicatingIt()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();
            var child = ctx.Ecs.Spawn();

            ctx.Ecs.SetParent(child, first);
            ctx.Ecs.SetParent(child, second);

            Assert.Equal(second, ctx.Ecs.ParentOf(child));
            Assert.Empty(ctx.Ecs.ChildrenOf(first));
            Assert.Equal([child], ctx.Ecs.ChildrenOf(second));

            ctx.Ecs.ClearParent(child);
            Assert.Equal(Entity.None, ctx.Ecs.ParentOf(child));
            Assert.Empty(ctx.Ecs.ChildrenOf(second));
        });

        harness.Run();
    }

    [Fact]
    public void ParentTransformsPropagateToChildren()
    {
        // This is what makes hierarchy worth having, and it only works because the headless app
        // installs TransformPlugin. MinimalPlugins leaves it out, which would make Transform
        // inert data and GlobalTransform never follow anything.
        using var harness = new EngineHarness(frames: 4);
        var parentHasGlobal = false;
        var childHasGlobal = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            ctx.Ecs.Add(parent, Transform.At(10f, 0f, 0f));

            var child = ctx.Ecs.Spawn();
            ctx.Ecs.Add(child, Transform.At(1f, 0f, 0f));
            ctx.Ecs.SetParent(child, parent);
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            // Only that propagation ran and the relationship survived; what the matrix holds is
            // checked in GlobalTransformReadsAChildsWorldPosition.
            parentHasGlobal = ctx.Ecs.Count<GlobalTransform>() >= 2;
            childHasGlobal = ctx.Ecs.Count<ChildOf>() == 1;
        });

        harness.Run();

        Assert.True(parentHasGlobal, "TransformPlugin did not add GlobalTransform");
        Assert.True(childHasGlobal, "the child lost its ChildOf relationship");
    }

    [Fact]
    public void MirrorTypesResolveToTheEnginesOwnComponent()
    {
        // The whole point of INativeComponent. Left to its layout, Transform would register a
        // second component that merely shares the name, and writes through it would reach
        // nothing Bevy's own systems read.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            Assert.Equal(NativeComponents.Transform, EcsWorld.ComponentId<Transform>());
            Assert.Equal(NativeComponents.ChildOf, EcsWorld.ComponentId<ChildOf>());

            // And the id is the engine's, not one registered from a C# layout: a spawn carrying
            // the mirror comes back through the id NativeComponents resolved by name.
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(4f, 0f, 0f));
            Assert.True(ctx.Ecs.HasById(entity, NativeComponents.Transform));
        });

        harness.Run();
    }

    [Fact]
    public void NameOnlyHandlesFilterButRefuseTheirBytes()
    {
        // ChildOf and Children have no C# mirror: an empty struct naming one is a single byte,
        // so an insert through it would write nonsense over a live component. Everything that
        // only needs the id still works.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            var child = ctx.Ecs.Spawn();
            ctx.Ecs.SetParent(child, parent);

            Assert.True(ctx.Ecs.Has<ChildOf>(child));
            Assert.False(ctx.Ecs.Has<ChildOf>(parent));
            Assert.Equal([child], ctx.Ecs.EntitiesWith<ChildOf>());
            Assert.Equal(1, ctx.Ecs.Count<Children>());

            var write = Assert.Throws<BevyNativeException>(
                () => ctx.Ecs.Add(child, default(ChildOf)));
            Assert.Equal(NativeStatus.Unsupported, write.Status);

            var read = Assert.Throws<BevyNativeException>(
                () => ctx.Ecs.TryGet<Children>(parent, out _));
            Assert.Equal(NativeStatus.Unsupported, read.Status);
        });

        harness.Run();
    }

    [Fact]
    public unsafe void GlobalTransformMirrorsTheEnginesAffine()
    {
        // Four sixteen-byte-aligned Vec3As, each using three floats of the four it occupies.
        // A mirror packing them tightly is 48 bytes and reads three of the four columns from the
        // wrong place; the size check catches that one, but not a mirror padded differently.
        Assert.Equal(64, Unsafe.SizeOf<GlobalTransform>());

        var probe = default(GlobalTransform);
        var origin = (nint)Unsafe.AsPointer(ref probe);
        Assert.Equal(0, (int)((nint)Unsafe.AsPointer(ref probe.XAxis) - origin));
        Assert.Equal(16, (int)((nint)Unsafe.AsPointer(ref probe.YAxis) - origin));
        Assert.Equal(32, (int)((nint)Unsafe.AsPointer(ref probe.ZAxis) - origin));
        Assert.Equal(48, (int)((nint)Unsafe.AsPointer(ref probe.Translation) - origin));

        using var harness = new EngineHarness(frames: 2);

        // Resolving the id compares all five numbers against the engine, so getting an id back
        // is the assertion.
        harness.OnContext(Stage.Startup, _ => Assert.True(NativeComponents.GlobalTransform >= 0));
        harness.Run();
    }

    [Fact]
    public void GlobalTransformReadsAChildsWorldPosition()
    {
        // The gap this closes: a child's Transform is relative to its parent, so it cannot answer
        // "where is this actually" on its own. Propagation runs in PostUpdate, so Last is the
        // first stage that can read the result.
        using var harness = new EngineHarness(frames: 4);
        var childLocal = Vec3.Zero;
        var childWorld = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            ctx.Ecs.Add(parent, Transform.At(10f, 0f, 0f));

            var child = ctx.Ecs.Spawn();
            ctx.Ecs.Add(child, Transform.At(1f, 2f, 0f));
            ctx.Ecs.SetParent(child, parent);
            ctx.Ecs.Add(child, new Marker());
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var row in ctx.Ecs.Query<Marker>(markChanged: false))
            {
                childLocal = ctx.Ecs.GetRef<Transform>(row.Entity).Translation;
                childWorld = ctx.Ecs.GetRef<GlobalTransform>(row.Entity).Translation;
            }
        });

        harness.Run();

        Assert.Equal(new Vec3(1f, 2f, 0f), childLocal);
        Assert.Equal(new Vec3(11f, 2f, 0f), childWorld);
    }

    [Fact]
    public void GlobalTransformDecomposesBackIntoATransform()
    {
        // The affine is the general form, so reading a rotation or a scale back out of it means
        // undoing the multiplication. This checks the decomposition against the values that went
        // in, for an unparented entity where the two must agree exactly.
        using var harness = new EngineHarness(frames: 4);
        var written = new Transform(new Vec3(3f, -1f, 2f), Quat.FromRotationY(0.75f), new Vec3(2f));
        var decomposed = Transform.Identity;
        var muzzle = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, written);
            ctx.Ecs.Add(entity, new Marker());
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var row in ctx.Ecs.Query<Marker>(markChanged: false))
            {
                ref var global = ref ctx.Ecs.GetRef<GlobalTransform>(row.Entity);
                decomposed = global.ToTransform();

                // A point one unit down the entity's local -Z, which is where its forward is.
                muzzle = global.TransformPoint(new Vec3(0f, 0f, -1f));
            }
        });

        harness.Run();

        Assert.Equal(written.Translation.X, decomposed.Translation.X, 4);
        Assert.Equal(written.Translation.Y, decomposed.Translation.Y, 4);
        Assert.Equal(written.Translation.Z, decomposed.Translation.Z, 4);

        Assert.Equal(2f, decomposed.Scale.X, 4);
        Assert.Equal(2f, decomposed.Scale.Y, 4);
        Assert.Equal(2f, decomposed.Scale.Z, 4);

        // Quaternions double-cover rotations, so q and -q are the same rotation. Compare the
        // angle between them rather than the components.
        var dot = written.Rotation.X * decomposed.Rotation.X
            + written.Rotation.Y * decomposed.Rotation.Y
            + written.Rotation.Z * decomposed.Rotation.Z
            + written.Rotation.W * decomposed.Rotation.W;
        Assert.Equal(1f, MathF.Abs(dot), 4);

        // Scaled by two and turned 0.75 radians about Y, so the local -Z sits two units along the
        // world forward the rotation produced, offset by the translation.
        var forward = new Vec3(-MathF.Sin(0.75f), 0f, -MathF.Cos(0.75f));
        Assert.Equal(3f + forward.X * 2f, muzzle.X, 3);
        Assert.Equal(-1f, muzzle.Y, 3);
        Assert.Equal(2f + forward.Z * 2f, muzzle.Z, 3);
    }

    [Fact]
    public void UnknownNativeComponentsFailWithAUsefulMessage()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => Native.Check(Native.bcs_component_id_of("NotARealComponent"), "lookup"));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
        });

        harness.Run();
    }

    /// <summary>Tags the one entity a test wants to read back.</summary>
    private struct Marker;
}

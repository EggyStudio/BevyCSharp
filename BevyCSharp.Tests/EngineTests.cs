using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Runs every engine test in one collection, since each owns a real Bevy app.</summary>
[CollectionDefinition("engine", DisableParallelization = true)]
public sealed class EngineCollection;

/// <summary>Covers the app lifecycle, resource world and frame-state mirroring.</summary>
[Collection("engine")]
public sealed class EngineTests
{
    [Fact]
    public void NativeBridgeMatchesTheExpectedAbi()
    {
        // If this fails, the managed and native halves are out of step and every other test's
        // failure would be a misleading downstream symptom.
        using var app = new App(Config.HeadlessFor(1));
        Assert.NotNull(app.Config);
    }

    [Fact]
    public void AppInsertsTheEngineResources()
    {
        using var app = new App(Config.HeadlessFor(1));

        Assert.True(app.World.ContainsResource<Time>());
        Assert.True(app.World.ContainsResource<Input>());
        Assert.True(app.World.ContainsResource<EcsWorld>());
        Assert.True(app.World.ContainsResource<EcsCommands>());
        Assert.True(app.World.ContainsResource<Config>());
    }

    [Fact]
    public void HeadlessRunStopsAfterTheRequestedFrames()
    {
        using var harness = new EngineHarness(frames: 7);
        var lastFrame = 0UL;

        harness.OnContext(Stage.Last, ctx => lastFrame = ctx.Time.FrameCount);
        harness.Run();

        // FrameCount is zero-based, so seven frames end on frame six.
        Assert.Equal(6UL, lastFrame);
    }

    [Fact]
    public void TimeAdvancesAndIsConsistentWithinAFrame()
    {
        using var harness = new EngineHarness(frames: 5);
        var elapsedPerFrame = new List<double>();
        var sawInconsistency = false;
        var firstStage = 0.0;

        harness.OnContext(Stage.First, ctx => firstStage = ctx.Time.ElapsedSeconds);

        harness.OnContext(Stage.Last, ctx =>
        {
            // Every stage in a frame must observe the same snapshot.
            if (Math.Abs(ctx.Time.ElapsedSeconds - firstStage) > double.Epsilon)
                sawInconsistency = true;

            elapsedPerFrame.Add(ctx.Time.ElapsedSeconds);
        });

        harness.Run();

        Assert.False(sawInconsistency, "Time changed partway through a frame.");
        Assert.Equal(5, elapsedPerFrame.Count);
        for (var i = 1; i < elapsedPerFrame.Count; i++)
            Assert.True(elapsedPerFrame[i] >= elapsedPerFrame[i - 1], "Elapsed time went backwards.");
    }

    [Fact]
    public void CommandsAreAppliedAfterPostUpdateNotDuringIt()
    {
        using var harness = new EngineHarness(frames: 3);
        var seenDuringPostUpdate = -1;
        var seenDuringLast = -1;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Time.FrameCount != 0) return;
            ctx.Cmd.SpawnBatch(5, (entity, ecs) => ecs.Add(entity, new Health { Value = 1 }));
        });

        harness.OnContext(Stage.PostUpdate, ctx =>
        {
            if (ctx.Time.FrameCount == 0) seenDuringPostUpdate = ctx.Ecs.Count<Health>();
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            if (ctx.Time.FrameCount == 0) seenDuringLast = ctx.Ecs.Count<Health>();
        });

        harness.Run();

        Assert.Equal(0, seenDuringPostUpdate);
        Assert.Equal(5, seenDuringLast);
    }

    [Fact]
    public void CleanupSystemsRunAfterTheLoopExits()
    {
        using var harness = new EngineHarness(frames: 3);
        var cleanupFrame = ulong.MaxValue;
        var lastFrame = 0UL;

        harness.OnContext(Stage.Last, ctx => lastFrame = ctx.Time.FrameCount);
        harness.OnContext(Stage.Cleanup, ctx => cleanupFrame = ctx.Time.FrameCount);

        harness.Run();

        Assert.Equal(2UL, lastFrame);
        Assert.NotEqual(ulong.MaxValue, cleanupFrame);
    }

    [Fact]
    public void CleanupSystemsCanStillReachTheWorld()
    {
        using var harness = new EngineHarness(frames: 2);
        var countAtCleanup = -1;

        harness.OnContext(Stage.Startup, ctx =>
        {
            for (var i = 0; i < 3; i++) ctx.Ecs.Add(ctx.Ecs.Spawn(), new Health());
        });

        harness.OnContext(Stage.Cleanup, ctx => countAtCleanup = ctx.Ecs.Count<Health>());

        harness.Run();
        Assert.Equal(3, countAtCleanup);
    }

    [Fact]
    public void RequestExitStopsTheLoopEarly()
    {
        using var harness = new EngineHarness(frames: 1000);
        var frames = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            frames++;
            if (ctx.Time.FrameCount >= 3) ctx.Exit();
        });

        harness.Run();

        Assert.True(frames < 20, $"expected an early exit, ran {frames} frames");
    }

    [Fact]
    public void RunningAnAppTwiceIsRejected()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.Run();

        Assert.Throws<InvalidOperationException>(() => app.Run());
    }

    [Fact]
    public void RegisteringASystemAfterRunIsRejected()
    {
        using var app = new App(Config.HeadlessFor(1));
        app.Run();

        Assert.Throws<InvalidOperationException>(
            () => app.AddSystem(Stage.Update, _ => { }));
    }

    [Fact]
    public void SystemExceptionsAreReportedRatherThanUnwindingIntoRust()
    {
        // The point of this test is that the process survives at all: an exception crossing the
        // FFI boundary would be undefined behavior, so the engine has to swallow it.
        using var app = new App(new Config
        {
            Headless = true,
            HeadlessFrames = 3,
            FailFastOnSystemException = false,
        });

        var ran = 0;
        app.AddSystem(Stage.Update, _ =>
        {
            ran++;
            throw new InvalidOperationException("deliberate");
        });

        var exitCode = app.Run();

        Assert.Equal(0, exitCode);
        Assert.True(ran >= 3, $"the loop should have kept going, ran {ran} times");
    }

    [Fact]
    public void HeadlessRunsAreNotTiedToTheMainThread()
    {
        // macOS requires the window event loop to own the main thread, but that constraint
        // belongs to windowing, not to the engine. A headless run has neither, so it must work
        // from any thread. Every other test in this suite depends on that, because the test
        // runner does not use the process main thread.
        Exception? failure = null;
        var frames = 0;

        var worker = new Thread(() =>
        {
            try
            {
                using var app = new App(Config.HeadlessFor(3));
                Assert.False(app.WillOpenWindow);

                app.AddSystem(Stage.Update, _ => frames++);
                app.Run();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        worker.Start();
        worker.Join();

        Assert.Null(failure);
        Assert.True(frames >= 3, $"expected the loop to run, saw {frames} frames");
    }

    [Fact]
    public void PluginDependenciesAreValidatedBeforeBuild()
    {
        using var app = new App(Config.HeadlessFor(1));

        var ex = Assert.Throws<PluginOrderException>(() => app.AddPlugin(new NeedsEnginePlugin()));
        Assert.Equal(nameof(EnginePlugin), ex.MissingDependency);
    }

    [Fact]
    public void AddingTheSamePluginTwiceIsANoOp()
    {
        using var app = new App(Config.HeadlessFor(1));

        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(new EnginePlugin());

        Assert.Equal(1, app.PluginCount);
    }

    [Fact]
    public void RemoveSystemsBySourceNeutersOnlyThatGeneration()
    {
        using var harness = new EngineHarness(frames: 3);
        var keptRuns = 0;
        var removedRuns = 0;

        harness.App.AddSystem(Stage.Update,
            new SystemDescriptor(_ => keptRuns++, "kept") { Source = "keep" });
        harness.App.AddSystem(Stage.Update,
            new SystemDescriptor(_ => removedRuns++, "removed") { Source = "drop" });

        Assert.Equal(1, harness.App.RemoveSystemsBySource("drop"));

        harness.Run();

        Assert.True(keptRuns >= 3);
        Assert.Equal(0, removedRuns);
    }

    /// <summary>A plugin that declares a dependency, for the ordering test.</summary>
    private sealed class NeedsEnginePlugin : IPlugin
    {
        public IReadOnlyCollection<Type> Dependencies => [typeof(EnginePlugin)];

        public void Build(App app)
        {
        }
    }
}

/// <summary>Covers the managed resource world in isolation.</summary>
public sealed class WorldTests
{
    [Fact]
    public void ResourcesRoundTripByType()
    {
        using var world = new World();

        world.InsertResource(new Health { Value = 5 });

        Assert.True(world.ContainsResource<Health>());
        Assert.Equal(5, world.Resource<Health>().Value);
        Assert.True(world.RemoveResource<Health>());
        Assert.False(world.ContainsResource<Health>());
    }

    [Fact]
    public void MissingResourceThrowsWithAnActionableMessage()
    {
        using var world = new World();

        var ex = Assert.Throws<InvalidOperationException>(() => world.Resource<Health>());
        Assert.Contains("InsertResource", ex.Message);
    }

    [Fact]
    public void GetOrInsertOnlyBuildsOnce()
    {
        using var world = new World();
        var built = 0;

        for (var i = 0; i < 3; i++)
            world.GetOrInsertResource(() =>
            {
                built++;
                return new SystemToggleRegistry();
            });

        Assert.Equal(1, built);
    }

    [Fact]
    public void DisposeDisposesResources()
    {
        var resource = new DisposableResource();

        using (var world = new World())
        {
            world.InsertResource(resource);
        }

        Assert.True(resource.Disposed);
    }

    private sealed class DisposableResource : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;
    }
}

/// <summary>Covers the pieces that do not need a running engine.</summary>
public sealed class UnitTests
{
    [Fact]
    public void KeyTableMatchesTheNativeBitsetWidth()
    {
        // Each bitset word carries 64 keys; the native struct sizes itself from KEY_COUNT.
        var wordsNeeded = (KeyTable.Count + 63) / 64;
        Assert.Equal(NativeInput.KeyWords, wordsNeeded);
    }

    [Fact]
    public void KeyAliasesPointAtTheSameBits()
    {
        Assert.Equal(Key.ControlLeft, Key.LCtrl);
        Assert.Equal(Key.ShiftRight, Key.RShift);
        Assert.Equal(Key.ArrowUp, Key.Up);
        Assert.Equal(Key.Enter, Key.Return);
    }

    [Fact]
    public void ComponentAlignmentProbeReportsNaturalAlignment()
    {
        Assert.Equal(4, ComponentType<Health>.Alignment);
        Assert.Equal(4, ComponentType<Health>.Size);
        Assert.Equal(8, ComponentType<AlignedEight>.Alignment);
    }

    [Fact]
    public void ComponentSizeIsAlwaysAMultipleOfAlignment()
    {
        // Bevy asserts this when registering a layout, so a violation would surface as a panic
        // in the bridge rather than a C# error.
        Assert.Equal(0, ComponentType<Ragged>.Size % ComponentType<Ragged>.Alignment);
        Assert.Equal(0, ComponentType<AlignedEight>.Size % ComponentType<AlignedEight>.Alignment);
    }

    [Fact]
    public void EntityHandleTracksIdentityNotBitPattern()
    {
        var entity = new Entity(0x0000_0003_FFFF_FFF8UL);

        Assert.False(entity.IsNone);
        Assert.True(Entity.None.IsNone);
        Assert.Equal(entity, new Entity(entity.Bits));
        Assert.NotEqual(entity, new Entity(entity.Bits + 1));
    }

    [Fact]
    public void SystemDescriptorReportsAccessConflicts()
    {
        var writer = new SystemDescriptor(_ => { }, "w").Write<Health>();
        var reader = new SystemDescriptor(_ => { }, "r").Read<Health>();
        var unrelated = new SystemDescriptor(_ => { }, "u").Write<Armour>();
        var unannotated = new SystemDescriptor(_ => { }, "n");

        Assert.True(writer.ConflictsWith(reader));
        Assert.False(writer.ConflictsWith(unrelated));

        // An unannotated system is assumed to touch everything, so it never looks safe.
        Assert.True(writer.ConflictsWith(unannotated));
        Assert.True(unannotated.ConflictsWith(unrelated));
    }

    [Fact]
    public void RunConditionSkipsTheSystemWithoutInvokingIt()
    {
        using var world = new World();
        var ran = false;

        var descriptor = new SystemDescriptor(_ => ran = true, "s").RunIf(_ => false);

        Assert.False(descriptor.Invoke(world));
        Assert.False(ran);
    }

    [Fact]
    public void ToggleRegistryFlipsAndRemembers()
    {
        var registry = new SystemToggleRegistry();

        Assert.True(registry.Get("a"));
        registry.Flip("a");
        Assert.False(registry.Get("a"));
        registry.Flip("a");
        Assert.True(registry.Get("a"));

        Assert.False(registry.Get("b", defaultEnabled: false));
    }

    [Fact]
    public void SourceScopeTagsOnlyWhatIsInsideIt()
    {
        Assert.Null(SystemRegistrationSourceScope.Current);

        using (new SystemRegistrationSourceScope("outer"))
        {
            Assert.Equal("outer", SystemRegistrationSourceScope.Current);

            using (new SystemRegistrationSourceScope("inner"))
                Assert.Equal("inner", SystemRegistrationSourceScope.Current);

            Assert.Equal("outer", SystemRegistrationSourceScope.Current);
        }

        Assert.Null(SystemRegistrationSourceScope.Current);
    }

    // These exist only so their layouts can be measured; the fields are never read.
#pragma warning disable CS0649
    private struct AlignedEight
    {
        public double Value;
        public int Tag;
    }

    private struct Ragged
    {
        public int A;
        public int B;
        public int C;
    }
#pragma warning restore CS0649
}

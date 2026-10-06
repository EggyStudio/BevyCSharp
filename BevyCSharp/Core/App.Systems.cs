using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class App : IDisposable
{
    // -- System registration

    /// <summary>Registers a system function in <paramref name="stage"/>.</summary>
    public App AddSystem(Stage stage, SystemFn system) =>
        AddSystem(stage, new SystemDescriptor(system));

    /// <summary>Registers a system function with a run condition.</summary>
    public App AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition) =>
        AddSystem(stage, new SystemDescriptor(system).RunIf(runCondition));

    /// <summary>Registers a described system in <paramref name="stage"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// The app is already running and <see cref="EnableDynamicSystems"/> was not called before it
    /// started.
    /// </exception>
    public App AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning && _dynamicStages is not null) return AddDynamicSystem(stage, descriptor);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot register system '{descriptor.Name}': the app is already running. "
                + "Register systems from a plugin's Build method or before calling Run. To add "
                + "one while it runs, call EnableDynamicSystems before Run.");

        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        var registration = new RegisteredSystem(this, descriptor, stage);
        _systems.Add(registration);

        registration.NativeId = Native.Check(
            Native.bcs_app_add_system(
                _handle,
                (int)stage,
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"registering system '{descriptor.Name}'");

        return this;
    }

    /// <summary>Runs <paramref name="setup"/> once as the app starts, as Bevy's <c>Startup</c> systems do.</summary>
    /// <param name="setup">What to do, handed the frame's context, where the world is on loan.</param>
    /// <param name="name">What the system is called, for ordering and for the editor's list of systems.</param>
    /// <remarks>
    /// Bevy's <c>app.add_systems(Startup, setup)</c>, which a game says as often as it makes a scene.
    /// <see cref="AddSystem(Stage, SystemDescriptor)"/> says the same with the stage, a descriptor
    /// and a context made from the world, which a system that orders itself or runs on a condition
    /// still needs.
    /// </remarks>
    public App Startup(Action<BehaviorContext> setup, string name = "Startup") => On(Stage.Startup, setup, name);

    /// <summary>Runs <paramref name="update"/> every frame, as Bevy's <c>Update</c> systems do.</summary>
    /// <param name="update">What to do, handed the frame's context, where the world is on loan.</param>
    /// <param name="name">What the system is called, for ordering and for the editor's list of systems.</param>
    /// <remarks>Bevy's <c>app.add_systems(Update, update)</c>. See <see cref="Startup"/>.</remarks>
    public App Update(Action<BehaviorContext> update, string name = "Update") => On(Stage.Update, update, name);

    /// <summary>
    /// Runs <paramref name="run"/> in <paramref name="stage"/>, as a system Bevy adds to that
    /// schedule, and only while <paramref name="runIf"/> passes where one is given.
    /// </summary>
    /// <param name="stage">When in the frame it runs.</param>
    /// <param name="run">What to do, handed the frame's context, where the world is on loan.</param>
    /// <param name="name">What the system is called, for ordering and for the editor's list of systems.</param>
    /// <param name="runIf">Whether it runs this frame, asked before it does, or none to run every frame.</param>
    /// <remarks>
    /// Bevy's <c>app.add_systems(stage, run.run_if(condition))</c>, for the stages <see cref="Startup"/>
    /// and <see cref="Update"/> leave, such as <see cref="Stage.FixedUpdate"/> or
    /// <see cref="Stage.PostUpdate"/>.
    /// </remarks>
    public App On(Stage stage, Action<BehaviorContext> run, string name, Func<World, bool>? runIf = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        var descriptor = new SystemDescriptor(world => run(new BehaviorContext(world)), name);
        return AddSystem(stage, runIf is null ? descriptor : descriptor.RunIf(runIf));
    }

    /// <summary>
    /// Spawns a glTF file's scene once it has loaded, as Bevy's <c>SceneRoot</c> of a glTF does, and
    /// hands the root to <paramref name="spawned"/> once the scene is in the world under it.
    /// </summary>
    /// <param name="path">The file, under the asset root.</param>
    /// <param name="spawned">What to do with the scene's root, or none.</param>
    /// <param name="scene">Which of the file's scenes, by its place in the file.</param>
    /// <remarks>
    /// Bevy spawns the root at once and its scene under it when the file has loaded, a frame or more
    /// later. The root is handed over with <see cref="WorldInstanceReady"/>, once its meshes,
    /// lights and players are among its <see cref="EcsWorld.Descendants"/>, so
    /// <paramref name="spawned"/> can reach into the scene at once. A file that never loads hands
    /// nothing over.
    /// </remarks>
    public App SpawnGltf(string path, Action<BehaviorContext, Entity>? spawned = null, int scene = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var handle = AssetHandle.None;
        var root = Entity.None;
        var done = false;

        return Update(ctx =>
        {
            if (done) return;
            if (handle == AssetHandle.None) handle = AssetServer.LoadGltfScene(path, scene);

            if (root == Entity.None)
            {
                if (AssetServer.StateOf(handle) == AssetLoadState.Loaded) root = ctx.Ecs.SpawnScene(handle);
                return;
            }

            foreach (var ready in ctx.Read<WorldInstanceReady>())
            {
                if (ready.Entity != root) continue;
                done = true;
                spawned?.Invoke(ctx, root);
                return;
            }
        }, $"SpawnGltf({path})");
    }

    /// <summary>
    /// Registers <paramref name="systems"/> in <paramref name="stage"/>, each to run after the one
    /// before it.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>.chain()</c>, a sequence of systems that pass work along in one frame. Each is
    /// ordered after the previous one by its name, as <see cref="SystemDescriptor.After"/> orders
    /// one system after another, so systems sharing a name are ordered together.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.Chain(Stage.Update,
    ///     new SystemDescriptor(NewRound, "NewRound"),
    ///     new SystemDescriptor(Score, "Score"),
    ///     new SystemDescriptor(GameOver, "GameOver"));
    /// </code>
    /// </example>
    public App Chain(Stage stage, params SystemDescriptor[] systems)
    {
        ArgumentNullException.ThrowIfNull(systems);
        for (var i = 0; i < systems.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(systems[i]);
            if (i > 0) systems[i].After(systems[i - 1].Name);
            AddSystem(stage, systems[i]);
        }

        return this;
    }

    /// <summary>Runs <paramref name="observer"/> each time a <typeparamref name="TEvent"/> is triggered.</summary>
    /// <remarks>
    /// Bevy's <c>add_observer</c>, the same as <see cref="EcsWorld.Observe{TEvent}(Action{On{TEvent}})"/>
    /// on this app's world, which says what may be observed and when the observer runs.
    /// </remarks>
    public App AddObserver<TEvent>(Action<On<TEvent>> observer)
    {
        World.Resource<EcsWorld>().Observe(observer);
        return this;
    }

    /// <summary>Hands Bevy the order each system asked for, by the systems' names, as the app starts.</summary>
    /// <exception cref="InvalidOperationException">
    /// A system names one its stage has none of, or one that runs on a state's transition, which
    /// is not in any stage to be ordered within.
    /// </exception>
    private void ApplyOrder()
    {
        foreach (var system in _systems)
        {
            var descriptor = system.Descriptor;
            if (descriptor.RunsAfter.Count == 0 && descriptor.RunsBefore.Count == 0) continue;

            if (system.NativeId < 0 || system.Stage is Stage.Cleanup)
                throw new InvalidOperationException(
                    $"System '{descriptor.Name}' asks to be ordered, and it runs on a state's transition "
                    + "or on the way out, where there is no stage to order it within.");

            foreach (var name in descriptor.RunsAfter) Order(system, name, after: true);
            foreach (var name in descriptor.RunsBefore) Order(system, name, after: false);
        }

        void Order(RegisteredSystem system, string name, bool after)
        {
            var others = _systems.Where(other => other != system && other.NativeId >= 0 && other.Descriptor.Name == name).ToList();
            var here = others.Where(other => other.Stage == system.Stage).ToList();

            if (here.Count == 0)
            {
                var elsewhere = others.Select(other => other.Stage.ToString()).Distinct().ToList();
                throw new InvalidOperationException(
                    $"System '{system.Descriptor.Name}' is to run {(after ? "after" : "before")} '{name}', "
                    + (elsewhere.Count == 0
                        ? $"and no system in {system.Stage} or any other stage is called that."
                        : $"which runs in {string.Join(" and ", elsewhere)} rather than in {system.Stage}, and systems "
                          + "are ordered only within their stage."));
            }

            foreach (var other in here)
            {
                var (first, then) = after ? (other, system) : (system, other);
                Native.Check(
                    Native.bcs_app_order_systems(_handle, (int)system.Stage, first.NativeId, then.NativeId),
                    $"ordering '{first.Descriptor.Name}' before '{then.Descriptor.Name}'");
            }
        }
    }

    /// <summary>
    /// Puts a stage's systems added while running in an order that keeps every system's
    /// <see cref="SystemDescriptor.After"/> and <see cref="SystemDescriptor.Before"/>, and otherwise
    /// the order they were added in.
    /// </summary>
    /// <remarks>
    /// Those run from a list rather than Bevy's schedule, so their order is the list's. A name none
    /// of them carries is passed over until one that does arrives, and a cycle keeps the order they
    /// were added in for the systems caught in it.
    /// </remarks>
    private static void SortByOrder(List<RegisteredSystem> systems)
    {
        // Each system's predecessors among the others, from either side's declaration.
        var before = systems.ToDictionary(system => system, _ => new HashSet<RegisteredSystem>());
        foreach (var system in systems)
        {
            foreach (var other in systems)
            {
                if (other == system) continue;
                if (system.Descriptor.RunsAfter.Contains(other.Descriptor.Name)) before[system].Add(other);
                if (system.Descriptor.RunsBefore.Contains(other.Descriptor.Name)) before[other].Add(system);
            }
        }

        // Repeatedly the earliest added whose predecessors are all placed.
        var placed = new List<RegisteredSystem>(systems.Count);
        var left = new List<RegisteredSystem>(systems);
        while (left.Count > 0)
        {
            var next = left.FirstOrDefault(system => before[system].All(placed.Contains)) ?? left[0];
            placed.Add(next);
            left.Remove(next);
        }

        systems.Clear();
        systems.AddRange(placed);
    }

    /// <summary>The stages a dynamically added system can be put in.</summary>
    /// <remarks>
    /// The ones a behavior can name. The two internal stages are left out, because what they do is
    /// fixed, and nothing loaded at runtime has business in either.
    /// </remarks>
    private static readonly Stage[] DispatchStages =
    [
        Stage.First, Stage.PreUpdate, Stage.Update, Stage.FixedUpdate,
        Stage.PostUpdate, Stage.Render, Stage.Last,
    ];

    /// <summary>Where a dynamically added system waits to be run, or null when none may be.</summary>
    private Dictionary<Stage, List<RegisteredSystem>>? _dynamicStages;

    /// <summary>
    /// A transition system added while the app runs, by the state's slot, the value whose edge it
    /// runs on, and whether that is the way in or out, or for a move between two particular values
    /// the value it comes from as well.
    /// </summary>
    private readonly record struct DynamicEdge(int Slot, int Value, bool Entering, RegisteredSystem System, int? From = null);

    /// <summary>Transition systems added while the app runs, which the edge dispatcher runs.</summary>
    private readonly List<DynamicEdge> _dynamicEdges = [];

    /// <summary>What each slot with a dynamic edge held when last looked at, or nothing for no state.</summary>
    private readonly Dictionary<int, int?> _seenStates = [];

    /// <summary>
    /// Allows systems to be added after the loop has started.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A schedule cannot be added to once Bevy owns it, so this puts one dispatcher in each stage
    /// beforehand and runs whatever has arrived since. That lets a behavior compiled at runtime,
    /// from a script file that was edited while the app ran, reach the schedule at all.
    /// </para>
    /// <para>
    /// Off unless asked for, because it costs a call across the boundary per stage per frame
    /// whether or not anything was ever added. Retiring a generation is
    /// <see cref="RemoveSystemsBySource"/>, which is why a reloaded script registers under a tag
    /// of its own.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">The app is already running.</exception>
    public App EnableDynamicSystems()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_dynamicStages is not null) return this;

        if (IsRunning)
            throw new InvalidOperationException(
                "Cannot enable dynamic systems, because the app is already running and the dispatchers "
                + "have to be in the schedule before the loop takes it. Call this before Run.");

        _dynamicStages = [];

        // Transition systems that arrive while running, which Bevy's transition schedules cannot
        // take any more. Before the Update dispatcher, after Bevy has applied the frame's
        // transitions, so they run the frame a state changes, as Bevy's own would.
        AddSystem(Stage.Update, new SystemDescriptor(_ => RunDynamicEdges(), "DynamicSystems.Transitions")
        {
            Source = "Core.DynamicSystems",
        });

        foreach (var stage in DispatchStages)
        {
            var waiting = new List<RegisteredSystem>();
            _dynamicStages[stage] = waiting;

            AddSystem(stage, new SystemDescriptor(world =>
            {
                // Indexed rather than enumerated, because a system that runs may register
                // another, and a removed one is dropped here rather than left to be skipped
                // forever.
                for (var i = waiting.Count - 1; i >= 0; i--)
                {
                    if (waiting[i].IsRemoved) waiting.RemoveAt(i);
                }

                for (var i = 0; i < waiting.Count; i++)
                {
                    if (!waiting[i].IsRemoved) waiting[i].Descriptor.Invoke(world);
                }
            }, $"DynamicSystems.{stage}")
            {
                Source = "Core.DynamicSystems",
            });
        }

        return this;
    }

    /// <summary>Runs the exits and enters of every state a dynamic edge watches whose value changed.</summary>
    private void RunDynamicEdges()
    {
        if (_dynamicEdges.Count == 0) return;

        _dynamicEdges.RemoveAll(edge => edge.System.IsRemoved);

        foreach (var slot in _dynamicEdges.Select(edge => edge.Slot).Distinct().ToArray())
        {
            var now = ReadSlot(slot);
            var was = _seenStates.GetValueOrDefault(slot);
            if (now == was) continue;

            _seenStates[slot] = now;

            // Out of the old value first, then the move from it to the new one, and into the new
            // one after, as Bevy orders them.
            foreach (var edge in _dynamicEdges.ToArray())
            {
                if (edge.Slot == slot && edge.From is null && !edge.Entering && edge.Value == was) edge.System.Descriptor.Invoke(World);
            }

            foreach (var edge in _dynamicEdges.ToArray())
            {
                if (edge.Slot == slot && edge.From is { } from && was == from && edge.Value == now) edge.System.Descriptor.Invoke(World);
            }

            foreach (var edge in _dynamicEdges.ToArray())
            {
                if (edge.Slot == slot && edge.From is null && edge.Entering && edge.Value == now) edge.System.Descriptor.Invoke(World);
            }
        }
    }

    /// <summary>What a state slot holds, or nothing while it holds no state.</summary>
    private static int? ReadSlot(int slot)
    {
        int value;
        return Native.bcs_state_get(slot, &value) == NativeStatus.Ok ? value : null;
    }

    /// <summary>Adds a system to the dispatcher for its stage.</summary>
    /// <remarks>
    /// A startup system is the exception. It is run once, here, rather than queued. The stage
    /// already happened, so queueing it would mean it never ran at all, and what it means for
    /// something loaded at runtime is "when this arrives" rather than "when the app began". That
    /// lets a reloaded script spawn what it needs.
    /// </remarks>
    private App AddDynamicSystem(Stage stage, SystemDescriptor descriptor)
    {
        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        if (stage == Stage.Startup)
        {
            descriptor.Invoke(World);
            return this;
        }

        if (_dynamicStages is null || !_dynamicStages.TryGetValue(stage, out var waiting))
            throw new InvalidOperationException(
                $"Cannot register system '{descriptor.Name}' in {stage} while running, because only "
                + string.Join(", ", DispatchStages) + " and Startup accept one.");

        var registration = new RegisteredSystem(this, descriptor, stage);
        _systems.Add(registration);
        waiting.Add(registration);
        SortByOrder(waiting);

        return this;
    }

    /// <summary>
    /// Removes every system tagged with <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// Bevy has no API for pulling a system back out of a built schedule, so the descriptors
    /// stay registered and are neutered instead, so a removed system's callback returns
    /// immediately. That keeps hot-reload swapping generations correctly at the cost of an
    /// empty call per removed system per frame.
    /// </remarks>
    /// <returns>How many systems were removed.</returns>
    public int RemoveSystemsBySource(string source)
    {
        var removed = 0;
        foreach (var system in _systems)
        {
            if (system.Descriptor.Source != source || system.IsRemoved) continue;
            system.IsRemoved = true;
            removed++;
        }

        return removed;
    }

    /// <summary>The descriptors registered for <paramref name="stage"/>, in registration order.</summary>
    public IReadOnlyList<SystemDescriptor> SystemsIn(Stage stage) =>
        _systems.Where(s => s.Stage == stage && !s.IsRemoved)
            .Select(s => s.Descriptor)
            .ToArray();
}

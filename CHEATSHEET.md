# Cheatsheet

Every public method of the library, a line each, grouped as the guide is, with the first sentence of
what its documentation says. A line is a reminder, and the guide page each group links to is the
explanation. Each group is one block, a type's methods under a comment naming it. `build/cheatsheet`
writes the page from the built library, and `CheatsheetTests` holds it to the library, so a method
added without its line, or a line left for one that is gone, fails the suite.

## Running an app

The guide's page is [running-a-game.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/running-a-game.md).

```csharp
// BevyApp
static int Run(Config? config = null);                          // Creates an app with DefaultPlugins, runs it, and disposes it
static int Run(Action<App> configure, Config? config = null);   // Creates an app with DefaultPlugins, lets configure add to it, then runs it
static App Build(Config? config = null);                        // Builds an app with DefaultPlugins without running it

// App
App DetectAmbiguities(Stage stage, AmbiguityReport report = AmbiguityReport.Warn, Func<string, bool>? among = null);  // Reports, as the app starts, the systems of stage whose declared access conflicts and whose order nothing states, as Bevy's ambiguity_detection in a schedule's build settings does
IReadOnlyList<SystemAmbiguity> AmbiguitiesIn(Stage stage, Func<string, bool>? among = null);  // The pairs of systems of stage whose declared access conflicts and whose order nothing states, in the order they were added, which DetectAmbiguities reports
ComponentArray<T> AddComponentArray<T>();                       // Keeps every entity's T in a storage buffer a shader reads by the entity's mesh tag, as Bevy's GpuComponentArrayBufferPlugin does
App OnAdd<T>(ComponentHook<T> hook);                            // Runs hook the first time a T is put on an entity, Bevy's on_add
App OnInsert<T>(ComponentHook<T> hook);                         // Runs hook each time a T is put on an entity, after OnAdd the first time, Bevy's on_insert
App OnDiscard<T>(ComponentHook<T> hook);                        // Runs hook with a T's value before it is overwritten or taken off, Bevy's on_discard
App OnRemove<T>(ComponentHook<T> hook);                         // Runs hook as a T comes off an entity, by removal or by despawn, Bevy's on_remove
App AddPlugin(IPlugin plugin);                                  // Adds a plugin, building it immediately
App AddPlugins(IPluginGroup group);                             // Adds every plugin in a group, in Order order
bool HasPlugin<T>();                                            // True when a plugin of type T is registered
int Run();                                                      // Runs the engine
static void RequestExit();                                      // Asks the engine to shut down after the current frame
static void RequestExit(int code);                              // Asks the engine to shut down after the current frame, the run ending with code
static string? DescribeAdapter();                               // Describes the graphics adapter the renderer actually chose, or null in a headless run
void Dispose();                                                 // Releases what it holds
App AddSystem(ScheduleLabel schedule, SystemDescriptor descriptor);  // Puts a system in a schedule of the game's own, which runs it when a system asks with TryRunSchedule, as Bevy's add_systems with a label of the game's does
App AddSchedule(ScheduleLabel schedule, Stage after, bool singleThreaded = false);  // Places a schedule of the game's own in Bevy's main order, run once a frame right after after, or once at startup where after is PreStartup or Startup, as Bevy's custom_schedule places its own
App OnStateTransition<TState>(TransitionStep step, Action<BehaviorContext, StateTransitionEvent<TState>> run, string name);  // Runs run in Bevy's transition of TState, at step, with the move the state made, in every frame it makes one
App AddState<TState>(TState initial);                           // Adds a state machine over TState, starting at initial
App AddSubState<TState>(TState initial);                        // Adds a sub-state over TState, which exists only while its parent holds the value its SubStateOfAttribute names
App AddComputedState<TState, TSource>(params (TSource, TState)[] table);  // Adds a state worked out from another rather than set
App AddComputedState<TState, TSource>(Func<TSource, TState?> rule);  // Adds a state worked out from another by a rule, for what a table cannot state
App AddComputedState<TState, TFirst, TSecond>(Func<TFirst, TSecond, TState?> rule);  // Adds a state worked out from two others at once, by a rule of the game's own
App AddComputedState<TState, TFirst, TSecond, TThird>(Func<TFirst, TSecond, TThird, TState?> rule);  // Adds a state worked out from three others at once, by a rule of the game's own
static TState State<TState>();                                  // The current value of TState
static bool TryState<TState>(out TState value);                 // The current value of TState, or false when it does not exist
static void SetState<TState>(TState value);                     // Queues a transition of TState
App AddStateSystem<TState>(TState value, bool entering, SystemDescriptor descriptor);  // Registers a system to run once when TState enters or leaves value
App AddTransitionSystem<TState>(TState from, TState to, SystemDescriptor descriptor);  // Registers a system to run once when TState moves from from to to, and on no other move
App AddSystem(Stage stage, SystemFn system);                    // Registers a system function in stage
App AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition);  // Registers a system function with a run condition
App AddSystem(Stage stage, SystemDescriptor descriptor);        // Registers a described system in stage
App Startup(Action<BehaviorContext> setup, string name = "Startup");  // Runs setup once as the app starts, as Bevy's Startup systems do
App Update(Action<BehaviorContext> update, string name = "Update");  // Runs update every frame, as Bevy's Update systems do
App On(Stage stage, Action<BehaviorContext> run, string name, Func<World, bool>? runIf = null);  // Runs run in stage, as a system Bevy adds to that schedule, and only while runIf passes where one is given
App SpawnGltf(string path, Action<BehaviorContext, Entity>? spawned = null, int scene = 0);  // Spawns a glTF file's scene once it has loaded, as Bevy's SceneRoot of a glTF does, and hands the root to spawned once the scene is in the world under it
App Chain(Stage stage, params SystemDescriptor[] systems);      // Registers systems in stage, each to run after the one before it
App AddObserver<TEvent>(Action<On<TEvent>> observer);           // Runs observer each time a TEvent is triggered
App EnableDynamicSystems();                                     // Allows systems to be added after the loop has started
int RemoveSystemsBySource(string source);                       // Removes every system tagged with source
IReadOnlyList<SystemDescriptor> SystemsIn(Stage stage);         // The descriptors registered for stage, in registration order

// Config
static Config HeadlessFor(uint frames);                         // A windowless configuration that runs frames ticks and exits
static Config OffscreenFor(uint width = 1280, uint height = 720, uint frames = 0);  // A configuration that draws into an image of the given size instead of a window
static Config Windowed(string title, uint width = 1280, uint height = 720, GraphicsBackend backend = GraphicsBackend.Automatic);  // A window of the given size, drawn with backend

// World
bool IsChanged<T>();                                            // Whether the resource of type T was changed since the running system last ran, as Bevy's Res::is_changed answers, inserting it among the changes
bool IsAdded<T>();                                              // Whether the resource of type T was inserted since the running system last ran, as Bevy's Res::is_added answers
string? ChangedBy<T>();                                         // The file and line of the call that last changed the resource of type T, as Bevy's changed_by gives it, or null where TrackChangeLocations is off or the resource was never changed
void SetResource<T>(T value, string file = "", int line = 0);   // Replaces the resource of type T, marked changed, and added as well where there was none
bool SetResourceIfNeq<T>(T value, string file = "", int line = 0);  // Replaces the resource of type T only where it differs from the one held, as Bevy's set_if_neq does, and answers whether it did
void MarkChanged<T>(string file = "", int line = 0);            // Marks the resource of type T changed, after a field of the object it holds was written, which the world cannot see, as Bevy's ResMut marks it when written through
void InsertResource<T>(T value, string file = "", int line = 0);  // Adds or replaces the resource of type T, marked added and changed
T GetOrInsertResource<T>(T value);                              // Returns the existing resource, or inserts and returns value
T GetOrInsertResource<T>(Func<T> factory);                      // Returns the existing resource, or inserts one built by factory
T InitResource<T>();                                            // Returns the existing resource, or inserts a default-constructed one
bool RemoveResource<T>();                                       // Removes the resource of type T
bool ContainsResource<T>();                                     // True when a resource of type T is registered
T Resource<T>();                                                // Gets a required resource
T? TryResource<T>();                                            // Gets a resource, or null if it is not registered
bool TryGetResource<T>(out T value);                            // Gets a resource, reporting whether it was found
void Clear();                                                   // Disposes every disposable resource and clears the world
void Dispose();                                                 // Releases what it holds

// IPlugin
void Build(App app);                                            // Registers this plugin's contributions on app

// IPluginGroup
IEnumerable<(IPlugin, int)> GetPlugins();                       // The plugins in this group, with their relative ordering

// DefaultPlugins
IEnumerable<(IPlugin, int)> GetPlugins();                       // The plugins in this group, with their relative ordering

// EnginePlugin
void Build(App app);                                            // Registers this plugin's contributions on app

// BehaviorsPlugin
void Build(App app);                                            // Registers this plugin's contributions on app

// Time
void Pause();                                                   // Stops the game's clock, from the next frame
void Resume();                                                  // Starts the game's clock again, from the next frame
void Step(int frames = 1);                                      // Runs the clock for a number of frames and stops it again, to watch a paused game move a frame at a time
void SetSpeed(float speed);                                     // Sets how fast the game's clock runs against the wall's

// FrameProfile
static void Start();                                            // Starts measuring, from nothing
static void Stop();                                             // Stops measuring
static FrameCosts Take(EcsWorld world);                         // What was measured since Start or the last take, which starts the count over
```

## Behaviors and systems

The guide's page is [behaviors.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/behaviors.md).

```csharp
// BehaviorContext
T Res<T>();                                                     // Gets a required resource
bool TryRes<T>(out T value);                                    // Gets a resource, reporting whether it was found
TState State<TState>();                                         // The current value of TState
void SetState<TState>(TState value);                            // Asks Bevy to move TState to value
void Send<TMessage>(TMessage message);                          // Broadcasts a message for every reader to see next frame
ReadOnlySpan<TMessage> Read<TMessage>();                        // The messages of type TMessage sent during the previous frame
bool TryRunSchedule(ScheduleLabel schedule);                    // Runs the systems of a schedule of the game's own to their end, before this call returns, as Bevy's World::try_run_schedule does, and answers whether it ran
void Exit();                                                    // Asks the engine to shut down after this frame
void Exit(int code);                                            // Asks the engine to shut down after this frame, the run ending with code, as RequestExit does

// BehaviorConditions
static Func<World, bool> HasResource<T>();                      // Passes while a resource of type T exists
static Func<World, bool> ResourceIs<T>(Func<T, bool> predicate);  // Passes while a resource exists and satisfies predicate
static Func<World, bool> AnyWithComponent<T>();                 // Passes while at least one entity carries T
static Func<World, bool> InState<TState>(TState value);         // Passes while TState holds value
static Func<World, bool> RunOnce();                             // Passes only on the first frame
static Func<World, bool> KeyToggle<TTag>(Key key, KeyModifier modifiers = KeyModifier.None, bool defaultEnabled = true);  // A keyboard toggle keyed by TTag, usually the behavior struct
static Func<World, bool> KeyToggle(string systemId, Key key, KeyModifier modifiers = KeyModifier.None, bool defaultEnabled = true);  // A keyboard toggle keyed by an explicit id
static bool ModifiersHeld(Input input, KeyModifier modifiers);  // True when every modifier set in modifiers is currently held

// BehaviorRegistry
static void Add(Action<App> register);                          // Records a generated registration
static IReadOnlyList<Action<App>> Snapshot();                   // A snapshot of the recorded registrations

// BehaviorRunners
static void Run<T>(World world, BehaviorRunner<T> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, int parallelThreshold = 4096);  // Runs body for every entity carrying T
static void Run<T, T1>(World world, BehaviorRunner<T, T1> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, bool writesFirst = true, int parallelThreshold = 4096);  // Runs body for every entity carrying T and T1, handing it both by reference from the same storage
static void Run<T, T1, T2>(World world, BehaviorRunner<T, T1, T2> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, bool writesFirst = true, bool writesSecond = true, int parallelThreshold = 4096);  // Runs body for every entity carrying T, T1 and T2, handing it all three by reference from the same storage, as the two-component form does

// SystemDescriptor
SystemDescriptor RunIf(Func<World, bool> condition);            // Attaches a run condition
SystemDescriptor After(string name);                            // Runs this system after every system in its stage named name
SystemDescriptor Before(string name);                           // Runs this system before every system in its stage named name
SystemDescriptor AmbiguousWith(string name);                    // Says this system may run either side of every system in its stage named name, though their declared access conflicts
SystemDescriptor Read<T>();                                     // Declares a read of resource type T
SystemDescriptor Write<T>();                                    // Declares a write of resource type T
bool ConflictsWith(SystemDescriptor other);                     // True when this system's declared access overlaps other's in a way that would prevent the two running concurrently
bool Invoke(World world);                                       // Runs the system, honoring RunCondition

// StageOrder
static ReadOnlySpan<Stage> AllInOrder();                        // Every user-facing stage, in execution order
static ReadOnlySpan<Stage> FrameStages();                       // The stages that run exactly once every frame, in execution order
static bool IsInternal(Stage stage);                            // True for the two stages reserved for the engine itself

// SystemToggleRegistry
bool Get(string id, bool defaultEnabled = true);                // The state for id, or defaultEnabled if unset
void Set(string id, bool enabled);                              // Sets the state for id
void Flip(string id, bool defaultEnabled = true);               // Flips the state for id

// SystemRegistrationSourceScope
void Dispose();                                                 // Releases what it holds

// Stepping
static void Add(Stage stage);                                   // Steps the systems of stage, from the next frame, while stepping is on
static void Remove(Stage stage);                                // Runs stage whole again, and forgets what was set for its systems
static void Enable();                                           // Turns stepping on, so the systems of the added stages wait for a step
static void Disable();                                          // Turns stepping off, so every system runs every frame again
static void StepFrame();                                        // Runs the next stepped system in the next frame
static void ContinueFrame();                                    // Runs the stepped systems from the next one to the end of the stepping frame, or to the next breakpoint, in the next frame
static void AlwaysRun(Stage stage, string system);              // Runs the systems of stage called system every frame while stepping is on
static void NeverRun(Stage stage, string system);               // Runs the systems of stage called system in no frame while stepping is on
static void SetBreakpoint(Stage stage, string system);          // Stops a continued frame before the systems of stage called system, which the next step or continue then runs
static void ClearBreakpoint(Stage stage, string system);        // Steps the systems of stage called system as the rest are stepped, clearing a breakpoint or AlwaysRun or NeverRun, as Bevy's clear_breakpoint clears whichever was set
static string? Describe();                                      // Bevy's own account of the stepping, its action, stages, order and cursor, as Bevy's helper prints it, or null where nothing has asked for stepping

// EcsWorld
bool Added<T>(Entity entity);                                   // Whether an entity's component was added since the running system last ran, as Bevy's Ref::is_added answers
bool SetIfNeq<T>(Entity entity, T component, string file = "", int line = 0);  // Overwrites a component's value only where it differs from the one held, as Bevy's set_if_neq does, so what watches for changes sees a change only when there is one, and answers whether it wrote
string? ChangedBy<T>(Entity entity);                            // The file and line of the call that last changed an entity's component, as Bevy's changed_by gives it, or null where it is not known
Entity Spawn();                                                 // Spawns an entity with no components
Entity Spawn(Action<Entity, EcsWorld> build);                   // Spawns an entity and applies build to it
void SpawnBatch(int count, Action<Entity, EcsWorld> build);     // Spawns count entities, applying build to each
void SpawnBatch<T>(int count, Func<int, T> factory);            // Spawns count entities carrying a component from factory
Entity SpawnScene(AssetHandle scene);                           // Spawns a scene asset under a new entity, and returns that entity
Entity Clone(Entity entity);                                    // Spawns a copy of an entity with every component it carries
bool Despawn(Entity entity);                                    // Destroys an entity and everything on it
void DespawnOnExit<TState>(Entity entity, TState state);        // Despawns an entity when a state leaves the value it belongs to
void DespawnOnEnter<TState>(Entity entity, TState state);       // Despawns entity as TState enters state, Bevy's DespawnOnEnter
void DespawnWhen<TState>(Entity entity, Func<StateTransitionEvent<TState>, bool> rule);  // Despawns entity at the first transition of TStaterule answers true for, Bevy's DespawnWhen
bool IsAlive(Entity entity);                                    // True when the handle still refers to a live entity
void Add<T>(Entity entity, T component, string file = "", int line = 0);  // Adds or replaces a component on an entity
void Set<T>(Entity entity, T component, string file = "", int line = 0);  // Overwrites a component's value
bool Remove<T>(Entity entity);                                  // Removes a component
bool Has<T>(Entity entity);                                     // True when an entity carries a component
bool TryGet<T>(Entity entity, out T component);                 // Reads a component, reporting whether it was present
T GetOrDefault<T>(Entity entity);                               // Reads a component, or returns default when absent
T GetRef<T>(Entity entity);                                     // Returns a reference straight into Bevy's storage, so writes land in place
bool Mutate<T>(Entity entity, Func<T, T> mutate);               // Applies mutate to a component in place, if the entity has one
bool Changed<T>(Entity entity);                                 // True when a component changed since the previous frame
bool ChangedById(Entity entity, int componentId);               // True when a component changed since the previous frame, by raw component id
int Count<T>();                                                 // Counts entities carrying T
static int ComponentId<T>();                                    // The Bevy component id for T, registering it if needed
bool HasById(Entity entity, int componentId);                   // True when an entity carries the component with this id
int CountById(int componentId);                                 // Counts entities carrying the component with this id
bool RemoveById(Entity entity, int componentId);                // Removes the component with this id
bool SetParent(Entity child, Entity parent);                    // Makes child a child of parent
bool ClearParent(Entity child);                                 // Detaches an entity from its parent
Entity ParentOf(Entity entity);                                 // An entity's parent, or None if it has none
Entity[] ChildrenOf(Entity entity);                             // An entity's direct children, in order
void Patch<T>(Entity entity, PatchOf<T> change);                // Changes the fields of a component without replacing the rest of it
int PatchTree<T>(Entity root, PatchOf<T> change);               // The same, for an entity and everything under it
Entity[] All();                                                 // Every live entity in the world
int[] ComponentsOf(Entity entity);                              // The ids of the components an entity carries
string ComponentName(int component);                            // What a component is called
string? NameOf(Entity entity);                                  // What an entity is called, or null when it is called nothing
void SetName(Entity entity, string? name);                      // Names an entity, or takes its name away when given nothing
ChunkSet<T> Chunks<T>(ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, bool markChanged = true);  // Collects the storage runs holding T, optionally filtered
ChunkSet<T> Chunks<T>(int componentId, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, bool markChanged = true);  // Collects the storage runs for an explicitly named component
ComponentQuery<T> Query<T>(bool markChanged = true);            // Iterates every T in the world by reference
Entity[] EntitiesWith<T>();                                     // The entities carrying T
IDisposable Observe<TEvent>(Action<On<TEvent>> observer);       // Runs observer each time a TEvent is triggered
IDisposable Observe<TEvent>(Entity entity, Action<On<TEvent>> observer);  // Runs observer each time a TEvent reaches entity
void Trigger<TEvent>(TEvent value);                             // Runs the observers of value now
string? GetReflected(Entity entity, string typePath, string path = "");  // Reads one of Bevy's components, or one field of it, as JSON
void SetReflected(Entity entity, string typePath, string path, string json);  // Writes a value given as JSON over one of Bevy's components, or one field of it
string? GetVariant(Entity entity, string typePath, string path);  // Which variant an enum inside one of Bevy's components holds
void SetVariant(Entity entity, string typePath, string path, string variant);  // Switches an enum inside one of Bevy's components to another variant
AssetHandle? GetReflectedAsset(Entity entity, string typePath, string path);  // Reads an asset handle inside one of Bevy's components, such as the image a material or a sprite draws with
void SetReflectedAsset(Entity entity, string typePath, string path, AssetHandle asset);  // Points an asset handle inside one of Bevy's components at another asset
Color? GetReflectedColor(Entity entity, string typePath, string path);  // Reads a color inside one of Bevy's components as linear RGBA
void SetReflectedColor(Entity entity, string typePath, string path, Color color);  // Writes a color inside one of Bevy's components from linear RGBA
void InsertReflected(Entity entity, string typePath, string? json = null);  // Puts one of Bevy's components on an entity, from JSON or at its default
bool RemoveReflected(Entity entity, string typePath);           // Takes one of Bevy's components off an entity
T? Get<T>(Entity entity);                                       // A typed wrapper over one of Bevy's components on an entity, or null when the entity does not carry it
T Wrap<T>(Entity entity);                                       // A typed wrapper over one of Bevy's components an entity is known to carry
T Insert<T>(Entity entity, string? json = null);                // Puts one of Bevy's components on an entity, from JSON or at its default, and returns a typed wrapper over it
T[] All<T>();                                                   // A typed wrapper over one of Bevy's components on each entity carrying it
Entity? ResourceEntity(string typePath);                        // The entity holding one of Bevy's resources, or null when the world has none of it
T? Resource<T>();                                               // A typed wrapper over one of Bevy's resources, or null when the world has none of it
T InsertResource<T>(string? json = null);                       // Puts one of Bevy's resources in the world, from JSON or at its default, replacing the one it has, and returns a typed wrapper over it
void Relate<TRelation>(Entity source, Entity target);           // Makes source relate to target by TRelation, in place of what it related to by it before, as inserting one of Bevy's relationship components does
void Unrelate<TRelation>(Entity source);                        // Takes source's relation by TRelation away, as removing Bevy's relationship component does
Entity? RelationOf<TRelation>(Entity source);                   // What source relates to by TRelation, or null where it relates to nothing by it
Entity[] RelatedTo<TRelation>(Entity target);                   // The entities relating to target by TRelation, in the order they came to, as Bevy's relationship target lists them
IEnumerable<Entity> RelationAncestors<TRelation>(Entity entity);  // What entity relates to by TRelation, what that relates to, and on, as Bevy's iter_ancestors follows a relationship
List<Entity> Removed<T>();                                      // The entities that lost T, by a removal or a despawn, since the running system last asked, oldest first
List<Entity> RemovedById(int componentId);                      // The entities that lost the component with this id since the running system last asked, as Removed lists them
Entity SpawnMesh(AssetHandle mesh, AssetHandle material, Transform at);  // Spawns an entity drawn with a mesh and a material, placed by a transform, as Bevy's (Mesh3d(mesh), MeshMaterial3d(material), transform) bundle does
Entity SpawnPointLight(Vec3 at, bool shadows = false, float intensity = 1000000f, float range = 20f, float radius = 0f);  // Spawns a point light at a place, as Bevy's default one is, a million lumens reaching twenty units and casting no shadow unless asked
Entity SpawnCamera3d(Transform at, CameraSettings? settings = null);  // Spawns a 3D camera placed by a transform, with Bevy's defaults or the settings given
IEnumerable<Entity> Descendants(Entity root);                   // Every entity under root, nearer ones first, as Bevy's iter_descendants walks them

// EcsCommands
EcsCommands Spawn(Action<Entity, EcsWorld> build);              // Queues an entity spawn, passing the new entity to build
EcsCommands SpawnBatch(int count, Action<Entity, EcsWorld> build);  // Queues a spawn of count entities
EcsCommands SpawnBatch<T>(int count, Func<int, T> factory);     // Queues a spawn of count entities each carrying a component
EcsCommands Despawn(Entity entity);                             // Queues a despawn
EcsCommands Add<T>(Entity entity, T component);                 // Queues adding or replacing a component
EcsCommands Remove<T>(Entity entity);                           // Queues removing a component
EcsCommands Run(Action<EcsWorld> action);                       // Queues an arbitrary action against the world
EcsCommands Delayed(float seconds);                             // A queue whose commands land once seconds have passed after this queue is applied, Bevy's commands.delayed().secs
void Apply(EcsWorld world, double elapsedSeconds);              // Drains the queue against world at the app's time, landing the delayed queues whose time has come first and starting the delays queued since
void Apply(EcsWorld world);                                     // Drains the queue against world
void Clear();                                                   // Discards every queued command without applying it, delayed ones included

// ChunkSet<T>
void Dispose();                                                 // Returns the pooled buffer

// ReloadedComponents
static IReadOnlyList<ComponentSchema> Before();                 // The schemas there are, taken before the new assembly registers its own over them
static int Carry(EcsWorld world, IReadOnlyList<ComponentSchema> before);  // Moves every component of a type registered again since before onto the type registered in its place
static int Revive(EcsWorld world);                              // Puts on their entities the components a scene held that no type described when it was loaded and one does now
```

## States

The guide's page is [states.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/states.md).

```csharp
// StateRegistry
static void Declare<TState>(TState initial);                    // Declares TState a state starting at initial, which an app that uses it adds as it starts to run
static IReadOnlyList<string> Unentered();                       // The states declared on their enums that the running app has not added, by name
static TState Current<TState>();                                // The current value of TState
static bool TryCurrent<TState>(out TState value);               // The current value of TState, reporting whether it exists at all
static void Set<TState>(TState value);                          // Asks Bevy to move TState to value
```

## Messages

The guide's page is [messages-and-hierarchy.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/messages-and-hierarchy.md).

```csharp
// MessageBus
void Send<T>(T message);                                        // Sends a message, for every reader to see next frame
void SendAll<T>(ReadOnlySpan<T> messages);                      // Sends several messages at once
ReadOnlySpan<T> Read<T>();                                      // The messages of type T sent during the previous frame
int Count<T>();                                                 // How many messages of type T are readable this frame
bool IsEmpty<T>();                                              // True when nothing of type T arrived
void Swap();                                                    // Makes this frame's messages readable and starts a new frame's queue
void Clear();                                                   // Discards everything, sent and readable alike

// MessageReader<T>
T[] Read(BehaviorContext ctx);                                  // The messages this reader has not read yet, oldest first, and the reader moved past them

// MessageMutator<T>
MessageMutator.Unread Read(BehaviorContext ctx);                // The messages this mutator has not read yet, oldest first, in place, and the mutator moved past them

// On<TEvent>
void Propagate(bool propagate);                                 // Lets the event go on up to the parent, or, given false, stops it here
```

## Components

The guide's page is [components.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/components.md).

```csharp
// ComponentHooks
static void OnRemove<T>(RemoveHook<T> hook);                    // Declares what runs when a T leaves an entity
static void OnClone<T>(CloneHook<T> hook);                      // Declares what rewrites the copy of a T an entity clone receives

// ComponentSchema
bool Add(EcsWorld world, Entity entity);                        // Puts a default one on an entity
bool Remove(EcsWorld world, Entity entity);                     // Takes it off again
ComponentField? Field(string name);                             // Finds a field by name, or null
ComponentMethod? Method(string name);                           // Finds a method by name, or null
object? Read(EcsWorld world, Entity entity, string field);      // Reads one field by name, or null when there is no such field
bool Write(EcsWorld world, Entity entity, string field, object value);  // Writes one field by name, reporting whether it landed

// ComponentSchemas
static void Add(ComponentSchema schema);                        // Registers a schema, replacing any earlier one for the same type
static ComponentSchema? For(int componentId);                   // The schema for a component id, or null when none describes it
static ComponentSchema? For(string name);                       // The schema for a component's full name, or null
static bool TryCoerce<TField>(object value, out TField coerced);  // Turns a boxed value from a tool into the field's own type, reporting whether it fits

// ComponentField
object? Read(EcsWorld world, Entity entity);                    // Reads the field, or null when the entity does not carry it
bool Write(EcsWorld world, Entity entity, object value);        // Writes the field, reporting whether it landed

// ItemFields
object Create();                                                // An item at its defaults
ItemFields.Bound Bind(object item);                             // The fields of a copy of an item, and the copy as the fields leave it

// EcsList<T>
static EcsList<T> New();                                        // Makes an empty list now, rather than when its first item is added
void Add(T item);                                               // Adds an item at the end, making the list if this is its first
bool Remove(T item);                                            // Takes the first item equal to item out, reporting whether there was one
void RemoveAt(int index);                                       // Takes the item at a place out
void Clear();                                                   // Empties the list, keeping it
bool Contains(T item);                                          // Whether it holds an item equal to item
EcsList<T> Copy();                                              // A handle to a new list holding the same items, or a handle to nothing when this names none
bool Free();                                                    // Frees the list, after which this handle and every copy of it name nothing

// EcsMap<TKey, TValue>
static EcsMap<TKey, TValue> New();                              // Makes an empty dictionary now, rather than when its first entry is written
void Set(TKey key, TValue value);                               // Writes a value under a key, making the dictionary if this is its first entry
bool TryGetValue(TKey key, out TValue value);                   // Reads the value under a key, reporting whether there was one
bool ContainsKey(TKey key);                                     // Whether there is an entry under a key
bool Remove(TKey key);                                          // Takes the entry under a key out, reporting whether there was one
void Clear();                                                   // Empties the dictionary, keeping it
EcsMap<TKey, TValue> Copy();                                    // A handle to a new dictionary holding the same entries, or a handle to nothing when this names none
bool Free();                                                    // Frees the dictionary, after which this handle and every copy of it name nothing

// EcsText
static EcsText New(string text);                                // Makes text holding text
EcsText Copy();                                                 // A handle to new text the same as this, or a handle to nothing when this names none
bool Free();                                                    // Frees the text, after which this handle and every copy of it name nothing

// IInlineList<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Clear();                                                   // Empties the list

// InlineList4<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list

// InlineList8<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list

// InlineList16<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list

// InlineList32<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list

// InlineList64<T>
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list

// ListValue
static ListValue From<T>(ReadOnlySpan<T> items);                // The items of a span, boxed, as a generated schema reads an inline list
static ListValue From<T>(IReadOnlyList<T> items);               // The items of a list, boxed, as a generated schema reads a stored list
ListValue With(int index, object? item);                        // The same items with the one at index replaced
ListValue Adding(object? item);                                 // The same items with one more at the end
ListValue Without(int index);                                   // The same items without the one at index
ListValue Moving(int from, int to);                             // The same items with one moved from one place to another

// MapValue
static MapValue From<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> entries);  // The entries of a dictionary, boxed, as a generated schema reads a stored map
MapValue Rekeyed(int index, object key);                        // The same entries with the one at index given another key
MapValue With(int index, object? value);                        // The same entries with the one at index holding another value
MapValue Adding(object key, object? value);                     // The same entries with one more at the end
MapValue Without(int index);                                    // The same entries without the one at index

// IReflectedComponent<TSelf>
TSelf Create(EcsWorld world, Entity entity);                    // Makes a wrapper over the component on entity
```

## Scenes and saves

The guide's page is [scenes-and-saves.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/scenes-and-saves.md).

```csharp
// DataAssets
static void Register<T>(string name, Func<T> create, Func<DataBox<T>, IReadOnlyList<ComponentField>> fields, IReadOnlyList<string>? formerNames = null, int version = 0, Func<int, JsonObject, JsonObject>? migrate = null);  // Registers a data asset type
static T Get<T>(DataRef<T> reference);                          // The value of the data asset a reference names, loaded once and shared
static bool TryGet<T>(DataRef<T> reference, out T value);       // Reads the data asset a reference names, reporting whether there is one to read
static bool TryGet<T>(DataRef<T> reference, out T value, out string? problem);  // Reads the data asset a reference names, or says why there is none to read
static DataRef<T> Create<T>(string path);                       // Writes a new data asset at its type's defaults, gives it an id, and returns a reference
static ulong Create(string type, string path);                  // Makes a new data asset of a type named at runtime, for a menu offering every type
static ulong Copy(ulong id, string path);                       // Writes a copy of a data asset to a new file with an id of its own, for the one thing that should differ from the rest sharing it
static void Save<T>(DataRef<T> reference, T value);             // Writes a value over the data asset a reference names, and shares it from then on
static void Save(ulong id);                                     // Writes a loaded data asset's value to its file, as it is now
static ComponentSchema? SchemaOf(ulong id);                     // The fields of a data asset, bound to its loaded value, for a tool to draw and edit
static IReadOnlyList<string> Files(string? type = null);        // The data asset files under the asset root, as paths from it, holding one type or any
static string? TypeOf(string path);                             // The full name of the type a data asset file holds, or nothing for a file that holds none
static void Reload(ulong id);                                   // Forgets a loaded data asset, so the next read loads its file again
static void ReloadAll();                                        // Forgets every loaded data asset
static int SaveChanged();                                       // Writes every data asset changed through its fields since the last call

// SceneFile
static bool IsComputed(string name);                            // Whether a component, by its full name, is one the engine works out for itself
static int Save(EcsWorld world, string path, Func<Entity, bool>? include = null, bool giveIds = false);  // Writes entities to a scene file, and their children with them
static int Write(EcsWorld world, Utf8JsonWriter json, Func<Entity, bool>? include = null, bool giveIds = false);  // Writes entities as a scene document
static bool CanDescribe(EcsWorld world, Entity entity);         // Whether a scene can say how to make what an entity is drawn with, as Write writes it, or the entity is drawn with nothing
static string Resolve(string path);                             // The full path a scene path names, one under the asset root, with or without AssetsPrefix, one under the player's own directory after Prefix, or an absolute one as it is
static SceneLoad Load(EcsWorld world, string path, Entity parent = default);  // Spawns everything a scene file holds
static SceneLoad Read(EcsWorld world, JsonElement scene, Entity parent = default);  // Spawns everything a scene document holds

// SceneInstances
static Entity Spawn(EcsWorld world, string path);               // Places a scene asset in the world under a new entity, as an instance
static bool IsSubscene(string path);                            // Whether a path names a scene file, placed as a subscene, rather than a glTF scene
static bool IsInstance(EcsWorld world, Entity entity);          // Whether an entity is an instance's root
static string? SceneOf(EcsWorld world, Entity root);            // The scene an instance places, as the asset path it was spawned from, or nothing
static IReadOnlyList<InstanceOverride> Overrides(EcsWorld world, Entity root);  // The overrides an instance holds, in the order they were made
static IReadOnlyList<string> Missed(EcsWorld world, Entity root);  // The paths the last application of an instance's overrides could not find, which are kept in the instance and written back
static Entity RootOf(EcsWorld world, Entity entity);            // The instance root an entity belongs to, the nearest ancestor that is one, or the entity itself, or none
static Entity OuterRootOf(EcsWorld world, Entity entity);       // The outermost instance root an entity belongs to, which is the one a scene file writes, or none
static string? PathOf(EcsWorld world, Entity root, Entity node);  // A node's path of names from an instance's root, or nothing when it is not under the root
static Entity Find(EcsWorld world, Entity root, string path);   // The node a path of names leads to from an instance's root, or none
static bool Set(EcsWorld world, Entity node, string component, string field, object value);  // Writes a field of a component on a node of an instance, and records it as an override
static bool Mark(EcsWorld world, Entity node, string component, string field, object? before = null);  // Records what a field on a node of an instance holds as an override, for a tool that wrote it some other way, such as an inspector writing through the field itself
static bool IsOverridden(EcsWorld world, Entity node, string component, string field);  // Whether a field on a node of an instance is set by one of its overrides
static bool CanRevert(EcsWorld world, Entity node, string component, string field);  // Whether the model's value of an overridden field is known, so it can be put back
static bool Revert(EcsWorld world, Entity node, string component, string field);  // Puts the model's value back in an overridden field and takes the field out of the override
static bool Add(EcsWorld world, Entity node, string component);  // Puts a component on a node of an instance at its defaults, and records it
static bool MarkAdded(EcsWorld world, Entity node, string component);  // Records a component a node of an instance carries as added, with every field it holds, for a tool that put it on some other way
static bool Remove(EcsWorld world, Entity node, string component);  // Takes a component off a node of an instance, and records it
static bool MarkRemoved(EcsWorld world, Entity node, string component);  // Records a component as taken off a node of an instance, for a tool that took it off some other way
static bool Rename(EcsWorld world, Entity node, string name);   // Gives a node of an instance another name, and records it
static bool Delete(EcsWorld world, Entity node);                // Despawns a node of an instance and what is under it, and records it
static IReadOnlyList<string> Apply(EcsWorld world, Entity root);  // Applies an instance's overrides to the scene spawned under it
static bool IsFromModel(EcsWorld world, Entity entity);         // Whether an entity was spawned from the scene of an instance it is under, rather than added by the scene the instance is placed in

// SceneReferences
void Name(Entity entity, int id);                               // Numbers an entity that is being written, so a field referring to it can name it
void Spawned(int id, Entity entity);                            // Says which entity a local id was spawned as, so a field naming it can be read
bool WriteEntity(Utf8JsonWriter json, Entity entity);           // Writes a reference to an entity, reporting whether it could be named
object? ReadEntity(JsonElement json);                           // Reads a reference to an entity, or nothing when the id names none
bool WriteAsset(Utf8JsonWriter json, AssetHandle asset);        // Writes a reference to an asset by its file, reporting whether it has one
object? ReadAsset(JsonElement json, string kind);               // Reads a reference to an asset by loading its file as the kind of asset given
void WriteFile(Utf8JsonWriter json, string path);               // Writes a reference to a file under the asset root as its id and its path, or as its path alone when it has no id
static string? ReadFile(JsonElement json);                      // The path a file reference names, where the file holding its id is now, with the label the reference gave, or the path it recorded when no file holds the id

// SceneResources
static void Register<T>(string name, Func<T> create, Func<DataBox<T>, IReadOnlyList<ComponentField>> fields, IReadOnlyList<string>? formerNames = null, int version = 0, Func<int, JsonObject, JsonObject>? migrate = null);  // Registers a resource type a scene writes

// SceneValue
static bool Write(Utf8JsonWriter json, ComponentField field, object value, SceneReferences? references = null);  // Writes one value as the JSON its kind takes
static object? Read(JsonElement json, ComponentField field, SceneReferences? references = null);  // Reads one value of a field's kind, boxed as Write takes it, or null when the JSON is not that kind
static JsonElement Migrated(JsonElement json, ComponentSchema schema);  // A component object brought up to its type's current version, or the object as it is when it is current or the type has no migration
static int WriteComponent(Utf8JsonWriter json, ComponentSchema schema, EcsWorld world, Entity entity, SceneReferences? references = null, IReadOnlyList<KeyValuePair<string, string>>? kept = null);  // Writes every field of one component on an entity as an object, nesting the fields of a struct inside the struct's name
static int ReadComponent(JsonElement json, ComponentSchema schema, EcsWorld world, Entity entity, SceneReferences? references = null);  // Writes every field a component object names onto an entity's component, leaving a field the object leaves out as it is
static IReadOnlyList<KeyValuePair<string, string>> Unread(JsonElement json, ComponentSchema schema);  // The values a component object holds that no field of the schema reads, by dotted name with their JSON

// SaveGame
static void Carry(IPersistentValue value);                      // Makes a persistent value part of every save, copied into the save and put back when the save is loaded
static void Drop(IPersistentValue value);                       // Stops carrying a persistent value in saves
static IReadOnlyList<Entity> Start(EcsWorld world, params string[] scenes);  // Starts a game from scenes, and remembers which saved entities they hold, so a save can tell what play changed
static void Begin(EcsWorld world, params string[] scenes);      // Starts a game from scenes already loaded, remembering them and the saved entities they hold, as Start does after it loads them
static int Save(EcsWorld world, string path = "user://saves/slot.save.json");  // Writes what play changed over the scenes the game started from
static SceneLoad Load(EcsWorld world, string path = "user://saves/slot.save.json");  // Loads the scenes a save names and lays the save over them

// SaveId
static SaveId New();                                            // A random id, which two entities are as good as certain never to share
static SaveId? Parse(string text);                              // An id written as sixteen hex digits, or nothing when the text is not one

// Persistent<T>
void Set(T value);                                              // Replaces the value, without writing it
void Update(Func<T, T> change);                                 // Changes the value through a function of the old one, without writing it
void Persist();                                                 // Writes the value to its file
void Revert();                                                  // Throws away changes not yet written, by reading the file again
void Reset();                                                   // Puts the default back and writes it, as a settings screen's reset does

// IPersistentValue
void Write(Utf8JsonWriter json);                                // Writes the value as JSON
bool Read(JsonElement json);                                    // Replaces the value with the one JSON holds, reporting whether it could be read

// ProjectSettings
static ProjectSettings Read();                                  // The settings in the assets an app reads, or every one at its default when there is no file or it does not read, which Problem says
static ProjectSettings ReadFrom(string assets);                 // The settings in a folder of assets on disk, for a tool reading them before any app exists, or every one at its default when there is no file or it does not read, which Problem says
static ProjectSettings Parse(string text);                      // Reads settings from the text of a project file
void Write(string path);                                        // Writes the settings to a project file, leaving out the ones at their default

// UserData
static string Resolve(string path);                             // The full path of a user:// path, or of a path relative to the directory
static void WriteAtomically(string full, ReadOnlySpan<byte> bytes);  // Writes text to a file through a temporary one renamed over it
```

## Assets and models

The guide's page is [assets-and-models.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/assets-and-models.md).

```csharp
// AssetServer
static AssetHandle LoadGltfMesh(string path, int mesh = 0, int primitive = 0);  // Starts loading one drawable piece of geometry out of a glTF file
static AssetHandle LoadImage(string path, TextureSettings settings);  // Starts loading an image with the sampler it should be drawn with
static AssetHandle LoadGltfMaterial(string path, int material = 0);  // Starts loading one material out of a glTF file, as the renderer's own material type
static AssetHandle LoadGltfScene(string path, int scene = 0);   // Starts loading one scene out of a glTF file
static AssetHandle LoadFolder(string path);                     // Starts loading every asset in a folder and the folders inside it, and answers the folder's handle
static bool TryReadFolder(AssetHandle folder, out AssetHandle[] assets);  // The handles of the assets a folder holds, once it and they have loaded
static AssetHandle LoadGltfSceneCompressed(string path, int scene = 0);  // Starts loading one scene out of a glTF file with the file's meshes compressed as they load, as Bevy's glTF loader does given its regular compression
static string? PathOf(AssetHandle handle);                      // The path an asset was loaded from, or null when it has none
static AssetHandle Load(string kind, string path);              // Starts loading an asset and returns a handle to it
static AssetLoadState StateOf(AssetHandle handle);              // How far along an asset's load is
static AssetLoadState StateWithDependenciesOf(AssetHandle handle);  // How far along an asset's load is, counting everything it depends on
static bool IsAlive(AssetHandle handle);                        // True when the engine is still holding this handle
static bool Release(AssetHandle handle);                        // Releases a handle
static bool ReleaseWhenUnused(AssetHandle handle);              // Releases a handle once nothing else holds its asset, as a component drawing with it does until its entity is despawned

// AssetFiles
static void Use(Assembly? assembly);                            // Looks for the asset files in an assembly's resources, or in none, and in no pack
static void Use(Assembly? assembly, AssetPack? pack);           // Looks for the asset files in a pack and an assembly's resources, either of which may be nothing
static void Mount(string folder, AssetPack pack);               // Reads a pack's files under a folder of the asset root while an app runs, on both sides of the bridge, until Unmount or the next app, as a scene pack fetched for the player is read
static void Mount(string folder, string directory);             // Reads the files of a folder on this machine under a folder of the asset root while an app runs, on both sides of the bridge, until Unmount or the next app, as a scene's meshlets cut once and kept in a cache are read on the runs after
static bool Unmount(string folder);                             // Stops reading what is mounted under a folder, and closes it where it is a pack
static bool Exists(string path);                                // Whether a file is there, on disk or among what the game carries
static bool IsCarried(string path);                             // Whether a file is read from what the game carries, there being none on disk
static string ReadAllText(string path);                         // A file's text, from disk or from what the game carries
static byte[] ReadAllBytes(string path);                        // A file's bytes, from disk or from what the game carries
static IReadOnlyList<string> Carried(string suffix);            // The paths under the asset root of every carried file ending in suffix, such as the id sidecars a shipped game carries

// AssetIds
static ulong IdOf(string path, bool create = false);            // The id of an asset file, or zero when it has none and create is false
static string? PathOf(ulong id);                                // Where the file with an id is now, or null when no sidecar under the root holds it
static void Move(string from, string to);                       // Moves a file and its sidecar together, so references to it keep working by id
static void Reindex();                                          // Reads every sidecar under the root again
static bool IsSidecar(string path);                             // Whether a path is a sidecar, which a list of assets leaves out
static int WriteIndex(string? path = null);                     // Writes every id under the root, with the path it names, to one index file, for a shipped game to carry instead of the sidecars
static int IndexForShipping(string root);                       // Turns the sidecars under a folder into the one index a shipped game carries, writing IndexName at its root and deleting the sidecars

// AssetPack
static bool TryOpen(string path, out AssetPack? pack, out string? problem);  // Opens a pack and reads its index, or says why it cannot, naming the file
static AssetPack Open(string path);                             // Opens a pack and reads its index
bool Contains(string path);                                     // Whether the pack holds a file
Stream? OpenFile(string path);                                  // A file in the pack, opened for reading and seeking, or nothing when the pack lacks it
static int Write(string folder, string pack, Func<string, bool>? include = null);  // Writes a pack of every file under a folder that include takes
void Dispose();                                                 // Closes the pack

// Streaming
static StreamRead Read(string path, long offset = 0, int length = -1, int priority = 0);  // Starts reading length bytes at offset of a file on a worker thread, and answers the read at once
static bool TryTake(StreamRead read, out byte[]? bytes);        // Takes what a read brought back, if it has finished, fits this frame's budget, and no finished read of higher priority is waiting
static void Cancel(StreamRead read);                            // Stops a read, or forgets one that finished and was never taken

// GltfContents
static IReadOnlyList<GltfPart>? Read(string path);              // The meshes, materials and textures a glTF file holds, or nothing when it cannot be read as one

// GltfPart
string PathIn(string model);                                    // The asset path that loads this part of model

// Animation
static bool TryClips(Entity scene, out IReadOnlyList<string> clips);  // The names of a model's clips, or false while it has not arrived
static bool Play(Entity scene, string clip, AnimationSettings? settings = null);  // Plays one of a model's clips, fading out whatever played before
static void Stop(Entity scene);                                 // Stops every clip on a model, leaving it in the pose it was in
static void Pause(Entity scene);                                // Holds what a model is playing where it is
static void Resume(Entity scene);                               // Lets what a model is playing go on from where it was held
static void Seek(Entity scene, float seconds);                  // Moves the clip a model is playing to a time from its start
static void SetRepeat(Entity scene, uint times);                // Changes how many times the clip playing plays, counting those it has finished, without starting it over, zero for ever, one for once
static void SetSpeed(Entity scene, float speed);                // Changes how fast the clip a model is playing goes
static AnimationState? StateOf(Entity scene);                   // What a model is playing, or nothing while it has not arrived
static AssetHandle CreateClip();                                // Makes an empty clip, which AddCurve fills
static void AddCurve(AssetHandle clip, AnimationTarget target, AnimationCurve curve);  // Adds a curve to a clip, moving one property of the entity target names
static (AssetHandle Graph, uint Node) GraphFromClip(AssetHandle clip);  // A graph holding the one clip, and the clip's node in it, as Bevy's AnimationGraph::from_clip
static void PlayGraph(Entity player, AssetHandle graph, uint node, bool repeat = false);  // Makes an entity a player of a graph, playing one of its nodes
static void Animate(Entity entity, AnimationTarget target, Entity player);  // Makes an entity the target a clip's curves are aimed at, moved by a player
static AssetHandle LoadClip(string path);                       // Starts loading one of a model file's clips by its label, as models/Fox.glb#Animation2
static void SetClipDuration(AssetHandle clip, float seconds);   // Sets how long a clip lasts, which one holding only events needs
static bool AddEvent<TEvent>(AssetHandle clip, float time, TEvent value);  // Places an event on a clip, triggered at its player as the clip reaches the time
static bool AddEvent<TEvent>(AssetHandle clip, AnimationTarget target, float time, TEvent value);  // Places an event on a clip, triggered at the entity a target names as the clip reaches the time
static (AssetHandle Graph, uint Root) CreateGraph();            // Makes an empty graph, its root a blend, for AddBlend and AddClip to fill
static uint AddBlend(AssetHandle graph, float weight, uint parent, bool additive = false);  // Adds a blend to a graph under a node, answering its node
static uint AddClip(AssetHandle graph, AssetHandle clip, float weight, uint parent, ulong mask = 0);  // Adds a clip to a graph under a node, answering its node
static void AddToMaskGroup(AssetHandle graph, AnimationTarget target, uint group);  // Puts a target into one of a graph's mask groups, numbered from zero
static void SetNodeMask(AssetHandle graph, uint node, ulong mask);  // Sets which mask groups a graph's node leaves out, which takes as it plays
static AnimationTarget? TargetOf(Entity entity);                // The target an entity is aimed at by, or null for one no clip aims at
static void SetGraph(Entity player, AssetHandle graph);         // Gives an entity a graph to play from, and a player where it has none
static void PlayNode(Entity player, uint node, bool repeat = false);  // Starts a node of an entity's graph playing beside whatever it plays already
static bool SetNodeWeight(Entity player, uint node, float weight);  // Sets the weight a playing node is mixed in at
static void Run(Entity scene, AnimationStateMachine machine);   // Runs a state machine on a model, which enters its starting state once the model arrives
static bool StopMachine(Entity scene);                          // Stops the machine a model runs, leaving its clip playing as it was
static string? MachineStateOf(Entity scene);                    // The state a model's machine is in, or nothing where none runs or the model has not arrived
static void SetParameter(Entity scene, string name, float value);  // Sets a number a model's machine compares, such as its speed
static void SetParameter(Entity scene, string name, bool value);  // Sets a flag a model's machine reads, such as whether it stands on the ground
static void Trigger(Entity scene, string name);                 // Fires a trigger a model's machine waits on, such as a jump, which stays until a transition takes it

// AnimationCurve
AnimationCurve PingPong();                                      // The same eased curve going back to its start after reaching its end, as Bevy's ping_pong
static AnimationCurve Translation(ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values);  // A translation through values at times
static AnimationCurve Translation(Vec3 from, Vec3 to, EaseFunction ease, float duration);  // A translation eased from one place to another
static AnimationCurve Rotation(ReadOnlySpan<float> times, ReadOnlySpan<Quat> values);  // A rotation through values at times
static AnimationCurve Rotation(Quat from, Quat to, EaseFunction ease, float duration);  // A rotation eased from one turn to another
static AnimationCurve Scale(ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values);  // A scale through values at times
static AnimationCurve Scale(Vec3 from, Vec3 to, EaseFunction ease, float duration);  // A scale eased from one size to another
static AnimationCurve UiScale(ReadOnlySpan<float> times, ReadOnlySpan<Vec2> values);  // An interface node's scale through values at times
static AnimationCurve UiRotation(ReadOnlySpan<float> times, ReadOnlySpan<float> radians);  // An interface node's rotation through values at times
static AnimationCurve TextColor(ReadOnlySpan<float> times, ReadOnlySpan<Color> values);  // A text's color through values at times, interpolated in sRGB as Bevy's example does
static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<float> values);  // A number in a field of a reflected component through values at times
static AnimationCurve Field(string component, string path, float from, float to, EaseFunction ease, float duration);  // A number in a field of a reflected component eased from one value to another
static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec2> values);  // A two-component vector in a field of a reflected component through values at times
static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values);  // A three-component vector in a field of a reflected component through values at times
static AnimationCurve Field(string component, string path, Vec3 from, Vec3 to, EaseFunction ease, float duration);  // A three-component vector in a field of a reflected component eased from one value to another
static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec4> values);  // A four-component vector in a field of a reflected component through values at times
static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Quat> values);  // A rotation in a field of a reflected component through values at times, along the shorter arc
static AnimationCurve Field(string component, string path, Quat from, Quat to, EaseFunction ease, float duration);  // A rotation in a field of a reflected component eased from one turn to another

// AnimationTarget
static AnimationTarget FromNames(ReadOnlySpan<string> names);   // The target at the end of a path of names, the first the topmost

// MeshFiles
static bool IsMeshFile(string path);                            // Whether a path names a mesh file
static AssetHandle Load(string path);                           // The mesh a file holds, loaded the first time it is asked for and shared from then on
static bool TryLoad(string path, out AssetHandle mesh, out string? problem);  // The mesh a file holds, or why it holds none, naming the file
static string? PathOf(AssetHandle mesh);                        // The file a mesh was loaded from or saved to, or nothing for one that has none
static void SaveAs(AssetHandle mesh, string path);              // Writes a mesh made in memory to a new file and makes the mesh that file's, so a scene refers to the file from then on
static bool Save(AssetHandle mesh);                             // Writes a mesh loaded from a file back to that file, as it is now

// MaterialFiles
static bool IsMaterialFile(string path);                        // Whether a path names a material file
static AssetHandle Load(string path);                           // The material a file holds, loaded the first time it is asked for and shared from then on
static bool TryLoad(string path, out AssetHandle material, out string? problem);  // The material a file holds, or why it holds none, naming the file
static string? PathOf(AssetHandle material);                    // The file a material was loaded from, or nothing for one made in memory
static void SaveAs(AssetHandle material, string path);          // Writes a material's settings to a new file and makes the material that file's, so saving it again writes there and a scene refers to the file
static bool Save(AssetHandle material);                         // Writes a material loaded from a file back to that file, as it is now
```

## Drawing

The guide's page is [drawing.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/drawing.md).

```csharp
// Render
static void SetFreeCamera(Entity camera, FreeCameraSettings settings);  // Puts Bevy's free camera on a camera, or changes the settings of the one there
static FreeCameraSettings? FreeCameraOf(Entity camera);         // The free camera's settings on a camera, or null where it has none
static void ClearFreeCamera(Entity camera);                     // Takes the free camera off a camera, which then stays where it is
static FreeCameraState? FreeCameraStateOf(Entity camera);       // What the free camera holds about a camera as it moves it, or null where it has no free camera
static void SetFreeCameraState(Entity camera, FreeCameraState state);  // Writes what the free camera holds about a camera, as a settings screen turns it off or a game resets its speed
static string FreeCameraControls(Entity camera);                // Bevy's own description of the free camera's controls on a camera, a line a control with Bevy's names for the keys, which its example shows
static void SetPanCamera(Entity camera, PanCameraSettings settings);  // Puts Bevy's pan camera on a 2D camera, or changes the settings of the one there
static PanCameraSettings? PanCameraOf(Entity camera);           // The pan camera's settings on a camera, the zoom it has come to among them, or null where it has none
static void ClearPanCamera(Entity camera);                      // Takes the pan camera off a camera, which then stays where it is
static string PanCameraControls(Entity camera);                 // Bevy's own description of the pan camera's controls on a camera, a line a control with Bevy's names for the keys, which its example shows
static void DollyZoom(Entity camera, CameraProjection projection);  // Moves a pan-orbit camera to another projection over Bevy's dolly zoom, keeping the point it orbits the same size on the screen as the field of view narrows or widens
static void LookTo(Entity camera, Vec3 direction);              // Turns a pan-orbit camera to face a direction, about the point it orbits, over Bevy's look-to animation
static Entity SpawnCamera3d();                                  // Spawns a 3D camera and returns it
static void SetViewport(Entity camera, uint x, uint y, uint width, uint height);  // Gives a camera part of the window to draw into, or the whole of it
static void SetPerspective(Entity camera, float fieldOfView, float near, float far);  // Sets a camera's field of view and how near and how far it sees, making it a perspective camera if it was not one
static void SetRoundedCorners(Entity camera, float radius, (float R, float G, float B, float A) fill = default);  // Rounds the corners of a camera's picture, showing fill outside them, which is clear unless given
static void SetClearColor((float R, float G, float B, float A) color);  // Sets the world's clear color, in linear RGBA, which a camera clearing to World clears to
static Entity SpawnCamera3d(CameraSettings settings);           // Spawns a 3D camera set up by settings
static Entity SpawnLight(LightKind kind, float intensity);      // Spawns a light and returns it
static Entity SpawnLight(LightSettings settings);               // Spawns a light set up by settings
static void SetShadowCascades(Entity light, int cascades = 0, float minimum = 0f, float maximum = 0f, float firstBound = 0f, float overlap = 0f);  // Sets how a directional light divides its shadows across the distance
static void SetLightCookie(Entity light, AssetHandle cookie);   // Shapes a spot light's beam with a picture, the way a gobo shapes a stage light
static void SetSoftShadows(Entity light, float size);           // Softens a light's shadow the farther it falls from what casts it, as a light size world units across does
static void SetShadowFiltering(Entity camera, ShadowFiltering filtering);  // Sets how a camera filters the shadow maps it reads
static void SetCameraFlags(Entity camera, CameraFlags flags);   // Says how a camera draws beyond what it looks at
static void SetPostProcessing(Entity camera, PostSettings settings);  // Sets what a camera does to the picture after the scene has been drawn
static void SetEffects(Entity camera, EffectSettings settings);  // Sets the lens a camera draws through
static void SetAtmosphere(Entity camera, AtmosphereSettings settings);  // Draws the sky the air scatters, seen from a camera
static void SetSunDisk(Entity light, SunDisk disk);             // Gives a directional light the disk of the sun it stands for, which an atmosphere draws in its sky where the light comes from
static void ClearAtmosphere(Entity camera);                     // Stops a camera drawing the sky
static void SetAdjustedPerspective(Entity camera, ReadOnlySpan<(int, int, float)> entries, float fieldOfView = 0.7853982f, float near = 0.1f, float far = 1000f);  // Gives a camera Bevy's perspective with some entries of its clip-from-view matrix written over it, a projection of the game's own, as Bevy's custom_projection declares one
static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f);  // Builds a mesh primitive and returns a handle to it
static MeshRecipe? RecipeOf(AssetHandle mesh);                  // The shape and measures a mesh was made from, or null for one that was not made by CreateMesh
static bool RebuildMesh(AssetHandle mesh, string shape, float a = 1f, float b = 1f, float c = 1f);  // Builds a primitive again with other measures, in place, so everything drawn with the mesh changes and keeps its handle
static bool CompressMesh(AssetHandle mesh);                     // Compresses a mesh as Bevy's regular compression does, in place, so everything drawn with it holds a fraction of the memory
static MeshData? DataOf(AssetHandle mesh);                      // The geometry a mesh was built from, or null for one that was not made by CreateMesh
static bool TryGetMeshInfo(AssetHandle mesh, out MeshInfo info);  // What a mesh is made of, its counts, its attributes and its bounds, read without copying its vertices
static bool GenerateTangents(AssetHandle mesh);                 // Works out tangents for a mesh that has none, from its normals and texture coordinates, as a normal map and anisotropy read them, which Bevy's primitives are made without
static bool TryReadMaterial(AssetHandle material, out MaterialSettings? settings);  // A standard material's settings, read back from the engine, whether code made the material or a glTF file brought it
static bool TryReadImage(AssetHandle image, out ImagePixels? pixels);  // Reads an image's texels from the copy the app keeps of it, or answers false while it is loading or where no copy is kept
static bool TryImageSize(AssetHandle image, out uint width, out uint height, out uint depth);  // Reads how large an image is in texels, once it has loaded
static void WriteImagePixels(AssetHandle image, ReadOnlySpan<byte> texels);  // Writes texels over the copy an image keeps, as many bytes as it holds, so the GPU is given them again and everything showing the image changes
static bool TryReadMesh(AssetHandle mesh, out MeshData? triangles);  // Reads a mesh's triangles back, where each vertex is, and which three make each triangle
static bool TryReadNormals(AssetHandle mesh, out Vec3[] positions, out Vec3[] normals);  // Reads a mesh's positions back with the normal at each
static void SetMeshFlags(EcsWorld world, Entity entity, MeshFlags flags);  // Says how an entity's mesh is treated beyond what it looks like
static AssetHandle CreateMaterial(float red, float green, float blue, float alpha = 1f, float metallic = 0f, float roughness = 0.5f);  // Builds a physically based material and returns a handle to it
static AssetHandle CreateMaterial((float R, float G, float B, float A) color);  // Builds a material of one color and returns a handle to it, as Bevy makes a StandardMaterial from a Color
static AssetHandle CreateMaterial(MaterialSettings settings);   // Builds a material from settings and returns a handle to it
static bool WriteMaterial(AssetHandle material, MaterialSettings settings);  // Writes settings over a standard material in place, so everything drawn with it changes
static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh);  // Gives an entity a mesh to draw
static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material);  // Gives an entity a material to draw its mesh with
static AssetHandle CreateDecalMaterial(MaterialSettings settings, float depthFadeFactor = 1f, float depthBias = 0f);  // Builds Bevy's forward decal material from settings and returns a handle to it, for an entity made a decal with SetForwardDecal
static void SetForwardDecal(Entity entity, AssetHandle material);  // Makes entity a forward decal drawn with material, its box the entity's transform, as Bevy's ForwardDecal with its material does
static void Screenshot(string path);                            // Writes what is being drawn to a PNG file
static void Screenshot(string path, AssetHandle target);        // Writes what a camera drew into an image to a PNG file
static void Screenshot(string path, Entity window);             // Writes what a window the game spawned shows to a PNG file
static void SetImageLighting(Entity camera, AssetHandle cubemap, float intensity = 1000f, Quat? rotation = null);  // Lights the scene from a cubemap, filtered on the GPU
static void SetEnvironmentMap(Entity camera, AssetHandle diffuse, AssetHandle specular, float intensity = 1000f, Quat? rotation = null);  // Lights the scene from a pair of cubemaps somebody baked earlier
static void SetReflectionProbe(Entity probe, AssetHandle diffuse, AssetHandle specular, float intensity = 1000f, Vec3? falloff = null);  // Makes an entity a reflection probe, a box inside which surfaces reflect a pair of baked cubemaps rather than the camera's environment
static void SetIrradianceVolume(Entity probe, AssetHandle voxels, float intensity = 1000f, Vec3? falloff = null);  // Makes an entity an irradiance volume, a box inside which surfaces take their diffuse indirect light from a grid of points held in a 3D image
static void SetProbeCapture(Entity probe, ProbeCaptureSettings? settings);  // Makes an entity a reflection probe that renders what is around it, or with null stops
static void RecaptureProbe(Entity probe);                       // Captures a probe that is not live again, after what is around it has changed
static void SetSkyLighting(Entity camera, float intensity = 1f, uint size = 512);  // Lights the scene from the sky this camera is already scattering
static void ClearSkyLighting(Entity camera);                    // Stops lighting the scene from the sky
static void SetSkybox(Entity camera, AssetHandle cubemap, float brightness = 1000f, Quat? rotation = null);  // Draws a cubemap behind everything a camera draws
static void SetColorGrading(Entity camera, GradingSettings? settings);  // Grades the picture a camera drew, after tonemapping
static void SetExposure(Entity camera, float ev100);            // Sets the exposure a camera meters the scene at, in EV-100
static void SetLensExposure(Entity camera, float aperture = 0f, float shutter = 0f, float sensitivity = 0f);  // Sets a camera's exposure from the lens it stands in for
static void SetLens(Entity camera, PhysicalLens lens);          // Meters a camera from a lens written down once (PhysicalLens)
static void SetSortedTransparency(Entity camera, bool on = true, uint layers = 0, float average = 0f, float threshold = 0f);  // Sorts transparent fragments rather than whole objects, for one camera
static void SetAmbientLight((float R, float G, float B) color, float brightness);  // Sets the ambient light every camera without its own is lit by
static void SetAmbientLight(Entity camera, (float R, float G, float B)? color, float brightness = 80f);  // Gives a camera an ambient light of its own, or with null takes it away so the camera is lit by everyone's again
static void SetAmbientOcclusion(Entity camera, AmbientOcclusionQuality? quality, float thickness = 0f);  // Turns Bevy's screen-space ambient occlusion on for a camera at a quality, or with null off
static void SetDeferredRendering(bool on);                      // Draws Bevy's own materials deferred, into a G-buffer lit afterward, or forward, lit as they are drawn, which is the default
static void SetContactShadows(Entity camera, ContactShadowSettings? settings);  // Draws contact shadows on a camera, or with null stops
static void SetScreenSpaceReflections(Entity camera, ReflectionSettings? settings);  // Turns Bevy's screen-space reflections on for a camera, or with null off
static void CauseError(RenderErrorCause cause);                 // Causes an error on the GPU, for a game to see what its ErrorPolicy does with one, as Bevy's render_recovery example causes them
static void SetGlobalIllumination(GlobalIllumination quality);  // Lights the camera's picture with the light each surface reflects onto the others, traced through the scene's distance field each frame as cascades of probes around the camera, at a quality, or none with Off, which it is by default
static Capture BeginCapture(AssetHandle target);                // Asks for a picture to be read back into memory rather than written to a file
static Capture BeginCapture();                                  // Asks for a picture of whatever this run is drawing into
static bool TryReadCapture(Capture capture, out CapturedImage? picture);  // Reads a capture once it has arrived, and forgets it
static bool TryReadCaptureAsItIs(Capture capture, out CapturedTexels? picture);  // Reads a capture once it has arrived, in the format it was drawn in, and forgets it
static void ReleaseCapture(Capture capture);                    // Forgets a capture that will not be read
static AssetHandle CreateTarget(uint width, uint height, TargetFormat format = TargetFormat.Rgba8, uint layers = 1);  // Creates an empty image a camera can draw into
static void SetMipGeneration(AssetHandle image, bool on);       // Has the GPU make an image's mipmaps from its top level each frame, as Bevy's MipGenerationJobs does, or stop
static void SetSampler(AssetHandle image, TextureSettings settings);  // Gives an image how it repeats past its edges and how it is filtered
static AssetHandle CreateImage(ReadOnlySpan<byte> pixels, uint width, uint height, bool srgb = true);  // Makes an image out of pixels held here, and hands back a handle to it
static void MakeCubemap(AssetHandle image);                     // Has an image of six square faces treated as a cubemap
static AssetHandle CubemapFromFaces(AssetHandle positiveX, AssetHandle negativeX, AssetHandle positiveY, AssetHandle negativeY, AssetHandle positiveZ, AssetHandle negativeZ);  // Makes a cubemap out of six images, one a face, as a cubemap shipped as six files is
static void MakeTextureArray(AssetHandle image, int layers);    // Has an image of layers equal pictures stacked from top to bottom treated as an array of them
static void MakeVolume(AssetHandle image, int slices);          // Has an image of slices equal pictures stacked from top to bottom treated as a 3D texture that many deep
static void SetCameraTarget(Entity camera, AssetHandle target);  // Points a camera at an image instead of at the window
static void SetCameraTarget(Entity camera, Entity window);      // Points a camera at a window the game spawned, Bevy's RenderTarget::Window naming it
static void SetCameraTarget(Entity camera, AssetHandle target, int layer);  // Points a camera at one layer of an image with several, such as a face of a cube
static bool TryProject(Entity camera, Vec3 point, out float x, out float y);  // Where a world point lands on a camera's viewport, in logical pixels
static bool TryRay(Entity camera, float x, float y, out Vec3 origin, out Vec3 direction);  // The ray through a point on a camera's viewport
static bool TryGetBounds(Entity entity, out Vec3 min, out Vec3 max);  // The box an entity occupies in the world, or false when it has none
static void SetWireframe(Entity entity, bool on, (float R, float G, float B, float A) color = default);  // Draws an entity's mesh as its own edges in an app that asked for wireframes with Wireframes, or stops drawing them
static void SetShadowMapSize(uint directional = 0, uint point = 0);  // Sets how large a shadow map each kind of light gets, in pixels on a side
static void SetLayers(EcsWorld world, Entity entity, uint layers);  // Puts an entity on a set of render layers, as a bit per layer
static AssetHandle CreateMesh(MeshData mesh);                   // Builds a mesh from vertices and returns a handle to it
static void WriteMesh(AssetHandle mesh, MeshData data);         // Writes vertices over a mesh, so everything drawn with it changes
static AssetHandle CreateImage(ReadOnlySpan<byte> pixels, uint width, uint height, bool srgb, bool mipmaps);  // Makes an image out of pixels held here, with its mipmaps made from them, and hands back a handle to it
static IReadOnlyList<PassTiming> Timings();                     // How long each render pass took, smoothed over the last frames, where the app asked for it with GpuTimings
static bool PipelinesReady();                                   // True once the renderer has compiled every pipeline it was asked for, so whatever has been spawned can be drawn
static void SetRayTracedLighting(Entity camera, bool on);       // Lights a camera with Bevy's ray tracing, or with false the usual way again
static void SetPathTraced(Entity camera, bool on);              // Draws a camera's picture with Bevy's path tracer, or with on false the usual way again
static void SetRayTracedDenoiser(bool on, DenoiserSettings? settings = null);  // Turns the bridge's denoiser over ray-traced lighting on or off, and sets what it filters by, the defaults of DenoiserSettings where settings is not given
static void SetRayTraced(Entity entity, AssetHandle mesh);      // Makes an entity's mesh one the rays of ray-traced lighting meet
static void CountDrawnMeshes(bool on);                          // Starts or stops reading back how many meshes the opaque pass draws, which DrawnMeshes then answers
static DrawnMeshCount DrawnMeshes(out uint drawn);              // How many meshes the opaque pass drew in a recent frame, while CountDrawnMeshes is on
static AssetHandle CreateMeshletMesh(AssetHandle mesh, uint quantization = 0, string? saveTo = null);  // Starts cutting a mesh into clusters that Bevy's meshlet renderer culls and picks a level of detail for on the GPU, and answers the meshlet mesh at once
static void SetMeshletMesh(EcsWorld world, Entity entity, AssetHandle meshlet);  // Gives an entity a meshlet mesh to draw, in place of any ordinary mesh it had
static AssetHandle CreateClusterMaterial(uint seed = 0);        // Makes a material that draws each cluster of a meshlet mesh in a color of its own
static string MeshPathOf(Entity entity);                        // Where an entity's mesh was loaded from, or empty when it was not loaded from anywhere
static string MaterialPathOf(Entity entity);                    // Where an entity's material was loaded from, or empty when it was not
static AssetHandle MeshOf(EcsWorld world, Entity entity);       // The mesh an entity is drawn with, or None when it has none
static AssetHandle MaterialOf(EcsWorld world, Entity entity);   // The standard material an entity is drawn with, or None when it has none, as MeshOf reads the mesh
static bool IsDrawn(Entity entity);                             // Whether an entity carries a mesh the renderer draws
static AssetHandle CreateExtrusion(ReadOnlySpan<Vec2> corners, float depth);  // Builds a convex polygon pushed out into a solid depth deep along Z, centered on its middle, from its corners in order around it
static void SetSceneField(int cascades, float cellSize = 0.25f, int budget = 1, float farCascade = 0f);  // Builds a distance field of the scene around the camera, in one to eight cascades, the finest's cells a given width and a given number built a frame at most, with a far cascade after them where asked, or turns it off with none, which it is by default
static void SetSceneFieldShadows(FieldShadows shadows);         // Chooses where the scene's distance field darkens the light of each directional light it closes a surface off from, past the light's shadow map's reach by default, everywhere, or nowhere
static void DrawSceneFieldWindow();                             // Draws the scene's distance field in an ImGui window of its own, what it is built from and a slice across one of its cascades, with its settings to change while it is looked at
static string[]? MorphTargetNames(AssetHandle mesh);            // The names a mesh gives its morph targets, in their order, empty where it names none, or null while it loads
static void SetMeshJoints(AssetHandle mesh, ReadOnlySpan<ushort> joints, ReadOnlySpan<float> weights);  // Gives a mesh made in code the joints each vertex follows and how much each of them moves it, which a skin then bends it by
static AssetHandle CreateSkin(ReadOnlySpan<Transform> inverseBindposes);  // Makes a skin, Bevy's inverse bindposes, one for each joint, which SetSkin binds an entity's mesh to its joints with
static void SetSkin(Entity entity, AssetHandle skin, ReadOnlySpan<Entity> joints);  // Skins the mesh an entity draws with a skin and the joints that move it, Bevy's SkinnedMesh

// MeshShape
static string Ring(string outline);                             // A flat band along the inside of another flat shape's outline, sized by that shape's measures and, third, how wide the band is
static string Ring(string outline, float thickness);            // A band along the inside of another flat shape's outline, as Ring, its width given here rather than as the third measure, which leaves that free for an Extrusion of the band to take as its depth
static string Extrusion(string outline);                        // A flat shape pushed out into a solid along Z, sized by that shape's measures and, third, how deep it is, centered on its middle
static string UvAngle(string arc);                              // A sector or a segment whose image is mapped at an angle, sized by its radius, the half angle it spans and, third, the angle in radians

// Render2d
static Entity SpawnCamera2d(int order = 0);                     // Spawns a 2D camera and returns it
static AssetHandle CreateAtlas(uint tileWidth, uint tileHeight, uint columns, uint rows, (uint X, uint Y) padding = default, (uint X, uint Y) offset = default);  // Builds an atlas layout over a grid of equal tiles and returns it
static PackedAtlas? PackAtlas(ReadOnlySpan<AssetHandle> images, (uint X, uint Y) padding = default);  // Packs images into one atlas as the app runs, as Bevy's TextureAtlasBuilder does, or answers null while any of them is still loading
static void SetSprite(EcsWorld world, Entity entity, AssetHandle image);  // Attaches a sprite to an entity, or replaces the one it has
static void SetSprite(EcsWorld world, Entity entity, AssetHandle image, SpriteSettings settings);  // Attaches a sprite drawn as settings describes
static int SetSpriteFrames(ReadOnlySpan<Entity> sprites, ReadOnlySpan<uint> frames);  // Moves sprites and sprite meshes to frames of their sheets, each to the frame beside it, in one call
static AssetHandle CreateMaterial(ColorMaterialSettings settings);  // Makes a 2D mesh's material and returns it
static void WriteMaterial(AssetHandle material, ColorMaterialSettings settings);  // Writes settings over a 2D mesh's material in place, so every mesh drawn with it changes
static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh);  // Gives an entity a mesh for a 2D camera to draw
static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material);  // Gives an entity a 2D material to draw its mesh with, a color material from CreateMaterial or a shader material from CreateMaterial2d
static void SetTilemap(EcsWorld world, Entity entity, TilemapChunk chunk, ReadOnlySpan<TileData?> tiles);  // Makes an entity a tilemap chunk with its tiles, Bevy's TilemapChunk and TilemapChunkTileData
static void SetTiles(EcsWorld world, Entity entity, int start, ReadOnlySpan<TileData?> tiles);  // Writes tiles over a chunk's from start on, which Bevy draws from the next frame
static TileData? TileAt(EcsWorld world, Entity entity, int index);  // A chunk's tile at index, or null for an empty cell

// CapturedImage
(byte R, byte G, byte B, byte A) At(uint x, uint y);            // The color at a point, as four bytes
byte[] ToPng();                                                 // The picture as a PNG file's bytes, alpha included

// CapturedTexels
Vector4 ColorAt(uint x, uint y);                                // The color at a point as four floats, linear for a float format and the stored value divided by 255 for an eight-bit one

// EffectSettings
EffectSettings Through(PhysicalLens lens);                      // Takes the depth of field's aperture and sensor from a lens, the one SetLens meters the same camera from

// Picking
static Entity[] Drain();                                        // Takes the scene entities clicked since the last call, none where meshes are not picked
static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal);  // The nearest mesh a ray meets, where, and which way the surface there faces
static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal, out Vec2? uv);  // The nearest mesh a ray meets, as TryCast says, and where on its texture
static PointerId SpawnPointer();                                // Makes a pointer of the game's own, which it moves and presses itself
static void MovePointer(PointerId pointer, AssetHandle image, Vec2 position);  // Moves a pointer from SpawnPointer to a place on an image
static void PressPointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary);  // Presses a button of a pointer from SpawnPointer where it is put
static void ReleasePointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary);  // Lets a button of a pointer from SpawnPointer go where it is put
static void CapturePointer(PointerId pointer, Entity entity, PointerHit hit);  // Locks a pointer to an entity, so the entity is all the pointer is over until the capture is released or the pointer's button is let go
static void ReleaseCapture(PointerId pointer);                  // Releases what a pointer was locked to by CapturePointer, which does nothing where it was not locked
```

## Modeling

The guide's page is [modeling.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/modeling.md).

```csharp
// EditMesh
EditMesh Bevel(float width, int segments = 1);                  // Cuts the selected edges off by a width, each a strip of faces across a round profile, the corners where they meet closed
EditMesh Epsilon(float distance);                               // Sets how near a point is to a plane to lie in it, for the booleans
EditMesh Union(string path, Vec3 at = default);                 // Adds another model's solid, made from its file and moved by an offset, or a solid primitive, joined into this one where they overlap
EditMesh Subtract(string path, Vec3 at = default);              // Takes another model's solid, made from its file and moved by an offset, or a solid primitive, away from this one
EditMesh Intersect(string path, Vec3 at = default);             // Keeps what this solid and another model's, made from its file and moved by an offset, or a solid primitive, both hold
EditMesh Cut(string primitive);                                 // Takes a solid primitive, made where its line places it, away from this mesh
EditMesh Window(float width, float height, float along, float sill);  // Cuts a window through the wall whose face was selected first, measured on every selected face in its plane, as deep as the wall
EditMesh Door(float width, float height, float along);          // Cuts a door through the wall whose face was selected first, from its bottom, as deep as the wall
EditMesh Room(Vec3 size, float thickness, Vec3 at = default, bool centered = false);  // Adds a room, a box with its inside hollowed out by a thickness of wall, floor and ceiling
EditMesh Ramp(float rise, float run, float width, Vec3 at = default, bool centered = false);  // Adds a ramp rising along Z, a wedge given in the words of a flight of stairs
Vec3 Position(int vertex);                                      // Where a vertex is
ReadOnlySpan<int> Corners(int face);                            // A face's vertices, in order round it, counter-clockwise seen from the side it faces
string SlotOf(int face);                                        // The name of a face's material slot
float SmoothOf(int face);                                       // The angle in degrees up to which a face is smoothed into those beside it, or zero where it is drawn flat
Vec3 Normal(int face);                                          // The way a face faces, a length of one, or zero for a face with no area
Vec3 Center(int face);                                          // The middle of a face, the mean of its corners
IEnumerable<(int, int)> Edges();                                // The edges, each once, its ends in the order of their indices
EditMesh Clear();                                               // Takes away every vertex, face and slot but the first, and the selection with them
EditMesh Bridge();                                              // Takes away the two selected faces, or two regions of them, and joins their rims with a band of four sided faces
EditMesh Fill();                                                // Closes each hole whose rim's edges are selected with one face
EditMesh DissolveFaces();                                       // Joins each region of selected faces into one face, its rim its corners, the vertices inside it taken away
EditMesh DissolveEdges();                                       // Takes away the selected edges, each two faces either side joined into one
string Export(string path, bool embedded = false);              // Writes the mesh, built with its modifiers, and its slots' materials as a glTF 2.0 file
EditMesh Import(string path);                                   // Adds the meshes of a glTF file, read in the engine's managed reader, to this mesh as faces, and selects them
EditMesh Knife(Vec3 axis, Vec3? at = null);                     // Cuts the selected faces in two where a plane crosses them
EditMesh Mirror(Vec3 axis, Vec3? at = null);                    // Copies the selected faces mirrored across a plane, the vertices on it shared by both halves
EditMesh Spin(Vec3 axis, float degrees, int steps, Vec3? at = null);  // Sweeps the selected edges round an axis in steps, a face between each step and the next for each edge
EditMesh Smooth(float factor, int repeat = 1);                  // Moves each selected vertex toward the mean of its neighbors along the edges, by a factor, as many times as asked
EditMesh AddModifier(string modifier);                          // Adds a modifier at the end of the stack, applied to a copy of the mesh each time it is built
EditMesh MoveModifier(int from, int to);                        // Moves a modifier to another place in the stack, the others closing up behind it
EditMesh RemoveModifier(int index);                             // Takes a modifier out of the stack, leaving the base as it is
EditMesh ApplyModifier(int index);                              // Bakes a modifier into the base, as though its operation were run on the whole mesh, and takes it out of the stack
EditMesh Built();                                               // A copy of the mesh with its modifiers applied in order, what is drawn and collided with
EditMesh Box(Vec3 size, Vec3 at = default, bool centered = false);  // Adds a box
EditMesh Plane(float width, float depth, Vec3 at = default, bool centered = false);  // Adds a flat rectangle facing up
EditMesh Cylinder(float radius, float height, int sides, Vec3 at = default, bool centered = false);  // Adds a cylinder standing along Y, its round side made of flat sides
EditMesh Cone(float radius, float height, int sides, Vec3 at = default, bool centered = false);  // Adds a cone standing along Y, its point at the top over the middle of its base
EditMesh Wedge(Vec3 size, Vec3 at = default, bool centered = false);  // Adds a wedge, a box cut through from one bottom edge to the opposite top edge, rising along Z
EditMesh Stairs(int steps, float rise, float run, float width, Vec3 at = default, bool centered = false);  // Adds a flight of stairs rising along Z, closed underneath
EditMesh Arch(float radius, float thickness, float depth, int sides, Vec3 at = default, bool centered = false);  // Adds an arch, half a ring standing on its two feet, its opening along Z
bool Undo();                                                    // Takes back the last operation, by making the mesh again from the lines before it
bool Redo();                                                    // Does again the operation last taken back
EditMesh Run(string line);                                      // Runs one line of a recipe on the mesh, as though its operation were called
static EditMesh FromRecipe(string text, string name = "the recipe");  // Makes a mesh from a recipe's text, its lines run in order from an empty mesh
static EditMesh Load(string path);                              // Makes a mesh from a recipe's file, read as the game reads its assets
string Save(string path);                                       // Writes the recipe to a file, from which Load makes the mesh again
bool IsVertexSelected(int vertex);                              // Whether a vertex is selected
bool IsFaceSelected(int face);                                  // Whether a face is selected
EditMesh SelectAll();                                           // Selects every vertex and every face
EditMesh SelectNone();                                          // Selects nothing
EditMesh SelectInvert();                                        // Selects what was not selected, and leaves what was
EditMesh SelectFaces(IEnumerable<int> faces, SelectionMode mode = SelectionMode.Replace);  // Selects faces by their indices
EditMesh SelectEdges(IEnumerable<(int, int)> edges, SelectionMode mode = SelectionMode.Replace);  // Selects edges by their two ends, which selects the ends
EditMesh SelectFacing(Vec3 direction, SelectionMode mode = SelectionMode.Replace);  // Selects the faces that face nearest one way along an axis, such as up
EditMesh SelectInside(Vec3 corner, Vec3 opposite, SelectionMode mode = SelectionMode.Replace);  // Selects the vertices inside a box, the faces all of whose corners are inside it, and the edges between
EditMesh SelectSlot(string slot, SelectionMode mode = SelectionMode.Replace);  // Selects the faces in a material slot
SlotLook LookOf(string slot);                                   // What a slot is drawn with, nothing given where it is the prototype material of its name
EditMesh SlotAdd(string slot);                                  // Adds a slot no face is in yet, drawn with the prototype material of its name
EditMesh SlotRename(string slot, string name);                  // Gives a slot another name, its faces and its look kept
EditMesh SlotRemove(string slot);                               // Takes a slot away, its faces given the first slot
EditMesh SlotColor(string slot, float red, float green, float blue, float alpha = 1f);  // Gives a slot a color, which tints its texture, or its grid where it has no texture
EditMesh SlotTexture(string slot, string? path);                // Gives a slot a picture its faces show, in the grid's place, or takes it away
EditMesh SlotNormalMap(string slot, string? path);              // Gives a slot a normal map, or takes it away
EditMesh SlotOcclusionMap(string slot, string? path);           // Gives a slot an ambient occlusion map, or takes it away
EditMesh SlotMetallicRoughnessMap(string slot, string? path);   // Gives a slot a map of metalness and roughness, or takes it away
EditMesh SlotEmissive(string slot, float red, float green, float blue);  // Gives a slot an emissive color
EditMesh SlotEmissiveMap(string slot, string? path);            // Gives a slot an emissive map, or takes it away
EditMesh SlotMetallic(string slot, float metallic);             // Sets how metal a slot's faces are
EditMesh SlotRoughness(string slot, float roughness);           // Sets how rough a slot's faces are
EditMesh LoopCut(float fraction);                               // Cuts the ring of four sided faces across the selected edge in two, along a loop through each of them
EditMesh Subdivide(int cuts = 1);                               // Cuts each edge of the selected faces into equal parts and each face into smaller ones through the cuts
EditMesh Extrude(float distance);                               // Pushes the selected faces out along their normals, the sides between them and what they left filled
EditMesh Extrude(Vec3 by);                                      // Pushes the selected faces out by one vector, the sides between them and what they left filled
EditMesh Inset(float distance);                                 // Makes each selected face a smaller face inside it, with a band of faces round it
EditMesh Delete(bool fill = false);                             // Takes away the selected faces, or where none is selected the selected vertices with every face using them
EditMesh Merge(float distance);                                 // Joins the selected vertices nearer one another than a distance into one, where the first of them is
EditMesh Flip();                                                // Turns the selected faces to face the other way, their corners run backwards
EditMesh Triangulate();                                         // Cuts each selected face of more than three corners into triangles, as the drawing cuts it
EditMesh Duplicate();                                           // Copies the selected faces where they are, with vertices of their own, and selects the copy
EditMesh Join(string path);                                     // Adds another model's mesh, made from its recipe's file, to this one, and selects it
EditMesh Shade(bool smooth, float degrees = 30f);               // Draws the selected faces smoothed into one another, or flat
EditMesh Move(Vec3 by);                                         // Moves the selected vertices
EditMesh Rotate(Vec3 axis, float degrees, Vec3? pivot = null);  // Turns the selected vertices about an axis
EditMesh Scale(Vec3 factor, Vec3? pivot = null);                // Scales the selected vertices toward or away from a point
EditMesh Scale(float factor, Vec3? pivot = null);               // Scales the selected vertices by one factor along every axis
EditMesh Grid(float step);                                      // Sets the grid step a point picked by hand lands on and Snap rounds to
EditMesh Snap();                                                // Puts each selected vertex on the nearest point of the grid
EditMesh Material(string slot);                                 // Puts the selected faces in a material slot, which is made where the mesh has none of that name
Vec2[] UvsOf(int face);                                         // The texture coordinates of a face's corners, in their order, its own or the world-aligned default
bool HasOwnUvs(int face);                                       // Whether a face has texture coordinates of its own, given by a uv operation, rather than the world-aligned default
EditMesh UvPlanar(Vec3 facing);                                 // Projects the selected faces' coordinates along a direction, as a picture shone onto them from that side
EditMesh UvBox();                                               // Gives the selected faces the world-aligned default as coordinates of their own, which then follow their corners
EditMesh UvCylinder(Vec3 axis, Vec3? center = null);            // Wraps the selected faces' coordinates round an axis, as a label round a can
EditMesh UvSphere(Vec3? center = null);                         // Wraps the selected faces' coordinates round a point, as a map of the world round a globe
EditMesh UvMove(float u, float v);                              // Moves the selected faces' coordinates
EditMesh UvScale(float u, float v, bool each = false);          // Scales the selected faces' coordinates about their middle, so a picture is shown smaller where they grow
EditMesh UvScale(float factor, bool each = false);              // Scales the selected faces' coordinates by one factor about their middle
EditMesh UvRotate(float degrees, bool each = false);            // Turns the selected faces' coordinates about their middle
EditMesh UvDensity(float pixelsPerMeter, int texturePixels);    // Scales the selected faces' coordinates about their middle so a texture of a size shows so many of its pixels a meter
EditMesh UvReset();                                             // Puts the selected faces back on the world-aligned default

// Modeling
static MeshData[] Build(EditMesh mesh);                         // Builds the mesh, its modifiers applied, into one mesh's data for each material slot, in the order of the slots
static AssetHandle[] Meshes(EditMesh mesh);                     // The meshes the mesh is drawn with, one for each material slot, made the first time and written again in place once it has changed, so an entity drawn with one follows the edits
static EditMesh? Source(AssetHandle mesh);                      // The edit mesh a mesh of Meshes was built from, or nothing for a mesh it did not build
static PhysicsShape Collider(EditMesh mesh);                    // The mesh's triangles as a shape a static body collides as, its faces as one surface whatever their slots
static AssetHandle[] Materials(EditMesh mesh);                  // The material each slot of a mesh is drawn with, in the order of its slots
static AssetHandle Material(string name, SlotLook look);        // The material a look makes, for a slot of a name
static AssetHandle Prototype(string name);                      // The prototype material of a part of a level, the grid tinted by its color
static AssetHandle Prototype(Color color);                      // A prototype material of any color, the grid tinted by it
static void Register(string name, EditMesh mesh);               // Gives a mesh a name the console's model commands reach it by, so a game's level is shaped from the terminal and by a drive, model.cut level window 1 1 2 0.9 cutting a window in it
static EditMesh? Find(string name);                             // The model of a name, or nothing where none has it
static void DrawWindow(BehaviorContext ctx, EditMesh mesh, Entity camera = default, Transform? placed = null);  // Draws the modeling window on a mesh, and takes the pointer in the 3D view, where a camera is given, to pick its faces and extrude or move them
static void DrawPanel(BehaviorContext ctx, EditMesh mesh, Entity camera = default, Transform? placed = null);  // Draws the modeling window's contents where the program has begun a window or a panel of its own, as the editor's Model tab does, and takes the pointer in the 3D view as DrawWindow does

// Images
static AssetHandle Grid();                                      // Makes the grid of a meter's cell, in its default colors and size
static AssetHandle Grid(int pixelsPerMeter);                    // Makes the grid of a meter's cell, in its default colors, at a size
static AssetHandle Grid(int pixelsPerMeter, Color face, Color line, Color fine);  // Makes the image of one meter's cell of a grid, its edges a line and a finer line every tenth of a meter, to tile a level's faces whose texture coordinates are in meters
```

## Shaders

The guide's page is [shaders.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/shaders.md).

```csharp
// Shaders
static AssetHandle CreateBuffer(int size);                      // Makes a buffer of size bytes that shaders read and write, holding zeros
static AssetHandle CreateBuffer<T>(ReadOnlySpan<T> items, int size = 0);  // Makes a buffer holding items, at least size bytes long
static void WriteBuffer<T>(AssetHandle buffer, ReadOnlySpan<T> items);  // Replaces a buffer's contents with items, padded with zeros to its size
static int GrowBuffer(AssetHandle buffer, int size);            // Makes a buffer at least size bytes, keeping what it holds, and answers its size
static AssetHandle CreateInstanceBuffer(int capacity);          // Makes a buffer with capacity slots that the engine fills every frame with the transforms of the entities put in them, this frame's and the previous frame's
static GeometryPool CreateGeometryPool();                       // Makes an empty geometry pool, buffers holding the vertices and triangles of every mesh added to it, which any shader reaches by a mesh's number and a triangle's
static int AddToGeometryPool(GeometryPool pool, AssetHandle mesh);  // Adds a mesh to a geometry pool and answers its number there, counted from zero, which is the index of its PoolMesh
static RayScene CreateRayScene(GeometryPool pool, int capacity);  // Makes a scene rays are traced through, over the meshes of a geometry pool, with capacity slots for the entities in it
static void SetRaySceneInstance(RayScene scene, int slot, Entity entity, int mesh);  // Puts an entity made of pool mesh mesh in a slot of a ray scene, where rays meet it at its transform every frame, or empties the slot with None
static void RebuildRayScene(RayScene scene, int? mesh = null);  // Builds a ray scene's pool mesh again from what the pool's buffers hold now, or every mesh with null
static AssetHandle CreateMaterialBuffer(int capacity);          // Makes a buffer with capacity slots that the engine fills every frame with what the entities put in them are made of, from their standard materials
static void SetInstance(AssetHandle buffer, int slot, Entity entity);  // Puts an entity in a slot of an instance or material buffer, or with None empties the slot
static int BufferSize(AssetHandle buffer);                      // A buffer's size in bytes
static BufferRead BeginBufferRead(AssetHandle buffer);          // Starts copying a buffer back from the GPU. Only valid inside a system
static BufferRead BeginImageRead(AssetHandle image);            // Starts copying an image back from the GPU, as BeginBufferRead copies a buffer
static bool TryReadBuffer(BufferRead read, out byte[]? bytes);  // The bytes a read brought back, once they have arrived
static bool TryReadBuffer<T>(BufferRead read, out T[]? items);  // The elements a read brought back, once they have arrived
static AssetHandle CreateImage(uint width, uint height, ShaderImageFormat format = ShaderImageFormat.Rgba8, uint depth = 1, uint mips = 1);  // Makes an image a compute shader writes and anything samples
static AssetHandle CreateImage<T>(uint width, uint height, ShaderImageFormat format, ReadOnlySpan<T> texels, uint depth = 1);  // Makes an image as CreateImage does, starting with texels rather than zeros
static void WriteImage<T>(AssetHandle image, ReadOnlySpan<T> texels, uint x, uint y, uint width, uint height, uint z = 0, uint depth = 1, uint mip = 0);  // Writes texels into a region of an image, width by height at x, y, on the GPU before this frame's work runs
static int TexelBytes(ShaderImageFormat format);                // How many bytes one texel of format takes, or for a block-compressed format one four by four block
static ShaderProgram CreateProgram(ShaderStage fragment);       // Makes a program drawn by one fragment shader, leaving the rest to Bevy
static ShaderProgram CreateProgram(ShaderProgramSettings settings);  // Makes a program from the Slang named
static ShaderMaterial CreateMaterial(ShaderProgram program, AlphaMode alpha = AlphaMode.Opaque);  // Makes a material drawn by a program
static ShaderMaterial CreateMaterial(ShaderMaterialSettings settings);  // Makes a material drawn by a program
static ShaderMaterial CreateMaterial2d(ShaderProgram program, AlphaMode2d? alpha = null, float cutoff = 0.5f);  // Makes a material a program draws a 2D mesh or a sprite with
static ShaderMaterial CreateUiMaterial(ShaderProgram program);  // Makes a material a program draws an interface node with
static ShaderMaterial MaterialOn(Entity entity);                // The shader material an entity is drawn with, to read or set its values
static ShaderProgram ProgramOn(Entity entity);                  // Which program draws an entity's material, or None where the entity is not drawn by one
static ShaderInstance CreateInstance(ShaderProgram program);    // Makes an instance of a program, which a pass over a camera's picture or a compute dispatch runs
static void SetPasses(Entity camera, params ShaderPass[] passes);  // Replaces the full-screen passes a camera runs over what it drew, in the order given
static void SetPrepass(Entity camera, bool depth, bool normals = false, bool motion = false, bool deferred = false, bool previous = false, bool pyramid = false);  // Asks a camera to draw its depth, its normals, its motion vectors or any of them before the scene, for its passes and its compute shaders to read
static void SetViewImages(Entity camera, params ViewImage[] images);  // Gives a camera images that its passes and compute shaders keep from frame to frame, replacing any it had
static IReadOnlyList<string> DrawnViewImageNames(Entity camera);  // The names a Watch on the camera would find, as of the last frame it drew, its own images and those of EngineViewImageNames it has
static IReadOnlyList<string> ViewImageNames(Entity camera);     // The names of the images a camera owns, as SetViewImages last gave them, with their _previous and _mip names
static AssetHandle Watch(Entity camera, string name, uint width = 320, uint height = 180, float scale = 1f, float offset = 0f);  // Starts watching one of a camera's images
static void Unwatch(Entity camera, string name);                // Stops watching one of a camera's images
static void SetViewDispatches(Entity camera, params ViewDispatch[] dispatches);  // Replaces the compute shaders a camera runs every frame, in order
static void Dispatch(ShaderInstance instance, uint x, uint y = 1, uint z = 1);  // Runs an instance's compute shader once, this frame, before any camera draws
static void SetViewDraws(Entity camera, params ViewDraw[] draws);  // Replaces the geometry a camera draws every frame out of buffers, in order
static void DispatchIndirect(ShaderInstance instance, AssetHandle buffer, uint offset = 0);  // Runs an instance's compute shader once, this frame, before any camera draws, with as many workgroups as the buffer says

// ShaderValues
static T Set<T>(T target, string name, float value);            // Sets a float
static T Set<T>(T target, string name, int value);              // Sets a float
static T Set<T>(T target, string name, uint value);             // Sets a float
static T Set<T>(T target, string name, bool value);             // Sets a float
static T Set<T>(T target, string name, Vector2 value);          // Sets a float
static T Set<T>(T target, string name, Vector3 value);          // Sets a float
static T Set<T>(T target, string name, Vector4 value);          // Sets a float
static T Set<T>(T target, string name, Quaternion value);       // Sets a float
static T Set<T>(T target, string name, Matrix4x4 value);        // Sets a float
static T Set<T, TItem>(T target, string name, ReadOnlySpan<TItem> items);  // Sets a float
static T Set<T, TItem>(T target, string name, TItem[] items);   // Sets a float
static T SetNumbers<T, TItem>(T target, string name, int components, ReadOnlySpan<TItem> numbers);  // Sets numbers components to an element, for the shapes C# has no type for, such as an int2, a uint3, a float3x3, or an array of any of them
static T SetBytes<T, TItem>(T target, string name, ReadOnlySpan<TItem> items);  // Copies bytes, as they are, to where a name is, a struct, an array of structs, or a whole ConstantBuffer
static T SetStruct<T, TItem>(T target, string name, TItem value);  // Sets a struct, as its bytes
static T SetTexture<T>(T target, string name, AssetHandle image, int index = 0, int mip = -1);  // Puts an image on a texture, at index in an array of them, or takes it off again with None
static T SetRayScene<T>(T target, string name, RayScene scene);  // Puts a RayScene on an acceleration structure the shader traces, or takes it off again with default
static T SetBuffer<T>(T target, string name, AssetHandle buffer);  // Puts a buffer from CreateBuffer on a storage buffer, or takes it off again with None
static T SetSampler<T>(T target, string name, SamplerSettings settings, int index = 0);  // Says how a sampler reads, at index in an array of them
static T Unset<T>(T target, string name);                       // Takes the value set under a name off, so the shader reads zeros or a stand-in there again
static float[] GetFloats<T>(T target, string name);             // The floats set under a name, or an empty array where nothing was
static int[] GetInts<T>(T target, string name);                 // The signed integers set under a name
static uint[] GetUInts<T>(T target, string name);               // The unsigned integers set under a name

// ShaderMaterial
ShaderMaterial Configure(AlphaMode alpha, float cutoff = 0.5f, CullMode cull = CullMode.Back, float depthBias = 0f);  // Changes what the renderer does where the material is not opaque, which faces it leaves undrawn and how far its depth is pushed toward the camera

// ShaderProgram
void Reload();                                                  // Compiles every stage again now, whether a file changed or not

// ShaderStage
static ShaderStage Slang(string source, string? entry = null);  // A stage made from Slang handed over as text

// ViewDispatch
static ViewDispatch PerPixel(ShaderInstance instance, FramePoint point, uint groupX = 8, uint groupY = 8, float scale = 1f);  // Enough workgroups of groupX by groupY pixels to cover scale of the picture, for a shader working a pixel at a time
static ViewDispatch Fixed(ShaderInstance instance, FramePoint point, uint x, uint y = 1, uint z = 1);  // Exactly this many workgroups
static ViewDispatch Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0);  // As many workgroups as three unsigned integers in a buffer say, when it runs

// ViewDraw
static ViewDraw Fixed(ShaderInstance instance, FramePoint point, uint vertices, uint instances = 1, DrawBlend blend = DrawBlend.Opaque, bool writesDepth = true);  // vertices vertices, instances times
static ViewDraw Meshes(ShaderInstance instance, FramePoint point, uint x, uint y = 1, uint z = 1, DrawBlend blend = DrawBlend.Opaque, bool writesDepth = true);  // x by y by z workgroups of a program's task shader, or of its mesh shader where it has no task shader, for a program drawing with mesh shaders (DrawMesh)
static ViewDraw Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0, DrawBlend blend = DrawBlend.Opaque, bool writesDepth = true);  // As many vertices and instances as four unsigned integers in a buffer say when it runs, vertices, instances, the first vertex and the first instance

// ComponentArray<T>
int PlaceOf(Entity entity);                                     // Where an entity's entry is, its mesh tag, or -1 where it has none
```

## Gizmos

The guide's page is [gizmos.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/gizmos.md).

```csharp
// Gizmos
static void Fade(Vec3 start, Vec3 end, (float R, float G, float B, float A) from, (float R, float G, float B, float A) to, bool inFront = true);  // Draws a line that fades from one color to another along its length
static void Line(Vec3 start, Vec3 end, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a line between two points
static void Sphere(Vec3 center, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a sphere
static void Sphere(Vec3 center, Quat rotation, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a sphere, turned, which turns its three circles with it
static void Arrow(Vec3 start, Vec3 end, (float R, float G, float B, float A) color, bool inFront = true, float tipLength = 0f, bool doubleEnd = false);  // Draws a line with a head on its far end
static void Circle(Vec3 center, Quat rotation, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a circle
static void Ellipse(Vec3 center, Quat rotation, float halfWidth, float halfHeight, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of an ellipse
static void Arc(Vec3 center, Quat rotation, float radius, float angle, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws part of a circle
static void Rect(Vec3 center, Quat rotation, float width, float height, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a rectangle
static void RoundedRect(Vec3 center, Quat rotation, float width, float height, (float R, float G, float B, float A) color, float? cornerRadius = null, bool inFront = true, uint arcResolution = 0);  // Draws the outline of a rectangle with rounded corners
static void Box(Vec3 center, Quat rotation, Vec3 size, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the twelve edges of a box
static void RoundedCuboid(Vec3 center, Quat rotation, Vec3 size, (float R, float G, float B, float A) color, float? edgeRadius = null, bool inFront = true, uint arcResolution = 0);  // Draws the edges of a box with rounded edges and corners
static void Capsule(Vec3 center, Quat rotation, float radius, float length, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a capsule
static void Cone(Vec3 center, Quat rotation, float radius, float height, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a cone
static void Cylinder(Vec3 center, Quat rotation, float radius, float halfHeight, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a cylinder
static void Torus(Vec3 center, Quat rotation, float major, float minor, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a torus
static void Grid(Vec3 center, Quat rotation, uint across, uint down, float spacing, (float R, float G, float B, float A) color, bool inFront = false, float spacingDown = 0f);  // Draws a flat grid of lines
static void Frustum(Vec3 center, Quat rotation, float bottom, float top, float height, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a cone with its point cut off
static void Rect2d((float X, float Y) center, float width, float height, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of a rectangle, flat, for a 2D camera
static void RoundedRect2d((float X, float Y) center, float width, float height, (float R, float G, float B, float A) color, float? cornerRadius = null, float angle = 0f, bool inFront = true, uint arcResolution = 0);  // Draws the outline of a rectangle with rounded corners, flat, for a 2D camera
static void Circle2d((float X, float Y) center, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a circle, flat, for a 2D camera
static void Ellipse2d((float X, float Y) center, float halfWidth, float halfHeight, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true, uint resolution = 0);  // Draws the outline of an ellipse, flat, for a 2D camera
static void Line2d((float X, float Y) start, (float X, float Y) end, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a line between two points, flat, for a 2D camera
static void Line2d((float X, float Y) start, (float X, float Y) end, (float R, float G, float B, float A) from, (float R, float G, float B, float A) to, bool inFront = true);  // Draws a line that fades from one color to another, flat, for a 2D camera
static void Arrow2d((float X, float Y) start, (float X, float Y) end, (float R, float G, float B, float A) color, bool inFront = true, float tipLength = 0f, bool doubleEnd = false);  // Draws a line with a head on its far end, flat, for a 2D camera
static void Arc2d((float X, float Y) center, float radius, float angle, (float R, float G, float B, float A) color, float from = 0f, bool inFront = true, uint resolution = 0);  // Draws part of a circle, flat, for a 2D camera
static void Grid2d((float X, float Y) center, uint across, uint down, float spacing, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = false, float spacingDown = 0f);  // Draws a grid of lines, flat, for a 2D camera
static void Plane2d((float X, float Y) point, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws a plane, flat, for a 2D camera, as Bevy's primitive_2d draws its Plane2d: a line through point and an arrow for the side it faces
static void Plane(Vec3 center, Quat rotation, float halfWidth, float halfDepth, (float R, float G, float B, float A) color, bool inFront = false);  // Draws a plane as Bevy's primitive_3d draws its Plane3d, the arrow it faces along and lines across it, facing up before rotation turns it
static void EndlessLine2d((float X, float Y) point, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws a line with no ends, flat, for a 2D camera, as Bevy's primitive_2d draws its Line2d: a line far out both ways through point and an arrow along it
static void EndlessLine(Vec3 point, Quat rotation, (float R, float G, float B, float A) color, bool inFront = false);  // Draws a line with no ends, as Bevy's primitive_3d draws its Line3d, along X before rotation turns it
static void Capsule2d((float X, float Y) center, float radius, float halfLength, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of a capsule, flat, for a 2D camera, its length along Y
static void Annulus2d((float X, float Y) center, float innerRadius, float outerRadius, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of a ring between two circles, flat, for a 2D camera
static void CircularSector2d((float X, float Y) center, float radius, float halfAngle, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of a slice of a circle, its arc and the two radii to it, flat, for a 2D camera, the arc centered on Y
static void CircularSegment2d((float X, float Y) center, float radius, float halfAngle, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of the part of a circle cut off by a chord, its arc and the chord, flat, for a 2D camera, the arc centered on Y
static void RegularPolygon2d((float X, float Y) center, float radius, int sides, (float R, float G, float B, float A) color, float angle = 0f, bool inFront = true);  // Draws the outline of a polygon of equal sides, flat, for a 2D camera, a corner up
static void Polyline2d(ReadOnlySpan<(float, float)> points, (float R, float G, float B, float A) color, bool closed = false, bool inFront = true);  // Draws lines through points in order, flat, for a 2D camera, closed back to the first where asked
static void Lines(ReadOnlySpan<GizmoSegment> lines, bool inFront = false);  // Draws a whole run of lines in one crossing
static void Polyline(ReadOnlySpan<Vec3> points, (float R, float G, float B, float A) color, bool closed = false, bool inFront = false);  // Draws a line through a run of points, joining the last back to the first where closed is set
static void Triangle(Vec3 a, Vec3 b, Vec3 c, (float R, float G, float B, float A) color, bool inFront = false);  // Draws the outline of a triangle through three points
static void Tetrahedron(Vec3 a, Vec3 b, Vec3 c, Vec3 d, (float R, float G, float B, float A) color, bool inFront = false);  // Draws the six edges of a tetrahedron through four points
static void Tetrahedron(Vec3 center, float size, (float R, float G, float B, float A) color, bool inFront = false);  // Draws a regular tetrahedron standing on its base, centered on a point
static void Configure(float width = 0f, uint layers = 0, bool enabled = true, GizmoGroup which = GizmoGroup.Both);  // Sets how every gizmo is drawn
static void SetLineStyle(GizmoLine style = GizmoLine.Solid, float gapScale = 0f, float lineScale = 0f, GizmoJoint joint = GizmoJoint.None, uint jointResolution = 0, bool perspective = false, GizmoGroup which = GizmoGroup.Both);  // Sets what a gizmo line looks like
static void SetDepthBias(float bias, GizmoGroup which = GizmoGroup.Both);  // Moves a group's gizmos toward the camera or away from it before they are tested against the scene's depth
static void ShowLights(bool all, LightGizmoColoring coloring = LightGizmoColoring.MatchLight, (float R, float G, float B, float A) color = default);  // Sets whether Bevy draws the shape of every light, and how it colors them
static void ShowBounds(bool all, (float R, float G, float B, float A)? color = null);  // Sets whether Bevy draws every entity's bounding box, and in what color
static void ShowFrusta(bool all, (float R, float G, float B, float A)? color = null);  // Sets whether Bevy draws the frustum of every entity that has one, and in what color
static void ShowSkinnedBounds(bool all, (float R, float G, float B, float A)? color = null);  // Sets whether Bevy draws the bounds of every skinned mesh's joints, and in what color
static AssetHandle Record(Action draw);                         // Keeps the shapes draw asks for in an asset, rather than drawing them for one frame, for an entity to draw every frame with Attach
static void Attach(EcsWorld world, Entity entity, AssetHandle gizmo);  // Has an entity draw a gizmo asset Record made, every frame, placed by the entity's transform
static void Axes(Transform transform, float length = 1f, bool inFront = true);  // Draws a set of axes, which show an orientation
static Gizmos.BatchScope Batch();                               // Gathers every shape drawn until the returned scope ends, and hands them over in one call
static void Text(string text, Vec3 position, Quat rotation, float size, (float X, float Y) anchor, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a run of text in the world, in Bevy's stroke font, facing the way it is turned
static void Text2d(string text, (float X, float Y) position, float angle, float size, (float X, float Y) anchor, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a run of text, flat, for a 2D camera

// Gizmos.BatchScope
void Dispose();                                                 // Hands the gathered shapes to the engine

// GizmoSegment
static GizmoSegment Fading(Vec3 start, Vec3 end, (float R, float G, float B, float A) from, (float R, float G, float B, float A) to);  // A line that fades from one color to another
```

## The interface

The guide's page is [ui.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/ui.md).

```csharp
// Ui
static Entity SpawnNode(UiSettings settings);                   // Spawns a rectangle and returns it
static Entity SpawnText(string text, UiSettings settings, float fontSize = 20f);  // Spawns a run of text and returns it
static Entity SpawnText(string text, UiSettings settings, UiTextSettings style);  // Spawns a run of text, set as style describes
static Entity SpawnTextSpan(Entity parent, string text, UiTextSettings style, (float R, float G, float B, float A) color);  // Adds a run of text to an existing one, set in its own font and color
static void SetText(Entity entity, string text);                // Replaces what a text entity says
static IReadOnlyList<FontAtlasPicture> FontAtlasPictures();     // The picture of each font atlas Bevy holds and the size of the font drawn into it, the smaller sizes first and the atlases of a size in the order Bevy opened them
static void ClearFontAtlases();                                 // Empties the font atlases, which Bevy fills again with the glyphs drawn after
static UiInteraction InteractionOf(Entity entity);              // Reports how the pointer stands on an interactive node
static void SetImage(Entity entity, AssetHandle image);         // Draws an image inside a node, or replaces the one it draws
static void SetMaterial(Entity entity, AssetHandle material);   // Draws a node with a material a shader program draws, from CreateUiMaterial, or replaces the one it draws
static void SetImage(Entity entity, UiImageSettings settings);  // Draws an image inside a node, tinted, cut down or sliced
static void SetScroll(Entity entity, float x, float y);         // Moves a scrolling node's contents inside it
static void Focus(Entity entity, FocusCause cause = FocusCause.Navigated);  // Gives entity the input focus, so the keys go to it
static Entity? Navigate(NavAction action);                      // The entity the focus would move to along the tab order, without moving it
static void SetUnderline(Entity text, bool underlined = true);  // Draws a run of text with a line under it, or takes the line off
static void SetStrikethrough(Entity text, bool struck = true);  // Draws a run of text with a line through it, or takes the line off
static void SetFontFeatures(Entity text, ReadOnlySpan<(string, uint)> features);  // Sets the OpenType features a run of text is drawn with, replacing those it had
static void SetFontVariations(Entity text, ReadOnlySpan<(string, float)> variations);  // Sets where a variable font sits on each of its axes for a run of text, replacing what it had
static void SetEditableText(Entity node, UiEditableTextSettings settings);  // Makes a node a text field the player types into
static string? EditableTextOf(Entity node);                     // What a text field holds, or null for a node that is no field
static void SetTextCursor(Entity field, UiTextCursorSettings settings);  // Gives a text field a cursor and selection of its own
static TextViewport? TextViewportOf(Entity field);              // The part of a text field's text it shows, or null for a node that is no field
static void ScrollText(Entity field, Vec2 offset);              // Scrolls a text field to show its text from a point of its layout
static void SetVisibleLines(Entity field, float lines);         // Sets how many lines tall a text field is, its text and cursor left as they are
static void SetEditableValue(Entity node, string text);         // Replaces what a text field holds, its cursor put at the end
static void EditText(Entity field, TextEdit edit);              // Queues one of Bevy's text edits on a field, applied with those the keyboard makes in the frame, so a button or a virtual key types into a field as a key would
static bool BeginHidden();                                      // Opens the hidden scope, answering whether what is drawn inside it is drawn this frame, false while Hidden holds
static void EndHidden();                                        // Closes the hidden scope BeginHidden opened
static void SelfUpdate(Entity widget, UiWidgetKind kind);       // Makes one of Bevy's widgets keep its own state as the player works it

// UiGrid
static void Set(Entity node, GridSettings settings);            // Lays a node's children out on a grid
static void Place(Entity node, GridPlacement placement);        // Places one child on its parent's grid

// TextEdit
static TextEdit Insert(string text);                            // Puts text at the cursor, in place of the selection where there is one
static TextEdit Left(bool select = false);                      // Moves the cursor a character left, extending the selection where asked
static TextEdit Right(bool select = false);                     // Moves the cursor a character right, extending the selection where asked
static TextEdit Up(bool select = false);                        // Moves the cursor up a line as laid out, extending the selection where asked
static TextEdit Down(bool select = false);                      // Moves the cursor down a line as laid out, extending the selection where asked
static TextEdit LineStart(bool select = false);                 // Moves the cursor to the start of its line as laid out
static TextEdit LineEnd(bool select = false);                   // Moves the cursor to the end of its line as laid out
static TextEdit ScrollByLines(float lines);                     // Scrolls by lines lines as laid out, down where it is above zero

// Feathers
static Entity Button(Entity parent, string caption, ButtonVariant variant = ButtonVariant.Normal, RoundedCorners corners = RoundedCorners.All);  // A button with its caption, Bevy's FeathersButton
static Entity ToolButton(Entity parent, string caption, ButtonVariant variant = ButtonVariant.Normal, RoundedCorners corners = RoundedCorners.All);  // A square button for an icon or a symbol, Bevy's FeathersToolButton
static Entity Checkbox(Entity parent, string caption);          // A checkbox with its caption beside it, Bevy's FeathersCheckbox
static Entity Radio(Entity parent, string caption);             // A radio button with its caption beside it, Bevy's FeathersRadio, one of those under a node carrying Bevy's RadioGroup
static Entity ToggleSwitch(Entity parent);                      // A toggle switch, Bevy's FeathersToggleSwitch, a checkbox drawn as a switch
static Entity Slider(Entity parent, float min = 0f, float max = 1f);  // A slider showing its value, Bevy's FeathersSlider, starting at its least value
static Entity Caption(Entity parent, string text);              // Text in the theme's font and color, as a control's caption is, Bevy's caption
static Entity Label(Entity parent, string text);                // A label beside a control, Bevy's label
static Entity LabelDim(Entity parent, string text);             // A label in the theme's dimmer text, Bevy's label_dim
static Entity LabelSmall(Entity parent, string text);           // A smaller label, Bevy's label_small
static Entity NumberInput(Entity parent, string? letter = null, string? sigil = null);  // A number input, Bevy's FeathersNumberInput, a field the value is typed into or dragged across, with a letter at its left and a strip in a theme color along its edge where given
static Entity VirtualKeyboard(Entity parent, string[][] keys);  // A virtual keyboard, Bevy's VirtualKeyboard, a button a key in rows, which reports the key pressed as VirtualKeyPressed on the keyboard
static Entity Menu(Entity parent);                              // A menu, Bevy's FeathersMenu, which holds its button and its popup, spawned under it with MenuButton and MenuPopup
static Entity MenuButton(Entity menu, string caption, bool arrow = true, RoundedCorners corners = RoundedCorners.All);  // A menu's button, Bevy's FeathersMenuButton, which opens its popup
static Entity MenuToolButton(Entity menu, string caption, bool arrow = true, RoundedCorners corners = RoundedCorners.All);  // A menu's square button for a symbol, Bevy's FeathersMenuToolButton
static Entity MenuPopup(Entity menu);                           // A menu's popup, Bevy's FeathersMenuPopup, its items spawned under it
static Entity MenuItem(Entity popup, string caption);           // A menu's item, Bevy's FeathersMenuItem, which reports Activate
static Entity MenuDivider(Entity popup);                        // A line between a menu's items, Bevy's FeathersMenuDivider
static Entity Select(Entity parent, string[] options, int selected = -1, int maxVisible = 8, RoundedCorners corners = RoundedCorners.All);  // A select, Bevy's FeathersSelect, a button showing the option chosen that opens a list of its options
static Entity ListView(Entity parent, string[] rows, int selected = -1);  // A list view, Bevy's FeathersListView, rows in a column that scrolls past its height, which reports the row clicked as a ValueChange of its Entity
static Entity ListRow(Entity list, string caption);             // A row of a list view, Bevy's FeathersListRow, with its caption
static Entity DisclosureToggle(Entity parent);                  // A toggle drawn as a chevron pointing right or down, Bevy's FeathersDisclosureToggle, a checkbox for showing what is folded under a heading
static Entity TextInputContainer(Entity parent);                // The frame a text input is drawn in, Bevy's FeathersTextInputContainer, its input spawned under it with TextInput
static Entity TextInput(Entity container, float visibleWidth = 0f, int maxCharacters = 0);  // A text input, Bevy's FeathersTextInput, a field as SetEditableText makes one, in the theme's font and colors
static Entity ColorSwatch(Entity parent, float opaquePercentage = 0f, RoundedCorners corners = RoundedCorners.All, float cornerRadius = 5f);  // A swatch showing a color, Bevy's FeathersColorSwatch, the color its reflected ColorSwatchValueRef holds
static Entity ColorPlane(Entity parent, ColorPlaneAxes axes);   // A plane two parts of a color are picked on, Bevy's FeathersColorPlane, which reports the point picked as a ValueChange of a Vec2
static Entity ColorSlider(Entity parent, ColorChannel channel, float value = 0f);  // A slider of one part of a color, Bevy's FeathersColorSlider, which reports the value asked for as a ValueChange of a float
static Entity ColorInput(Entity parent, float opaquePercentage = 0f);  // A color input, Bevy's FeathersColorInput, a swatch that opens a picker of a wheel, planes and sliders, and reports the color picked as a ValueChange of a Color
static Entity Pane(Entity parent);                              // An editor's pane, Bevy's pane, holding its header and its body
static Entity PaneHeader(Entity pane);                          // A pane's header, Bevy's pane_header, a row of its tool buttons
static Entity PaneHeaderDivider(Entity header);                 // A line between a pane header's buttons, Bevy's pane_header_divider
static Entity PaneBody(Entity pane);                            // A pane's body, Bevy's pane_body
static Entity Subpane(Entity parent);                           // A pane within a pane, Bevy's subpane, holding its header and its body
static Entity SubpaneHeader(Entity subpane);                    // A subpane's header, Bevy's subpane_header
static Entity SubpaneBody(Entity subpane);                      // A subpane's body, Bevy's subpane_body
static Entity Group(Entity parent);                             // A group of properties, Bevy's group, holding its header and its body
static Entity GroupHeader(Entity group);                        // A group's header, Bevy's group_header
static Entity GroupBody(Entity group);                          // A group's body, Bevy's group_body
static Entity FlexSpacer(Entity parent);                        // A node that takes the room left in its row, Bevy's flex_spacer
static Entity Icon(Entity parent, string image, bool tinted = true);  // An icon fourteen pixels tall, Bevy's icon, tinted as the text around it is, or Bevy's icon_untinted
static Entity Dialog(Entity parent, float width = 0f);          // A modal dialog over the window, Bevy's FeathersDialog, its contents spawned under ContentsOf it, as DialogHeader, DialogBody and DialogFooter
static Entity FloatingDialog(Entity parent, string title, float width = 0f, float left = 120f, float top = 120f);  // A dialog floating over the window, dragged by its title, Bevy's FeathersFloatingDialog, its contents spawned under ContentsOf it
static Entity DialogHeader(Entity dialog);                      // A dialog's header, Bevy's FeathersDialogHeader, its title and close button in a row
static Entity DialogBody(Entity dialog);                        // A dialog's body, Bevy's FeathersDialogBody
static Entity DialogFooter(Entity dialog);                      // A dialog's footer, Bevy's FeathersDialogFooter, its buttons in a row
static Entity DialogClose(Entity header);                       // A dialog's close button, Bevy's FeathersDialogClose, which reports RequestClose
static Entity ContentsOf(Entity control);                       // The node a control's contents go under, where Bevy's own props would hold them, a dialog's panel, a floating dialog's body, or the part of a list view or a select that scrolls
static void SetNumber(Entity input, float value);               // Writes a number input's value as a Single, the type it then reports
static void SetNumber(Entity input, double value);              // Writes a number input's value as a Double, the type it then reports
static void SetNumber(Entity input, int value);                 // Writes a number input's value as an Int32, the type it then reports
static void SetNumber(Entity input, long value);                // Writes a number input's value as a Int64, the type it then reports
static NumberInputValue? NumberOf(Entity input);                // What a number input holds, or null where nothing has been written to it
static void SetNumberLimit(Entity input, float min, float max, bool hard = false);  // Bounds a number input's Single value, hard, which neither typing nor dragging passes, or soft, which a drag stays within and typing can pass
static void SetNumberLimit(Entity input, double min, double max, bool hard = false);  // Bounds a number input's Double value, hard or soft
static void SetNumberLimit(Entity input, int min, int max, bool hard = false);  // Bounds a number input's Int32 value, hard or soft
static void SetNumberLimit(Entity input, long min, long max, bool hard = false);  // Bounds a number input's Int64 value, hard or soft
static void SetNumberUnits(Entity input, NumberUnits units);    // Has a number input show its value in units, typed in any of their dimension
static void SetTheme(ReadOnlySpan<(string, Color)> colors);     // Replaces the theme with one that colors each of the tokens given and nothing else, as Bevy's examples' basic theme does, Bevy's ThemeProps::new_non_contextual
static void SetDarkTheme();                                     // Puts Bevy's dark theme back, the one the app begins with
static void ThemeColor(Entity node, string token, ThemedPart part = ThemedPart.Background);  // Colors a node of the game's own from the theme, by one of FeathersToken's tokens, so it follows the theme as the controls do

// Length
static Length Px(float value);                                  // A distance in logical pixels
static Length Percent(float value);                             // A share of the parent, where 100 is all of it

// Sides
static Sides All(Length value);                                 // The same length on every side
static Sides Horizontal(Length value);                          // Left and right, with nothing at the top and bottom
static Sides Vertical(Length value);                            // Top and bottom, with nothing at the left and right

// Corners
static Corners All(Length value);                               // The same radius on every corner
static Corners Top(Length value);                               // Rounded along the top edge only, like a tab
static Corners Bottom(Length value);                            // Rounded along the bottom edge only

// Track
static Track Px(float value);                                   // A fixed number of logical pixels
static Track Percent(float value);                              // A percentage of the grid across that axis
static Track Fr(float share);                                   // A share of whatever room is left after the fixed tracks have taken theirs
static Track Flex(float share);                                 // A share of the room left over, as Fr is, that may also be narrower than what it holds
Track Repeated(int count);                                      // The same track, stated count times over
Track Filling(bool collapse = false);                           // The same track, repeated as many times as the grid has room for

// ImGuiRuntime
static void Start(string? fonts = null, float size = 15f, params string[] faces);  // Creates the context and hands the engine the font atlas
static ImFontPtr Face(string face);                             // One of the loaded faces, or whatever is in force when it was not loaded
static void Begin(BehaviorContext ctx);                         // Starts a frame, how large the window is, what the pointer did, what was typed
static void End();                                              // Ends the frame and hands the engine what came of it

// ImGuiTextures
static ulong Load(string path);                                 // What to call the picture at a path, loading it the first time it is asked for
static ulong Of(AssetHandle image);                             // What to call a picture the caller already holds, such as one a camera is drawing into
static (uint Width, uint Height) SizeOf(ulong picture);         // How large a picture is, in pixels, or zero by zero while it is still loading
static void Draw(string path, float size, Vector4 tint);        // Draws a picture, tinted
static bool Button(string id, string path, float size, Vector4 tint);  // A button that is a picture
```

## Audio

The guide's page is [audio.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/audio.md).

```csharp
// Audio
static float BusVolume(string? bus);                            // A bus's volume, which is one for a bus nobody has set and for no bus at all
static void SetBusVolume(string bus, float volume);             // Sets a bus's volume, which every sound on it is heard at times its own
static AssetHandle CreatePitch(float frequency, TimeSpan duration);  // Makes a tone of one frequency, as Bevy's Pitch is, for Play to play as it plays a clip
static AssetHandle CreateSampleStream(int sampleRate = 44100, int channels = 1);  // Makes a stream of samples the game writes as it plays, as a Decodable of Bevy's makes them, for Play to play as it plays a clip
static int QueueSamples(AssetHandle stream, ReadOnlySpan<float> samples);  // Writes samples after those a stream from CreateSampleStream holds, and returns how many it then holds
static int QueuedSamples(AssetHandle stream);                   // How many samples a stream from CreateSampleStream holds that have not been played
static Entity Play(AssetHandle clip);                           // Plays a sound and returns the entity playing it
static Entity Play(AssetHandle clip, AudioSettings settings);   // Plays a sound as settings describes
static void SetVolume(Entity playing, float volume);            // Sets a playing sound's volume
static void Pause(Entity playing, float volume = 1f);           // Pauses a playing sound, keeping its place
static void Resume(Entity playing, float volume = 1f);          // Resumes a paused sound
static float VolumeOf(Entity playing);                          // A sound's volume, its own times its bus's, before the global volume
static bool HasStarted(Entity playing);                         // Whether Bevy has attached the sink that plays a sound, which the calls that reach the sink need
static bool IsPaused(Entity playing);                           // Whether a sound is paused
static void Stop(Entity playing);                               // Stops a sound and despawns the entity playing it
static void SetListener(Entity entity, float earGap = 0f);      // Makes an entity the ear spatial sound is heard from
static float VolumeFromDecibels(float decibels);                // A volume multiplier from a number of decibels
static float DecibelsFromVolume(float volume);                  // The decibels a volume multiplier stands for
static void SetListener(Entity entity, Vec3 left, Vec3 right);  // Makes an entity the ear, with each ear placed exactly
static float PositionOf(Entity playing);                        // How far into its clip a sound has played, in seconds
static void Seek(Entity playing, float seconds);                // Moves playback to a point in the clip, in seconds from its start
static void SetSpeed(Entity playing, float speed);              // Sets how fast a playing sound plays, one as recorded, Bevy's AudioSink::set_speed
static float SpeedOf(Entity playing);                           // How fast a playing sound plays, one as recorded
static void SetMuted(Entity playing, bool muted);               // Mutes a playing sound or lets it be heard again, Bevy's AudioSink::mute and unmute
static bool IsMuted(Entity playing);                            // Whether a playing sound is muted
static void SetGlobalVolume(float volume);                      // Scales every sound at once, as a settings screen does
```

## Physics

The guide's page is [physics.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/physics.md).

```csharp
// Physics.PhysicsWorld
bool IsCharacter(Entity entity);                                // Whether an entity's body is a character's
void SetContinuous(Entity entity, bool continuous);             // Sweeps an entity's body over each step to find what it would meet within it, for a body fast enough to cross a thin wall in one step, as a shot or a ball struck hard is, or stops sweeping it
bool IsContinuous(Entity entity);                               // Whether an entity's body is swept over each step
bool Has(Entity entity);                                        // Whether an entity has a body
void Add(Entity entity, PhysicsShape shape, BodyKind kind, Transform at, float mass = 1f, bool sensor = false, PhysicsMaterial? material = null);  // Gives an entity a body, starting where at puts it
void SetMaterial(Entity entity, PhysicsMaterial material);      // Changes how a body's surface slides and bounces, from the next step
bool Remove(Entity entity);                                     // Takes an entity's body away, leaving the entity where it is
(Vec3 Linear, Vec3 Angular) Velocity(Entity entity);            // A body's velocity, how fast it moves, and how fast it turns about each axis
void SetVelocity(Entity entity, Vec3 linear, Vec3 angular = default);  // Sets a dynamic body's velocity, waking it if it had come to rest
void Place(EcsWorld ecs, Entity entity, Vec3 position);         // Puts a dynamic body's entity at a place at once and at rest, as a character's respawn or a teleport does, rather than carrying it there through what lies between
void ApplyImpulse(Entity entity, Vec3 impulse, Vec3 offset = default);  // Pushes a dynamic body with an impulse, a change in momentum, at a point offset from its center, which turns it as well where the point is off center
bool IsAsleep(Entity entity);                                   // Whether a dynamic body has come to rest and stopped being simulated
PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance);  // The nearest body a ray meets within distance, or null for none
PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance, Entity from);  // The nearest body a ray cast from from's body meets within distance, passing through that body and what its layer does not collide with, or null for none
void Step(EcsWorld ecs, float seconds, MessageBus? messages = null);  // Advances the simulation by seconds: kinematic bodies follow their entities, everything is stepped, and dynamic bodies are written back
void Dispose();                                                 // Tears the simulation down, returning its memory
float ContactImpulse(Entity a, Entity b);                       // The push the last step gave two touching bodies along the normals of their contacts, in mass times units a second, or zero for a pair not touching
JointHandle Connect(Entity a, Entity b, Joint joint);           // Joins two bodies with a joint, which holds from the next step on
bool SetMotor(JointHandle joint, float degreesPerSecond, float torque);  // Changes a hinge's motor while it runs, to open a door on command or stop a fan
bool SetDistance(JointHandle joint, float minimum, float maximum);  // Changes how far apart a distance joint keeps its points, as a winch reeling a rope in does when it is set a little shorter each frame
bool SetDrive(JointHandle joint, float unitsPerSecond, float force);  // Changes a slider's drive while it runs, to send a lift up or hold it where it is
float? SliderPosition(JointHandle joint);                       // How far a slider's second body is along its axis from where it was joined, or null for a joint that is no slider
float JointForce(JointHandle joint);                            // The force a joint held its bodies together with over the last step, in newtons where mass is in kilograms, as a rope holding a three kilogram box reads the box's weight, 29.4
bool Disconnect(JointHandle joint);                             // Takes a joint away, leaving both bodies free
void MarkPlaced(Entity entity);                                 // Says that entity was put where it is rather than moved there, as when a level starts again, so its kinematic body is put at the new place, at rest, and not carried there through whatever is between
void SetLayer(Entity entity, int layer);                        // Puts an entity's body on one of 32 layers, 0 to 31, which decides what it collides with, as SetLayersCollide says
int LayerOf(Entity entity);                                     // The layer an entity's body is on
void SetLayersCollide(int a, int b, bool collide);              // Whether bodies on layer a collide with bodies on layer b, both ways, as the player's shots pass through the player and the enemies through each other
bool LayersCollide(int a, int b);                               // Whether bodies on two layers collide
JointHandle? JointOf(Entity entity);                            // The joint an entity's JointBetween made, for a game to drive its motor with SetMotor or SetDrive, or nothing while its bodies are still to come or where it could not be made
void Sync(EcsWorld ecs);                                        // Makes, remakes and takes away the bodies of entities carrying a RigidBody and a Collider, so the simulation holds what the world says

// Physics.PhysicsPlugin
void Build(App app);                                            // Registers this plugin's contributions on app

// Physics.PhysicsShape
PhysicsShape Moved(Vec3 offset);                                // The same shape with its middle moved off the entity's origin
static PhysicsShape Box(Vec3 size);                             // A box of the given full size along each axis
static PhysicsShape Sphere(float radius);                       // A sphere of the given radius
static PhysicsShape Capsule(float radius, float length);        // A capsule standing along Y: a cylinder of length with a half sphere of radius at each end, the shape a character usually collides as
static PhysicsShape Cylinder(float radius, float length);       // A cylinder standing along Y
static PhysicsShape Mesh(ReadOnlySpan<Vec3> positions, ReadOnlySpan<uint> indices);  // Triangles, as a level's floors and walls are, from positions and three indices a triangle
static PhysicsShape Mesh(MeshData mesh);                        // The same, from a mesh read back with TryReadMesh
static PhysicsShape Hull(ReadOnlySpan<Vec3> points);            // The smallest convex shape holding every point, as a rock or an odd crate tumbles
static PhysicsShape Hull(MeshData mesh);                        // The same, around the vertices of a mesh read back with TryReadMesh

// Physics.Joint
static Joint Ball(Vec3 anchorA, Vec3 anchorB);                  // A point on one body held to a point on the other, free to turn any way about it, such as a shoulder, a pendulum's pivot, a chain's links
static Joint Hinge(Vec3 anchorA, Vec3 axisA, Vec3 anchorB, Vec3 axisB);  // The same, and turning only about one axis, such as a door, a wheel or an elbow
static Joint Weld();                                            // The two held exactly as they are to each other when joined, as though glued, such as a sword in a hand, a part bolted onto a vehicle
static Joint Distance(Vec3 anchorA, Vec3 anchorB, float minimum, float maximum);  // A point on each kept between minimum and maximum apart, a rope where the minimum is zero and a rod where the two are equal
static Joint Slider(Vec3 axisA);                                // The second body sliding along an axis against the first, neither turning against the other, starting as they are placed, as a drawer, a sliding door or a lift on its frame does
Joint WithMotor(float degreesPerSecond, float torque);          // A hinge that turns itself, a fan or a wheel driven at a speed, pushing with no more than a torque
Joint WithLimits(float lowestDegrees, float highestDegrees);    // A hinge that stops at an angle each way, a door that opens to ninety degrees and no further
Joint WithCone(Vec3 axisA, float swingDegrees, float twistDegrees);  // A ball joint kept within a cone, the second body swung no further than swingDegrees from the first's axisA and twisted about it no further than twistDegrees either way, as a shoulder or a link of a chain
Joint WithTravel(float minimum, float maximum);                 // A slider that stops between minimum and maximum units along its axis from where it was joined, a drawer that stops out and in
Joint WithDrive(float unitsPerSecond, float force);             // A slider that drives itself at unitsPerSecond along its axis, toward the axis's tip for a positive speed, pushing with no more than force, as a lift's winch does

// Physics.Colliders
static bool TryFit(EcsWorld world, Entity entity, out ColliderFit fit);  // What an entity's collider comes to, or false while a mesh it fits has not loaded or a hull or a mesh has none to fit
static bool Draw(EcsWorld world, Entity entity, (float R, float G, float B, float A) color);  // Draws an entity's collider as a gizmo, where it collides
```

## Input

The guide's page is [input.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/input.md).

```csharp
// Input
bool KeyDown(Key key);                                          // True while key is held down
bool KeyPressed(Key key);                                       // True on the single frame key went down
bool KeyReleased(Key key);                                      // True on the single frame key went up
bool KeyDown(LogicalKey key);                                   // True while the key that reads as key is held down
bool KeyPressed(LogicalKey key);                                // True on the single frame the key that reads as key went down
bool KeyReleased(LogicalKey key);                               // True on the single frame the key that reads as key went up
bool AnyKeyDown();                                              // True while any key at all is held
bool AnyKeyPressed();                                           // True when any key at all went down this frame
bool AnyKeyDown(ReadOnlySpan<Key> keys);                        // True while at least one of keys is held
bool AllKeysDown(ReadOnlySpan<Key> keys);                       // True while every one of keys is held
bool AnyKeyPressed(ReadOnlySpan<Key> keys);                     // True when at least one of keys went down this frame
bool AnyKeyReleased(ReadOnlySpan<Key> keys);                    // True when at least one of keys went up this frame
bool MouseDown(MouseButton button);                             // True while button is held down
bool MousePressed(MouseButton button);                          // True on the single frame button went down
bool MouseReleased(MouseButton button);                         // True on the single frame button went up
bool AnyMouseDown();                                            // True while any mouse button is held
bool AnyMousePressed();                                         // True when any mouse button went down this frame

// LogicalKey
static LogicalKey Character(string text);                       // The key that types text
static LogicalKey Named(string name);                           // The key Bevy names name, as Enter or ArrowLeft
static LogicalKey Dead(string text);                            // A dead key, which changes the character the next key types

// Gamepad
bool Down(GamepadButton button);                                // Whether a button is held
bool Pressed(GamepadButton button);                             // Whether a button went down this frame
bool Released(GamepadButton button);                            // Whether a button came up this frame
float Axis(GamepadAxis axis);                                   // Where an axis stands, a stick from minus one to one and a trigger from zero to one
void Rumble(float strong, float weak, float seconds);           // Rumbles the pad, its strong motor and its weak one each from zero to one, for some seconds
void StopRumble();                                              // Stops every rumble the pad is playing

// SyntheticInput
static void MoveTo(float x, float y);                           // Moves the pointer to a point in the window, in logical pixels
static void Press(float x, float y, MouseButton button = MouseButton.Left);  // Presses a button where the pointer is put
static void Release(float x, float y, MouseButton button = MouseButton.Left);  // Releases a button where the pointer is put
static Entity ConnectGamepad(string name = "Console pad");      // Connects a pretended gamepad and returns its entity
static void DisconnectGamepad(Entity gamepad);                  // Disconnects a pretended gamepad, which leaves Gamepads the next frame
static void SetGamepadButton(Entity gamepad, GamepadButton button, float value = 1f);  // Sets one of a pretended pad's buttons, from zero, up, to one, down
static void SetGamepadAxis(Entity gamepad, GamepadAxis axis, float value);  // Sets one of a pretended pad's axes, a stick from minus one to one and a trigger from zero to one
static void Wheel(float lines, float sideways = 0f);            // Rolls the wheel, in the lines a wheel with detents reports
static void Press(Key key, string typed = "");                  // Presses a key where a real one is reported, at the window
static void Lift(Key key);                                      // Lets a key go, where a real one is reported
static void Tap(Key key, string typed = "");                    // Presses a key and lets it go again
static void Key(ImGuiKey key, string? typed = null);            // Presses and releases a key in the interface's own queue
static void Compose(string text, int caretStart = -1, int caretEnd = -1);  // Composes text as the platform's input method would, with the caret over caretStart to caretEnd, or hidden at -1
static void Commit(string text);                                // Commits text as the platform's input method would, arriving as an ImeCommit
static void Type(string text);                                  // Types a run of characters
static void Send(float x, float y, PointerAction action, MouseButton button = MouseButton.Left);  // Moves, presses or releases the pointer
static void Forget();                                           // Gives the pointer back to the hand on the desk
static bool TryTakeClickWithoutWindow(out float x, out float y);  // Takes the oldest left click given to a run with no window that nothing has answered yet

// KeyTable
static bool IsMapped(Key key);                                  // True when key has a bit in the input bitsets
```

## The window

The guide's page is [window.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/window.md).

```csharp
// Window
static void SetTitle(string title);                             // Sets the window's title
static void SetSize(uint width, uint height);                   // Resizes the window, in logical pixels
static (uint Width, uint Height) Size();                        // The window's current size, in logical pixels
static Entity Entity();                                         // The primary window's entity, or None where there is no window
static float Scale();                                           // How many physical pixels a logical one is
static void SetMode(WindowMode mode);                           // Sets how the window fills the screen
static void SetPosition(int x, int y);                          // Moves the window, in physical pixels from the desktop's top-left corner
static void SetIme(bool enabled, float x = 0f, float y = 0f);   // Turns the platform's input method on or off, and says where the text being composed is
static void SetStyle(bool decorations = true, bool resizable = true, bool alwaysOnTop = false);  // Sets whether the window has a title bar and border, whether it can be resized by dragging, and whether it stays above other windows
static void SetCursorShape(CursorShape shape);                  // Sets the shape of the pointer while it is over the window
static void SetCursorImage(AssetHandle image, uint hotspotX = 0, uint hotspotY = 0);  // Sets the pointer over the window to an image, its hotspot, the pixel that points, given in pixels across and down from the image's top left corner
static void SetIcon(AssetHandle image);                         // Shows the window by an image, as the platform's icon for it in its title bar and task switcher
static void SetCursorAtlas(AssetHandle? layout, uint index = 0, uint hotspotX = 0, uint hotspotY = 0);  // Cuts the pointer's image to one frame of an atlas, its hotspot given across and down from the frame's top left corner, or shows the whole image again with no layout
static void SetUpdateSettings(UpdateSettings settings);         // Sets how often the app updates while its window has focus and while it does not, as inserting Bevy's WinitSettings does
static void RequestRedraw();                                    // Asks for one more update, as Bevy's RequestRedraw does, for an app in a reactive mode with something still moving that no event would wake it for
static void WakeUp();                                           // Wakes winit's loop for one more update, as Bevy's WinitUserEvent::WakeUp does
static void Minimize();                                         // Minimizes the window to the taskbar or dock
static void SetMaximized(bool maximized);                       // Maximizes the window to fill the screen less the taskbar, or puts it back to the size it had
static WindowPlace Place();                                     // Where the window is, how large, and whether it is maximized
static void StartDragMove();                                    // Hands the window to the platform to be moved by the pointer, until the button held now is let go
static void StartDragResize(WindowEdge edge);                   // Hands the window to the platform to be resized by the pointer from one edge or corner, until the button held now is let go
static void SetFullscreenOn(Entity window, Entity monitor);     // Makes a window a game spawned fill a monitor, at the mode the monitor shows, and centers it there for when it leaves fullscreen
static int MonitorCount();                                      // How many monitors the platform reports
static MonitorInfo Monitor(int index);                          // Describes one monitor, by an index below MonitorCount
static string MonitorName(int index);                           // The name the platform gives a monitor, by an index below MonitorCount
static VideoMode[] MonitorModes(int index);                     // Every video mode a monitor can run at, by an index below MonitorCount
static void SetVideoMode(int monitor, int mode);                // Takes the screen over in exclusive fullscreen at one of a monitor's own video modes
static void SetCursor(CursorGrab grab, bool visible);           // Sets whether the cursor is confined or hidden
static void SetTitle(Entity window, string title);              // Sets a window's title
static void SetSize(Entity window, uint width, uint height);    // Resizes a window, in logical pixels
static (uint Width, uint Height) Size(Entity window);           // A window's current size, in logical pixels
static float Scale(Entity window);                              // How many physical pixels a logical one is in a window, which may differ by monitor
static void SetMode(Entity window, WindowMode mode);            // Sets how a window fills the screen
static void SetPosition(Entity window, int x, int y);           // Moves a window, in physical pixels from the desktop's top-left corner
static void SetIme(Entity window, bool enabled, float x = 0f, float y = 0f);  // Turns the platform's input method on or off for a window, and says where in it the text being composed is
static void SetStyle(Entity window, bool decorations = true, bool resizable = true, bool alwaysOnTop = false);  // Sets whether a window has a title bar and border, whether it can be resized by dragging, and whether it stays above other windows
static void SetCursorShape(Entity window, CursorShape shape);   // Sets the shape of the pointer while it is over a window
static void SetCursorImage(Entity window, AssetHandle image, uint hotspotX = 0, uint hotspotY = 0);  // Sets the pointer over a window to an image, with its hotspot from the image's top left
static void SetCursorAtlas(Entity window, AssetHandle? layout, uint index = 0, uint hotspotX = 0, uint hotspotY = 0);  // Cuts the pointer's image over a window to one frame of an atlas, or shows the whole image again with no layout
static void SetIcon(Entity window, AssetHandle image);          // Shows a window by an image, as the platform's icon for it
static void SetCursor(Entity window, CursorGrab grab, bool visible);  // Sets whether the cursor is confined to a window or hidden over it
static void Minimize(Entity window);                            // Minimizes a window to the taskbar or dock
static void SetMaximized(Entity window, bool maximized);        // Maximizes a window, or puts it back to the size it had
static WindowPlace Place(Entity window);                        // Where a window is, how large, and whether it is maximized
static void StartDragMove(Entity window);                       // Hands a window to the platform to be moved by the pointer, until the button held now is let go
static void StartDragResize(Entity window, WindowEdge edge);    // Hands a window to the platform to be resized by the pointer from one edge or corner, until the button held now is let go
static void SetVideoMode(Entity window, int monitor, int mode);  // Takes the screen over with a window in exclusive fullscreen at one of a monitor's own video modes

// UpdateMode
static UpdateMode Reactive(TimeSpan wait);                      // Updates after wait, or sooner for any event
static UpdateMode ReactiveLowPower(TimeSpan wait);              // Updates after wait, or sooner for the window's events and the app's own, but not for the devices' own, as a mouse moved where the window is not
```

## Math

```csharp
// Vec3
static float Dot(Vec3 a, Vec3 b);                               // The dot product
static Vec3 Cross(Vec3 a, Vec3 b);                              // The cross product, perpendicular to both operands

// Quat
static Quat FromAxisAngle(Vec3 axis, float radians);            // A rotation of radians about an arbitrary axis
static Quat FromRotationX(float radians);                       // A rotation about the X axis
static Quat FromRotationY(float radians);                       // A rotation about the Y axis
static Quat FromRotationZ(float radians);                       // A rotation about the Z axis
static Quat FromBasis(Vec3 x, Vec3 y, Vec3 z);                  // The rotation whose local axes are x, y and z
static Quat FromEuler(float x, float y, float z);               // The rotation that rolls about Z, then pitches about X, then turns about Y, in radians
Vec3 ToEuler();                                                 // The turns about X, Y and Z this rotation is made of, in radians
static Quat Slerp(Quat from, Quat to, float t);                 // The rotation a fraction of the way between two, turning at an even rate the shorter way round, as Bevy's Quat::slerp does
static Quat Lerp(Quat from, Quat to, float t);                  // A straight blend of the two rotations, taken the shorter way round and made a rotation again, as Bevy's Quat::lerp does

// Color
static Color FromSrgb(float r, float g, float b, float a = 1f);  // A color from sRGB components between zero and one, as a color picker gives them
static Color FromHex(string hex);                               // A color from an sRGB hex string such as #ff8800 or ff8800cc
static Color FromSrgb8(byte r, byte g, byte b, byte a = 255);   // A color from sRGB bytes, as Bevy's Color::srgb_u8 and a hex code give them
static Color FromHsl(float hue, float saturation, float lightness, float alpha = 1f);  // A color from a hue in degrees, a saturation and a lightness between zero and one, as Bevy's Color::hsl gives it
Vec4 ToSrgb();                                                  // The color as sRGB components between zero and one, alpha unchanged
Color WithAlpha(float alpha);                                   // The color with another alpha
static Color From(ColorSpace space, Vec4 components);           // A color from its four components in one of Bevy's color spaces, converted as Bevy converts it
Vec4 In(ColorSpace space);                                      // The color's four components in one of Bevy's color spaces, converted as Bevy converts it, in the order From takes them
string ToHex();                                                 // The color as sRGB hex, #RRGGBB, or #RRGGBBAA where it is not opaque, as Bevy's Srgba::to_hex writes it and FromHex reads it

// Transform
static Transform At(float x, float y, float z);                 // A transform at x, y, z
static Transform LookingAt(Vec3 eye, Vec3 target, Vec3 up);     // A transform at eye oriented so that its forward axis points at target

// GlobalTransform
Transform ToTransform();                                        // The same transform expressed as a position, rotation and scale
Vec3 TransformPoint(Vec3 point);                                // Maps a point in the entity's local space into world space
Vec3 TransformDirection(Vec3 direction);                        // Maps a direction in the entity's local space into world space

// EaseFunction
static EaseFunction Steps(int count, JumpAt jump);              // A staircase of count steps rising where jump says, as Bevy's EaseFunction::Steps
static EaseFunction Elastic(float omega);                       // A spring of angular frequency omega settling at one, as Bevy's EaseFunction::Elastic
float Sample(float t);                                          // The value at t, from zero at its start to one at its end
```

## The tools

The guide's page is [tools.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/tools.md).

```csharp
// Capture
static Task<string> StartRecording(string path, int intervalMs = 100, float scale = 1f);  // Starts recording what this run draws, a picture at each interval of its clock, until StopRecording, as an animated GIF or PNG
static string Screenshot();                                     // Saves a picture of the frame being drawn into ScreenshotFolder, as the key does
static Task<string>? StopRecording();                           // Stops a recording, whose file is written once the pictures it asked for have come back

// ConsoleCommands
static void Add(ConsoleCommand command);                        // Adds a command, replacing one of the same name
static void Add(string name, string help, Func<string[], string> run);  // Adds a command written out in place
static bool Remove(string name);                                // Takes one back out
static ConsoleCommand? Find(string name);                       // The command of a given name, or null
static IReadOnlyList<ConsoleCommand> Starting(string prefix);   // The commands whose names start with what has been typed so far
static string? Run(string line);                                // Runs a line as it was typed, and answers with what to write back
static string Unwrap(string line);                              // A line with one pair of enclosing quotes taken off, if it has them
static string[] Split(string line);                             // A line of arguments as words, keeping quoted runs together

// Diagnostics
static void Register(string path, string suffix = "", int history = 0);  // Registers a diagnostic, so measurements of it are kept
static bool Measure(string path, double value);                 // Adds a measurement to a diagnostic, taken now
static bool TryRead(string path, out DiagnosticReading reading);  // What a diagnostic holds now
static bool SetEnabled(string path, bool on);                   // Turns a diagnostic's measuring and its line in Bevy's log on or off, its history kept
static IReadOnlyList<DiagnosticReading> All();                  // Every diagnostic in the store, by path
static void SetLogFilter(IEnumerable<string>? paths);           // Has Bevy's log print only these diagnostics, or every one when given nothing

// ConsoleHost
static void Fail(string code, string message);                  // Says the command failed, with a code a script can branch on
static void Hold(ulong frame);                                  // Holds this command's answer until the clock's count of frames reaches frame
static void Later(Func<string> poll);                           // Says this command's answer is not ready yet, and how to ask for it
static ConsoleHost.Scope Lend(World world);                     // Lends the world to commands run inside the returned scope

// ConsoleHost.Scope
void Dispose();                                                 // Ends it, putting back whatever was lent before

// ConsoleLog
static void Start();                                            // Starts teeing the output and error streams into the ring
static void Write(LogLevel level, string text);                 // Adds a line
static void Write(string text);                                 // Adds a line at the ordinary level
static LogLine[] All();                                         // The lines kept, oldest first
static void Clear();                                            // Forgets everything

// CliClient
static string Send(CliSession session, string operation, string? line = null, double seconds = 30);  // Sends one request and reads the one answer
static CliAnswer Run(CliSession session, string line, double seconds = 5);  // Runs a console command in a running app and reads what it answered

// CliJson
static string Envelope(string command, bool success, Action<Utf8JsonWriter>? data = null, IReadOnlyList<CliError>? errors = null, IReadOnlyList<string>? warnings = null, string? id = null);  // Writes an envelope, with whatever data writes as its payload
static string Ok(string command, Action<Utf8JsonWriter>? data = null, string? id = null);  // An envelope that worked
static string Fail(string command, string code, string message, string? id = null);  // An envelope that did not

// CliPlugin
void Build(App app);                                            // Registers this plugin's contributions on app

// CliSessionFile
static string PathFor(int pid);                                 // Where the file for a given process is
static void Write(CliSession session);                          // Writes one, replacing whatever was there
static CliSession? Read(string path);                           // Reads one, or nothing when the file is missing or not one of these
static IReadOnlyList<CliSession> All();                         // Every session written down, newest first, whether or not it is still alive
static void Remove(int pid);                                    // Takes one back out, on the way down
static int Prune();                                             // Deletes the files of sessions whose processes are no longer there
```

## Everything else

```csharp
// Aabb2d
static Aabb2d FromCenter(Vec2 center, Vec2 halfSize);           // The box about a center, reaching half its size each way
static Aabb2d FromPointCloud(Isometry2d isometry, ReadOnlySpan<Vec2> points);  // The box about points placed by an isometry
Vec2 ClosestPoint(Vec2 point);                                  // The point of the box nearest a point, the point itself where the box holds it
bool Intersects(Aabb2d other);                                  // Whether it overlaps another box, touching counting
bool Intersects(BoundingCircle circle);                         // Whether it overlaps a circle, touching counting

// AabbCast2d
float? AabbCollisionAt(Aabb2d other);                           // How far along the ray the swept box first touches another, or null

// Arc2d
static Arc2d FromRadians(float radius, float angle);            // An arc spanning angle radians in all, Bevy's from_radians
static Arc2d FromDegrees(float radius, float angle);            // An arc spanning angle degrees in all, Bevy's from_degrees
static Arc2d FromTurns(float radius, float fraction);           // An arc spanning fraction of a whole turn, half a turn a semicircle, Bevy's from_turns
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// BoundingCircle
Vec2 ClosestPoint(Vec2 point);                                  // The point of the circle nearest a point, the point itself where the circle holds it
bool Intersects(BoundingCircle other);                          // Whether it overlaps another circle, touching counting
bool Intersects(Aabb2d box);                                    // Whether it overlaps a box, touching counting

// BoundingCircleCast
float? CircleCollisionAt(BoundingCircle other);                 // How far along the ray the swept circle first touches another, or null

// Capsule2d
static Capsule2d FromLength(float radius, float length);        // A capsule of a radius whose straight middle is a length long, as Bevy's Capsule2d::new takes it
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// Circle
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// CircularSector
static CircularSector FromRadians(float radius, float angle);   // A sector spanning angle radians in all
static CircularSector FromDegrees(float radius, float angle);   // A sector spanning angle degrees in all
static CircularSector FromTurns(float radius, float fraction);  // A sector spanning fraction of a whole turn, half a turn a half disc
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// CircularSegment
static CircularSegment FromRadians(float radius, float angle);  // A segment whose arc spans angle radians in all
static CircularSegment FromDegrees(float radius, float angle);  // A segment whose arc spans angle degrees in all
static CircularSegment FromTurns(float radius, float fraction);  // A segment whose arc spans fraction of a whole turn, half a turn a half disc
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// CompassOctants
static CompassOctant? Of(Vec2 direction);                       // The octant a direction points in, north being positive Y as on a stick, or null for no direction at all

// ConsoleView
LogLine[] Lines();                                              // The lines worth showing, oldest first
void Run(string line, World? world = null);                     // Runs a line, and writes both it and its answer into the log
static void AnswerLater(World world);                           // Asks each command waiting on an answer for it, and writes any that arrive into the log
string Back(string current);                                    // What was typed before this, or what is already there at the end of the list
string Forward(string current);                                 // The other way, and back to nothing once the end is reached
string? Completion(string typed);                               // The command a half-typed name would become, or nothing
string Hint(string typed);                                      // What it would complete to, or what it takes, said under the input
static string Written(LogLine line);                            // How a line reads on screen, with its repeat count when it has one

// CrashLog
static string? Write(string title, string what);                // Writes a crash to this run's crash file, made the first time, and says where on the log

// CubicBezier<T>
CubicCurve<T>? ToCurve();                                       // The curve, or null where there is no segment

// CubicBSpline<T>
CubicCurve<T>? ToCurve();                                       // The curve, a segment for each four points in a row, or null with fewer than four
CubicCurve<T>? ToCurveCyclic();                                 // The curve round its points and back, a segment starting at each point, or null with none

// CubicCardinalSpline<T>
static CubicCardinalSpline<T> CatmullRom(IEnumerable<T> points);  // A Catmull-Rom spline, a cardinal spline of tension one half
CubicCurve<T>? ToCurve();                                       // The curve from the first point to the last, or null with fewer than two
CubicCurve<T>? ToCurveCyclic();                                 // The curve from the first point round through the last and back to the first, or null with fewer than two, its first segment the one from the first point to the second, as Bevy's is

// CubicCurve<T>
T Position(float t);                                            // The point at t
T Velocity(float t);                                            // How fast the point moves at t
T Acceleration(float t);                                        // How fast that changes at t
IEnumerable<T> IterPositions(int subdivisions);                 // Points along the whole curve at even steps of t, subdivisions of them and one more for the end, Bevy's iter_positions

// CubicHermite<T>
CubicCurve<T>? ToCurve();                                       // The curve from the first point to the last, or null with fewer than two
CubicCurve<T>? ToCurveCyclic();                                 // The curve from the first point round through the last and back to the first, or null with none

// CubicSegment<T>
static CubicSegment<T> Coefficients(T p0, T p1, T p2, T p3, float[] matrix);  // The segment four control points make under a spline's characteristic matrix, row by row
T Position(float t);                                            // The point at t, from zero at its start to one at its end
T Velocity(float t);                                            // How fast the point moves at t, the first derivative
T Acceleration(float t);                                        // How fast that changes at t, the second derivative

// Cuboid
static Cuboid FromLength(float length);                         // A cube whose sides are each length long
static Cuboid FromSize(Vec3 size);                              // A box of the size given along each axis
Vec3 SampleInterior(Random random);                             // A point inside it, each as likely as any other
Vec3 SampleBoundary(Random random);                             // A point on its surface, each as likely as any other

// Debug
static void AddOverlaySection(string name, Func<IEnumerable<string>> lines);  // Adds a section of a program's own to the overlay's left column, under the engine's lines and any section added before, in place of one of the same name
static bool RemoveOverlaySection(string name);                  // Takes a section of the overlay away by its name

// GameTimer
static GameTimer FromSeconds(float seconds, TimerMode mode);    // A timer of seconds that runs once or over and over, as Bevy's Timer::from_seconds
GameTimer Tick(float delta);                                    // Runs it on by delta seconds, and answers it as it is after, so timer.Tick(delta).JustFinished reads as Bevy's timer.tick(delta).just_finished()
void Pause();                                                   // Stops it where it is until Unpause
void Unpause();                                                 // Lets it run again from where it was paused
void Reset();                                                   // Starts it again from nothing, unfinished, its duration and mode kept

// HookContext
T Res<T>();                                                     // One of the app's managed resources

// IBounded2d
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// ImGuiConsole
static void Draw(BehaviorContext ctx);                          // Opens or closes it on its key, and draws it while it is open

// Isometry2d
static Isometry2d FromTransform(Transform transform);           // A placement where the shape is turned by its transform's rotation about Z and moved to its translation's X and Y, as Bevy's examples place theirs

// Log
static void Trace(string message);                              // Writes a line at the trace level, which Bevy leaves out unless asked for
static void Debug(string message);                              // Writes a line at the debug level, which Bevy leaves out unless asked for
static void Info(string message);                               // Writes a line at the info level, the quietest Bevy shows by default
static void Warn(string message);                               // Writes a line at the warn level, for something wrong that is not a failure
static void Error(string message);                              // Writes a line at the error level, for something that failed
static void TraceOnce(string message, string file = "", int line = 0);  // Writes a line at the trace level the first time this line of code runs, Bevy's trace_once!
static void DebugOnce(string message, string file = "", int line = 0);  // Writes a line at the debug level the first time this line of code runs, Bevy's debug_once!
static void InfoOnce(string message, string file = "", int line = 0);  // Writes a line at the info level the first time this line of code runs, Bevy's info_once!
static void WarnOnce(string message, string file = "", int line = 0);  // Writes a line at the warn level the first time this line of code runs, Bevy's warn_once!
static void ErrorOnce(string message, string file = "", int line = 0);  // Writes a line at the error level the first time this line of code runs, Bevy's error_once!
static void Once(Action work, string file = "", int line = 0);  // Runs work the first time this line of code runs and never again, Bevy's once!

// MemoryGuard
static long ResidentBytes();                                    // How many bytes of the machine's memory the process holds now
static void Watch(double gigabytes);                            // Holds the process to a cap from here on, starting the watch the first time, or with zero lifts the cap
static double DefaultTheEnvironment();                          // Gives the environment the default cap where it names none, for a tool to call before it starts an app or makes one, so the app holds to it

// MeshStream
static MeshStream From<T>(string name, MeshStreamFormat format, uint location, ReadOnlySpan<T> values);  // A stream from values laid out as the format's bytes, such as a uint a vertex for UInt or a Vector4 a vertex for Float4
static int SizeOf(MeshStreamFormat format);                     // Bytes a vertex of format

// Navigation
static Entity? Move(CompassOctant direction);                   // Moves the input focus to the node beside the one holding it in a direction, and answers the node, or null where nothing lies that way, an edge blocks it or nothing holds the focus
static void AddEdge(Entity from, Entity to, CompassOctant direction, bool bothWays = false);  // Draws an edge from one node to another in a direction, and back the opposite way where asked
static void BlockEdge(Entity node, CompassOctant direction, Entity other = default);  // Blocks a direction a node would otherwise be left by, and the opposite one from another node where asked
static void AddEdges(ReadOnlySpan<Entity> nodes, CompassOctant direction, bool looping = false);  // Draws edges between nodes in their order in a direction, each to the next and back, and around from the last where asked
static void Forget(Entity node);                                // Takes a node's edges out, those from it and to it, as one taken off the screen needs
static void Clear();                                            // Takes every edge out, leaving the nearest nodes on the screen

// Ray2d
static Ray2d Toward(Vec2 origin, Vec2 direction);               // A ray from a point toward a direction of any length but zero, which is made length one
Vec2 At(float distance);                                        // The point a distance along it

// RayCast2d
float? AabbIntersectionAt(Aabb2d box);                          // How far along the ray it meets a box, or null
float? CircleIntersectionAt(BoundingCircle circle);             // How far along the ray it meets a circle, or null

// Rectangle
static Rectangle FromSize(float width, float height);           // A rectangle of a width and a height
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// RegularPolygon
IEnumerable<Vec2> Vertices(float rotation);                     // Its corners, turned by an angle, the first up before turning
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// Rot2
static Rot2 Radians(float radians);                             // A rotation by an angle, in radians, counterclockwise

// ScenePacks
static IReadOnlyList<ScenePack> List(string folder, out IReadOnlyList<string> problems);  // Reads every manifest in a folder, in the order of their names
static bool TryRead(string file, out ScenePack? pack, out string? problem);  // Reads one manifest, or says why it cannot, naming the file
static string PathOf(ScenePack pack);                           // Where a pack is kept once fetched
static bool IsFetched(ScenePack pack);                          // Whether a pack has been fetched, by its file being there at the manifest's size, the hash having been checked as it arrived
static Task<string> FetchAsync(ScenePack pack, IProgress<double>? progress = null, CancellationToken cancel = default);  // Fetches a pack into Folder, telling how far it has come, and keeps it where its SHA-256 is the manifest's
static bool TryMount(ScenePack pack, out string? model, out string? problem);  // Mounts a fetched pack under packs/ and its name while an app runs, and answers the path its model is loaded by, or says why it cannot
static bool Unmount(ScenePack pack);                            // Stops reading a mounted pack

// Segment2d
static Segment2d FromDirectionAndLength(Vec2 direction, float length);  // A segment centered on the origin along a direction, a length long
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle

// Sphere
Vec3 SampleInterior(Random random);                             // A point inside it, each as likely as any other
Vec3 SampleBoundary(Random random);                             // A point on its surface, each as likely as any other

// TileData
static TileData FromTilesetIndex(ushort tilesetIndex);          // A tile drawn from a layer of the tileset, white, shown and upright, Bevy's from_tileset_index

// TilemapChunk
int IndexOf(uint x, uint y);                                    // The place in the chunk's tiles of the tile x across and y up
Transform TileTransform(uint x, uint y);                        // Where the tile x across and y up sits, its middle, in the chunk's own space, Bevy's calculate_tile_transform

// Triangle2d
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle
(Vec2 Center, float Radius) Circumcircle();                     // The circle through its three corners, its center and its radius

// Vec2
static Vec2 FromAngle(float radians);                           // The vector of length one at radians counterclockwise from X, Bevy's from_angle
float ToAngle();                                                // The angle from X to this vector in radians, between minus and plus a half turn, Bevy's to_angle
static float Dot(Vec2 a, Vec2 b);                               // The dot product
static float PerpDot(Vec2 a, Vec2 b);                           // The dot product of a turned a quarter turn counterclockwise with b, Bevy's perp_dot, positive where b lies counterclockwise of a
static Vec2 Min(Vec2 a, Vec2 b);                                // The smaller of each component
static Vec2 Max(Vec2 a, Vec2 b);                                // The larger of each component
static Vec2 Clamp(Vec2 v, Vec2 min, Vec2 max);                  // Each component held between its bounds

// Weather
static void SetPreset(WeatherPreset preset, bool immediately = false);  // Sets the weather to a preset, eased into over its transition, or at once
```

# Cheatsheet

Every public method of the library, a line each, grouped as the guide is, with the first sentence of
what its documentation says. A line is a reminder, and the guide page each group links to is the
explanation. `CheatsheetTests` holds this page to the library, so a method added without its line,
or a line left for one that is gone, fails the suite.

## Running an app

The guide's page is [running-a-game.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/running-a-game.md).

### `BevyApp`

```csharp
static int Run(Config config = null);                           // Creates an app with DefaultPlugins, runs it, and disposes it
static int Run(Action<App> configure, Config config = null);    // Creates an app with DefaultPlugins, lets configure add to it, then runs it
static App Build(Config config = null);                         // Builds an app with DefaultPlugins without running it
```

### `App`

```csharp
App AddPlugin(IPlugin plugin);                                  // Adds a plugin, building it immediately
App AddPlugins(IPluginGroup group);                             // Adds every plugin in a group, in Order order
bool HasPlugin<T>();                                            // True when a plugin of type T is registered
int Run();                                                      // Runs the engine
static void RequestExit();                                      // Asks the engine to shut down after the current frame
static void RequestExit(int code);                              // Asks the engine to shut down after the current frame, the run ending with code
static string DescribeAdapter();                                // Describes the graphics adapter the renderer actually chose, or null in a headless run
void Dispose();                                                 // Releases what it holds
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
App On(Stage stage, Action<BehaviorContext> run, string name, Func<World, bool> runIf = null);  // Runs run in stage, as a system Bevy adds to that schedule, and only while runIf passes where one is given
App SpawnGltf(string path, Action<BehaviorContext, Entity> spawned = null, int scene = 0);  // Spawns a glTF file's scene once it has loaded, as Bevy's SceneRoot of a glTF does, and hands the root to spawned once the scene is in the world under it
App Chain(Stage stage, params SystemDescriptor[] systems);      // Registers systems in stage, each to run after the one before it
App AddObserver<TEvent>(Action<On<TEvent>> observer);           // Runs observer each time a TEvent is triggered
App EnableDynamicSystems();                                     // Allows systems to be added after the loop has started
int RemoveSystemsBySource(string source);                       // Removes every system tagged with source
IReadOnlyList<SystemDescriptor> SystemsIn(Stage stage);         // The descriptors registered for stage, in registration order
```

### `Config`

```csharp
static Config HeadlessFor(uint frames);                         // A windowless configuration that runs frames ticks and exits
static Config OffscreenFor(uint width = 1280, uint height = 720, uint frames = 0);  // A configuration that draws into an image of the given size instead of a window
static Config Windowed(string title, uint width = 1280, uint height = 720, GraphicsBackend backend = GraphicsBackend.Automatic);  // A window of the given size, drawn with backend
```

### `World`

```csharp
void InsertResource<T>(T value);                                // Adds or replaces the resource of type T
T GetOrInsertResource<T>(T value);                              // Returns the existing resource, or inserts and returns value
T GetOrInsertResource<T>(Func<T> factory);                      // Returns the existing resource, or inserts one built by factory
T InitResource<T>();                                            // Returns the existing resource, or inserts a default-constructed one
bool RemoveResource<T>();                                       // Removes the resource of type T
bool ContainsResource<T>();                                     // True when a resource of type T is registered
T Resource<T>();                                                // Gets a required resource
T TryResource<T>();                                             // Gets a resource, or null if it is not registered
bool TryGetResource<T>(out T value);                            // Gets a resource, reporting whether it was found
void Clear();                                                   // Disposes every disposable resource and clears the world
void Dispose();                                                 // Releases what it holds
```

### `IPlugin`

```csharp
void Build(App app);                                            // Registers this plugin's contributions on app
```

### `IPluginGroup`

```csharp
IEnumerable<(IPlugin, int)> GetPlugins();                       // The plugins in this group, with their relative ordering
```

### `DefaultPlugins`

```csharp
IEnumerable<(IPlugin, int)> GetPlugins();                       // The plugins in this group, with their relative ordering
```

### `EnginePlugin`

```csharp
void Build(App app);                                            // Registers this plugin's contributions on app
```

### `BehaviorsPlugin`

```csharp
void Build(App app);                                            // Registers this plugin's contributions on app
```

### `Time`

```csharp
void Pause();                                                   // Stops the game's clock, from the next frame
void Resume();                                                  // Starts the game's clock again, from the next frame
void Step(int frames = 1);                                      // Runs the clock for a number of frames and stops it again, to watch a paused game move a frame at a time
void SetSpeed(float speed);                                     // Sets how fast the game's clock runs against the wall's
```

### `FrameProfile`

```csharp
static void Start();                                            // Starts measuring, from nothing
static void Stop();                                             // Stops measuring
static FrameCosts Take(EcsWorld world);                         // What was measured since Start or the last take, which starts the count over
```

## Behaviors and systems

The guide's page is [behaviors.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/behaviors.md).

### `BehaviorContext`

```csharp
T Res<T>();                                                     // Gets a required resource
bool TryRes<T>(out T value);                                    // Gets a resource, reporting whether it was found
TState State<TState>();                                         // The current value of TState
void SetState<TState>(TState value);                            // Asks Bevy to move TState to value
void Send<TMessage>(TMessage message);                          // Broadcasts a message for every reader to see next frame
ReadOnlySpan<TMessage> Read<TMessage>();                        // The messages of type TMessage sent during the previous frame
void Exit();                                                    // Asks the engine to shut down after this frame
void Exit(int code);                                            // Asks the engine to shut down after this frame, the run ending with code, as App.RequestExit(int) does
```

### `BehaviorConditions`

```csharp
static Func<World, bool> HasResource<T>();                      // Passes while a resource of type T exists
static Func<World, bool> ResourceIs<T>(Func<T, bool> predicate);  // Passes while a resource exists and satisfies predicate
static Func<World, bool> AnyWithComponent<T>();                 // Passes while at least one entity carries T
static Func<World, bool> InState<TState>(TState value);         // Passes while TState holds value
static Func<World, bool> RunOnce();                             // Passes only on the first frame
static Func<World, bool> KeyToggle<TTag>(Key key, KeyModifier modifiers = KeyModifier.None, bool defaultEnabled = true);  // A keyboard toggle keyed by TTag, usually the behavior struct
static Func<World, bool> KeyToggle(string systemId, Key key, KeyModifier modifiers = KeyModifier.None, bool defaultEnabled = true);  // A keyboard toggle keyed by an explicit id
static bool ModifiersHeld(Input input, KeyModifier modifiers);  // True when every modifier set in modifiers is currently held
```

### `BehaviorRegistry`

```csharp
static void Add(Action<App> register);                          // Records a generated registration
static IReadOnlyList<Action<App>> Snapshot();                   // A snapshot of the recorded registrations
```

### `BehaviorRunners`

```csharp
static void Run<T>(World world, BehaviorRunner<T> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, int parallelThreshold = 4096);  // Runs body for every entity carrying T
static void Run<T, T1>(World world, BehaviorRunner<T, T1> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, bool writesFirst = true, int parallelThreshold = 4096);  // Runs body for every entity carrying T and T1, handing it both by reference from the same storage
static void Run<T, T1, T2>(World world, BehaviorRunner<T, T1, T2> body, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, ReadOnlySpan<int> changed = default, bool writesFirst = true, bool writesSecond = true, int parallelThreshold = 4096);  // Runs body for every entity carrying T, T1 and T2, handing it all three by reference from the same storage, as the two-component form does
```

### `SystemDescriptor`

```csharp
SystemDescriptor RunIf(Func<World, bool> condition);            // Attaches a run condition
SystemDescriptor After(string name);                            // Runs this system after every system in its stage named name
SystemDescriptor Before(string name);                           // Runs this system before every system in its stage named name
SystemDescriptor Read<T>();                                     // Declares a read of resource type T
SystemDescriptor Write<T>();                                    // Declares a write of resource type T
bool ConflictsWith(SystemDescriptor other);                     // True when this system's declared access overlaps other's in a way that would prevent the two running concurrently
bool Invoke(World world);                                       // Runs the system, honoring RunCondition
```

### `StageOrder`

```csharp
static ReadOnlySpan<Stage> AllInOrder();                        // Every user-facing stage, in execution order
static ReadOnlySpan<Stage> FrameStages();                       // The stages that run exactly once every frame, in execution order
static bool IsInternal(Stage stage);                            // True for the two stages reserved for the engine itself
```

### `SystemToggleRegistry`

```csharp
bool Get(string id, bool defaultEnabled = true);                // The state for id, or defaultEnabled if unset
void Set(string id, bool enabled);                              // Sets the state for id
void Flip(string id, bool defaultEnabled = true);               // Flips the state for id
```

### `SystemRegistrationSourceScope`

```csharp
void Dispose();                                                 // Releases what it holds
```

### `EcsWorld`

```csharp
Entity Spawn();                                                 // Spawns an entity with no components
Entity Spawn(Action<Entity, EcsWorld> build);                   // Spawns an entity and applies build to it
void SpawnBatch(int count, Action<Entity, EcsWorld> build);     // Spawns count entities, applying build to each
void SpawnBatch<T>(int count, Func<int, T> factory);            // Spawns count entities carrying a component from factory
Entity SpawnScene(AssetHandle scene);                           // Spawns a scene asset under a new entity, and returns that entity
Entity Clone(Entity entity);                                    // Spawns a copy of an entity with every component it carries
bool Despawn(Entity entity);                                    // Destroys an entity and everything on it
void DespawnOnExit<TState>(Entity entity, TState state);        // Despawns an entity when a state leaves the value it belongs to
void DespawnOnEnter<TState>(Entity entity, TState state);       // Despawns entity as TState enters state, Bevy's DespawnOnEnter
void DespawnWhen<TState>(Entity entity, Func<StateTransitionEvent<TState>, bool> rule);  // Despawns entity at the first transition of TState rule answers true for, Bevy's DespawnWhen
bool IsAlive(Entity entity);                                    // True when the handle still refers to a live entity
void Add<T>(Entity entity, T component);                        // Adds or replaces a component on an entity
void Set<T>(Entity entity, T component);                        // Overwrites a component's value
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
string NameOf(Entity entity);                                   // What an entity is called, or null when it is called nothing
void SetName(Entity entity, string name);                       // Names an entity, or takes its name away when given nothing
ChunkSet<T> Chunks<T>(ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, bool markChanged = true);  // Collects the storage runs holding T, optionally filtered
ChunkSet<T> Chunks<T>(int componentId, ReadOnlySpan<int> with = default, ReadOnlySpan<int> without = default, bool markChanged = true);  // Collects the storage runs for an explicitly named component
ComponentQuery<T> Query<T>(bool markChanged = true);            // Iterates every T in the world by reference
Entity[] EntitiesWith<T>();                                     // The entities carrying T
IDisposable Observe<TEvent>(Action<On<TEvent>> observer);       // Runs observer each time a TEvent is triggered
IDisposable Observe<TEvent>(Entity entity, Action<On<TEvent>> observer);  // Runs observer each time a TEvent reaches entity
void Trigger<TEvent>(TEvent value);                             // Runs the observers of value now
string GetReflected(Entity entity, string typePath, string path = "");  // Reads one of Bevy's components, or one field of it, as JSON
void SetReflected(Entity entity, string typePath, string path, string json);  // Writes a value given as JSON over one of Bevy's components, or one field of it
string GetVariant(Entity entity, string typePath, string path);  // Which variant an enum inside one of Bevy's components holds
void SetVariant(Entity entity, string typePath, string path, string variant);  // Switches an enum inside one of Bevy's components to another variant
AssetHandle? GetReflectedAsset(Entity entity, string typePath, string path);  // Reads an asset handle inside one of Bevy's components, such as the image a material or a sprite draws with
void SetReflectedAsset(Entity entity, string typePath, string path, AssetHandle asset);  // Points an asset handle inside one of Bevy's components at another asset
Color? GetReflectedColor(Entity entity, string typePath, string path);  // Reads a color inside one of Bevy's components as linear RGBA
void SetReflectedColor(Entity entity, string typePath, string path, Color color);  // Writes a color inside one of Bevy's components from linear RGBA
void InsertReflected(Entity entity, string typePath, string json = null);  // Puts one of Bevy's components on an entity, from JSON or at its default
bool RemoveReflected(Entity entity, string typePath);           // Takes one of Bevy's components off an entity
T? Get<T>(Entity entity);                                       // A typed wrapper over one of Bevy's components on an entity, or null when the entity does not carry it
T Wrap<T>(Entity entity);                                       // A typed wrapper over one of Bevy's components an entity is known to carry
T Insert<T>(Entity entity, string json = null);                 // Puts one of Bevy's components on an entity, from JSON or at its default, and returns a typed wrapper over it
Entity? ResourceEntity(string typePath);                        // The entity holding one of Bevy's resources, or null when the world has none of it
T? Resource<T>();                                               // A typed wrapper over one of Bevy's resources, or null when the world has none of it
T InsertResource<T>(string json = null);                        // Puts one of Bevy's resources in the world, from JSON or at its default, replacing the one it has, and returns a typed wrapper over it
Entity SpawnMesh(AssetHandle mesh, AssetHandle material, Transform at);  // Spawns an entity drawn with a mesh and a material, placed by a transform, as Bevy's (Mesh3d(mesh), MeshMaterial3d(material), transform) bundle does
Entity SpawnPointLight(Vec3 at, bool shadows = false, float intensity = 1000000f, float range = 20f, float radius = 0f);  // Spawns a point light at a place, as Bevy's default one is, a million lumens reaching twenty units and casting no shadow unless asked
Entity SpawnCamera3d(Transform at, CameraSettings settings = null);  // Spawns a 3D camera placed by a transform, with Bevy's defaults or the settings given
IEnumerable<Entity> Descendants(Entity root);                   // Every entity under root, nearer ones first, as Bevy's iter_descendants walks them
```

### `EcsCommands`

```csharp
EcsCommands Spawn(Action<Entity, EcsWorld> build);              // Queues an entity spawn, passing the new entity to build
EcsCommands SpawnBatch(int count, Action<Entity, EcsWorld> build);  // Queues a spawn of count entities
EcsCommands SpawnBatch<T>(int count, Func<int, T> factory);     // Queues a spawn of count entities each carrying a component
EcsCommands Despawn(Entity entity);                             // Queues a despawn
EcsCommands Add<T>(Entity entity, T component);                 // Queues adding or replacing a component
EcsCommands Remove<T>(Entity entity);                           // Queues removing a component
EcsCommands Run(Action<EcsWorld> action);                       // Queues an arbitrary action against the world
void Apply(EcsWorld world);                                     // Drains the queue against world
void Clear();                                                   // Discards every queued command without applying it
```

### `ChunkSet<T>`

```csharp
void Dispose();                                                 // Returns the pooled buffer
```

### `ReloadedComponents`

```csharp
static IReadOnlyList<ComponentSchema> Before();                 // The schemas there are, taken before the new assembly registers its own over them
static int Carry(EcsWorld world, IReadOnlyList<ComponentSchema> before);  // Moves every component of a type registered again since before onto the type registered in its place
static int Revive(EcsWorld world);                              // Puts on their entities the components a scene held that no type described when it was loaded and one does now
```

## States

The guide's page is [states.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/states.md).

### `StateRegistry`

```csharp
static void Declare<TState>(TState initial);                    // Declares TState a state starting at initial, which an app that uses it adds as it starts to run
static IReadOnlyList<string> Unentered();                       // The states declared on their enums that the running app has not added, by name
static TState Current<TState>();                                // The current value of TState
static bool TryCurrent<TState>(out TState value);               // The current value of TState, reporting whether it exists at all
static void Set<TState>(TState value);                          // Asks Bevy to move TState to value
```

## Messages

The guide's page is [messages-and-hierarchy.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/messages-and-hierarchy.md).

### `MessageBus`

```csharp
void Send<T>(T message);                                        // Sends a message, for every reader to see next frame
void SendAll<T>(ReadOnlySpan<T> messages);                      // Sends several messages at once
ReadOnlySpan<T> Read<T>();                                      // The messages of type T sent during the previous frame
int Count<T>();                                                 // How many messages of type T are readable this frame
bool IsEmpty<T>();                                              // True when nothing of type T arrived
void Swap();                                                    // Makes this frame's messages readable and starts a new frame's queue
void Clear();                                                   // Discards everything, sent and readable alike
```

### `On<TEvent>`

```csharp
void Propagate(bool propagate);                                 // Lets the event go on up to the parent, or, given false, stops it here
```

## Components

The guide's page is [components.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/components.md).

### `ComponentHooks`

```csharp
static void OnRemove<T>(RemoveHook<T> hook);                    // Declares what runs when a T leaves an entity
static void OnClone<T>(CloneHook<T> hook);                      // Declares what rewrites the copy of a T an entity clone receives
```

### `ComponentSchema`

```csharp
bool Add(EcsWorld world, Entity entity);                        // Puts a default one on an entity
bool Remove(EcsWorld world, Entity entity);                     // Takes it off again
ComponentField Field(string name);                              // Finds a field by name, or null
ComponentMethod Method(string name);                            // Finds a method by name, or null
object Read(EcsWorld world, Entity entity, string field);       // Reads one field by name, or null when there is no such field
bool Write(EcsWorld world, Entity entity, string field, object value);  // Writes one field by name, reporting whether it landed
```

### `ComponentSchemas`

```csharp
static void Add(ComponentSchema schema);                        // Registers a schema, replacing any earlier one for the same type
static ComponentSchema For(int componentId);                    // The schema for a component id, or null when none describes it
static ComponentSchema For(string name);                        // The schema for a component's full name, or null
static bool TryCoerce<TField>(object value, out TField coerced);  // Turns a boxed value from a tool into the field's own type, reporting whether it fits
```

### `ComponentField`

```csharp
object Read(EcsWorld world, Entity entity);                     // Reads the field, or null when the entity does not carry it
bool Write(EcsWorld world, Entity entity, object value);        // Writes the field, reporting whether it landed
```

### `ItemFields`

```csharp
object Create();                                                // An item at its defaults
ItemFields.Bound Bind(object item);                             // The fields of a copy of an item, and the copy as the fields leave it
```

### `EcsList<T>`

```csharp
static EcsList<T> New();                                        // Makes an empty list now, rather than when its first item is added
void Add(T item);                                               // Adds an item at the end, making the list if this is its first
bool Remove(T item);                                            // Takes the first item equal to item out, reporting whether there was one
void RemoveAt(int index);                                       // Takes the item at a place out
void Clear();                                                   // Empties the list, keeping it
bool Contains(T item);                                          // Whether it holds an item equal to item
EcsList<T> Copy();                                              // A handle to a new list holding the same items, or a handle to nothing when this names none
bool Free();                                                    // Frees the list, after which this handle and every copy of it name nothing
```

### `EcsMap<TKey, TValue>`

```csharp
static EcsMap<TKey, TValue> New();                              // Makes an empty dictionary now, rather than when its first entry is written
void Set(TKey key, TValue value);                               // Writes a value under a key, making the dictionary if this is its first entry
bool TryGetValue(TKey key, out TValue value);                   // Reads the value under a key, reporting whether there was one
bool ContainsKey(TKey key);                                     // Whether there is an entry under a key
bool Remove(TKey key);                                          // Takes the entry under a key out, reporting whether there was one
void Clear();                                                   // Empties the dictionary, keeping it
EcsMap<TKey, TValue> Copy();                                    // A handle to a new dictionary holding the same entries, or a handle to nothing when this names none
bool Free();                                                    // Frees the dictionary, after which this handle and every copy of it name nothing
```

### `IInlineList<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Clear();                                                   // Empties the list
```

### `InlineList4<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list
```

### `InlineList8<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list
```

### `InlineList16<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list
```

### `InlineList32<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list
```

### `InlineList64<T>`

```csharp
T ItemAt(int index);                                            // One item, by its place in the list, as a copy
Span<T> AsSpan();                                               // The items, in order, to write through
bool TryAdd(T item);                                            // Adds an item at the end, reporting whether there was room
void Add(T item);                                               // Adds an item at the end
void RemoveAt(int index);                                       // Takes the item at a place out, moving the ones after it down
void Clear();                                                   // Empties the list
```

### `ListValue`

```csharp
static ListValue From<T>(ReadOnlySpan<T> items);                // The items of a span, boxed, as a generated schema reads an inline list
static ListValue From<T>(IReadOnlyList<T> items);               // The items of a list, boxed, as a generated schema reads a stored list
ListValue With(int index, object item);                         // The same items with the one at index replaced
ListValue Adding(object item);                                  // The same items with one more at the end
ListValue Without(int index);                                   // The same items without the one at index
ListValue Moving(int from, int to);                             // The same items with one moved from one place to another
```

### `MapValue`

```csharp
static MapValue From<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> entries);  // The entries of a dictionary, boxed, as a generated schema reads a stored map
MapValue Rekeyed(int index, object key);                        // The same entries with the one at index given another key
MapValue With(int index, object value);                         // The same entries with the one at index holding another value
MapValue Adding(object key, object value);                      // The same entries with one more at the end
MapValue Without(int index);                                    // The same entries without the one at index
```

### `IReflectedComponent<TSelf>`

```csharp
TSelf Create(EcsWorld world, Entity entity);                    // Makes a wrapper over the component on entity
```

## Scenes and saves

The guide's page is [scenes-and-saves.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/scenes-and-saves.md).

### `DataAssets`

```csharp
static void Register<T>(string name, Func<T> create, Func<DataBox<T>, IReadOnlyList<ComponentField>> fields, IReadOnlyList<string> formerNames = null, int version = 0, Func<int, JsonObject, JsonObject> migrate = null);  // Registers a data asset type
static T Get<T>(DataRef<T> reference);                          // The value of the data asset a reference names, loaded once and shared
static bool TryGet<T>(DataRef<T> reference, out T value);       // Reads the data asset a reference names, reporting whether there is one to read
static DataRef<T> Create<T>(string path);                       // Writes a new data asset at its type's defaults, gives it an id, and returns a reference
static ulong Create(string type, string path);                  // Makes a new data asset of a type named at runtime, for a menu offering every type
static ulong Copy(ulong id, string path);                       // Writes a copy of a data asset to a new file with an id of its own, for the one thing that should differ from the rest sharing it
static void Save<T>(DataRef<T> reference, T value);             // Writes a value over the data asset a reference names, and shares it from then on
static void Save(ulong id);                                     // Writes a loaded data asset's value to its file, as it is now
static ComponentSchema SchemaOf(ulong id);                      // The fields of a data asset, bound to its loaded value, for a tool to draw and edit
static IReadOnlyList<string> Files(string type = null);         // The data asset files under the asset root, as paths from it, holding one type or any
static string TypeOf(string path);                              // The full name of the type a data asset file holds, or nothing for a file that holds none
static void Reload(ulong id);                                   // Forgets a loaded data asset, so the next read loads its file again
static void ReloadAll();                                        // Forgets every loaded data asset
static int SaveChanged();                                       // Writes every data asset changed through its fields since the last call
```

### `SceneFile`

```csharp
static bool IsComputed(string name);                            // Whether a component, by its full name, is one the engine works out for itself
static int Save(EcsWorld world, string path, Func<Entity, bool> include = null, bool giveIds = false);  // Writes entities to a scene file, and their children with them
static int Write(EcsWorld world, Utf8JsonWriter json, Func<Entity, bool> include = null, bool giveIds = false);  // Writes entities as a scene document
static bool CanDescribe(EcsWorld world, Entity entity);         // Whether a scene can say how to make what an entity is drawn with, as Write writes it, or the entity is drawn with nothing
static SceneLoad Load(EcsWorld world, string path, Entity parent = default);  // Spawns everything a scene file holds
static SceneLoad Read(EcsWorld world, JsonElement scene, Entity parent = default);  // Spawns everything a scene document holds
static string Resolve(string path);                             // The full path a scene path names: one under the asset root, with or without AssetsPrefix, one under the player's own directory after Prefix, or an absolute one as it is
```

### `SceneInstances`

```csharp
static Entity Spawn(EcsWorld world, string path);               // Places a scene asset in the world under a new entity, as an instance
static bool IsSubscene(string path);                            // Whether a path names a scene file, placed as a subscene, rather than a glTF scene
static bool IsInstance(EcsWorld world, Entity entity);          // Whether an entity is an instance's root
static string SceneOf(EcsWorld world, Entity root);             // The scene an instance places, as the asset path it was spawned from, or nothing
static IReadOnlyList<InstanceOverride> Overrides(EcsWorld world, Entity root);  // The overrides an instance holds, in the order they were made
static IReadOnlyList<string> Missed(EcsWorld world, Entity root);  // The paths the last application of an instance's overrides could not find, which are kept in the instance and written back
static Entity RootOf(EcsWorld world, Entity entity);            // The instance root an entity belongs to: the nearest ancestor that is one, or the entity itself, or none
static Entity OuterRootOf(EcsWorld world, Entity entity);       // The outermost instance root an entity belongs to, which is the one a scene file writes, or none
static string PathOf(EcsWorld world, Entity root, Entity node);  // A node's path of names from an instance's root, or nothing when it is not under the root
static Entity Find(EcsWorld world, Entity root, string path);   // The node a path of names leads to from an instance's root, or none
static bool Set(EcsWorld world, Entity node, string component, string field, object value);  // Writes a field of a component on a node of an instance, and records it as an override
static bool Mark(EcsWorld world, Entity node, string component, string field, object before = null);  // Records what a field on a node of an instance holds as an override, for a tool that wrote it some other way, such as an inspector writing through the field itself
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
```

### `SceneReferences`

```csharp
void Name(Entity entity, int id);                               // Numbers an entity that is being written, so a field referring to it can name it
void Spawned(int id, Entity entity);                            // Says which entity a local id was spawned as, so a field naming it can be read
bool WriteEntity(Utf8JsonWriter json, Entity entity);           // Writes a reference to an entity, reporting whether it could be named
object ReadEntity(JsonElement json);                            // Reads a reference to an entity, or nothing when the id names none
bool WriteAsset(Utf8JsonWriter json, AssetHandle asset);        // Writes a reference to an asset by its file, reporting whether it has one
object ReadAsset(JsonElement json, string kind);                // Reads a reference to an asset by loading its file as the kind of asset given
void WriteFile(Utf8JsonWriter json, string path);               // Writes a reference to a file under the asset root as its id and its path, or as its path alone when it has no id
static string ReadFile(JsonElement json);                       // The path a file reference names: where the file holding its id is now, with the label the reference gave, or the path it recorded when no file holds the id
```

### `SceneValue`

```csharp
static bool Write(Utf8JsonWriter json, ComponentField field, object value, SceneReferences references = null);  // Writes one value as the JSON its kind takes
static object Read(JsonElement json, ComponentField field, SceneReferences references = null);  // Reads one value of a field's kind, boxed as Write takes it, or null when the JSON is not that kind
static JsonElement Migrated(JsonElement json, ComponentSchema schema);  // A component object brought up to its type's current version, or the object as it is when it is current or the type has no migration
static int WriteComponent(Utf8JsonWriter json, ComponentSchema schema, EcsWorld world, Entity entity, SceneReferences references = null, IReadOnlyList<KeyValuePair<string, string>> kept = null);  // Writes every field of one component on an entity as an object, nesting the fields of a struct inside the struct's name
static int ReadComponent(JsonElement json, ComponentSchema schema, EcsWorld world, Entity entity, SceneReferences references = null);  // Writes every field a component object names onto an entity's component, leaving a field the object leaves out as it is
static IReadOnlyList<KeyValuePair<string, string>> Unread(JsonElement json, ComponentSchema schema);  // The values a component object holds that no field of the schema reads, by dotted name with their JSON
```

### `SaveGame`

```csharp
static void Carry(IPersistentValue value);                      // Makes a persistent value part of every save, copied into the save and put back when the save is loaded
static void Drop(IPersistentValue value);                       // Stops carrying a persistent value in saves
static IReadOnlyList<Entity> Start(EcsWorld world, params string[] scenes);  // Starts a game from scenes, and remembers which saved entities they hold, so a save can tell what play changed
static void Begin(EcsWorld world, params string[] scenes);      // Starts a game from scenes already loaded, remembering them and the saved entities they hold, as Start does after it loads them
static int Save(EcsWorld world, string path = "user://saves/slot.save.json");  // Writes what play changed over the scenes the game started from
static SceneLoad Load(EcsWorld world, string path = "user://saves/slot.save.json");  // Loads the scenes a save names and lays the save over them
```

### `SaveId`

```csharp
static SaveId New();                                            // A random id, which two entities are as good as certain never to share
static SaveId? Parse(string text);                              // An id written as sixteen hex digits, or nothing when the text is not one
```

### `Persistent<T>`

```csharp
void Set(T value);                                              // Replaces the value, without writing it
void Update(Func<T, T> change);                                 // Changes the value through a function of the old one, without writing it
void Persist();                                                 // Writes the value to its file
void Revert();                                                  // Throws away changes not yet written, by reading the file again
void Reset();                                                   // Puts the default back and writes it, as a settings screen's reset does
```

### `IPersistentValue`

```csharp
void Write(Utf8JsonWriter json);                                // Writes the value as JSON
bool Read(JsonElement json);                                    // Replaces the value with the one JSON holds, reporting whether it could be read
```

### `ProjectSettings`

```csharp
static ProjectSettings Read();                                  // The settings in the assets an app reads, or every one at its default when there is no file
static ProjectSettings ReadFrom(string assets);                 // The settings in a folder of assets on disk, for a tool reading them before any app exists
static ProjectSettings Parse(string text);                      // Reads settings from the text of a project file
void Write(string path);                                        // Writes the settings to a project file, leaving out the ones at their default
```

### `UserData`

```csharp
static string Resolve(string path);                             // The full path of a user:// path, or of a path relative to the directory
static void WriteAtomically(string full, ReadOnlySpan<byte> bytes);  // Writes text to a file through a temporary one renamed over it
```

## Assets and models

The guide's page is [assets-and-models.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/assets-and-models.md).

### `AssetServer`

```csharp
static AssetHandle LoadGltfMesh(string path, int mesh = 0, int primitive = 0);  // Starts loading one drawable piece of geometry out of a glTF file
static AssetHandle LoadImage(string path, TextureSettings settings);  // Starts loading an image with the sampler it should be drawn with
static AssetHandle LoadGltfMaterial(string path, int material = 0);  // Starts loading one material out of a glTF file, as the renderer's own material type
static AssetHandle LoadGltfScene(string path, int scene = 0);   // Starts loading one scene out of a glTF file
static string PathOf(AssetHandle handle);                       // The path an asset was loaded from, or null when it has none
static AssetHandle Load(string kind, string path);              // Starts loading an asset and returns a handle to it
static AssetLoadState StateOf(AssetHandle handle);              // How far along an asset's load is
static AssetLoadState StateWithDependenciesOf(AssetHandle handle);  // How far along an asset's load is, counting everything it depends on
static bool IsAlive(AssetHandle handle);                        // True when the engine is still holding this handle
static bool Release(AssetHandle handle);                        // Releases a handle
```

### `AssetFiles`

```csharp
static void Use(Assembly assembly);                             // Looks for the asset files in an assembly's resources, or in none, and in no pack
static void Use(Assembly assembly, AssetPack pack);             // Looks for the asset files in a pack and an assembly's resources, either of which may be nothing
static bool Exists(string path);                                // Whether a file is there, on disk or among what the game carries
static bool IsCarried(string path);                             // Whether a file is read from what the game carries, there being none on disk
static string ReadAllText(string path);                         // A file's text, from disk or from what the game carries
static byte[] ReadAllBytes(string path);                        // A file's bytes, from disk or from what the game carries
static IReadOnlyList<string> Carried(string suffix);            // The paths under the asset root of every carried file ending in suffix, such as the id sidecars a shipped game carries
```

### `AssetIds`

```csharp
static ulong IdOf(string path, bool create = false);            // The id of an asset file, or zero when it has none and create is false
static string PathOf(ulong id);                                 // Where the file with an id is now, or null when no sidecar under the root holds it
static void Move(string from, string to);                       // Moves a file and its sidecar together, so references to it keep working by id
static void Reindex();                                          // Reads every sidecar under the root again
static bool IsSidecar(string path);                             // Whether a path is a sidecar, which a list of assets leaves out
static int WriteIndex(string path = null);                      // Writes every id under the root, with the path it names, to one index file, for a shipped game to carry instead of the sidecars
static int IndexForShipping(string root);                       // Turns the sidecars under a folder into the one index a shipped game carries, writing IndexName at its root and deleting the sidecars
```

### `AssetPack`

```csharp
static AssetPack Open(string path);                             // Opens a pack and reads its index
bool Contains(string path);                                     // Whether the pack holds a file
Stream OpenFile(string path);                                   // A file in the pack, opened for reading and seeking, or nothing when the pack lacks it
static int Write(string folder, string pack, Func<string, bool> include = null);  // Writes a pack of every file under a folder that include takes
void Dispose();                                                 // Closes the pack
```

### `Streaming`

```csharp
static StreamRead Read(string path, long offset = 0, int length = -1, int priority = 0);  // Starts reading length bytes at offset of a file on a worker thread, and answers the read at once
static bool TryTake(StreamRead read, out byte[] bytes);         // Takes what a read brought back, if it has finished, fits this frame's budget, and no finished read of higher priority is waiting
static void Cancel(StreamRead read);                            // Stops a read, or forgets one that finished and was never taken
```

### `GltfContents`

```csharp
static IReadOnlyList<GltfPart> Read(string path);               // The meshes, materials and textures a glTF file holds, or nothing when it cannot be read as one
```

### `GltfPart`

```csharp
string PathIn(string model);                                    // The asset path that loads this part of model
```

### `Animation`

```csharp
static bool TryClips(Entity scene, out IReadOnlyList<string> clips);  // The names of a model's clips, or false while it has not arrived
static bool Play(Entity scene, string clip, AnimationSettings settings = null);  // Plays one of a model's clips, fading out whatever played before
static void Stop(Entity scene);                                 // Stops every clip on a model, leaving it in the pose it was in
static void Pause(Entity scene);                                // Holds what a model is playing where it is
static void Resume(Entity scene);                               // Lets what a model is playing go on from where it was held
static void Seek(Entity scene, float seconds);                  // Moves the clip a model is playing to a time from its start
static void SetRepeat(Entity scene, uint times);                // Changes how many times the clip playing plays, counting those it has finished, without starting it over: zero for ever, one for once
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
```

### `AnimationCurve`

```csharp
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
```

### `AnimationTarget`

```csharp
static AnimationTarget FromNames(ReadOnlySpan<string> names);   // The target at the end of a path of names, the first the topmost
```

### `MeshFiles`

```csharp
static bool IsMeshFile(string path);                            // Whether a path names a mesh file
static AssetHandle Load(string path);                           // The mesh a file holds, loaded the first time it is asked for and shared from then on
static string PathOf(AssetHandle mesh);                         // The file a mesh was loaded from or saved to, or nothing for one that has none
static void SaveAs(AssetHandle mesh, string path);              // Writes a mesh made in memory to a new file and makes the mesh that file's, so a scene refers to the file from then on
static bool Save(AssetHandle mesh);                             // Writes a mesh loaded from a file back to that file, as it is now
```

### `MaterialFiles`

```csharp
static bool IsMaterialFile(string path);                        // Whether a path names a material file
static AssetHandle Load(string path);                           // The material a file holds, loaded the first time it is asked for and shared from then on
static string PathOf(AssetHandle material);                     // The file a material was loaded from, or nothing for one made in memory
static void SaveAs(AssetHandle material, string path);          // Writes a material's settings to a new file and makes the material that file's, so saving it again writes there and a scene refers to the file
static bool Save(AssetHandle material);                         // Writes a material loaded from a file back to that file, as it is now
```

## Drawing

The guide's page is [drawing.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/drawing.md).

### `Render`

```csharp
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
static void SetPostProcessing(Entity camera, PostSettings settings);  // Sets what a camera does to the picture after the scene has been drawn
static void SetEffects(Entity camera, EffectSettings settings);  // Sets the lens a camera draws through
static void SetAtmosphere(Entity camera, AtmosphereSettings settings);  // Draws the sky the air scatters, seen from a camera
static void ClearAtmosphere(Entity camera);                     // Stops a camera drawing the sky
static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f);  // Builds a mesh primitive and returns a handle to it
static MeshRecipe? RecipeOf(AssetHandle mesh);                  // The shape and measures a mesh was made from, or null for one that was not made by CreateMesh
static bool RebuildMesh(AssetHandle mesh, string shape, float a = 1f, float b = 1f, float c = 1f);  // Builds a primitive again with other measures, in place, so everything drawn with the mesh changes and keeps its handle
static MeshData DataOf(AssetHandle mesh);                       // The geometry a mesh was built from, or null for one that was not made by CreateMesh
static bool TryGetMeshInfo(AssetHandle mesh, out MeshInfo info);  // What a mesh is made of: its counts, its attributes and its bounds, read without copying its vertices
static bool TryReadMaterial(AssetHandle material, out MaterialSettings settings);  // A standard material's settings, read back from the engine, whether code made the material or a glTF file brought it
static AssetHandle CreateMesh(MeshData mesh);                   // Builds a mesh from vertices and returns a handle to it
static void WriteMesh(AssetHandle mesh, MeshData data);         // Writes vertices over a mesh, so everything drawn with it changes
static void SetMeshJoints(AssetHandle mesh, ReadOnlySpan<ushort> joints, ReadOnlySpan<float> weights);  // Gives a mesh made in code the joints each vertex follows and how much each of them moves it, which a skin then bends it by
static AssetHandle CreateSkin(ReadOnlySpan<Transform> inverseBindposes);  // Makes a skin, Bevy's inverse bindposes, one for each joint
static void SetSkin(Entity entity, AssetHandle skin, ReadOnlySpan<Entity> joints);  // Skins the mesh an entity draws with a skin and the joints that move it, Bevy's SkinnedMesh
static bool TryReadImage(AssetHandle image, out ImagePixels pixels);  // Reads an image's texels from the copy the app keeps of it, or answers false while it is loading or where no copy is kept
static bool TryImageSize(AssetHandle image, out uint width, out uint height, out uint depth);  // Reads how large an image is in texels, once it has loaded
static void WriteImagePixels(AssetHandle image, ReadOnlySpan<byte> texels);  // Writes texels over the copy an image keeps, as many bytes as it holds, so the GPU is given them again and everything showing the image changes
static bool TryReadMesh(AssetHandle mesh, out MeshData triangles);  // Reads a mesh's triangles back: where each vertex is, and which three make each triangle
static bool TryReadNormals(AssetHandle mesh, out Vec3[] positions, out Vec3[] normals);  // Reads a mesh's positions back with the normal at each
static void SetMeshFlags(EcsWorld world, Entity entity, MeshFlags flags);  // Says how an entity's mesh is treated beyond what it looks like
static AssetHandle CreateMaterial(float red, float green, float blue, float alpha = 1f, float metallic = 0f, float roughness = 0.5f);  // Builds a physically based material and returns a handle to it
static AssetHandle CreateMaterial((float R, float G, float B, float A) color);  // Builds a material of one color and returns a handle to it, as Bevy makes a StandardMaterial from a Color
static AssetHandle CreateMaterial(MaterialSettings settings);   // Builds a material from settings and returns a handle to it
static bool WriteMaterial(AssetHandle material, MaterialSettings settings);  // Writes settings over a standard material in place, so everything drawn with it changes
static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh);  // Gives an entity a mesh to draw
static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material);  // Gives an entity a material to draw its mesh with
static void Screenshot(string path);                            // Writes what is being drawn to a PNG file
static void Screenshot(string path, AssetHandle target);        // Writes what a camera drew into an image to a PNG file
static void Screenshot(string path, Entity window);             // Writes what a window the game spawned shows to a PNG file
static void SetImageLighting(Entity camera, AssetHandle cubemap, float intensity = 1000f, Quat? rotation = null);  // Lights the scene from a cubemap, filtered on the GPU
static void SetEnvironmentMap(Entity camera, AssetHandle diffuse, AssetHandle specular, float intensity = 1000f, Quat? rotation = null);  // Lights the scene from a pair of cubemaps somebody baked earlier
static void SetReflectionProbe(Entity probe, AssetHandle diffuse, AssetHandle specular, float intensity = 1000f, Vec3? falloff = null);  // Makes an entity a reflection probe: a box inside which surfaces reflect a pair of baked cubemaps rather than the camera's environment
static void SetIrradianceVolume(Entity probe, AssetHandle voxels, float intensity = 1000f, Vec3? falloff = null);  // Makes an entity an irradiance volume: a box inside which surfaces take their diffuse indirect light from a grid of points held in a 3D image
static void SetProbeCapture(Entity probe, ProbeCaptureSettings settings);  // Makes an entity a reflection probe that renders what is around it, or with null stops
static void RecaptureProbe(Entity probe);                       // Captures a probe that is not live again, after what is around it has changed
static void SetSkyLighting(Entity camera, float intensity = 1f, uint size = 512);  // Lights the scene from the sky this camera is already scattering
static void ClearSkyLighting(Entity camera);                    // Stops lighting the scene from the sky
static void SetSkybox(Entity camera, AssetHandle cubemap, float brightness = 1000f, Quat? rotation = null);  // Draws a cubemap behind everything a camera draws
static void SetColorGrading(Entity camera, GradingSettings settings);  // Grades the picture a camera drew, after tonemapping
static void SetExposure(Entity camera, float ev100);            // Sets the exposure a camera meters the scene at, in EV-100
static void SetLensExposure(Entity camera, float aperture = 0f, float shutter = 0f, float sensitivity = 0f);  // Sets a camera's exposure from the lens it stands in for
static void SetLens(Entity camera, PhysicalLens lens);          // Meters a camera from a lens written down once (PhysicalLens)
static void SetSortedTransparency(Entity camera, bool on = true, uint layers = 0, float average = 0f, float threshold = 0f);  // Sorts transparent fragments rather than whole objects, for one camera
static void SetAmbientLight((float R, float G, float B) color, float brightness);  // Sets the ambient light every camera without its own is lit by
static void SetAmbientLight(Entity camera, (float R, float G, float B)? color, float brightness = 80f);  // Gives a camera an ambient light of its own, or with null takes it away so the camera is lit by everyone's again
static void SetAmbientOcclusion(Entity camera, AmbientOcclusionQuality? quality, float thickness = 0f);  // Turns Bevy's screen-space ambient occlusion on for a camera at a quality, or with null off
static void SetDeferredRendering(bool on);                      // Draws Bevy's own materials deferred, into a G-buffer lit afterward, or forward, lit as they are drawn, which is the default
static void SetContactShadows(Entity camera, ContactShadowSettings settings);  // Draws contact shadows on a camera, or with null stops
static void SetScreenSpaceReflections(Entity camera, ReflectionSettings settings);  // Turns Bevy's screen-space reflections on for a camera, or with null off
static Capture BeginCapture(AssetHandle target);                // Asks for a picture to be read back into memory rather than written to a file
static Capture BeginCapture();                                  // Asks for a picture of whatever this run is drawing into
static bool TryReadCapture(Capture capture, out CapturedImage picture);  // Reads a capture once it has arrived, and forgets it
static bool TryReadCaptureAsItIs(Capture capture, out CapturedTexels picture);  // Reads a capture once it has arrived, in the format it was drawn in, and forgets it
static void ReleaseCapture(Capture capture);                    // Forgets a capture that will not be read
static AssetHandle CreateTarget(uint width, uint height, TargetFormat format = TargetFormat.Rgba8, uint layers = 1);  // Creates an empty image a camera can draw into
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
static IReadOnlyList<PassTiming> Timings();                     // How long each render pass took, smoothed over the last frames, where the app asked for it with GpuTimings
static bool PipelinesReady();                                   // True once the renderer has compiled every pipeline it was asked for, so whatever has been spawned can be drawn
static void SetRayTracedLighting(Entity camera, bool on);       // Lights a camera with Bevy's ray tracing, or with false the usual way again
static void SetRayTraced(Entity entity, AssetHandle mesh);      // Makes an entity's mesh one the rays of ray-traced lighting meet
static AssetHandle CreateMeshletMesh(AssetHandle mesh, uint quantization = 0, string saveTo = null);  // Starts cutting a mesh into clusters that Bevy's meshlet renderer culls and picks a level of detail for on the GPU, and answers the meshlet mesh at once
static void SetMeshletMesh(EcsWorld world, Entity entity, AssetHandle meshlet);  // Gives an entity a meshlet mesh to draw, in place of any ordinary mesh it had
static AssetHandle CreateClusterMaterial();                     // Makes a material that draws each cluster of a meshlet mesh in a color of its own
static string MeshPathOf(Entity entity);                        // Where an entity's mesh was loaded from, or empty when it was not loaded from anywhere
static string MaterialPathOf(Entity entity);                    // Where an entity's material was loaded from, or empty when it was not
static AssetHandle MeshOf(EcsWorld world, Entity entity);       // The mesh an entity is drawn with, or None when it has none
static AssetHandle MaterialOf(EcsWorld world, Entity entity);   // The standard material an entity is drawn with, or None when it has none, as MeshOf reads the mesh
static bool IsDrawn(Entity entity);                             // Whether an entity carries a mesh the renderer draws
```

### `MeshShape`

```csharp
static string Ring(string outline);                             // A flat band along the inside of another flat shape's outline, sized by that shape's measures and, third, how wide the band is
static string Extrusion(string outline);                        // A flat shape pushed out into a solid along Z, sized by that shape's measures and, third, how deep it is, centered on its middle
static string UvAngle(string arc);                              // A sector or a segment whose image is mapped at an angle, sized by its radius, the half angle it spans and, third, the angle in radians
```

### `Render2d`

```csharp
static Entity SpawnCamera2d(int order = 0);                     // Spawns a 2D camera and returns it
static AssetHandle CreateAtlas(uint tileWidth, uint tileHeight, uint columns, uint rows, (uint X, uint Y) padding = default, (uint X, uint Y) offset = default);  // Builds an atlas layout over a grid of equal tiles and returns it
static void SetSprite(EcsWorld world, Entity entity, AssetHandle image);  // Attaches a sprite to an entity, or replaces the one it has
static void SetSprite(EcsWorld world, Entity entity, AssetHandle image, SpriteSettings settings);  // Attaches a sprite drawn as settings describes
static int SetSpriteFrames(ReadOnlySpan<Entity> sprites, ReadOnlySpan<uint> frames);  // Moves sprites and sprite meshes to frames of their sheets, each to the frame beside it, in one call
static AssetHandle CreateMaterial(ColorMaterialSettings settings);  // Makes a 2D mesh's material and returns it
static void WriteMaterial(AssetHandle material, ColorMaterialSettings settings);  // Writes settings over a 2D mesh's material in place, so every mesh drawn with it changes
static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh);  // Gives an entity a mesh for a 2D camera to draw
static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material);  // Gives an entity a 2D material, from CreateMaterial, to draw its mesh with
static void SetTilemap(EcsWorld world, Entity entity, TilemapChunk chunk, ReadOnlySpan<TileData?> tiles);  // Makes an entity a tilemap chunk with its tiles, Bevy's TilemapChunk and TilemapChunkTileData
static void SetTiles(EcsWorld world, Entity entity, int start, ReadOnlySpan<TileData?> tiles);  // Writes tiles over a chunk's from start on, which Bevy draws from the next frame
static TileData? TileAt(EcsWorld world, Entity entity, int index);  // A chunk's tile at index, or null for an empty cell
```

### `TilemapChunk`

```csharp
int IndexOf(uint x, uint y);                                    // The place in the chunk's tiles of the tile x across and y up
Transform TileTransform(uint x, uint y);                        // Where the tile x across and y up sits, its middle, in the chunk's own space, Bevy's calculate_tile_transform
```

### `TileData`

```csharp
static TileData FromTilesetIndex(ushort tilesetIndex);          // A tile drawn from a layer of the tileset, white, shown and upright, Bevy's from_tileset_index
```

### `CapturedImage`

```csharp
(byte R, byte G, byte B, byte A) At(uint x, uint y);            // The color at a point, as four bytes
byte[] ToPng();                                                 // The picture as a PNG file's bytes, alpha included
```

### `CapturedTexels`

```csharp
Vector4 ColorAt(uint x, uint y);                                // The color at a point as four floats, linear for a float format and the stored value divided by 255 for an eight-bit one
```

### `EffectSettings`

```csharp
EffectSettings Through(PhysicalLens lens);                      // Takes the depth of field's aperture and sensor from a lens, the one SetLens meters the same camera from
```

### `Picking`

```csharp
static Entity[] Drain();                                        // Takes the scene entities clicked since the last call, none where meshes are not picked
static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal);  // The nearest mesh a ray meets, where, and which way the surface there faces
static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal, out Vec2? uv);  // The nearest mesh a ray meets, as TryCast says, and where on its texture
static PointerId SpawnPointer();                                // Makes a pointer of the game's own, which it moves and presses itself
static void MovePointer(PointerId pointer, AssetHandle image, Vec2 position);  // Moves a pointer from SpawnPointer to a place on an image
static void PressPointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary);  // Presses a button of a pointer from SpawnPointer where it is put
static void ReleasePointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary);  // Lets a button of a pointer from SpawnPointer go where it is put
```

## Shaders

The guide's page is [shaders.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/shaders.md).

### `Shaders`

```csharp
static AssetHandle CreateBuffer(int size);                      // Makes a buffer of size bytes that shaders read and write, holding zeros
static AssetHandle CreateBuffer<T>(ReadOnlySpan<T> items, int size = 0);  // Makes a buffer holding items, at least size bytes long
static void WriteBuffer<T>(AssetHandle buffer, ReadOnlySpan<T> items);  // Replaces a buffer's contents with items, padded with zeros to its size
static int GrowBuffer(AssetHandle buffer, int size);            // Makes a buffer at least size bytes, keeping what it holds, and answers its size
static AssetHandle CreateInstanceBuffer(int capacity);          // Makes a buffer with capacity slots that the engine fills every frame with the transforms of the entities put in them, this frame's and the previous frame's
static GeometryPool CreateGeometryPool();                       // Makes an empty geometry pool, buffers holding the vertices and triangles of every mesh added to it, which any shader reaches by a mesh's number and a triangle's
static int AddToGeometryPool(GeometryPool pool, AssetHandle mesh);  // Adds a mesh to a geometry pool and answers its number there, counted from zero, which is the index of its bcs_scene::PoolMesh
static RayScene CreateRayScene(GeometryPool pool, int capacity);  // Makes a scene rays are traced through, over the meshes of a geometry pool, with capacity slots for the entities in it
static void SetRaySceneInstance(RayScene scene, int slot, Entity entity, int mesh);  // Puts an entity made of pool mesh mesh in a slot of a ray scene, where rays meet it at its transform every frame, or empties the slot with None
static void RebuildRayScene(RayScene scene, int? mesh = null);  // Builds a ray scene's pool mesh again from what the pool's buffers hold now, or every mesh with null
static AssetHandle CreateMaterialBuffer(int capacity);          // Makes a buffer with capacity slots that the engine fills every frame with what the entities put in them are made of, from their standard materials
static void SetInstance(AssetHandle buffer, int slot, Entity entity);  // Puts an entity in a slot of an instance or material buffer, or with None empties the slot
static int BufferSize(AssetHandle buffer);                      // A buffer's size in bytes
static BufferRead BeginBufferRead(AssetHandle buffer);          // Starts copying a buffer back from the GPU. Only valid inside a system
static BufferRead BeginImageRead(AssetHandle image);            // Starts copying an image back from the GPU, as BeginBufferRead copies a buffer
static bool TryReadBuffer(BufferRead read, out byte[] bytes);   // The bytes a read brought back, once they have arrived
static bool TryReadBuffer<T>(BufferRead read, out T[] items);   // The elements a read brought back, once they have arrived
static AssetHandle CreateImage(uint width, uint height, ShaderImageFormat format = ShaderImageFormat.Rgba8, uint depth = 1, uint mips = 1);  // Makes an image a compute shader writes and anything samples
static AssetHandle CreateImage<T>(uint width, uint height, ShaderImageFormat format, ReadOnlySpan<T> texels, uint depth = 1);  // Makes an image as CreateImage does, starting with texels rather than zeros
static void WriteImage<T>(AssetHandle image, ReadOnlySpan<T> texels, uint x, uint y, uint width, uint height, uint z = 0, uint depth = 1, uint mip = 0);  // Writes texels into a region of an image, width by height at x, y, on the GPU before this frame's work runs
static int TexelBytes(ShaderImageFormat format);                // How many bytes one texel of format takes, or for a block-compressed format one four by four block
static ShaderProgram CreateProgram(ShaderStage fragment);       // Makes a program drawn by one fragment shader, leaving the rest to Bevy
static ShaderProgram CreateProgram(ShaderProgramSettings settings);  // Makes a program from the Slang named
static ShaderMaterial CreateMaterial(ShaderProgram program, AlphaMode alpha = AlphaMode.Opaque);  // Makes a material drawn by a program
static ShaderMaterial CreateMaterial(ShaderMaterialSettings settings);  // Makes a material drawn by a program
static ShaderMaterial MaterialOn(Entity entity);                // The shader material an entity is drawn with, to read or set its values
static ShaderProgram ProgramOn(Entity entity);                  // Which program draws an entity's material, or None where the entity is not drawn by one
static ShaderInstance CreateInstance(ShaderProgram program);    // Makes an instance of a program, which a pass over a camera's picture or a compute dispatch runs
static void SetPasses(Entity camera, params ShaderPass[] passes);  // Replaces the full-screen passes a camera runs over what it drew, in the order given
static void SetPrepass(Entity camera, bool depth, bool normals = false, bool motion = false, bool deferred = false, bool previous = false, bool pyramid = false);  // Asks a camera to draw its depth, its normals, its motion vectors or any of them before the scene, for its passes and its compute shaders to read
static void SetViewImages(Entity camera, params ViewImage[] images);  // Gives a camera images that its passes and compute shaders keep from frame to frame, replacing any it had
static IReadOnlyList<string> DrawnViewImageNames(Entity camera);  // The names a Watch on the camera would find, as of the last frame it drew: its own images and those of EngineViewImageNames it has
static IReadOnlyList<string> ViewImageNames(Entity camera);     // The names of the images a camera owns, as SetViewImages last gave them, with their _previous and _mip names
static AssetHandle Watch(Entity camera, string name, uint width = 320, uint height = 180, float scale = 1f, float offset = 0f);  // Starts watching one of a camera's images
static void Unwatch(Entity camera, string name);                // Stops watching one of a camera's images
static void SetViewDispatches(Entity camera, params ViewDispatch[] dispatches);  // Replaces the compute shaders a camera runs every frame, in order
static void Dispatch(ShaderInstance instance, uint x, uint y = 1, uint z = 1);  // Runs an instance's compute shader once, this frame, before any camera draws
static void SetViewDraws(Entity camera, params ViewDraw[] draws);  // Replaces the geometry a camera draws every frame out of buffers, in order
static void DispatchIndirect(ShaderInstance instance, AssetHandle buffer, uint offset = 0);  // Runs an instance's compute shader once, this frame, before any camera draws, with as many workgroups as the buffer says
```

### `ShaderValues`

```csharp
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
static T SetNumbers<T, TItem>(T target, string name, int components, ReadOnlySpan<TItem> numbers);  // Sets numbers components to an element, for the shapes C# has no type for: an int2, a uint3, a float3x3, or an array of any of them
static T SetBytes<T, TItem>(T target, string name, ReadOnlySpan<TItem> items);  // Copies bytes, as they are, to where a name is: a struct, an array of structs, or a whole ConstantBuffer
static T SetStruct<T, TItem>(T target, string name, TItem value);  // Sets a struct, as its bytes
static T SetTexture<T>(T target, string name, AssetHandle image, int index = 0, int mip = -1);  // Puts an image on a texture, at index in an array of them, or takes it off again with None
static T SetRayScene<T>(T target, string name, RayScene scene);  // Puts a RayScene on an acceleration structure the shader traces, or takes it off again with default
static T SetBuffer<T>(T target, string name, AssetHandle buffer);  // Puts a buffer from CreateBuffer on a storage buffer, or takes it off again with None
static T SetSampler<T>(T target, string name, SamplerSettings settings, int index = 0);  // Says how a sampler reads, at index in an array of them
static T Unset<T>(T target, string name);                       // Takes the value set under a name off, so the shader reads zeros or a stand-in there again
static float[] GetFloats<T>(T target, string name);             // The floats set under a name, or an empty array where nothing was
static int[] GetInts<T>(T target, string name);                 // The signed integers set under a name
static uint[] GetUInts<T>(T target, string name);               // The unsigned integers set under a name
```

### `ShaderMaterial`

```csharp
ShaderMaterial Configure(AlphaMode alpha, float cutoff = 0.5f, CullMode cull = CullMode.Back, float depthBias = 0f);  // Changes what the renderer does where the material is not opaque, which faces it leaves undrawn and how far its depth is pushed toward the camera
```

### `ShaderProgram`

```csharp
void Reload();                                                  // Compiles every stage again now, whether a file changed or not
```

### `ShaderStage`

```csharp
static ShaderStage Slang(string source, string entry = null);   // A stage made from Slang handed over as text
```

### `ViewDispatch`

```csharp
static ViewDispatch PerPixel(ShaderInstance instance, FramePoint point, uint groupX = 8, uint groupY = 8, float scale = 1f);  // Enough workgroups of groupX by groupY pixels to cover scale of the picture, for a shader working a pixel at a time
static ViewDispatch Fixed(ShaderInstance instance, FramePoint point, uint x, uint y = 1, uint z = 1);  // Exactly this many workgroups
static ViewDispatch Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0);  // As many workgroups as three unsigned integers in a buffer say, when it runs
```

### `ViewDraw`

```csharp
static ViewDraw Fixed(ShaderInstance instance, FramePoint point, uint vertices, uint instances = 1, DrawBlend blend = DrawBlend.Opaque, bool writesDepth = true);  // vertices vertices, instances times
static ViewDraw Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0, DrawBlend blend = DrawBlend.Opaque, bool writesDepth = true);  // As many vertices and instances as four unsigned integers in a buffer say when it runs: vertices, instances, the first vertex and the first instance
```

## Gizmos

The guide's page is [gizmos.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/gizmos.md).

### `Gizmos`

```csharp
static void Fade(Vec3 start, Vec3 end, (float R, float G, float B, float A) from, (float R, float G, float B, float A) to, bool inFront = true);  // Draws a line that fades from one color to another along its length
static void Line(Vec3 start, Vec3 end, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a line between two points
static void Sphere(Vec3 center, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a sphere
static void Arrow(Vec3 start, Vec3 end, (float R, float G, float B, float A) color, bool inFront = true, float tipLength = 0f, bool doubleEnd = false);  // Draws a line with a head on its far end
static void Circle(Vec3 center, Quat rotation, float radius, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of a circle
static void Ellipse(Vec3 center, Quat rotation, float halfWidth, float halfHeight, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws the outline of an ellipse
static void Arc(Vec3 center, Quat rotation, float radius, float angle, (float R, float G, float B, float A) color, bool inFront = true, uint resolution = 0);  // Draws part of a circle
static void Rect(Vec3 center, Quat rotation, float width, float height, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a rectangle
static void RoundedRect(Vec3 center, Quat rotation, float width, float height, (float R, float G, float B, float A) color, float? cornerRadius = null, bool inFront = true, uint arcResolution = 0);  // Draws the outline of a rectangle with rounded corners
static void Box(Vec3 center, Quat rotation, Vec3 size, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the twelve edges of a box
static void RoundedCuboid(Vec3 center, Quat rotation, Vec3 size, (float R, float G, float B, float A) color, float? edgeRadius = null, bool inFront = true, uint arcResolution = 0);  // Draws the edges of a box with rounded edges and corners
static void Capsule(Vec3 center, Quat rotation, float radius, float length, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a capsule
static void Cone(Vec3 center, Quat rotation, float radius, float height, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a cone
static void Cylinder(Vec3 center, Quat rotation, float radius, float halfHeight, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a cylinder
static void Torus(Vec3 center, Quat rotation, float major, float minor, (float R, float G, float B, float A) color, bool inFront = true);  // Draws the outline of a torus
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
static AssetHandle Record(Action draw);                         // Keeps the shapes draw asks for in an asset, rather than drawing them for one frame, for an entity to draw every frame with Attach
static void Attach(EcsWorld world, Entity entity, AssetHandle gizmo);  // Has an entity draw a gizmo asset Record made, every frame, placed by the entity's transform
static void Axes(Transform transform, float length = 1f, bool inFront = true);  // Draws a set of axes, so an orientation can be read at a glance
static Gizmos.BatchScope Batch();                               // Gathers every shape drawn until the returned scope ends, and hands them over in one call
static void Text(string text, Vec3 position, Quat rotation, float size, (float X, float Y) anchor, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a run of text in the world, in Bevy's stroke font, facing the way it is turned
static void Text2d(string text, (float X, float Y) position, float angle, float size, (float X, float Y) anchor, (float R, float G, float B, float A) color, bool inFront = true);  // Draws a run of text, flat, for a 2D camera
```

### `Gizmos.BatchScope`

```csharp
void Dispose();                                                 // Hands the gathered shapes to the engine
```

### `GizmoSegment`

```csharp
static GizmoSegment Fading(Vec3 start, Vec3 end, (float R, float G, float B, float A) from, (float R, float G, float B, float A) to);  // A line that fades from one color to another
```

## The interface

The guide's page is [ui.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/ui.md).

### `Ui`

```csharp
static Entity SpawnNode(UiSettings settings);                   // Spawns a rectangle and returns it
static Entity SpawnText(string text, UiSettings settings, float fontSize = 20f);  // Spawns a run of text and returns it
static Entity SpawnText(string text, UiSettings settings, UiTextSettings style);  // Spawns a run of text, set as style describes
static Entity SpawnTextSpan(Entity parent, string text, UiTextSettings style, (float R, float G, float B, float A) color);  // Adds a run of text to an existing one, set in its own font and color
static void SetText(Entity entity, string text);                // Replaces what a text entity says
static UiInteraction InteractionOf(Entity entity);              // Reports how the pointer stands on an interactive node
static void SetImage(Entity entity, AssetHandle image);         // Draws an image inside a node, or replaces the one it draws
static void SetImage(Entity entity, UiImageSettings settings);  // Draws an image inside a node, tinted, cut down or sliced
static void SetScroll(Entity entity, float x, float y);         // Moves a scrolling node's contents inside it
static void Focus(Entity entity, FocusCause cause = FocusCause.Navigated);  // Gives entity the input focus, so the keys go to it
static Entity? Navigate(NavAction action);                      // The entity the focus would move to along the tab order, without moving it
static void SetUnderline(Entity text, bool underlined = true);  // Draws a run of text with a line under it, or takes the line off
static void SetStrikethrough(Entity text, bool struck = true);  // Draws a run of text with a line through it, or takes the line off
static void SetFontFeatures(Entity text, ReadOnlySpan<(string, uint)> features);  // Sets the OpenType features a run of text is drawn with, replacing those it had
static void SetFontVariations(Entity text, ReadOnlySpan<(string, float)> variations);  // Sets where a variable font sits on each of its axes for a run of text, replacing what it had
static void SetEditableText(Entity node, UiEditableTextSettings settings);  // Makes a node a text field the player types into
static string EditableTextOf(Entity node);                      // What a text field holds, or null for a node that is no field
static void SetEditableValue(Entity node, string text);         // Replaces what a text field holds, its cursor put at the end
static void SelfUpdate(Entity widget, UiWidgetKind kind);       // Makes one of Bevy's widgets keep its own state as the player works it
```

### `UiGrid`

```csharp
static void Set(Entity node, GridSettings settings);            // Lays a node's children out on a grid
static void Place(Entity node, GridPlacement placement);        // Places one child on its parent's grid
```

### `Length`

```csharp
static Length Px(float value);                                  // A distance in logical pixels
static Length Percent(float value);                             // A share of the parent, where 100 is all of it
```

### `Sides`

```csharp
static Sides All(Length value);                                 // The same length on every side
static Sides Horizontal(Length value);                          // Left and right, with nothing at the top and bottom
static Sides Vertical(Length value);                            // Top and bottom, with nothing at the left and right
```

### `Corners`

```csharp
static Corners All(Length value);                               // The same radius on every corner
static Corners Top(Length value);                               // Rounded along the top edge only, like a tab
static Corners Bottom(Length value);                            // Rounded along the bottom edge only
```

### `Track`

```csharp
static Track Px(float value);                                   // A fixed number of logical pixels
static Track Percent(float value);                              // A percentage of the grid across that axis
static Track Fr(float share);                                   // A share of whatever room is left after the fixed tracks have taken theirs
static Track Flex(float share);                                 // A share of the room left over, as Fr is, that may also be narrower than what it holds
Track Repeated(int count);                                      // The same track, stated count times over
Track Filling(bool collapse = false);                           // The same track, repeated as many times as the grid has room for
```

### `ImGuiRuntime`

```csharp
static void Start(string fonts = null, float size = 15f, params string[] faces);  // Creates the context and hands the engine the font atlas
static ImFontPtr Face(string face);                             // One of the loaded faces, or whatever is in force when it was not loaded
static void Begin(BehaviorContext ctx);                         // Starts a frame: how large the window is, what the pointer did, what was typed
static void End();                                              // Ends the frame and hands the engine what came of it
```

### `Navigation`

```csharp
static Entity? Move(CompassOctant direction);                   // Moves the input focus to the node beside the one holding it in a direction, and answers the node, or null where nothing lies that way, an edge blocks it or nothing holds the focus
static void AddEdge(Entity from, Entity to, CompassOctant direction, bool bothWays = false);  // Draws an edge from one node to another in a direction, and back the opposite way where asked
static void BlockEdge(Entity node, CompassOctant direction, Entity other = default);  // Blocks a direction a node would otherwise be left by, and the opposite one from another node where asked
static void AddEdges(ReadOnlySpan<Entity> nodes, CompassOctant direction, bool looping = false);  // Draws edges between nodes in their order in a direction, each to the next and back, and around from the last where asked
static void Forget(Entity node);                                // Takes a node's edges out, those from it and to it, as one taken off the screen needs
static void Clear();                                            // Takes every edge out, leaving the nearest nodes on the screen
```

### `ImGuiTextures`

```csharp
static ulong Load(string path);                                 // What to call the picture at a path, loading it the first time it is asked for
static ulong Of(AssetHandle image);                             // What to call a picture the caller already holds, such as one a camera is drawing into
static (uint Width, uint Height) SizeOf(ulong picture);         // How large a picture is, in pixels, or zero by zero while it is still loading
static void Draw(string path, float size, Vector4 tint);        // Draws a picture, tinted
static bool Button(string id, string path, float size, Vector4 tint);  // A button that is a picture
```

## Audio

The guide's page is [audio.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/audio.md).

### `Audio`

```csharp
static float BusVolume(string bus);                             // A bus's volume, which is one for a bus nobody has set and for no bus at all
static void SetBusVolume(string bus, float volume);             // Sets a bus's volume, which every sound on it is heard at times its own
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

### `Physics.PhysicsWorld`

```csharp
bool IsCharacter(Entity entity);                                // Whether an entity's body is a character's
bool Has(Entity entity);                                        // Whether an entity has a body
void Add(Entity entity, PhysicsShape shape, BodyKind kind, Transform at, float mass = 1f, bool sensor = false, PhysicsMaterial? material = null);  // Gives an entity a body, starting where at puts it
void SetMaterial(Entity entity, PhysicsMaterial material);      // Changes how a body's surface slides and bounces, from the next step
bool Remove(Entity entity);                                     // Takes an entity's body away, leaving the entity where it is
(Vec3 Linear, Vec3 Angular) Velocity(Entity entity);            // A body's velocity: how fast it moves, and how fast it turns about each axis
void SetVelocity(Entity entity, Vec3 linear, Vec3 angular = default);  // Sets a dynamic body's velocity, waking it if it had come to rest
void ApplyImpulse(Entity entity, Vec3 impulse, Vec3 offset = default);  // Pushes a dynamic body with an impulse, a change in momentum, at a point offset from its center, which turns it as well where the point is off center
bool IsAsleep(Entity entity);                                   // Whether a dynamic body has come to rest and stopped being simulated
PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance);  // The nearest body a ray meets within distance, or null for none
void Step(EcsWorld ecs, float seconds, MessageBus messages = null);  // Advances the simulation by seconds: kinematic bodies follow their entities, everything is stepped, and dynamic bodies are written back
void Dispose();                                                 // Tears the simulation down, returning its memory
JointHandle Connect(Entity a, Entity b, Joint joint);           // Joins two bodies with a joint, which holds from the next step on
bool SetMotor(JointHandle joint, float degreesPerSecond, float torque);  // Changes a hinge's motor while it runs, to open a door on command or stop a fan
bool Disconnect(JointHandle joint);                             // Takes a joint away, leaving both bodies free
void MarkPlaced(Entity entity);                                 // Says that entity was put where it is rather than moved there, as when a level starts again, so its kinematic body is put at the new place, at rest, and not carried there through whatever is between
void Sync(EcsWorld ecs);                                        // Makes, remakes and takes away the bodies of entities carrying a RigidBody and a Collider, so the simulation holds what the world says
```

### `Physics.PhysicsPlugin`

```csharp
void Build(App app);                                            // Registers this plugin's contributions on app
```

### `Physics.PhysicsShape`

```csharp
PhysicsShape Moved(Vec3 offset);                                // The same shape with its middle moved off the entity's origin
static PhysicsShape Box(Vec3 size);                             // A box of the given full size along each axis
static PhysicsShape Sphere(float radius);                       // A sphere of the given radius
static PhysicsShape Capsule(float radius, float length);        // A capsule standing along Y: a cylinder of length with a half sphere of radius at each end, the shape a character usually collides as
static PhysicsShape Cylinder(float radius, float length);       // A cylinder standing along Y
static PhysicsShape Mesh(ReadOnlySpan<Vec3> positions, ReadOnlySpan<uint> indices);  // Triangles, as a level's floors and walls are, from positions and three indices a triangle
static PhysicsShape Mesh(MeshData mesh);                        // The same, from a mesh read back with TryReadMesh
static PhysicsShape Hull(ReadOnlySpan<Vec3> points);            // The smallest convex shape holding every point, as a rock or an odd crate tumbles
static PhysicsShape Hull(MeshData mesh);                        // The same, around the vertices of a mesh read back with TryReadMesh
```

### `Physics.Joint`

```csharp
static Joint Ball(Vec3 anchorA, Vec3 anchorB);                  // A point on one body held to a point on the other, free to turn any way about it: a shoulder, a pendulum's pivot, a chain's links
static Joint Hinge(Vec3 anchorA, Vec3 axisA, Vec3 anchorB, Vec3 axisB);  // The same, and turning only about one axis: a door, a wheel, an elbow
static Joint Weld();                                            // The two held exactly as they are to each other when joined, as though glued: a sword in a hand, a part bolted onto a vehicle
static Joint Distance(Vec3 anchorA, Vec3 anchorB, float minimum, float maximum);  // A point on each kept between minimum and maximum apart, a rope where the minimum is zero and a rod where the two are equal
Joint WithMotor(float degreesPerSecond, float torque);          // A hinge that turns itself, a fan or a wheel driven at a speed, pushing with no more than a torque
Joint WithLimits(float lowestDegrees, float highestDegrees);    // A hinge that stops at an angle each way, a door that opens to ninety degrees and no further
```

### `Physics.Colliders`

```csharp
static bool TryFit(EcsWorld world, Entity entity, out ColliderFit fit);  // What an entity's collider comes to, or false while the mesh it fits has not loaded
static bool Draw(EcsWorld world, Entity entity, (float R, float G, float B, float A) color);  // Draws an entity's collider as a gizmo, where it collides
```

## Input

The guide's page is [input.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/input.md).

### `Input`

```csharp
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
```

### `LogicalKey`

```csharp
static LogicalKey Character(string text);                       // The key that types text
static LogicalKey Named(string name);                           // The key Bevy names name, as Enter or ArrowLeft
static LogicalKey Dead(string text);                            // A dead key, which changes the character the next key types
```

### `Gamepad`

```csharp
bool Down(GamepadButton button);                                // Whether a button is held
bool Pressed(GamepadButton button);                             // Whether a button went down this frame
bool Released(GamepadButton button);                            // Whether a button came up this frame
float Axis(GamepadAxis axis);                                   // Where an axis stands, a stick from minus one to one and a trigger from zero to one
void Rumble(float strong, float weak, float seconds);           // Rumbles the pad, its strong motor and its weak one each from zero to one, for some seconds
void StopRumble();                                              // Stops every rumble the pad is playing
```

### `SyntheticInput`

```csharp
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
static void Key(ImGuiKey key, string typed = null);             // Presses and releases a key in the interface's own queue
static void Compose(string text, int caretStart = -1, int caretEnd = -1);  // Composes text as the platform's input method would, with the caret over caretStart to caretEnd, or hidden at -1
static void Commit(string text);                                // Commits text as the platform's input method would, arriving as an ImeCommit
static void Type(string text);                                  // Types a run of characters
static void Send(float x, float y, PointerAction action, MouseButton button = MouseButton.Left);  // Moves, presses or releases the pointer
static void Forget();                                           // Gives the pointer back to the hand on the desk
static bool TryTakeClickWithoutWindow(out float x, out float y);  // Takes the oldest left click given to a run with no window that nothing has answered yet
```

### `KeyTable`

```csharp
static bool IsMapped(Key key);                                  // True when key has a bit in the input bitsets
```

## The window

The guide's page is [window.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/window.md).

### `Window`

```csharp
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
static void Minimize();                                         // Minimizes the window to the taskbar or dock
static void SetMaximized(bool maximized);                       // Maximizes the window to fill the screen less the taskbar, or puts it back to the size it had
static WindowPlace Place();                                     // Where the window is, how large, and whether it is maximized
static void StartDragMove();                                    // Hands the window to the platform to be moved by the pointer, until the button held now is let go
static void StartDragResize(WindowEdge edge);                   // Hands the window to the platform to be resized by the pointer from one edge or corner, until the button held now is let go
static int MonitorCount();                                      // How many monitors the platform reports
static MonitorInfo Monitor(int index);                          // Describes one monitor, by an index below MonitorCount
static string MonitorName(int index);                           // The name the platform gives a monitor, by an index below MonitorCount
static VideoMode[] MonitorModes(int index);                     // Every video mode a monitor can be driven at, by an index below MonitorCount
static void SetVideoMode(int monitor, int mode);                // Takes the screen over in exclusive fullscreen at one of a monitor's own video modes
static void SetCursor(CursorGrab grab, bool visible);           // Sets whether the cursor is confined or hidden
```

## Math

### `Vec3`

```csharp
static float Dot(Vec3 a, Vec3 b);                               // The dot product
static Vec3 Cross(Vec3 a, Vec3 b);                              // The cross product, perpendicular to both operands
```

### `Vec2`

```csharp
static float Dot(Vec2 a, Vec2 b);                               // The dot product
static float PerpDot(Vec2 a, Vec2 b);                           // The dot product of a turned a quarter turn counterclockwise with b, positive where b lies counterclockwise of a
static Vec2 FromAngle(float radians);                           // The vector of length one at radians counterclockwise from X, Bevy's from_angle
float ToAngle();                                                // The angle from X to this vector in radians, between minus and plus a half turn, Bevy's to_angle
static Vec2 Min(Vec2 a, Vec2 b);                                // The smaller of each component
static Vec2 Max(Vec2 a, Vec2 b);                                // The larger of each component
static Vec2 Clamp(Vec2 v, Vec2 min, Vec2 max);                  // Each component held between its bounds
```

### `Rot2`

```csharp
static Rot2 Radians(float radians);                             // A rotation by an angle, in radians, counterclockwise
```

### `Isometry2d`

```csharp
static Isometry2d FromTransform(Transform transform);           // A placement where the shape is turned by its transform's rotation about Z and moved to its translation's X and Y
```

### `Aabb2d`

```csharp
static Aabb2d FromCenter(Vec2 center, Vec2 halfSize);           // The box about a center, reaching half its size each way
static Aabb2d FromPointCloud(Isometry2d isometry, ReadOnlySpan<Vec2> points);  // The box about points placed by an isometry
Vec2 ClosestPoint(Vec2 point);                                  // The point of the box nearest a point, the point itself where the box holds it
bool Intersects(Aabb2d other);                                  // Whether it overlaps another box, touching counting
bool Intersects(BoundingCircle circle);                         // Whether it overlaps a circle, touching counting
```

### `BoundingCircle`

```csharp
Vec2 ClosestPoint(Vec2 point);                                  // The point of the circle nearest a point, the point itself where the circle holds it
bool Intersects(BoundingCircle other);                          // Whether it overlaps another circle, touching counting
bool Intersects(Aabb2d box);                                    // Whether it overlaps a box, touching counting
```

### `Ray2d`

```csharp
static Ray2d Toward(Vec2 origin, Vec2 direction);               // A ray from a point toward a direction of any length but zero, which is made length one
Vec2 At(float distance);                                        // The point a distance along it
```

### `RayCast2d`

```csharp
float? AabbIntersectionAt(Aabb2d box);                          // How far along the ray it meets a box, or null
float? CircleIntersectionAt(BoundingCircle circle);             // How far along the ray it meets a circle, or null
```

### `AabbCast2d`

```csharp
float? AabbCollisionAt(Aabb2d other);                           // How far along the ray the swept box first touches another, or null
```

### `BoundingCircleCast`

```csharp
float? CircleCollisionAt(BoundingCircle other);                 // How far along the ray the swept circle first touches another, or null
```

### `IBounded2d`

```csharp
Aabb2d AabbAt(Isometry2d isometry);                             // The box about the shape where it is placed, Bevy's aabb_2d, which Rectangle, Circle, Triangle2d, Segment2d, Capsule2d, RegularPolygon, Arc2d, CircularSector and CircularSegment each give
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about the shape where it is placed, Bevy's bounding_circle, which each of them gives
```

### `Rectangle`

```csharp
static Rectangle FromSize(float width, float height);           // A rectangle of a width and a height
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `Circle`

```csharp
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `Triangle2d`

```csharp
(Vec2 Center, float Radius) Circumcircle();                     // The circle through its three corners, its center and its radius
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // The smallest circle about it where it is placed
```

### `Segment2d`

```csharp
static Segment2d FromDirectionAndLength(Vec2 direction, float length);  // A segment centered on the origin along a direction, a length long
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `Capsule2d`

```csharp
static Capsule2d FromLength(float radius, float length);        // A capsule of a radius whose straight middle is a length long
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `RegularPolygon`

```csharp
IEnumerable<Vec2> Vertices(float rotation);                     // Its corners, turned by an angle, the first up before turning
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `Arc2d`

```csharp
static Arc2d FromRadians(float radius, float angle);            // An arc spanning angle radians in all
static Arc2d FromDegrees(float radius, float angle);            // An arc spanning angle degrees in all
static Arc2d FromTurns(float radius, float fraction);           // An arc spanning fraction of a whole turn, half a turn a semicircle
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `CircularSector`

```csharp
static CircularSector FromRadians(float radius, float angle);   // A sector spanning angle radians in all
static CircularSector FromDegrees(float radius, float angle);   // A sector spanning angle degrees in all
static CircularSector FromTurns(float radius, float fraction);  // A sector spanning fraction of a whole turn, half a turn a half disc
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `CircularSegment`

```csharp
static CircularSegment FromRadians(float radius, float angle);  // A segment whose arc spanning angle radians in all
static CircularSegment FromDegrees(float radius, float angle);  // A segment whose arc spanning angle degrees in all
static CircularSegment FromTurns(float radius, float fraction); // A segment whose arc spanning fraction of a whole turn, half a turn a half disc
Aabb2d AabbAt(Isometry2d isometry);                             // The box about it where it is placed
BoundingCircle BoundingCircleAt(Isometry2d isometry);           // A circle about it where it is placed
```

### `CubicSegment<T>`

```csharp
static CubicSegment<T> Coefficients(T p0, T p1, T p2, T p3, float[,] matrix);  // The segment four control points make under a spline's characteristic matrix, row by row
T Position(float t);                                            // The point at t, from zero at its start to one at its end
T Velocity(float t);                                            // How fast the point moves at t, the first derivative
T Acceleration(float t);                                        // How fast that changes at t, the second derivative
```

### `CubicCurve<T>`

```csharp
T Position(float t);                                            // The point at t, from zero to its number of segments
T Velocity(float t);                                            // How fast the point moves at t
T Acceleration(float t);                                        // How fast that changes at t
IEnumerable<T> IterPositions(int subdivisions);                 // Points along the whole curve at even steps of t, subdivisions of them and one more for the end
```

### `CubicBezier<T>`

```csharp
CubicCurve<T>? ToCurve();                                       // The curve of Bézier segments, four control points each, or null where there is none
```

### `CubicHermite<T>`

```csharp
CubicCurve<T>? ToCurve();                                       // The curve through each point along its tangent, first to last, or null with fewer than two
CubicCurve<T>? ToCurveCyclic();                                 // The curve round through the last point and back to the first, or null with none
```

### `CubicCardinalSpline<T>`

```csharp
static CubicCardinalSpline<T> CatmullRom(IEnumerable<T> points);  // A Catmull-Rom spline, a cardinal spline of tension one half
CubicCurve<T>? ToCurve();                                       // The curve through each point, first to last, or null with fewer than two
CubicCurve<T>? ToCurveCyclic();                                 // The curve round through the last point and back to the first, or null with fewer than two
```

### `CubicBSpline<T>`

```csharp
CubicCurve<T>? ToCurve();                                       // The curve drawn toward the points, a segment for each four in a row, or null with fewer than four
CubicCurve<T>? ToCurveCyclic();                                 // The curve round its points and back, a segment starting at each, or null with none
```

### `Cuboid`

```csharp
static Cuboid FromLength(float length);                         // A cube whose sides are each length long
static Cuboid FromSize(Vec3 size);                              // A box of the size given along each axis
Vec3 SampleInterior(Random random);                             // A point inside it, each as likely as any other
Vec3 SampleBoundary(Random random);                             // A point on its surface, each as likely as any other
```

### `Sphere`

```csharp
Vec3 SampleInterior(Random random);                             // A point inside it, each as likely as any other
Vec3 SampleBoundary(Random random);                             // A point on its surface, each as likely as any other
```

### `CompassOctants`

```csharp
static CompassOctant? Of(Vec2 direction);                       // The octant a direction points in, north being positive Y as on a stick, or null for no direction at all
```

### `Quat`

```csharp
static Quat FromAxisAngle(Vec3 axis, float radians);            // A rotation of radians about an arbitrary axis
static Quat FromRotationX(float radians);                       // A rotation about the X axis
static Quat FromRotationY(float radians);                       // A rotation about the Y axis
static Quat FromRotationZ(float radians);                       // A rotation about the Z axis
static Quat FromBasis(Vec3 x, Vec3 y, Vec3 z);                  // The rotation whose local axes are x, y and z
static Quat FromEuler(float x, float y, float z);               // The rotation that rolls about Z, then pitches about X, then turns about Y, in radians
Vec3 ToEuler();                                                 // The turns about X, Y and Z this rotation is made of, in radians
static Quat Slerp(Quat from, Quat to, float t);                 // The rotation a fraction of the way between two, turning at an even rate the shorter way round, as Bevy's Quat::slerp does
static Quat Lerp(Quat from, Quat to, float t);                  // A straight blend of the two rotations, taken the shorter way round and made a rotation again, as Bevy's Quat::lerp does
```

### `Color`

```csharp
static Color FromSrgb(float r, float g, float b, float a = 1f);  // A color from sRGB components between zero and one, as a color picker gives them
static Color FromHex(string hex);                               // A color from an sRGB hex string such as #ff8800 or ff8800cc
static Color FromSrgb8(byte r, byte g, byte b, byte a = 255);   // A color from sRGB bytes, as Bevy's Color::srgb_u8 and a hex code give them
static Color FromHsl(float hue, float saturation, float lightness, float alpha = 1f);  // A color from a hue in degrees, a saturation and a lightness between zero and one, as Bevy's Color::hsl gives it
Vec4 ToSrgb();                                                  // The color as sRGB components between zero and one, alpha unchanged
Color WithAlpha(float alpha);                                   // The color with another alpha
```

### `Transform`

```csharp
static Transform At(float x, float y, float z);                 // A transform at x, y, z
static Transform LookingAt(Vec3 eye, Vec3 target, Vec3 up);     // A transform at eye oriented so that its forward axis points at target
```

### `GlobalTransform`

```csharp
Transform ToTransform();                                        // The same transform expressed as a position, rotation and scale
Vec3 TransformPoint(Vec3 point);                                // Maps a point in the entity's local space into world space
Vec3 TransformDirection(Vec3 direction);                        // Maps a direction in the entity's local space into world space
```

### `EaseFunction`

```csharp
static EaseFunction Steps(int count, JumpAt jump);              // A staircase of count steps rising where jump says, as Bevy's EaseFunction::Steps
static EaseFunction Elastic(float omega);                       // A spring of angular frequency omega settling at one, as Bevy's EaseFunction::Elastic
float Sample(float t);                                          // The value at t, from zero at its start to one at its end
```

## The tools

The guide's page is [tools.md](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/tools.md).

### `ConsoleCommands`

```csharp
static void Add(ConsoleCommand command);                        // Adds a command, replacing one of the same name
static void Add(string name, string help, Func<string[], string> run);  // Adds a command written out in place
static bool Remove(string name);                                // Takes one back out
static ConsoleCommand Find(string name);                        // The command of a given name, or null
static IReadOnlyList<ConsoleCommand> Starting(string prefix);   // The commands whose names start with what has been typed so far
static string Run(string line);                                 // Runs a line as it was typed, and answers with what to write back
static string Unwrap(string line);                              // A line with one pair of enclosing quotes taken off, if it has them
static string[] Split(string line);                             // A line of arguments as words, keeping quoted runs together
```

### `ConsoleHost`

```csharp
static void Fail(string code, string message);                  // Says the command failed, with a code a script can branch on
static void Hold(ulong frame);                                  // Holds this command's answer until the frame counter reaches frame
static void Later(Func<string> poll);                           // Says this command's answer is not ready yet, and how to ask for it
static ConsoleHost.Scope Lend(World world);                     // Lends the world to commands run inside the returned scope
```

### `ConsoleHost.Scope`

```csharp
void Dispose();                                                 // Ends it, putting back whatever was lent before
```

### `Log`

```csharp
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
```

### `ConsoleLog`

```csharp
static void Start();                                            // Starts teeing the output and error streams into the ring
static void Write(LogLevel level, string text);                 // Adds a line
static void Write(string text);                                 // Adds a line at the ordinary level
static LogLine[] All();                                         // The lines kept, oldest first
static void Clear();                                            // Forgets everything
```

### `CliClient`

```csharp
static string Send(CliSession session, string operation, string line = null, double seconds = 30);  // Sends one request and reads the one answer
static CliAnswer Run(CliSession session, string line, double seconds = 5);  // Runs a console command in a running app and reads what it answered
```

### `CliJson`

```csharp
static string Envelope(string command, bool success, Action<Utf8JsonWriter> data = null, IReadOnlyList<CliError> errors = null, IReadOnlyList<string> warnings = null, string id = null);  // Writes an envelope, with whatever data writes as its payload
static string Ok(string command, Action<Utf8JsonWriter> data = null, string id = null);  // An envelope that worked
static string Fail(string command, string code, string message, string id = null);  // An envelope that did not
```

### `CliPlugin`

```csharp
void Build(App app);                                            // Registers this plugin's contributions on app
```

### `CliSessionFile`

```csharp
static string PathFor(int pid);                                 // Where the file for a given process is
static void Write(CliSession session);                          // Writes one, replacing whatever was there
static CliSession Read(string path);                            // Reads one, or nothing when the file is missing or not one of these
static IReadOnlyList<CliSession> All();                         // Every session written down, newest first, whether or not it is still alive
static void Remove(int pid);                                    // Takes one back out, on the way down
static int Prune();                                             // Deletes the files of sessions whose processes are no longer there
```

## Everything else

### `GameTimer`

```csharp
static GameTimer FromSeconds(float seconds, TimerMode mode);    // A timer of seconds that runs once or over and over, as Bevy's Timer::from_seconds
GameTimer Tick(float delta);                                    // Runs it on by delta seconds, and answers it as it is after, so timer.Tick(delta).JustFinished reads as Bevy's timer.tick(delta).just_finished()
void Pause();                                                   // Stops it where it is until Unpause
void Unpause();                                                 // Lets it run again from where it was paused
void Reset();                                                   // Starts it again from nothing, unfinished, its duration and mode kept
```

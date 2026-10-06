# States

A game's modes, its menu, its play and its pause, as Bevy's states, which systems are scoped to and
which run code as they are entered, left and moved between.

A game is usually in one of a few modes, and most systems belong to one of them. `AddState` sets
one up over any enum, and `[InState]` scopes a method to a value of it:

```csharp
public enum Screen { Menu, Playing, Paused }

app.AddState(Screen.Menu);

[OnUpdate]
[InState(Screen.Playing)]
public void Tick(BehaviorContext ctx) { }
```

Read and change it from a system:

```csharp
var screen = ctx.State<Screen>();
ctx.SetState(Screen.Paused);
```

`[InState]` runs a method every frame the state is held. In an app that never added the state it
never runs, and the app says so once on the error stream for the state, naming the first method's
value, however many methods are scoped to it, since they share the one missing `AddState`. To run
one *as* the state changes, on the edge rather than throughout, use `[OnEnter]` and `[OnExit]`:

```csharp
[OnEnter(Screen.Playing)]
public static void BuildLevel(BehaviorContext ctx) { }

[OnExit(Screen.Playing)]
public static void TearDown(BehaviorContext ctx) { }
```

That is where a screen is built and taken away: once per transition, not once per frame. A
transition attribute replaces the stage attribute rather than joining it, because the two say
different things about when a method runs, and asking for both is reported as an error.

What depends on where the state came from as well as where it went is `[OnTransition]`, which runs
on a move from one value to a particular other and on no other move. Play resumed from the pause
keeps the level it had, and play entered from the menu builds one:

```csharp
[OnTransition(Screen.Menu, Screen.Playing)]
public static void BuildLevel(BehaviorContext ctx) { }
```

It runs after the exit of the value left and before the entry of the value entered, as Bevy orders
them. Both values are of the same enum, and naming two is an error when the code is compiled.

A teardown method that lists everything the screen spawned goes stale the first time something
new is added to the screen. Tie the entity to the state instead and leaving takes it with you:

```csharp
[OnEnter(Screen.Playing)]
public static void BuildLevel(BehaviorContext ctx)
{
    var enemy = ctx.Ecs.Spawn();
    ctx.Ecs.DespawnOnExit(enemy, Screen.Playing);
}
```

The despawn is Bevy's own, so it reaches the entity's children as well, and it happens at the
transition rather than inside `[OnExit]`, which means it covers every way out of the value.

A mode that only means anything inside another one is a sub-state. A pause outside a run is not
"off", it is nothing, and saying so keeps a pause from being held when the next run starts:

```csharp
public enum Screen { Menu, Playing }

[SubStateOf(typeof(Screen), Screen.Playing)]
public enum Paused { No, Yes }

app.AddState(Screen.Menu);
app.AddSubState(Paused.No);      // after its parent, which it is computed from
```

While `Screen` is anything but `Playing` the state does not exist, so
`App.TryState<Paused>(out var held)` answers false rather than a value, and a method scoped to
`[InState(Paused.Yes)]` does not run. Entering `Playing` brings it into existence at `Paused.No`
every time, which is why a pause left on when a run ended is off again when the next one begins.
`[OnEnter]`, `[OnExit]` and `DespawnOnExit` work on it exactly as they do on a plain state, because
the relationship is written on the enum rather than at the call.

A state carries two sub-states, and a sub-state cannot itself be a parent. Both limits come from the
same place. Bevy names a sub-state's parent as an associated type, so every pairing exists when the
native library is built, and a third sub-state or a chain of them is refused rather than
half-worked. A run that can be paused and played at a difficulty needs two, and raising it is a
longer list in the same place as the state slots below.

A mode that means something only inside two others names both, and exists only while each holds
its value:

```csharp
[SubStateOf(typeof(Screen), Screen.Playing)]
[SubStateOf(typeof(Mode), Mode.Online)]
public enum Lobby { Browsing, Ready }

app.AddState(Screen.Menu);
app.AddState(Mode.Offline);
app.AddSubState(Lobby.Browsing);   // after both
```

It is set like any sub-state while it exists, keeps its value through a change to a state it does
not name, and starts over each time it comes back. These are kept apart from the sub-states of one
state, each fed every state slot, so which states one lives inside is the game's choice.

A state whose value follows from another's is a computed state. Whether the interface is up is
true on some screens and false on the rest, and writing that as a plain state leaves two facts to
keep in step until one of them lies:

```csharp
[ComputedFrom(typeof(Screen))]
public enum Hud { Shown, Dimmed }

app.AddState(Screen.Menu);
app.AddComputedState((Screen.Playing, Hud.Shown), (Screen.Paused, Hud.Dimmed));
```

The table says what it is while the source holds each value, and a value the table says nothing
about means it does not exist at all, so `TryState<Hud>` answers false there and a method scoped to
`[InState(Hud.Shown)]` does not run. Setting one is refused, since there is nothing to set. Its
`[OnEnter]` and `[OnExit]` edges run like any other state's, so it is useful rather than merely
tidy.

Where a table cannot say it, a rule can, as a function of the source's value answering what the
state is or nothing:

```csharp
app.AddComputedState<Music, Level>(level => level > Level.Ten ? Music.Boss : Music.Calm);
```

Bevy asks it from inside a transition with the source's value alone, so it reads no world, and one
that throws is taken as answering nothing.

A fact that follows from two facts together names both, and its rule takes both values:

```csharp
[ComputedFrom(typeof(Level), typeof(Pause))]
public enum Music { Calm, Boss, Quiet }

app.AddComputedState<Music, Level, Pause>((level, pause) =>
    pause == Pause.On ? Music.Quiet : level == Level.Last ? Music.Boss : Music.Calm);
```

Three sources take the same form with a third type. Bevy works it out again whenever any source
changes, and a value worked out again to what it already was runs no `[OnEnter]`. While any source
holds no state, it does not exist. The bridge keeps a fixed set of these joint states beside the
slots, each fed every slot, so which states one reads is chosen by the game rather than when the
bridge is built.

A state can be declared on its enum instead of added by a call, which is how a behavior script
says what states it has, since a script has no `Program.cs` to call `AddState` from:

```csharp
[InitialState(Menu)]
public enum Mode { Menu, Playing, Won }

[SubStateOf(typeof(Mode), Mode.Playing)]
[InitialState(Off)]
public enum Pause { Off, On }
```

A generator finds the attribute at compile time, and an app adds each declared state one of its
systems names as it starts to run, a parent before the sub-states inside it, so a game whose
scripts are compiled into it and the editor's player running the same scripts get the same states.
A state is known by its enum's full name, so a script compiled again while the game runs, whose
enum is a new type of the same name, reads and changes the state the game started with. One
declared by an assembly loaded once the app was running, as the editor loads a project's scripts,
is not added, and the app says so once for it. The editor turns that off
(`StateRegistry.ReportUnentered`) and says once which of the game's states it does not enter while
a level is edited.

A transition is queued rather than immediate. It lands at Bevy's next transition point, so every
system in the frame agrees on which state it is in rather than some seeing the change halfway
through.

A Bevy state is a Rust type and C# cannot define one, so the bridge provides eight state slots
that hold an integer, and each enum claims one the first time it is added. A slot is one
independent state machine rather than one value, because the integer it holds gives an enum as
many members as it likes, and eight is the number of *unrelated* machines a game can run at once, which
is past what most need. Running out reports it, and raising the count is a list in
`native/bevy_csharp/src/states.rs` and a rebuild, at about four seconds of build time per slot. `[InState]` is a run condition, so it composes
with `[RunIf]` and `[ToggleKey]` rather than replacing them, and a method carrying more than one
runs only when all of them pass.

---

Before this, [Behaviors](behaviors.md).
Next, [Messages and the hierarchy](messages-and-hierarchy.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#state). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#states). The [guide's contents](../README.md#guide) list every page.

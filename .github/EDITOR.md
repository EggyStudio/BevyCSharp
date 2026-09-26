# The editor

What `BevyCSharp.Editor` is, how it is meant to look, and what the engine has to grow for it to
work. The page it is built on is described in the README; this is about what goes on top of it.

## What it is for

An editor made of panels that a person can rearrange, replace or write their own version of. The
shipped set is a starting point rather than the product. The world, the details, the console, the
assets, the settings and the style are six panels using one mechanism six ways, and a menu, an enum
dropdown and a right click are a seventh use of the same thing.

That is the reason the framework came first. A fixed arrangement of menubar, hierarchy, viewport
and inspector is one arrangement, and the moment it is baked into the shell it stops being a
choice.

Everything the editor offers is registered in a table rather than drawn into a panel, and each
table is a line a game writes. The menu, the toolbar, the tabs, the settings, the ways of telling
what an entity is, and the commands the console takes are all lists something adds to.

## The shape of it

Bevy's own words are used where there is one. The left column is the **world**, because Bevy calls
the thing being listed that, and the right column answers what is **selected**, whether that is an
entity, an asset or a setting. Nothing is called a hierarchy or an inspector.

`EditorShell` owns the arrangement and nothing else. Three numbers, the rectangles that follow from
them, and a call to each part that draws one. The parts are:

| class | what it draws |
|---|---|
| `EditorPanes` | the panel holding the world beside or above the details, and the handles that move it |
| `EditorStrip` | the tabs along the bottom left, and whichever is open above them |
| `ToolbarView` | the groups of buttons that float in the scene's corners, and the list of keys |
| `EditorFlyout` | the menu, and every submenu of it |
| `EditorSceneFrame` | the corner taken off the scene, and the button that docks the panel |
| `EditorPicking` | what a click on the scene selects, and what an unanswered one clears |

What they draw with is five more, which nothing else in the editor duplicates:

| class | what it is |
|---|---|
| `EditorSurface` | the gaps and the plates: gutters, cards, regions, grab handles, what a thing lying on the scene wears |
| `EditorWidgets` | the controls ImGui does not draw the way this look needs: a round box to tick, a slider, a pill, a list to choose from, a flyout, a tooltip |
| `EditorDraw` | the shapes drawn by hand: an exactly rounded rectangle, a capsule, an icon, an eye |
| `EditorSort` | the order names are listed in, which reads a run of digits as the number it spells |
| `EditorText` | names as a panel shows them: a component without its path, a file cut to the room it has |
| `EditorRows` | a list of named values, as a name in one column and what it holds in the other |
| `RoundedRows` | rounded highlights behind the rows ImGui draws square |

And the panels themselves are `WorldPanel`, `DetailsPanel` with `ComponentFields`, `FieldNumbers`
and `FieldPickers`, `ConsoleTab` over `ConsoleView`, `AssetsTab` over `EditorAssets`, `SettingsTab`
over `EditorSettings`, and `StyleTab` over `EditorTheme`.

A part reads the shell's numbers and draws; nothing reads back. Adding a panel is a class with a
draw method and one line in `EditorBoot`, and a tab is a name and that method.

**Docking changes what is behind the panels, not where they are.** The panel and the strip reach
the window's own edges in both states and hold their cards in by their padding, so nothing on
screen moves when it is switched. What changes is the scene. Floating, it fills the window and the
cards lie over it; docked, it is given the rectangle they leave and the ground shows in the gaps.

**There is a way out of a text field.** A shortcut asks `ImGuiRuntime.Typing`, which is true only
while a box with a caret in it has the keyboard. Asking whether the interface takes the keyboard at
all is a different question with a different answer. With keyboard navigation on, it takes it
whenever any window is focused, which in an editor whose panels are always up is always.

## Design language

Unity's editor is the reference, deliberately and not as a copy. It is the interface most people
coming to this already have in their hands, its density is right for a tool looked at all day
beside the thing being edited, and Unity publishes the values rather than leaving them to be
guessed at. What follows is taken from Unity's Editor Foundations design system, then adjusted
where this editor differs.

### Color

Everything in the editor is drawn on one of a ladder of grays, and which rung says what a thing is.
This is the whole of the scheme, and the values live in `EditorTheme` and nowhere else. Anything
drawn by hand reads the rung out of the running style rather than out of the record, so a color
changed in the style tab changes what is drawn instead of being written over on the next frame.

| rung | value | what it is |
|---|---|---|
| `Ground` | `#000000` | the window behind everything, and the frame round a docked scene |
| `Panel` | `#0C0C0C` | the window a panel is drawn in, seen through at `WindowAlpha` |
| `Card` | `#1A1A1A` | a card inside one, and the plate on anything lying on the scene |
| `Group` | `#262626` | a component's card inside a card |
| `Field` | `#3E3E3E` | a box that is typed in, and the groove of a bar |
| `Hover` | `#525252` | a control under the pointer, and the plate of a menu or a tooltip |
| `Active` | `#666666` | one being held down |
| `Line` | `#3C3C40` | the rare rule, where a gap will not do |
| `Text` | `#F2F2F2` | what is being read |
| `Dim` | `#9A9A9A` | a label, a unit, a shortcut beside a menu row |
| `Faint` | `#666666` | what is switched off |
| `Accent` | `#2B6CF6` | selected, or in force, and nothing else |
| `Warn` | `#E0B04C` | something that will probably go wrong |
| `Bad` | `#E06C63` | something that already has |

Three rules follow from the ladder, and they matter more than the values:

- **Separation is a step on the ladder first and a line second.** A component's card is a step
  above the card holding it, a field a step above that. A border round each would be a third thing
  saying what the step and the gap already say.
- **What floats is lighter than what it covers.** A menu and a tooltip are held up in front of the
  work rather than lying under it, so they wear the brightest plate the ladder has, which is the
  same gray a row wears under the pointer. So a flyout reads as one of those rows grown large enough
  to hold a list.
- **A row lifts off whatever it is lying on.** A fixed gray that lifts off a card disappears into a
  menu's plate, so a row under the pointer is drawn as a wash of the text color instead, which is
  a step above anything. Over a card it comes out at `Hover`.

The accent means one thing only, what is selected or what is in force. A color that also draws every
component header is a color that means nothing. `Warn` and `Bad` are the exception, and they are for
what the program has to say rather than for what it is.

**Transparency stops at the panel.** The window a panel is drawn in is the layer against the scene
and is the most transparent thing there is, at `WindowAlpha`, which the modern look leaves at
nothing at all. The cards in it are `PanelAlpha` solid, and everything from a component's card
inwards is solid outright. A box somebody is about to type a number into whose tone drifts with
whatever passes behind it is a box with no reliable contrast.

### Density

Unity's numbers are the reference. What this editor settled on, and where each lives:

| number | value | where |
|---|---|---|
| text | 15px, one face for words and one for figures | `EditorShell.Lettering` |
| a row | the text plus `FramePadding` of `6, 3`, which is 21px | `EditorTheme` |
| the gap between any two surfaces | 14px | `EditorSurface.Gutter` |
| the air a card keeps inside its own edge | 6px | `EditorSurface.Air` |
| anything lying on the scene | 28px tall | `EditorSurface.Tall` |
| a word on something pressable | 10px of air at each end of it | `EditorSurface.Sides` |
| a menu or a tooltip's own air | `10, 8` | `EditorSurface.Around` |
| the narrowest the panel goes | 320px | `EditorShell.Narrowest` |
| where it stops being two columns | 460px | `EditorShell.Stacks` |

**One gap, everywhere.** A panel's edge to its cards, one card to the next, the scene to whatever
is beside it, and a card to the window's edge are all the same number. Two gaps of different widths
in one picture read as an arrangement that has slipped. A grab handle lies in one of those gaps, a
third of it thick, which is the thickness ImGui gives a scrollbar's grab.

- **Text is left aligned**, except button labels and the number in a box, which are centered, and
  the number on a bar, which is centered until the handle comes close enough to touch it.
- **Indentation carries nesting** in hierarchies, inspectors and menus.
- The inspector must not scroll horizontally at its **300px width**, which the name column and a
  value beside it need without either being cut.
- **The name is a column, not a label.** Every value in a panel starts at the same place however
  long the names are, so a column of values can be read down its own edge. Each row is a two
  column table, weighted toward the value, because a name that runs out of room is still readable
  from its first half and a number that runs out of room is a different number.
- **A panel fills its column.** Its height never changes, so what it holds scrolls inside a frame
  that stays put. A panel that grows and shrinks as its contents change is the single thing that
  makes an interface feel unsteady, and an inspector's contents change constantly. Two panels in
  one column share it.
- **A panel clips and scrolls.** What does not fit is hidden rather than drawn over whatever is
  below, and what is cut off is cut along a rounded edge, because a card scrolled under a square
  clip is the one square corner in a look made of round ones. What is a list scrolls; what is a way
  in stays. The tags under an inspector are the end of the list and scroll with it, and the button
  that adds a component keeps its row at the bottom, because adding one is not something to go
  looking for past everything already there.
- **The bottom left is the tabs.** A tab opens into a card above the strip, and docked the scene is
  given what is left rather than being covered, so nothing is hidden behind what was opened. The
  card can be dragged taller by the handle in the gap above it, and the height it is left at is the
  height the next one opens at.

### Windows

Unity separates window kinds by how they dismiss, which is the distinction worth copying:

| kind | dismisses | draggable |
|---|---|---|
| default | never, docked or floating | yes, by its title bar |
| dropdown | on any click outside | no |
| popup | only when told | no |
| auxiliary | on click outside, one instance | yes |
| modal | blocks everything behind | no |
| utility | stays on top, does not block | yes |

An editor needs at least the first three. A hierarchy is a default window; a component picker is a
dropdown; a detached graph is a popup.

Unity's **overlays** are the closer model for the floating look this editor has: containers that
float over the viewport, dock to its edges, collapse to an icon, and are toggled from one menu
bound to a single key. Their layout modes are panel, collapsed, horizontal and vertical, and an
overlay with no horizontal or vertical layout collapses when docked to an edge.

## What the engine has to grow

The interface draws panels, edits fields and runs commands on its own. What it cannot do without
the bridge is find out what is in the world, which is most of what an editor shows. Every row below
is bridged except the last.

| need | how | blocks | entry point |
|---|---|---|---|
| every live entity | `World::iter_entities` | hierarchy | `bcs_ecs_entities` |
| an entity's components | `World::inspect_entity` | inspector | `bcs_ecs_components_of` |
| a component's name | `ComponentInfo::name` | every label in the inspector | `bcs_component_name` |
| an entity's name | Bevy's `Name`, which holds a `String` | hierarchy labels | `bcs_ecs_entity_name` |
| naming an entity | the same, written | a list nobody can work in | `bcs_ecs_set_entity_name` |
| a field's name and type | the source generator, not the bridge | editing a value | none needed |
| where a panel ended up | ImGui's own layout | the viewport, the orientation cross | none needed |
| the entity under the cursor | `bevy_picking`, already compiled in | selection in the viewport | `bcs_pick_events` |
| what an entity fills | `Aabb` through the global transform | outlining and framing a selection | `bcs_render_bounds` |
| pausing | `Time<Virtual>` | play and pause | none yet |

The field table is the one that decides whether an inspector is possible at all, and it needed no
engine work. A `[Behavior]` struct is a C# type the generator already reads, so it emits the field
names, types and a pair of accessors beside the runner it emits today. That turns a component id,
which the bridge already hands over, into a list of editable fields, with no reflection crossing
the ABI.

Bevy's own components stay a curated list, because a general answer needs a byte-compatible mirror
on this side and that is written by hand per type.

## The interface

The editor's interface is **Dear ImGui**, running in C# and drawn by Bevy. The managed side owns the
context and builds the windows; the engine gets triangles. `BevyCSharp/Ui/ImGuiRuntime.cs` is the
join, `native/bevy_csharp/src/imgui` the pass.

What that buys:

- **A panel is a function.** It runs every frame and draws what is true then. There is no widget to
  build, no state to invalidate, and nothing that can be out of step with the world it is showing.
- **The arrangement is a calculation.** Whether the panel is docked, how wide it is and which tab is
  open are three numbers; every rectangle follows from them, so a layout cannot get into a state
  nothing put it in.
- **The scene is a viewport, not a hole.** Floating, the camera fills the window and the panels are
  over it. Docked, `bcs_render_set_viewport` gives the camera what is left, so the picture is the
  shape of the space rather than the shape of the window with something on top.
- **The engine only rasterizes.** The Rust side draws clipped, textured triangles in screen space
  and knows nothing else. The entire interface can be rewritten without touching it.
- **Immediate mode costs a redraw a frame.** A few thousand triangles and one buffer write, which is
  the trade ImGui makes and the reason it is the tool for a panel full of numbers that change.
- **The look is data.** `EditorTheme` is a record of every color and metric, written to
  `assets/theme.txt` and read back at startup, and the style tab edits that record rather than
  ImGui's style, so every knob it shows is one that survives being saved.

### What is drawn by hand, and why

ImGui rounds a rectangle by at most half its shortest side less a pixel, and several of its widgets
clamp further still. A checkbox is held to a quarter of its box so that it keeps reading as a
checkbox. That leaves a flat edge of a pixel or two on everything meant to end in a half circle,
which is invisible on a wide field and plain on a handle, a pill or a tick.

So the shapes that have to be round are drawn rather than asked for. `EditorDraw.Rounded` lays out
the four arcs itself and keeps the radius it is given; `Capsule` is that with the radius at its
limit. `EditorWidgets` draws the fill of a box to tick, the groove and handle of a slider, and the
pill under a word, and leaves ImGui to draw the tick over them, so every click, drag and control
click is still ImGui's. Under the stock look all of that steps aside, because that look is ImGui's
decisions taken whole.

A bar's own value is the one thing drawn over ImGui rather than under it. ImGui writes it across
the middle, which is where the handle passes, so `Sliding` hides that and writes it itself, pushed
aside by as little as it takes to clear the handle and never past either end of the bar. The format
still goes to ImGui, because the format is also what fills the box when somebody control clicks to
type a value in.

The one thing this cannot reach is a widget drawn inside a call that draws many, which is why the
style tab lists the theme's own fields instead of using `ImGui.ShowStyleEditor`.

### Why there are no borders

A bordered box inside a bordered box inside a bordered panel is three lines saying what one gap
says better. What this uses instead is the ladder above, where the panel is a step above the
ground, a card a step above the panel, a component's card a step above that, and what is under the
pointer a step above whatever it lies on.

## The inspector

**Accessors, not offsets.** An offset would need a size, a signedness and a layout to be read
through, and all of those are already known to the compiler that emitted the table, so each field
carries a closure that reads and writes the real struct instead.

`DetailsPanel` draws the cards, `ComponentFields` draws the rows in them and `FieldNumbers` decides
how a number in one is written and dragged. What a field is drawn as follows from its kind and from
the hints its attributes declared: a round box to tick for a flag, a box for a number, a bar for a
number with two ends, three boxes across for a vector, a list to choose from for a choice, a swatch
for three numbers that are a color, and a dimmed box for anything that cannot be edited. A
heading, a rule, a sentence and a unit are the field's own declaration too.

**Every edit is undoable, and a run of them is one edit.** A row compares what the field held
before its widget was drawn with what it holds after, and records the difference under the field's
own name, so a drag along a bar or a number typed one character at a time comes back as one
change. The same goes for what the panels do outside a field: a rename, a drag on the handles, an
entity taken out of its parent, something hidden with its eye, and a component put on or taken off.
Taking one off keeps what it held, so putting it back is the component again rather than a fresh
empty one in its place.

There is no drawer table. A field kind is one arm of one switch, so a kind drawn in a different
shape is a branch in one method.

A component's **properties are described as well as its fields**, and read and written through
themselves. Something worked out from two fields, something clamped on the way in, or something kept
in one unit and shown in another is the property's own business, and a tool that went round it would
show a number nothing else in the program agrees with. A property with no setter is a row that can
be read and not changed.

A property says so, through `ComponentField.Derived`. Drawing a row does not care, but anything
that writes a whole component back has to write the state and not the views of it, or a setter that
changes what it was derived from runs over the value just restored.

**A field says how it is drawn, in attributes.** The generator reads them at compile time and leaves
the answers on the schema as `FieldHints`, so nothing reflects at runtime:

| attribute | what it does |
|---|---|
| `[Label("...")]` | what the row is called, when the field's name is not the right words |
| `[Tooltip("...")]` | a sentence shown beside the row while the pointer is over it |
| `[Unit("m")]` | what the number is measured in, in a column after the value |
| `[Step(0.1)]` | what one pixel of a drag on the handle is worth |
| `[Range(min, max)]` | draws a bar with the number in it; `Readout` asks for a box beside it or for nothing |
| `[ReadOnly]` | drawn dim and not written back |
| `[Hidden]` | not drawn at all, on a field or on a method |
| `[Header("...")]` | a word above the field, grouping what follows |
| `[Space]` | a blank row above the field |
| `[Separator]` | a line across the panel above the field |
| `[Info("...", Kind = ...)]` | a sentence in the panel above the field: something to know, a warning, an error |
| `[Foldout("A/B")]` | puts the field in a fold, which opens and shuts; slashes nest them. **Not drawn yet** |
| `[Color]` | three numbers that are a color, as a swatch the width of the row that opens a picker |
| `[Inline]` | three numbers beside each other rather than one per row, as a vector already is |
| `[Wide]` | drawn across the panel, with no name column beside it |
| `[ShowIf(nameof(Other))]` | drawn only while another field reads true, or equals a value |
| `[HideIf(nameof(Other), Value)]` | the same, reversed; several conditions may sit on one field |
| `[OnValueChanged(nameof(M))]` | calls the named methods once the field has been changed |
| `[Order(n)]` | where the field or button sits among its neighbors |
| `[Button("...")]` | what a method's button says; `Line` and `Weight` share a row between buttons |
| `[Asset(AssetKind.Mesh)]` | which files to offer for a field that holds an asset |

**A struct inside a component is taken apart.** A field whose type is a struct with fields of its
own is described as those fields, named by the path they came from and folded under the name of the
field they came from. `Front.Held.At` is a row called `At`, inside a fold called `Held`, inside one
called `Front`. Writing one reads the whole component, changes the part and writes it back, so a
part written does not wipe its neighbors. A vector is left alone, because it is three numbers a
drawer already draws as one thing.

**The console reads and writes.** The tab along the bottom is where a log is read, and the key under
Escape raises or puts away that tab. `ConsoleTab` draws it and `ConsoleView` decides what it shows,
which is a search, a switch per level, and what was typed before, reached with the arrows.
Everything either of them shows lives outside both. `ConsoleLog` is a ring of leveled lines that the
output and error streams are teed into, and `ConsoleCommands` is the list of what can be typed.

A command is a static method with `[Command]` on it, found at compile time by a generator and
registered by a module initializer, so nothing scans for them and one that does not compile is not
a command:

```csharp
[Command("select", "Selects the first entity with a name: select <name>")]
internal static string Select(string name) { … }
```

Its parameters are read from the words that follow the name and may be strings, numbers or flags;
one string parameter takes everything typed after the name, and a name with a space in it is one
word when it is quoted. Returning a string writes that line back. Anything a person can get wrong
is answered with a sentence rather than an exception, since a console is where people type things
that are not quite right.

**The menu is reachable from the console.** `do` runs any row of the menu by its path and, with
nothing after it, lists the paths there are. One command rather than one per row, so the console
stays a short list and the menu stays the one place the editor's commands are written down.

**More than one thing can be selected.** Control and a click in the world list adds to the
selection or takes something out of it, shift takes everything between. A drag on the handles
moves, turns or stretches all of them, each about its own origin or all about the middle, and
undoes as one change. What the details panel shows is the last one picked, with a count beside its
heading saying how many others there are.

What an entity is drawn with is saved as well, by the path its mesh and material were loaded from,
which is the only thing about a typed handle this side can name. A mesh built in memory is a set of
numbers with no name, so it is left out rather than written as something it is not.

**The editor tells one entity from another by name.** Spawning something gives it a name nothing
else is called, because the world file matches a saved entity back up by name and a selection that
survives a script reload is found again by it, so a second thing called Cube is a thing the editor
confuses with the first. Wherever names are listed they are ordered by `EditorSort`, which reads a
run of digits as the number it spells, so Cube 2 comes before Cube 10 rather than after it.

A field holding an asset shows the file it points at rather than the number a handle is, and
pressing it offers the files under the asset root that suit it. Which those are is the field's own
business, because a handle to a mesh and a handle to a sound are the same type and nothing else can
tell them apart.

**A number is dragged on the box itself**, at the field's own step or a hundredth, and control
clicking or double clicking one opens it for typing. What is shown while it is not being typed into
is as few decimal places as say the value, up to three; what it opens holding is the number whole,
because a box that opened at three places would offer 1.235 to somebody who came to correct 1.2345.
Nothing rounds the value itself.

### What the inspector does not do

Written down because the attributes and the shape of the thing suggest otherwise:

- **`[Foldout]` is carried but not drawn.** A field that asks for a fold is drawn in place.
- **A selection of several things shows the last one**, not what they agree and disagree about.
- **There is no pass system and no list drawer.** What an entity hangs from and what hangs from it
  are not shown as rows, and a component cannot hold a list to begin with, so nothing in the
  inspector draws one.
- **Nothing puts a field back to its default.** What a freshly added component holds is known to
  the schema, and no row asks it.
- **A duplicate is not offered**, because the editor can read only what has a schema off an entity,
  and the mesh and the material an entity is drawn with have none.

## Adding to it

None of this needs a panel of its own.

```csharp
// A row on the menu, which the hamburger, the right click on the world and the console's "do" all
// reach at once. The keys it names are written on the row; whether it can be run is asked.
EditorMenu.Command(
    "Spawn/Enemy",
    world => Spawn(world, "Enemy"),
    40,
    EditorIcons.Entity,
    "Ctrl+E",
    () => EditorSelection.Any);

// A button in the middle of the toolbar. A picture alone is a circle, a word alone a pill, and a
// button that is only a picture says what it is when the pointer rests on it.
EditorToolbar.Add(new ToolbarButton(
    ToolbarSlot.Center,
    "icons/ui/zap.png",
    () => string.Empty,
    world => Bake(world),
    () => Baking,
    40,
    "Bake the lighting"));

// A tab along the bottom, which is a name and what to draw. The menu offers a row for it because
// it is in this list.
EditorShell.Tabs.Add(new EditorTab("Profiler", ProfilerTab.Draw));

// A page of settings, which appears because something is on it and is saved and restored with the
// rest of the editor's preferences.
EditorSettings.Heading("My game", "Difficulty");
EditorSettings.Number("My game", "Enemy speed", () => Speed, value => Speed = value, 1);
EditorSettings.Flag("My game", "Friendly fire", () => Friendly, value => Friendly = value, 2);

// What one of the game's own entities looks like in the world list.
EditorKinds.Add(new EntityKind("MyGame.Enemy", "icons/ui/users.png", 15));

// Something to type. The attribute is found at compile time, so nothing registers it.
[Command("bake", "Bakes the lighting: bake <quality>")]
internal static string Bake(int quality) => $"baked at {quality}";

// How one of the game's own values is drawn in the inspector. The attributes on the field say so,
// and the generator carries them through to the schema.
[Range(0f, 100f), Unit("hp")] public float Health;
```

A panel of its own is a class with a draw method, a line adding it to `EditorShell.Tabs`, and
whatever it draws with taken from `EditorSurface`, `EditorWidgets` and `EditorRows` so that it
looks like the rest.

## Verification

**Input is driven rather than simulated.** `SyntheticInput` writes the window's own messages: the
`CursorMoved` and `MouseButtonInput` a real pointer produces, and the `KeyboardInput` a real key
produces, each both as itself and inside the `WindowEvent` batch the backends read. So a click goes
through the picking raycast, the widget that decides it was clicked, and the button state the camera
reads, exactly as a hand's would. Calling the method a click would have called tests the method and
not the path to it, and the failures were on the path: a ring that could not be grabbed, a flyout
that opened once, a selection that cleared itself on the frame it was made, a text field that could
not be typed into. `SyntheticInput.Wheel` does the same for the wheel, which a list that pages and a
camera that zooms read. What none of it can do is move the desktop's cursor, and it does not try.

**A running editor can be driven instead, and usually should be.** `./bcs open --editor` starts one
that answers a socket; `./bcs command input.click 1450 700`, `./bcs command frames.wait 5` and
`./bcs shot /tmp/after.png` then do from a terminal what a probe script does from the environment,
against an editor that is already up. The difference is the loop. A probe is one arrangement per
process, decided before the run starts and read afterwards, while a session is asked and answered a
frame at a time, so what to do next can depend on what the last answer said. The probe covers what a
session cannot, which is a fixed arrangement captured the same way every time with no session to
keep alive. A comparison of the chrome before and after a change needs that, and it is one command
rather than a session to open, drive and stop. Either works on a machine with no display, as long as
the run is given `--offscreen`, which draws the editor into an image instead of onto a screen.

`BCS_PROBE` names what a run should do, as words separated by commas, and `BCS_SHOT` says where to
write the picture. `select`, `several` and `many` put something in the world and choose it, `dock`,
`wide`, `narrow`, `tab`, `assets`, `settings` and `style` arrange the editor, `click`, `hover`,
`drag`, `band`, `pick` and `pull` drive the pointer at the points `BCS_PROBE_CLICK` lists, `into`
opens a number for typing, `keys`, `enter`, `back` and `undo` drive the keyboard, and `project`
saves and reads back. A run prints what the editor ended up holding, so a capture and a few lines of
output answer most questions about a change.

`click` takes three points, a dozen frames apart, so a run can press what opens a list and then
press a row inside it. `BCS_SHOT_FRAME` says which frame to write, for anything that has to be
caught before a run has finished; the default of 180 is after everything a script does, and the
pointer is still wherever it was last put, so what is under it is still under it.

Nothing here is provable by a test alone. `Render.Screenshot` exists for that reason, because a
panel either lays out correctly or it does not, and only the picture says which. A change to the
look is checked by capturing the same probe before and after and comparing the chrome pixel by
pixel, so a refactor that was meant to change nothing can be shown to have changed nothing.

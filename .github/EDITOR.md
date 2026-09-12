# The editor

What `BevyCSharp.Editor` is, how it is meant to look, and what the engine has to grow for it to
work. The page it is built on is described in the README; this is about what goes on top of it.

## What it is for

An editor made of panels that a person can rearrange, replace or write their own version of. The
shipped set is a starting point rather than the product: the world, the entity, the assets, the
tools and the key strip are five panels using one mechanism five ways, and a menu, an enum
dropdown and a right-click are a sixth use of the same thing.

That is the reason the framework came first. A fixed arrangement of menubar, hierarchy, viewport
and inspector is one arrangement, and the moment it is baked into the shell it stops being a
choice.

## The shape of it

Bevy's own words where there is one: the left column is the **world**, because that is what Bevy
calls the thing being listed, and the right column answers what is **selected**, whether that is
an entity, an asset or a setting. Nothing is called a hierarchy or an inspector.

`EditorShell` owns the arrangement and nothing else: three numbers, the rectangles that follow
from them, and a call to each part that draws one. The parts are:

| class | what it draws |
|---|---|
| `EditorPanes` | the panel holding the world beside or above the details, and the handles that move it |
| `EditorStrip` | the tabs along the bottom left, and whichever is open above them |
| `ToolbarView` | the groups of buttons that float in the scene's corners |
| `EditorFlyout` | the menu, and every submenu of it |
| `EditorSceneFrame` | the corner taken off the scene, and the button that docks the panel |
| `EditorSurface` | what all of them draw with: the gutters, the cards, the grab handles, the icons |
| `EditorPicking` | what a click on the scene selects, and what an unanswered one clears |

A part reads the shell's numbers and draws; nothing reads back. Adding a panel is a class with a
draw method and one line in `Tick`, and moving one is moving that line.

**There is a way out of a text field.** A shortcut asks `ImGuiRuntime.Typing`, which is true only
while a box with a caret in it has the keyboard. Asking whether the interface wants the keyboard at
all is a different question with a different answer: with keyboard navigation on, it wants it
whenever any window is focused, which in an editor whose panels are always up is always.

## Design language

Unity's editor is the reference, deliberately and not as a copy. It is the interface most people
coming to this already have in their hands, its density is right for a tool looked at all day
beside the thing being edited, and Unity publishes the values rather than leaving them to be
guessed at. What follows is taken from Unity's Editor Foundations design system, then adjusted
where this editor differs.

### Color

Everything in the editor is drawn on one of six surfaces, and which one says what a thing is. This
is the whole of the scheme; the values live in `EditorTheme` and nowhere else.

| token | value | what it is |
|---|---|---|
| `--app` | `#141414` | the window's ground |
| `--sunken` | `#1A1A1A` | a well cut into a pane: a list, a track |
| `--pane-head` | `#1D1D1D` | a panel's header band |
| `--pane` | `#242424` | a docked panel |
| `--band` | `#2D2D2D` | a section header on a pane |
| `--overlay` | `#2E2E2E` | what floats: a menu, a flyout, a tab, a hint |
| `--raised` | `#313131` | a control's plate |
| `--field` | `#0D0D0D` | a box that is typed in |

Two rules follow from the ladder, and they matter more than the values:

- **Separation is a step on the ladder first and a line second.** A component's name is a band, not
  a rule above a row; a list is a well, not a bordered box. The lines that remain are seams between
  surfaces of different colors, so they are darker than both (`--edge`), never lighter: a light
  hairline reads as a raised edge and suits a translucent panel over a scene, which this is not.
- **What floats is lighter than what it covers.** An overlay painted the same grey as the panel
  under it reads as transparent however opaque it is. One surface color for panels, menus and
  flyouts alike makes a color picker over an inspector an outline with writing in it.

The accent (`#0070E0`) is for what is acting: a tick that is on, a menu row under the pointer, a
field with the keyboard. Everything else is grey, including a slider's fill, which is a lighter grey
against a darker one and reads perfectly well.

Space, height and type come from scales in the same block: `--gap-1` to `--gap-5`, `--row-height`,
`--control-height`, `--head-height`, and `--type-tiny` to `--type-head`. Rules use the names, so the
density of the whole editor is a handful of numbers in one place rather than whatever each rule
happened to be typed with.

### Density

Unity's numbers, which this follows:

- **Font**: 12px body, 11px for toolbar search fields, 10px for grid and track labels, 9px only
  when unavoidable, 14px for list headings, 19px for window titles.
- **Row heights**: 16px for mini controls, 18px for a standard single-line control, 20px for an
  inspector title bar.
- **Text is left aligned**, except button labels, which are centred.
- **Indentation carries nesting** in hierarchies, inspectors and menus.
- The inspector must not scroll horizontally at its **300px width**, which is what the name column
  and a value beside it need without either being cut.
- **The name is a column, not a label.** Every value in a panel starts at the same place however
  long the names are, so a column of values can be read down its own edge. Each row is a two
  column table, weighted towards the value, because a name that runs out of room is still readable
  from its first half and a number that runs out of room is a different number.
- **A panel fills its column.** Its height never changes, so what it holds scrolls inside a frame
  that stays put. A panel that grows and shrinks as its contents change is the single thing that
  makes an interface feel unsteady, and an inspector's contents change constantly. Two panels in
  one column share it.
- **A panel clips and scrolls.** What does not fit is hidden rather than drawn over whatever is
  below, and the whole of a panel's contents scrolls together: the strip of tags and the button
  under an inspector are the end of the list, not furniture pinned below it. The room the tail
  needs is set aside whether or not the tail is showing, because a reserve that depends on what it
  decides is a list that shakes at the bottom.
- **The bottom of the window is the tabs and the key list.** What a tab opens is a flyout over the
  work, dismissed by a click anywhere else, so the columns are the full height of the window whether
  a tab is open or not. A tab can be dragged taller for as long as it is up, and the next one opens
  at the size that suits reading a list.

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

The framework opens documents, binds fields and dispatches commands on its own. What it cannot do
without the bridge is find out what is in the world, which is most of what an editor shows. Every
row below is bridged except the last.

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
- **The engine only rasterises.** The Rust side draws clipped, textured triangles in screen space
  and knows nothing else. The entire interface can be rewritten without touching it.
- **Immediate mode costs a redraw a frame.** A few thousand triangles and one buffer write, which is
  the trade ImGui makes and the reason it is the tool for a panel full of numbers that change.
- **The look is data.** `EditorTheme` is a record of every colour and metric, written to
  `assets/theme.txt` and read back at startup. Nothing else in the editor names a colour.

### Why there are no borders

A bordered box inside a bordered box inside a bordered panel is three lines saying what one gap
says better. What this uses instead is a ladder: the panel is a step above the ground, a card a step above the panel, what is under the
pointer a step above that. Two rules keep it honest:

- **Transparency stops at the panel.** The window a panel is drawn in is the layer against the
  scene and is the most transparent thing there is; the cards in it are heavier; everything from a
  component's card inwards is solid. A box somebody is about to type a number into whose tone
  drifts with whatever passes behind it is a box with no reliable contrast, and the steps between
  the rungs have to hold however bright the scene is.
- **The accent means one thing.** Selected, or in force. A colour that also draws every component
  header is a colour that means nothing.

## The inspector

**Accessors, not offsets.** An offset would need a size, a signedness and a layout to be read
through, and all of those are already known to the compiler that emitted the table, so each field
carries a closure that reads and writes the real struct instead.

`DetailsPanel` draws the cards and `ComponentFields` draws the rows in them. What a field is drawn
as follows from its kind and from the hints its attributes declared: a checkbox for a flag, a field
for a number, a bar with a readout for a number with two ends, three fields across for a vector, a
button that offers the list for a choice, and a dimmed field for anything that cannot be edited. A
heading, a rule and a unit are the field's own declaration too.

There is no drawer table. A field kind is one arm of one switch, so a kind that wants a different
shape is a branch in one method.

A component's **properties are described as well as its fields**, and read and written through
themselves. Something worked out from two fields, something clamped on the way in, something kept
in one unit and shown in another: all of that is the property's own business, and a tool that went
round it would show a number nothing else in the program agrees with. A property with no setter is
a row that can be read and not changed.

**A field says how it wants to be drawn, in attributes.** The generator reads them at compile time
and leaves the answers on the schema as `FieldHints`, so nothing reflects at runtime:

| attribute | what it does |
|---|---|
| `[Label("...")]` | what the row is called, when the field's name is not the right words |
| `[Tooltip("...")]` | a sentence shown beside the row while the pointer is over it |
| `[Unit("m")]` | what the number is measured in, in a column after the value |
| `[Step(0.1)]` | what one pixel of a drag on the handle is worth |
| `[Range(min, max)]` | draws a bar, with the number beside it; `Readout` asks for a box or nothing instead |
| `[ReadOnly]` | drawn on a flat plate rather than in a box, and not written back |
| `[Hidden]` | not drawn at all, on a field or on a method |
| `[Header("...")]` | a word above the field, grouping what follows |
| `[Space]` | a blank row above the field |
| `[Separator]` | a line across the panel above the field |
| `[Info("...", Kind = ...)]` | a sentence in the panel above the field: something to know, a warning, an error |
| `[Foldout("A/B")]` | puts the field in a fold, which opens and shuts; slashes nest them |
| `[Color]` | three numbers that are a color: a patch that opens a picker, and the numbers under it |
| `[Inline]` | three numbers beside each other rather than one per row |
| `[Wide]` | drawn across the panel, with no name column beside it |
| `[ShowIf(nameof(Other))]` | drawn only while another field reads true, or equals a value |
| `[HideIf(nameof(Other), Value)]` | the same, reversed; several conditions may sit on one field |
| `[OnValueChanged(nameof(M))]` | calls the named methods once the field has been changed |
| `[Order(n)]` | where the field or button sits among its neighbours |
| `[Button("...")]` | what a method's button says; `Line` and `Weight` share a row between buttons |
| `[Asset(AssetKind.Mesh)]` | which files to offer for a field that holds an asset |

**A struct inside a component is taken apart.** A field whose type is a struct with fields of its
own is described as those fields, named by the path they came from and folded under the name of the
field they came from. `Front.Held.At` is a row called `At`, inside a fold called `Held`, inside one
called `Front`. Writing one reads the whole component, changes the part and writes it back, so a
part written does not wipe its neighbours. A vector is left alone: it is three numbers a drawer
already draws as one thing.

**The console reads and writes.** The tab along the bottom is where a log is read; the key under
Escape drops the same console into the middle of the window, which is where one command is run and
dismissed. Both are documents with bindings over one `ConsoleView`, and everything they show lives
outside them: `ConsoleLog` is a ring of levelled lines that the output and error streams are teed
into, and `ConsoleCommands` is the list of what can be typed.

A command is a static method with `[Command]` on it, found at compile time by a generator and
registered by a module initialiser, so nothing scans for them and one that does not compile is not
a command:

```csharp
[Command("select", "Selects the first entity with a name: select <name>")]
internal static string Select(string name) { … }
```

Its parameters are read from the words that follow the name and may be strings, numbers or flags;
one string parameter takes everything typed after the name. Returning a string writes that line
back. Anything a person can get wrong is answered with a sentence rather than an exception, since
a console is where people type things that are not quite right.

Note the two attributes that used to share a word: `[Command]` is the console's, and a click on an
element is `[OnClick]`, named for when it runs like `[OnChange]` and `[OnRefresh]` beside it.

**A pass adds what is not a field at all.** `EditorInspector` runs passes before the components,
per component, per field, per method, and after everything. A pass that takes a field says so and
the ordinary drawing of it is skipped, which is enough to add a row, replace a row, hide a row, or
take over a whole component without any of those being a separate mechanism. The editor uses it
for the two things an entity has that are not a component's field: what it hangs from, and what
hangs from it.

**A list is drawn by whoever has one.** Components are laid out in memory, so a component cannot
hold a list or a dictionary and no attribute can change that. Everything else an inspector shows
can: an asset's contents, a tool's own state, a game's managed objects, the children of an entity.
So `InspectorList` is offered a count, a way to draw one element and what adding and removing mean,
and arranges them the way every list in the editor is arranged:

```csharp
InspectorList.Add(
    plan,
    key: "entity/children",
    name: "Children",
    count: children.Length,
    element: (into, index) => into.Add(new ChildLine(into.World, children[index])),
    add: () => items.Add(new Slot()),        // left out for a list that cannot grow
    remove: items.RemoveAt);                 // and for one nothing can be taken from
```

A fold that says how many there are, a fold per element, whatever the element drew inside it, a
button to take each one away and one to add another. A shut list draws one row and does not ask its
elements to draw at all, so a list of a thousand things costs nothing while it is closed.

**More than one thing can be selected.** Control and a click in the hierarchy adds to the
selection or takes something out of it, and the inspector then shows the components every selected
thing carries, marks a field the selection disagrees about as mixed, and writes an edit to all of
them. A vector says so per row, because two things in different places may still agree about two
of the three numbers. A drag on the handles moves, turns or stretches all of them, each about its
own origin, and undoes as one change. What still reads the last one picked is where the handles are
drawn and what the panel's heading names.

A field holding an asset shows the file it points at rather than the number a handle is, and
pressing it offers the files under the asset root that suit it. Which those are is the field's own
business, because a handle to a mesh and a handle to a sound are the same type and nothing else can
tell them apart.

Every number has a handle beside its box. Dragging it sideways changes the number, at the field's
own step or the editor's; shift moves ten times as fast, alt a tenth, and control lands on the
tool's grid. Right clicking a field's name puts it back to what it is on a freshly added component,
which is asked of the component rather than assumed.

## Adding to it

None of this needs a panel of its own.

```csharp
// A row on the menu, which the hamburger, the plus button, the right click on the world and any
// key bound to that path all get at once.
EditorMenu.Command("Spawn/Enemy", world => Spawn(world, "Enemy"), 40);

// A button in the middle of the toolbar. A picture alone is a circle, a word alone a pill.
EditorToolbar.Add(new ToolbarButton(
    ToolbarSlot.Centre, "icons/ui/zap.png", () => string.Empty, world => Bake(world), null, 40));

// A page of settings, which appears because something is on it and is saved and restored with the
// rest of the editor's preferences.
EditorSettings.Heading("My game", "Difficulty");
EditorSettings.Number("My game", "Enemy speed", () => Speed, value => Speed = value, 1);
EditorSettings.Flag("My game", "Friendly fire", () => Friendly, value => Friendly = value, 2);

// What one of the game's own entities looks like in the hierarchy.
EditorKinds.Add(new EntityKind("MyGame.Enemy", "icons/ui/users.png", 15));

// How one of the game's own values is drawn in the inspector: the attributes on the field say so,
// and the generator carries them through to the page.
[Range(0f, 100f), Unit("hp")] public float Health;
```

## Verification

**Input is driven rather than simulated.** `SyntheticInput` writes the window's own messages: the
`CursorMoved` and `MouseButtonInput` a real pointer produces, and the `KeyboardInput` a real key
produces, each both as itself and inside the `WindowEvent` batch the backends read. So a click goes through the picking raycast, the
widget that decides it was clicked, and the button state the camera reads, exactly as a hand's
would. Calling the method a click would have called tests the method and not the path to it, and
the path is where the failures were: a ring that could not be grabbed, a flyout that opened once, a
selection that cleared itself on the frame it was made, a text field that could not be typed into.
`SyntheticInput.Wheel` does the same for the wheel, which is what a list that pages and a camera
that zooms read. What none of it can do is move the desktop's cursor, and it does not try.

Nothing here is provable by a test alone. `Render.Screenshot` exists for that reason: a panel
either lays out correctly or it does not, and only the picture says which. A change to the look is
checked by capturing the same probe before and after and comparing the chrome pixel by pixel, so a
refactor that was meant to change nothing can be shown to have changed nothing.

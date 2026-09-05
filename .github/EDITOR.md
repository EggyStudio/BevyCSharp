# The editor

What `BevyCSharp.Editor` is, how it is meant to look, and what the engine has to grow for it to
work. The panel framework it is built on is described in the README; this is about what goes on
top of it.

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

| where | what | opens |
|---|---|---|
| left column | the world: a tree, a search, and one button that adds to it | always |
| right column | whatever is selected: an entity's components, or an asset's particulars | when something is selected or asked for |
| viewport corners | the menu and undo at the left, the tools in the middle, the information button at the right, the orientation cross at the bottom right | always |
| bottom band | the asset browser or the console, whichever tab is open | from its tab |
| bottom row | the tabs, like a browser's, and the key list beside them | always |
| over the viewport | what the keys do | always |
| over everything | menus, dropdowns, context menus | while they are being read |

**A top split and a bottom one.** The bottom split runs the whole width of the window: whichever
tab is open, and under it one row holding the tabs on the left and the key list beside them, both
the same height. Everything above is the top split, and that is three columns — the side columns as
wide as their contents until their inner edge is dragged, up to a third of the window each, and the
viewport between them. The three dragged numbers are the whole arrangement.

**The middle of the screen is the middle of the screen.** The tool buttons are centred on the
window, not on the viewport, alone among the corner panels. Centring them on the viewport moves
them whenever a column opens, which is a tool that is somewhere else every time it is reached for.

**Nothing is ever told how tall it is.** A panel given a height measures that height afterwards, so
its own contents can never be asked about again: a tree that grows a dozen rows sits inside a panel
that stays the size it was, for good. Instead the height is handed back to the contents
(`bcs_xui_set_rect` reads infinity as `auto`) and the room the column has is written as a maximum
(`bcs_xui_set_limits`). The measurement stays where it belongs and the cap only stops it running
past the column. The same rule is why no member of a row is told the row's height — the row is as
tall as its tallest member, and telling one of them that is a loop that eats the window in two
frames.

**Put away, not closed.** The panels that come and go all day — the world, the panel that describes
the selection, whichever tab is open, the information panel — are never closed. Closing a document
takes it out of the interface's list, and changing that list respawns every widget of every panel: a
blink, a frame at the wrong font, and a panel that eventually does not come back. So they are
concealed instead: `EditorShell.Conceal` and `Reveal` write one display property, the layout is
handed only what is showing, and the document is loaded once per session. `Toggle` still closes,
and that is what a flyout wants.

**A rebuild is counted, not waited out.** `bcs_xui_generation` says how many times the interface has
respawned everything, so a window notices that every element handle it holds is dead at exactly the
moment it becomes true. What this replaced was a fixed number of frames started by whoever opened or
closed a panel, and that was wrong twice over: a rebuild nobody here caused went unnoticed, and two
rebuilds overlapping ended the wait early and left every panel holding handles to widgets that no
longer existed — which is an editor where nothing opens and nothing can be selected until it is
restarted.

**There is a way out of a text field.** A widget takes the keyboard when it is clicked and gives it
up when another widget is clicked, and a click on the scene is not a click on a widget. Without
`bcs_xui_blur` somebody who types in the search box and goes back to the viewport leaves the box
holding the keyboard, and every key the editor binds is a letter going into it. The shell also
refuses to believe a focused element no panel owns, which is what a rebuild leaves behind.

**A docked panel is as tall as its contents**, capped by what its column has left. There is no
fill: a panel with four rows is four rows tall, and opening a tab along the bottom shortens
whatever would have reached into it. The catch is that a panel given a height measures that height
afterwards, so the layout remembers what each one measured while it was free and compares that
against the room instead.

**Nothing is pushed down by a toolbar**, because there is no bar: the tools float in the
viewport's corners as round buttons over the scene, and they follow the viewport as columns open
and close. What is in them is `EditorToolbar`, a table like the menu's, so a game adds a mode to
the viewport by adding a line. A button carries a picture, a word, or both: a picture alone is a
circle, a word alone a pill, and the slot and an order index are the whole of where it goes.

**One gesture cannot mean two things.** The right button steers the camera and asks for a menu, so
how far the pointer travelled while it was held decides which — and the answer is worked out once,
at the top of the frame, before anything reads a right click. Everything downstream agrees with it:
the menu the viewport offers, the menu a panel's row offers (the interface reports its own right
clicks knowing nothing about the camera), and the mesh picking, which ignores the secondary button
outright because a look that begins over an object is not a choice to select that object. The
cursor is locked while the camera turns, so where it is never changes; how far the mouse moved is
the only thing that tells the two apart.

**A handle is the size a hand needs, not the size of what it is on.** `ViewportGizmos.Reach` asks
the camera what ninety pixels are worth where the selection is — a ray through the centre and
another through a point ninety pixels beside it, taken to the same depth — so a handle on a coin
can be grabbed and one on a building does not fill the screen, and neither changes as the camera
moves. It also has to hold still while it is used, and an object's own bounds change with every
frame of a scale drag.

**A handle is grabbed by what it looks like.** Move and scale draw a line along the axis and are
measured against that line. A turn draws a ring, and measuring a ring against the line is why one
could only be grabbed near its centre, where nothing is drawn: the ring is walked as a few dozen
projected segments instead. The point a drag is measured about is the middle of what is on screen
and is held for the whole drag, because an entity's origin and the middle of its bounds are not the
same place, and turning about one while the ring is drawn about the other answers to somewhere
nobody can see.

**A gizmo is drawn about the world, not in it.** The default gizmo config has `depth_bias = -1`,
so a handle on an object is in front of the object rather than inside it, and the queue C# fills is
drained after the managed `Last` systems rather than merely in `Last` — both live in that schedule,
and without the ordering the scheduler may drain the queue before the frame has filled it, which
holds every shape back a frame. A frame is invisible on a selection box and unmissable on the
orientation cross, which is placed relative to the camera and swims across the screen when the
camera turns.

**The orientation cross is laid out by the interface and drawn by the scene.** The bottom right
bar holds an empty transparent square; the panel reads back where the layout put it and
`EditorGizmoSlot` says so; `ViewportGizmos` casts a ray through its centre and draws six arms a few
centimetres in front of the camera, sized by a second ray through the square's edge so no field of
view is assumed and nothing in the scene can get in front of it. Nothing renders to a texture and
nothing tracks a screen position of its own — the square already knows one, and it moves inwards
with the panels because it is in the bar with the other buttons.

**The menu is the editor.** Everything that is not a tool lives behind the hamburger, as a table
of slash separated paths: `Panels/Rendering`, `Spawn/Light/Point`, `View/Every entity`. The same
table serves the hamburger, the plus button (which opens at `Spawn`), a right click on the world
(the same), a right click on an entity (`Entity`), and any key bound to a path. Adding a command
is one line, and it appears in all of them.

**The tools are the toolbar.** Select, move, rotate and scale, on Q, W, E and R, with snapping on
Control. A toolbar of buttons that open panels is a menu that escaped its menu; what belongs on
screen at all times is the handful of things that change what a drag does.

## Design language

Unity's editor is the reference, deliberately and not as a copy. It is the interface most people
coming to this already have in their hands, its density is right for a tool looked at all day
beside the thing being edited, and Unity publishes the values rather than leaving them to be
guessed at. What follows is taken from Unity's Editor Foundations design system, then adjusted
where this editor differs.

### Colour

Unity's dark theme, for reference:

| role | Unity |
|---|---|
| app toolbar | `#191919` |
| window | `#383838` |
| default background | `#282828` |
| toolbar | `#3C3C3C` |
| input field | `#2A2A2A` |
| inspector titlebar | `#3E3E3E` |
| default border | `#232323` |
| button border | `#303030` |
| default text | `#D2D2D2` |
| label text | `#C4C4C4` |
| button text | `#EEEEEE` |
| highlight background | `#2C5D87` |
| focus border | `#7BAEFA` |
| link | `#4C7EFF` |

The rule that matters more than the values: **accent appears on focused, hovered, pressed and
selected states, and greys carry everything else.** Unity's own note is that accented borders
exist "to inset and outset UI to support the layering of the user interface", which is why their
accent is a border and a selection fill rather than a fill on every control.

This editor departs in two ways, both deliberate:

- **Panels are black with transparency, not opaque grey.** The viewport is fullscreen behind the
  panels rather than a pane between them, so a panel is a thing floating over the work rather
  than a wall beside it. Unity's greys assume an opaque docked frame.
- **Corners are rounded.** Unity's are square. The rounding, the inset margin and the single
  accent are the parts taken from newer tools rather than from Unity.

### Density

Unity's numbers, which this follows:

- **Font**: 12px body, 11px for toolbar search fields, 10px for grid and track labels, 9px only
  when unavoidable, 14px for list headings, 19px for window titles.
- **Row heights**: 16px for mini controls, 18px for a standard single-line control, 20px for an
  inspector title bar.
- **Text is left aligned**, except button labels, which are centred.
- **Indentation carries nesting** in hierarchies, inspectors and menus.
- The inspector must not scroll horizontally at its **275px minimum width**.

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

The framework could already open documents, bind fields and dispatch commands. What it could not
do was find out what is in the world, which is most of what an editor shows. Every row below is
now bridged except the last.

| need | how | blocks | entry point |
|---|---|---|---|
| every live entity | `World::iter_entities` | hierarchy | `bcs_ecs_entities` |
| an entity's components | `World::inspect_entity` | inspector | `bcs_ecs_components_of` |
| a component's name | `ComponentInfo::name` | every label in the inspector | `bcs_component_name` |
| an entity's name | Bevy's `Name`, which holds a `String` | hierarchy labels | `bcs_ecs_entity_name` |
| naming an entity | the same, written | a list nobody can work in | `bcs_ecs_set_entity_name` |
| a field's name and type | the source generator, not the bridge | editing a value | none needed |
| where an element ended up | `ComputedNode` and `UiGlobalTransform` | arranging, dragging, dismissing | `bcs_xui_rect` |
| where an element goes | the element's own `Node` | a layout that is data | `bcs_xui_set_rect` |
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

## Stages

Ordered so that each one is worth having before the next exists.

1. **Introspection.** Done. `bcs_ecs_entities`, `bcs_ecs_components_of`, `bcs_component_name`,
   `bcs_ecs_entity_name` and, added later for the same reason, `bcs_ecs_set_entity_name`. Bevy
   discards component names without its `debug` feature, which cost 495 KB to turn back on and is
   the difference between an inspector with headings and one with numbers.
2. **Component metadata.** Done. The generator emits a `ComponentSchema` per `[Behavior]` struct
   with fields and their kinds, and `ComponentSchemas` maps a component id to it.
   **Accessors, not offsets**: an offset would need a size, a signedness and a layout to be read
   through, and all of those are already known to the compiler that emitted the table, so each
   field carries a closure that reads and writes the real struct. Bevy's `Transform` and
   `Visibility` are described by hand, which is the curated list the plan asked for.
3. **Framework: docks, toolbars, flyouts.** Done. Placement moved out of the stylesheet:
   `bcs_xui_set_rect`, `set_visible`, `set_layer`, `bcs_xui_get_visible` and `bcs_xui_rect` write
   and read the ordinary `bevy_ui` node the crate spawned, and `EditorLayout` holds a table of
   placements it arranges from. Because that table is data, a layout writes to text and reads
   back, a drag is nothing more than writing one entry, and a flyout is a panel whose declaration
   says a press outside dismisses it. A toolbar turned out to need nothing at all: it is a panel in
   the top dock whose contents are buttons.

   Docks rather than a grid of nine regions, because the bottom band has to push the columns up
   rather than cover them. `set_layer` writes a **global** z index: a panel is the root of its own
   document, and an order that counts only within one document cannot put a menu over a panel.
4. **The panels.** Done. World, entity, assets, asset, rendering, information, toolbar, tabs, key
   strip and the menu. The test of the framework was whether they needed anything it did not have,
   and they needed four things, all general rather than panel-specific: **repeated bindings**,
   where one id stands for a numbered pool of elements and the member is an array; **`[Show]`**,
   which ties a bool to whether an element is drawn and can be written more than once on one
   member; **`[OnRefresh]`**, where a panel reads the world before its values are written out; and
   **`[Context]`**, which is a right click rather than a click and needed the bridge to tell the
   two apart.

   Two pieces of the framework came out of the panels and belong to anything built on it.
   `EditorMenu` is a table of slash separated paths that the hamburger, the plus button and both
   right-click menus all read, so a command is added once and appears in all of them. `EditorTabs`
   is a list of panels that live minimised along the bottom, which is where a browser belongs.
5. **Selection and saving.** Done, with two caveats. `bcs_pick_events` reports a click on a mesh,
   so the viewport selects, and `bcs_render_bounds` gives the box drawn around what is selected.
   The world's edits and the arrangement of the panels save to `assets/world.json` and
   `assets/layout.txt` and load back.

   **The picking half is unverified.** It is wired end to end and has not been seen to fire: this
   machine runs the editor on Wayland and nothing here can synthesise a pointer into it. The
   hierarchy's selection is verified, and the outline is drawn from the engine's own bounds, so a
   click that selects will be visible the moment it works.

   **The world file keeps one half of the world**: an entity's name and every component with a
   schema, which is what the editor can change. Bevy's own `bevy_world_serialization` is compiled
   in and would write the engine's reflected components properly, and it cannot see a C# component
   at all, since those are bytes registered at runtime with no Rust type behind them. Between a
   file holding the engine's half and one holding the program's half, the program's half is the
   one an editor changed. A file with both is what a world asset eventually needs.

## What the documents cannot do

These constraints shaped the panels, and all of them are the crate's rather than ours:

- **One input per row draws.** Three number boxes side by side show the first one's text and leave
  the other two empty, whatever is written to them, however often. So a vector is three rows, one
  per axis, each with a box that draws. This is the single largest thing a fork of the crate would
  buy back.
- **Text written to a widget before its own text child exists is held and never drawn.** A widget
  draws its text through a child spawned a frame or two after the widget itself, and a write before
  then changes the field, is noticed with no child to update, and is overwritten when the child
  arrives carrying what the document said. The bridge keeps every write for four frames and applies
  it again, which touches the field and has the change noticed a second time with the child there
  to receive it. This was done on the managed side by writing the value with a trailing space every
  other frame for forty frames, which worked and was visible: a trailing space changes how wide a
  label measures, so a panel grew and shrank and a number walked left and right for a second every
  time anything appeared.
- **An element's display is put back by the stylesheet** whenever the interface restyles the
  widget, so what a panel last wrote is not what is in force. Visibility is read before it is
  written rather than remembered.
- **The font has no arrows, chevrons or hamburger.** A glyph it lacks draws as a box and says
  nothing about why, so every icon drawn as text is ASCII and lives in one table, `EditorIcons`.
  Real icons are images instead: the toolbar's are PNGs rasterised from the SVG set in
  `assets/icons`, pointed at by `bcs_xui_set_image`, which sets the source the interface loads
  from. An image inside a `<button>` is dropped, because a button draws its own text and nothing
  else, so a button with a picture in it is a `<div>` that takes the click instead.
- **A widget restyles for one frame at the wrong font size** after anything is written to it. That
  is why nothing is written to an element that already holds the value: writing regardless is a
  flicker sixty times a second.
- **`align-self` and `align-content` are not read.** The first does not matter, because a document's
  body is a column and a column sizes its children by their contents. The second does: a wrapping
  box taller than its lines spreads them down its height with no way to say otherwise, which is an
  asset grid with its rows pushed apart. The fix is to give the lines a box of their own, inside a
  column, so there is no spare height for them to be spread through.
- **A button that is written to loses its font.** A button draws its text through a child it
  rebuilds whenever the text changes, and the rebuilt child comes back without the stylesheet's
  font and layout, so a row of text the editor writes ends up half again too large. A paragraph
  keeps what it was given. So nothing whose text this editor writes is a `<button>`: a row, a tab,
  a menu line and a labelled button are each a `<div>` with a `<p>` inside, and the div takes the
  click just as well, because what reports one is the nearest ancestor with an id.


- **CSS ids are global**, not per document. Every open document is one document as far as the
  crate is concerned, so `#row-0` in one panel and `#row-0` in another are the same element. Every
  id in this editor is prefixed by its panel.
- **A class cannot be given to an element after the document is parsed.** So a selected row says
  so in its own text rather than by being styled. This is the one place the editor visibly settles
  for less than it should, and an entry point that set a class would replace it.
- **Every document lays a body over the whole window**, and a body that takes the pointer swallows
  every click meant for the panels underneath it. `body { pointer-events: none }` and
  `.panel { pointer-events: auto }` in the shipped stylesheet are what make more than one panel
  possible at once.
- **One list of open documents, not one per document.** Opening a second panel with the crate's
  own `add_and_use` takes the first one off the screen, because it replaces that list rather than
  adding to it. The bridge writes the registry's list directly instead, batched to once a frame
  behind a dirty flag, and deliberately does not go through `use_uis`: that asks for a rebuild of
  every open document, and a rebuild is not a blink but a loss — the widgets come back without
  their stylesheet, at the default font and the default layout. Writing the list leaves the
  documents that were already up alone, so opening a panel does not disturb the others. Elements
  resolved during the frames a document is being built are looked up uncached, because the
  entities behind the ids change while it settles.

A fourth is cosmetic and left alone: a slider whose stylesheet is edited while the editor runs
keeps its value and draws its handle at zero until the value next changes. What is drawn and what
is held come apart in the restyle, and nothing on this side can see the difference.

And one that follows from the button finding. Picking reports the deepest thing under the pointer
and the bridge walks up to the first ancestor with an id, so a label with an id of its own would
answer for the row it sits in — and every label the editor writes has one, since that is how it is
written to. `pointer-events: none` on every label and picture inside something clickable puts the
answer back where the command is.

## Verification

**Clicks are driven, not simulated.** `SyntheticInput` writes the window's own messages — the
`CursorMoved` and `MouseButtonInput` a real pointer produces, both as themselves and inside the
`WindowEvent` batch the picking backend reads — so a click goes through the picking raycast, the
widget that decides it was clicked, and the button state the camera reads, exactly as a hand's
would. Calling the method a click would have called tests the method and not the path to it, and
the path is where the failures were: a ring that could not be grabbed, a flyout that opened once, a
selection that cleared itself on the frame it was made. What it cannot do is move the desktop's
cursor, and it does not try.

Nothing here is provable by a test alone. `Render.Screenshot` exists for that reason: a panel
either lays out correctly or it does not, and only the picture says which. Every stage ends with a
capture, and the pictures are compared against the density and colour rules above rather than
against an opinion.

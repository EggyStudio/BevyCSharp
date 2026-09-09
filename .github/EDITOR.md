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
| left column | the world: a tree with a picture per row, a search, and one button that adds to it | always |
| right column | whatever is selected: an entity's components, or an asset's particulars | when something is selected or asked for |
| the whole window | settings: pages on the left, one page's contents on the right | from the menu, the toolbar or Ctrl+, |
| viewport corners | the menu and undo at the left, the tools in the middle, the information button at the right, the orientation cross at the bottom right | always |
| bottom band | the asset browser or the console, both with a filter, whichever tab is open | from its tab |
| bottom row | the tabs, like a browser's, and the key list beside them | always |
| over the viewport | what the keys do | always |
| over everything | menus, dropdowns, context menus | while they are being read |

**A top split and a bottom one.** The bottom split runs the whole width of the window: whichever
tab is open, and under it one row holding the tabs on the left and the key list beside them, both
the same height. Everything above is the top split, and that is three columns: the side columns as
wide as their contents until their inner edge is dragged, up to a third of the window each, and the
viewport between them. The three dragged numbers are the whole arrangement.

**The tool buttons are centred on the window**, alone among the corner panels. Centring them on the
viewport moves them whenever a column opens, which is a tool that is somewhere else every time it is
reached for.

**Nothing measured is ever written back to the thing that was measured.** This is the one rule the
arrangement has, and breaking it costs the same way every time:

| written back | what follows |
|---|---|
| a panel's own measured height | it measures that height for ever and stops following its contents |
| the row height a strip contributed to | the strip grows until it fills the window, in two frames |
| the width a column's only panel measured while put away | the column is a strip of padding for the rest of the session |

A measurement may decide *where* something goes (where the next panel starts, where the viewport
ends, where the right column's inner edge is) and may never decide *how large* it is. How large
comes from three places and no others: the stylesheet, the contents, or a number a person dragged.
So a height is handed back to the contents (`bcs_xui_set_rect` reads infinity as `auto`) with the
room the column has written as a maximum (`bcs_xui_set_limits`); an undragged width is not written
at all, because `auto` is not the stylesheet's answer but "as wide as the longest word in it"; and
no member of a row is ever told the row's height.

**Nothing is closed while anybody is using it.** Opening or closing a document takes it out of the
interface's list, and changing that list respawns every widget of every panel: a rebuild in the
middle of whatever gesture caused it. Every panel that comes and goes is concealed instead:
`EditorShell.Conceal` and `Reveal` write one display property, the layout is handed only what is
showing, and each document is loaded once per session. `Hide` closes for good and nothing in the
ordinary run of the editor calls it.

Flyouts are where this costs the most, because a flyout is dismissed by the first press of whatever
somebody does next, so closing one there is a rebuild in the middle of that press. There is one
menu, built the first time anything asks for one and pointed at something else thereafter, so
opening the hamburger six times in a row leaves the rebuild counter where it started. A menu built
fresh each time moves it twice per opening.

**Four tables, and nothing added any other way.** What is on the menu, what is on the toolbar, what
a page of settings holds, and what an entity looks like in a list are all lists of records with an
order and a lookup: `EditorMenu`, `EditorToolbar`, `EditorSettings`, `EditorKinds`. A game adds a
spawn command, a tool button, a preferences page or a picture for its own component by adding a line
to one of them, and no panel in this editor knows the name of a single one of its types. That is the
whole reason the panels are as short as they are, and it is why the tables are the part with tests
under them rather than the panels.

**Escape closes before it quits.** In order of how much of the screen a thing has: a sheet, then a
flyout, then the program. Quitting because somebody reached for the key that shuts every other
window they have ever used is not a defensible thing for a tool to do, and a settings sheet covering
the window is exactly when they will reach for it.

**A sheet takes the whole window.** Settings take the whole window rather than a column, because a
page of preferences squeezed beside a hierarchy is a page nobody opens. Nothing this side can
reliably draw one panel over another, so a sheet does not cover the editor. It puts it away and
fetches it back, which is both what the interface can do and what a sheet means. While one is up
nothing else may be revealed either, or a panel that answers to something other than a person asking
for it, such as the selection, fetches itself back on top.

It keeps the margin, the border and the rounded corners the panels have, so it reads as a page laid
on the editor rather than as a different program. What it does not keep is the translucency: a whole
window of settings with a scene showing through it reads as a mistake.

**A blue dot is what "this one" looks like.** The open tab, the tool in force, the axes in use and
the selected row in the hierarchy all wear the same dot. A character in front of the name would do
as well and moves the name sideways whenever the state changes, so a list of them shuffles as
somebody arrows down it. A dot has a place of its own and the names hold still.

**A row of the hierarchy says what a thing is.** An entity is whatever components are on it and the
engine has no notion of a camera or a light, so the answer is assembled from what it carries and
the first match in order wins: a camera beats a mesh, a mesh beats a behavior of the project's own,
and anything else is a plain entity. Matched on the component's qualified name and remembered per
component id, because naming one crosses the ABI and the answer never changes while an app runs.

**An inspector shows what can be edited and lists what cannot.** A component with fields is a block
with a heading that opens and shuts, and every C# behavior is one of those. A behavior *is* a
component and the generator writes a schema for it, so `Spin` sits beside `Transform` with its own
`Speed` and `Angle`. A component with nothing to show is a chip in a strip at the foot: a marker
with no fields and one this side has no description of make the same statement, which is "this is on
it, and that is all I can tell you". A heading with an empty space under it says the opposite.

**Panels are not dragged.** The arrangement is three columns and a bottom split, and every
panel belongs to one of them: what a person adjusts is where the edges between them are, not where
a panel floats. Dragging a docked panel by its title bar was a window manager's idea in a layout
that is not one, and picking one up by a corner and dropping it over the scene left it nowhere the
layout could put it back from. The edges are still dragged; the panels are not.

**A press and the click it becomes arrive separately.** A flyout's own button sees both: the press
dismisses the flyout as an outside click, and the click a few frames later finds it closed and opens
it again, so the button that opened it could never close it. `DismissedByThisPress` is what the
click asks, and only a button whose own menu was the one dismissed declines to reopen; a press that
happened to close somebody else's menu opens its own.

**A rebuild is counted rather than waited out.** `bcs_xui_generation` says how many times the
interface has respawned everything, so a window notices that every element handle it holds is dead
at exactly the moment it becomes true. Waiting a fixed number of frames from whoever opened or
closed a panel is wrong twice over: a rebuild nobody here caused goes unnoticed, and two rebuilds
overlapping end the wait early and leave every panel holding handles to widgets that do not exist,
which is an editor where nothing opens and nothing can be selected until it is restarted.

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

**A right click is told from a look by where the pointer ends up.** Whether a right click was a
click or a camera look is decided by how far the pointer ended up from where it started, not by how
far it travelled: a hand reports a fraction of a pixel most frames, and adding those up calls any
click held for a moment a drag, which is a context menu that works the first quick time and never
again. Summed as a direction, that jitter cancels itself. The distance travelled still counts, but
at a threshold no click reaches however long it is held, because a menu that never opens is a far
worse failure than one that opens after somebody turned the camera in a circle and let go where they
started.

**Everything that reads a right click reads the same answer.** The right button steers the camera
and asks for a menu, so how far the pointer travelled while it was held decides which, and the
answer is worked out once, at the top of the frame, before anything reads a right click. Everything
downstream agrees with it: the menu the viewport offers, the menu a panel's row offers (the
interface reports its own right clicks knowing nothing about the camera), and the mesh picking,
which ignores the secondary button outright because a look that begins over an object is not a
choice to select that object. The cursor is locked while the camera turns, so where it is never
changes; how far the mouse moved is the only thing that tells the two apart.

**A handle is a fixed size on screen.** `ViewportGizmos.Reach` asks
the camera what ninety pixels are worth where the selection is: a ray through the centre and
another through a point ninety pixels beside it, taken to the same depth. So a handle on a coin
can be grabbed and one on a building does not fill the screen, and neither changes as the camera
moves. It also has to hold still while it is used, and an object's own bounds change with every
frame of a scale drag.

**What the interface can undo is written on every arrangement.** A widget restyled by the
crate goes back to what the stylesheet says, so anything the stylesheet has no opinion about (a
maximum height, a layer) is quietly dropped. Those are written on every arrangement; only what can
be read back (a position, a visibility) is compared first. A remembered write is a cap that
silently stops holding.

**A handle is a solid thing, drawn with lines.** There is nothing to draw with but lines, so the
ball in the middle, the ball on the end of a stretch arm and the cone on the end of a move arm are
all filled the same way: rings out from the middle and spokes across them, with the counts worked
out from how many pixels across the shape is, so the gap between neighbours stays under the width
of a line. A wire outline of a small thing is a scribble, and a ring in the middle of three arms
reads as a fourth thing to aim at the edge of rather than as one thing to press.

**Every tool has a handle that picks no axis.** The ball in the middle is the thing the three arms
cannot do: a move across the screen rather than along a line, a turn about whichever way the hand
went rather than about one axis, and a stretch of all three at once. The move is a ray onto the
plane through the selection facing the camera, so the thing follows the pointer exactly however the
view is angled; the turn is a trackball about the camera's own two axes; the stretch is how far the
hand has gone as a fraction of the handle's own size, because the distance from the middle, where
the grab began, is nothing to divide by.

**Global or local, and both halves have to agree.** `EditorTools.Space` is asked once, and the
handle drawn and the drag applied read the same answer. The three axes are captured when a handle
is taken hold of and held for the whole drag: in local space they turn with the thing, and a
rotation read against axes that the same rotation is moving accelerates away from the hand.

**A handle is grabbed by what it looks like.** Move and scale draw a line along the axis and are
measured against that line. A turn draws a ring, and measuring a ring against the line is why one
could only be grabbed near its centre, where nothing is drawn: the ring is walked as a few dozen
projected segments instead. The point a drag is measured about is the middle of what is on screen
and is held for the whole drag, because an entity's origin and the middle of its bounds are not the
same place, and turning about one while the ring is drawn about the other answers to somewhere
nobody can see.

**Two gizmo groups, one depth tested and one not.** A handle, an outline or a
marker is a control drawn *about* the scene and has to be reachable, so it goes in a group with
`depth_bias = -1` and nothing can hide it. A grid, a path or a wireframe is drawn *in* the scene and
has to be behind what is in front of it, or it is not describing the scene at all. That is the
default group, left exactly as the engine set it up. `Gizmos.Line` and its neighbours take
`inFront`, true by default, and the bridge routes the shape to one group or the other.

**The ground grid** is what says which way is level and how big things are; a scene without one is a
handful of objects in a void, and moving something is a guess about how far it went. Its spacing
steps by tens as the camera climbs, so it is the same density on screen at any height, and it has a
height of its own, under the scene rather than through it: a grid on the same plane as a floor
fights it for every pixel, and one at the height of what is standing on it cuts those things in
half.

**The grid has no edge, and no step between spacings.** Round rather than square: each line is cut
to the chord of a disc and thins to nothing at the rim, because a square of lines ending all at once
announces where the editor stopped drawing, which is a fact about the editor and not about the
scene.

Three spacings are drawn, a decade apart, and each fades **in** as well as out. How solid one is
asks a question about that spacing alone: full when its cells are the size the height calls for,
fading away over the decade below and the two decades above. So nothing changes at the moment two
of them swap roles, and a ten metre line is as solid at ninety metres up as at a hundred and ten.
Both halves matter and only one is obvious: a grid written without thinking about the way up fades
out correctly and *appears* at full strength, so descending looks right and every step of the climb
drops a whole new spacing onto the floor in one frame. Coming in takes twice as long as going and
eases rather than ramping, because something arriving is noticed and something leaving is not.

**How far the grid reaches follows the camera's height.** Sized by its own spacing, the coarsest
grid runs a hundred times further out than the finest, a haze of lines at the horizon long after
they have stopped saying anything about where things are. Every spacing stops at the same distance,
which is a multiple of how high the camera is.

**The grid is centred under the camera.** Following the point the camera looks at sounds helpful
and is not: turning on the spot then drags the whole floor around with the view, and the grid stops
being a fixed thing the camera moves over.

**The two axes are drawn once.** Every spacing has a line at zero and would color it, so leaving it
to them puts the axis out three times at three strengths, each fading outwards from its own grid's
centre, which is snapped to its own spacing. The lines land on top of each other and their fades do
not, which reads as one line that will not line up with itself.

The whole thing costs about four percent of a frame.

**Gizmos in the front group are drawn over the scene.** The default gizmo config has `depth_bias =
-1`, so a handle on an object is in front of the object rather than inside it, and the queue C#
fills is drained after the managed `Last` systems rather than merely in `Last`. Both live in that
schedule, and without the ordering the scheduler may drain the queue before the frame has filled it,
which holds every shape back a frame. A frame is invisible on a selection box and unmissable on the
orientation cross, which is placed relative to the camera and swims across the screen when the
camera turns.

**The orientation cross is laid out by the interface and drawn by the scene.** The bottom right
bar holds an empty transparent square; the panel reads back where the layout put it and
`EditorGizmoSlot` says so; `ViewportGizmos` casts a ray through its centre and draws six arms a few
centimetres in front of the camera, sized by a second ray through the square's edge so no field of
view is assumed and nothing in the scene can get in front of it. Nothing renders to a texture and
nothing tracks a screen position of its own: the square knows one, and it moves inwards
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

### Color

Everything in the editor is drawn on one of six surfaces, and which one says what a thing is. This
is the whole of the scheme; the values live in `:root` at the top of `editor.css` and nowhere else.

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
  under it reads as transparent however opaque it is. That single mistake — one surface color for
  panels, menus and flyouts alike — is what made this interface look unfinished for a long time: a
  color picker over an inspector was an outline with writing in it.

The accent (`#0070E0`) is for what is acting: a tick that is on, a menu row under the pointer, a
field with the keyboard. Everything else is grey, including a slider's fill, which is a lighter grey
against a darker one and reads perfectly well.

Space, height and type come from scales in the same block: `--gap-1` to `--gap-5`, `--row-height`,
`--control-height`, `--head-height`, and `--type-tiny` to `--type-head`. Rules use the names, so the
density of the whole editor is a handful of numbers in one place rather than whatever each rule
happened to be typed with.

### On using a CSS framework

Worth writing down, because it looks like the obvious answer. The classless frameworks
(Pico, Simple, MVP, Tacit) style semantic HTML for reading: headings, prose, forms, tables, on a
light-first palette, at a document's density. A tool is the opposite of a document — eighteen pixel
rows, panes that fill a column, nothing that reflows — so adopting one means overriding nearly all
of it and inheriting the half that does not apply. Open Props is the closer idea, being tokens and
no components, but its tokens are a web palette (fluid type, shadow ramps, animations) sized for
pages rather than panels.

What was worth taking is the principle rather than any package: one place that holds the surfaces,
the space, the type and the radii, and rules that name those rather than repeat numbers. That is
what the block above is. The renderer is also a subset of CSS — it has grid, calc, transitions and
custom properties, but no pseudo-elements — so a framework written for browsers would be partly
ignored in ways that are hard to see.

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
  long the names are, so a column of values can be read down its own edge. A drawer that wants the
  whole width says so and there is no name column for that row at all.
- **A docked panel fills its column.** Its height never changes, so what it holds scrolls inside a
  frame that stays put. A panel that grows and shrinks as its contents change is the single thing
  that makes an interface feel unsteady, and an inspector's contents change constantly. Two panels
  in one column share it and are as tall as what is in them, since neither can fill it.
- **A panel clips and scrolls.** What does not fit is hidden rather than drawn over whatever is
  below, and the whole of a panel's contents scrolls together: the strip of tags and the button
  under an inspector are the end of the list, not furniture pinned below it. The room the tail
  needs is set aside whether or not the tail is showing, because a reserve that depends on what it
  decides is a list that shakes at the bottom. A panel that would rather fill its column than hug
  its contents says `Stretch` in its placement.
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

## The interface is ours

`native/bcs_ui` is this project's copy of the interface that draws the panels, taken from
`bevy_extended_ui` under Apache 2.0 with `NOTICE.md` beside it saying so. It was copied rather than
depended on because the editor kept hitting the same wall: a shortcoming in the interface meant a
workaround in the editor, and the workaround made the next thing harder.

What changed since:

- **A program's decisions about an element's box survive a restyle.** Applying the stylesheet used
  to write the whole of a `Node`, so where a panel was, how large it was and whether it was on
  screen lasted until the next restyle and no longer. A `StyleOverride` component holds what the
  program decided and is applied last, which is what `bcs_xui_set_rect`, `set_limits` and
  `set_visible` now write.
- **A document's body is the window.** It was sized by its contents, so an element anchored to the
  right or the bottom was anchored to the right of nothing and landed off screen. It also does not
  take the pointer, which every document but the front one has to do or it swallows their clicks.
- **`align-content` and `align-self` are read**, so a wrapping box can be told to pack its lines.
- **A widget that comes and goes inside a frame no longer ends the process.** The marker a restyle
  leaves on a new node is queued quietly rather than asserted.
- **The pictures on a checkbox, a choice box and a color swatch are the editor's own**, drawn in
  the same hand as the rest, and compiled in so a game that ships no icons still has a checkbox
  that looks like one.

## What the documents cannot do

These constraints shaped the panels, and the ones left are the interface's rather than ours:

- ~~Hiding an element takes two writes~~ and ~~painting an element defeats hiding~~ and ~~a failed
  command ends the process~~. **All three are fixed** in `native/bcs_ui`, which is this project's
  copy of the interface rather than a dependency. Applying the stylesheet used to write the whole
  of an element's box, so anything a panel had decided about where an element went, how large it
  was or whether it was on screen lasted until the next restyle. A `StyleOverride` component now
  holds what the program decided and is applied after the stylesheet, which is why a panel no
  longer has to fight for its own layout. The marker a restyle leaves on a new node is queued
  quietly, so a widget that comes and goes inside one frame no longer ends the process.
- **Text written to a widget before its own text child exists is held and never drawn.** A widget
  draws its text through a child spawned a frame or two after the widget itself, and a write before
  then changes the field, is noticed with no child to update, and is overwritten when the child
  arrives carrying what the document said. The bridge keeps every write for four frames and applies
  it again, which touches the field and has the change noticed a second time with the child there
  to receive it. Forcing the redraw from the managed side instead means writing the value with a
  trailing space every other frame, which works and is visible: a trailing space changes how wide a
  label measures, so a panel grows and shrinks and a number walks left and right for as long as the
  writing goes on.
- **What a program decides about an element used to be put back by the stylesheet** whenever the
  interface restyled the widget: its display, its box, and its color. All three are now written
  as a decision the sheet is applied before rather than after, so a panel can hide a row, place a
  flyout and paint a patch of color and have all three still be true a frame later. Painting was
  the last of them, and the one that shaped the most code around it: the inspector drew its handles
  as pictures of colors and its color fields as six digits because a painted element could not
  reliably be hidden.
- **A panel's widgets draw over a flyout that is provably above them.** The drawing order is one
  sorted list and `bcs_xui_stack` reports each element's place in it: a menu opened over the
  inspector sits at 1797 against the panel's 143 to 972, and the menu's own background and text do
  cover the panel's background. Some of the panel's widgets still draw over the menu. Painting the
  menu bright green proved it is opaque and that it is the panel's boxes and patches that arrive
  afterwards, so this is not translucency and not the stack. Not yet explained; a menu opened from
  a row is still put at that row, because being next to its subject is worth more than being clear
  of it.
- **Two rules of equal weight were settled by whichever the map handed over first.** A compound
  selector counted only its first name, so `.field-note.warn` weighed the same as `.field-note` and
  which color a warning took was a matter of iteration order. Every name in a step is counted now.
- **A report in flight outlives the widget that made it.** A widget's change is noticed on one
  frame and delivered on the next, and a rebuild in between hands the entity ids out again: the
  report still names an id, that id belongs to another element now, and the value it carries is
  written into whatever binding claims it. A scale of one became the mass from ten rows down. The
  reports around a rebuild are dropped, for as many frames as the interface takes to respawn its
  widgets, since nothing typed before one is worth keeping.
- **A row that is reused reports what it used to say.** The pool draws whatever line is scrolled
  into it, and a widget whose text is replaced reports the change a frame or two later, which is
  indistinguishable from somebody typing that text into the new field. A row that turns therefore
  ignores what its widgets report for a few frames.
- **The font has no arrows, chevrons or hamburger.** A glyph it lacks draws as a box and says
  nothing about why, so every icon drawn as text is ASCII and lives in one table, `EditorIcons`.
  Real icons are images instead: the toolbar's are PNGs rasterised from the SVG set in
  `assets/icons`, pointed at by `bcs_xui_set_image`, which sets the source the interface loads
  from. An image inside a `<button>` is dropped, because a button draws its own text and nothing
  else, so a button with a picture in it is a `<div>` that takes the click instead.
- **A widget restyles for one frame at the wrong font size** after anything is written to it. That
  is why nothing is written to an element that already holds the value: writing regardless is a
  flicker sixty times a second, and why only the *first* write to a widget is repeated for the
  frames its text child takes to appear. Repeating every write costs four restyles per row per
  frame, which a panel pointed at something moving pays on every row it has.
- ~~`align-self` and `align-content` are not read.~~ **Both are read now.** They were parsed
  nowhere and applied nowhere, so a wrapping box spread its lines down whatever height it had and
  nothing could say otherwise. `native/bcs_ui/tests/selectors.rs` checks that a rule naming them
  survives parsing.
- **A button that is written to loses its font.** A button draws its text through a child it
  rebuilds whenever the text changes, and the rebuilt child comes back without the stylesheet's
  font and layout, so a row of text the editor writes ends up half again too large. A paragraph
  keeps what it was given. So nothing whose text this editor writes is a `<button>`: a row, a tab,
  a menu line and a labelled button are each a `<div>` with a `<p>` inside, and the div takes the
  click just as well, because what reports one is the nearest ancestor with an id.


- ~~`backdrop-filter` is drawn over the element's own background rather than under it.~~ **It
  works now**, and `BevyCSharp.Sample`'s own panel uses it: the element's background color is the
  tint the blurred screen is mixed with, which is what a frosted panel is. It was fixed by
  something else: a document's body was sized by its contents rather than by the window, and what
  the blur sampled followed. The editor's panels are still plain translucent black, which is a
  choice rather than a limitation.
- ~~Only the first class in a `class` attribute is matched.~~ **Not true**, and the editor's
  one-class-per-element habit is left over from believing it. A rule naming two classes matches an
  element carrying both, and an element carrying several is matched by a rule naming any of them,
  which `native/bcs_ui/tests/selectors.rs` now checks.
- **CSS ids are global**, not per document. Every open document is one document as far as the
  crate is concerned, so `#row-0` in one panel and `#row-0` in another are the same element. Every
  id in this editor is prefixed by its panel.
- **A class is one class.** An element's class can be set while the editor runs, and the interface
  applies the stylesheet again when it notices, which is how a selected row takes on a background
  the document knew nothing about. What it cannot have is two of them: the interface matches only
  the first, so `bcs_xui_set_class` takes one name and replaces whatever was there. It is written
  only when it changes, since the interface restyles the element every time it is written.
- **Every document lays a body over the whole window**, and a body that takes the pointer swallows
  every click meant for the panels underneath it. `body { pointer-events: none }` and
  `.panel { pointer-events: auto }` in the shipped stylesheet are what make more than one panel
  possible at once.
- **One list of open documents, not one per document.** Opening a second panel with the crate's
  own `add_and_use` takes the first one off the screen, because it replaces that list rather than
  adding to it. The bridge writes the registry's list directly instead, batched to once a frame
  behind a dirty flag, and deliberately does not go through `use_uis`: that asks for a rebuild of
  every open document, and a rebuild is not a blink but a loss: the widgets come back without
  their stylesheet, at the default font and the default layout. Writing the list leaves the
  documents that were already up alone, so opening a panel does not disturb the others. Elements
  resolved during the frames a document is being built are looked up uncached, because the
  entities behind the ids change while it settles.

A fourth is cosmetic and left alone: a slider whose stylesheet is edited while the editor runs
keeps its value and draws its handle at zero until the value next changes. What is drawn and what
is held come apart in the restyle, and nothing on this side can see the difference.

And one that follows from the button finding. Picking reports the deepest thing under the pointer
and the bridge walks up to the first ancestor with an id, so a label with an id of its own would
answer for the row it sits in, and every label the editor writes has one, since that is how it is
written to. `pointer-events: none` on every label and picture inside something clickable puts the
answer back where the command is.

## The inspector

The panel that shows what is selected owns a pool of rows and nothing else. Everything about what
goes in them is decided elsewhere, in three pieces.

**A drawer draws one kind of value.** `IFieldDrawer` answers whether it takes a field, says how
many rows it wants, fills each of them, and reads back what was typed or ticked. There is one per
kind of value and each is a file: a number, a number with two ends, three numbers, a rotation as
three angles, a color, a flag, a choice, a set of flags, a reference to another entity, and text
for anything left over. The table is searched newest first, so a game takes over a field by adding
a drawer after the built-in ones.

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

Nothing below needs a panel, a document or a stylesheet.

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

// How one of the game's own values is drawn in the inspector. Added after the built-in drawers,
// so it wins for the fields it takes.
EditorDrawers.Add(new HealthBarDrawer());

// A line in the inspector that is not a field of anything: a warning, a readout, a button that
// only makes sense for one kind of thing.
EditorInspector.OnAfter(plan =>
{
    if (plan.World.NameOf(plan.Entity) is null) plan.Note("This entity has no name.");
});
```

## Verification

**Clicks and the wheel are driven rather than simulated.** `SyntheticInput` writes the window's own messages: the
`CursorMoved` and `MouseButtonInput` a real pointer produces, both as themselves and inside the
`WindowEvent` batch the picking backend reads. So a click goes through the picking raycast, the
widget that decides it was clicked, and the button state the camera reads, exactly as a hand's
would. Calling the method a click would have called tests the method and not the path to it, and
the path is where the failures were: a ring that could not be grabbed, a flyout that opened once, a
selection that cleared itself on the frame it was made. `SyntheticInput.Wheel` does the same for
the wheel, which is what a list that pages and a camera that zooms read. What none of it can do is
move the desktop's cursor, and it does not try.

Nothing here is provable by a test alone. `Render.Screenshot` exists for that reason: a panel
either lays out correctly or it does not, and only the picture says which. Every stage ends with a
capture, and the pictures are compared against the density and color rules above rather than
against an opinion.

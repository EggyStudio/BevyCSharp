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
| top, centred | the tools, and the information button | always |
| left column | the world: a tree, a search, and the editor's own commands | always |
| right column | the entity, the asset, the rendering settings, the information | when something is selected or asked for |
| bottom band | the asset browser and the console, and whatever else is given a tab | from its tab |
| bottom strip | the tabs, and what the keys do | always |
| over everything | menus, dropdowns, context menus | while they are being read |

The bottom band pushes the columns up rather than covering them, which is what `EditorDock` and
the layout are for: a dock is a place panels share and reflow within, not a container that owns
them. A panel can be dragged out of its dock by its title bar and becomes floating, which is one
entry in a table that can be saved.

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
- **Text written to a widget before its own text child exists is held and never drawn**, and
  writing the same value again is not a change, so nothing redraws it. Every text binding therefore
  writes the value with a trailing space twice a second and without one the rest of the time: a
  change the eye cannot see, and everything that reads a value back trims it.
- **An element's display is put back by the stylesheet** whenever the interface restyles the
  widget, so what a panel last wrote is not what is in force. Visibility is read before it is
  written rather than remembered.
- **The font has no arrows, chevrons or hamburger.** A glyph it lacks draws as a box and says
  nothing about why, so every icon in the editor is ASCII and lives in one table, `EditorIcons`,
  for whoever ships a font with the better ones.


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
  adding to it. The bridge sets the whole list every time, and since doing that respawns every
  widget on screen, the shell stops all the panels reading their elements until the rebuild
  reports itself done.

A fourth is cosmetic and left alone: a slider whose stylesheet is edited while the editor runs
keeps its value and draws its handle at zero until the value next changes. What is drawn and what
is held come apart in the restyle, and nothing on this side can see the difference.

## Verification

Nothing here is provable by a test alone. `Render.Screenshot` exists for that reason: a panel
either lays out correctly or it does not, and only the picture says which. Every stage ends with a
capture, and the pictures are compared against the density and colour rules above rather than
against an opinion.

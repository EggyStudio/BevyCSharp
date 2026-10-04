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
| `Ground` | `#000000` | what lies between the panels and round the scene while docked, solid unless its alpha is lowered to let the desktop through |
| `Panel` | `#1A1A1A` | a panel: the world list, the details, an open tab and the window that adds an entity |
| `Group` | `#262626` | a component's rows inside a panel |
| `Field` | `#3E3E3E` | a box that is typed in, a button, the groove of a bar, the plate of a menu or a tooltip, and the plate of the toolbars, the tabs and the window's buttons |
| `Hover` | `#525252` | a control under the pointer |
| `Active` | `#666666` | one being held down |
| `Line` | `#3C3C3C` | the rare rule, where a gap will not do, gray like the rest of the ladder |
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
- **What floats wears the field gray.** A menu and a tooltip are held up in front of the work, and
  they wear the plate every box and button in a panel is drawn on, so a flyout reads as one of the
  controls that opened it grown large enough to hold a list. A step brighter than that stood apart
  from everything round it. A window with fields of its own in it, such as the one that adds an
  entity, is a card instead, since a box on the field gray is the color of what it sits on.
- **A row lifts off whatever it is lying on.** A fixed gray that lifts off a card disappears into a
  menu's plate, so a row under the pointer is drawn as a wash of the text color instead, which is
  a step above anything. Over a card it comes out at `Hover`.

The accent means one thing only, what is selected or what is in force. A color that also draws every
component header is a color that means nothing. `Warn` and `Bad` are the exception, and they are for
what the program has to say rather than for what it is.

**Every color carries its own alpha, and that is the only transparency.** Every surface is solid
by default, docked or floating, since a panel whose tone drifts with whatever passes behind it is a
panel with no reliable contrast. Lowering the ground's alpha lets the desktop show dimmed between
the docked panels on a see-through window. The style tab edits the alpha
under each color's picker, and a theme file writes a color with alpha as `#RRGGBBAA`. The ground
is painted by the renderer rather than by the interface, as the window's clear color round the
viewport and as the fill of the viewport's rounded corners, since anything the interface paints
lies over the scene as well as round it.

### Density

Blender's density is the reference, where areas sit a few pixels apart and every pixel of gap is
taken from the scene and the panels. What this editor settled on, and where each lives:

| number | value | where |
|---|---|---|
| text | 15px, one face for words and one for figures | `EditorShell.Lettering` |
| a row | the text plus `FramePadding` of `6, 3`, which is 21px | `EditorTheme` |
| the gap between any two surfaces | 6px | `EditorSurface.Gutter` |
| the air a card keeps inside its own edge | 4px | `EditorSurface.Air` |
| anything lying on the scene | 28px tall | `EditorSurface.Tall` |
| a word on something pressable | 10px of air at each end of it | `EditorSurface.Sides` |
| a menu or a tooltip's own air | `8, 6` | `EditorSurface.Around` |
| the title row a floating panel's toolbars sit in | the toolbars' inset plus a button, 36px | `EditorWindowFrame.Grip` |
| the narrowest the panel goes | 320px | `EditorShell.Narrowest` |
| where it stops being two columns | 460px | `EditorShell.Stacks` |

**One gap, everywhere.** A panel's edge to its cards, one card to the next, the scene to whatever
is beside it, and a card to the window's edge are all the same number. Two gaps of different widths
in one picture read as an arrangement that has slipped. A grab handle lies in one of those gaps, a
third of it thick, which is the thickness ImGui gives a scrollbar's grab, and is drawn only while
the pointer is on it or it is being dragged, docked or floating.

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

### The window's own frame

The editor asks for a window without the platform's title bar and border, and draws its own in
their place. The window's pin, an empty button to take hold of it by, and its minimize, maximize
and close buttons lie at its top right as round buttons like the rest of what floats, over the top of the panel's column, since the panels and
the scene run up to the window's top, with the same gap there as at every other edge. The field
in the row under them stops short of them (`EditorSurface.FullWidth`), which is the world's search
box when the world is above the details and the entity's name when the two are side by side. While the panel floats, a title row runs across the scene
with the toolbars at its left, and pressing it where nothing else is moves the window and pressing
it twice maximizes it. Along the top of the window runs a band that does the same and draws
nothing: over the scene down to where its toolbars begin, and over the panel down to where its
fields begin, so the window is taken hold of without aiming for a line a few pixels tall. The
editor opens docked, and the arrangement it was left in is saved with its preferences. The few pixels round every edge resize the window, with the pointer changing shape to say
so. None of that is drawn. The editor draws every frame it can rather than waiting for the display
(`Config.Vsync` off), so the statistics card measures what a scene costs rather than the refresh
rate.

The window is see-through (`Config.Transparent`) where the platform allows. Docked, what lies
between the panels and round the scene is the ground, solid black unless its alpha is lowered, and
only the window's rounded corners show the desktop. The ground is a camera of its own behind the scene, which draws nothing and clears the
window to the ground color with the window's corners rounded off to clear, so docked the window is
as round as it is floating. The scene keeps its gray, because its camera clears to it, and its
corners are rounded by the renderer and filled with the ground, so they meet what surrounds them.
Floating, the scene is the whole window and its corners are the window's, rounded to clear.
Maximized, the window's corners are square. Where the platform cannot composite a window with
alpha, what is clear is black, which is how the editor looked before.

### Adding and arranging entities

The Add Entity button at the bottom of the world list, `Ctrl+A` and `Entity/Add` open a window in
the middle of the screen listing every row under `Spawn/` in the menu, with a search box that has
the keyboard from the moment it opens and says in its hint where the new entity will go. The
arrows move the choice and Enter adds it, as Godot's node window does. Add Component at the bottom
of the details opens the same window (`PickerWindow`) over what can be put on the entity, so the
two ways of adding something read as one. While it is open the rest of the editor is dimmed, with the dim rounded
to the window's corners, so they stay clear. What is added goes under the entity selected when the window opened, or at the top of the
world when nothing was, and undoing it takes it away as one step. A game's own
`EditorMenu.Command("Spawn/Enemy", …)` appears in the window without anything else.

A row in the world list can be dragged onto another to go under it, and onto the empty space
below the rows to go back to the top of the world. Dragging a selected row drags the selection,
and a branch moves whole. A row that would end up under itself is refused before the drop, so it
does not light up. Whatever moves keeps its place in the world, because its transform is worked
out again against the new parent (`EditorHierarchy.Reparent`), and the move is one step to undo.

### What an entity is drawn with

The details show an entity's mesh and material under its components, each as a button naming its
file, or "made here" for one built in code, that opens the picker as a grid of tiles with Bevy's
shapes (or a new material), what the scene already draws with, and every part of every model file, with a card folded under each
(`DrawnCards`). The Mesh card turns the mesh in a preview, draws it as edges on request, edits
a primitive's measures as rows that rebuild it in place, and lists what it is made of and where it
came from. The Material card turns a sphere in the material
and lists its settings as rows, which write the material itself, so the card says how many entities
share it. Each card offers "Make unique" while something else shares what it shows, and "Save as
asset" for a mesh or a material made here, which writes a `*.mesh.json` or `*.material.json` named
after the entity into the folder the browser shows. Every asset field picks from the asset
browser's grid, with a picture of everything offered ([ASSETS.md](ASSETS.md)), and a model's node
lists the material of each of its parts.

### The asset browser

The Assets tab shows the folder chosen in its tree as tiles: an image as itself, a model as a
thumbnail drawn once and kept under `user://thumbnails/` (`Thumbnails`), and anything else as its
kind's icon. A search box narrows the tiles by name, a chip narrows them to one kind, and a slider
sizes them. A model's thumbnail is drawn on a transparent background, so the tile's own color shows
round it, or on the color the setting "Thumbnail background" under Assets gives as hex with alpha.
Twice on a model's tile goes into it, as into a folder, where its meshes and materials are tiles of
their own, and ".." leads back out.

Picking a file selects it and lets go of any entity, since there is one selection, of entities or
of a file, and picking an entity lets go of the file. The details panel then shows the file: its
name and kind, a look at it by kind (a model turning, a mesh or material file under the card an
entity's mesh or material gets, an image fitted to the panel, a scene's count of entities), "Place
in the scene" for a model or a scene, and its path, size, last change and id.

### Instances

A model or scene tile's "Place in the scene" puts it in the scene being edited as an instance
([SCENES.md](SCENES.md), §5), named after its file, in front of the camera, and dragging the tile
onto the scene puts it on whatever is under the pointer, met by its triangles
(`Picking.TryCast`, `bcs_pick_ray`), or on the ground where nothing is. An edit in the details
panel or with the gizmo to one of its nodes is kept as an override, so the scene file holds the change rather than
a copy of the model, and the field's name turns the accent color. Right-clicking the name puts the
model's value back, as one step to undo. Setting a field back to the model's value, by hand or by
undoing, leaves no override behind. A component added or removed and a node deleted are kept the
same way. Renaming a node inside an instance is kept as an override too, and its path keeps the model's
name, so the overrides made on it still find it. An entity dragged under one of its nodes in the world list is the
scene's own and is saved with it, under that node.

### Moving files

A tile's right-click menu renames it in place or deletes it, after asking, and a tile dropped on a
folder, in the tree or among the tiles, moves into it. Each goes through `EditorAssets.Move` and
`EditorAssets.Delete`, which carry a file's `.uid` sidecar along or take it away with the file, so
a scene or a data asset referring to the file by id still finds it after a rename. A folder moves
whole, and a folder dropped into itself is refused before the drop. `assets.move` and
`assets.delete` do the same from the console and `./bcs`. None of it is a step in the history,
since it changes files rather than the scene.

### The scene view's camera

The camera button beside the statistics opens a card of the scene view's own camera, which is the
editor's rather than the world's and so is not listed in the world. Its lens (the field of view and
the near and far distances, through `Render.SetPerspective`), how it flies (its speed, how long it
eases up to speed and back to a stop, and how far the mouse turns it), and how it draws. A fly,
a pan or an orbit begins only when its button goes down over the scene, so a right click on a
panel opens that panel's flyout rather than turning the camera, and a drag begun over the scene
keeps going over a panel. The view
is the editor's plain one unless overridden, when it takes a tonemapper, bloom, an antialiasing
pass, and an exposure that follows the light or one of its own. Everything is saved with the
editor's preferences, on a page of its own in the settings, and Reset puts it back.

### Playing

The Play tab and `Project/Play project` run the game in a window of its own, as Godot does, and the
same places stop it. Play scene, `F5` and `Project/Play scene` play the scene being edited instead,
written as it is to a file of the editor's own and played by `BevyCSharp.Player` from where the
editor's camera is. The tab also builds the project without running it, and shows what the game or
the build wrote, which the console also shows marked `[game]` or `[build]`. It runs the sample
unless the tab's field names another project. It is a tab rather than a button on the scene's
toolbar, so the viewport holds the scene and the tools that act on it, and nothing about running the
project. The game is a process of its own, so one that crashes leaves the editor running. A second
row exports the project for a chosen platform, self-contained, with its assets as files, compiled
into its assembly or in a pack beside it, into the project's `bin/Export/<rid>/`, which
`project.export` does from the console too. While a game plays, the tab lists the running game's named entities and the fields of
the one picked, which can be changed for as long as it runs, with buttons that pause it and step it
a frame at a time ([PLAY.md](PLAY.md) §2).

### Statistics

The round button at the scene's top right opens a card of what the engine is doing, the one every
engine's viewport has. First comes a line of the last four seconds of frames, with a rule at a
sixtieth of a second and hitches past a thirtieth drawn in the warning color, and under it frames a
second, the frame at its best, typical and worst, and how long the app has run. Then the GPU (the
frame's render time, the time spent recording it, and the slowest passes), the world (entities,
named ones, ones in a hierarchy, the selection and the last undo step), memory (the process, the
managed heap, what is allocated a frame, collections and the last pause), and the renderer (the API,
the device, the window, vsync, the bridge's profile and the runtime). The keys come last. Everything
is read from what the engine already reports, and the card scrolls where the scene is too short for
it.

### Tabs

A tab lays its parts out on panes, rounded surfaces a step lighter than the tab with air of their
own (`EditorSurface.Pane`), as a component's rows lie on a group in the details. The Frame tab is
the images, the passes with a bar for each one's share of the slowest, and the picture; the
Shaders tab is the programs beside what the compiler said about the chosen one. An empty pane says
in its middle what is not there and, dimmer, how it comes to be (`EditorSurface.Empty`), since
an empty pane is usually one somebody has not yet learned how to fill.

### When there is not room

Nothing overlaps and nothing is cut square. The toolbars share the top line of the scene while they
fit, and when the scene is too narrow the tools drop to a line of their own under the menu and the
statistics button, and past that every group has a line of its own. A group wider than the scene
itself is clipped at the scene's edge and scrolled with the wheel, and the tabs along the bottom
scroll the same way, with no scrollbar. A button or tab half out of its row is drawn as a pill of
the part that shows, with its word running to the curve and no further. A group with no scene
left to lie on is not drawn, and every group is placed on whole pixels, so none shifts while the
window is resized. A list that runs past its edge has
that edge's corners rounded, and a component's group keeps its own rounded corners where it is cut.

The toolbars are measured from the same point docked and floating, the scene's corner while docked
and the same gap in from the window's while floating, so docking the panel moves no button.

### Folds

A field that asks for a fold (`[Foldout("Advanced/Debug")]`) is drawn under a header of the
editor's own, as wide as the fields beside it. Each open fold is a card a step darker than what it
lies on, and what it holds is set one step further in, so a field's depth and the surface it lies
on both say which fold it is in.

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
| a field's name and type | the generator for C#, Bevy's type registry for Bevy's | editing a value | `bcs_reflect_types` |
| where a panel ended up | ImGui's own layout | the viewport, the orientation cross | none needed |
| the entity under the cursor | `bevy_picking`, already compiled in | selection in the viewport | `bcs_pick_events` |
| what an entity fills | `Aabb` through the global transform | outlining and framing a selection | `bcs_render_bounds` |
| pausing | `Time<Virtual>` | play and pause | none yet |

The field table is the one that decides whether an inspector is possible at all, and it needed no
engine work. A `[Behavior]` struct is a C# type the generator already reads, so it emits the field
names, types and a pair of accessors beside the runner it emits today. That turns a component id,
which the bridge already hands over, into a list of editable fields, with no reflection crossing
the ABI.

Bevy's own components are described from Bevy's type registry instead, and their fields are read
and written as JSON by Bevy's reflect path, so a light or a camera is drawn with no code per type.
The few with a mirror on this side keep a schema written by hand, which writes the bytes in place.

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
number with two ends, three boxes across for a vector, a list to choose from for a choice (a field
whose bottom corners go square while its list is open, meeting a list whose top corners are square,
so the two read as one shape grown downward), a swatch for three numbers that are a color (whose
picker, `ColorPicker`, is laid out as WinUI's is, with a rounded spectrum, a hue bar and a
clearness bar as pills, the color as it was beside the color as it is, a hex box, and numbers in
RGB, HSV, HSL, OKLCH or OKLab, where the square and the bar change with the space, so OKLCH picks
chroma across and lightness down at a hue from an evenly bright hue strip, OKLab picks its two color
axes at a lightness, and colors a screen cannot show are dimmed past a soft edge), and a dimmed box for anything that cannot be edited. A heading, a rule, a sentence and
a unit are the field's own declaration too. A unit is written after the field's name, dimmed and
in parentheses, as `Speed (m/s)`, so every box in a column ends at the
same edge and the unit is read with the name it qualifies.

**Every edit is undoable, and a run of them is one edit.** A row compares what the field held
before its widget was drawn with what it holds after, and records the difference under the field's
own name, so a drag along a bar or a number typed one character at a time comes back as one
change. The same goes for what the panels do outside a field: a rename, a drag on the handles, an
entity taken out of its parent, something hidden with its eye, and a component put on or taken off.
Taking one off keeps what it held, so putting it back is the component again rather than a fresh
empty one in its place. A delete keeps what went as a scene in memory, so undoing it brings the
entities back with what was under them, what they were drawn with and where they stood, and points
the entity fields that named them at the ones that came back. A node of a placed model, and an
entity drawn with something a scene cannot describe, are deleted with a line in the console saying
there is no way back.

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
| `[Foldout("A/B")]` | puts the field in a fold, which opens and shuts; slashes nest them, and each level sets what it holds one step further in |
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
undoes as one change. What the details panel shows is the last one picked, with a line over it
saying how many are selected. The panels have no headings otherwise, since a list of names and a
name at the top of a card say what they are.

What an entity is drawn with is saved as well, by the path its mesh and material were loaded from,
or, for one made in memory, as how to make it again: a primitive as its shape and measures, a
standard material as its settings, a mesh built vertex by vertex as its geometry, and a material
from a material file by the file.

**The editor tells one entity from another by name.** Spawning something gives it a name nothing
else is called, because a selection that survives a script reload is found again by it, so a
second thing called Cube is a thing the editor confuses with the first. A scene file gives every
entity an id of its own, so names there are for people. Wherever names are listed they are ordered by `EditorSort`, which reads a
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

### Data assets

A data asset is edited in the details panel when its file is selected in the asset browser, as one
card of its fields under the file's name. The rows are the ones a component has,
so its attributes are honored the same way, and an edit is written to the file at the end of the
frame and is a step in the history like any other. `Project/New data asset` makes one of any
registered type in the directory being browsed, and a `DataRef` field picks from the files of its
type. Under a set `DataRef` row, a fold named for how many entities share the asset holds its
fields, and "Make unique" there copies it for the one entity being edited.

### What the inspector does not do

Written down because the attributes and the shape of the thing suggest otherwise:

- **A selection of several things shows the last one**, not what they agree and disagree about.
- **There is no pass system and no list drawer.** What an entity hangs from and what hangs from it
  are not shown as rows, and a component cannot hold a list to begin with, so nothing in the
  inspector draws one.
- **Nothing puts a field back to its default.** What a freshly added component holds is known to
  the schema, and no row asks it.
- **A duplicate copies the entity, not its children.** Duplicate (`Ctrl+D`, the world list's
  menu, `Entity/Duplicate`) is the engine's own clone, so the copy carries everything the original
  does, its mesh and material included, under the same parent and with the next free name ("Cube
  2"). The original's children stay with the original, which is Bevy's default for a relationship.

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

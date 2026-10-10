# The interface

Bevy's interface of nodes and text, laid out by flexbox and grid, and Dear ImGui for tools.

An interface is **Dear ImGui**, running in C# and drawn by Bevy. Immediate mode: a call per widget
per frame, no document to load, no binding to declare, and nothing to keep in step with the world.
It is part of the library rather than part of the editor, so a game gets one by referencing
`BevyCSharp` and nothing else.

<!-- compiled with:
using ImGuiNET;
-->
```csharp
[Behavior]
public partial struct Interface
{
    private static int _score;

    [OnStartup] public static void Open(BehaviorContext ctx) => ImGuiRuntime.Start();

    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        ImGuiRuntime.Begin(ctx);

        if (ImGui.Begin("Sample"))
        {
            ImGui.Text($"Score {_score}");
            if (ImGui.Button("Add one")) _score++;
        }

        ImGui.End();
        ImGuiRuntime.End();
    }
}
```

`BevyCSharp.FeatureTest` draws its admin panel, its overlay and its console that way, in
`Behaviors/Interface.cs`. It needs a bridge with the interface compiled in
(`build/build-native.sh --editor`) and `Config.Gui` asked for.

**A console is one call.** `ImGuiConsole.Draw` between `Begin` and `End` gives a game the editor's
console, opened and closed by the key under Escape along the top of the window. It shows the log,
Bevy's lines and the program's, filtered by level and searched, over a line that runs any
`[Command]` the game or the engine declares, with the arrows walking back through what was typed and
the tab key completing a name. It draws nothing while it is closed, so a game calls it every frame.

```csharp
ImGuiRuntime.Begin(ctx);
ImGuiConsole.Draw(ctx);
ImGuiRuntime.End();
```

**The engine only rasterizes.** ImGui hands over vertices, indices and a list of draw calls, each
with a clip rectangle and a picture; `bcs_imgui_frame` takes them and a pass in Bevy's renderer
draws them straight onto the window, over whatever the cameras drew. Nothing on the Rust side knows
what a widget is, which is why the interface can change completely without touching it.

**Pictures come from the asset server.** `ImGuiTextures.Load("icons/ui/camera.png")` answers a name
ImGui can put in a draw call, decoded by the engine like any other asset, and drawn with
`ImGui.Image` tinted to whatever it means.

**Input is fed, not polled.** `ImGuiRuntime.Begin` turns the engine's per-frame input into the
events ImGui expects, and `ImGuiRuntime.WantsMouse` stops the camera flying, or a click picking
something behind a panel, while the interface has the pointer. `SyntheticInput` writes into both
halves, ImGui's queue and the window's own messages, so a test selects a mesh and drags a handle the
way a hand does.

## UI

Panels and text, on a render build:

<!-- compiled with:
int score = 0;
-->
```csharp
var panel = Ui.SpawnNode(new UiSettings
{
    Absolute = true,
    Left = Length.Px(16f),
    Top = Length.Px(16f),
    Padding = Length.Px(10f),
    Color = (0f, 0f, 0f, 0.45f),
});

var label = Ui.SpawnText("Score: 0", new UiSettings { Color = (1f, 1f, 1f, 1f) }, 18f);
ctx.Ecs.SetParent(label, panel);

Ui.SetText(label, $"Score: {score}");
```

A length carries its unit, because a bare number cannot say whether it means pixels, a share of the
parent, or "work it out": `Length.Px`, `Length.Percent`, `Length.Auto`. `Absolute` pins a node to
its parent's edges rather than laying it out beside its siblings, as a HUD does.

A node stacks its children along one axis, which turns a pile of them into a screen:

```csharp
var menu = Ui.SpawnNode(new UiSettings
{
    Direction = UiDirection.Column,
    Align = UiAlign.Center,
    RowGap = Length.Px(12f),
    Padding = Length.Px(16f),
    Border = Length.Px(2f),
    BorderColor = (0.4f, 0.7f, 1f, 1f),
});
```

`Direction` is that axis, `Justify` spreads the children along it and `Align` places them across it.
A column centered with `Align` is a menu, a row spread with `UiJustify.SpaceBetween` is a toolbar.
`RowGap` and `ColumnGap` space the children apart from the parent's side, which is steadier than a
margin on each of them.

`Padding`, `Margin` and `Border` are four lengths each, and a single `Length` assigned to one of
them means the same distance on every side:

```csharp
var panel = new UiSettings
{
    Padding = Length.Px(16f),                                   // all four
    Border = Sides.Vertical(Length.Px(2f)),                     // a rule above and below
    Margin = new Sides(Length.Px(8f), Length.Zero, Length.Auto, Length.Zero),
};
```

A border draws only where `BorderColor` is not transparent. `Length.Auto` in a margin is not zero.
It swallows whatever room the parent has left over, which is how the third line above pushes a node
to the right without the parent arranging it.

Nodes are entities, so nesting is `SetParent` and removal is `Despawn`, and a node can carry your
own components like anything else. `SetText` rewrites in place rather than respawning, because a
score changes every frame and the entity behind it should not.

Text is set in the font Bevy compiles in, so words reach the screen with no asset loaded at all. A
game with a font of its own loads it like anything else:

```csharp
var font = AssetServer.Load(AssetKind.Font, "fonts/inter.ttf");

Ui.SpawnText("Score: 0", new UiSettings { Color = (1f, 1f, 1f, 1f) },
    new UiTextSettings { Font = font, FontSize = 18f });
```

TrueType and OpenType. A handle that names nothing is refused rather than falling back quietly,
because a game that ships a font and silently does not use it looks exactly like a font that failed
to load. Asking for a font by family name, the way a web page asks for `sans-serif`, is not offered,
because Bevy resolves those through `system_font_discovery`, which links against fontconfig on
Linux, and the bridge builds with nothing but a C compiler.

A label fits on one line; a paragraph has to be told how to break:

<!-- compiled with:
string paragraph = "";
-->
```csharp
Ui.SpawnText(paragraph, new UiSettings { Color = (1f, 1f, 1f, 1f) }, new UiTextSettings
{
    FontSize = 14f,
    Justify = TextJustify.Center,
    Wrap = TextWrap.WordBoundary,
});
```

The width it breaks against comes from the layout, so the text or something above it needs a
`Width` or a `MaxWidth`; a node free to grow sideways never wraps however `Wrap` is set.
`TextWrap.NoWrap` is the opposite choice, for a line that should run past the edge and be clipped
rather than folded. `Justify` aligns the lines against each other inside the text's own box, which
is a different question from where that box sits in its parent.

`UiSettings.Color` is the text's color rather than a background for a run of text, and it is
transparent by default like any other node, so a `SpawnText` that passes plain `new UiSettings()`
lays out correctly and draws nothing.

`LineHeight` sets the spacing between lines, as a multiple of the font size unless
`LineHeightInPixels` says otherwise, and `LetterSpacing` does the same between the letters, where a
negative value pulls them together and `LetterSpacingInPixels` switches the unit the same way.
`Smooth` turned off keeps a pixel font sharp, since smoothing a font drawn to land on whole pixels
makes it look blurred. `ShadowOffset` and `ShadowColor` put a shadow behind the glyphs, which keeps
light text readable over a picture that might be light too.

`Ui.SpawnTextSpan` adds a run to a text that already exists, in its own font, size and color, such
as a bold word inside a sentence. The spans read in the order they were added, after whatever the
parent itself says, and the whole is broken and aligned as one block by the settings the parent was
given. A span has no node of its own, so it takes its color as an argument where a whole text takes
the color of the node it sits in.

A run, whole or a span, is drawn with a line under it or through it by `Ui.SetUnderline` and
`Ui.SetStrikethrough`, in the text's color or the one an `UnderlineColorRef` or
`StrikethroughColorRef` on the run gives, and over a color of its own with `TextBackgroundColorRef`.
Its `TextFontRef` sets the weight, the width and the style a font is asked for, and a font's
OpenType features and a variable font's axes are each a four-letter tag and a value:

<!-- compiled with:
Entity heading = default, price = default, title = default;
-->
```csharp
Ui.SetUnderline(heading);
ctx.Ecs.Wrap<TextFontRef>(heading).Weight = 700;                // bold, where the font has it
Ui.SetFontFeatures(price, ("tnum", 1));                         // figures all one width
Ui.SetFontVariations(title, ("wght", 650f), ("wdth", 85f));     // a variable font's axes
```

A generic family such as sans-serif needs Bevy's system font discovery, which the bridge does not
compile in, so a run names its font as a loaded asset.

**Laying out on a grid.** Flexbox lays a run of children along one axis and takes the other from
what they are. A grid states both axes up front and drops the children into the cells, so a column
lines up with the column above it:

<!-- compiled with:
Entity panel = default;
-->
```csharp
UiGrid.Set(panel, new GridSettings
{
    Columns = [Track.Px(96f).Filling()],   // as many 96-pixel columns as there is room for
    AutoRows = [Track.Px(96f)],            // and a row of the same height whenever one is needed
});
```

A track is sized by `Track.Px`, `Track.Percent`, `Track.Fr` (a share of whatever is left after the
fixed tracks have taken theirs), `Track.Flex` (the same share, kept even where what the track holds
is wider), `Track.Auto`, `Track.MinContent` or `Track.MaxContent`. Equal columns, or a row meant to
fit the window, are `Flex`, since an `Fr` track grows to fit its contents and pushes the rest along.
`Repeated(n)` states the same track n times over, and `Filling()` states it as many times as the
grid has room for, as a gallery that reflows with its window does. `Rows` and `Columns` are the
tracks stated up front; `AutoRows` and `AutoColumns` are the ones made when an item lands past them,
cycled through as often as they are needed, so a list of unknown length states its columns and
leaves its rows to these.

`UiGrid.Place` puts one child somewhere in particular. Lines are counted from one, and a negative
counts back from the far edge, so a column of `-1` is the last one whatever the grid turned out to
be. A child with no placement goes wherever `Flow` reaches next.

Since the tracks are lists and the rest of the layout arrives as a flat struct, a grid is set on a
node that already exists rather than passed with one. `UiGrid.Set` also switches the node to
`UiDisplay.Grid`, so nothing has to say that twice.

`Corners` rounds the node, clockwise from the top left, and a single `Length` assigned to it is the
same radius on all four. The background, the border and anything the node clips follow the same
curve, and a percentage is read against the node's own size, so fifty percent everywhere is an
ellipse. `Sizing` decides what `Width` and the rest measure, and Bevy's default is the border box
rather than the web's content box, because a node told to be a hundred pixels wide and then given a
border is easier to place if it stays a hundred pixels wide.

The children answer back. `Grow` takes a share of whatever room the parent has left over, `Shrink`
gives up a share of the overflow, `Basis` is the size to start from, and `AlignSelf` overrides the
parent's alignment for one child. `MinWidth` and its three companions bound the result, `Wrap` runs
the children onto more lines, and `Display` takes a node out of the layout altogether:

```csharp
var filler = Ui.SpawnNode(new UiSettings { Grow = 1f, Basis = Length.Px(0f) });
var fixedWidth = Ui.SpawnNode(new UiSettings { Shrink = 0f, Width = Length.Px(64f) });

var menu = Ui.SpawnNode(new UiSettings { Display = UiDisplay.None });   // put away, not despawned
```

`UiDisplay.None` is not `Visibility.Hidden`. The first takes the node's space back and moves its
siblings up, and the second stops it drawing and leaves the hole. A screen that is toggled uses the
first, and a health bar that blinks the second.

`OverflowX` and `OverflowY` say what happens to contents past an edge: drawn anyway, clipped, or
clipped and scrollable. Bevy has no scrolling of its own, so a list is moved by reading the wheel
like any other input and calling `Ui.SetScroll(list, 0f, offset)`. `ClipBox` says where the
clipping falls, which for a list with a border is the difference between rows disappearing at the
border and disappearing inside it, and `ClipMargin` pushes that line out by a few pixels for a
shadow or a focus ring.

`AspectRatio` decides the side the layout was not told about, so a tile stays square while only its
width is being decided, and `AlignContent` spreads the lines a wrapped node produced the way
`Justify` spreads the children within one line.

`Camera` names which camera draws the screen, and carries to the node's children. Left alone, Bevy
picks whichever camera draws to the window, which suits a game; a run drawing into an image has
none, so a screen that should appear in an offscreen capture names the camera itself.

A node can hold a picture as well as a color:

```csharp
var icon = Ui.SpawnNode(new UiSettings { Width = Length.Px(32f), Height = Length.Px(32f) });
Ui.SetImage(icon, AssetServer.Load(AssetKind.Image, "ui/icon.png"));
```

`UiImageSettings` tints it, mirrors it, cuts one icon out of a sheet with `Rect` or by frame number
with `Atlas` and `Frame`, and chooses how it meets the node's size. The layout `Atlas` takes is the
one `Render2d.CreateAtlas` makes, so a sheet of icons serves the world and the interface without
being cut a second way. `UiImageMode.Sliced` is the one worth knowing. The image is cut into nine,
the corners keep their size and the middle stretches, so one small picture draws a panel at any
size. `SliceTiling` makes the edges or the middle repeat rather than stretch, as a patterned border
needs. `Auto` keeps the picture's own size, which a node with no width or height of its own then
takes.

A node can be asked to report the pointer, which makes it a button:

```csharp
var button = Ui.SpawnNode(new UiSettings
{
    Interactive = true,
    Width = Length.Px(140f),
    Height = Length.Px(40f),
    Color = (0.15f, 0.35f, 0.6f, 1f),
});

var state = Ui.InteractionOf(button);       // None, Hovered or Pressed
```

`Pressed` lasts from the frame the pointer goes down until it is released, so a click is the edge
into it, which is the previous answer kept in a behavior field and compared. A node is hovered while
the pointer is over it or over a node inside it, as Bevy's picking reads a hover, so a button stays
hovered with the pointer on its label. An interactive node captures the pointer, so nothing behind
it is hovered through it, and a plain node carries nothing to update, which is why it is not the
default. Asking a plain node is refused rather than answered `None`, since a button that quietly
never fires is the harder mistake to find.

A node becomes a text field the player types into with `Ui.SetEditableText`, Bevy's own editable
text with its cursor, selection, copy and paste:

```csharp
var name = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f), Padding = Sides.All(Length.Px(8f)) });
Ui.SetEditableText(name, new UiEditableTextSettings { MaxCharacters = 16, Allowed = "abcdefghijklmnopqrstuvwxyz" });
ctx.Ecs.Insert<AutoFocusRef>(name);                        // focused as it opens

string? typed = Ui.EditableTextOf(name);                   // what it holds
Ui.SetEditableValue(name, string.Empty);                    // cleared
```

Keys reach the field with the input focus, which a click on it gives it, Bevy's `AutoFocus` gives it
as it is spawned, and Tab moves through a `TabGroup` by each field's or button's `TabIndex`. The
focus itself is Bevy's `InputFocus` resource, `ctx.Ecs.Resource<InputFocusRef>()`, and a game moves
it with `Ui.Focus`, which Bevy records so the field that lost it and the one that gained it are
told, and asks where Tab would move it with `Ui.Navigate`. `Allowed` names the only characters a
field takes, where Bevy's filter is a function of the game's own, and the field's font, size and
wrapping are the node's `TextFont` and `TextLayout`. A field set again is changed where it stands,
its text replaced with the settings' text and the rest of them taken.

Each key reaches the focused entity as Bevy's `FocusedInput<KeyboardInput>`, which goes on up its
parents, so a game observes Enter in a field, or in any field of a row:

<!-- compiled with:
private static void Submit(EcsWorld ecs, Entity entity) { }
Entity row = default;
-->
```csharp
ctx.Ecs.Observe<FocusedInput<KeyboardInput>>(row, on =>
{
    if (on.Event.Input.State == ButtonState.Pressed && on.Event.Input.LogicalKey == LogicalKey.Enter)
        Submit(on.Ecs, on.Event.FocusedEntity);
});
if (Ui.Navigate(NavAction.Next) is { } next) Ui.Focus(next);      // on to the next field
```

Bevy hands keys out only where there is a primary window, and the bridge hands them out itself in a
run with none, so a test or `./bcs command input.key` types into an offscreen run's fields as a hand
would. Tab moves the focus there only through `Ui.Navigate`, since Bevy's own Tab is heard at the
window.

The arrows or a pad move the focus by direction where a node carries Bevy's
`AutoDirectionalNavigation`. `Navigation.Move` moves it to the nearest such node on the screen that
way, eight ways round, and a game calls it on whichever keys or buttons it chooses. An edge drawn
with `Navigation.AddEdge` goes before the search, so a row wraps to the next, a far node is joined
to the others, or a way is blocked with `Navigation.BlockEdge`:

<!-- compiled with:
Entity button = default, endOfRow = default, startOfNextRow = default;
Vec2 stick = default;
-->
```csharp
ctx.Ecs.Insert<AutoDirectionalNavigationRef>(button);
Navigation.AddEdge(endOfRow, startOfNextRow, CompassOctant.East, bothWays: true);
if (CompassOctants.Of(stick) is { } way) Navigation.Move(way);   // the focus moved, or null
```

Bevy's widgets are its own components, a slider, a checkbox, a radio group, a scrollbar, put on a
node through their wrappers. A widget reports a change rather than making it, and
`Ui.SelfUpdate` attaches Bevy's own listener that makes it, so the state is read back from the
widget's components:

```csharp
var volume = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f), Height = Length.Px(12f) });
ctx.Ecs.Insert<SliderRef>(volume);
var range = ctx.Ecs.Insert<SliderRangeRef>(volume);
(range.Start, range.End) = (0f, 100f);
ctx.Ecs.Insert<SliderValueRef>(volume).Value = 80f;
Ui.SelfUpdate(volume, UiWidgetKind.Slider);

float level = ctx.Ecs.Wrap<SliderValueRef>(volume).Value;   // as the player drags it
```

A game that decides a change itself, keeping the value somewhere of its own or refusing it,
observes what the widget reports instead. A button and a menu item report Bevy's `Activate`, a
slider a `ValueChange<float>`, a checkbox or a radio button a `ValueChange<bool>`, and a radio
group a `ValueChange<Entity>` naming the button chosen. A menu reports `MenuEvent`, asking to open
or close, which goes up from the item to the menu's owner, where the game spawns the menu's popup
or despawns it:

<!-- compiled with:
public sealed class Settings { public float Volume; }
private static void Save(EcsWorld ecs) { }
private static void ToggleMenu(EcsWorld ecs, Entity entity, MenuAction action) { }
Entity save = default, volume = default, owner = default;
var settings = new Settings();
-->
```csharp
ctx.Ecs.Observe<Activate>(save, on => Save(on.Ecs));
ctx.Ecs.Observe<ValueChange<float>>(volume, on => settings.Volume = on.Event.Value);
ctx.Ecs.Observe<MenuEvent>(owner, on => ToggleMenu(on.Ecs, on.Entity, on.Event.Action));
```

What a widget looks like is the game's. The thumb is a child node marked `SliderThumbRef` that the
game places from the value, and a scrollbar's `ScrollbarRef.Target` names the node it scrolls. Some
of a widget's components, as a slider's value, are immutable in Bevy, so writing one through its
wrapper inserts it again, which is how Bevy means them to change. A widget is worked by the pointer
through Bevy's picking, which an offscreen run points at through the image it draws into, so a test
or `./bcs command input.click` works a widget there as a hand would.

---

Before this, [Gizmos](gizmos.md).
Next, [Audio](audio.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#ui-user-interface). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#the-interface). The [guide's contents](../README.md#guide) list every page.

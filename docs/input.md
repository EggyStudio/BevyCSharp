# Input

The keyboard, the mouse, typed text, touches and gamepads.

## Text and touch

Keys tell you what the hardware did, and `Input.Text` tells you what the user meant. It holds this
frame's typed characters, after the keyboard layout and any dead keys have been applied, for a name
field:

```csharp
name += ctx.Input.Text;
if (ctx.Input.KeyPressed(Key.Backspace) && name.Length > 0)
    name = name[..^1];
```

Control characters are left out, because Backspace and Enter arrive as text on some platforms and
a field that inserted them would be wrong on all of them. Read those as keys, as above. `Text` is
empty on most frames and never null.

A `Key` is a place on the keyboard, the same whatever layout is set, which suits moving with WASD.
A `LogicalKey` names what that place means in the layout, the character it types or its name,
which suits a key named by what it types, as '?' for help or '+' for zoom, wherever the layout puts
it. Both are read through the same calls:

```csharp
if (ctx.Input.KeyPressed(LogicalKey.Character("?"))) ShowHelp();
if (ctx.Input.KeyPressed(LogicalKey.Enter) && ctx.Input.KeyDown(LogicalKey.Control)) Submit();
```

A named key is named as Bevy names it, and those without a property of their own are
`LogicalKey.Named("MediaPlayPause")`. A key pretended through `SyntheticInput` reads as a keyboard's
would, its name for one that types nothing, as Enter or Backspace, and what it typed for any other.

Japanese, Chinese and Korean are typed through the platform's input method, which composes a
candidate before it becomes text. A field turns it on while it has the focus, and shows what is
being composed until it is committed:

```csharp
Window.SetIme(true, caretX, caretY + lineHeight);   // the candidate list sits under the caret

foreach (var composing in ctx.Read<ImeComposing>()) preview = composing.Text;
foreach (var commit in ctx.Read<ImeCommit>()) { name += commit.Text; preview = ""; }
```

It is off unless asked for, since the keys it takes stop reaching the game. A test says what a real
input method would through `SyntheticInput.Compose` and `Commit`.

Touches arrive the same way, as this frame's list:

```csharp
foreach (var touch in ctx.Input.Touches)
    if (touch.Phase == TouchPhase.Started) Aim(touch.X, touch.Y);
```

A touch that ends is reported once, on the frame it ends, and is gone after that.

Gamepads are this frame's list too, each with its buttons as a key is read and its sticks and
triggers as numbers, in the order Bevy found them:

```csharp
if (ctx.Input.Gamepads is [var pad, ..])
{
    move += new Vec3(pad.LeftStick.X, 0f, -pad.LeftStick.Y);
    if (pad.Pressed(GamepadButton.South)) Jump();      // A, or Cross
    if (pad.Axis(GamepadAxis.RightTrigger) > 0.1f) Accelerate();
}

foreach (var joined in ctx.Read<GamepadConnected>()) AddPlayer(joined.Gamepad);
```

The face buttons are named by where they sit, `South` being A on one pad and Cross on another. A
stick at rest reads zero through Bevy's dead zones, and a trigger is both an axis and, down past
three quarters, a button. `pad.Rumble(strong, weak, seconds)` shakes one. Real pads are found by
gilrs in the render and editor profiles, and every profile reads a pad pretended by
`SyntheticInput.ConnectGamepad`, which is how a test or `./bcs command input.button 0 South 10`
presses a button on a machine with none attached.

## What a pointer does to an entity

Bevy's picking finds what is under the mouse or a finger, an interface node, a sprite or a mesh,
and says what the pointer does to it: comes over it, presses it, clicks it, drags it or drops
something on it. A game observes that at the entity, as Bevy's `Pointer<E>` events are observed:

```csharp
ctx.Ecs.Observe<Pointer<Click>>(button, on => Spawn(on.Ecs));
ctx.Ecs.Observe<Pointer<Over>>(card, on => Highlight(on.Ecs, on.Entity));
ctx.Ecs.Observe<Pointer<Drag>>(cube, on => Turn(on.Ecs, on.Entity, on.Event.Event.Delta));
ctx.Ecs.Observe<Pointer<DragDrop>>(bin, on => on.Ecs.Despawn(on.Event.Event.Dropped));
```

Each carries the entity, which pointer, where it is in the window and what it did, and most say
where the pointer met the entity, which for a mesh or a sprite is a point in the world. They go up
the entity's parents as Bevy's do, so a panel hears what happens to the text inside it, until an
observer calls `on.Propagate(false)`. There are seventeen, `Over`, `Out`, `Enter`, `Leave`, `Press`,
`Release`, `Click`, `Move`, `DragStart`, `Drag`, `DragEnd`, `DragEnter`, `DragOver`, `DragLeave`,
`DragDrop`, `Scroll` and `Cancel`, named as Bevy names them.

Nodes are found as in Bevy, and a sprite where it carries Bevy's `Pickable`, as Bevy's own
examples give theirs (`ecs.Insert<PickableRef>(sprite)`). Meshes are found where the app asks, with
`Config.MeshPicking`, since that casts a ray at every mesh as the pointer moves, and Bevy's own
programs add mesh picking only where they pick one. It all needs a bridge with the renderer. An
offscreen run has the pretend pointer of `SyntheticInput` put on the image it draws into, so a
test or `./bcs command input.click` picks there as a hand would in a window, and its wheel,
`SyntheticInput.Wheel` or `input.wheel`, scrolls whatever that pointer is over as `Pointer<Scroll>`.

A game can have pointers of its own, for an interface drawn into an image that a mesh in the scene
wears, where no mouse is. `Picking.SpawnPointer()` makes one, and `Picking.MovePointer`,
`PressPointer` and `ReleasePointer` put it on that image and press it there, so the interface's
nodes hear it as they would the mouse, its events saying which pointer it was. Where it goes on the
image is the game's to say, usually where a ray from the mouse meets the mesh, which
`Picking.TryCast` answers with the texture coordinate there:

```csharp
if (Render.TryRay(camera, x, y, out var origin, out var direction)
    && Picking.TryCast(origin, direction, out var hit, out _, out _, out var uv)
    && uv is { } at)
    Picking.MovePointer(screenPointer, screenImage, new Vec2(512f * at.X, 512f * at.Y));
```

---

Before this, [Physics](physics.md).
Next, [Running a game](running-a-game.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#input). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#input). The [guide's contents](../README.md#guide) list every page.

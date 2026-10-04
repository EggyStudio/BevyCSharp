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

---

Before this, [Physics](physics.md).
Next, [Running a game](running-a-game.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#input). The [guide's contents](../README.md#guide) list every page.

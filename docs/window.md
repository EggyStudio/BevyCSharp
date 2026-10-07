# Images and the window

Drawing into an image a game reads or shows elsewhere, and the window the game runs in.

### Drawing into an image

A camera can draw into a texture instead of into the window, for a portal, a security monitor, a
mirror or a second viewport:

<!-- compiled with:
Entity screen = default;
-->
```csharp
var target = Render.CreateTarget(512, 512);
var watcher = Render.SpawnCamera3d(new CameraSettings { Order = 1 });

ctx.Ecs.Add(watcher, Transform.LookingAt(new Vec3(0f, 6f, 0f), Vec3.Zero, Vec3.UnitZ));
Render.SetCameraTarget(watcher, target);

// The same handle, read as a texture, so the screen shows what that camera sees.
Render.SetMaterial(ctx.Ecs, screen, Render.CreateMaterial(new MaterialSettings
{
    BaseColorTexture = target,
}));
```

The image is empty until something draws into it, and the handle is usable on the frame it is
returned, because nothing loads. `Render.SetCameraTarget(camera, AssetHandle.None)` puts the camera
back on the window, and `Render.Screenshot(path, target)` writes out what it drew.

`Render.CreateTarget(512, 512, TargetFormat.Rgba16Float)` holds half floats instead, so a camera
with `Hdr` and no tonemapper draws light brighter than white into it as it is, which a reflection
or a shader reading the picture on needs. A capture of it reads as eight-bit sRGB clamped at white.

A target can have layers, and a camera can draw into one of them, which is how a cube map of the
game's own is captured:

<!-- compiled with:
Vec3 center = default;
(Vec3 Forward, Vec3 Up)[] Faces = new (Vec3 Forward, Vec3 Up)[6];
-->
```csharp
var cube = Render.CreateTarget(256, 256, TargetFormat.Rgba16Float, layers: 6);

for (var face = 0; face < 6; face++)
{
    var eye = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 90f });
    ctx.Ecs.Add(eye, Transform.LookingAt(center, center + Faces[face].Forward, Faces[face].Up));
    Render.SetCameraTarget(eye, cube, face);    // plus and minus X, Y and Z, in that order
}
```

Six square layers read as a cube, in a material's cube slot or a shader's `TextureCube`, and any
other count as an array. Each camera draws into an image of its own that is copied into its layer
once the cameras have drawn, since Bevy draws a camera into a whole image.

A picture can also come back into memory rather than into a file, so a test can assert on what was
drawn:

<!-- compiled with:
AssetHandle target = default;
-->
```csharp
var ticket = Render.BeginCapture(target);       // or BeginCapture() for what the run is drawing

// A frame or two later, because the picture has to come back off the GPU:
if (Render.TryReadCapture(ticket, out var picture))
{
    var (r, g, b, a) = picture.At(16, 8);       // four bytes a pixel, rows top to bottom
}
```

An image no camera draws into, such as one a compute shader wrote or a watch, is read back from
the GPU as it is, and turned into eight-bit color the same way where its format allows (a float
image does, an integer one does not, and says so in the log).

Eight-bit color is the picture as a person sees it, so a half-float target's light brighter than
white reads as white. `TryReadCaptureAsItIs` reads the same capture in the format it was drawn in,
for a program measuring light rather than looking at it:

<!-- compiled with:
Capture ticket = default;
-->
```csharp
if (Render.TryReadCaptureAsItIs(ticket, out var drawn))
{
    var light = drawn.ColorAt(16, 8);           // (3, 1.5, 0.25, 1) where the material said so
}
```

Its format is one a shader image is made in (`ShaderImageFormat`), and an eight-bit picture comes
back as `Rgba8`, red first, whatever order the GPU held it in. A capture is read once, either way.

`TryReadCapture` answers false while the picture is still on its way, hands it over once it has
arrived, and drops the engine's copy when it does. `Render.ReleaseCapture(ticket)` is for a caller
that stopped waiting. A capture taken in the first frames of a run is a picture of a cleared window,
because a material's pipeline is compiled the first time something asks to be drawn with it.

`Render.CreateImage` goes the other way, turning bytes into an asset with no file behind it, for a
texture worked out at startup or a capture handed on to a material. It takes the same RGBA layout a
capture comes back in, so a picture can be read, changed and given back. Pass `srgb: false` for a
picture whose numbers mean something other than a color, such as a normal map or a roughness mask.
Such an image takes Bevy's default sampler, clamped at its edges and filtered linearly, as a loaded
one does with no settings given, and `Render.SetSampler(image, TextureSettings.Tiling)` changes
that afterward, for a small pattern repeated across a floor. The settings are the ones `AssetServer.LoadImage` takes, and
an image still loading is refused, since there is nothing yet to give them to.

### The window

The window can be driven while the app runs:

```csharp
Window.SetTitle("Level 2");
Window.SetMode(WindowMode.BorderlessFullscreen);
Window.SetCursor(CursorGrab.Locked, visible: false);
Window.SetPosition(100, 100);
Window.SetStyle(decorations: false, resizable: false, alwaysOnTop: true);
var (width, height) = Window.Size();
```

`WindowMode.Fullscreen` takes the monitor exclusively at its current video mode, which can be worth
a frame of latency and makes alt-tabbing heavier; most desktop games use `BorderlessFullscreen`. A
first-person camera needs `CursorGrab.Locked`, since it reads how far the mouse moved rather than
where it is. Platforms differ in which grab they support, Windows confining and macOS locking and
each emulating the other, so hide the cursor while it is grabbed either way.

The monitors are readable, so a settings screen can offer a choice of them:

```csharp
for (var i = 0; i < Window.MonitorCount(); i++)
{
    var m = Window.Monitor(i);
    var name = Window.MonitorName(i);
    Console.WriteLine($"{(name.Length > 0 ? name : $"Display {i + 1}")}: {m.Width}x{m.Height} at {m.RefreshHz:F0} Hz");
}
```

A monitor's name is read separately from the rest of it, because it is text. Platforms name a
monitor nothing often enough that a settings screen needs the fallback shown above. A headless run
has no window, and every call here says so rather than doing nothing.

A window made without the platform's frame (`SetStyle(decorations: false)`) has no title bar to be
moved by and no border to be resized from, so an app drawing its own asks the platform to do both
on its behalf:

```csharp
Window.StartDragMove();                          // on the press over a title bar of its own
Window.StartDragResize(WindowEdge.BottomRight);  // on the press over an edge or corner
Window.Minimize();
Window.SetMaximized(true);
Window.SetCursorShape(CursorShape.ResizeFalling);
```

The two drags are called when the button goes down and last until it is let go, since the
platform moves the window rather than the app. On Wayland an app is never told where its window
is, so this is the only way a borderless window moves there at all. Bevy does not say whether a
window is maximized, so a button that toggles it keeps that itself. The ImGui runtime sets the
cursor shape as the pointer crosses a field or an edge, whenever the shape it asks for changes, and
the editor draws its own frame this way.

On GNOME under Wayland, winit draws a title bar of its own that imitates an older GNOME rather than
the libadwaita one the desktop's other windows have, because GNOME leaves drawing it to the app.
`Config.DesktopTitleBar` opens the window through XWayland there instead, where GNOME draws its
own. An XWayland window is softer on a display at a fractional scale, so it is off unless asked
for, and it does nothing on another desktop or platform.

<!-- compiled with:
Config config = new();
-->
```csharp
config.DesktopTitleBar = true;                    // before the app is built
```

A second window is Bevy's `Window` on an entity of its own, spawned through reflection as Bevy's
other components are, and Bevy opens it once it is. A camera aimed at it draws there rather than in
the first window, and an interface for it names that camera, since a node is drawn by one camera:

<!-- compiled with:
EcsWorld ecs = ctx.Ecs;
-->
```csharp
var second = ecs.Spawn();
var window = ecs.Insert<WindowRef>(second);
window.Title = "Second window";
window.ResolutionScaleFactorOverride = 2f;        // text there drawn twice as large

var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(6f, 0f, 0f), Vec3.Zero, Vec3.UnitY));
Render.SetCameraTarget(camera, second);
Ui.SpawnText("Second window", new UiSettings { Camera = camera });
```

What one window alone shows goes on a render layer its camera alone sees (`Render.SetLayers`), as
Bevy's `multi_window_text` keeps a line of text to each window. Closing the window despawns its
entity. The calls on `Window` above address the first window alone, and a further one is changed
through its `WindowRef`. An offscreen run opens no window, and draws each window the game spawns
into an image of its own, at the window's size and scale, so the game runs the same with no display
and `Render.Screenshot(path, second)` reads the picture each window would show.

`Window.MonitorModes` lists the resolutions and refresh rates a monitor can actually be driven at,
and `Window.SetVideoMode(monitor, mode)` takes the screen over at one of them. That is the case
`WindowMode.Fullscreen` does not cover, where a game runs at a resolution the desktop is not in. The
mode is named by its place in the list rather than by numbers, because a monitor can only be driven
at the modes it offers, and how many it offers depends on the platform as much as on the hardware.

---

Before this, [Ray tracing](ray-tracing.md).
Next, [2D](2d.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#window). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#the-window). The [guide's contents](../README.md#guide) list every page.

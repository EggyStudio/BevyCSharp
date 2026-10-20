BevyCSharp feature test
=======================

Every feature of the engine on one map, for trying a build on your own machine. Run
BevyCSharp.FeatureTest (BevyCSharp.FeatureTest.exe on Windows) from this folder. It needs a GPU with
Vulkan or DirectX 12, and on Linux glibc 2.35 or newer with the ALSA and udev libraries every
desktop has, and nothing else installed.

The map
-------

You start on the hub, a cube turning in the middle and a signpost to each zone. East is the course,
ramps, stairs, a beam, a tunnel, ice, a bounce pad, moving platforms and gaps, beside a playground
of crates, doors, a lift, a rope and a heightmap terrain. West is the render gallery, spheres from a
dielectric to a metal and from polished to rough, and a Cornell box. North is the light hall, a bay
for each way of lighting a scene. South is a meadow of grass and trees in the wind. North-east is
the scenes zone, where the panel's Scenes page fetches Intel Sponza and stands it there. A day
passes over all of it, with the weather on top.

Keys
----

  Mouse            look around (Tab locks the cursor to the window)
  W A S D          walk, or a pad's left stick
  Space            jump, and in creative mode a double tap flies
  Shift            sprint
  C                crouch
  F3 held with F4  step through walking, creative mode and spectator mode
  F5               step the view, from the eyes, from behind and from in front
  F11              fullscreen
  F1               the panel
  F3               the overlay, the frame time, where you stand and what the program holds
  Key under Esc    the console

In spectator mode the right button held looks and flies with W A S D, Q and E, the middle button
slides, the wheel moves along the view, Alt with the left button orbits and F frames the middle.

The panel
---------

F1 opens it, a list steered with the arrow keys or a pad. Its pages set the graphics, the effects,
the weather, the audio and the controls, turn on the debug draws, teleport you to each zone, fetch
and stand the scenes, spawn things and set the time of day. A setting marked "from the next start"
needs the program started again. Every setting is kept for the next run in feature-test.json, in
~/.local/share/BevyCSharp Feature Test on Linux and %APPDATA%\BevyCSharp Feature Test on Windows.
Deleting the file puts everything back as it came.

The console
-----------

The key under Escape opens it. Type "help" for the commands, Tab completes one, and the arrows step
through what you typed before. A few to start with:

  player                   where you stand and how you move
  mode creative            walking, creative or spectator
  day.hour 22              the hour of the day
  setting                  every setting's name, and "setting <name> <value>" changes one
  memory                   what the program holds

When something goes wrong
-------------------------

The program writes what it says to logs/latest.log in this folder, the last five runs kept beside
it. A crash is written to logs/crash-<time>.txt with the last lines of the log and what the machine
is, and the next run says where. Send those two files with a report.

The program stops itself, saying why on the console and in a crash file, if it ever holds more than
eight gigabytes of memory, or a quarter of the machine's where that is less, rather than take the
rest of the machine with it.

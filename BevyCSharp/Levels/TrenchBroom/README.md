# TrenchBroom for BevyCSharp

The game config and the entity classes that let TrenchBroom draw a BevyCSharp level, with the
prototype materials' pictures it shows. The format's base classes are the Levels module's
(`modules/Levels/TrenchBroom/Levels.fgd`), and the config names that file beside `BevyCSharp.fgd`,
the classes the engine adds. To install them, copy this folder with the module's `Levels.fgd` into
TrenchBroom's games folder, which its preferences name, as a folder called `BevyCSharp`, choose
BevyCSharp in its game list, and give it a game path holding a `textures` folder, this folder itself
or a game's `assets`. A new face takes a scale of a sixteenth, so a prototype's picture of 512
pixels spans a meter, 32 units, as the engine draws it, and the brush tool's 8, 16 and 32 unit grid
ends on the grid's quarter, half and meter lines. A level is saved in the Valve map format, which is
the only one offered, and `./bcs command level.load` builds it into a running app.

`textures/proto` holds a picture for each prototype material, written by
`LevelSpawner.WritePrototypePictures` or `./bcs command level.textures <folder>`, which a test holds
to what the engine draws. `BevyCSharp.fgd` names the classes the engine knows, and grows as the
engine learns more.

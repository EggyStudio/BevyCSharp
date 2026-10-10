# BevyCSharp templates

A template for a game on [BevyCSharp](https://github.com/EggyStudio/BevyCSharp), Bevy written in
C#.

```bash
dotnet new install BevyCSharp.Templates
dotnet new bevycsharp -o MyGame && cd MyGame
dotnet run
```

`dotnet new bevycsharp` makes the program the engine's README opens with: a window with a turning
cube, a behavior that turns it, and the one line that runs the app. It asks for the version of the
engine packed with this template, and `--package-folder <folder>` writes a `nuget.config` that takes
the engine from a folder of packages built from a checkout.

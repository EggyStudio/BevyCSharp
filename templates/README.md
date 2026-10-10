# BevyCSharp templates

Templates for a game on [BevyCSharp](https://github.com/EggyStudio/BevyCSharp), Bevy written in
C#.

```bash
dotnet new install BevyCSharp.Templates
dotnet new bevycsharp -o MyGame && cd MyGame
dotnet run
```

`dotnet new bevycsharp` makes the program the engine's README opens with: a window with a turning
cube, a behavior that turns it, and the one line that runs the app. `dotnet new bevycsharp-empty`
makes an empty window titled for the project, the first step of the guide's first game. Each asks
for the version of the engine packed with these templates, and `--package-folder <folder>` writes a
`nuget.config` that takes the engine from a folder of packages built from a checkout.

// Bevy's compute_shader_game_of_life example, examples/shader/compute_shader_game_of_life.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// Conway's game of life run by a compute shader, on a grid of 320 by 180 cells drawn four times
// over to fill the window. Two images take turns, one read as the state so far and the other
// written with the next, and the sprite shows each in turn.
internal static class ComputeShaderGameOfLife
{
    private const uint DisplayFactor = 4, Width = 1280 / DisplayFactor, Height = 720 / DisplayFactor, WorkgroupSize = 8;

    private static AssetHandle _a, _b;
    private static ShaderInstance _init, _aToB, _bToA;
    private static Entity _sprite;

    // Waiting for the programs, then seeding once, then a step a frame, each from the image the
    // last one wrote, as Bevy's render graph node runs it.
    private enum Stage { Loading, Init, Update }

    private static Stage _stage;
    private static int _index;
    private static bool _showingA;

    // The steps run, and whether they were said, which a capture's log carries beside its picture.
    private static int _steps;
    private static bool _said;

    public static void Configure(Config config) => (config.Width, config.Height) = (Width * DisplayFactor, Height * DisplayFactor);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_stage, _index, _showingA, _steps, _said) = (Stage.Loading, 1, true, 0, false);
            Render.SetClearColor((0f, 0f, 0f, 1f));

            _a = Shaders.CreateImage(Width, Height, ShaderImageFormat.Rgba32Float);
            _b = Shaders.CreateImage(Width, Height, ShaderImageFormat.Rgba32Float);

            _sprite = ecs.Spawn();
            ecs.Add(_sprite, new Transform(Vec3.Zero, Quat.Identity, new Vec3(DisplayFactor)));
            Render2d.SetSprite(ecs, _sprite, _a, new SpriteSettings { Size = (Width, Height) });
            Render2d.SpawnCamera2d();

            var alive = new Vector4(1f, 0f, 0f, 1f);
            ShaderInstance Instance(string entry, AssetHandle input, AssetHandle output) =>
                Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = new ShaderStage("shaders/game_of_life.slang", entry) }))
                    .SetTexture("input", input)
                    .SetTexture("output", output)
                    .Set("config.alive_color", alive);

            _init = Instance("init", _a, _b);
            _aToB = Instance("update", _a, _b);
            _bToA = Instance("update", _b, _a);
        }, "compute_shader_game_of_life.Setup");

        app.Update(ctx =>
        {
            // Bevy swaps the sprite's image every frame, so it shows the one written a frame ago
            // on one frame and the newest on the next.
            _showingA = !_showingA;
            Render2d.SetSprite(ctx.Ecs, _sprite, _showingA ? _a : _b, new SpriteSettings { Size = (Width, Height) });

            // Where the workflow runs, as a capture does, the steps run by two seconds of the game's
            // clock, the moment the capture is taken in, and the image shown, so its log says which
            // generation the picture holds.
            if (!_said && ctx.Time.ElapsedSeconds >= 2.0 && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            {
                _said = true;
                Console.WriteLine($"[compute_shader_game_of_life] {_steps} steps by two seconds, in frame {ctx.Time.FrameCount}, showing {(_showingA ? "a" : "b")}");
            }

            switch (_stage)
            {
                case Stage.Loading:
                    if (_init.Program.State == ShaderProgramState.Ready) _stage = Stage.Init;
                    break;

                case Stage.Init:
                    Shaders.Dispatch(_init, Width / WorkgroupSize, Height / WorkgroupSize);
                    if (_aToB.Program.State == ShaderProgramState.Ready) _stage = Stage.Update;
                    break;

                default:
                    Shaders.Dispatch(_index == 0 ? _aToB : _bToA, Width / WorkgroupSize, Height / WorkgroupSize);
                    _index = 1 - _index;
                    _steps++;
                    break;
            }
        }, "compute_shader_game_of_life.Step");
    }
}

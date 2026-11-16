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

    // The steps run. Where the workflow runs, as a capture does, they stop at a count, and the image
    // the last of them wrote is shown from then on, so a picture holds that generation whichever frame
    // the programs were ready in. The capture's frame is one of the set clock's, held while what the
    // example loads arrives, and the programs' compiles are not among what it holds for, so without
    // the stop a runner ready a frame sooner takes a step more by the same frame and shows the other
    // image.
    private const int CapturedSteps = 60;
    private static int _steps;

    public static void Configure(Config config) => (config.Width, config.Height) = (Width * DisplayFactor, Height * DisplayFactor);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_stage, _index, _showingA, _steps) = (Stage.Loading, 1, true, 0);
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
            var stopped = _steps == CapturedSteps && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

            // Bevy swaps the sprite's image every frame, so it shows the one written a frame ago
            // on one frame and the newest on the next. The swaps start once the programs are ready,
            // since both images are blank until then and a swap shows nothing, while a clock held
            // as they compile runs as many frames as the device takes, which would decide the image
            // shown at a given moment of the clock.
            if (_stage != Stage.Loading && !stopped)
            {
                _showingA = !_showingA;
                Render2d.SetSprite(ctx.Ecs, _sprite, _showingA ? _a : _b, new SpriteSettings { Size = (Width, Height) });
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
                    if (stopped) break;
                    var wrote = _index == 0 ? _b : _a;
                    Shaders.Dispatch(_index == 0 ? _aToB : _bToA, Width / WorkgroupSize, Height / WorkgroupSize);
                    _index = 1 - _index;
                    _steps++;

                    // The last of a capture's steps, its image shown from here on, which the log
                    // carries beside the picture.
                    if (_steps == CapturedSteps && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
                    {
                        _showingA = wrote == _a;
                        Render2d.SetSprite(ctx.Ecs, _sprite, wrote, new SpriteSettings { Size = (Width, Height) });
                        Console.WriteLine($"[compute_shader_game_of_life] {_steps} steps by frame {ctx.Time.FrameCount}, showing {(_showingA ? "a" : "b")} from then on");
                    }
                    break;
            }
        }, "compute_shader_game_of_life.Step");
    }
}

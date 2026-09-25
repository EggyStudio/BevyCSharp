using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Runs an offscreen app through a list of steps and keeps the pictures it was asked to take.
/// </summary>
/// <remarks>
/// <para>
/// A shader test is a sequence rather than one frame: wait for a compile, take a picture, edit a
/// file, wait for the edit to arrive, take another. Each step is checked once a frame and the next
/// starts when it says it is done, which is what lets a test wait on the thing it is about rather
/// than on a number of frames somebody guessed.
/// </para>
/// <para>
/// A step that throws ends the run and the exception is raised from <see cref="Go"/>, because an
/// exception inside a system is caught by the engine and would otherwise be a line on stderr. A
/// run that reaches its frame limit with steps left is a failure naming the step it was stuck on.
/// </para>
/// </remarks>
internal sealed class PictureRun
{
    private readonly List<(string Name, Func<World, bool> Done)> _steps = [];
    private readonly Dictionary<string, CapturedImage> _pictures = [];
    private readonly List<Exception> _failures = [];
    private int _next;

    /// <summary>Where the assets are.</summary>
    public string AssetRoot { get; init; } = EngineHarness.AssetDirectory;

    /// <summary>The picture's width.</summary>
    public uint Width { get; init; } = 96;

    /// <summary>The picture's height.</summary>
    public uint Height { get; init; } = 96;

    /// <summary>How many frames the steps have before the run is given up on.</summary>
    public uint Frames { get; init; } = 1200;

    /// <summary>What to build at startup.</summary>
    public Action<EcsWorld>? Scene { get; init; }

    /// <summary>
    /// What to do every frame, before the steps are looked at, which is how a test keeps something
    /// moving while it waits and takes pictures.
    /// </summary>
    public Action<World>? EachFrame { get; init; }

    /// <summary>A picture taken by <see cref="Capture"/>, by name.</summary>
    public CapturedImage Picture(string name) =>
        _pictures.TryGetValue(name, out var picture)
            ? picture
            : throw new InvalidOperationException($"No picture was taken called {name}.");

    /// <summary>Adds a step that is done when <paramref name="done"/> says so.</summary>
    public PictureRun Until(string name, Func<World, bool> done)
    {
        _steps.Add((name, done));
        return this;
    }

    /// <summary>Adds a step that does something once.</summary>
    public PictureRun Do(string name, Action<World> action) =>
        Until(name, world =>
        {
            action(world);
            return true;
        });

    /// <summary>Adds a step that waits a number of frames.</summary>
    public PictureRun Wait(uint frames)
    {
        ulong? until = null;

        return Until($"waiting {frames} frames", world =>
        {
            var now = world.Resource<Time>().FrameCount;
            until ??= now + frames;
            return now >= until;
        });
    }

    /// <summary>Adds a step that takes a picture and waits for it to come back.</summary>
    public PictureRun Capture(string name)
    {
        Bevy.Capture? ticket = null;

        return Until($"capturing {name}", _ =>
        {
            ticket ??= Render.BeginCapture();

            if (!Render.TryReadCapture(ticket.Value, out var picture) || picture is null)
                return false;

            _pictures[name] = picture;
            return true;
        });
    }

    /// <summary>Runs the app until the steps are done, and raises whatever went wrong.</summary>
    public void Go()
    {
        var config = Config.OffscreenFor(Width, Height, frames: Frames);
        config.AssetRoot = AssetRoot;

        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world => Guard(() => Scene?.Invoke(world.Resource<EcsWorld>())),
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world => Guard(() =>
            {
                EachFrame?.Invoke(world);
                while (_next < _steps.Count && _steps[_next].Done(world)) _next++;
                if (_next == _steps.Count) App.RequestExit();
            }),
            "Test.Steps"));

        var code = app.Run();

        if (_failures.Count == 1) throw _failures[0];
        if (_failures.Count > 1) throw new AggregateException(_failures);

        Assert.True(
            _next == _steps.Count,
            $"the run ended at frame {Frames} still {_steps[Math.Min(_next, _steps.Count - 1)].Name}");

        Assert.Equal(0, code);
    }

    private void Guard(Action body)
    {
        if (_failures.Count > 0) return;

        try
        {
            body();
        }
        catch (Exception exception)
        {
            _failures.Add(exception);
            App.RequestExit();
        }
    }

    // -- Scenes the shader tests share

    /// <summary>A camera six units back from the origin, looking at it, over black.</summary>
    public static Entity Camera(EcsWorld ecs, Vec3? eye = null)
    {
        var camera = Render.SpawnCamera3d(new CameraSettings
        {
            Clear = ClearMode.Custom,
            ClearColor = (0f, 0f, 0f, 1f),
        });

        ecs.Add(camera, Transform.LookingAt(eye ?? new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
        return camera;
    }

    /// <summary>A cube of the given size with the given material, at a place.</summary>
    public static Entity Cube(EcsWorld ecs, AssetHandle material, float size = 2f, Vec3? at = null)
    {
        var cube = ecs.Spawn();

        // A cube rather than a plane, so no rotation has to be right for a test to be about the
        // material.
        Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, size, size, size));
        Render.SetMaterial(ecs, cube, material);

        var place = at ?? Vec3.Zero;
        ecs.Add(cube, Transform.At(place.X, place.Y, place.Z));
        return cube;
    }

    /// <summary>How many pixels are mostly green.</summary>
    public static int Green(CapturedImage picture) =>
        Count(picture, (r, g, b) => g > 120 && r < 90 && b < 90);

    /// <summary>How many pixels are mostly red.</summary>
    public static int Red(CapturedImage picture) =>
        Count(picture, (r, g, b) => r > 120 && g < 90 && b < 90);

    /// <summary>How many pixels are magenta, which is what a shader that never compiled draws.</summary>
    public static int Magenta(CapturedImage picture) =>
        Count(picture, (r, g, b) => r > 120 && b > 120 && g < 90);

    /// <summary>How many pixels pass a test of their color.</summary>
    public static int Count(CapturedImage picture, Func<byte, byte, byte, bool> test)
    {
        var count = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (test(picture.Pixels[i], picture.Pixels[i + 1], picture.Pixels[i + 2])) count++;
        }

        return count;
    }

    /// <summary>A fresh directory for assets a test writes, removed by disposing the result.</summary>
    public static TemporaryAssets Temporary() => new();

    /// <summary>A directory of assets a test writes and edits.</summary>
    internal sealed class TemporaryAssets : IDisposable
    {
        /// <summary>The directory.</summary>
        public string Root { get; } = Path.Combine(
            Path.GetTempPath(),
            "bcs-shader-test-" + Guid.NewGuid().ToString("N"));

        public TemporaryAssets() => Directory.CreateDirectory(Root);

        /// <summary>Writes a file under the directory, making its parents.</summary>
        /// <remarks>
        /// Written beside and moved into place, so a poll that lands mid-write never compiles half
        /// a file, which is also what most editors do on save.
        /// </remarks>
        public void Write(string relative, string text)
        {
            var path = Path.Combine(Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var staging = path + ".writing";
            File.WriteAllText(staging, text);
            File.Move(staging, path, overwrite: true);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
                // Something still has a file open, which on Windows stops the delete. The
                // directory is under the temporary path, so leaving it costs nothing.
            }
        }
    }
}

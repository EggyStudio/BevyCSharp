using System.Runtime.InteropServices;
using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// A swarm of fireflies simulated by a compute shader and drawn from the same buffer, and an old
/// screen's look over the whole picture on F4.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is a shader file under <c>assets/shaders</c>, and every one of them reloads
/// when it is saved while the sample runs, so a change to the path in <c>fireflies_step.wgsl</c>
/// changes how the swarm flies, and a change to the color in <c>fireflies.wgsl</c> how it glows.
/// The same files written in Slang, with <c>import bcs_compute;</c> and <c>import bcs;</c>, work
/// the same way on a machine with <c>slangc</c>.
/// </para>
/// <para>
/// The positions never leave the GPU. The compute shader writes them into a buffer every frame, the
/// material reads the same buffer, and the mesh is only a square per firefly with its corners at
/// the origin, so where each square is drawn comes from the buffer alone.
/// </para>
/// </remarks>
[Behavior]
public partial struct ShaderShowcase
{
    /// <summary>How many fireflies there are.</summary>
    private const int Count = 768;

    /// <summary>How many a workgroup moves, which is what the compute shader says it is.</summary>
    private const int PerWorkgroup = 64;

    private static ShaderProgram _step;
    private static AssetHandle _flies;
    private static Entity _camera;
    private static bool _screen;

    /// <summary>What the compute shader and the material call a firefly, laid out as WGSL lays it.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Firefly(Vec3 Position, float Phase, Vec3 Home, float Speed);

    /// <summary>Builds the swarm and the programs that move and draw it.</summary>
    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var random = new Random(7);
        var flies = new Firefly[Count];

        for (var i = 0; i < Count; i++)
        {
            // Around the lamp and the cube, above the ground.
            var home = new Vec3(
                (random.NextSingle() * 10f) - 5f,
                (random.NextSingle() * 2.5f) - 0.6f,
                (random.NextSingle() * 10f) - 5f);

            flies[i] = new Firefly(home, random.NextSingle() * MathF.Tau, home, 0.3f + random.NextSingle());
        }

        _flies = Shaders.CreateBuffer<Firefly>(flies);
        _step = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/fireflies_step.wgsl" });

        var swarm = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(swarm, "Fireflies");
        Render.SetMesh(ctx.Ecs, swarm, Render.CreateMesh(Squares(Count)));
        Render.SetMaterial(ctx.Ecs, swarm, Shaders.CreateMaterial(new ShaderMaterialSettings
        {
            Program = Shaders.CreateProgram(new ShaderProgramSettings
            {
                Vertex = "shaders/fireflies.wgsl",
                Fragment = "shaders/fireflies.wgsl",
            }),
            Buffer = _flies,
            Alpha = AlphaMode.Add,
            Cull = CullMode.None,
        }));

        // The squares are drawn wherever the buffer says, so the mesh's own bounds, which are a
        // single square at the origin, say nothing about where they are, and culling by them
        // would lose the whole swarm whenever the origin left the view. Glowing points cast no
        // shadow either.
        ctx.Ecs.Add(swarm, Transform.Identity);
        Render.SetMeshFlags(ctx.Ecs, swarm, MeshFlags.NoFrustumCulling | MeshFlags.NoShadowCasting);

        foreach (var row in ctx.Ecs.Query<FlyCamera>(markChanged: false))
        {
            _camera = row.Entity;
            break;
        }

        Console.WriteLine($"[Shaders] {Count} fireflies moved by a compute shader; F4 toggles the old screen");
    }

    /// <summary>Moves the swarm, and puts the old screen on and off.</summary>
    [OnUpdate]
    public static void Step(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || !_step.IsValid) return;

        Shaders.Dispatch(new DispatchSettings
        {
            Program = _step,
            X = (Count + PerWorkgroup - 1) / PerWorkgroup,
            Parameters = [0.9f],
            Buffers = { [0] = _flies },
        });

        if (!ctx.Input.KeyPressed(Key.F4) || _camera == Entity.None) return;

        _screen = !_screen;

        if (_screen)
        {
            Shaders.SetPasses(_camera, new ShaderPassSettings
            {
                Program = Shaders.CreateProgram("shaders/crt.wgsl"),
                Parameters = [1f],
                AfterTonemapping = true,
            });
        }
        else
        {
            Shaders.SetPasses(_camera);
        }
    }

    /// <summary>One square per firefly, each with its corners around the origin.</summary>
    private static MeshData Squares(int count)
    {
        var corners = new Vec3[count * 4];
        var uvs = new float[count * 8];
        var indices = new uint[count * 6];

        Vec3[] square = [new(-1f, -1f, 0f), new(1f, -1f, 0f), new(1f, 1f, 0f), new(-1f, 1f, 0f)];
        float[] squareUvs = [0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f];

        for (var i = 0; i < count; i++)
        {
            square.CopyTo(corners, i * 4);
            squareUvs.CopyTo(uvs, i * 8);

            var first = (uint)(i * 4);
            uint[] two = [first, first + 1, first + 2, first, first + 2, first + 3];
            two.CopyTo(indices, i * 6);
        }

        return new MeshData { Positions = corners, Uvs = uvs, Indices = indices };
    }
}

using System.Runtime.InteropServices;
using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// A swarm of fireflies simulated by a compute shader and drawn from the same buffer, and an old
/// screen's look over the whole picture on F4.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is a Slang file under <c>assets/shaders</c>, and every one of them reloads when
/// it is saved while the sample runs, so a change to the path in <c>fireflies_step.slang</c>
/// changes how the swarm flies, and a change to <c>fireflies.slang</c> how it glows. What each
/// shader declares is set from here by name: the glow's color and size, how far a firefly strays,
/// and how strong the old screen is.
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

    /// <summary>How many a workgroup moves, as the compute shader declares.</summary>
    private const int PerWorkgroup = 64;

    private static ShaderInstance _step;
    private static ShaderInstance _screen;
    private static Entity _camera;
    private static bool _screenOn;

    /// <summary>What the compute shader and the material call a firefly, laid out as Slang lays it.</summary>
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

        var buffer = Shaders.CreateBuffer<Firefly>(flies);

        _step = Shaders.CreateInstance(Shaders.CreateProgram(
                new ShaderProgramSettings { Compute = "shaders/fireflies_step.slang" }))
            .SetBuffer("flies", buffer)
            .Set("reach", 0.9f);

        _screen = Shaders.CreateInstance(Shaders.CreateProgram(
                new ShaderProgramSettings { Pass = "shaders/crt.slang" }))
            .Set("strength", 1f);

        var glow = Shaders.CreateMaterial(new ShaderMaterialSettings
            {
                Program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = "shaders/fireflies.slang",
                    Fragment = "shaders/fireflies.slang",
                }),
                Alpha = AlphaMode.Add,
                Cull = CullMode.None,
            })
            .SetBuffer("flies", buffer)
            .Set("glow", new System.Numerics.Vector4(1f, 0.75f, 0.25f, 12f))
            .Set("size", 0.05f)
            .Set("flicker_rate", 7f);

        var swarm = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(swarm, "Fireflies");
        Render.SetMesh(ctx.Ecs, swarm, Render.CreateMesh(Squares(Count)));
        Render.SetMaterial(ctx.Ecs, swarm, glow);

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

        Shaders.Dispatch(_step, (Count + PerWorkgroup - 1) / PerWorkgroup);

        if (!ctx.Input.KeyPressed(Key.F4) || _camera == Entity.None) return;

        _screenOn = !_screenOn;

        if (_screenOn)
        {
            Shaders.SetPasses(_camera, new ShaderPass(_screen, AfterTonemapping: true));
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

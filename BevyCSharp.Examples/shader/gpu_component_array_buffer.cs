// Bevy's gpu_component_array_buffer example, examples/shader/gpu_component_array_buffer.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// Small cubes spawned every 0.3 seconds and despawned one a second, each in a random color its
// component holds, which the engine keeps in one buffer for the shader and the cube's mesh tag
// finds, the tags moving as cubes go. Two materials share the buffer, each with a texture of its
// own. Bevy places its cubes by ChaCha8 from its seed and here .NET's random numbers do from the
// same one, so they fall in other places.
internal static class GpuComponentArrayBuffer
{
    private static ComponentArray<CustomMaterialData> _array = null!;
    private static Random _random = new(12345);
    private static AssetHandle _mesh;
    private static ShaderMaterial _light;
    private static ShaderMaterial _dark;
    private static GameTimer _add;
    private static GameTimer _remove;

    public static void Build(App app)
    {
        _array = app.AddComponentArray<CustomMaterialData>();
        app.Startup(Setup, "gpu_component_array_buffer.Setup");
        app.Update(AddCube, "gpu_component_array_buffer.AddCube");
        app.Update(RemoveCube, "gpu_component_array_buffer.RemoveCube");
    }

    private static void Setup(BehaviorContext ctx)
    {
        _random = new Random(12345);
        (_add, _remove) = (GameTimer.FromSeconds(0.3f, TimerMode.Repeating), GameTimer.FromSeconds(1f, TimerMode.Repeating));
        _mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);

        var program = Shaders.CreateProgram("shaders/gpu_component_array_buffer.slang");
        ShaderMaterial Material(string texture) => Shaders.CreateMaterial(program)
            .SetBuffer("material_data", _array.Buffer)
            .SetTexture("color_texture", AssetServer.Load(AssetKind.Image, texture));
        (_light, _dark) = (Material("branding/icon.png"), Material("branding/bevy_bird_dark.png"));

        ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 1.25f, 2.5f), Vec3.Zero, Vec3.UnitY));
    }

    // A cube every 0.3 seconds, somewhere on the ground, in a random color and either material.
    private static void AddCube(BehaviorContext ctx)
    {
        _add = _add.Tick(ctx.Time.Delta);
        if (!_add.JustFinished) return;

        var (x, z) = (Between(-1f, 1f), Between(-1f, 1f));
        var color = new Vec3(Between(0f, 1f), Between(0f, 1f), Between(0f, 1f));
        var material = _random.Next(2) == 0 ? _light : _dark;

        var cube = ctx.Ecs.SpawnMesh(_mesh, material, Transform.At(x, 0.5f, z) with { Scale = new Vec3(0.1f) });
        ctx.Ecs.Add(cube, new CustomMaterialData { Color = color });
    }

    // A cube a second, any of them.
    private static void RemoveCube(BehaviorContext ctx)
    {
        _remove = _remove.Tick(ctx.Time.Delta);
        if (!_remove.JustFinished) return;

        var cubes = ctx.Ecs.EntitiesWith<CustomMaterialData>();
        if (cubes.Length > 0) ctx.Ecs.Despawn(cubes[_random.Next(cubes.Length)]);
    }

    private static float Between(float low, float high) => low + (float)_random.NextDouble() * (high - low);
}

/// <summary>
/// A cube's tint, which the engine keeps in a buffer for the shader, Bevy's example's
/// <c>CustomMaterialData</c> and <c>GpuCustomMaterialData</c> in one, since it crosses as it is.
/// </summary>
[Behavior]
public partial struct CustomMaterialData
{
    /// <summary>What the texture is multiplied by.</summary>
    public Vec3 Color;

    /// <summary>Four bytes more, since a structured buffer gives a <c>float3</c> sixteen.</summary>
    public uint Pad;
}

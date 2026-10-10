using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <see cref="ComponentArray{T}"/>, every entity's component kept in one buffer that a
/// shader reads at the entity's mesh tag.
/// </summary>
[Collection("engine")]
public sealed class ComponentArrayTests
{
    /// <summary>
    /// Two squares sharing one material are each drawn in the color their own component holds, a
    /// changed component draws its square again in the new color, and when the first square is
    /// despawned the second moves into its place, its tag following, and keeps its color.
    /// </summary>
    [SkippableFact]
    public void EachEntityIsDrawnWithItsOwnEntryAsEntriesMove()
    {
        Needs.Shaders();

        ComponentArray<ArrayTint> tints = null!;
        Entity left = Entity.None, right = Entity.None;
        var places = new List<(int Left, int Right, uint RightTag)>();

        var run = new PictureRun
        {
            Build = app => tints = app.AddComponentArray<ArrayTint>(),
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/component_tint.slang" });
                var material = Shaders.CreateMaterial2d(program).SetBuffer("tints", tints.Buffer);
                left = Square(ecs, material, -24f, new ArrayTint { R = 1f, A = 1f });
                right = Square(ecs, material, 24f, new ArrayTint { G = 1f, A = 1f });
            },
        };

        void Note(World world) =>
            places.Add((tints.PlaceOf(left), tints.PlaceOf(right), world.Resource<EcsWorld>().Wrap<MeshTagRef>(right).Value));

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Capture("both")
            .Do("noting the places", Note)
            .Do("turning the left one blue", world => world.Resource<EcsWorld>().Set(left, new ArrayTint { B = 1f, A = 1f }))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("changed")
            .Do("despawning the left one", world => world.Resource<EcsWorld>().Despawn(left))
            .Wait(ShaderMaterialTests.Settled)
            .Capture("moved")
            .Do("noting the places again", Note)
            .Go();

        var both = run.Picture("both");
        Assert.True(both.At(24, 48) is { R: > 240, G: < 10 }, $"the left square was {both.At(24, 48)}");
        Assert.True(both.At(72, 48) is { G: > 240, R: < 10 }, $"the right square was {both.At(72, 48)}");
        Assert.True(run.Picture("changed").At(24, 48) is { B: > 240, R: < 10 }, $"the changed square was {run.Picture("changed").At(24, 48)}");

        var moved = run.Picture("moved");
        Assert.True(moved.At(24, 48) is { G: < 90, R: < 90, B: < 90 }, $"where the left square was is {moved.At(24, 48)}");
        Assert.True(moved.At(72, 48) is { G: > 240, R: < 10 }, $"the right square after the move was {moved.At(72, 48)}");

        Assert.Equal((0, 1, 1u), places[0]);
        Assert.Equal((-1, 0, 0u), places[1]);
    }

    // A square forty pixels across, `x` pixels right of the middle, with its tint.
    private static Entity Square(EcsWorld ecs, ShaderMaterial material, float x, ArrayTint tint)
    {
        var square = ecs.Spawn();
        ecs.Add(square, Transform.At(x, 0f, 0f) with { Scale = new Vec3(40f) });
        ecs.Add(square, tint);
        Render2d.SetMesh(ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
        Render2d.SetMaterial(ecs, square, material);
        return square;
    }
}

/// <summary>A color a component array keeps, laid out as the shader's <c>float4</c>.</summary>
[Behavior]
public partial struct ArrayTint
{
    public float R;
    public float G;
    public float B;
    public float A;
}

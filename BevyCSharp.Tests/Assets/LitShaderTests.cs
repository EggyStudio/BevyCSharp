using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a Slang material lit by Bevy's lighting through <c>bcs::lit</c>, which a standard material
/// extended by a shader of its own is drawn with in Bevy.
/// </summary>
[Collection("engine")]
public sealed class LitShaderTests
{
    // A red surface lit as Bevy's standard material lights one.
    private const string Lit = """
        import bcs;

        uniform float4 color;

        [shader("fragment")]
        float4 fragment(bcs::VertexOutput mesh) : SV_Target
        {
            var surface = bcs::surface(mesh);
            surface.base_color = color;
            return bcs::lit(surface, mesh);
        }
        """;

    /// <summary>
    /// A sphere drawn by a Slang shader that calls <c>bcs::lit</c> is bright where the sun falls on
    /// it and dark where it does not, and comes out as the same sphere drawn with Bevy's standard
    /// material in the same color beside it.
    /// </summary>
    [SkippableFact]
    public void ALitShaderIsLitAsTheStandardMaterialIs()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                // Orthographic, so the two spheres are seen alike, highlights and all.
                ShaderMaterialTests.Row(ecs);
                Render.SetAmbientLight((1f, 1f, 1f), 0f);

                // From the left, so each sphere is lit on its left and dark on its right.
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 5000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-1f, 0f, 0f), Vec3.Zero, Vec3.UnitY));

                var sphere = Render.CreateMesh(MeshShape.Sphere, 0.8f);
                Place(ecs, sphere, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.8f, 0.1f, 0.1f, 1f) }), ShaderMaterialTests.InRow(1));
                var program = Shaders.CreateProgram(ShaderStage.Slang(Lit));
                Place(ecs, sphere, Shaders.CreateMaterial(program).Set("color", new System.Numerics.Vector4(0.8f, 0.1f, 0.1f, 1f)), ShaderMaterialTests.InRow(4));
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        // Each sphere's middle is at the middle of its sixth of the picture, and each side of one
        // is fifteen pixels from its middle, a little over half its radius.
        var picture = run.Picture("picture");
        var (standardLit, standardDark) = (picture.At(96 - 15, 32), picture.At(96 + 15, 32));
        var (slangLit, slangDark) = (picture.At(288 - 15, 32), picture.At(288 + 15, 32));

        var (lit, dark) = slangLit.R >= slangDark.R ? (slangLit, slangDark) : (slangDark, slangLit);
        Assert.True(lit.R > 60 && lit.R > dark.R + 40, $"the lit shader's sphere was {slangLit} on one side and {slangDark} on the other");
        Assert.True(Math.Abs(slangLit.R - standardLit.R) <= 12 && Math.Abs(slangDark.R - standardDark.R) <= 12,
            $"the lit shader's sphere was {slangLit} and {slangDark} where the standard material's was {standardLit} and {standardDark}");
    }

    private static void Place(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Vec3 at)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.At(at.X, at.Y, at.Z));
        Render.SetMesh(ecs, entity, mesh);
        Render.SetMaterial(ecs, entity, material);
    }
}

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

    // Green under no decal, and under one red for the tag 1 and blue for any other, unlit.
    private const string Tagged = """
        import bcs;

        [shader("fragment")]
        float4 fragment(bcs::VertexOutput mesh) : SV_Target
        {
            if (bcs::decal_count(mesh) == 0) return float4(0.0, 0.5, 0.0, 1.0);
            return bcs::decal_tag(mesh, 0) == 1 ? float4(0.5, 0.0, 0.0, 1.0) : float4(0.0, 0.0, 0.5, 1.0);
        }
        """;

    // A surface with the decals over it laid on before it is lit.
    private const string Decaled = """
        import bcs;

        uniform float4 color;

        [shader("fragment")]
        float4 fragment(bcs::VertexOutput mesh) : SV_Target
        {
            var surface = bcs::surface(mesh);
            surface.base_color = color;
            return bcs::lit(bcs::decals(surface, mesh), mesh);
        }
        """;

    /// <summary>
    /// A shader reads the tag of the clustered decal over each point, red under the decal tagged 1,
    /// blue under the one tagged 2 and green under none, as Bevy's clustered_decals example tints
    /// its decals by their tags.
    /// </summary>
    [SkippableFact]
    public void AShaderReadsTheTagOfTheDecalOverIt()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                ShaderMaterialTests.Row(ecs);
                var cube = Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f);
                var white = Solid(255, 255, 255, 255);

                // A black cube of Bevy's standard material, which shows the decal over it where the
                // device can have clustered decals at all.
                Place(ecs, cube, Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Unlit = true }), ShaderMaterialTests.InRow(0));
                Decal(ecs, ShaderMaterialTests.InRow(0), white, tag: 0);

                var tagged = Shaders.CreateMaterial(Shaders.CreateProgram(ShaderStage.Slang(Tagged)));
                foreach (var index in new[] { 1, 3, 5 }) Place(ecs, cube, tagged, ShaderMaterialTests.InRow(index));
                Decal(ecs, ShaderMaterialTests.InRow(1), white, tag: 1);
                Decal(ecs, ShaderMaterialTests.InRow(3), white, tag: 2);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var standard = picture.At(32, 32);
        Skip.If(standard.R < 100, $"this device draws no clustered decals, the standard material's cube under one being {standard}");

        var (one, two, none) = (picture.At(96, 32), picture.At(224, 32), picture.At(352, 32));
        Assert.True(one.R > 60 && one.G < 20 && one.B < 20, $"under the decal tagged 1 the cube was {one}, where red is right");
        Assert.True(two.B > 60 && two.R < 20 && two.G < 20, $"under the decal tagged 2 the cube was {two}, where blue is right");
        Assert.True(none.G > 60 && none.R < 20 && none.B < 20, $"under no decal the cube was {none}, where green is right");
    }

    /// <summary>
    /// A half transparent decal laid on by <c>bcs::decals</c> before the surface is lit comes out as
    /// the same decal does on Bevy's standard material beside it.
    /// </summary>
    [SkippableFact]
    public void DecalsLaidOnByAShaderAreLaidOnAsTheStandardMaterialLaysThem()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Width = 384,
            Height = 64,
            Scene = ecs =>
            {
                ShaderMaterialTests.Row(ecs);
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 5000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0.5f, 0.5f, 1f), Vec3.Zero, Vec3.UnitY));

                var cube = Render.CreateMesh(MeshShape.Cuboid, 1.5f, 1.5f, 1.5f);
                var orange = Solid(255, 128, 0, 128);

                Place(ecs, cube, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.2f, 0.2f, 0.6f, 1f) }), ShaderMaterialTests.InRow(1));
                var decaled = Shaders.CreateMaterial(Shaders.CreateProgram(ShaderStage.Slang(Decaled))).Set("color", new System.Numerics.Vector4(0.2f, 0.2f, 0.6f, 1f));
                Place(ecs, cube, decaled, ShaderMaterialTests.InRow(4));
                Decal(ecs, ShaderMaterialTests.InRow(1), orange, tag: 0);
                Decal(ecs, ShaderMaterialTests.InRow(4), orange, tag: 0);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var (standard, slang) = (picture.At(96, 32), picture.At(288, 32));
        Skip.If(standard.R <= standard.B, $"this device draws no clustered decals, the standard material's cube under one being {standard}");

        Assert.True(Math.Abs(slang.R - standard.R) <= 12 && Math.Abs(slang.G - standard.G) <= 12 && Math.Abs(slang.B - standard.B) <= 12,
            $"the shader's cube under the decal was {slang} where the standard material's was {standard}");
    }

    /// <summary>An image of four by four pixels of one color, in eight bits a channel.</summary>
    private static AssetHandle Solid(byte r, byte g, byte b, byte a)
    {
        var texels = new byte[4 * 4 * 4];
        for (var i = 0; i < texels.Length; i += 4) (texels[i], texels[i + 1], texels[i + 2], texels[i + 3]) = (r, g, b, a);
        return Shaders.CreateImage<byte>(4, 4, ShaderImageFormat.Rgba8, texels);
    }

    /// <summary>A clustered decal a unit square, facing the camera across the cube at <paramref name="at"/>.</summary>
    private static void Decal(EcsWorld ecs, Vec3 at, AssetHandle image, uint tag)
    {
        var decal = ecs.Spawn();
        ecs.Add(decal, new Transform(at, Quat.Identity, new Vec3(1f, 1f, 3f)));
        var clustered = ecs.Insert<Bevy.Reflected.ClusteredDecalRef>(decal);
        clustered.BaseColorTexture = image;
        clustered.Tag = tag;
    }

    private static void Place(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Vec3 at)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, Transform.At(at.X, at.Y, at.Z));
        Render.SetMesh(ecs, entity, mesh);
        Render.SetMaterial(ecs, entity, material);
    }
}

using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An anisotropic material drawn forward on a camera with a normal prepass, which Bevy 0.20 draws
/// blown white.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's forward shader loads the normal the prepass wrote where the camera has a normal prepass
/// and draws a pixel once (<c>LOAD_PREPASS_NORMALS</c>, set in <c>bevy_pbr</c>'s
/// <c>render/mesh.rs</c>), and then skips the block of <c>render/pbr_fragment.wesl</c> that builds
/// the tangent frame, which holds anisotropy's setup as well. The stretch's tangent and bitangent
/// stay at zero while the material's <c>STANDARD_MATERIAL_ANISOTROPY</c> still has the light take
/// the anisotropic highlight, whose distribution with both at zero comes to
/// <c>1 / (π α² cos⁴θ)</c>, which rises without bound toward the sphere's edge. Bevy leaves the
/// prepass normal out for a transmissive material and not for an anisotropic one.
/// </para>
/// <para>
/// Ambient occlusion asks for the normal prepass, which is how the feature test's gallery met it. A
/// camera drawing deferred draws the same sphere right. When the second test fails, Bevy has mended
/// its shader, and the remarks on <see cref="MaterialSettings.AnisotropyStrength"/> and the
/// materials guide's line come out with it.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class AnisotropyPrepassTests
{
    [SkippableTheory]
    [InlineData("forward")]
    [InlineData("deferred with occlusion")]
    public void ABrushedSphereKeepsItsHighlight(string camera)
    {
        Needs.Renderer();

        var white = WhiteOf(view =>
        {
            if (camera != "forward")
            {
                Shaders.SetPrepass(view, depth: true, deferred: true);
                Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            }
        }, out var pixels);

        Assert.True(white < pixels / 50, $"{white} of {pixels} pixels blown white drawn {camera}");
    }

    [SkippableTheory]
    [InlineData("a normal prepass")]
    [InlineData("occlusion")]
    public void ABrushedSphereBlowsWhiteForwardUnderANormalPrepassAsBevyDrawsIt(string camera)
    {
        Needs.Renderer();

        var white = WhiteOf(view =>
        {
            if (camera == "occlusion") Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            else Shaders.SetPrepass(view, depth: true, normals: true);
        }, out var pixels);

        // About half the picture is the sphere, all of it white while Bevy's shader is as it is.
        Assert.True(white > pixels / 4, $"only {white} of {pixels} pixels blown white under {camera}, so Bevy may have mended its shader; see the remarks");
    }

    /// <summary>How many pixels of a brushed metal sphere come out white, on a camera set up as given.</summary>
    private static int WhiteOf(Action<Entity> camera, out int pixels)
    {
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                var view = PictureRun.Camera(ecs);
                Render.SetPostProcessing(view, new PostSettings { Msaa = 1 });
                camera(view);

                // With tangents, which the stretch is read along.
                var sphere = Render.CreateMesh(MeshShape.Sphere, 2f);
                Render.GenerateTangents(sphere);
                ecs.SpawnMesh(sphere, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.4f, 0.4f, 0.43f, 1f),
                    Metallic = 1f,
                    Roughness = 0.45f,
                    AnisotropyStrength = 1f,
                    AnisotropyRotation = 0.5f,
                }), Transform.Identity);

                var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3_000f, Shadows = false });
                ecs.Add(light, Transform.LookingAt(new Vec3(2f, 3f, 4f), Vec3.Zero, Vec3.UnitY));
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        pixels = (int)(picture.Width * picture.Height);
        return PictureRun.Count(picture, (r, g, b) => r > 245 && g > 245 && b > 245);
    }
}

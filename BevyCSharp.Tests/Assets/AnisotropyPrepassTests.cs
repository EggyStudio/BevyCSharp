using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// An anisotropic material drawn forward on a camera with a normal prepass, which Bevy 0.20 draws
/// blown white and the bridge mends, drawn as on every other camera.
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
/// The bridge mends the shader in the text Bevy ships, the setup moved out of the skipped block with
/// a tangent frame of its own (<c>render/mended.rs</c>). Ambient occlusion asks for the normal
/// prepass, which is how the feature test's gallery met it. A release of Bevy whose text moved
/// refuses the mend, which the last test here holds, so the mend is written again for that release,
/// or taken out where Bevy has mended its shader.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class AnisotropyPrepassTests
{
    [SkippableTheory]
    [InlineData("forward")]
    [InlineData("deferred with occlusion")]
    [InlineData("a normal prepass")]
    [InlineData("occlusion")]
    public void ABrushedSphereKeepsItsHighlight(string camera)
    {
        Needs.Renderer();

        var white = WhiteOf(view =>
        {
            if (camera == "deferred with occlusion")
            {
                Shaders.SetPrepass(view, depth: true, deferred: true);
                Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            }
            else if (camera == "occlusion")
            {
                Render.SetAmbientOcclusion(view, AmbientOcclusionQuality.High);
            }
            else if (camera == "a normal prepass")
            {
                Shaders.SetPrepass(view, depth: true, normals: true);
            }
        }, out var pixels, out _);

        Assert.True(white < pixels / 50, $"{white} of {pixels} pixels blown white drawn {camera}");
    }

    /// <summary>
    /// The bridge mended Bevy's shader, which a release of Bevy whose text moved refuses, failing
    /// here so the mend follows it.
    /// </summary>
    [SkippableFact]
    public void TheBridgeMendsBevysShaderForAnisotropy()
    {
        Needs.Renderer();

        WhiteOf(view => Shaders.SetPrepass(view, depth: true, normals: true), out _, out var mended);

        Assert.True(mended == true, mended is null
            ? "the standard material's shader had not loaded when the picture was taken"
            : "Bevy's pbr_fragment.wesl is not the text the bridge's mend was written for; follow it in native/bevy_csharp/src/render/mended.rs");
    }

    /// <summary>
    /// How many pixels of a brushed metal sphere come out white, on a camera set up as given, and
    /// whether the bridge had mended Bevy's shader by the picture.
    /// </summary>
    private static int WhiteOf(Action<Entity> camera, out int pixels, out bool? mended)
    {
        bool? read = null;
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

        run.Wait(ShaderMaterialTests.Settled).Do("mend", _ => read = Render.AnisotropyMended).Capture("picture").Go();

        mended = read;
        var picture = run.Picture("picture");
        pixels = (int)(picture.Width * picture.Height);
        return PictureRun.Count(picture, (r, g, b) => r > 245 && g > 245 && b > 245);
    }
}

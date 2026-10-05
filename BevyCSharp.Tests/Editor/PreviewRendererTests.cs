using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the editor's previews, a mesh and a material drawn into pictures of their own, read back
/// to see that something other than the background was drawn.
/// </summary>
/// <remarks>
/// An offscreen app, since a preview is a camera drawing into an image and a headless bridge has
/// neither. The picture is read back into memory, so the test asks whether the subject's pixels are
/// there rather than whether a setting was accepted.
/// </remarks>
[Collection("engine")]
public sealed class PreviewRendererTests
{
    [SkippableFact]
    public void AMeshAndAMaterialAreDrawnIntoTheirPictures()
    {
        Needs.Renderer();

        var pictures = new Dictionary<string, CapturedImage>();
        var tickets = new Dictionary<string, Capture>();

        using var app = new App(Config.OffscreenFor(320, 180, frames: 240));
        app.AddPlugin(new EnginePlugin());

        AssetHandle mesh = default, material = default;

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            _ =>
            {
                mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
                material = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0.1f, 0.1f, 1f) });
            },
            "Test.Make"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                var ctx = new BehaviorContext(world);
                PreviewRenderer.Show(ctx, "mesh", new PreviewSubject.Mesh(mesh));
                PreviewRenderer.Show(ctx, "material", new PreviewSubject.Material(material));

                // Late enough for the pipelines to be built and the framing to have found bounds.
                if (world.Resource<Time>().FrameCount == 120)
                {
                    tickets["mesh"] = Render.BeginCapture(PreviewRenderer.TargetOf("mesh"));
                    tickets["material"] = Render.BeginCapture(PreviewRenderer.TargetOf("material"));
                }

                foreach (var (key, ticket) in tickets)
                {
                    if (!pictures.ContainsKey(key) && Render.TryReadCapture(ticket, out var picture) && picture is not null)
                        pictures[key] = picture;
                }
            },
            "Test.Show"));

        Assert.Equal(0, app.Run());

        // The corner is the background, and the middle is the subject, a different color.
        foreach (var key in new[] { "mesh", "material" })
        {
            Assert.True(pictures.ContainsKey(key), $"the {key} picture never arrived");
            var picture = pictures[key];
            var corner = picture.At(2, 2);
            var middle = picture.At(picture.Width / 2, picture.Height / 2);
            Assert.True(
                Math.Abs(corner.R - middle.R) + Math.Abs(corner.G - middle.G) + Math.Abs(corner.B - middle.B) > 60,
                $"the {key} picture is {corner} at the corner and {middle} in the middle");
        }

        // The material's sphere is drawn in the material's red.
        var red = pictures["material"].At(pictures["material"].Width / 2, pictures["material"].Height / 2);
        Assert.True(red.R > red.G + 40, $"the material's sphere came back as {red}");
    }
}

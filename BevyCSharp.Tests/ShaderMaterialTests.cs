using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a material drawn by a shader the game wrote.
/// </summary>
/// <remarks>
/// Whether the numbers reached the shader is not a question anything but a picture can answer. A
/// material that never compiled draws nothing, one whose uniform is laid out differently from what
/// the shader declares draws the wrong colour, and both look the same from the managed side.
/// </remarks>
[Collection("engine")]
public sealed class ShaderMaterialTests
{
    /// <summary>Frames to let the pipelines compile before the picture is worth reading.</summary>
    private const ulong Settled = 120;

    [Fact]
    public void AShaderMaterialDrawsTheColourItWasGiven()
    {
        if (!App.HasRenderer) return;

        var picture = Draw();

        Assert.NotNull(picture);

        // The middle of the screen is the quad, which the shader paints from the first four
        // floats. Green rather than the material's own, so nothing else could have drawn it.
        var middle = picture.At(48, 48);

        Assert.True(
            middle.G > 120 && middle.R < 90 && middle.B < 90,
            $"the quad came out {middle} rather than green");
    }

    /// <summary>A vertex shader moves the mesh, which shows as the shape covering more of it.</summary>
    /// <remarks>
    /// The same cube drawn twice by the same slot, pushed along its own normals by the first
    /// number the material carries. A vertex shader that never ran would leave the two pictures
    /// the same size, which is the only thing that tells it apart from a fragment shader.
    /// </remarks>
    [Fact]
    public void AVertexShaderMovesTheMesh()
    {
        if (!App.HasRenderer) return;

        var still = Draw(vertex: true, swell: 0f);
        var swollen = Draw(vertex: true, swell: 0.6f);

        Assert.NotNull(still);
        Assert.NotNull(swollen);

        var small = Lit(still);
        var large = Lit(swollen);

        Assert.True(small > 0, "the cube was not drawn at all");
        Assert.True(
            large > small + 200,
            $"the shape covered {small} pixels still and {large} pushed out");
    }

    /// <summary>How many pixels the shape reached, which is green either way.</summary>
    private static int Lit(CapturedImage picture)
    {
        var drawn = 0;

        for (var i = 0; i < picture.Pixels.Length; i += 4)
        {
            if (picture.Pixels[i + 1] > 120 && picture.Pixels[i] < 90) drawn++;
        }

        return drawn;
    }

    /// <summary>A slot nothing was said about has nothing to draw with, so it is refused.</summary>
    [Fact]
    public void ASlotWithNoShaderIsRefused()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var refused = Assert.Throws<BevyNativeException>(
                () => Shaders.CreateMaterial(Shaders.SlotCount - 1, [1f, 0f, 0f, 1f]));

            Assert.NotEqual(0, (int)refused.Status);
        });

        harness.Run();
    }

    /// <summary>More numbers than a material carries is a mistake rather than a truncation.</summary>
    [Fact]
    public void TooManyNumbersAreRefused()
    {
        Assert.Throws<ArgumentException>(
            () => Shaders.CreateMaterial(0, new float[Shaders.ParameterCount + 1]));
    }

    /// <summary>Runs an offscreen app drawing one quad with the test's own shader.</summary>
    private static CapturedImage? Draw(bool vertex = false, float swell = 0f)
    {
        CapturedImage? picture = null;

        var config = Config.OffscreenFor(96, 96, frames: (uint)Settled + 40);
        config.AssetRoot = EngineHarness.AssetDirectory;

        using var app = new App(config);

        app.AddPlugin(new EnginePlugin());
        if (vertex) app.UseShader(1, "shaders/ripple.wgsl", "shaders/ripple.wgsl");
        else app.UseShader(0, "shaders/flat.wgsl");

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 0f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

                var quad = ecs.Spawn();

                // A cube rather than a plane, so no rotation has to be right for the test to be
                // about the material.
                Render.SetMesh(ecs, quad, Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f));

                // Green, in the first four floats, which is what the shader paints with.
                Render.SetMaterial(
                    ecs,
                    quad,
                    vertex
                        ? Shaders.CreateMaterial(1, [swell, 0f, 0f, 0f, 0f, 1f, 0f, 1f])
                        : Shaders.CreateMaterial(0, [0f, 1f, 0f, 1f]));

                ecs.Add(quad, Transform.Identity);
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled)
                {
                    world.InsertResource(new Ticket(Render.BeginCapture()));
                    return;
                }

                if (picture is not null) return;
                if (!world.TryGetResource<Ticket>(out var ticket)) return;

                if (Render.TryReadCapture(ticket.Capture, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        return picture;
    }

    /// <summary>Where the run keeps what it asked for, so a later frame can pick it up.</summary>
    private sealed record Ticket(Capture Capture);
}

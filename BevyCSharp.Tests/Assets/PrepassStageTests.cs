using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A material's prepass vertex shader reads the time from the prepass's own binding, and the
/// pipeline it is drawn with builds, where the prepass draws a shadow.
/// </summary>
/// <remarks>
/// The guide's example of a stage moving with the time read <c>bcs::globals</c> in its prepass,
/// which the prepass does not bind, and the feature test's grass, written from it, ended the app at
/// the first shadow drawn. <c>bcs::prepass_globals</c> is the binding the prepass gives.
/// </remarks>
[Collection("engine")]
public sealed class PrepassStageTests
{
    [SkippableFact]
    public void APrepassStageReadingTheTimeIsDrawn()
    {
        Needs.Shaders();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 4f, 6f));

                // A sun casting shadows, so the prepass draws the ball.
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 8000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(0.2f, -1f, 0.1f), Vec3.UnitZ));

                const string File = "shaders/bob.slang";
                var program = Shaders.CreateProgram(new ShaderProgramSettings
                {
                    Vertex = File,
                    Fragment = File,
                    PrepassVertex = new ShaderStage(File, "prepass_vertex"),
                });

                var ball = ecs.Spawn();
                Render.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Sphere, 1f));
                Render.SetMaterial(ecs, ball, Shaders.CreateMaterial(program).Set("color", ShaderMaterialTests.Green));
                ecs.Add(ball, Transform.Identity);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady()).Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var green = PictureRun.Count(run.Picture("picture"), (r, g, b) => g > 150 && r < 60 && b < 60);
        Assert.True(green > 100, $"the ball was {green} green pixels");
    }
}

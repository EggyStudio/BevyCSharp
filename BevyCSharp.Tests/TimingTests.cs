using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers measuring how long render passes take.</summary>
[Collection("engine")]
public sealed class TimingTests
{
    /// <summary>
    /// With timings asked for, a compute shader dispatched every frame is measured under its file's
    /// name, beside Bevy's own passes.
    /// </summary>
    [Fact]
    public void ADispatchIsTimedUnderItsFilesName()
    {
        if (!ShaderMaterialTests.CanRun) return;

        ShaderInstance fill = default;
        IReadOnlyList<PassTiming> timings = [];

        new PictureRun
        {
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f));

                fill = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/fill.slang" }))
                    .Set("color", new Vector4(1f, 0f, 0f, 1f))
                    .SetBuffer("colors", Shaders.CreateBuffer(16));
            },
            EachFrame = _ =>
            {
                if (fill.IsValid) Shaders.Dispatch(fill, 1);
            },
        }
            .Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Wait(ShaderMaterialTests.Settled)
            .Do("reading the timings", _ => timings = Render.Timings())
            .Go();

        var names = string.Join(", ", timings.Select(timing => timing.Name));

        Assert.Contains(timings, timing => timing.Name == "shader fill" && timing.CpuMilliseconds is not null);
        Assert.True(timings.Count > 3, $"only these passes were timed: {names}");
    }

    /// <summary>Without asking, nothing is measured.</summary>
    [Fact]
    public void NothingIsTimedUnlessAsked()
    {
        if (!App.HasRenderer) return;

        IReadOnlyList<PassTiming> timings = [new("unset", null, null)];

        new PictureRun { Scene = ecs => PictureRun.Camera(ecs) }
            .Wait(10)
            .Do("reading the timings", _ => timings = Render.Timings())
            .Go();

        Assert.Empty(timings);
    }
}

using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what the bridge makes on the GPU for the dispatches and draws asked for every frame,
/// which it makes once and keeps.
/// </summary>
/// <remarks>
/// What it made is read from the <c>memory</c> command's <c>shaders.</c> pairs, which count every
/// buffer of a shader's numbers and every bind group of its values the bridge makes, over a span of
/// frames in which nothing a shader binds changes but numbers.
/// </remarks>
[Collection("engine")]
public sealed class KeptGroupTests
{
    private static ShaderInstance Compute(string file) =>
        Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = file }));

    /// <summary>
    /// A dispatch asked for every frame with a number that moves, and a camera's dispatch and draw
    /// run every frame, make no buffer and no group once their first frames have made theirs, and
    /// the dispatch writes the number it was given last.
    /// </summary>
    [SkippableFact]
    public void WhatRunsEveryFrameMakesNoBufferOrGroupOnceMade()
    {
        Needs.Shaders();

        var fill = default(ShaderInstance);
        var colors = AssetHandle.None;
        var read = default(BufferRead);
        float[]? written = null;
        var (dispatching, frame, sent) = (false, 0, 0);
        var made = new List<(long Buffers, long Groups)>();

        void Count(World world)
        {
            using (ConsoleHost.Lend(world))
            {
                var pairs = MemoryCommandTests.Pairs(ConsoleHost.Run("memory").Text);
                made.Add((pairs["shaders.buffersMade"], pairs["shaders.groupsMade"]));
            }
        }

        var run = new PictureRun
        {
            Width = 64,
            Height = 64,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                var centers = Shaders.CreateBuffer<Vector4>([new(-2f, 0f, 0f, 1f), new(2f, 0f, 0f, 1f)]);
                var counts = Shaders.CreateBuffer(16);
                colors = Shaders.CreateBuffer<float>([0f, 0f, 0f, 0f]);
                fill = Compute("shaders/fill.slang").SetBuffer("colors", colors);

                var count = Compute("shaders/write_draw_counts.slang").SetBuffer("counts", counts).Set("squares", 1u);
                var squares = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                    {
                        DrawVertex = "shaders/draw_quads.slang",
                        DrawFragment = "shaders/draw_quads.slang",
                    }))
                    .SetBuffer("centers", centers)
                    .Set("size", 0.5f)
                    .Set("color", ShaderMaterialTests.Green);

                Shaders.SetViewDispatches(camera, ViewDispatch.Fixed(count, FramePoint.AfterOpaque, 1));
                Shaders.SetViewDraws(camera, ViewDraw.Indirect(squares, FramePoint.AfterOpaque, counts));
            },
            // A new number every frame, which the buffer kept for the dispatch's numbers is
            // written with.
            EachFrame = _ =>
            {
                if (dispatching) Shaders.Dispatch(fill.Set("color", new Vector4(++frame, 0f, 0f, 1f)), 1);
            },
        };

        run.Until("compiled", _ => ShaderMaterialTests.ProgramsReady())
            .Do("dispatching", _ => dispatching = true)
            .Wait(10)
            .Do("before", Count)
            .Wait(60)
            .Do("after", Count)
            .Do("stopped", _ => (dispatching, sent) = (false, frame))
            .Wait(3)
            .Do("asking for it back", _ => read = Shaders.BeginBufferRead(colors))
            .Until("read back", _ => Shaders.TryReadBuffer(read, out written))
            .Go();

        Assert.Equal(made[0], made[1]);
        Assert.True(sent > 60, $"the dispatch ran {sent} times");
        Assert.NotNull(written);
        Assert.Equal([(float)sent, 0f, 0f, 1f], written[..4]);
    }
}

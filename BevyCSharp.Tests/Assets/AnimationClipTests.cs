using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers animation clips built in code, played from a graph on the entities they name.</summary>
/// <remarks>
/// Each frame steps a tenth of a second exactly, so where a clip has moved an entity by a frame is
/// known, and a run that draws slowly reads the same.
/// </remarks>
[Collection("engine")]
public sealed class AnimationClipTests
{
    /// <summary>Each field of a curve's description sits where the bridge reads it.</summary>
    [Theory]
    [InlineData(nameof(NativeAnimationCurve.Property), 0)]
    [InlineData(nameof(NativeAnimationCurve.Kind), 4)]
    [InlineData(nameof(NativeAnimationCurve.Count), 8)]
    [InlineData(nameof(NativeAnimationCurve.Ease), 12)]
    [InlineData(nameof(NativeAnimationCurve.EaseSteps), 16)]
    [InlineData(nameof(NativeAnimationCurve.EaseJump), 20)]
    [InlineData(nameof(NativeAnimationCurve.EaseOmega), 24)]
    [InlineData(nameof(NativeAnimationCurve.Duration), 28)]
    [InlineData(nameof(NativeAnimationCurve.PingPong), 32)]
    public void EveryFieldOfACurveSitsWhereTheBridgeReadsIt(string field, int offset)
    {
        Assert.Equal(offset, Marshal.OffsetOf<NativeAnimationCurve>(field).ToInt32());
        Assert.Equal(36, Marshal.SizeOf<NativeAnimationCurve>());
    }

    /// <summary>
    /// A clip built in code moves the entity its curve is aimed at, partway along by the middle of
    /// the curve and at its last value once the clip is over, and an eased curve played there and
    /// back comes home.
    /// </summary>
    [SkippableFact]
    public void AClipBuiltInCodeMovesTheEntityItNames()
    {
        Needs.Renderer();

        var (sampled, eased) = (Entity.None, Entity.None);
        var frame = 0;
        float? midway = null, end = null, easedFar = null, easedHome = null;

        var config = Config.OffscreenFor(64, 64, frames: 40);
        config.FrameSeconds = 0.1;
        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render.SpawnCamera3d();

            // Ten units along over two seconds, played once.
            var clip = Animation.CreateClip();
            var target = AnimationTarget.FromNames("mover");
            Animation.AddCurve(clip, target, AnimationCurve.Translation([0f, 2f], [Vec3.Zero, new Vec3(10f, 0f, 0f)]));
            var (graph, node) = Animation.GraphFromClip(clip);
            sampled = ecs.Spawn();
            ecs.Add(sampled, Transform.Identity);
            Animation.PlayGraph(sampled, graph, node);
            Animation.Animate(sampled, target, sampled);

            // Ten units over one second, eased, and back over the next.
            var there = Animation.CreateClip();
            var traveler = AnimationTarget.FromNames("traveler");
            Animation.AddCurve(there, traveler, AnimationCurve.Translation(Vec3.Zero, new Vec3(10f, 0f, 0f), EaseFunction.CubicInOut, 1f).PingPong());
            var (back, backNode) = Animation.GraphFromClip(there);
            eased = ecs.Spawn();
            ecs.Add(eased, Transform.Identity);
            Animation.PlayGraph(eased, back, backNode);
            Animation.Animate(eased, traveler, eased);
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            frame++;
            if (frame == 11) (midway, easedFar) = (ecs.GetOrDefault<Transform>(sampled).Translation.X, ecs.GetOrDefault<Transform>(eased).Translation.X);
            if (frame == 35) (end, easedHome) = (ecs.GetOrDefault<Transform>(sampled).Translation.X, ecs.GetOrDefault<Transform>(eased).Translation.X);
        }, "Test.Read"));

        Assert.Equal(0, app.Run());

        // About a second in, about halfway along, and the eased one about at its far end.
        Assert.InRange(midway!.Value, 3.5f, 6.5f);
        Assert.InRange(easedFar!.Value, 8.5f, 10.01f);

        // Long past, the clip held at its last value and the eased one home again.
        Assert.Equal(10f, end!.Value, 2);
        Assert.Equal(0f, easedHome!.Value, 2);
    }

    /// <summary>A curve whose times do not rise, or whose times and values differ in number, is refused before it reaches Bevy.</summary>
    [Fact]
    public void ACurveWithTimesOutOfOrderOrOfAnotherCountIsRefused()
    {
        Assert.Throws<ArgumentException>(() => AnimationCurve.Translation([0f, 1f, 1f], [Vec3.Zero, Vec3.Zero, Vec3.Zero]));
        Assert.Throws<ArgumentException>(() => AnimationCurve.Scale([0f, 1f], [Vec3.One]));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnimationCurve.Rotation(Quat.Identity, Quat.Identity, EaseFunction.Linear, 0f));
        Assert.Throws<InvalidOperationException>(() => AnimationCurve.UiRotation([0f], [0f]).PingPong());
    }
}

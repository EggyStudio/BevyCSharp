using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's resources reached through reflection, each a component on an entity of its own.
/// </summary>
[Collection("engine")]
public sealed class ResourceTests
{
    /// <summary>
    /// The clear color is found as a resource, and a camera that clears to the default draws the
    /// color written to it.
    /// </summary>
    [SkippableFact]
    public void AResourceWrittenThroughItsWrapperChangesWhatIsDrawn()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();
                if (ecs.Resource<ClearColorRef>() is not { } clear) throw new InvalidOperationException("The world has no clear color.");
                clear.Value = new Color(0f, 1f, 0f, 1f);
            },
        };

        run.Wait(10).Capture("cleared").Go();

        var (r, g, b, _) = run.Picture("cleared").At(10, 10);
        Assert.True(g > 200 && r < 60 && b < 60, $"the window was not cleared to the resource's green, but ({r}, {g}, {b})");
    }

    /// <summary>
    /// A resource inserted over the one the world has replaces it on the same entity, and a
    /// component asked for as a resource is refused rather than reported absent.
    /// </summary>
    [SkippableFact]
    public void AResourceIsReplacedWhereItIsAndAComponentIsNoResource()
    {
        Needs.Renderer();

        Entity? before = null, after = null;
        float scale = 0f;
        BevyNativeException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                before = ecs.ResourceEntity(UiScaleRef.TypePath);
                ecs.InsertResource<UiScaleRef>("2.0");
                after = ecs.ResourceEntity(UiScaleRef.TypePath);
                scale = ecs.Resource<UiScaleRef>()?.Value ?? 0f;
                refused = Assert.Throws<BevyNativeException>(() => ecs.ResourceEntity("bevy_transform::components::transform::Transform"));
            },
        };

        run.Wait(2).Go();

        Assert.NotNull(before);
        Assert.Equal(before, after);
        Assert.Equal(2f, scale);
        Assert.Equal(NativeStatus.NoComponent, refused!.Status);
    }

    /// <summary>
    /// An insert from nothing over a resource a plugin made, of a type reflecting no default, is
    /// refused with the call to use instead, and leaves the plugin's as it was, while one where the
    /// world has none is built field by field.
    /// </summary>
    /// <remarks>
    /// Bevy's default filters hold the component that disables an entity, and built from nothing
    /// they would hold none. Bevy's switch for static transforms, taken off, is no longer found,
    /// and goes back on the entity Bevy kept for it, built at its first variant, enabled, which is
    /// also where Bevy starts it.
    /// </remarks>
    [Fact]
    public void AnInsertFromNothingKeepsAPluginsResourceAndBuildsOneTheWorldLacks()
    {
        using var harness = new EngineHarness(frames: 1);
        InvalidOperationException? refused = null;
        string? filters = null;
        StaticTransformOptimizationsRef.ValueVariant? built = null;
        Entity? before = null, after = null;
        var absent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            var ecs = ctx.Ecs;
            refused = Assert.Throws<InvalidOperationException>(() => ecs.InsertResource<DefaultQueryFiltersRef>());
            filters = ecs.GetReflected(ecs.ResourceEntity(DefaultQueryFiltersRef.TypePath)!.Value, DefaultQueryFiltersRef.TypePath, "disabling");

            before = ecs.ResourceEntity(StaticTransformOptimizationsRef.TypePath);
            if (ecs.Resource<StaticTransformOptimizationsRef>() is { } held) held.Remove();
            absent = ecs.Resource<StaticTransformOptimizationsRef>() is null;
            built = ecs.InsertResource<StaticTransformOptimizationsRef>().Value;
            after = ecs.ResourceEntity(StaticTransformOptimizationsRef.TypePath);
        });
        harness.Run();

        Assert.Contains("Resource<DefaultQueryFiltersRef>()", refused!.Message);
        Assert.NotNull(filters);
        Assert.NotEqual("[]", filters);
        Assert.True(absent, "a resource taken off was still found");
        Assert.Equal(StaticTransformOptimizationsRef.ValueVariant.Enabled, built);
        Assert.NotNull(before);
        Assert.Equal(before, after);
    }
}

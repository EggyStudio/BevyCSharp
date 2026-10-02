using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the typed wrappers generated from the checked-in description of Bevy's components.
/// </summary>
/// <remarks>
/// The wrappers are compiled into the library, so these exercise the generator through what it
/// emitted: a property that read the wrong path, or had the wrong type, fails here against a real
/// world rather than in a snapshot of the generated text.
/// </remarks>
[Collection("engine")]
public sealed class ReflectedWrapperTests
{
    [Fact]
    public void AWrapperAndTheMirrorAgreeOnATransform()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(1f, 2f, 3f));

            var transform = ctx.Ecs.Get<TransformRef>(entity)
                ?? throw new InvalidOperationException("The transform has no wrapper.");
            Assert.Equal(new Vec3(1f, 2f, 3f), transform.Translation);
            Assert.Equal(Transform.Identity.Rotation, transform.Rotation);

            // Written through the wrapper, read through the mirror, so both reach the same bytes.
            transform.Scale = new Vec3(2f, 3f, 4f);
            Assert.Equal(new Vec3(2f, 3f, 4f), ctx.Ecs.GetRef<Transform>(entity).Scale);

            ctx.Ecs.GetRef<Transform>(entity).Translation = new Vec3(7f, 8f, 9f);
            Assert.Equal(new Vec3(7f, 8f, 9f), transform.Translation);

            // An entity without the component has no wrapper, rather than one that throws later.
            Assert.Null(ctx.Ecs.Get<TransformRef>(ctx.Ecs.Spawn()));
        });

        harness.Run();
    }

    [Fact]
    public void AWrapperOverAComponentTakenOffThrowsRatherThanReadingNothing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            var transform = ctx.Ecs.Insert<TransformRef>(entity);
            Assert.Equal(Vec3.One, transform.Scale);

            Assert.True(transform.Remove());
            Assert.False(transform.Remove());
            Assert.Throws<BevyNativeException>(() => transform.Scale);
        });

        harness.Run();
    }

    [Fact]
    public void APointLightIsWrittenThroughItsWrapper()
    {
        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (!App.HasRenderer) return;

            var lamp = ctx.Ecs.Spawn();
            var light = ctx.Ecs.Insert<PointLightRef>(lamp);

            light.Intensity = 5000f;
            light.ShadowMapsEnabled = true;
            light.Color = PointLightRef.ColorVariant.LinearRgba;

            // Read back through the string paths, which the wrapper's properties stand for.
            Assert.Equal("5000.0", ctx.Ecs.GetReflected(lamp, PointLightRef.TypePath, ".intensity"));
            Assert.Equal("true", ctx.Ecs.GetReflected(lamp, PointLightRef.TypePath, ".shadow_maps_enabled"));
            Assert.Equal("LinearRgba", ctx.Ecs.GetVariant(lamp, PointLightRef.TypePath, ".color"));

            Assert.Equal(5000f, light.Intensity);
            Assert.Equal(PointLightRef.ColorVariant.LinearRgba, light.Color);
            ran = true;
        });

        harness.Run();

        if (App.HasRenderer) Assert.True(ran);
    }
}

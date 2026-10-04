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
/// emitted. A property that read the wrong path, or had the wrong type, fails here against a real
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

    [SkippableFact]
    public void APointLightIsWrittenThroughItsWrapper()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var lamp = ctx.Ecs.Spawn();
            var light = ctx.Ecs.Insert<PointLightRef>(lamp);

            light.Intensity = 5000f;
            light.ShadowMapsEnabled = true;
            light.Color = Color.FromHex("#ff8800");

            // Read back through the string paths, which the wrapper's properties stand for.
            Assert.Equal("5000.0", ctx.Ecs.GetReflected(lamp, PointLightRef.TypePath, ".intensity"));
            Assert.Equal("true", ctx.Ecs.GetReflected(lamp, PointLightRef.TypePath, ".shadow_maps_enabled"));

            Assert.Equal(5000f, light.Intensity);
            Assert.Equal(Color.FromHex("#ff8800").G, light.Color.G, 4);
            ran = true;
        });

        harness.Run();

        Assert.True(ran);
    }

    [SkippableFact]
    public void ANameAndAVisibilityClassReadAsValues()
    {
        Needs.Renderer();

        string? name = null, classes = null;

        // Offscreen, since a mesh is given its visibility class by the rendering a headless run
        // leaves out.
        using var app = new App(new Config { Offscreen = true, Width = 32, Height = 32, HeadlessFps = 60, HeadlessFrames = 4 });

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var cube = ecs.Spawn();
            ecs.SetName(cube, "Named cube");
            Render.SetMesh(ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
        }, "Test.Spawn"));

        app.AddSystem(Stage.Update, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var cube = ecs.All().First(entity => ecs.NameOf(entity) == "Named cube");

            // Through the rows a listing and the inspector read, since a hashed string and a list
            // of Rust type ids have no plain value of their own.
            name = ComponentSchemas.For("bevy_ecs::name::Name")!.Fields[0].Read(ecs, cube) as string;
            classes = ecs.GetReflected(cube, "bevy_camera::visibility::VisibilityClass");
        }, "Test.Read"));

        Assert.Equal(0, app.Run());

        Assert.Equal("\"Named cube\"", name);
        Assert.NotNull(classes);
        Assert.Contains("Mesh3d", classes);
    }
}

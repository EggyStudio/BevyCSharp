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

    /// <summary>
    /// An enum whose variants hold values is a record a variant, written whole and read back as the
    /// variant and the values it was given.
    /// </summary>
    [SkippableFact]
    public void FogsFalloffIsEachVariantItWasGivenWithItsValues()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var seen = new List<FogFalloff>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var fog = ctx.Ecs.Insert<DistanceFogRef>(ctx.Ecs.Spawn());
            FogFalloff[] falloffs =
            [
                new FogFalloff.Linear(5f, 20f),
                new FogFalloff.Exponential(0.07f),
                new FogFalloff.ExponentialSquared(0.12f),
                new FogFalloff.Atmospheric(new Vec3(0.1f, 0.2f, 0.3f), new Vec3(0.4f, 0.5f, 0.6f)),
                new FogFalloff.Linear(1f, 8f),
            ];

            foreach (var falloff in falloffs)
            {
                fog.Falloff = falloff;
                seen.Add(fog.Falloff);
            }
        });

        harness.Run();

        Assert.Equal(
        [
            new FogFalloff.Linear(5f, 20f),
            new FogFalloff.Exponential(0.07f),
            new FogFalloff.ExponentialSquared(0.12f),
            new FogFalloff.Atmospheric(new Vec3(0.1f, 0.2f, 0.3f), new Vec3(0.4f, 0.5f, 0.6f)),
            new FogFalloff.Linear(1f, 8f),
        ], seen);
    }

    /// <summary>
    /// A length in the interface is the variant and its number, and an <c>Option</c> is a nullable
    /// that reads back as nothing once it is set to nothing.
    /// </summary>
    [SkippableFact]
    public void AnInterfaceLengthAndAnOptionalViewReadBackAsTheyWereWritten()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        Val? left = null, width = null;
        SubCameraView? view = null, cleared = new(1, 1, Vec2.Zero, 1, 1);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var node = ctx.Ecs.Insert<NodeRef>(ctx.Ecs.Spawn());
            node.Left = new Val.Px(12f);
            node.Width = new Val.Percent(50f);
            (left, width) = (node.Left, node.Width);

            var camera = ctx.Ecs.Get<CameraRef>(Render.SpawnCamera3d())!.Value;
            camera.SubCameraView = new SubCameraView(10, 10, new Vec2(5f, 0f), 5, 10);
            view = camera.SubCameraView;
            camera.SubCameraView = null;
            cleared = camera.SubCameraView;
        });

        harness.Run();

        Assert.Equal(new Val.Px(12f), left);
        Assert.Equal(new Val.Percent(50f), width);
        Assert.Equal(new SubCameraView(10, 10, new Vec2(5f, 0f), 5, 10), view);
        Assert.Null(cleared);
    }

    /// <summary>
    /// A variant holding a handle can be chosen, and a component holding one inserted, which need a
    /// handle made for them before the one written arrives, since Bevy registers no default for a
    /// handle.
    /// </summary>
    [SkippableFact]
    public void AVariantHoldingAHandleIsChosenWithTheHandleWritten()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        FontSource? font = null;
        AssetHandle? texture = AssetHandle.None, cleared = AssetHandle.None;
        var image = AssetHandle.None;
        var loaded = AssetHandle.None;
        var inserted = AssetHandle.None;
        var layout = PointLightTextureRef.CubemapLayoutVariant.SequenceHorizontal;

        harness.OnContext(Stage.Startup, ctx =>
        {
            loaded = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            var text = ctx.Ecs.Insert<TextFontRef>(ctx.Ecs.Spawn());
            text.Font = new FontSource.Handle(loaded);
            font = text.Font;

            image = Render.CreateImage(new byte[4 * 4 * 4], 4, 4);
            var fog = ctx.Ecs.Insert<FogVolumeRef>(ctx.Ecs.Spawn());
            fog.DensityTexture = image;
            texture = fog.DensityTexture;
            fog.DensityTexture = null;
            cleared = fog.DensityTexture;

            // A component holding a handle has no default of its own, and is made from its fields'.
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional });
            var caustics = ctx.Ecs.Insert<DirectionalLightTextureRef>(light);
            caustics.Image = image;
            inserted = caustics.Image;

            // And one holding an enum with no default, which takes its first plain variant.
            var point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point });
            var faces = ctx.Ecs.Insert<PointLightTextureRef>(point);
            faces.CubemapLayout = PointLightTextureRef.CubemapLayoutVariant.CrossVertical;
            layout = faces.CubemapLayout;
        });

        harness.Run();

        Assert.Equal(new FontSource.Handle(loaded), font);
        Assert.Equal(image, texture);
        Assert.Null(cleared);
        Assert.Equal(image, inserted);
        Assert.Equal(PointLightTextureRef.CubemapLayoutVariant.CrossVertical, layout);
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

    /// <summary>
    /// A component of unnamed fields with no default of its own, as a slider's value is, is
    /// inserted at its fields' defaults, as one of named fields is.
    /// </summary>
    [SkippableFact]
    public void AStructOfUnnamedFieldsWithNoDefaultIsInsertedAtItsFieldsDefaults()
    {
        Needs.Renderer();

        float? inserted = null, written = null;

        using var app = new App(new Config { Offscreen = true, Width = 32, Height = 32, HeadlessFps = 60, HeadlessFrames = 3 });
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            var slider = Ui.SpawnNode(new UiSettings());
            var value = ecs.Insert<Bevy.Reflected.SliderValueRef>(slider);
            inserted = value.Value;
            value.Value = 42f;
            written = ecs.Get<Bevy.Reflected.SliderValueRef>(slider)?.Value;
        }, "Test.Insert"));

        Assert.Equal(0, app.Run());

        Assert.Equal(0f, inserted);
        Assert.Equal(42f, written);
    }
}

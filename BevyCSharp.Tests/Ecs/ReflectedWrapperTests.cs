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
    /// A range of numbers, which Bevy reflects as a value of its own with no fields to reach, is read
    /// and written whole by its wrapper, and its row is its two ends.
    /// </summary>
    [SkippableFact]
    public void AVisibilityRangesMarginsAreRangesReadAndWrittenWhole()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var (start, end, json, row, written) = (default(FloatRange), default(FloatRange), (string?)null, (object?)null, false);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            var range = ctx.Ecs.Insert<VisibilityRangeRef>(entity);
            range.StartMargin = new FloatRange(3f, 4f);
            range.EndMargin = new FloatRange(8f, 9.5f);
            (start, end) = (range.StartMargin, range.EndMargin);
            json = ctx.Ecs.GetReflected(entity, VisibilityRangeRef.TypePath, ".end_margin");

            var margin = ComponentSchemas.For(VisibilityRangeRef.TypePath)!.Field("start_margin")!;
            written = margin.Write(ctx.Ecs, entity, new Vec2(1f, 2f));
            row = margin.Read(ctx.Ecs, entity);
        });

        harness.Run();

        Assert.Equal(new FloatRange(3f, 4f), start);
        Assert.Equal(new FloatRange(8f, 9.5f), end);
        Assert.Equal("""{"start":8.0,"end":9.5}""", json?.Replace(" ", string.Empty));
        Assert.True(written);
        Assert.Equal(new Vec2(1f, 2f), row);
    }

    /// <summary>
    /// An enum inside a variant is a record of its own inside the variant's, chosen and written with
    /// it and read back as it was given, an enum whose variants hold nothing is a plain enum there,
    /// and an <c>Option</c> there is a nullable.
    /// </summary>
    [SkippableFact]
    public void AnEnumInsideAVariantIsWrittenAndReadWithTheVariant()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var (projection, slicer, scaled) = ((Projection?)null, (SpriteImageModeValue?)null, (SpriteImageModeValue?)null);
        var styles = new List<FontStyle>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            var camera = ctx.Ecs.Insert<ProjectionRef>(ctx.Ecs.Spawn());
            camera.Value = new Projection.Orthographic(0f, 100f, new Vec2(0.5f, 0.5f), new ScalingMode.Fixed(16f, 9f), 2f, new Vec2(-1f, -1f), new Vec2(1f, 1f));
            projection = camera.Value;

            // A slicer, which Bevy registers no default for, made for its variant from its fields'.
            var sprite = ctx.Ecs.Insert<SpriteRef>(ctx.Ecs.Spawn());
            sprite.ImageMode = new SpriteImageModeValue.Sliced(new Vec2(4f, 5f), new Vec2(6f, 7f), new SliceScaleMode.Tile(0.5f), new SliceScaleMode.Stretch(), 0.25f);
            slicer = sprite.ImageMode;
            sprite.ImageMode = new SpriteImageModeValue.Scale(SpriteScalingMode.FitStart);
            scaled = sprite.ImageMode;

            var font = ctx.Ecs.Insert<TextFontRef>(ctx.Ecs.Spawn());
            foreach (var style in new FontStyle[] { new FontStyle.Oblique(0.25f), new FontStyle.Oblique(null), new FontStyle.Italic() })
            {
                font.Style = style;
                styles.Add(font.Style);
            }
        });

        harness.Run();

        Assert.Equal(new Projection.Orthographic(0f, 100f, new Vec2(0.5f, 0.5f), new ScalingMode.Fixed(16f, 9f), 2f, new Vec2(-1f, -1f), new Vec2(1f, 1f)), projection);
        Assert.Equal(new SpriteImageModeValue.Sliced(new Vec2(4f, 5f), new Vec2(6f, 7f), new SliceScaleMode.Tile(0.5f), new SliceScaleMode.Stretch(), 0.25f), slicer);
        Assert.Equal(new SpriteImageModeValue.Scale(SpriteScalingMode.FitStart), scaled);
        Assert.Equal([new FontStyle.Oblique(0.25f), new FontStyle.Oblique(null), new FontStyle.Italic()], styles);
    }

    /// <summary>
    /// A list of records inside a component, as a node's box shadows and its gradients are, is
    /// written whole, growing and shrinking to what it is given, and read back as it was given, a
    /// list inside a variant of an item too.
    /// </summary>
    [SkippableFact]
    public void ListsOfRecordsAreWrittenAndReadWhole()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        IReadOnlyList<ShadowStyle>? two = null, one = null;
        IReadOnlyList<Gradient>? gradients = null;

        ShadowStyle[] shadows =
        [
            new(Color.Black, new Val.Px(4f), new Val.Px(6f), new Val.Percent(10f), new Val.Px(8f)),
            new(Color.White, new Val.Auto(), new Val.Vw(1f), new Val.Px(0f), new Val.Px(2f)),
        ];
        ColorStop[] stops = [new(Color.Black, new Val.Percent(0f), 0.5f), new(Color.White, new Val.Percent(100f), 0.25f)];
        AngularColorStop[] angles = [new(Color.Black, null, 0.5f), new(Color.White, 3f, 0.5f)];

        harness.OnContext(Stage.Startup, ctx =>
        {
            var node = ctx.Ecs.Spawn();
            var shadow = ctx.Ecs.Insert<BoxShadowRef>(node);
            shadow.Value = shadows;
            two = shadow.Value;
            shadow.Value = [shadows[1]];
            one = shadow.Value;

            var background = ctx.Ecs.Insert<BackgroundGradientRef>(node);
            background.Value =
            [
                new Gradient.Linear(InterpolationColorSpace.Oklaba, 0.5f, stops),
                new Gradient.Radial(InterpolationColorSpace.Srgba, new Vec2(0.5f, 0.25f), new Val.Percent(50f), new Val.Px(3f), new RadialGradientShape.Ellipse(new Val.Px(10f), new Val.Px(20f)), [stops[1]]),
                new Gradient.Conic(InterpolationColorSpace.Hsla, 0.25f, Vec2.Zero, new Val.Px(1f), new Val.Px(2f), angles),
            ];
            gradients = background.Value;
        });

        harness.Run();

        Assert.Equal(shadows, two);
        Assert.Equal([shadows[1]], one);

        Assert.NotNull(gradients);
        Assert.Equal(3, gradients.Count);
        var linear = Assert.IsType<Gradient.Linear>(gradients[0]);
        Assert.Equal((InterpolationColorSpace.Oklaba, 0.5f), (linear.ColorSpace, linear.Angle));
        Assert.Equal(stops, linear.Stops);
        var radial = Assert.IsType<Gradient.Radial>(gradients[1]);
        Assert.Equal(new RadialGradientShape.Ellipse(new Val.Px(10f), new Val.Px(20f)), radial.Shape);
        Assert.Equal((new Vec2(0.5f, 0.25f), (Val)new Val.Percent(50f), (Val)new Val.Px(3f)), (radial.PositionAnchor, radial.PositionX, radial.PositionY));
        Assert.Equal([stops[1]], radial.Stops);
        var conic = Assert.IsType<Gradient.Conic>(gradients[2]);
        Assert.Equal((InterpolationColorSpace.Hsla, 0.25f), (conic.ColorSpace, conic.Start));
        Assert.Equal(angles, conic.Stops);
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
    /// An <c>Option</c> of a record written while it is already <c>Some</c> keeps what the record
    /// does not hold, as a sprite's texture atlas keeps its layout when its frame is written.
    /// </summary>
    [SkippableFact]
    public void AnOptionalRecordWrittenAgainKeepsWhatItDoesNotHold()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var layout = AssetHandle.None;
        AssetHandle? kept = AssetHandle.None;
        TextureAtlas? frame = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            layout = Render2d.CreateAtlas(24, 24, 7, 1);
            var sprite = ctx.Ecs.Insert<SpriteRef>(ctx.Ecs.Spawn());
            sprite.TextureAtlas = new TextureAtlas(0);
            ctx.Ecs.SetReflectedAsset(sprite.Entity, SpriteRef.TypePath, ".texture_atlas.0.layout", layout);

            sprite.TextureAtlas = new TextureAtlas(3);
            frame = sprite.TextureAtlas;
            kept = ctx.Ecs.GetReflectedAsset(sprite.Entity, SpriteRef.TypePath, ".texture_atlas.0.layout");
        });

        harness.Run();

        Assert.Equal(new TextureAtlas(3), frame);
        Assert.Equal(layout, kept);
    }

    /// <summary>
    /// A variant holding a handle can be chosen, and a component holding one inserted, which need a
    /// handle made for them before the one written arrives, since Bevy registers no default for a
    /// handle.
    /// </summary>
    [SkippableFact]
    [ExpectsError("bevy", "FiraSans-Bold.ttf")]
    public void AVariantHoldingAHandleIsChosenWithTheHandleWritten()
    {
        Needs.Renderer();

        // No font is among the tests' assets, so the handle names one that is not there, and the run
        // goes on until Bevy has said so, which it says as an error.
        using var harness = new EngineHarness(frames: 0, fps: 240);
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
        harness.OnContext(Stage.Update, ctx =>
        {
            if (loaded.State != AssetLoadState.Loading || ctx.Time.FrameCount > 2400) ctx.Exit();
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

using System.Globalization;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reaching Bevy's components through Bevy's own reflection, and the schemas built from it.
/// </summary>
/// <remarks>
/// A headless bridge reflects the transform and the hierarchy, which is enough to check the values,
/// the failures and the schemas against the mirrors. A light, an enum that carries data and the
/// command line over both need a component only a render build compiles, so those tests return
/// early on a headless bridge, as the visibility test does.
/// </remarks>
[Collection("engine")]
public sealed class ReflectedComponentTests
{
    private const string TransformPath = "bevy_transform::components::transform::Transform";
    private const string ChildOfPath = "bevy_ecs::hierarchy::ChildOf";
    private const string PointLightPath = "bevy_light::point_light::PointLight";

    [Fact]
    public void AWriteThroughReflectionIsReadByTheMirrorAndTheOtherWayRound()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(1f, 2f, 3f));

            // The same bytes, reached two ways. If either path read a copy, one of these would
            // see the value from before the other's write.
            ctx.Ecs.SetReflected(entity, TransformPath, ".translation.y", "5");
            Assert.Equal(new Vec3(1f, 5f, 3f), ctx.Ecs.GetRef<Transform>(entity).Translation);

            ctx.Ecs.GetRef<Transform>(entity).Scale = new Vec3(2f, 2f, 2f);
            var scale = ctx.Ecs.GetReflected(entity, TransformPath, ".scale");
            Assert.Equal([2f, 2f, 2f], Numbers(scale));
        });

        harness.Run();
    }

    [Fact]
    public void AComponentIsPutOnAtItsDefaultAndTakenOff()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            Assert.Null(ctx.Ecs.GetReflected(entity, TransformPath));
            Assert.False(ctx.Ecs.RemoveReflected(entity, TransformPath));

            ctx.Ecs.InsertReflected(entity, TransformPath);
            Assert.Equal(Transform.Identity.Scale, ctx.Ecs.GetRef<Transform>(entity).Scale);

            Assert.True(ctx.Ecs.RemoveReflected(entity, TransformPath));
            Assert.False(ctx.Ecs.Has<Transform>(entity));
        });

        harness.Run();
    }

    [Fact]
    public void ARefusalSaysWhereItStopped()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.Identity);

            // A string path fails when it is used rather than when it is compiled, so the message
            // is all a caller has to go on, and it has to name what was wrong.
            var path = Assert.Throws<BevyNativeException>(() =>
                ctx.Ecs.SetReflected(entity, TransformPath, ".nowhere", "1"));
            Assert.Equal(NativeStatus.InvalidState, path.Status);
            Assert.Contains("nowhere", path.Message);

            var value = Assert.Throws<BevyNativeException>(() =>
                ctx.Ecs.SetReflected(entity, TransformPath, ".translation", "\"up\""));
            Assert.Contains("up", value.Message);

            var type = Assert.Throws<BevyNativeException>(() =>
                ctx.Ecs.GetReflected(entity, "no::such::Component"));
            Assert.Equal(NativeStatus.NoComponent, type.Status);

            // Nothing was written by the refused calls.
            Assert.Equal(Transform.Identity.Translation, ctx.Ecs.GetRef<Transform>(entity).Translation);
        });

        harness.Run();
    }

    [Fact]
    public void AReflectedComponentIsDescribedAndAMirroredOneKeepsItsMirror()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var parent = ctx.Ecs.Spawn();
            var child = ctx.Ecs.Spawn();
            ctx.Ecs.Add(child, Transform.Identity);
            ctx.Ecs.SetParent(child, parent);

            var schemas = ctx.Ecs.ComponentsOf(child).Select(ComponentSchemas.For).ToArray();

            // The transform is still described by its mirror, so the inspector writes it in place.
            var transform = Assert.Single(schemas, schema => schema?.Name == "Transform");
            Assert.Equal(SchemaOrigin.Mirrored, transform!.Origin);
            Assert.DoesNotContain(
                ComponentSchemas.All, schema => schema.QualifiedName == TransformPath);

            // The hierarchy has no mirror, so it is described from Bevy's reflection. It is a
            // newtype over an entity, and immutable, so writing it inserts it again, which moves
            // the child under the new parent as Bevy's own insert does.
            var childOf = Assert.Single(schemas, schema => schema?.QualifiedName == ChildOfPath);
            Assert.Equal(SchemaOrigin.Reflected, childOf!.Origin);

            var link = Assert.Single(childOf.Fields);
            Assert.Equal(FieldKind.Entity, link.Kind);
            Assert.Equal(parent, link.Read(ctx.Ecs, child));

            var other = ctx.Ecs.Spawn();
            Assert.True(link.Write(ctx.Ecs, child, other));
            Assert.Equal(other, link.Read(ctx.Ecs, child));
            Assert.Contains(child, ctx.Ecs.ChildrenOf(other));
        });

        harness.Run();
    }

    [Fact]
    public void ReflectedSchemasBelongToOneApp()
    {
        // Ids belong to a world, so the schemas described from one app cannot be the ones a second
        // app is answered with, or a component would be read through another world's id.
        ComponentSchema? first = null;
        ComponentSchema? second = null;

        using (var harness = new EngineHarness(frames: 2))
        {
            harness.OnContext(Stage.Startup, _ => first = ComponentSchemas.For(ChildOfPath));
            harness.Run();
        }

        using (var harness = new EngineHarness(frames: 2))
        {
            harness.OnContext(Stage.Startup, _ => second = ComponentSchemas.For(ChildOfPath));
            harness.Run();
        }

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void TheCommandLineReachesAFieldInsideANestedStruct()
    {
        // Splitting at the last dot named the component "Sprung.Front" and found nothing.
        string? answer = null;
        var damping = 0f;

        using var harness = new EngineHarness(frames: 2);

        // Once, because the command finds the entity by name and a second frame would spawn a
        // second one called the same.
        harness.On(Stage.Update, world =>
        {
            if (answer is not null) return;

            var ecs = world.Resource<EcsWorld>();
            var entity = ecs.Spawn();
            ecs.SetName(entity, "Sprung");
            ecs.Add(entity, new Sprung { Mass = 1f });

            using (ConsoleHost.Lend(world))
                answer = ConsoleCommands.Run("entity.set Sprung Sprung.Front.Damping 0.5");

            damping = ecs.GetOrDefault<Sprung>(entity).Front.Damping;
        });

        harness.Run();

        Assert.Contains("Sprung.Front.Damping = 0.5", answer);
        Assert.Equal(0.5f, damping);
    }

    [SkippableFact]
    public void AnOptionalHandleIsGivenAnAssetThroughReflection()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        string? before = null, after = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            // A fog volume's density texture is an Option<Handle<Image>>, None to begin with.
            const string Volume = "bevy_light::volumetric::FogVolume";
            var fog = ctx.Ecs.Spawn();
            ctx.Ecs.InsertReflected(fog, Volume);
            before = ctx.Ecs.GetVariant(fog, Volume, ".density_texture");

            ctx.Ecs.SetReflectedAsset(fog, Volume, ".density_texture", Render.CreateImage(new byte[4 * 4 * 4], 4, 4));
            after = ctx.Ecs.GetVariant(fog, Volume, ".density_texture");
        });

        harness.Run();

        Assert.Equal("None", before);
        Assert.Equal("Some", after);
    }

    /// <summary>
    /// A component holding a range of numbers is written from JSON and read back, which needs the
    /// range's serde data that Bevy's reflection leaves out and the bridge registers.
    /// </summary>
    [SkippableFact]
    public void AComponentHoldingARangeIsWrittenFromJson()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        string? end = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            const string Range = "bevy_camera::visibility::range::VisibilityRange";
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.InsertReflected(entity, Range,
                """{"start_margin":{"start":0,"end":0},"end_margin":{"start":3,"end":4},"use_aabb":false}""");
            // A range is opaque to a path into it, so it is written whole.
            ctx.Ecs.SetReflected(entity, Range, ".end_margin", """{"start":3,"end":5}""");
            end = ctx.Ecs.GetReflected(entity, Range, ".end_margin");
        });

        harness.Run();

        Assert.NotNull(end);
        Assert.Contains("\"start\":3", end, StringComparison.Ordinal);
        Assert.Contains("\"end\":5", end, StringComparison.Ordinal);
    }

    [SkippableFact]
    public void APointLightIsSetThroughReflectionAndReadBackThroughBevy()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.On(Stage.Update, world =>
        {
            if (ran) return;

            var ecs = world.Resource<EcsWorld>();
            var entity = ecs.Spawn();
            ecs.SetName(entity, "Lamp");
            ecs.InsertReflected(entity, PointLightPath);

            ecs.SetReflected(entity, PointLightPath, ".intensity", "5000");
            Assert.Equal(5000f, float.Parse(
                ecs.GetReflected(entity, PointLightPath, ".intensity")!,
                CultureInfo.InvariantCulture));

            var schema = ecs.ComponentsOf(entity)
                .Select(ComponentSchemas.For)
                .Single(schema => schema?.QualifiedName == PointLightPath)!;
            Assert.Equal("PointLight", schema.Name);
            Assert.True(schema.CanAdd);

            var intensity = schema.Field("intensity")!;
            Assert.Equal(FieldKind.Float, intensity.Kind);
            Assert.Equal("Intensity", intensity.Title);
            Assert.Equal(5000f, intensity.Read(ecs, entity));
            Assert.True(intensity.Write(ecs, entity, 100.0));
            Assert.Equal(100f, intensity.Read(ecs, entity));

            // Bevy's Color holds a color in any of ten spaces. It is one swatch row, read as linear
            // whatever space it is in, and written back in the space it was in.
            var color = schema.Field("color")!;
            Assert.Equal(FieldKind.Color, color.Kind);
            Assert.DoesNotContain(schema.Fields, field => field.Name.StartsWith("color.", StringComparison.Ordinal));

            var orange = Color.FromHex("#ff8800");
            Assert.True(color.Write(ecs, entity, orange));
            var read = (Color)color.Read(ecs, entity)!;
            Assert.Equal(orange.R, read.R, 4);
            Assert.Equal(orange.G, read.G, 4);
            Assert.Equal(orange.B, read.B, 4);

            // The command line goes through the same schema, by Bevy's own lower-case name or the
            // Pascal case a C# field would have.
            using (ConsoleHost.Lend(world))
            {
                var answer = ConsoleCommands.Run("entity.set Lamp PointLight.Intensity 42");
                Assert.Contains("PointLight.Intensity = 42", answer);
            }

            Assert.Equal(42f, intensity.Read(ecs, entity));
            ran = true;
        });

        harness.Run();

        Assert.True(ran);
    }

    [SkippableFact]
    public void AMeshHandleIsReadAsTheKeyItWasGivenAndWrittenAsAnother()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f);
            var ball = Render.CreateMesh(MeshShape.Sphere, 0.5f);
            Render.SetMesh(ctx.Ecs, entity, cube);

            var schema = ctx.Ecs.ComponentsOf(entity)
                .Select(ComponentSchemas.For)
                .Single(schema => schema?.Name == "Mesh3d")!;

            // A newtype over a handle, so its one row is the handle, picked from meshes.
            var mesh = Assert.Single(schema.Fields);
            Assert.Equal(FieldKind.Asset, mesh.Kind);
            Assert.Equal(AssetKind.Mesh, mesh.Hints.Asset);

            // The key the program already holds comes back, rather than a second one for the same
            // mesh, which would leave the table growing a slot a frame under an inspector.
            Assert.Equal(cube, mesh.Read(ctx.Ecs, entity));
            Assert.Equal(cube, mesh.Read(ctx.Ecs, entity));

            Assert.True(mesh.Write(ctx.Ecs, entity, ball));
            Assert.Equal(ball, ctx.Ecs.GetReflectedAsset(entity, schema.QualifiedName, ".0"));

            // A handle has no empty state to write, and an image is not a mesh.
            Assert.False(mesh.Write(ctx.Ecs, entity, AssetHandle.None));
            var image = Render.CreateImage([255, 255, 255, 255], 1, 1);
            Assert.False(mesh.Write(ctx.Ecs, entity, image));
            Assert.Equal(ball, mesh.Read(ctx.Ecs, entity));
            ran = true;
        });

        harness.Run();

        Assert.True(ran);
    }

    [SkippableFact]
    public void AnEditorBuildCarriesBevysDocumentationAsTooltips()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        string? tooltip = "unread";

        harness.OnContext(Stage.Startup, ctx =>
        {
            tooltip = ComponentSchemas.For(PointLightPath)!.Field("intensity")!.Hints.Tooltip;
        });

        harness.Run();

        // Only the editor profile keeps Bevy's doc comments, so a game's library is no larger for
        // them and its rows have no tooltip to show.
        if (App.HasEditor)
        {
            Assert.False(string.IsNullOrWhiteSpace(tooltip));
            Assert.DoesNotContain("\n", tooltip);
        }
        else
        {
            Assert.Null(tooltip);
        }
    }

    [SkippableFact]
    public void ARowOfAVariantNotHeldReadsNothingAndRefusesAWrite()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            const string Sun = "bevy_light::directional_light::DirectionalLight";
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.InsertReflected(entity, Sun);

            var schema = ComponentSchemas.For(Sun)!;
            var size = schema.Field("soft_shadow_size")!;
            var some = schema.Field("soft_shadow_size.Some")!;

            // An Option is an enum with data: a row choosing the variant, and the variant's value
            // as a row shown only while it is the one held.
            Assert.Equal(FieldKind.Enum, size.Kind);
            Assert.Equal(["None", "Some"], size.Options);
            Assert.Contains(new FieldCondition("soft_shadow_size", "Some"), some.Hints.Conditions);

            // Bevy's path to the value does not name the variant, so without a guard the row would
            // read whatever the path happened to reach. While the option is None it reads nothing
            // and refuses a write rather than landing anywhere.
            Assert.Equal("None", size.Read(ctx.Ecs, entity));
            Assert.Null(some.Read(ctx.Ecs, entity));
            Assert.False(some.Write(ctx.Ecs, entity, 2.5f));

            Assert.True(size.Write(ctx.Ecs, entity, "Some"));
            Assert.Equal(0f, some.Read(ctx.Ecs, entity));
            Assert.True(some.Write(ctx.Ecs, entity, 2.5f));
            Assert.Equal(2.5f, some.Read(ctx.Ecs, entity));
            ran = true;
        });

        harness.Run();

        Assert.True(ran);
    }

    /// <summary>The numbers of a JSON array, read without a serializer.</summary>
    private static float[] Numbers(string? json) =>
        json!.Trim('[', ']')
            .Split(',')
            .Select(part => float.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
}

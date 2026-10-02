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
            // newtype over an entity, and immutable, so it reads and refuses to be written.
            var childOf = Assert.Single(schemas, schema => schema?.QualifiedName == ChildOfPath);
            Assert.Equal(SchemaOrigin.Reflected, childOf!.Origin);

            var link = Assert.Single(childOf.Fields);
            Assert.Equal(FieldKind.Entity, link.Kind);
            Assert.Equal(parent, link.Read(ctx.Ecs, child));
            Assert.False(link.Write(ctx.Ecs, child, ctx.Ecs.Spawn()));
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

    [Fact]
    public void APointLightIsSetThroughReflectionAndReadBackThroughBevy()
    {
        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.On(Stage.Update, world =>
        {
            if (!App.HasRenderer || ran) return;

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

            // Color is an enum that carries data. Choosing a variant is one row, and the variant's
            // own numbers are rows shown only while it is the one chosen.
            var color = schema.Field("color")!;
            Assert.Equal(FieldKind.Enum, color.Kind);
            Assert.Contains("LinearRgba", color.Options);

            var red = schema.Field("color.LinearRgba.red")!;
            Assert.Equal(FieldKind.Float, red.Kind);
            Assert.Contains(new FieldCondition("color", "LinearRgba"), red.Hints.Conditions);

            Assert.True(color.Write(ecs, entity, "LinearRgba"));
            Assert.Equal("LinearRgba", color.Read(ecs, entity));
            Assert.True(red.Write(ecs, entity, 0.25f));
            Assert.Equal(0.25f, red.Read(ecs, entity));

            // Bevy's path to the red of an Srgba is the same as to the red of a LinearRgba, since it
            // does not name the variant. The row of the variant not held reads nothing rather than
            // the other one's red, and refuses a write rather than landing on it.
            var srgbRed = schema.Field("color.Srgba.red")!;
            Assert.Null(srgbRed.Read(ecs, entity));
            Assert.False(srgbRed.Write(ecs, entity, 1f));
            Assert.Equal(0.25f, red.Read(ecs, entity));

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

        if (App.HasRenderer) Assert.True(ran);
    }

    [Fact]
    public void AMeshHandleIsReadAsTheKeyItWasGivenAndWrittenAsAnother()
    {
        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (!App.HasRenderer) return;

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

        if (App.HasRenderer) Assert.True(ran);
    }

    [Fact]
    public void AnEditorBuildCarriesBevysDocumentationAsTooltips()
    {
        using var harness = new EngineHarness(frames: 2);
        string? tooltip = "unread";

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (!App.HasRenderer) return;

            tooltip = ComponentSchemas.For(PointLightPath)!.Field("intensity")!.Hints.Tooltip;
        });

        harness.Run();

        if (!App.HasRenderer) return;

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

    /// <summary>The numbers of a JSON array, read without a serializer.</summary>
    private static float[] Numbers(string? json) =>
        json!.Trim('[', ']')
            .Split(',')
            .Select(part => float.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
}

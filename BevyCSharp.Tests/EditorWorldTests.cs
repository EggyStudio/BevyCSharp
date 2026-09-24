using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>Something to save and read back, so a round trip has values to compare.</summary>
[Behavior]
public partial struct Saved
{
    /// <summary>A number.</summary>
    public float Weight;

    /// <summary>A whole number beside it.</summary>
    public int Count;
}

/// <summary>
/// Covers the world file, which is what the editor saves of a scene.
/// </summary>
/// <remarks>
/// The file is a set of edits over a scene rather than the scene, so what is checked is that an
/// entity found by name comes back holding what it held. The engine's own components have no
/// schema, so the mesh and the material are written by where they were loaded from, which is the
/// only thing about them this side can name.
/// </remarks>
[Collection("engine")]
public sealed class EditorWorldTests
{
    [Fact]
    public void WhatWasSavedIsWhatComesBack()
    {
        var file = Path.Combine(Path.GetTempPath(), $"bcs-world-{Guid.NewGuid():N}.json");

        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            ctx.Ecs.SetName(entity, "Saved thing");
            ctx.Ecs.Add(entity, new Saved { Weight = 2.5f, Count = 7 });

            Assert.True(EditorWorld.Save(ctx.Ecs, file) > 0);

            // Changed under the file, which is what loading is for.
            ctx.Ecs.Add(entity, new Saved { Weight = 0f, Count = 0 });

            Assert.True(EditorWorld.Load(ctx.Ecs, file) > 0);

            var back = ctx.Ecs.GetOrDefault<Saved>(entity);

            Assert.Equal(2.5f, back.Weight, 4);
            Assert.Equal(7, back.Count);
        });

        harness.Run();

        File.Delete(file);
    }

    /// <summary>A mesh loaded from a file is written by its path and pointed at again.</summary>
    /// <remarks>
    /// The half of an entity the world file could not carry. A mesh built in memory still cannot
    /// be written, because a set of numbers has no name, and the test says so by leaving the
    /// material out of what it expects back.
    /// </remarks>
    [Fact]
    public void AMeshLoadedFromAFileSurvivesTheRoundTrip()
    {
        if (!App.HasRenderer) return;

        var file = Path.Combine(Path.GetTempPath(), $"bcs-world-{Guid.NewGuid():N}.json");

        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            ctx.Ecs.SetName(entity, "Drawn thing");

            Render.SetMesh(
                ctx.Ecs,
                entity,
                AssetServer.Load(AssetKind.Mesh, "models/triangle.gltf#Mesh0/Primitive0"));

            Assert.True(EditorWorld.Save(ctx.Ecs, file) > 0);

            // Pointed somewhere else, so what comes back has to have been read rather than left.
            Render.SetMesh(ctx.Ecs, entity, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
            Assert.Equal(string.Empty, Render.MeshPathOf(entity));

            Assert.True(EditorWorld.Load(ctx.Ecs, file) > 0);
            Assert.Contains("triangle.gltf", Render.MeshPathOf(entity));
        });

        harness.Run();

        File.Delete(file);
    }

    /// <summary>An entity that is only drawn is still worth writing.</summary>
    /// <remarks>
    /// Before the mesh was written, an entity with no component of this project's own was skipped
    /// entirely, because there was nothing to say about it. What it is drawn with is something.
    /// </remarks>
    [Fact]
    public void AnEntityWithNothingButAMeshIsStillSaved()
    {
        if (!App.HasRenderer) return;

        var file = Path.Combine(Path.GetTempPath(), $"bcs-world-{Guid.NewGuid():N}.json");

        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            ctx.Ecs.SetName(entity, "Only drawn");

            Render.SetMesh(
                ctx.Ecs,
                entity,
                AssetServer.Load(AssetKind.Mesh, "models/triangle.gltf#Mesh0/Primitive0"));

            Assert.Equal(1, EditorWorld.Save(ctx.Ecs, file));
        });

        harness.Run();

        File.Delete(file);
    }
}

// Bevy's asset_loading example, examples/asset/asset_loading.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows how to load meshes from glTF files, which happens in parallel without blocking, and how a
// material made in code is put in Bevy's assets beside them.
//
// Bevy's also loads a whole folder in one call, which the asset server here has no call for, so
// this is written in part, the torus loaded by its path as Bevy's loads it after the folder.
internal static class AssetLoading
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        var cube = AssetServer.LoadGltfMesh("models/cube/cube.gltf");
        var sphere = AssetServer.LoadGltfMesh("models/sphere/sphere.gltf");

        // Loading has only begun, so the sphere is not there yet, as Bevy's says.
        Console.WriteLine(AssetServer.StateOf(sphere) == AssetLoadState.Loaded ? "sphere has loaded" : "sphere hasn't loaded yet");

        var torus = AssetServer.LoadGltfMesh("models/torus/torus.gltf");
        var material = Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f));

        ecs.SpawnMesh(torus, material, Transform.At(-3f, 0f, 0f));
        ecs.SpawnMesh(cube, material, Transform.Identity);
        ecs.SpawnMesh(sphere, material, Transform.At(3f, 0f, 0f));

        ecs.SpawnPointLight(new Vec3(4f, 5f, 4f));
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 3f, 10f), Vec3.Zero, Vec3.UnitY));
    }, "asset_loading.Setup");
}

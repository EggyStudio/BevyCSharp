// Bevy's multi_asset_sync example, examples/asset/multi_asset_sync.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows waiting for a hundred loads at once before using any of them: a red floor and "Loading..."
// until every model is in, then a hundred models standing on a green floor and "Loaded!".
//
// Bevy counts the loads down with a barrier its loads hold a guard of, and a task waiting on it
// flips a flag the app reads. Here the app asks each load's state, and the same flag is set by a
// task waiting on the count.
internal static class MultiAssetSync
{
    private static readonly string[] Models =
    [
        "models/GolfBall/GolfBall.glb", "models/AlienCake/alien.glb", "models/AlienCake/cakeBirthday.glb",
        "models/FlightHelmet/FlightHelmet.gltf", "models/torus/torus.gltf",
    ];

    private static readonly AssetHandle[] Things = new AssetHandle[100];
    private static TaskCompletionSource? _barrier;
    private static Task? _waiting;
    private static volatile bool _loaded;
    private static Entity _redFloor, _text;
    private static bool _spawned;

    public static void Build(App app)
    {
        (_loaded, _spawned) = (false, false);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            for (var i = 0; i < Things.Length; i++) Things[i] = AssetServer.LoadGltfScene(Models[i % Models.Length]);

            // A task that waits for the barrier to open, as Bevy's waits on its future.
            _barrier = new TaskCompletionSource();
            _waiting = _barrier.Task.ContinueWith(_ => _loaded = true);

            Render.SetAmbientLight((1f, 1f, 1f), 2000f);
            _text = Ui.SpawnText("Loading...", new UiSettings { Absolute = true, Left = Length.Px(12f), Top = Length.Px(12f) });

            ecs.Camera(Transform.LookingAt(new Vec3(10f, 10f, 15f), Vec3.Zero, Vec3.UnitY));
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            _redFloor = ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 50_000f, 50_000f), Scene.Material(Scene.Srgb(0.7f, 0.2f, 0.2f)), Transform.Identity);
        }, "multi_asset_sync.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;

            // The barrier opens once every load is in.
            if (_barrier is { Task.IsCompleted: false } barrier && Things.All(thing => AssetServer.StateOf(thing) == AssetLoadState.Loaded))
                barrier.SetResult();

            if (!_loaded || _spawned) return;
            _spawned = true;

            ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 50_000f, 50_000f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, 0f, -0.01f));
            for (var i = 0; i < 10; i++)
            {
                for (var j = 0; j < 10; j++)
                {
                    var root = ecs.SpawnScene(Things[i * 10 + j]);
                    ecs.Set(root, Transform.At(i - 5f, 0f, j - 5f));
                }
            }

            Ui.SetText(_text, "Loaded!");
            ecs.Despawn(_redFloor);
        }, "multi_asset_sync.WaitOnLoad");
    }
}

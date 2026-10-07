// Bevy's multiple_windows example, examples/window/multiple_windows.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// Uses two windows to visualize a 3D model from different angles. The second window is Bevy's
// Window on an entity of its own, opened once it is spawned, and the camera aimed at it draws there
// while the first camera draws in the first window. An interface is drawn by one camera, so each
// window's label names the camera of its window.
internal static class MultipleWindows
{
    public static void Build(App app) => app.Startup(SetupScene, "multiple_windows.SetupScene");

    private static void SetupScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        ecs.SpawnScene(AssetServer.LoadGltfScene("models/torus/torus.gltf", 0));

        // Bevy's DirectionalLight::default(), which casts no shadows.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(light, Transform.LookingAt(new Vec3(3f, 3f, 3f), Vec3.Zero, Vec3.UnitY));

        var firstWindowCamera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));

        var secondWindow = ecs.Spawn();
        ecs.Insert<WindowRef>(secondWindow).Title = "Second window";

        var secondWindowCamera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(6f, 0f, 0f), Vec3.Zero, Vec3.UnitY));
        Render.SetCameraTarget(secondWindowCamera, secondWindow);

        Label(ecs, "First window", firstWindowCamera);
        Label(ecs, "Second window", secondWindowCamera);
    }

    // A shadowed line of text in the corner of the window the camera draws.
    private static void Label(EcsWorld ecs, string text, Entity camera)
    {
        var node = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f), Camera = camera });
        var label = Ui.SpawnText(text, new UiSettings());
        ecs.Insert<TextShadowRef>(label);
        ecs.SetParent(label, node);
    }
}

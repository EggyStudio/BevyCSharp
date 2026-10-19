// Bevy's occlusion_culling example, examples/3d/occlusion_culling.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Small cubes on a sphere turning around a large cube that turns as well, so the large cube always
// hides some of them. With occlusion culling on, the GPU leaves out what the last frame's depth
// says is hidden, and the count at the top left, read back from the GPU's draws, is how many it drew
// of all the meshes there are. The buttons at the bottom left turn it off and on.
internal static class OcclusionCullingExample
{
    private const float OuterRadius = 3f;
    private const float RotationSpeed = 0.01f;
    private const float SmallCubeSize = 0.1f;
    private const float LargeCubeSize = 2f;

    private static Entity _sphereParent, _largeCube, _camera, _status;
    private static int _meshes;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        app.Startup(Setup, "occlusion_culling.Setup");
        app.Update(SpinSmallCubes, "occlusion_culling.SpinSmallCubes");
        app.Update(SpinLargeCube, "occlusion_culling.SpinLargeCube");
        app.Update(UpdateStatusText, "occlusion_culling.UpdateStatusText");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's spawn_small_cubes, a cube at each vertex of a sphere subdivided five times.
        var smallCube = Render.CreateMesh(MeshShape.Cuboid, SmallCubeSize, SmallCubeSize, SmallCubeSize);
        var silver = Render.CreateMaterial(Color.FromSrgb(0.75f, 0.75f, 0.75f));
        _sphereParent = ecs.Spawn();
        ecs.Add(_sphereParent, Transform.Identity);
        ecs.Insert<VisibilityRef>(_sphereParent);

        Render.TryReadMesh(Render.CreateMesh(MeshShape.IcoSphere, OuterRadius, 5f), out var sphere);
        foreach (var position in sphere!.Positions)
        {
            var cube = ecs.SpawnMesh(smallCube, silver, new Transform(position));
            ecs.SetParent(cube, _sphereParent);
        }

        // Bevy's spawn_large_cube, the Bevy icon on each face.
        _largeCube = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, LargeCubeSize, LargeCubeSize, LargeCubeSize),
            Render.CreateMaterial(new MaterialSettings { BaseColorTexture = AssetServer.Load(AssetKind.Image, "branding/icon.png") }),
            Transform.Identity);
        _meshes = sphere.Positions.Length + 1;

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationZ(0f) * Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));

        // Bevy's spawn_camera, with the depth prepass occlusion culling reads the last frame from.
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 9f), Vec3.Zero, Vec3.UnitY));
        ecs.Insert<DepthPrepassRef>(_camera);
        ecs.Insert<OcclusionCullingRef>(_camera);
        Render.CountDrawnMeshes(true);

        _status = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        Theme.BasicExampleTheme(Color.White);
        var row = Radio.FeathersOptionButtons(ecs, Radio.MainUiNode(), "Toggle occlusion culling", [(true, "ON"), (false, "OFF")], 0);
        Ui.SelfUpdate(row, UiWidgetKind.RadioGroup);
        ecs.Observe<ValueChange<Entity>>(on => HandleSelectionChange(on.Ecs, on.Event.Value));
    }

    // Bevy's spin_small_cubes and spin_large_cube, by a step each frame as Bevy's turn.
    private static void SpinSmallCubes(BehaviorContext ctx)
    {
        ref var transform = ref ctx.Ecs.GetRef<Transform>(_sphereParent);
        transform.Rotation = Quat.FromRotationY(RotationSpeed) * transform.Rotation;
    }

    private static void SpinLargeCube(BehaviorContext ctx)
    {
        ref var transform = ref ctx.Ecs.GetRef<Transform>(_largeCube);
        var step = Quat.FromRotationX(0.13f * RotationSpeed) * Quat.FromRotationY(0.29f * RotationSpeed) * Quat.FromRotationZ(0.35f * RotationSpeed);
        transform.Rotation = step * transform.Rotation;
    }

    // Bevy's update_status_text, how many of the meshes the GPU drew, or why that cannot be said.
    private static void UpdateStatusText(BehaviorContext ctx)
    {
        var text = Render.DrawnMeshes(out var drawn) switch
        {
            DrawnMeshCount.NoGpuCulling => "Occlusion culling not supported on this platform",
            DrawnMeshCount.Known => $"{drawn}/{_meshes} meshes rendered",
            _ => string.Empty,
        };
        Ui.SetText(_status, text);
    }

    // Bevy's handle_selection_change, the culling and its prepass put on the camera or taken off.
    private static void HandleSelectionChange(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<bool>(button, out var culling)) return;

        if (culling)
        {
            ecs.Insert<DepthPrepassRef>(_camera);
            ecs.Insert<OcclusionCullingRef>(_camera);
        }
        else
        {
            ecs.Wrap<DepthPrepassRef>(_camera).Remove();
            ecs.Wrap<OcclusionCullingRef>(_camera).Remove();
        }
    }
}

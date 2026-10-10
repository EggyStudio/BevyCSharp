// Bevy's irradiance_volumes example, examples/3d/irradiance_volumes.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using BevyCSharp.Examples.Animations;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates irradiance volumes, which light what is inside a box from a grid of points baked
// into a 3D image. A silver sphere, or Bevy's fox, stands in a room lit by one, the camera turning
// round it. A click moves it, Backspace shows the volume's voxels as cubes in the light each holds,
// Space turns the volume off for flat ambient light, Enter stops the turning and Tab swaps the
// sphere for the fox.
internal static class IrradianceVolumes
{
    private const float RotationSpeed = 0.2f;
    private const float FoxScale = 0.05f;
    private const float SphereScale = 2f;
    private const float IrradianceVolumeIntensity = 1800f;
    private const float AmbientLightBrightness = 0.06f;
    private const float VoxelCubeScale = 0.4f;

    // Bevy's AppStatus resource.
    private static bool _volumePresent, _rotating, _voxelsVisible, _fox;

    private static Entity _camera, _volume, _text;
    private static AssetHandle _voxels;

    public static void Build(App app)
    {
        (_volumePresent, _rotating, _voxelsVisible, _fox) = (true, true, false, false);

        app.Startup(Setup, "irradiance_volumes.Setup");
        app.Update(CreateCubes, "irradiance_volumes.CreateCubes");
        app.Update(RotateCamera, "irradiance_volumes.RotateCamera");
        app.Update(HandleInput, "irradiance_volumes.HandleInput");
        app.Update(DrawGizmo, "irradiance_volumes.DrawGizmo");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // No ambient light while the volume lights the room.
        Render.SetAmbientLight((1f, 1f, 1f), 0f);

        ecs.SpawnScene(AssetServer.LoadGltfScene("models/IrradianceVolumeExample/IrradianceVolumeExample.glb", 0));

        // Bevy's specular map of Pisa as the sky, since it is not too blurry.
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-10.012f, 4.8605f, 13.281f), Vec3.Zero, Vec3.UnitY));
        Render.SetSkybox(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"), 150f);

        // The volume's box is Bevy's VOXEL_FROM_WORLD matrix, whose columns are (-42.317566, 0, 0),
        // (0, 0, 44.601563) and (0, 16.73776, 0) over a translation of (0, 6.544792, 0). That is a
        // half turn about the line between Y and Z, which swaps them and turns X round, over a
        // scale of each column's length.
        _voxels = AssetServer.Load(AssetKind.Image, "irradiance_volumes/Example.vxgi.ktx2");
        _volume = ecs.Spawn();
        ecs.Add(_volume, new Transform(
            new Vec3(0f, 6.544792f, 0f),
            Quat.FromAxisAngle(new Vec3(0f, 1f, 1f).Normalized, MathF.PI),
            new Vec3(42.317566f, 44.601563f, 16.73776f)));
        Render.SetIrradianceVolume(_volume, _voxels, IrradianceVolumeIntensity);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 250_000f, Shadows = true });
        ecs.Add(light, Transform.At(4.0762f, 5.9039f, 1.0055f));

        // Bevy's default sphere, of radius a half, made of slices and rings.
        var sphere = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.UvSphere, 0.5f, 32f, 18f),
            Render.CreateMaterial(Color.FromSrgb8(192, 192, 192)),
            new Transform(new Vec3(0f, SphereScale, 0f), Quat.Identity, new Vec3(SphereScale)));
        ecs.Add(sphere, new MainObject());

        var voxelParent = ecs.Spawn();
        ecs.Add(voxelParent, Transform.Identity);
        ecs.Insert<VisibilityRef>(voxelParent).Value = Visibility.Hidden;
        ecs.Add(voxelParent, new VoxelCubeParent());

        // The fox, hidden until Tab, running its second clip as Bevy's plays it.
        var fox = ecs.SpawnScene(AssetServer.LoadGltfScene("models/animated/Fox.glb", 0));
        ecs.Set(fox, new Transform(Vec3.Zero, Quat.Identity, new Vec3(FoxScale)));
        ecs.Wrap<VisibilityRef>(fox).Value = Visibility.Hidden;
        ecs.Add(fox, new MainObject());
        ecs.Add(fox, new AnimationToPlay { Clip = 1 });

        _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
    }

    // A cube for each voxel of the volume, once its image has loaded and says how many there are,
    // under the parent that shows or hides them all.
    private static void CreateCubes(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ecs.EntitiesWith<VoxelCube>().Length > 0) return;
        if (!Render.TryImageSize(_voxels, out var width, out var height, out var depth)) return;

        var parents = ecs.EntitiesWith<VoxelCubeParent>();
        if (parents.Length == 0) return;

        var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/irradiance_volume_voxel_visualization.slang"))
            .Set("intensity", IrradianceVolumeIntensity);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var box = ecs.GetOrDefault<Transform>(_volume);

        for (var z = 0u; z < depth; z++)
        for (var y = 0u; y < height; y++)
        for (var x = 0u; x < width; x++)
        {
            // The middle of the voxel in the box's own unit cube, then in the world.
            var uvw = new Vec3((x + 0.5f) / width, (y + 0.5f) / height, (z + 0.5f) / depth) - new Vec3(0.5f);
            var at = box.Translation + box.Rotation * new Vec3(box.Scale.X * uvw.X, box.Scale.Y * uvw.Y, box.Scale.Z * uvw.Z);

            var voxel = ecs.SpawnMesh(cube, material, new Transform(at, Quat.Identity, new Vec3(VoxelCubeScale)));
            ecs.Add(voxel, new VoxelCube());
            ecs.Insert<NotShadowCasterRef>(voxel);
            ecs.SetParent(voxel, parents[0]);
        }
    }

    // The camera round the middle at a fifth of a radian a second, while it turns.
    private static void RotateCamera(BehaviorContext ctx)
    {
        if (!_rotating) return;

        var angle = RotationSpeed * ctx.Time.Delta;
        var at = ctx.Ecs.GetOrDefault<Transform>(_camera).Translation;
        var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
        var turned = new Vec3(at.X * cos - at.Z * sin, at.Y, at.X * sin + at.Z * cos);
        ctx.Ecs.Set(_camera, Transform.LookingAt(turned, Vec3.Zero, Vec3.UnitY));
    }

    private static void HandleInput(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var input = ctx.Input;
        var changed = false;

        // The sphere and the fox swap, one shown and the other hidden.
        if (input.KeyPressed(Key.Tab))
        {
            _fox = !_fox;
            foreach (var main in ecs.EntitiesWith<MainObject>())
            {
                var isFox = ecs.Has<AnimationToPlay>(main);
                ecs.Wrap<VisibilityRef>(main).Value = isFox == _fox ? Visibility.Visible : Visibility.Hidden;
            }
            changed = true;
        }

        // The volume off, and flat ambient light of the brightness it gave in its place, or back.
        if (input.KeyPressed(Key.Space))
        {
            _volumePresent = !_volumePresent;
            Render.SetIrradianceVolume(_volume, _volumePresent ? _voxels : AssetHandle.None, IrradianceVolumeIntensity);
            Render.SetAmbientLight((1f, 1f, 1f), _volumePresent ? 0f : AmbientLightBrightness * IrradianceVolumeIntensity);
            changed = true;
        }

        if (input.KeyPressed(Key.Backspace))
        {
            _voxelsVisible = !_voxelsVisible;
            foreach (var parent in ecs.EntitiesWith<VoxelCubeParent>())
                ecs.Wrap<VisibilityRef>(parent).Value = _voxelsVisible ? Visibility.Visible : Visibility.Hidden;
            changed = true;
        }

        if (input.KeyPressed(Key.Enter)) (_rotating, changed) = (!_rotating, true);

        // A press anywhere moves the sphere and the fox to where it points on the ground.
        if (input.MouseDown(MouseButton.Left))
        {
            var (x, y) = input.MousePosition;
            if (Render.TryRay(_camera, x, y, out var origin, out var direction) && MathF.Abs(direction.Y) > 1e-6f)
            {
                var along = -origin.Y / direction.Y;
                if (along >= 0f)
                {
                    var hit = origin + direction * along;
                    foreach (var main in ecs.EntitiesWith<MainObject>())
                    {
                        var transform = ecs.GetOrDefault<Transform>(main);
                        transform.Translation = new Vec3(hit.X, transform.Translation.Y, hit.Z);
                        ecs.Set(main, transform);
                    }
                }
            }
        }

        if (changed) Ui.SetText(_text, HelpText());
    }

    // The volume's box in yellow while the voxels are shown.
    private static void DrawGizmo(BehaviorContext ctx)
    {
        if (!_voxelsVisible) return;
        var box = ctx.Ecs.GetOrDefault<GlobalTransform>(_volume);
        Gizmos.Box(box.Translation, box.Rotation, box.Scale, Color.FromSrgb8(255, 255, 0));
    }

    private static string HelpText() => string.Join('\n',
        "Left click: Move the object",
        _voxelsVisible ? "Backspace: Hide the voxels" : "Backspace: Show the voxels",
        _volumePresent ? "Space: Disable the irradiance volume" : "Space: Enable the irradiance volume",
        _rotating ? "Enter: Stop rotation" : "Enter: Start rotation",
        _fox ? "Tab: Switch to a plain sphere mesh" : "Tab: Switch to a skinned mesh");
}

/// <summary>The sphere or the fox, whichever is shown, which a click moves.</summary>
[Behavior]
public partial struct MainObject;

/// <summary>A cube drawn at one voxel of the volume, in the light it holds.</summary>
[Behavior]
public partial struct VoxelCube;

/// <summary>The parent of the voxels' cubes, which shows or hides them all.</summary>
[Behavior]
public partial struct VoxelCubeParent;

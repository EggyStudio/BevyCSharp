// Bevy's visibility_range example, examples/3d/visibility_range.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates visibility ranges, the flight helmet drawn from its high-poly model close up and its
// low-poly one further away, faded one into the other as the camera crosses between them.
internal static class VisibilityRange
{
    private static readonly Vec3 FocalPoint = new(0f, 0.3f, 0f);
    private const float KeyboardZoomSpeed = 0.05f, KeyboardPanSpeed = 0.01f, MouseMovementSpeed = 0.25f;
    private const float MinZoomDistance = 0.5f;

    // Bevy's four ranges, each a start margin and an end margin.
    private static readonly (FloatRange Start, FloatRange End) HighPolyRange = (new(0f, 0f), new(3f, 4f));
    private static readonly (FloatRange Start, FloatRange End) LowPolyRange = (new(3f, 4f), new(8f, 9f));
    private static readonly (FloatRange Start, FloatRange End) SingleModelRange = (new(0f, 0f), new(8f, 9f));
    private static readonly (FloatRange Start, FloatRange End) InvisibleRange = (new(0f, 0f), new(0f, 0f));

    private static Entity _camera, _text;

    // Null shows both, by distance, and otherwise which one alone.
    private static bool? _showOnly;
    private static bool _prepass;

    public static void Build(App app)
    {
        (_showOnly, _prepass) = (null, false);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb(0.1f, 0.2f, 0.1f)), Transform.Identity);

            // Bevy's FULL_DAYLIGHT, turned by its EulerRot::ZYX, and cascades kept close.
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
            Render.SetShadowCascades(sun, maximum: 30f, firstBound: 0.9f);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), FocalPoint, Vec3.UnitY));
            Render.SetEnvironmentMap(
                _camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                150f);

            _text = Ui.SpawnText(Describe(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

            // Each model's root, which its meshes take their range from as they appear.
            ecs.Add(ecs.SpawnScene(AssetServer.LoadGltfScene("models/FlightHelmet/FlightHelmet.gltf")), new MainModel { Kind = MainModelKind.HighPoly });
            ecs.Add(ecs.SpawnScene(AssetServer.LoadGltfScene("models/FlightHelmetLowPoly/FlightHelmetLowPoly.gltf")), new MainModel { Kind = MainModelKind.LowPoly });
        }, "visibility_range.Setup");

        app.Update(SetVisibilityRanges, "visibility_range.SetVisibilityRanges");
        app.Update(MoveCamera, "visibility_range.MoveCamera");
        app.Update(UpdateMode, "visibility_range.UpdateMode");
        app.Update(TogglePrepass, "visibility_range.TogglePrepass");
    }

    // Gives each mesh of either model its range and its model as it appears, as Bevy's does for
    // each new Mesh3d, by the model it hangs under.
    private static void SetVisibilityRanges(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var model in ecs.EntitiesWith<MainModel>())
        {
            if (ecs.Get<Mesh3dRef>(model) is not null) continue;
            var kind = ecs.GetOrDefault<MainModel>(model).Kind;
            foreach (var entity in ecs.Descendants(model).ToArray())
            {
                if (ecs.Has<MainModel>(entity) || ecs.Get<Mesh3dRef>(entity) is null) continue;
                SetRange(ecs, entity, kind == MainModelKind.HighPoly);
                ecs.Add(entity, new MainModel { Kind = kind });
            }
        }
    }

    // Puts the range an entity of one of the two models is drawn over, as the mode shows it.
    private static void SetRange(EcsWorld ecs, Entity entity, bool highPoly)
    {
        var (start, end) = (highPoly, _showOnly) switch
        {
            (true, false) or (false, true) => InvisibleRange,
            (_, not null) => SingleModelRange,
            (true, null) => HighPolyRange,
            (false, null) => LowPolyRange,
        };

        var range = ecs.Insert<VisibilityRangeRef>(entity);
        range.StartMargin = start;
        range.EndMargin = end;
        range.UseAabb = false;
    }

    private static void MoveCamera(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var (zoom, theta) = (0f, 0f);

        if (input.KeyDown(Key.W) || input.KeyDown(Key.ArrowUp)) zoom -= KeyboardZoomSpeed;
        else if (input.KeyDown(Key.S) || input.KeyDown(Key.ArrowDown)) zoom += KeyboardZoomSpeed;

        if (input.KeyDown(Key.A) || input.KeyDown(Key.ArrowLeft)) theta -= KeyboardPanSpeed;
        else if (input.KeyDown(Key.D) || input.KeyDown(Key.ArrowRight)) theta += KeyboardPanSpeed;

        zoom -= input.WheelY * MouseMovementSpeed;
        if (zoom == 0f && theta == 0f) return;

        var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var magnitude = transform.Translation.Length;
        var direction = Quat.FromRotationY(theta) * transform.Translation.Normalized;
        var at = direction * MathF.Max(magnitude + zoom, MinZoomDistance);
        ctx.Ecs.Set(_camera, Transform.LookingAt(at, FocalPoint, Vec3.UnitY));
    }

    private static void UpdateMode(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.Digit1) || input.KeyPressed(Key.Numpad1)) _showOnly = null;
        else if (input.KeyPressed(Key.Digit2) || input.KeyPressed(Key.Numpad2)) _showOnly = true;
        else if (input.KeyPressed(Key.Digit3) || input.KeyPressed(Key.Numpad3)) _showOnly = false;
        else return;

        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<MainModel>())
        {
            if (ecs.Get<Mesh3dRef>(entity) is not null)
                SetRange(ecs, entity, ecs.GetOrDefault<MainModel>(entity).Kind == MainModelKind.HighPoly);
        }
        Ui.SetText(_text, Describe());
    }

    private static void TogglePrepass(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        _prepass = !_prepass;
        if (_prepass)
        {
            ctx.Ecs.Insert<DepthPrepassRef>(_camera);
            ctx.Ecs.Insert<NormalPrepassRef>(_camera);
        }
        else
        {
            ctx.Ecs.Wrap<DepthPrepassRef>(_camera).Remove();
            ctx.Ecs.Wrap<NormalPrepassRef>(_camera).Remove();
        }
        Ui.SetText(_text, Describe());
    }

    private static string Describe() =>
        $"""
        {(_showOnly is null ? '>' : ' ')} (1) Switch from high-poly to low-poly based on camera distance
        {(_showOnly is true ? '>' : ' ')} (2) Show only the high-poly model
        {(_showOnly is false ? '>' : ' ')} (3) Show only the low-poly model
        Press 1, 2, or 3 to switch which model is shown
        Press WASD or use the mouse wheel to move the camera
        Press Space to {(_prepass ? "disable" : "enable")} the prepass
        """;
}

/// <summary>Which of the two helmets a model is.</summary>
public enum MainModelKind { HighPoly, LowPoly }

/// <summary>
/// The helmet a model's root, and each of its meshes once it appears, belongs to, as Bevy's
/// <c>MainModel</c> enum keeps it on them.
/// </summary>
[Behavior]
public partial struct MainModel
{
    /// <summary>Which helmet.</summary>
    public MainModelKind Kind;
}

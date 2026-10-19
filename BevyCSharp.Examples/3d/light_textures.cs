// Bevy's light_textures example, examples/3d/light_textures.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates light textures, a picture a light shines through as a gobo shapes a stage light. A
// torch's beam on a spotlight, faces thrown out by a point light inside a box of them, and caustics
// tiled across a directional light. Drag to move the camera or the chosen light, and while a light is
// chosen the rows below show or hide it, scale it and roll it.
internal static class LightTextures
{
    internal const float CubeRotationSpeed = 0.02f;
    private const float MoveSpeed = 0.008f;

    // Bevy's light_consts::lux::AMBIENT_DAYLIGHT and CLEAR_SUNRISE.
    private const float AmbientDaylight = 10_000f, ClearSunrise = 400f;

    private static Entity _innerCube, _directional, _spot, _visibilityRow;

    // Bevy's AppStatus resource, what dragging moves, and whether the drag under way began on a
    // number input, which then sets its number and moves nothing.
    private static SelectionKind _selection;
    private static bool _dragOnInput;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        (_selection, _dragOnInput) = (SelectionKind.Camera, false);

        app.Startup(Setup, "light_textures.Setup");
        app.Update(DrawGizmos, "light_textures.DrawGizmos");
        app.Update(HideShadows, "light_textures.HideShadows");
        app.Update(UpdateDirectionalLight, "light_textures.UpdateDirectionalLight");
        app.Update(HandleDragAsMovement, "light_textures.HandleDragAsMovement");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var silver = Render.CreateMaterial(Color.FromSrgb8(192, 192, 192));

        // A cube turning in the middle of a larger one seen from inside, which the lights fall on.
        var turned = new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 3f), Vec3.One);
        ecs.Add(ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 3f, 3f, 3f), silver, turned), new Rotate());
        _innerCube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, -13f, -13f, -13f), silver, turned);

        // The directional light hangs hidden from an entity the selection moves, with caustics tiled
        // across it.
        var directionalParent = ecs.Spawn();
        ecs.Add(directionalParent, Transform.LookingAt(new Vec3(8f, 8f, 4f), Vec3.Zero, Vec3.UnitY));
        ecs.Insert<VisibilityRef>(directionalParent).Value = Visibility.Hidden;
        _directional = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = AmbientDaylight, Shadows = false });
        ecs.Add(_directional, Transform.Identity);
        ecs.SetParent(_directional, directionalParent);
        Caustics(ecs, _directional);
        ecs.Wrap<VisibilityRef>(_directional).Value = Visibility.Visible;
        ecs.Add(directionalParent, new Selection { Kind = SelectionKind.DirectionalLight });
        ecs.Add(directionalParent, new BaseScale { Value = Vec3.One });

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 2.5f, 9f), Vec3.Zero, Vec3.UnitY));
        ecs.Add(camera, new Selection { Kind = SelectionKind.Camera });

        // A torch's beam on a narrow spotlight.
        var torch = Color.FromSrgb(1f, 1f, 0.8f);
        _spot = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Spot,
            Color = (torch.R, torch.G, torch.B),
            Intensity = 10e6f,
            OuterAngle = 0.25f,
            InnerAngle = 0.25f,
            Shadows = true,
        });
        ecs.Add(_spot, Transform.LookingAt(new Vec3(6f, 1f, 2f), Vec3.Zero, Vec3.UnitY));
        ecs.Insert<SpotLightTextureRef>(_spot).Image = AssetServer.Load(AssetKind.Image, "lightmaps/torch_spotlight_texture.png");
        ecs.Add(_spot, new Selection { Kind = SelectionKind.SpotLight });
        ecs.Add(_spot, new BaseScale { Value = Vec3.One });

        // A blue point light inside a box of faces, which throws them on the walls, with an
        // emissive ball to see it by.
        var pointParent = ecs.Spawn();
        ecs.Add(pointParent, new Transform(new Vec3(0f, 1.8f, 0.01f), Quat.Identity, new Vec3(0.25f)));
        ecs.Insert<VisibilityRef>(pointParent).Value = Visibility.Hidden;
        ecs.Add(pointParent, new Selection { Kind = SelectionKind.PointLight });
        ecs.Add(pointParent, new BaseScale { Value = new Vec3(0.25f) });

        var ball = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Sphere, 1f),
            Render.CreateMaterial(new MaterialSettings { Emissive = Color.FromSrgb(0f, 0f, 300f) }),
            Transform.Identity);
        ecs.SetParent(ball, pointParent);

        var blue = Color.FromSrgb(0f, 0f, 1f);
        var point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Color = (blue.R, blue.G, blue.B), Intensity = 1e6f, Shadows = true });
        ecs.Add(point, Transform.Identity);
        ecs.SetParent(point, pointParent);
        var faces = ecs.Insert<PointLightTextureRef>(point);
        faces.Image = AssetServer.Load(AssetKind.Image, "lightmaps/faces_pointlight_texture_blurred.png");
        faces.CubemapLayout = PointLightTextureRef.CubemapLayoutVariant.CrossVertical;

        var facesScene = AssetServer.LoadGltfScene("models/Faces/faces.glb", 0);
        _pending = (facesScene, pointParent);

        // The choice of what dragging moves, and the rows that show, hide, scale and roll a light,
        // hidden while the camera is chosen, as it is first.
        Theme.BasicExampleTheme(Color.White);
        var column = Radio.MainUiNode();
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, "Drag to Move",
            [(SelectionKind.Camera, "Camera"), (SelectionKind.SpotLight, "Spotlight"), (SelectionKind.PointLight, "Point Light"), (SelectionKind.DirectionalLight, "Directional Light")], 0),
            UiWidgetKind.RadioGroup);
        _visibilityRow = Radio.FeathersOptionButtons(ecs, column, "Visibility", [(Visibility.Inherited, "Show"), (Visibility.Hidden, "Hide")], 0);
        Ui.SelfUpdate(_visibilityRow, UiWidgetKind.RadioGroup);
        foreach (var row in new[]
        {
            _visibilityRow,
            NumberInputs.NumberInputF32<AppNumberInput>(ecs, column, "Scale Multiplier", new AppNumberInput { Kind = AppNumberInputKind.Scale }, 1f, 2, 0.01f, 5f),
            // Short of π at each end, as clustered_decals' is.
            NumberInputs.NumberInputF32<AppNumberInput>(ecs, column, "Roll (-π to π)", new AppNumberInput { Kind = AppNumberInputKind.Roll }, 0f, 2, -MathF.PI + ClusteredDecals.RustEpsilon, MathF.BitDecrement(MathF.PI)),
        })
            ecs.Wrap<VisibilityRef>(row).Value = Visibility.Hidden;
        ecs.Observe<ValueChange<Entity>>(on => HandleSelectionChange(on.Ecs, on.Event.Value));
        ecs.Observe<ValueChange<Entity>>(on => HandleVisibilityChange(on.Ecs, on.Event.Value));
        ecs.Observe<ValueChange<float>>(on => HandleValueChangeNumberInput(on.Ecs, on.Event.Source, on.Event.Value));

        ecs.Add(Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), new LightTexturesHelpText());
    }

    private static (AssetHandle Scene, Entity Parent) _pending;

    // The thing of a kind, found by its Selection as Bevy's queries find it.
    private static Entity Selected(EcsWorld ecs, SelectionKind kind)
    {
        foreach (var entity in ecs.EntitiesWith<Selection>())
            if (ecs.GetOrDefault<Selection>(entity).Kind == kind) return entity;
        return Entity.None;
    }

    private static void Caustics(EcsWorld ecs, Entity light)
    {
        var caustics = ecs.Insert<DirectionalLightTextureRef>(light);
        caustics.Image = AssetServer.Load(AssetKind.Image, "lightmaps/caustic_directional_texture.png");
        caustics.Tiled = true;
    }

    // The yellow cone the spotlight's beam fills, while it is shown.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        if (ctx.Ecs.Wrap<VisibilityRef>(_spot).Value == Visibility.Hidden) return;

        var spot = ctx.Ecs.GetOrDefault<GlobalTransform>(_spot);
        var angle = ctx.Ecs.Wrap<SpotLightRef>(_spot).OuterAngle;
        Gizmos.Cone(spot.Translation * 0.5f, spot.Rotation * Quat.FromRotationX(MathF.PI / 2f), 7f * angle, 7f, Color.FromSrgb(1f, 1f, 0f), inFront: false);
    }

    // Everything but the turning cube casts no shadow, as each new mesh is given NotShadowCaster.
    private static void HideShadows(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (_pending.Scene != AssetHandle.None && AssetServer.StateOf(_pending.Scene) == AssetLoadState.Loaded)
        {
            var root = ecs.SpawnScene(_pending.Scene);
            ecs.SetParent(root, _pending.Parent);
            _pending = (AssetHandle.None, Entity.None);
        }

        foreach (var root in new[] { _innerCube, Selected(ecs, SelectionKind.PointLight) })
            Walk(root);

        void Walk(Entity entity)
        {
            if (ecs.Get<Mesh3dRef>(entity) is not null && ecs.Get<NotShadowCasterRef>(entity) is null)
                ecs.Insert<NotShadowCasterRef>(entity);
            foreach (var child in ecs.ChildrenOf(entity)) Walk(child);
        }
    }

    // Bevy's handle_value_change_number_input, the chosen light scaled from where it began, the
    // spotlight's cone widened with it, or rolled with its yaw and pitch kept, and the value written
    // back to the input.
    private static void HandleValueChangeNumberInput(EcsWorld ecs, Entity source, float value)
    {
        if (_selection == SelectionKind.Camera || !ecs.TryGet<AppNumberInput>(source, out var input)) return;

        var entity = Selected(ecs, _selection);
        var transform = ecs.GetOrDefault<Transform>(entity);
        if (input.Kind == AppNumberInputKind.Scale)
        {
            transform.Scale = ecs.GetOrDefault<BaseScale>(entity).Value * value;

            // The spotlight's cone began a quarter of a radian wide.
            if (entity == _spot)
            {
                var spot = ecs.Wrap<SpotLightRef>(_spot);
                spot.OuterAngle = Math.Clamp(0.25f * value, 0.01f, MathF.PI / 4f);
                spot.InnerAngle = spot.OuterAngle;
            }
        }
        else
        {
            var euler = transform.Rotation.ToEuler();
            transform.Rotation = Quat.FromEuler(euler.X, euler.Y, value);
        }

        ecs.Set(entity, transform);
        Feathers.SetNumber(source, value);
    }

    // Bevy's handle_selection_change, the rows below hidden while the camera is chosen, and
    // otherwise shown with Show or Hide checked as the light is and the light's scale and roll.
    private static void HandleSelectionChange(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<SelectionKind>(button, out var selection)) return;
        _selection = selection;

        var camera = selection == SelectionKind.Camera;
        var entity = Selected(ecs, selection);
        ecs.Wrap<VisibilityRef>(_visibilityRow).Value = camera ? Visibility.Hidden : Visibility.Inherited;
        if (!camera)
        {
            var shown = ecs.Wrap<VisibilityRef>(entity).Value;
            foreach (var option in ecs.ChildrenOf(_visibilityRow))
            {
                if (!Radio.OptionValue<Visibility>(option, out var visibility)) continue;
                if (visibility == shown) ecs.Insert<CheckedRef>(option);
                else ecs.Get<CheckedRef>(option)?.Remove();
            }
        }

        var transform = ecs.GetOrDefault<Transform>(entity);
        foreach (var input in ecs.EntitiesWith<AppNumberInput>())
        {
            ecs.Wrap<VisibilityRef>(ecs.ParentOf(input)).Value = camera ? Visibility.Hidden : Visibility.Inherited;
            if (camera) continue;

            Feathers.SetNumber(input, ecs.GetOrDefault<AppNumberInput>(input).Kind == AppNumberInputKind.Scale
                ? transform.Scale.X / ecs.GetOrDefault<BaseScale>(entity).Value.X
                : transform.Rotation.ToEuler().Z);
        }

        foreach (var help in ecs.EntitiesWith<LightTexturesHelpText>()) Ui.SetText(help, HelpText());
    }

    // Bevy's handle_visibility_change, the chosen light shown or hidden.
    private static void HandleVisibilityChange(EcsWorld ecs, Entity button)
    {
        if (Radio.OptionValue<Visibility>(button, out var visibility))
            ecs.Wrap<VisibilityRef>(Selected(ecs, _selection)).Value = visibility;
    }

    // The directional light shines with its caustics while it is shown, dims to a sunrise while
    // one of the other lights is shown, and is plain daylight otherwise.
    private static void UpdateDirectionalLight(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        bool Shown(SelectionKind which) => ecs.Wrap<VisibilityRef>(Selected(ecs, which)).Value != Visibility.Hidden;

        var light = ecs.Wrap<DirectionalLightRef>(_directional);
        var texture = ecs.Get<DirectionalLightTextureRef>(_directional);
        if (Shown(SelectionKind.DirectionalLight))
        {
            light.Illuminance = AmbientDaylight;
            if (texture is null) Caustics(ecs, _directional);
        }
        else
        {
            light.Illuminance = Shown(SelectionKind.PointLight) || Shown(SelectionKind.SpotLight) ? ClearSunrise : AmbientDaylight;
            texture?.Remove();
        }
    }

    // Bevy's handle_drag_as_movement, the point light moved across the view and anything else
    // round the middle at the same distance, keeping the roll it had, unless the drag began on a
    // number input.
    private static void HandleDragAsMovement(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var ecs = ctx.Ecs;
        if (input.MousePressed(MouseButton.Left)) _dragOnInput = ClusteredDecals.OverNumberInput(ecs);
        if (!input.MouseDown(MouseButton.Left) || _dragOnInput) return;

        var (dx, dy) = input.MouseDelta;
        if (dx == 0f && dy == 0f) return;

        var entity = Selected(ecs, _selection);
        var transform = ecs.GetOrDefault<Transform>(entity);
        if (_selection == SelectionKind.PointLight)
        {
            ecs.Set(entity, transform with { Translation = transform.Translation + new Vec3(dx, -dy, 0f) * MoveSpeed });
            return;
        }

        var position = transform.Translation;
        var radius = position.Length;
        var theta = MathF.Acos(position.Y / radius);
        var phi = MathF.Sign(position.Z) * MathF.Acos(position.X / MathF.Sqrt(position.X * position.X + position.Z * position.Z));

        // The camera moves the other way to a light.
        var (phiFactor, thetaFactor) = _selection == SelectionKind.Camera ? (1f, -1f) : (-1f, 1f);
        phi += phiFactor * dx * MoveSpeed;
        theta = Math.Clamp(theta + thetaFactor * dy * MoveSpeed, 0.001f, MathF.PI - 0.001f);

        var roll = transform.Rotation.ToEuler().Z;
        var at = radius * new Vec3(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));
        var looking = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY).Rotation.ToEuler();
        ecs.Set(entity, transform with { Translation = at, Rotation = Quat.FromEuler(looking.X, looking.Y, roll) });
    }

    private static string HelpText()
    {
        var selection = _selection switch
        {
            SelectionKind.Camera => "camera",
            SelectionKind.SpotLight => "spotlight",
            SelectionKind.PointLight => "point light",
            _ => "directional light",
        };
        return _selection == SelectionKind.Camera
            ? $"Click and drag to move {selection}."
            : $"Click and drag to move/scale/rotate {selection}.\n"
                + "To scale/rotate, start the drag within the corresponding number input.\n"
                + "To move, start the drag anywhere else in the example.";
    }
}

/// <summary>What dragging moves, as Bevy's <c>Selection</c> enum names them.</summary>
public enum SelectionKind { Camera, SpotLight, PointLight, DirectionalLight }

/// <summary>A thing dragging can move, and which of them it is.</summary>
[Behavior]
public partial struct Selection
{
    /// <summary>Which.</summary>
    public SelectionKind Kind;
}

/// <summary>The cube in the middle, which turns.</summary>
[Behavior]
public partial struct Rotate
{
    /// <summary>Turned about Y by a fiftieth of a radian each frame, as Bevy's <c>rotate_cube</c> turns it.</summary>
    [OnUpdate]
    public void RotateCube(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(LightTextures.CubeRotationSpeed) * transform.Rotation;
}

/// <summary>
/// The help text, Bevy's <c>HelpText</c> under another name since color_grading's shares the
/// namespace.
/// </summary>
[Behavior]
public partial struct LightTexturesHelpText;

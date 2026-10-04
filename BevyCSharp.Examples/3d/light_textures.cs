using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates light textures, a picture a light shines through as a gobo shapes a stage light: a
// torch's beam on a spotlight, faces thrown out by a point light inside a box of them, and caustics
// tiled across a directional light. Drag to move the camera or the chosen light, or over the Scale
// and Roll buttons to change what dragging does.
internal static class LightTextures
{
    private const float CubeRotationSpeed = 0.02f;
    private const float MoveSpeed = 0.008f;
    private const float ScaleSpeed = 0.05f;
    private const float RollSpeed = 0.01f;

    // Bevy's light_consts::lux::AMBIENT_DAYLIGHT and CLEAR_SUNRISE.
    private const float AmbientDaylight = 10_000f, ClearSunrise = 400f;

    private enum Selection { Camera, SpotLight, PointLight, DirectionalLight }

    private enum DragMode { Move, Scale, Roll }

    private static readonly Dictionary<Selection, Entity> Selectable = [];
    private static Entity _cube, _innerCube, _directional, _spot, _help, _bottomRight, _scale, _roll;
    private static Selection _selection;
    private static DragMode _dragMode;
    private static RadioButtons<Selection>? _selections;
    private static RadioButtons<bool>? _shown;

    public static void Build(App app)
    {
        Selectable.Clear();
        (_selection, _dragMode) = (Selection.Camera, DragMode.Move);

        app.Startup(Setup, "light_textures.Setup");
        app.Update(ctx =>
        {
            var cube = ctx.Ecs.GetOrDefault<Transform>(_cube);
            ctx.Ecs.Set(_cube, cube with { Rotation = Quat.FromRotationY(CubeRotationSpeed) * cube.Rotation });
        }, "light_textures.RotateCube");
        app.Update(DrawGizmos, "light_textures.DrawGizmos");
        app.Update(HideShadows, "light_textures.HideShadows");
        app.Update(HandleButtons, "light_textures.HandleButtons");
        app.Update(UpdateDirectionalLight, "light_textures.UpdateDirectionalLight");
        app.Update(ProcessDrag, "light_textures.ProcessDrag");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var silver = Scene.Material(Scene.Srgb8(192, 192, 192));

        // A cube turning in the middle of a larger one seen from inside, which the lights fall on.
        var turned = new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 3f), Vec3.One);
        _cube = ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 3f, 3f, 3f), silver, turned);
        _innerCube = ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, -13f, -13f, -13f), silver, turned);

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
        Selectable[Selection.DirectionalLight] = directionalParent;

        var camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 2.5f, 9f), Vec3.Zero, Vec3.UnitY));
        Selectable[Selection.Camera] = camera;

        // A torch's beam on a narrow spotlight.
        var torch = Scene.Srgb(1f, 1f, 0.8f);
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
        Selectable[Selection.SpotLight] = _spot;

        // A blue point light inside a box of faces, which throws them on the walls, with a glowing
        // ball to see it by.
        var pointParent = ecs.Spawn();
        ecs.Add(pointParent, new Transform(new Vec3(0f, 1.8f, 0.01f), Quat.Identity, new Vec3(0.1f)));
        ecs.Insert<VisibilityRef>(pointParent).Value = Visibility.Hidden;
        Selectable[Selection.PointLight] = pointParent;

        var ball = ecs.Mesh(
            Render.CreateMesh(MeshShape.Sphere, 1f),
            Render.CreateMaterial(new MaterialSettings { Emissive = Scene.Srgb(0f, 0f, 300f) }),
            Transform.Identity);
        ecs.SetParent(ball, pointParent);

        var blue = Scene.Srgb(0f, 0f, 1f);
        var point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Color = (blue.R, blue.G, blue.B), Intensity = 1e6f, Shadows = true });
        ecs.Add(point, Transform.Identity);
        ecs.SetParent(point, pointParent);
        var faces = ecs.Insert<PointLightTextureRef>(point);
        faces.Image = AssetServer.Load(AssetKind.Image, "lightmaps/faces_pointlight_texture_blurred.png");
        faces.CubemapLayout = PointLightTextureRef.CubemapLayoutVariant.CrossVertical;

        var facesScene = AssetServer.LoadGltfScene("models/Faces/faces.glb", 0);
        _pending = (facesScene, pointParent);

        _selections = new RadioButtons<Selection>(ecs, RadioButtons<Selection>.Column(), "Drag to Move",
            [(Selection.Camera, "Camera"), (Selection.SpotLight, "Spotlight"), (Selection.PointLight, "Point Light"), (Selection.DirectionalLight, "Directional Light")],
            _selection);

        _bottomRight = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Direction = UiDirection.Row,
            Right = Length.Px(10f),
            Bottom = Length.Px(10f),
            ColumnGap = Length.Px(6f),
        });
        _shown = new RadioButtons<bool>(ecs, _bottomRight, "", [(true, "Show"), (false, "Hide")], true);
        _scale = DragButton(ecs, "Scale");
        _roll = DragButton(ecs, "Roll");

        _help = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        ShowButtons(ecs);
    }

    private static (AssetHandle Scene, Entity Parent) _pending;

    private static void Caustics(EcsWorld ecs, Entity light)
    {
        var caustics = ecs.Insert<DirectionalLightTextureRef>(light);
        caustics.Image = AssetServer.Load(AssetKind.Image, "lightmaps/caustic_directional_texture.png");
        caustics.Tiled = true;
    }

    // A button that changes what dragging does while the pointer is over it, as Bevy's does.
    private static Entity DragButton(EcsWorld ecs, string label)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Border = Sides.All(Length.Px(1f)),
            BorderColor = (1f, 1f, 1f, 1f),
            Color = (0f, 0f, 0f, 1f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Padding = new Sides(Length.Px(12f), Length.Px(6f), Length.Px(12f), Length.Px(6f)),
            Corners = Corners.All(Length.Px(6f)),
        });
        ecs.SetParent(button, _bottomRight);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), 18f), button);
        return button;
    }

    // The yellow cone the spotlight's beam fills, while it is shown.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        if (ctx.Ecs.Wrap<VisibilityRef>(_spot).Value == Visibility.Hidden) return;

        var spot = ctx.Ecs.GetOrDefault<GlobalTransform>(_spot);
        var angle = ctx.Ecs.Wrap<SpotLightRef>(_spot).OuterAngle;
        Gizmos.Cone(spot.Translation * 0.5f, spot.Rotation * Quat.FromRotationX(MathF.PI / 2f), 7f * angle, 7f, Scene.Srgb(1f, 1f, 0f), inFront: false);
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

        foreach (var root in new[] { _innerCube, Selectable[Selection.PointLight] })
            Walk(root);

        void Walk(Entity entity)
        {
            if (ecs.Get<Mesh3dRef>(entity) is not null && ecs.Get<NotShadowCasterRef>(entity) is null)
                ecs.Insert<NotShadowCasterRef>(entity);
            foreach (var child in ecs.ChildrenOf(entity)) Walk(child);
        }
    }

    private static void HandleButtons(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var changed = false;

        if (_selections!.Pressed(out var selection) && selection != _selection)
        {
            _selection = selection;
            _selections.Select(ecs, selection);
            ShowButtons(ecs);
            changed = true;
        }

        if (_shown!.Pressed(out var shown) && _selection != Selection.Camera)
        {
            ecs.Wrap<VisibilityRef>(Selectable[_selection]).Value = shown ? Visibility.Inherited : Visibility.Hidden;
            _shown.Select(ecs, shown);
        }

        // Over Scale or Roll dragging scales or rolls, and anywhere else it moves, unless a drag is
        // under way.
        if (!ctx.Input.MouseDown(MouseButton.Left))
        {
            var mode = Ui.InteractionOf(_scale) == UiInteraction.Hovered ? DragMode.Scale
                : Ui.InteractionOf(_roll) == UiInteraction.Hovered ? DragMode.Roll
                : DragMode.Move;
            if (mode != _dragMode)
            {
                _dragMode = mode;
                TrySetCursor(mode == DragMode.Move ? CursorShape.Default : CursorShape.ResizeHorizontal);
                changed = true;
            }
        }

        if (changed) Ui.SetText(_help, HelpText());
    }

    // The Show and Hide buttons, and Scale and Roll, are for a light, and hidden for the camera,
    // with Show or Hide lit as the chosen light is.
    private static void ShowButtons(EcsWorld ecs)
    {
        ecs.Wrap<VisibilityRef>(_bottomRight).Value = _selection == Selection.Camera ? Visibility.Hidden : Visibility.Visible;
        _shown!.Select(ecs, ecs.Wrap<VisibilityRef>(Selectable[_selection]).Value != Visibility.Hidden);
    }

    // The directional light shines with its caustics while it is shown, dims to a sunrise while
    // one of the other lights is shown, and is plain daylight otherwise.
    private static void UpdateDirectionalLight(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        bool Shown(Selection which) => ecs.Wrap<VisibilityRef>(Selectable[which]).Value != Visibility.Hidden;

        var light = ecs.Wrap<DirectionalLightRef>(_directional);
        var texture = ecs.Get<DirectionalLightTextureRef>(_directional);
        if (Shown(Selection.DirectionalLight))
        {
            light.Illuminance = AmbientDaylight;
            if (texture is null) Caustics(ecs, _directional);
        }
        else
        {
            light.Illuminance = Shown(Selection.PointLight) || Shown(Selection.SpotLight) ? ClearSunrise : AmbientDaylight;
            texture?.Remove();
        }
    }

    private static void ProcessDrag(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (!input.MouseDown(MouseButton.Left)) return;

        var ecs = ctx.Ecs;
        var entity = Selectable[_selection];
        var (dx, dy) = input.MouseDelta;
        var transform = ecs.GetOrDefault<Transform>(entity);

        switch (_dragMode)
        {
            case DragMode.Move when _selection == Selection.PointLight:
                transform.Translation += new Vec3(dx, -dy, 0f) * MoveSpeed;
                break;

            case DragMode.Move:
                // Round the middle at the same distance, keeping the roll it had.
                var position = transform.Translation;
                var radius = position.Length;
                var theta = MathF.Acos(position.Y / radius);
                var phi = MathF.Sign(position.Z) * MathF.Acos(position.X / MathF.Sqrt(position.X * position.X + position.Z * position.Z));
                var (phiFactor, thetaFactor) = _selection == Selection.Camera ? (1f, -1f) : (-1f, 1f);
                phi += phiFactor * dx * MoveSpeed;
                theta = Math.Clamp(theta + thetaFactor * dy * MoveSpeed, 0.001f, MathF.PI - 0.001f);

                var roll = transform.Rotation.ToEuler().Z;
                var at = radius * new Vec3(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));
                var looking = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY).Rotation.ToEuler();
                transform = transform with { Translation = at, Rotation = Quat.FromEuler(looking.X, looking.Y, roll) };
                break;

            case DragMode.Scale:
                var factor = 1f + dx * ScaleSpeed;
                var scale = transform.Scale * factor;
                transform.Scale = new Vec3(Math.Clamp(scale.X, 0.01f, 5f), Math.Clamp(scale.Y, 0.01f, 5f), Math.Clamp(scale.Z, 0.01f, 5f));
                if (_selection == Selection.SpotLight)
                {
                    var spot = ecs.Wrap<SpotLightRef>(_spot);
                    spot.OuterAngle = Math.Clamp(spot.OuterAngle * factor, 0.01f, MathF.PI / 4f);
                    spot.InnerAngle = spot.OuterAngle;
                }
                break;

            case DragMode.Roll:
                var euler = transform.Rotation.ToEuler();
                transform.Rotation = Quat.FromEuler(euler.X, euler.Y, euler.Z + dx * RollSpeed);
                break;
        }

        ecs.Set(entity, transform);
    }

    private static string HelpText()
    {
        var mode = _dragMode.ToString().ToLowerInvariant();
        var selection = _selection switch
        {
            Selection.Camera => "camera",
            Selection.SpotLight => "spotlight",
            Selection.PointLight => "point light",
            _ => "directional light",
        };
        return $"Click and drag to {mode} {selection}";
    }

    // An offscreen run has no window to set a cursor on.
    private static void TrySetCursor(CursorShape shape)
    {
        try { Window.SetCursorShape(shape); }
        catch (Bevy.Interop.BevyNativeException) { }
    }
}

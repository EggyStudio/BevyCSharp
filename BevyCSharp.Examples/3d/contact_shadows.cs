// Bevy's contact_shadows example, examples/3d/contact_shadows.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Showcases contact shadows, the fine shadow detail a shadow map is too coarse to hold, traced in
// screen space where an object meets what it rests on, with the flight helmet lit by a turning
// directional, point or spot light and the model spun by dragging it.
internal static class ContactShadows
{
    internal const float LightRotationSpeed = 0.002f;

    private enum LightType
    {
        Directional,
        Point,
        Spot,
    }

    private static readonly Dictionary<LightType, Entity> Lights = [];
    private static Entity _camera, _helmet;
    private static CursorShape _cursor;
    private static bool _contactShadows, _shadowMaps, _receive, _dragging;
    internal static bool Rotating;
    private static LightType _lightType;
    private static RadioButtons<bool>? _contactButtons, _shadowMapButtons, _rotationButtons, _receiveButtons;
    private static RadioButtons<LightType>? _typeButtons;

    public static void Build(App app)
    {
        Lights.Clear();
        (_contactShadows, _shadowMaps, Rotating, _receive, _dragging) = (true, true, true, true, false);
        (_lightType, _helmet, _cursor) = (LightType.Point, Entity.None, CursorShape.Default);

        app.Startup(Setup, "contact_shadows.Setup");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
        {
            _helmet = root;
            ctx.Ecs.Set(root, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI), Vec3.One));
        });

        app.Update(SpinModel, "contact_shadows.SpinModel");
        app.Update(HandleSettingChange, "contact_shadows.HandleSettingChange");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 0f);

        var camera = _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-0.8f, 0.6f, -0.8f), new Vec3(0f, 0.35f, 0f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings
        {
            Hdr = true,
            Msaa = 1,
            AntiAlias = AntiAliasPass.Temporal,
            Bloom = true,
            Tonemapper = Tonemapper.AcesFitted,
        });

        // Motion blur, for spinning the model.
        Render.SetEffects(camera, new EffectSettings { ShutterAngle = 2f });
        Render.SetContactShadows(camera, new ContactShadowSettings());
        Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.High);
        var diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
        Render.SetSkybox(camera, diffuse, 1000f);
        Render.SetEnvironmentMap(camera, diffuse, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"), 1000f);

        // The three lights hang from one turning entity, and the one chosen is the one shown.
        var container = ecs.Spawn();
        ecs.Add(container, Transform.LookingAt(new Vec3(-0.8f, 1.5f, 1.2f), Vec3.Zero, Vec3.UnitY));
        ecs.Add(container, new LightContainer());
        Lights[LightType.Directional] = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true, ContactShadows = true });
        Lights[LightType.Point] = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 400_000f, Shadows = true, ContactShadows = true });
        Lights[LightType.Spot] = Render.SpawnLight(new LightSettings { Kind = LightKind.Spot, Intensity = 400_000f, Shadows = true, ContactShadows = true, InnerAngle = 0f, OuterAngle = MathF.PI / 4f });
        foreach (var (type, light) in Lights)
        {
            ecs.Add(light, Transform.Identity);
            ecs.SetParent(light, container);
            ecs.Wrap<VisibilityRef>(light).Value = type == _lightType ? Visibility.Visible : Visibility.Hidden;
        }

        var ground = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Circle, 0.5f),
            Render.CreateMaterial(Color.FromSrgb(0.06f, 0.06f, 0.06f)),
            new Transform(Vec3.Zero, Quat.FromAxisAngle(Vec3.UnitX, -MathF.PI / 2f), Vec3.One));
        ecs.Add(ground, new GroundPlane());

        var column = RadioButtons<bool>.Column();
        _contactButtons = new RadioButtons<bool>(ecs, column, "Contact Shadows", [(true, "On"), (false, "Off")], _contactShadows);
        _shadowMapButtons = new RadioButtons<bool>(ecs, column, "Shadow Maps", [(true, "On"), (false, "Off")], _shadowMaps);
        _rotationButtons = new RadioButtons<bool>(ecs, column, "Light Rotation", [(true, "On"), (false, "Off")], Rotating);
        _typeButtons = new RadioButtons<LightType>(ecs, column, "Light Type",
            [(LightType.Directional, "Directional"), (LightType.Point, "Point"), (LightType.Spot, "Spot")], _lightType);
        _receiveButtons = new RadioButtons<bool>(ecs, column, "Receive Shadows", [(true, "On"), (false, "Off")], _receive);

        var top = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Zero, Right = Length.Zero, Justify = UiJustify.Center });
        ecs.SetParent(Ui.SpawnText("Drag model to spin", new UiSettings(), 18f), top);
    }

    // Bevy observes picking's drag on the model. Here a press over the model, found by a ray from
    // the cursor, starts the drag, and the cursor shows when the model can be taken.
    private static void SpinModel(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (_helmet == Entity.None) return;

        var over = false;
        var (x, y) = input.MousePosition;
        if (Render.TryRay(_camera, x, y, out var origin, out var direction)
            && Picking.TryCast(origin, direction, out var hit, out _, out _))
            over = IsUnder(ctx.Ecs, hit, _helmet);

        if (input.MousePressed(MouseButton.Left) && over) _dragging = true;
        if (!input.MouseDown(MouseButton.Left)) _dragging = false;

        if (_dragging)
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_helmet);
            ctx.Ecs.Set(_helmet, at with { Rotation = Quat.FromRotationY(input.MouseDeltaX * 0.01f) * at.Rotation });
        }

        var cursor = _dragging ? CursorShape.Grabbing : over ? CursorShape.Grab : CursorShape.Default;
        if (cursor != _cursor) TrySetCursor(_cursor = cursor);
    }

    private static void HandleSettingChange(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        if (_contactButtons!.Pressed(out var contact) && contact != _contactShadows)
        {
            _contactShadows = contact;
            ecs.Wrap<DirectionalLightRef>(Lights[LightType.Directional]).ContactShadowsEnabled = contact;
            ecs.Wrap<PointLightRef>(Lights[LightType.Point]).ContactShadowsEnabled = contact;
            ecs.Wrap<SpotLightRef>(Lights[LightType.Spot]).ContactShadowsEnabled = contact;
            _contactButtons.Select(ecs, contact);
        }

        if (_shadowMapButtons!.Pressed(out var maps) && maps != _shadowMaps)
        {
            _shadowMaps = maps;
            ecs.Wrap<DirectionalLightRef>(Lights[LightType.Directional]).ShadowMapsEnabled = maps;
            ecs.Wrap<PointLightRef>(Lights[LightType.Point]).ShadowMapsEnabled = maps;
            ecs.Wrap<SpotLightRef>(Lights[LightType.Spot]).ShadowMapsEnabled = maps;
            _shadowMapButtons.Select(ecs, maps);
        }

        if (_rotationButtons!.Pressed(out var rotating) && rotating != Rotating)
        {
            Rotating = rotating;
            _rotationButtons.Select(ecs, rotating);
        }

        if (_typeButtons!.Pressed(out var lightType) && lightType != _lightType)
        {
            _lightType = lightType;
            foreach (var (type, light) in Lights)
                ecs.Wrap<VisibilityRef>(light).Value = type == lightType ? Visibility.Visible : Visibility.Hidden;
            _typeButtons.Select(ecs, lightType);
        }

        if (_receiveButtons!.Pressed(out var receive) && receive != _receive)
        {
            _receive = receive;
            foreach (var ground in ecs.EntitiesWith<GroundPlane>()) Render.SetMeshFlags(ecs, ground, receive ? MeshFlags.None : MeshFlags.NoShadowReceiving);
            _receiveButtons.Select(ecs, receive);
        }
    }

    private static bool IsUnder(EcsWorld ecs, Entity entity, Entity root)
    {
        for (var at = entity; at != Entity.None; at = ecs.ParentOf(at))
            if (at == root) return true;
        return false;
    }

    // An offscreen run has no window to set a cursor on.
    private static void TrySetCursor(CursorShape shape)
    {
        try { Window.SetCursorShape(shape); }
        catch (Bevy.Interop.BevyNativeException) { }
    }
}

/// <summary>The lights' parent, which carries them round the middle while rotation is on.</summary>
[Behavior]
public partial struct LightContainer
{
    /// <summary>Turned about the middle by a step each frame while rotation is on, as Bevy's <c>rotate_around</c> turns it.</summary>
    [OnUpdate]
    public void RotateLight(BehaviorContext ctx, ref Transform transform)
    {
        if (!ContactShadows.Rotating) return;
        var turn = Quat.FromRotationY(ContactShadows.LightRotationSpeed);
        transform = new Transform(turn * transform.Translation, turn * transform.Rotation, transform.Scale);
    }
}

/// <summary>The ground the helmet stands on, whose shadow receiving the buttons switch.</summary>
[Behavior]
public partial struct GroundPlane;

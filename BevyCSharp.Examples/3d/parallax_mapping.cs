// Bevy's parallax_mapping example, examples/3d/parallax_mapping.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A spinning cube whose faces are carved by a depth map, parallax mapping moving its texture along
// the line of sight so the carving shows though the faces are flat, among four large cubes of the
// same material. A left click moves the camera to the next of four views, 1 and 2 make the carving
// shallower and deeper, 3 and 4 halve and double the layers the depth map is cut into, and Space
// goes through occlusion mapping and relief mapping at two, four and eight steps.
internal static class ParallaxMapping
{
    private const float DepthChangeRate = 0.1f;
    private const float DepthUpdateStep = 0.03f;
    private const float MaxDepth = 0.3f;

    // The views a left click goes through, in Bevy's order.
    private static readonly (Vec3 At, Quat Facing)[] CameraPositions =
    [
        (new Vec3(1.5f, 1.5f, 1.5f), new Quat(-0.279f, 0.364f, 0.115f, 0.880f)),
        (new Vec3(2.4f, 0f, 0.2f), new Quat(0.094f, 0.676f, 0.116f, 0.721f)),
        (new Vec3(2.4f, 2.6f, -4.3f), new Quat(0.170f, 0.908f, 0.308f, 0.225f)),
        (new Vec3(-1f, 0.8f, -1.2f), new Quat(-0.004f, 0.909f, 0.247f, -0.335f)),
    ];

    private static Entity _camera, _text;
    private static AssetHandle _material;
    private static int _view;
    private static float _targetDepth, _depth, _targetLayers;
    private static bool _depthUpdate;
    private static ParallaxMethod _method;
    private static uint _steps;

    public static void Build(App app)
    {
        (_view, _targetDepth, _depth, _targetLayers, _depthUpdate) = (0, 0.09f, 0.09f, 5f, false);
        (_method, _steps) = (ParallaxMethod.Relief, 4);

        app.Startup(Setup, "parallax_mapping.Setup");
        app.Update(MoveCamera, "parallax_mapping.MoveCamera");
        app.Update(UpdateParallax, "parallax_mapping.UpdateParallax");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The normal map holds directions rather than colors, so it is read as it is stored.
        var normal = AssetServer.LoadImage("textures/parallax_example/cube_normal.png", new TextureSettings { Srgb = false });

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(1.5f, 1.5f, 1.5f), Vec3.Zero, Vec3.UnitY));

        // Bevy's default point light, a million lumens, shown by a small white sphere at it.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Shadows = true });
        ecs.Add(light, Transform.At(2f, 1f, -1.1f));
        var bulb = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 0.05f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.Identity);
        ecs.SetParent(bulb, light);

        // A dark green floor, a little rough and less reflective than most.
        var green = Color.FromSrgb8(0, 80, 0);
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Plane, 10f, 10f),
            Render.CreateMaterial(new MaterialSettings { BaseColor = (green.R, green.G, green.B, 1f), Roughness = 0.45f, Reflectance = 0.18f }),
            Transform.At(0f, -1f, 0f));

        _material = Render.CreateMaterial(new MaterialSettings
        {
            Roughness = 0.4f,
            BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/parallax_example/cube_color.png"),
            NormalMap = normal,
            DepthMap = AssetServer.Load(AssetKind.Image, "textures/parallax_example/cube_depth.png"),
            ParallaxDepthScale = _depth,
            ParallaxMethod = _method,
            ReliefSteps = _steps,
            ParallaxLayers = MathF.Pow(2f, _targetLayers),
        });

        // A depth map is read along the mesh's tangents, as a normal map is, which a primitive is
        // made without.
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        Render.GenerateTangents(cube);
        var spinning = ecs.SpawnMesh(cube, _material, Transform.Identity);
        ecs.Add(spinning, new ParallaxSpin { Speed = 0.3f });

        var background = Render.CreateMesh(MeshShape.Cuboid, 40f, 40f, 40f);
        Render.GenerateTangents(background);
        foreach (var at in new[] { new Vec3(45f, 0f, 0f), new Vec3(-45f, 0f, 0f), new Vec3(0f, 0f, 45f), new Vec3(0f, 0f, -45f) })
        {
            var big = ecs.SpawnMesh(background, _material, Transform.At(at.X, at.Y, at.Z));
            ecs.Add(big, new ParallaxSpin { Speed = -0.1f });
        }

        _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // A left click picks the next view, and the camera eases a fifth of the way there each frame.
    private static void MoveCamera(BehaviorContext ctx)
    {
        if (ctx.Input.MousePressed(MouseButton.Left)) _view = (_view + 1) % CameraPositions.Length;

        var (at, facing) = CameraPositions[_view];
        var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
        transform.Translation += (at - transform.Translation) * 0.2f;
        transform.Rotation = Quat.Slerp(transform.Rotation, facing, 0.2f);
        ctx.Ecs.Set(_camera, transform);
    }

    // Bevy's three systems that change every material with the keys, in one, since all three write
    // the same material and the same text.
    private static void UpdateParallax(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var changed = false;

        if (input.KeyPressed(Key.Digit1))
        {
            _targetDepth = MathF.Max(_targetDepth - DepthUpdateStep, 0f);
            _depthUpdate = true;
        }

        if (input.KeyPressed(Key.Digit2))
        {
            _targetDepth = MathF.Min(_targetDepth + DepthUpdateStep, MaxDepth);
            _depthUpdate = true;
        }

        // The depth eases a tenth of the way to its target each frame until it arrives.
        if (_depthUpdate)
        {
            var current = _depth;
            _depth = current + (_targetDepth - current) * DepthChangeRate;
            if (MathF.Abs(_depth - current) <= 0.000000001f) _depthUpdate = false;
            changed = true;
        }

        if (input.KeyPressed(Key.Digit3))
        {
            _targetLayers = MathF.Max(_targetLayers - 1f, 0f);
            changed = true;
        }
        else if (input.KeyPressed(Key.Digit4))
        {
            _targetLayers += 1f;
            changed = true;
        }

        if (input.KeyPressed(Key.Space))
        {
            (_method, _steps) = (_method, _steps) switch
            {
                (ParallaxMethod.Occlusion, _) => (ParallaxMethod.Relief, 2u),
                (_, < 3) => (ParallaxMethod.Relief, 4u),
                (_, < 5) => (ParallaxMethod.Relief, 8u),
                _ => (ParallaxMethod.Occlusion, _steps),
            };
            changed = true;
        }

        if (!changed || !Render.TryReadMaterial(_material, out var settings)) return;

        settings.ParallaxDepthScale = _depth;
        settings.ParallaxLayers = MathF.Pow(2f, _targetLayers);
        (settings.ParallaxMethod, settings.ReliefSteps) = (_method, _steps);
        Render.WriteMaterial(_material, settings);
        Ui.SetText(_text, HelpText());
    }

    private static string HelpText()
    {
        var method = _method == ParallaxMethod.Occlusion ? "Parallax Occlusion Mapping" : $"Relief Mapping with {_steps} steps";
        return FormattableString.Invariant(
            $"Parallax depth scale: {_depth:0.00000}\nLayers: {MathF.Pow(2f, _targetLayers):0}\n{method}\n\n\n")
            + "Controls:\nLeft click - Change view angle\n1/2 - Decrease/Increase parallax depth scale\n"
            + "3/4 - Decrease/Increase layer count\nSpace - Switch parallaxing algorithm\n";
    }
}

/// <summary>A cube of the parallax example turning about its own three axes, Bevy's example's <c>Spin</c>.</summary>
[Behavior]
public partial struct ParallaxSpin
{
    /// <summary>Radians a second about each axis, the third the other way.</summary>
    public float Speed;

    /// <summary>Turns it by its speed about its own Y, X and Z, as Bevy's <c>spin</c> does.</summary>
    [OnUpdate]
    public void Spin(BehaviorContext ctx, ref Transform transform)
    {
        var step = Speed * ctx.Time.Delta;
        transform.Rotation = transform.Rotation * Quat.FromRotationY(step) * Quat.FromRotationX(step) * Quat.FromRotationZ(-step);
    }
}

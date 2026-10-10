// Bevy's deferred_rendering example, examples/3d/deferred_rendering.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Two flight helmets, glossy spheres, an unlit glowing sphere with a light in it and a cube carved
// by parallax mapping, drawn deferred, forward, or forward with a prepass, which 1, 2 and 3 choose.
// The ground and two small cubes are drawn forward whichever is chosen, a material set apart from
// the rest. Space starts and stops the sun going round and the cube turning, and H hides the text.
internal static class DeferredRendering
{
    private enum Mode
    {
        Deferred,
        Forward,
        ForwardPrepass,
    }

    private static Entity _camera, _sun, _text;
    private static Mode _mode;
    private static bool _hideUi;

    /// <summary>Whether the sun and the cube are held still, as they are when the example starts.</summary>
    internal static bool Paused { get; private set; }

    public static void Build(App app)
    {
        (_mode, _hideUi, Paused) = (Mode.Deferred, false, true);

        app.Startup(Setup, "deferred_rendering.Setup");
        app.Startup(SetupParallax, "deferred_rendering.SetupParallax");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) => ctx.Ecs.Set(root, Transform.At(-4f, 0f, -3f)));

        app.Update(ctx =>
        {
            if (Paused) return;
            var transform = ctx.Ecs.GetOrDefault<Transform>(_sun);
            transform.Rotation = Quat.FromRotationY(ctx.Time.Delta * MathF.PI / 5f) * transform.Rotation;
            ctx.Ecs.Set(_sun, transform);
        }, "deferred_rendering.AnimateLightDirection");
        app.Update(SwitchMode, "deferred_rendering.SwitchMode");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's materials deferred unless one says otherwise, and the sun's shadow maps at 4096.
        Render.SetDeferredRendering(true);
        Render.SetShadowMapSize(directional: 4096);

        // Deferred rendering draws a pixel once, so the camera is given no multisampling, and
        // smooths its edges with a pass after instead.
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Msaa = 1, AntiAlias = AntiAliasPass.Fxaa });
        Shaders.SetPrepass(_camera, depth: true, motion: true, deferred: true);
        var fog = ecs.Insert<DistanceFogRef>(_camera);
        fog.Color = Color.FromSrgb8(43, 44, 47);
        fog.Falloff = new FogFalloff.Linear(1f, 8f);
        Render.SetEnvironmentMap(
            _camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            2000f);

        // Bevy's EulerRot::ZYX of nothing about Z or Y and an eighth of a turn down about X.
        _sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 15_000f, Shadows = true });
        ecs.Add(_sun, new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        Render.SetShadowCascades(_sun, cascades: 3, maximum: 10f);

        // The ground and two small cubes drawn forward while everything else is deferred.
        var forward = Render.CreateMaterial(new MaterialSettings
        {
            BaseColor = Color.FromSrgb(0.1f, 0.2f, 0.1f),
            OpaqueRenderMethod = OpaqueRenderMethod.Forward,
        });
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), forward, Transform.Identity);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.1f, 0.1f, 0.1f);
        ecs.SpawnMesh(cube, forward, Transform.At(-0.3f, 0.5f, -0.2f));
        ecs.SpawnMesh(cube, forward, Transform.At(0.2f, 0.5f, 0.2f));

        // A glowing sphere, unlit and brighter than white, with a point light of its color in it.
        var sphere = Render.CreateMesh(MeshShape.UvSphere, 0.125f, 32f, 18f);
        var glow = Color.FromSrgb(10f, 4f, 1f);
        var lamp = ecs.SpawnMesh(sphere, Render.CreateMaterial(new MaterialSettings { BaseColor = glow, Unlit = true }), Transform.At(0.4f, 0.5f, -0.8f));
        Render.SetMeshFlags(ecs, lamp, MeshFlags.NoShadowCasting);
        var light = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Point,
            Intensity = 800f,
            Radius = 0.125f,
            Shadows = true,
            Color = (glow.R, glow.G, glow.B),
        });
        ecs.Add(light, Transform.At(0.4f, 0.5f, -0.8f));

        // Two rows of three glossy spheres, blue, green and red, paler in the second row.
        for (var i = 0; i < 6; i++)
        {
            var j = i % 3;
            var pale = i < 3 ? 0f : 0.2f;
            var color = j switch
            {
                0 => Color.FromSrgb(pale, pale, 1f),
                1 => Color.FromSrgb(pale, 1f, pale),
                _ => Color.FromSrgb(1f, pale, pale),
            };
            var offset = i < 3 ? -0.15f : 0.15f;
            ecs.SpawnMesh(
                sphere,
                Render.CreateMaterial(new MaterialSettings { BaseColor = color, Roughness = 0.089f, Metallic = 0f }),
                Transform.At(j * 0.25f + offset - 0.4f, 0.125f, -j * 0.25f + offset + 0.4f));
        }

        // The sky, an unlit gray box around everything, drawn from inside.
        var sky = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 2f, 1f, 1f),
            Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromHex("888888"), Unlit = true, DoubleSided = true }),
            new Transform(Vec3.Zero, Quat.Identity, new Vec3(1_000_000f)));
        Render.SetMeshFlags(ecs, sky, MeshFlags.NoShadowCasting | MeshFlags.NoShadowReceiving);

        _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // The cube Bevy's parallax_mapping example carves, small and turning beside the glowing sphere.
    private static void SetupParallax(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The normal map holds directions rather than colors, so it is read as it is stored.
        var normal = AssetServer.LoadImage("textures/parallax_example/cube_normal.png", new TextureSettings { Srgb = false });

        // A depth map is read along the mesh's tangents, which a primitive is made without.
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.15f, 0.15f, 0.15f);
        Render.GenerateTangents(cube);

        var carved = Render.CreateMaterial(new MaterialSettings
        {
            Roughness = 0.4f,
            BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/parallax_example/cube_color.png"),
            NormalMap = normal,
            DepthMap = AssetServer.Load(AssetKind.Image, "textures/parallax_example/cube_depth.png"),
            ParallaxDepthScale = 0.09f,
            ParallaxMethod = ParallaxMethod.Relief,
            ReliefSteps = 4,
            ParallaxLayers = 32f,
        });

        var spinning = ecs.SpawnMesh(cube, carved, Transform.At(0.4f, 0.2f, -0.8f));
        ecs.Add(spinning, new DeferredSpin { Speed = 0.3f });
    }

    // 1, 2 and 3 draw deferred, forward, and forward with depth, normal and motion prepasses.
    private static void SwitchMode(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var changed = false;

        if (input.KeyPressed(Key.Space)) Paused = !Paused;

        if (input.KeyPressed(Key.Digit1))
        {
            (_mode, changed) = (Mode.Deferred, true);
            Console.WriteLine("DefaultOpaqueRendererMethod: Deferred");
            Shaders.SetPrepass(_camera, depth: true, motion: true, deferred: true);
            Render.SetDeferredRendering(true);
        }

        // The camera asking for no G-buffer leaves deferred rendering as it was, since another
        // camera may read it, so it is turned off after.
        if (input.KeyPressed(Key.Digit2))
        {
            (_mode, changed) = (Mode.Forward, true);
            Console.WriteLine("DefaultOpaqueRendererMethod: Forward");
            Shaders.SetPrepass(_camera, depth: false);
            Render.SetDeferredRendering(false);
        }

        if (input.KeyPressed(Key.Digit3))
        {
            (_mode, changed) = (Mode.ForwardPrepass, true);
            Console.WriteLine("DefaultOpaqueRendererMethod: Forward + Prepass");
            Shaders.SetPrepass(_camera, depth: true, normals: true, motion: true);
            Render.SetDeferredRendering(false);
        }

        if (input.KeyPressed(Key.H))
        {
            _hideUi = !_hideUi;
            changed = true;
        }

        if (changed) Ui.SetText(_text, HelpText());
    }

    private static string HelpText()
    {
        if (_hideUi) return "";

        string Chosen(Mode mode) => _mode == mode ? ">" : "";
        return $"(H) Hide UI\n(Space) Play/Pause\n\nRendering Method:\n(1) {Chosen(Mode.Deferred)} Deferred\n"
            + $"(2) {Chosen(Mode.Forward)} Forward\n(3) {Chosen(Mode.ForwardPrepass)} Forward + Prepass\n";
    }
}

/// <summary>
/// The carved cube of the deferred example turning about its own three axes while the example is
/// not paused, Bevy's example's <c>Spin</c>.
/// </summary>
[Behavior]
public partial struct DeferredSpin
{
    /// <summary>Radians a second about each axis, the third the other way.</summary>
    public float Speed;

    /// <summary>Turns it by its speed about its own Y, X and Z, as Bevy's <c>spin</c> does.</summary>
    [OnUpdate]
    public void Spin(BehaviorContext ctx, ref Transform transform)
    {
        if (DeferredRendering.Paused) return;

        var step = Speed * ctx.Time.Delta;
        transform.Rotation = transform.Rotation * Quat.FromRotationY(step) * Quat.FromRotationX(step) * Quat.FromRotationZ(-step);
    }
}

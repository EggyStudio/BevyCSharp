using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Ray-traced ambient occlusion, from the panel's graphics page, short rays from every surface on
/// screen through a scene of stand-in meshes, darkening what the sky cannot reach, fed to Bevy's
/// own lighting.
/// </summary>
/// <remarks>
/// <para>
/// A small version of the technique, to show the pieces a package of this kind is built from. The
/// rays are traced by a compute shader of the program's own (<c>assets/shaders/rtao.slang</c>),
/// compiled to SPIR-V for its ray queries, against a ray scene: the ground, the cube and the lamp as
/// a plane, a box and a sphere in a geometry pool, placed where the scene's entities are every
/// frame. The answer is written into the ambient occlusion texture Bevy's materials read, in place
/// of Bevy's screen-space estimate, so the lighting itself is darkened rather than a picture drawn
/// over it.
/// </para>
/// <para>
/// It needs a GPU that traces rays and says so otherwise. Screen-space global illumination
/// sets the same camera's dispatches, so turning either on turns the other off.
/// </para>
/// </remarks>
[Behavior]
public partial struct RayTracedOcclusion
{
    private static ShaderInstance _trace;
    private static ShaderInstance _show;
    private static Entity _camera;
    private static bool _on;

    /// <summary>Says how to turn it on, where it can be.</summary>
    [OnStartup]
    public static void Announce(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        Console.WriteLine(Shaders.SupportsRayQueries
            ? "[RayTracedOcclusion] the panel's graphics page turns on ray-traced ambient occlusion; feature.rtao show paints it"
            : "[RayTracedOcclusion] this GPU cannot trace rays, so there is no ray-traced ambient occlusion");
    }

    /// <summary>Whether it is to be on, as the panel's graphics page sets it.</summary>
    internal static bool On { get; set; }

    /// <summary>
    /// Whether it is tracing, and so writing the occlusion texture the effects page's own ambient
    /// occlusion would otherwise make.
    /// </summary>
    internal static bool Tracing => _on;

    /// <summary>Puts it on or off where the panel changed it.</summary>
    /// <remarks>
    /// The panel shows what it came to after, so one this GPU cannot trace goes back to off rather
    /// than being asked for again every frame.
    /// </remarks>
    [OnUpdate]
    public static void Toggle(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || On == _on) return;

        Console.WriteLine($"[RayTracedOcclusion] {Apply(ctx.Ecs, On ? "on" : "off")}");
        On = _on;
    }

    /// <summary>Sets the camera up to trace it, or takes it off.</summary>
    [Command("feature.rtao", "Ray-traced ambient occlusion on the feature test's camera: feature.rtao <on|off|show> [radius]")]
    internal static string Command(string state)
    {
        if (!App.HasRenderer) return "there is no renderer to trace with";

        var said = Apply(ConsoleHost.Ecs, state);
        On = _on;
        return said;
    }

    /// <summary>
    /// Makes every shader program the feature test can use, the occlusion's among them, which are
    /// made only as it is turned on, so all of them compile into the cache under the asset root.
    /// </summary>
    /// <remarks>
    /// For a build shipped without <c>slangc</c>, which reads the cache instead
    /// (<c>build/publish-feature-test.sh</c>). The rest are made as the program starts. The
    /// occlusion's are compiled whether or not this GPU traces rays, since compiling them asks
    /// nothing of it, and <c>shader.list</c> says when each is ready.
    /// </remarks>
    [Command("feature.shaders", "Makes every shader program the feature test can use, so each compiles into the cache a build shipped without slangc reads")]
    internal static string MakeEvery()
    {
        if (!App.HasRenderer) return "there is no renderer to compile for";

        if (!_trace.IsValid)
        {
            Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/rtao.slang", ComputeTarget = ShaderTarget.SpirV });
            Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/rtao_show.slang" });
        }

        return "every program is made; shader.list says when each has compiled";
    }

    private static string Apply(EcsWorld ecs, string state)
    {
        var words = state.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var show = words.Length > 0 && words[0] == "show";
        var on = show || (words.Length > 0 && words[0] is "on" or "1" or "true");

        if (Camera(ecs) is not { } camera) return "there is no camera to trace for";

        if (!on)
        {
            _on = false;
            Shaders.SetViewDispatches(camera);
            Shaders.SetPasses(camera);
            Render.SetAmbientOcclusion(camera, null);

            // The effects page's own occlusion comes back, where it is on.
            Applied.Due();
            return "ray-traced ambient occlusion is off";
        }

        if (!Shaders.SupportsRayQueries) return "this GPU cannot trace rays, so there is no ray-traced ambient occlusion";

        if (!_trace.IsValid && Build() is { } missing) return missing;

        var radius = words.Length > 1 && float.TryParse(words[1], System.Globalization.CultureInfo.InvariantCulture, out var given)
            ? given
            : 1.5f;

        _trace.Set("radius", radius);

        // Bevy's own occlusion makes the texture its lighting reads, and the dispatch writes over it
        // once the prepass has drawn the depth and normals the rays leave from.
        Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.Low);
        Shaders.SetPrepass(camera, depth: true, normals: true);
        Shaders.SetViewDispatches(camera, ViewDispatch.PerPixel(_trace, FramePoint.AfterPrepass));

        // The occlusion itself in place of the picture, which is how to tell what the rays found
        // from what the lighting made of it.
        if (show)
        {
            Shaders.SetPasses(camera, new ShaderPass(_show, AfterTonemapping: true));
        }
        else
        {
            Shaders.SetPasses(camera);
        }

        _on = true;
        return $"ray-traced ambient occlusion is on, reaching {radius} units";
    }

    /// <summary>
    /// Makes the stand-ins, the ray scene and the program, or says why it cannot yet.
    /// </summary>
    private static string? Build()
    {
        var (ground, cube, lamp) = Scene.Parts;
        if (ground == Entity.None) return "the scene has not been built yet";

        // The shapes the scene draws, at its sizes. They could be anything simpler, since the rays
        // only have to meet roughly what is there.
        var pool = Shaders.CreateGeometryPool();
        var plane = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Plane, 24f, 24f));
        var box = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Cuboid, 1.6f, 1.6f, 1.6f));
        var ball = Shaders.AddToGeometryPool(pool, Render.CreateMesh(MeshShape.Sphere, 0.6f));

        var scene = Shaders.CreateRayScene(pool, 3);
        Shaders.SetRaySceneInstance(scene, 0, ground, plane);
        Shaders.SetRaySceneInstance(scene, 1, cube, box);
        Shaders.SetRaySceneInstance(scene, 2, lamp, ball);

        _trace = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
            {
                Compute = "shaders/rtao.slang",
                ComputeTarget = ShaderTarget.SpirV,
            }))
            .SetRayScene("scene", scene)
            .Set("strength", 1f);

        _show = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/rtao_show.slang" }));

        return null;
    }

    /// <summary>The program's camera, found the first time it is asked for.</summary>
    private static Entity? Camera(EcsWorld ecs)
    {
        if (_camera == Entity.None)
        {
            foreach (var row in ecs.Query<FlyCamera>(markChanged: false))
            {
                _camera = row.Entity;
                break;
            }
        }

        return _camera == Entity.None ? null : _camera;
    }
}

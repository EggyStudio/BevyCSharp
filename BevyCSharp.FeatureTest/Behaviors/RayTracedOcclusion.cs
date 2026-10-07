using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Ray-traced ambient occlusion on F6: short rays from every surface on screen through a scene of
/// stand-in meshes, darkening what the sky cannot reach, fed to Bevy's own lighting.
/// </summary>
/// <remarks>
/// <para>
/// A small version of the technique, to show the pieces a package of this kind is built from. The
/// rays are traced by a compute shader of the sample's own (<c>assets/shaders/rtao.slang</c>),
/// compiled to SPIR-V for its ray queries, against a ray scene: the ground, the cube and the lamp as
/// a plane, a box and a sphere in a geometry pool, placed where the scene's entities are every
/// frame. The answer is written into the ambient occlusion texture Bevy's materials read, in place
/// of Bevy's screen-space estimate, so the lighting itself is darkened rather than a picture drawn
/// over it.
/// </para>
/// <para>
/// It needs a GPU that traces rays and says so otherwise. Screen-space global illumination (F5)
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
            ? "[RayTracedOcclusion] F6 toggles ray-traced ambient occlusion; sample.rtao show paints it"
            : "[RayTracedOcclusion] this GPU cannot trace rays, so F6 does nothing");
    }

    /// <summary>Puts it on and off.</summary>
    [OnUpdate]
    public static void Toggle(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || !ctx.Input.KeyPressed(Key.F6)) return;

        Console.WriteLine($"[RayTracedOcclusion] {Apply(ctx.Ecs, _on ? "off" : "on")}");
    }

    /// <summary>Sets the camera up to trace it, or takes it off.</summary>
    [Command("sample.rtao", "Ray-traced ambient occlusion on the sample's camera: sample.rtao <on|off|show> [radius]")]
    internal static string Command(string state) =>
        App.HasRenderer ? Apply(ConsoleHost.Ecs, state) : "there is no renderer to trace with";

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

    /// <summary>The sample's camera, found the first time it is asked for.</summary>
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

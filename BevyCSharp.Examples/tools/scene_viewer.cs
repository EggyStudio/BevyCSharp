// Bevy's scene_viewer example, examples/tools/scene_viewer/main.rs, scene_viewer_plugin.rs,
// animation_plugin.rs and morph_viewer_plugin.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Tools;

// A glTF scene viewer. The scene named after the example's name, or the flight helmet, is spawned
// over Bevy's infinite grid and looked at by a free camera placed by the scene's size, with a light
// where the scene has none. Its clips play, its morph targets are listed with keys that move them,
// and keys animate the light, turn its shadows on, draw bounds and frusta and go through the
// scene's cameras. Switches after the path add a depth prepass, occlusion culling, deferred
// shading, motion blur, a light, compressed meshes or no grid.
internal static class SceneViewer
{
    private const string Instructions = """

        Scene Controls:
            L           - animate light direction
            U           - toggle shadows
            B           - toggle bounding boxes
            F           - toggle camera frusta
            J           - toggle skinned mesh joint bounding boxes
            C           - cycle through the camera controller and any cameras loaded from the scene

            Space       - Play/Pause animation
            Enter       - Cycle through animations

        """;

    // Bevy's Args, read from the command line after the example's name.
    private sealed record Args(
        string ScenePath,
        bool DepthPrepass,
        bool OcclusionCulling,
        bool Deferred,
        bool AddLight,
        bool NoInfiniteGrid,
        bool MeshCompression,
        bool MotionBlur,
        float? MotionBlurShutterAngle);

    private static Args _args = Read([]);

    // Bevy's SceneHandle, the scene, the entity it is spawned under once its file has loaded, and
    // whether it is spawned and has a light of its own.
    private static AssetHandle _scene;
    private static Entity _root;
    private static bool _isLoaded, _hasLight, _setUp;

    // Bevy's CameraTracker, the cameras in the order they came and the one drawing.
    private static readonly List<Entity> Cameras = [];
    private static int? _active;
    private static Entity _freeCamera;

    private static bool _animateLight, _bounds, _frusta, _skinnedBounds;

    // The animation plugin's clips of the scene, and the one playing.
    private static IReadOnlyList<string> _clips = [];
    private static int _clip;
    private static bool _clipsAssigned;

    // Bevy's FreeCameraPlugin and InfiniteGridPlugin.
    public static void Configure(Config config) => (config.CameraControllers, config.DevTools) = (CameraControllers.Free, DevTools.InfiniteGrid);

    public static void Build(App app)
    {
        _args = Read(Environment.GetCommandLineArgs().Skip(2).ToArray());
        (_scene, _root, _isLoaded, _hasLight, _setUp) = (AssetHandle.None, Entity.None, false, false, false);
        (_active, _freeCamera, _animateLight, _bounds, _frusta, _skinnedBounds) = (null, Entity.None, false, false, false, false);
        (_clips, _clip, _clipsAssigned) = ([], 0, false);
        Cameras.Clear();
        MorphViewer.Reset();

        app.Startup(Setup, "scene_viewer.Setup");
        app.Update(SceneLoadCheck, "scene_viewer.SceneLoadCheck");
        app.Update(SetupSceneAfterLoad, "scene_viewer.SetupSceneAfterLoad");
        app.Update(UpdateLights, "scene_viewer.UpdateLights");
        app.Update(CameraTracker, "scene_viewer.CameraTracker");
        app.Update(ToggleGizmos, "scene_viewer.ToggleGizmos");
        app.Update(AssignClips, "scene_viewer.AssignClips");
        app.Update(HandleInputs, "scene_viewer.HandleInputs");
        app.Update(ctx => MorphViewer.Update(ctx, _root, _isLoaded), "scene_viewer.MorphViewer");
    }

    // The switches that take a value, the examples program's own and the viewer's.
    private static readonly string[] Valued = ["--size", "--frames", "--motion-blur-shutter-angle"];

    // The path and the switches, as Bevy's argh reads them, the path the first argument that is
    // neither a switch nor a switch's value, wherever the examples program's own switches put it.
    private static Args Read(string[] given)
    {
        bool Has(string name) => given.Contains(name);
        var angle = Array.IndexOf(given, "--motion-blur-shutter-angle");
        var path = given.Where((argument, i) => !argument.StartsWith("--", StringComparison.Ordinal) && (i == 0 || !Valued.Contains(given[i - 1])))
            .FirstOrDefault();
        return new Args(
            path ?? "models/FlightHelmet/FlightHelmet.gltf",
            Has("--depth-prepass"),
            Has("--occlusion-culling"),
            Has("--deferred"),
            Has("--add-light"),
            Has("--no-infinite-grid"),
            Has("--mesh-compression"),
            Has("--motion-blur"),
            angle >= 0 && angle + 1 < given.Length && float.TryParse(given[angle + 1], System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null);
    }

    // A path ending in #Scene and a number names that scene of the file, and the first otherwise.
    private static (string Path, int Scene) ParseScene(string path)
    {
        var hash = path.LastIndexOf('#');
        if (hash >= 0 && path[(hash + 1)..].StartsWith("Scene", StringComparison.Ordinal)
            && int.TryParse(path[(hash + 1 + "Scene".Length)..], out var index))
            return (path[..hash], index);
        return (path, 0);
    }

    private static void Setup(BehaviorContext ctx)
    {
        Console.WriteLine($"Loading {_args.ScenePath}");
        var (file, index) = ParseScene(_args.ScenePath);

        if (!_args.NoInfiniteGrid) ctx.Ecs.Insert<InfiniteGridRef>(ctx.Ecs.Spawn());

        // Materials drawn deferred where it was asked for, as Bevy inserts its renderer method.
        if (_args.Deferred) Render.SetDeferredRendering(true);

        _scene = _args.MeshCompression ? AssetServer.LoadGltfSceneCompressed(file, index) : AssetServer.LoadGltfScene(file, index);
    }

    // Bevy's scene_load_check, the scene spawned once its file has loaded and said to be ready once
    // its entities are there, and whether it has a light of its own found among them, where Bevy
    // finds it in the scene's world before spawning it.
    private static void SceneLoadCheck(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (_root == Entity.None)
        {
            if (AssetServer.StateOf(_scene) != AssetLoadState.Loaded) return;
            _root = ecs.SpawnScene(_scene);
            Console.WriteLine("Spawning scene...");
        }
        else if (!_isLoaded && ecs.Descendants(_root).Any())
        {
            _hasLight = ecs.Descendants(_root).Any(part => ecs.Get<DirectionalLightRef>(part) is not null || ecs.Get<PointLightRef>(part) is not null);
            Console.WriteLine("...done!");
            _isLoaded = true;
        }
    }

    // Bevy's setup_scene_after_load, once the scene is there and each mesh has its bounds, a free
    // camera looking at the scene from a distance its size gives, and a light where it has none.
    private static void SetupSceneAfterLoad(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (!_isLoaded || _setUp) return;

        // An approximate box about the scene's meshes, each mesh's box turned to a sphere and back
        // so a rotated one is bounded whole.
        var meshes = ecs.All<Mesh3dRef>();
        if (meshes.Any(mesh => ecs.Get<AabbRef>(mesh.Entity) is null)) return;
        _setUp = true;

        var (min, max) = (new Vec3(float.MaxValue), new Vec3(float.MinValue));
        foreach (var mesh in meshes)
        {
            var aabb = ecs.Get<AabbRef>(mesh.Entity)!.Value;
            var global = ecs.GetOrDefault<GlobalTransform>(mesh.Entity);
            var (center, half) = (aabb.Center, aabb.HalfExtents);
            var middle = global.Translation + global.Rotation * new Vec3(center.X * global.Scale.X, center.Y * global.Scale.Y, center.Z * global.Scale.Z);
            var radius = new Vec3(half.X * global.Scale.X, half.Y * global.Scale.Y, half.Z * global.Scale.Z).Length;
            min = new Vec3(MathF.Min(min.X, middle.X - radius), MathF.Min(min.Y, middle.Y - radius), MathF.Min(min.Z, middle.Z - radius));
            max = new Vec3(MathF.Max(max.X, middle.X + radius), MathF.Max(max.Y, middle.Y + radius), MathF.Max(max.Z, middle.Z + radius));
        }

        var size = (max - min).Length;
        var middleOfAll = (min + max) * 0.5f;

        Console.WriteLine("Spawning a controllable 3D perspective camera");
        var camera = _freeCamera = ecs.SpawnCamera3d(Transform.LookingAt(middleOfAll + size * new Vec3(0.5f, 0.25f, 0.5f), middleOfAll, Vec3.UnitY));
        var projection = ecs.Wrap<ProjectionRef>(camera);
        if (projection.Value is Projection.Perspective perspective) projection.Value = perspective with { Far = MathF.Max(perspective.Far, size * 10f) };
        ecs.Wrap<CameraRef>(camera).IsActive = false;
        Render.SetEnvironmentMap(
            camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            150f);

        var walk = size * 3f;
        Render.SetFreeCamera(camera, new FreeCameraSettings { WalkSpeed = walk, RunSpeed = 3f * walk });

        // The controls of the camera and of the viewer.
        Console.WriteLine(Render.FreeCameraControls(camera));
        Console.WriteLine(Instructions);

        if (_args.OcclusionCulling)
        {
            Shaders.SetPrepass(camera, depth: true);
            ecs.Insert<OcclusionCullingRef>(camera);
        }
        if (_args.DepthPrepass) Shaders.SetPrepass(camera, depth: true);
        if (_args.Deferred)
        {
            Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
            Shaders.SetPrepass(camera, depth: true, deferred: true);
        }
        if (_args.MotionBlur)
        {
            var blur = ecs.Insert<MotionBlurRef>(camera);
            if (_args.MotionBlurShutterAngle is { } angle) blur.ShutterAngle = angle;
        }

        if (!_hasLight || _args.AddLight)
        {
            Console.WriteLine("Spawning a directional light");
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
            ecs.Add(light, Transform.LookingAt(new Vec3(1f, 1f, 0f), Vec3.Zero, Vec3.UnitY));
            if (_args.OcclusionCulling) ecs.Insert<OcclusionCullingRef>(light);
            _hasLight = true;
        }
    }

    // Bevy's update_lights, U turning every directional light's shadows on or off and L turning
    // the lights about the scene with the time.
    private static void UpdateLights(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var lights = ecs.All<DirectionalLightRef>();
        if (ctx.Input.KeyPressed(Key.U))
            foreach (var light in lights) light.ShadowMapsEnabled = !light.ShadowMapsEnabled;

        if (ctx.Input.KeyPressed(Key.L)) _animateLight = !_animateLight;
        if (!_animateLight) return;

        var rotation = Quat.FromRotationY(ctx.Time.Elapsed * MathF.PI / 15f) * Quat.FromRotationX(-MathF.PI / 4f);
        foreach (var light in lights)
            ecs.Set(light.Entity, ecs.GetOrDefault<Transform>(light.Entity) with { Rotation = rotation });
    }

    // Bevy's camera_tracker, each camera that comes tracked, the scene's first and then the free
    // camera, the first drawing, and C handing the drawing on to the next.
    private static void CameraTracker(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var added = ecs.All<CameraRef>().Where(camera => !Cameras.Contains(camera.Entity)).ToList();
        foreach (var camera in added.Where(camera => camera.Entity != _freeCamera).Concat(added.Where(camera => camera.Entity == _freeCamera)))
        {
            Cameras.Add(camera.Entity);
            _active ??= Cameras.Count - 1;
            camera.IsActive = _active == Cameras.Count - 1;
        }

        if (!ctx.Input.KeyPressed(Key.C) || _active is not { } active) return;
        ecs.Wrap<CameraRef>(Cameras[active]).IsActive = false;
        _active = (active + 1) % Cameras.Count;
        ecs.Wrap<CameraRef>(Cameras[_active.Value]).IsActive = true;
    }

    // B drawing every bounding box, F every frustum and J every skinned mesh's joint bounds, or
    // none again.
    private static void ToggleGizmos(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.B)) Gizmos.ShowBounds(_bounds = !_bounds);
        if (input.KeyPressed(Key.F)) Gizmos.ShowFrusta(_frusta = !_frusta);
        if (input.KeyPressed(Key.J)) Gizmos.ShowSkinnedBounds(_skinnedBounds = !_skinnedBounds);
    }

    // Bevy's assign_clips, the scene's clips found once it is there and the first played over and
    // over. Bevy gives each animation player the clips that move its descendants, and the scene's
    // player here plays them all.
    private static void AssignClips(BehaviorContext ctx)
    {
        if (!_isLoaded || _clipsAssigned) return;

        // A scene that animates nothing is said as a refusal, which here is a scene of no clips.
        IReadOnlyList<string> clips;
        try
        {
            if (!Animation.TryClips(_root, out clips)) return;
        }
        catch (Bevy.Interop.BevyNativeException)
        {
            clips = [];
        }

        _clipsAssigned = true;
        if (clips.Count == 0) return;

        Console.WriteLine($"Found {clips.Count} animation{(clips.Count == 1 ? "" : "s")}");
        Console.WriteLine($"Animation names: [{string.Join(", ", clips.Select(clip => $"\"{clip}\""))}]");
        _clips = clips;
        Animation.Play(_root, _clips[_clip], new AnimationSettings { Repeat = true });
    }

    // Bevy's handle_inputs, Space pausing or resuming the clip and Enter going to the next one,
    // paused still where it was paused.
    private static void HandleInputs(BehaviorContext ctx)
    {
        if (_clips.Count == 0) return;
        var input = ctx.Input;
        var name = ctx.Ecs.NameOf(_root) ?? $"entity {_root}";
        var paused = Animation.StateOf(_root)?.Paused == true;

        if (input.KeyPressed(Key.Space))
        {
            Console.WriteLine(paused ? $"resuming animations for {name}" : $"pausing animation for {name}");
            if (paused) Animation.Resume(_root);
            else Animation.Pause(_root);
            paused = !paused;
        }

        if (_clips.Count <= 1 || !input.KeyPressed(Key.Enter)) return;

        Console.WriteLine($"switching to new animation for {name}");
        Animation.Stop(_root);
        _clip = (_clip + 1) % _clips.Count;
        Animation.Play(_root, _clips[_clip], new AnimationSettings { Repeat = true });
        if (paused) Animation.Pause(_root);
    }

    // Bevy's morph viewer plugin, the scene's morph targets listed at the top left once it is
    // there, each with the keys that move its weight up and down while they are held.
    private static class MorphViewer
    {
        private const float FontSize = 13f;
        private const float WeightPerSecond = 0.8f;

        private static readonly Key[] Modifiers = [Key.ShiftLeft, Key.ControlLeft, Key.AltLeft];
        private static readonly Key[] Plain =
        [
            Key.R, Key.T, Key.Z, Key.I, Key.O, Key.P, Key.F, Key.G, Key.H, Key.J, Key.K, Key.Y, Key.X, Key.C,
            Key.V, Key.B, Key.N, Key.M, Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        ];

        // Bevy's AVAILABLE_KEYS, each key alone and then with the left shift.
        private static readonly (string Name, bool Shift, Key Key)[] AvailableKeys =
        [
            .. Plain.Select(key => (KeyName(key), false, key)),
            .. Plain.Select(key => ($"lshift-{KeyName(key).ToUpperInvariant()}", true, key)),
        ];

        // A morph target, its entity and its place among the entity's weights, the weight it was
        // last seen at, and which way its key moves it.
        private sealed class Target
        {
            public string? EntityName;
            public Entity Entity;
            public string? Name;
            public int Index;
            public float Weight;
            public float Direction = 1f;

            public override string ToString()
            {
                var of = EntityName ?? Entity.ToString();
                var what = Name ?? $"animation{Index}";
                return $"{what} of {of}: {Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            }
        }

        private static readonly List<Target> Targets = [];
        private static Entity _text = Entity.None;
        private static bool _detected;

        public static void Reset() => (_text, _detected) = (Entity.None, false);

        public static void Update(BehaviorContext ctx, Entity root, bool loaded)
        {
            if (!_detected) DetectMorphs(ctx.Ecs, root, loaded);
            if (_detected && Targets.Count > 0)
            {
                UpdateMorphs(ctx);
                UpdateText(ctx.Ecs);
            }
        }

        private static string KeyName(Key key) => key.ToString().StartsWith("Digit", StringComparison.Ordinal)
            ? key.ToString()["Digit".Length..]
            : key.ToString().ToLowerInvariant();

        // Bevy's detect_morphs, every entity with morph weights listed once the scene is there, with
        // the names its first mesh gives its targets.
        private static void DetectMorphs(EcsWorld ecs, Entity root, bool loaded)
        {
            var morphs = ecs.All<MorphWeightsRef>();
            if (morphs.Length == 0 || !loaded) return;
            _detected = true;
            Targets.Clear();

            foreach (var morph in morphs)
            {
                var names = morph.FirstMesh is { IsValid: true } mesh ? Render.MorphTargetNames(mesh) : null;
                var weights = morph.Weights;
                for (var i = 0; i < weights.Count; i++)
                    Targets.Add(new Target
                    {
                        EntityName = ecs.NameOf(morph.Entity),
                        Entity = morph.Entity,
                        Name = names is not null && i < names.Length ? names[i] : null,
                        Index = i,
                        Weight = weights[i],
                    });
            }

            if (Targets.Count > AvailableKeys.Length) Targets.RemoveRange(AvailableKeys.Length, Targets.Count - AvailableKeys.Length);
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }, FontSize);
        }

        // Bevy's update_morphs, each target whose key is held, with the left shift as its key asks
        // and no other modifier, moved by its weight a second, turning back at either end.
        private static void UpdateMorphs(BehaviorContext ctx)
        {
            var input = ctx.Input;
            for (var i = 0; i < Targets.Count; i++)
            {
                var (_, shift, key) = AvailableKeys[i];
                var others = Modifiers.Where(modifier => !(shift && modifier == Key.ShiftLeft));
                if (!input.KeyDown(key) || (shift && !input.KeyDown(Key.ShiftLeft)) || others.Any(input.KeyDown)) continue;

                var target = Targets[i];
                if (ctx.Ecs.Get<MorphWeightsRef>(target.Entity) is not { } morph) continue;
                var weights = morph.Weights.ToArray();
                var change = ctx.Time.Delta * WeightPerSecond * target.Direction;
                if (weights[target.Index] + change is <= 0f or >= 1f)
                {
                    target.Direction = -target.Direction;
                    change = -change;
                }

                weights[target.Index] += change;
                morph.Weights = weights;
                target.Weight = weights[target.Index];
            }
        }

        // Bevy's update_text, each target's line with the weight it has.
        private static void UpdateText(EcsWorld ecs)
        {
            foreach (var target in Targets)
                if (ecs.Get<MorphWeightsRef>(target.Entity) is { } morph && target.Index < morph.Weights.Count)
                    target.Weight = morph.Weights[target.Index];
            Ui.SetText(_text, Text());
        }

        private static string Text() =>
            "Morph Target Controls\n---------------\n"
            + string.Concat(Targets.Select((target, i) => $"[{AvailableKeys[i].Name}] {target}\n"));
    }
}

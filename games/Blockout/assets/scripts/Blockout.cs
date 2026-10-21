using Bevy;
using Bevy.Physics;

namespace Blockout;

/// <summary>
/// The level, its edit mesh made from the recipe in the game's assets as the game starts, drawn by
/// a part for each material slot and walked on through a collider made from it.
/// </summary>
/// <remarks>
/// One level a game, so it is kept here, where the walk, the modeling window and the commands all
/// reach it. It is drawn and collided with again only where its edit mesh changed since, which the
/// mesh's version says, so a frame where nothing changed costs a comparison. The console's model
/// commands reach it by its name, and a model loaded in its place by one is the level from then on.
/// </remarks>
internal static class Level
{
    /// <summary>The recipe's file, under the game's assets.</summary>
    public const string File = "levels/blockout.model";

    /// <summary>The name the console's model commands reach the level by.</summary>
    public const string Name = "level";

    /// <summary>The level's edit mesh.</summary>
    public static EditMesh Mesh = new();

    /// <summary>The entities drawing it, one for each material slot.</summary>
    public static readonly List<Entity> Parts = [];

    /// <summary>The entity whose static body is the level's collider.</summary>
    public static Entity Body;

    /// <summary>The mesh's version the parts and the collider were last made at.</summary>
    public static int Drawn = -1;

    /// <summary>How many times the collider was made, the first included.</summary>
    public static int Colliders;

    /// <summary>The camera, at the walker's eyes.</summary>
    public static Entity Camera;

    /// <summary>Whether the modeling window is open.</summary>
    public static bool Editing;

    /// <summary>Whether ray-traced lighting lights the level, rather than the sun and ambient occlusion.</summary>
    public static bool RayTraced;
}

/// <summary>Builds the level and what lights and walks it, and keeps it drawn and solid as it is reshaped.</summary>
[Behavior]
public partial struct Game
{
    /// <summary>Where the walker starts, in the middle of the hall.</summary>
    internal static readonly Vec3 Home = new(5f, 0.2f, 4f);

    /// <summary>Makes the level from its recipe, the camera, the lights, the walker and the words on the screen.</summary>
    [OnStartup]
    public static void Begin(BehaviorContext ctx)
    {
        Level.Mesh = EditMesh.Load(Level.File);
        Modeling.Register(Level.Name, Level.Mesh);
        Console.WriteLine($"[blockout] {Level.File}, {Level.Mesh.FaceCount} faces from {Level.Mesh.Recipe.Count} lines");

        ImGuiRuntime.Start();

        // A wide view, as a walk through rooms takes, over a clear blue where the rooms are open to
        // the sky, drawn once a pixel, which ambient occlusion needs.
        Level.Camera = Render.SpawnCamera3d(new CameraSettings
        {
            FieldOfView = 70f,
            Clear = ClearMode.Custom,
            ClearColor = (0.55f, 0.7f, 0.9f, 1f),
        });
        ctx.Ecs.Set(Level.Camera, Transform.At(Home.X, Home.Y + Walker.Eyes, Home.Z));
        Render.SetPostProcessing(Level.Camera, new PostSettings { Msaa = 1 });

        // The sun from the south and the west, high, through the second room's window and into the
        // rooms open to the sky, and the light of the sky everywhere else, which ambient occlusion
        // darkens in the corners. Ray-traced lighting takes over from the last two where it runs.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
        ctx.Ecs.Set(sun, Transform.LookingAt(new Vec3(-6f, 30f, -24f), new Vec3(11f, 0f, 4f), Vec3.UnitY));
        Render.SetAmbientLight(Level.Camera, (0.75f, 0.82f, 1f), 400f);
        Render.SetAmbientOcclusion(Level.Camera, AmbientOcclusionQuality.Medium);

        // A lamp under the hall's ceiling, since the room over it keeps the sun out and only the
        // doorway and the stairwell let the day in, which ray-traced lighting shows as the dark it is.
        var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 400_000f, Range = 16f, Color = (1f, 0.92f, 0.8f) });
        ctx.Ecs.Set(lamp, Transform.At(5f, 2.9f, 4f));

        // A capsule as tall as a person, standing in the hall, which the physics plugin makes a
        // character from its components.
        var walker = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(walker, "Walker");
        ctx.Ecs.Add(walker, Transform.At(Home.X, Home.Y, Home.Z));
        ctx.Ecs.Add(walker, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
        ctx.Ecs.Add(walker, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f), Offset = new Vec3(0f, 0.9f, 0f) });
        ctx.Ecs.Add(walker, new CharacterController());
        ctx.Ecs.Add(walker, new Walker { Speed = 4f });

        Ui.SpawnText(
            "WASD to walk, the right button held or the arrows to look, Space to jump\nTab for the modeling window on the level, F5 to save it",
            new UiSettings { Absolute = true, Left = Length.Px(12f), Top = Length.Px(10f), Color = (1f, 1f, 1f, 0.9f) },
            15f);
    }

    /// <summary>Draws and collides with the level again where it changed, opens and closes the modeling window, and saves.</summary>
    [OnUpdate]
    public static void Keep(BehaviorContext ctx)
    {
        // A model loaded under the level's name by a command is the level from then on.
        if (Modeling.Find(Level.Name) is { } named && !ReferenceEquals(named, Level.Mesh))
        {
            Level.Mesh = named;
            Level.Drawn = -1;
        }

        if (Level.Mesh.Version != Level.Drawn) Rebuild(ctx);

        // Ray-traced lighting comes on once the renderer has asked the GPU, and the level's parts
        // are handed to it then, the sky's light and the occlusion left to it.
        if (!Level.RayTraced && Render.RayTracingActive)
        {
            Level.RayTraced = true;
            Render.SetRayTracedLighting(Level.Camera, true);
            Render.SetAmbientOcclusion(Level.Camera, null);
            Render.SetAmbientLight(Level.Camera, (0f, 0f, 0f), 0f);
            foreach (var part in Level.Parts.Where(part => !part.IsNone)) Render.SetRayTraced(part, Render.MeshOf(ctx.Ecs, part));
        }

        var typing = ImGuiRuntime.Typing;
        if (!typing && ctx.Input.KeyPressed(Key.Tab)) Level.Editing = !Level.Editing;
        if (!typing && ctx.Input.KeyPressed(Key.F5)) Console.WriteLine($"[blockout] saved the level to {Level.Mesh.Save(Level.File)}");

        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);
        if (Level.Editing) Modeling.DrawWindow(ctx, Level.Mesh, Level.Camera);
        ImGuiRuntime.End();
    }

    /// <summary>Writes the level's parts and makes its collider again, after a change or at the start.</summary>
    private static void Rebuild(BehaviorContext ctx)
    {
        var meshes = Modeling.Meshes(Level.Mesh);
        var materials = Modeling.Materials(Level.Mesh);

        // A part for each slot with faces, given its mesh and material again each time, since a slot
        // given a look, renamed or taken away changes what each part draws.
        for (var slot = 0; slot < meshes.Length; slot++)
        {
            while (Level.Parts.Count <= slot) Level.Parts.Add(Entity.None);
            if (meshes[slot] == AssetHandle.None) continue;

            if (Level.Parts[slot].IsNone)
            {
                Level.Parts[slot] = ctx.Ecs.Spawn();
                ctx.Ecs.Add(Level.Parts[slot], Transform.Identity);
            }

            var part = Level.Parts[slot];
            ctx.Ecs.SetName(part, $"Level {Level.Mesh.Slots[slot]}");
            Render.SetMesh(ctx.Ecs, part, meshes[slot]);
            Render.SetMaterial(ctx.Ecs, part, materials[slot]);
            if (Level.RayTraced) Render.SetRayTraced(part, meshes[slot]);
        }

        for (var slot = Level.Parts.Count - 1; slot >= meshes.Length; slot--)
        {
            if (!Level.Parts[slot].IsNone) ctx.Ecs.Despawn(Level.Parts[slot]);
            Level.Parts.RemoveAt(slot);
        }

        // The collider made again whole, since a static body's mesh is fixed once it is made.
        var physics = ctx.Res<PhysicsWorld>();
        if (Level.Body.IsNone)
        {
            Level.Body = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(Level.Body, "Level collider");
        }
        else
        {
            physics.Remove(Level.Body);
        }

        physics.Add(Level.Body, Modeling.Collider(Level.Mesh), BodyKind.Static, Transform.Identity);
        Level.Colliders++;
        Level.Drawn = Level.Mesh.Version;
    }
}

/// <summary>The one walking the level, from its own eyes, by the keys and the mouse or by the autopilot.</summary>
[Behavior]
public partial struct Walker
{
    /// <summary>How high the eyes are over the feet.</summary>
    internal const float Eyes = 1.6f;

    /// <summary>How fast it walks, in meters a second.</summary>
    public float Speed;

    /// <summary>Which way it looks about the upright, in radians, nothing looking along -Z.</summary>
    public float Yaw;

    /// <summary>How far up it looks, in radians.</summary>
    public float Pitch;

    /// <summary>Walks where the keys or the autopilot say, along the way it looks, and jumps.</summary>
    [OnUpdate]
    public void Walk(BehaviorContext ctx, ref CharacterController body, ref Transform place)
    {
        var input = ctx.Input;
        var typing = ImGuiRuntime.Typing;
        var turn = 2f * ctx.Time.Delta;

        // The mouse turns the view while the right button is held, and the arrows turn it too, as a
        // drive's held keys do.
        if (input.MouseDown(MouseButton.Right))
        {
            var (dx, dy) = input.MouseDelta;
            Yaw -= dx * 0.004f;
            Pitch -= dy * 0.004f;
        }

        if (!typing)
        {
            Yaw += (input.KeyDown(Key.ArrowLeft) ? turn : 0f) - (input.KeyDown(Key.ArrowRight) ? turn : 0f);
            Pitch += (input.KeyDown(Key.ArrowUp) ? turn : 0f) - (input.KeyDown(Key.ArrowDown) ? turn : 0f);
        }

        Pitch = Math.Clamp(Pitch, -1.4f, 1.4f);

        var forward = new Vec3(-MathF.Sin(Yaw), 0f, -MathF.Cos(Yaw));
        var right = new Vec3(MathF.Cos(Yaw), 0f, -MathF.Sin(Yaw));
        var move = Vec3.Zero;

        if (Autopilot.Steer(place.Translation, ctx.Time.Delta) is { } toward)
        {
            // Facing the way it walks, so the view and a capture see where it goes.
            move = toward;
            Yaw = MathF.Atan2(-toward.X, -toward.Z);
            Pitch = 0f;
        }
        else if (!typing)
        {
            var across = (input.KeyDown(Key.D) ? 1f : 0f) - (input.KeyDown(Key.A) ? 1f : 0f);
            var ahead = (input.KeyDown(Key.W) ? 1f : 0f) - (input.KeyDown(Key.S) ? 1f : 0f);
            move = forward * ahead + right * across;
        }

        body.Move = move == Vec3.Zero ? Vec3.Zero : move.Normalized * Speed;
        if (!typing && input.KeyPressed(Key.Space) && body.Grounded) body.Jump = 4.5f;
    }

    /// <summary>Puts the camera at the eyes, after the walk, looking the way the walker looks.</summary>
    [OnPostUpdate]
    public static void Look(BehaviorContext ctx)
    {
        foreach (var row in ctx.Ecs.Query<Walker>(markChanged: false))
        {
            var walker = row.Component;
            var feet = ctx.Ecs.GetOrDefault<Transform>(row.Entity).Translation;
            var view = Quat.FromAxisAngle(Vec3.UnitY, walker.Yaw) * Quat.FromAxisAngle(Vec3.UnitX, walker.Pitch);
            ctx.Ecs.Set(Level.Camera, new Transform(feet + new Vec3(0f, Eyes, 0f), view, Vec3.One));
        }
    }
}

/// <summary>Walks the walker along a route of points, for the drive and the soak, as a player would walk it.</summary>
/// <remarks>
/// A route is points on the floors, reached in turn, each within a third of a meter along the
/// ground, the stairs climbed by walking at their top as a player climbs them. Where the walker
/// comes no nearer its point for three seconds it is stuck, which the status says, so a drive fails
/// with where rather than waiting on a walk that will never end.
/// </remarks>
internal static class Autopilot
{
    // The foot of the stairs, their top on the floor over the hall, and the middle of that room.
    private static readonly Vec3[] Up = [new(2.5f, 0f, 1f), new(0.8f, 0f, 1f), new(0.8f, 0f, 6.8f), new(5f, 0f, 6.8f), new(5f, 0f, 4.5f)];

    // Back down the stairs from the room over the hall, to the middle of the hall.
    private static readonly Vec3[] Down = [new(5f, 0f, 6.8f), new(0.8f, 0f, 6.8f), new(0.8f, 0f, 1f), new(2.5f, 0f, 1f), Game.Home];

    // Through the doors and the corridor to the second room's window and back.
    private static readonly Vec3[] Across = [Game.Home, new(11f, 0f, 4f), new(19f, 0f, 4f), new(19f, 0f, 1.5f), new(19f, 0f, 4f), new(11f, 0f, 4f), Game.Home];

    // Two meters out from the middle of the hall and back, a second of the game, which a soak's
    // round walks. A loop of the hall of fifteen meters made the round some 164 seconds on lavapipe
    // held to four processors beside three other games, five rounds where the soak's check judges six.
    private static readonly Vec3[] Loop = [new(7f, 0f, 4f), Game.Home];

    /// <summary>The routes by name, each walked from where the one before it ends, the tour and the loop from the middle of the hall.</summary>
    public static readonly IReadOnlyDictionary<string, Vec3[]> Routes = new Dictionary<string, Vec3[]>(StringComparer.Ordinal)
    {
        ["upstairs"] = Up,
        ["downstairs"] = Down,
        ["loop"] = Loop,
        ["tour"] = [.. Across, .. Up, .. Down],
    };

    private static Vec3[] _route = [];
    private static int _next;
    private static float _nearest;
    private static float _since;

    /// <summary>The route being walked, or nothing.</summary>
    public static string? Route { get; private set; }

    /// <summary>Whether the route was walked to its end.</summary>
    public static bool Done { get; private set; }

    /// <summary>Whether the walker came no nearer its point for three seconds.</summary>
    public static bool Stuck { get; private set; }

    /// <summary>Which point of the route is next, from one, and how many it has.</summary>
    public static (int Next, int Of) Progress => (Math.Min(_next + 1, _route.Length), _route.Length);

    /// <summary>Starts walking a route, from its first point, or stops with nothing.</summary>
    public static void Walk(string? route)
    {
        Route = route;
        _route = route is null ? [] : Routes[route];
        (_next, _nearest, _since, Done, Stuck) = (0, float.MaxValue, 0f, false, false);
    }

    /// <summary>The way to walk from where the walker stands, a length of one, or nothing where no route is being walked.</summary>
    /// <param name="at">Where the walker stands.</param>
    /// <param name="delta">The frame's seconds, which the three seconds of a stuck walker are counted in.</param>
    public static Vec3? Steer(Vec3 at, float delta)
    {
        if (Route is null || Done || Stuck) return null;

        var toward = _route[_next] - new Vec3(at.X, 0f, at.Z);
        toward = new Vec3(toward.X, 0f, toward.Z);
        var far = toward.Length;

        if (far < 0.3f)
        {
            if (++_next == _route.Length)
            {
                Done = true;
                return null;
            }

            (_nearest, _since) = (float.MaxValue, 0f);
            return Steer(at, delta);
        }

        // Nearer by a hundredth of a meter starts the three seconds again.
        if (far < _nearest - 0.01f) (_nearest, _since) = (far, 0f);
        else if ((_since += delta) > 3f) Stuck = true;

        return toward * (1f / far);
    }
}

/// <summary>What play.sh and the soak ask the game and tell it.</summary>
internal static class Commands
{
    /// <summary>The walker, the floor it is on, the lighting, the window, the autopilot and the level's counts.</summary>
    [Command("blockout.status", "Says where the walker stands, its floor, the lighting, the modeling window, the autopilot and the level")]
    internal static string Status()
    {
        var ecs = ConsoleHost.Ecs;
        var at = Vec3.Zero;
        var grounded = false;
        foreach (var row in ecs.Query<CharacterController>(markChanged: false))
        {
            at = ecs.GetOrDefault<Transform>(row.Entity).Translation;
            grounded = row.Component.Grounded;
        }

        var (next, of) = Autopilot.Progress;
        var autopilot = Autopilot.Route is null ? "off"
            : Autopilot.Stuck ? $"{Autopilot.Route} stuck before point {next} of {of}"
            : Autopilot.Done ? $"{Autopilot.Route} done"
            : $"{Autopilot.Route} at point {next} of {of}";
        var mesh = Level.Mesh;

        return string.Join("\n",
            FormattableString.Invariant($"at {at.X:0.00} {at.Y:0.00} {at.Z:0.00}, {(grounded ? "on the ground" : "in the air")}, on the {(at.Y > 3f ? "upper" : "ground")} floor"),
            $"lighting {(Level.RayTraced ? "ray traced" : "the sun, the sky's light and ambient occlusion")}",
            $"modeling window {(Level.Editing ? "open" : "closed")}",
            $"autopilot {autopilot}",
            $"level {mesh.VertexCount} vertices, {mesh.EdgeCount} edges, {mesh.FaceCount} faces, collider made {Level.Colliders} times");
    }

    /// <summary>Walks a route, up from the hall to the room over it, down again, round the hall, or the tour of every room and back, or stops.</summary>
    [Command("blockout.autopilot", "Walks a route by itself: blockout.autopilot upstairs|downstairs|loop|tour|off")]
    internal static string Pilot(string route)
    {
        if (route == "off")
        {
            Autopilot.Walk(null);
            return "autopilot off";
        }

        if (!Autopilot.Routes.ContainsKey(route))
        {
            var message = $"There is no route called {route}, only {string.Join(", ", Autopilot.Routes.Keys)} and off.";
            ConsoleHost.Fail("BAD_ARGUMENT", message);
            return message;
        }

        Autopilot.Walk(route);
        return $"autopilot walking {route}, {Autopilot.Progress.Of} points";
    }
}

using System.Globalization;
using System.Numerics;
using Bevy;
using Bevy.Physics;
using ImGuiNET;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// How the player moves, cycled with F3 held and F4, as Minecraft cycles its modes.
/// </summary>
public enum PlayerMode
{
    /// <summary>
    /// The character under gravity, walking, running, jumping and crouching, stopped by walls.
    /// </summary>
    Walking,

    /// <summary>
    /// The character as walking, and flying with collisions on a double tap of jump.
    /// </summary>
    Creative,

    /// <summary>A free camera through everything, the character left where it stood.</summary>
    Spectator,
}

/// <summary>Where the view is put in walking and creative modes, cycled with F5.</summary>
public enum PlayerView
{
    /// <summary>From the character's eyes.</summary>
    FirstPerson,

    /// <summary>From behind and above it, looking where it looks.</summary>
    Behind,

    /// <summary>From in front of it, looking back at it.</summary>
    InFront,
}

/// <summary>
/// The player, a capsule on <see cref="CharacterController"/> walked about the map in three modes
/// and seen in three views.
/// </summary>
/// <remarks>
/// <para>
/// WASD or the left stick walk it at a run, Alt slows it to a walk, Shift or the stick pressed in
/// sprints, and C, Control or the pad's east button crouch it under what is low. Space or the pad's
/// south button jumps, with coyote time, a jump a moment after walking off an edge still taken, and
/// jump buffering, a jump pressed a moment before landing taken as it lands, each a little over a
/// tenth of a second. In the air it steers at the controller's limited rate. It rides what it
/// stands on and pushes what is lighter, both the controller's own doing.
/// </para>
/// <para>
/// The mouse turns the view while the cursor is locked (Tab) or the right button is held, and the
/// right stick turns it at any time, at the controls page's speed. F3 held with F4 steps the mode,
/// naming it at the top of the screen. In creative mode a double tap of jump takes off, Space then
/// rising and Shift sinking, and touching ground while sinking lands. In spectator mode the
/// character is left standing and the free camera (<see cref="FlyCamera"/>) goes through
/// everything. F5 steps the view, and the panel's player page and the <c>mode</c> command set the
/// same.
/// </para>
/// <para>
/// Falling below the map puts it back at the start of the zone it was last in, as falling into the
/// course's pit does, and a teleport puts it at the zone's start (<see cref="Zones"/>).
/// </para>
/// </remarks>
[Behavior]
public partial struct Player
{
    /// <summary>Which way it faces, in radians about Y, zero facing minus Z.</summary>
    public float Yaw;

    /// <summary>How far up or down it looks, in radians.</summary>
    public float Pitch;

    /// <summary>When it last stood on ground, in the game's seconds.</summary>
    public double Grounded;

    /// <summary>When jump was last pressed and not yet taken, or never.</summary>
    public double Asked;

    /// <summary>When jump was pressed the time before, for a double tap.</summary>
    public double Tapped;

    /// <summary>
    /// Whether it has jumped since it last stood on ground, so coyote time gives one jump.
    /// </summary>
    public bool Jumped;

    private const float Radius = 0.35f;
    private const float Tall = 1.8f;
    private const float Crouched = 1.1f;
    private const float WalkSpeed = 2f;
    private const float RunSpeed = 4.5f;
    private const float SprintSpeed = 7.5f;
    private const float CrouchSpeed = 1.8f;
    private const float FlySpeed = 8f;
    private const float JumpSpeed = 5.5f;
    private const float BounceSpeed = 13f;
    private const double Coyote = 0.12;
    private const double Buffer = 0.12;
    private const double DoubleTap = 0.3;
    private const float FallLimit = -25f;

    private static Entity _figure = Entity.None;
    private static double _shownAt = double.NegativeInfinity;
    private static PlayerMode? _asked;

    /// <summary>The player's entity, or none before it is made.</summary>
    public static Entity Entity { get; private set; } = Entity.None;

    /// <summary>How it moves now.</summary>
    public static PlayerMode Mode { get; private set; }

    /// <summary>Where the view is put in walking and creative modes.</summary>
    public static PlayerView View { get; set; } = PlayerView.Behind;

    /// <summary>Where a fall puts it back, the start of the zone it was last in.</summary>
    public static Vec3 Respawn { get; private set; }

    /// <summary>
    /// Asks for a mode, which the next update takes, naming it at the top of the screen.
    /// </summary>
    public static void Ask(PlayerMode mode) => _asked = mode;

    /// <summary>Makes the player at the hub's start.</summary>
    [OnStartup]
    public static void Make(BehaviorContext ctx)
    {
        Mode = PlayerMode.Walking;
        _asked = null;
        Respawn = Zones.All[0].Start;

        var player = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(player, "Player");
        ctx.Ecs.Add(player, Transform.At(Respawn.X, Respawn.Y, Respawn.Z));
        ctx.Ecs.Add(player, new RigidBody { Kind = BodyKind.Dynamic, Mass = 80f });
        ctx.Ecs.Add(player, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(Radius * 2f, Tall, Radius * 2f), Offset = new Vec3(0f, Tall / 2f, 0f) });
        ctx.Ecs.Add(player, new CharacterController { MaxSlope = 46f, StepHeight = 0.35f });
        ctx.Ecs.Add(player, new Player { Asked = double.NegativeInfinity, Tapped = double.NegativeInfinity });
        Entity = player;

        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        // A capsule to be seen in the third person, with a visor showing which way it faces.
        _figure = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, _figure, Render.CreateMesh(MeshShape.Capsule, Radius, Tall - (2f * Radius)));
        Render.SetMaterial(ctx.Ecs, _figure, Render.CreateMaterial(0.9f, 0.45f, 0.15f, roughness: 0.6f));
        ctx.Ecs.Add(_figure, Transform.At(0f, Tall / 2f, 0f));
        ctx.Ecs.SetParent(_figure, player);

        var visor = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, visor, Render.CreateMesh(MeshShape.Cuboid, 0.4f, 0.14f, 0.12f));
        Render.SetMaterial(ctx.Ecs, visor, Render.CreateMaterial(0.15f, 0.2f, 0.25f, metallic: 0.6f, roughness: 0.2f));
        ctx.Ecs.Add(visor, Transform.At(0f, (Tall / 2f) - 0.25f, -Radius + 0.02f));
        ctx.Ecs.SetParent(visor, _figure);

        Console.WriteLine("[Player] WASD walks, Space jumps, Shift sprints, C crouches, F3 and F4 step the mode, F5 the view");
    }

    /// <summary>
    /// Reads the keys, the mouse and the pad, and walks, flies or leaves the character.
    /// </summary>
    [OnUpdate]
    public void Steer(BehaviorContext ctx, ref CharacterController body)
    {
        var now = ctx.Time.ElapsedSeconds;
        var input = ctx.Input;
        var pad = input.Gamepads.Count > 0 ? input.Gamepads[0] : null;
        var free = !Panel.IsOpen && !ImGuiConsole.IsOpen && !ImGuiRuntime.Typing;

        if (input.KeyDown(Key.F3) && input.KeyPressed(Key.F4)) _asked = (PlayerMode)(((int)Mode + 1) % 3);
        if (free && (input.KeyPressed(Key.F5) || (pad?.Pressed(GamepadButton.Select) ?? false)))
            View = (PlayerView)(((int)View + 1) % 3);

        if (_asked is { } asked)
        {
            _asked = null;
            Change(ctx, ref body, asked);
        }

        if (Mode == PlayerMode.Spectator)
        {
            body.Move = Vec3.Zero;
            body.Fly = false;
            return;
        }

        if (body.Grounded)
        {
            Grounded = now;
            Jumped = false;
        }

        // Turned by the mouse while the cursor is locked or the right button held, and by the
        // right stick at any time, at the controls page's speed and the way up it says.
        var settings = Settings.Current;
        var turn = 0.003f * settings.LookSpeed;
        var (dx, dy) = free && (Renderer.CursorLocked || input.MouseDown(MouseButton.Right)) ? input.MouseDelta : (0f, 0f);
        if (pad is not null)
        {
            dx += pad.RightStick.X * 600f * ctx.Time.Delta;
            dy -= pad.RightStick.Y * 600f * ctx.Time.Delta;
        }

        Yaw -= dx * turn;
        Pitch = Math.Clamp(Pitch - ((settings.InvertY ? -dy : dy) * turn), -1.45f, 1.45f);
        ctx.Ecs.GetRef<Transform>(ctx.Entity).Rotation = Quat.FromRotationY(Yaw);

        var ahead = Held(input, Key.W) - Held(input, Key.S) + (pad?.LeftStick.Y ?? 0f);
        var aside = Held(input, Key.D) - Held(input, Key.A) + (pad?.LeftStick.X ?? 0f);
        if (!free) ahead = aside = 0f;

        var forward = new Vec3(-MathF.Sin(Yaw), 0f, -MathF.Cos(Yaw));
        var right = new Vec3(MathF.Cos(Yaw), 0f, -MathF.Sin(Yaw));
        var wish = (forward * ahead) + (right * aside);
        if (wish.Length > 1f) wish = wish.Normalized;

        var jump = free && (input.KeyPressed(Key.Space) || (pad?.Pressed(GamepadButton.South) ?? false));
        var crouch = free && (input.AnyKeyDown([Key.C, Key.ControlLeft]) || (pad?.Down(GamepadButton.East) ?? false));
        var sprint = free && (input.KeyDown(Key.ShiftLeft) || (pad?.Down(GamepadButton.LeftThumb) ?? false));
        var walk = free && input.AnyKeyDown([Key.AltLeft, Key.AltRight]);

        // A double tap of jump in creative mode takes off or comes down.
        if (jump && Mode == PlayerMode.Creative && now - Tapped <= DoubleTap)
        {
            body.Fly = !body.Fly;
            Tapped = double.NegativeInfinity;
            Asked = double.NegativeInfinity;
            jump = false;
        }
        else if (jump)
        {
            Tapped = now;
        }

        if (body.Fly)
        {
            var rise = (free && input.KeyDown(Key.Space) ? 1f : 0f) - (sprint ? 1f : 0f);
            body.Move = (wish * (input.KeyDown(Key.ControlLeft) ? FlySpeed * 2.5f : FlySpeed)) + new Vec3(0f, rise * FlySpeed, 0f);
            body.Height = 0f;

            // Touching ground while sinking lands, as Minecraft's flight does.
            if (body.Grounded && rise < 0f) body.Fly = false;
        }
        else
        {
            var speed = crouch ? CrouchSpeed : sprint ? SprintSpeed : walk ? WalkSpeed : RunSpeed;
            body.Move = wish * speed;
            body.Height = crouch ? Crouched : 0f;

            // What the course underfoot does, which a character's frictionless contacts and a belt
            // that does not move leave to the game. A belt carries it, ice keeps the speed it had
            // and gives it little grip to change it, and the pad throws it in the air.
            if (body.Grounded && ctx.World.TryGetResource<PhysicsWorld>(out var world) && world.Has(ctx.Entity))
            {
                var feet = ctx.Ecs.GetOrDefault<Transform>(ctx.Entity).Translation;
                var under = world.Raycast(feet + new Vec3(0f, 0.3f, 0f), new Vec3(0f, -1f, 0f), 0.6f, ctx.Entity) is { } hit
                    ? Course.Under(hit.Entity)
                    : Surface.Plain;

                switch (under)
                {
                    case Surface.Conveyor:
                        body.Move += Course.Belt;
                        break;
                    case Surface.Ice:
                        var (going, _) = world.Velocity(ctx.Entity);
                        var kept = going with { Y = 0f };
                        body.Move = kept + ((body.Move - kept) * MathF.Min(1f, 0.8f * ctx.Time.Delta));
                        break;
                    case Surface.Bounce:
                        body.Jump = BounceSpeed;
                        Jumped = true;
                        break;
                }
            }

            if (jump) Asked = now;

            // Buffered and with coyote time, so a press a moment before landing, or a moment after
            // walking off an edge, still jumps, once.
            if (now - Asked <= Buffer && now - Grounded <= Coyote && !Jumped)
            {
                if (body.Grounded)
                {
                    body.Jump = JumpSpeed;
                }
                else if (ctx.World.TryGetResource<PhysicsWorld>(out var physics) && physics.Has(ctx.Entity))
                {
                    // Off the edge already, where the controller takes no jump, so the body is
                    // given the speed itself.
                    var (velocity, _) = physics.Velocity(ctx.Entity);
                    physics.SetVelocity(ctx.Entity, velocity with { Y = JumpSpeed });
                }

                Jumped = true;
                Asked = double.NegativeInfinity;
            }
        }

        Fall(ctx);
    }

    /// <summary>
    /// Puts the view where the mode and the view say, after the character has moved.
    /// </summary>
    [OnPostUpdate]
    public static void Look(BehaviorContext ctx)
    {
        if (Mode == PlayerMode.Spectator || Scene.Camera is not { } camera || !ctx.Ecs.IsAlive(camera) || !ctx.Ecs.IsAlive(Entity)) return;

        var player = ctx.Ecs.GetOrDefault<Player>(Entity);
        var feet = ctx.Ecs.GetOrDefault<Transform>(Entity).Translation;
        var height = ctx.Ecs.GetOrDefault<CharacterController>(Entity).Height is > 0f and var asked ? asked : Tall;
        var rotation = Quat.FromRotationY(player.Yaw) * Quat.FromRotationX(player.Pitch);
        var looking = rotation * new Vec3(0f, 0f, -1f);

        if (_figure != Entity.None && ctx.Ecs.IsAlive(_figure))
        {
            ref var figure = ref ctx.Ecs.GetRef<Transform>(_figure);
            figure.Translation = new Vec3(0f, height / 2f, 0f);
            figure.Scale = new Vec3(1f, height / Tall, 1f);
        }

        var eye = feet + new Vec3(0f, height - 0.15f, 0f);
        var pivot = feet + new Vec3(0f, height * 0.85f, 0f);
        var place = View switch
        {
            PlayerView.FirstPerson => new Transform(eye, rotation, Vec3.One),
            PlayerView.Behind => Transform.LookingAt(Back(ctx, pivot, -looking, 4.5f), pivot + (looking * 2f), Vec3.UnitY),
            _ => Transform.LookingAt(Back(ctx, pivot, looking, 3.5f), pivot, Vec3.UnitY),
        };

        ctx.Ecs.Set(camera, place);
    }

    /// <summary>
    /// Where the view goes a distance from the pivot, brought in where a wall stands between so
    /// the character is not hidden behind it.
    /// </summary>
    private static Vec3 Back(BehaviorContext ctx, Vec3 pivot, Vec3 direction, float distance)
    {
        var away = direction.Normalized;
        if (ctx.World.TryGetResource<PhysicsWorld>(out var physics)
            && physics.Has(Entity)
            && physics.Raycast(pivot, away, distance, Entity) is { } hit)
            distance = MathF.Max(0.3f, hit.Distance - 0.2f);

        return pivot + (away * distance);
    }

    /// <summary>
    /// Puts the character back at its zone's start where it has fallen below the map.
    /// </summary>
    private static void Fall(BehaviorContext ctx)
    {
        var feet = ctx.Ecs.GetOrDefault<Transform>(Entity).Translation;

        // The start of the nearest zone it stands in is where a fall puts it back.
        foreach (var zone in Zones.All)
        {
            var flat = new Vec3(feet.X - zone.Center.X, 0f, feet.Z - zone.Center.Z);
            if (flat.Length < zone.Reach && feet.Y > FallLimit) Respawn = zone.Start;
        }

        if (feet.Y < FallLimit) Put(ctx, Respawn, "fell, and is back at the zone's start");
    }

    /// <summary>Puts the character somewhere at rest.</summary>
    internal static void Put(BehaviorContext ctx, Vec3 feet, string why)
    {
        if (!ctx.World.TryGetResource<PhysicsWorld>(out var physics) || !physics.Has(Entity)) return;

        physics.Place(ctx.Ecs, Entity, feet);
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[Player] {why}, at {feet.X:0.0} {feet.Y:0.0} {feet.Z:0.0}"));
    }

    /// <summary>Turns the character to face a point, as a teleport does.</summary>
    internal static void Face(BehaviorContext ctx, Vec3 target)
    {
        if (!ctx.Ecs.IsAlive(Entity)) return;

        ref var player = ref ctx.Ecs.GetRef<Player>(Entity);
        var feet = ctx.Ecs.GetOrDefault<Transform>(Entity).Translation;
        player.Yaw = MathF.Atan2(-(target.X - feet.X), -(target.Z - feet.Z));
        player.Pitch = 0f;
    }

    /// <summary>Takes a mode, the free camera starting from the view as it was.</summary>
    private static void Change(BehaviorContext ctx, ref CharacterController body, PlayerMode mode)
    {
        if (mode == Mode) return;

        Mode = mode;
        _shownAt = ctx.Time.ElapsedSeconds;
        body.Fly = false;
        body.Move = Vec3.Zero;

        if (mode == PlayerMode.Spectator && Scene.Camera is { } camera && ctx.Ecs.TryGet<Transform>(camera, out var view))
        {
            var ahead = view.Translation + (view.Rotation * new Vec3(0f, 0f, -1f));
            ctx.Ecs.Set(camera, FlyCamera.LookingAt(view.Translation, ahead));
        }

        Console.WriteLine($"[Player] {mode}");
    }

    /// <summary>Names the mode at the top of the screen for a moment after it changes.</summary>
    public static void DrawMode(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (ctx.Time.ElapsedSeconds - _shownAt > 1.5) return;

        var window = ImGuiRuntime.Size;
        ImGui.SetNextWindowPos(new Vector2(window.X / 2f, 64f), ImGuiCond.Always, new Vector2(0.5f, 0f));
        ImGui.SetNextWindowBgAlpha(0.7f);

        const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing;

        if (ImGui.Begin("Mode##feature-test", Flags)) ImGui.TextUnformatted($"{Mode} mode");
        ImGui.End();
    }

    /// <summary>Sets the player's mode from the console.</summary>
    [Command("mode", "Sets how the player moves: mode <walking|creative|spectator>")]
    internal static string ModeCommand(string name)
    {
        if (name.Trim().Length == 0) return Mode.ToString().ToLowerInvariant();
        if (!Enum.TryParse<PlayerMode>(name.Trim(), ignoreCase: true, out var mode))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"There is no mode called {name.Trim()}. The modes are walking, creative and spectator.");
            return "mode <walking|creative|spectator>";
        }

        Ask(mode);
        return mode.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Says where the player is and how it moves, for a tester and for the drive script.
    /// </summary>
    [Command("player", "Where the player is, how it moves and whether it stands on ground")]
    internal static string Where()
    {
        var ecs = ConsoleHost.Ecs;
        if (!ecs.IsAlive(Entity)) return "there is no player";

        var feet = ecs.GetOrDefault<Transform>(Entity).Translation;
        var body = ecs.GetOrDefault<CharacterController>(Entity);
        return string.Create(CultureInfo.InvariantCulture,
            $"at {feet.X:0.00} {feet.Y:0.00} {feet.Z:0.00}, {Mode.ToString().ToLowerInvariant()}, {View}, "
            + $"{(body.Fly ? "flying" : body.Grounded ? "grounded" : "in the air")}");
    }

    /// <summary>
    /// Puts the player at a point, facing as it was, for the drive script to try a station.
    /// </summary>
    [Command("player.put", "Puts the player's feet at a point, at rest: player.put <x> <y> <z>")]
    internal static string PutCommand(float x, float y, float z)
    {
        if (ConsoleHost.World is not { } world) return "there is no world to put the player in";

        Put(new BehaviorContext(world), new Vec3(x, y, z), "put");
        return Where();
    }

    private static float Held(Input input, Key key) => input.KeyDown(key) ? 1f : 0f;
}

// Bevy's first_person_view_model example, examples/camera/first_person_view_model.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// Shows a first-person view model, an arm drawn by a camera of its own so it never sinks into a
// wall the world camera sees, with the world camera's field of view changed by the arrow keys and
// the arm's left as it is.
internal static class FirstPersonViewModel
{
    private const uint WorldLayer = 1u << 0;
    private const uint ViewModelLayer = 1u << 1;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // The player holds both cameras and the arm, so turning it turns all three, and how
            // far the mouse turns it. Visible, so the arm under it is drawn, since an entity
            // without it hides its children.
            var player = ecs.Spawn();
            ecs.Add(player, Transform.At(0f, 1f, 0f));
            ecs.Add(player, Visibility.Inherited);
            ecs.Add(player, new ViewModelPlayer());
            ecs.Add(player, new CameraSensitivity { Value = new Vec2(0.003f, 0.002f) });

            var worldCamera = ecs.SpawnCamera3d(Transform.Identity, new CameraSettings { FieldOfView = 90f });
            ecs.Add(worldCamera, new WorldModelCamera { FieldOfView = 90f });
            ecs.SetParent(worldCamera, player);

            var viewModelCamera = ecs.SpawnCamera3d(Transform.Identity, new CameraSettings { FieldOfView = 70f, Order = 1, Layers = ViewModelLayer });
            ecs.SetParent(viewModelCamera, player);

            var arm = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 0.1f, 0.1f, 0.5f), Render.CreateMaterial(Color.FromSrgb8(153, 246, 228)), Transform.At(0.2f, -0.1f, -0.25f));
            Render.SetLayers(ecs, arm, ViewModelLayer);
            ecs.Insert<NotShadowCasterRef>(arm);
            ecs.SetParent(arm, player);

            var white = Render.CreateMaterial((1f, 1f, 1f, 1f));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), white, Transform.Identity);
            var cube = Render.CreateMesh(MeshShape.Cuboid, 2f, 0.5f, 1f);
            ecs.SpawnMesh(cube, white, Transform.At(0f, 0.25f, -3f));
            ecs.SpawnMesh(cube, white, Transform.At(0.75f, 1.75f, 0f));

            // On both layers, so the arm is lit as the room is.
            var rose = Color.FromSrgb8(253, 164, 175);
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Range = 20f, Shadows = true, Color = (rose.R, rose.G, rose.B) });
            ecs.Add(light, Transform.At(-2f, 4f, -0.75f));
            Render.SetLayers(ecs, light, WorldLayer | ViewModelLayer);

            Ui.SpawnText("Move the camera with your mouse.\nPress arrow up to decrease the FOV of the world model.\nPress arrow down to increase the FOV of the world model.",
                new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        }, "first_person_view_model.Setup");
    }
}

/// <summary>Bevy's <c>Player</c>, who holds both cameras and the arm, turned by the mouse.</summary>
[Behavior]
public partial struct ViewModelPlayer
{
    /// <summary>Turned by the mouse, up and down short of straight up or down, as Bevy's <c>move_player</c> does.</summary>
    [OnUpdate]
    public void MovePlayer(BehaviorContext ctx, ref Transform transform, in CameraSensitivity sensitivity)
    {
        var input = ctx.Input;
        if (input.MouseDelta == (0f, 0f)) return;

        var euler = transform.Rotation.ToEuler();
        const float PitchLimit = MathF.PI / 2f - 0.01f;
        var pitch = Math.Clamp(euler.X - input.MouseDeltaY * sensitivity.Value.Y, -PitchLimit, PitchLimit);
        transform.Rotation = Quat.FromEuler(pitch, euler.Y - input.MouseDeltaX * sensitivity.Value.X, euler.Z);
    }
}

/// <summary>How far a pixel of the mouse turns the player, across and up.</summary>
[Behavior]
public partial struct CameraSensitivity
{
    /// <summary>Radians a pixel, across and up.</summary>
    public Vec2 Value;
}

/// <summary>The camera the world is drawn with, and its field of view, which the arrow keys change.</summary>
[Behavior]
public partial struct WorldModelCamera
{
    /// <summary>Its field of view, in degrees.</summary>
    public float FieldOfView;

    /// <summary>Narrowed with Up and widened with Down, between 20 and 160 degrees, as Bevy's <c>change_fov</c> does.</summary>
    [OnUpdate]
    public void ChangeFov(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var change = (input.KeyDown(Key.ArrowDown) ? 1f : 0f) - (input.KeyDown(Key.ArrowUp) ? 1f : 0f);
        if (change == 0f) return;
        FieldOfView = Math.Clamp(FieldOfView + change, 20f, 160f);
        Render.SetPerspective(ctx.Entity, FieldOfView, 0.1f, 1000f);
    }
}

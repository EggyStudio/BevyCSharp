using System.Runtime.InteropServices;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers Bevy's camera controllers, the free camera, the pan camera and the pan-orbit camera, each
/// moving its camera from the input it is given.
/// </summary>
[Collection("engine")]
public sealed unsafe class CameraControllerTests
{
    /// <summary>The fields the bridge reads, at the places it reads them.</summary>
    [Fact]
    public void TheSettingsAreLaidOutAsTheBridgeReadsThem()
    {
        Assert.Equal(24, Marshal.OffsetOf<NativeFreeCamera>(nameof(NativeFreeCamera.VerticalMovement)).ToInt32());
        Assert.Equal(32, Marshal.OffsetOf<NativeFreeCamera>(nameof(NativeFreeCamera.Keys)).ToInt32());
        Assert.Equal(80, sizeof(NativeFreeCamera));
        Assert.Equal(16, Marshal.OffsetOf<NativeFreeCameraState>(nameof(NativeFreeCameraState.VelocityX)).ToInt32());
        Assert.Equal(28, Marshal.SizeOf<NativeFreeCameraState>());
        Assert.Equal(36, Marshal.OffsetOf<NativePanCamera>(nameof(NativePanCamera.MousePanButton)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<NativePanCamera>(nameof(NativePanCamera.Keys)).ToInt32());
        Assert.Equal(72, sizeof(NativePanCamera));
    }

    /// <summary>
    /// A settings object made with no arguments holds Bevy's defaults, byte for byte as Bevy's own
    /// default component crosses the bridge, so a game changing one setting keeps Bevy's others.
    /// </summary>
    [SkippableFact]
    public void ASettingsObjectStartsAtBevysDefaults()
    {
        Needs.Renderer();

        NativeFreeCamera bevyFree;
        Assert.Equal(NativeStatus.Ok, Native.bcs_free_camera_default(&bevyFree));
        var ourFree = Render.ToNative(new FreeCameraSettings());
        Assert.True(Bytes(&bevyFree, sizeof(NativeFreeCamera)).SequenceEqual(Bytes(&ourFree, sizeof(NativeFreeCamera))), "the free camera's defaults are not Bevy's");

        NativePanCamera bevyPan;
        Assert.Equal(NativeStatus.Ok, Native.bcs_pan_camera_default(&bevyPan));
        var ourPan = Render.ToNative(new PanCameraSettings());
        Assert.True(Bytes(&bevyPan, sizeof(NativePanCamera)).SequenceEqual(Bytes(&ourPan, sizeof(NativePanCamera))), "the pan camera's defaults are not Bevy's");
    }

    /// <summary>
    /// The free camera moves its camera forward while W is held, its settings and what it holds
    /// read back as they were written, and once it is turned off, held W moves the camera no more.
    /// </summary>
    [SkippableFact]
    public void TheFreeCameraMovesItsCameraWhileAKeyIsHeld()
    {
        Needs.Renderer();

        var camera = Entity.None;
        FreeCameraSettings? read = null;
        string controls = "";
        float before = 0f, moved = 0f, off = 0f;
        FreeCameraState? state = null, turnedOff = null;

        var run = new PictureRun
        {
            Configure = config => config.CameraControllers = CameraControllers.Free,
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);
                Render.SetFreeCamera(camera, new FreeCameraSettings { WalkSpeed = 7f, Forward = Key.I });
            },
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("read", world =>
            {
                read = Render.FreeCameraOf(camera);
                controls = Render.FreeCameraControls(camera);
                before = Z(world, camera);
                SyntheticInput.Press(Key.I);
            })
            .Wait(20)
            .Do("lifted", world =>
            {
                SyntheticInput.Lift(Key.I);
                moved = Z(world, camera);
                state = Render.FreeCameraStateOf(camera);
                Render.SetFreeCameraState(camera, state!.Value with { Enabled = false });
            })
            .Wait(30)
            .Do("pressed while off", _ => SyntheticInput.Press(Key.I))
            .Wait(20)
            .Do("read while off", world =>
            {
                SyntheticInput.Lift(Key.I);
                off = Z(world, camera);
                turnedOff = Render.FreeCameraStateOf(camera);
            })
            .Go();

        Assert.Equal(7f, read!.WalkSpeed);
        Assert.Equal(Key.I, read.Forward);
        Assert.Equal(Key.S, read.Back);
        Assert.Contains("KeyI", controls);
        Assert.True(moved < before - 0.1f, $"the camera stood at z {moved} after W was held, from {before}");
        Assert.True(state!.Value.Enabled);
        Assert.False(turnedOff!.Value.Enabled);
        Assert.True(MathF.Abs(off - moved) < 0.1f, $"the camera turned off moved from z {moved} to {off}");
    }

    /// <summary>
    /// The pan camera zooms its 2D camera while its zoom key is held, and its settings read back the
    /// zoom it came to, which is the camera's scale.
    /// </summary>
    [SkippableFact]
    public void ThePanCameraZoomsWhileItsKeyIsHeld()
    {
        Needs.Renderer();

        var camera = Entity.None;
        PanCameraSettings? zoomed = null;
        var scale = 0f;

        var run = new PictureRun
        {
            Configure = config => config.CameraControllers = CameraControllers.Pan,
            Scene = ecs =>
            {
                camera = Render2d.SpawnCamera2d();
                Render.SetPanCamera(camera, new PanCameraSettings { Up = null });
            },
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("press", _ => SyntheticInput.Press(Key.Equal))
            .Wait(10)
            .Do("lift", world =>
            {
                SyntheticInput.Lift(Key.Equal);
                zoomed = Render.PanCameraOf(camera);
                scale = world.Resource<EcsWorld>().GetOrDefault<Transform>(camera).Scale.X;
            })
            .Go();

        Assert.True(zoomed!.ZoomFactor > 1.2f, $"the zoom came to {zoomed.ZoomFactor}");
        Assert.Equal(zoomed.ZoomFactor, scale, 3);
        Assert.Null(zoomed.Up);
        Assert.Equal(Key.S, zoomed.Down);
    }

    /// <summary>
    /// The pan-orbit camera given as Bevy's reflected component turns to face down when asked, and
    /// orbits about the point it looks at while the right button drags across it.
    /// </summary>
    [SkippableFact]
    public void ThePanOrbitCameraTurnsWhenAskedAndOrbitsUnderADrag()
    {
        Needs.Renderer();

        var camera = Entity.None;
        Vec3 down = default, dragged = default, before = default;

        var run = new PictureRun
        {
            Width = 200,
            Height = 200,
            Configure = config => (config.CameraControllers, config.MeshPicking) = (CameraControllers.PanOrbit, true),
            Scene = ecs =>
            {
                camera = PictureRun.Camera(ecs);
                ecs.Insert<PanOrbitCameraRef>(camera);
                PictureRun.Cube(ecs, Render.CreateMaterial((0.6f, 0.6f, 0.6f, 1f)));
            },
        };

        run.Until("started", world => world.Resource<Time>().FrameCount >= 3)
            .Do("look down", _ => Render.LookTo(camera, -Vec3.UnitY))
            .Wait(90)
            .Do("read", world =>
            {
                down = Forward(world, camera);
                Render.LookTo(camera, -Vec3.UnitZ);
            })
            .Wait(90)
            .Do("press", world =>
            {
                before = Forward(world, camera);
                SyntheticInput.MoveTo(100f, 100f);
                SyntheticInput.Press(100f, 100f, MouseButton.Right);
            })
            .Wait(2)
            .Do("drag", _ => SyntheticInput.MoveTo(140f, 100f))
            .Wait(2)
            .Do("drag on", _ => SyntheticInput.MoveTo(180f, 100f))
            .Wait(2)
            .Do("release", _ => SyntheticInput.Release(180f, 100f, MouseButton.Right))
            .Wait(30)
            .Do("dragged", world => dragged = Forward(world, camera))
            .Go();

        Assert.True(down.Y < -0.99f, $"the camera asked to face down faced {down}");
        Assert.True(before.Z < -0.99f, $"the camera asked to face forward faced {before}");
        Assert.True(MathF.Abs(dragged.X) > 0.1f, $"the drag left the camera facing {dragged}");
    }

    private static ReadOnlySpan<byte> Bytes(void* at, int length) => new(at, length);

    private static float Z(World world, Entity camera) => world.Resource<EcsWorld>().GetOrDefault<Transform>(camera).Translation.Z;

    private static Vec3 Forward(World world, Entity camera) =>
        world.Resource<EcsWorld>().GetOrDefault<Transform>(camera).Rotation * -Vec3.UnitZ;
}

using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a pointer does to an entity, found by Bevy's picking and observed from C# as
/// <see cref="Pointer{TEvent}"/>.
/// </summary>
[Collection("engine")]
public sealed class PointerTests
{
    /// <summary>
    /// The mirror of the bridge's report sits field by field where the bridge writes it, which the
    /// bridge asserts with the same numbers.
    /// </summary>
    [Theory]
    [InlineData(nameof(NativePointerEvent.Entity), 0)]
    [InlineData(nameof(NativePointerEvent.PointerNumber), 8)]
    [InlineData(nameof(NativePointerEvent.HitCamera), 16)]
    [InlineData(nameof(NativePointerEvent.Other), 24)]
    [InlineData(nameof(NativePointerEvent.Duration), 32)]
    [InlineData(nameof(NativePointerEvent.Kind), 40)]
    [InlineData(nameof(NativePointerEvent.Button), 48)]
    [InlineData(nameof(NativePointerEvent.Count), 52)]
    [InlineData(nameof(NativePointerEvent.PositionX), 56)]
    [InlineData(nameof(NativePointerEvent.HitDepth), 64)]
    [InlineData(nameof(NativePointerEvent.HitFlags), 68)]
    [InlineData(nameof(NativePointerEvent.HitPositionX), 72)]
    [InlineData(nameof(NativePointerEvent.HitNormalX), 84)]
    [InlineData(nameof(NativePointerEvent.DeltaX), 96)]
    [InlineData(nameof(NativePointerEvent.DistanceX), 104)]
    [InlineData(nameof(NativePointerEvent.ScrollX), 112)]
    [InlineData(nameof(NativePointerEvent.ScrollUnit), 120)]
    [InlineData(nameof(NativePointerEvent.InBounds), 124)]
    public void EveryFieldSitsWhereTheBridgePutsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativePointerEvent>(field).ToInt32());

    /// <summary>The report is the size the bridge writes.</summary>
    [Fact]
    public void TheReportIsTheSizeTheBridgeWrites() => Assert.Equal(128, Marshal.SizeOf<NativePointerEvent>());

    /// <summary>
    /// The pointer moved over an interface node, pressed and let go is seen by the node's observers
    /// as coming over it and clicking it, where it was, and the click goes on up to the node's
    /// parent, as Bevy's does.
    /// </summary>
    [SkippableFact]
    public void AClickOnANodeIsObservedThereAndByItsParent()
    {
        Needs.Renderer();

        var (over, clicks, parentClicks) = (new List<Pointer<Over>>(), new List<Pointer<Click>>(), new List<Entity>());
        Entity inner = default, outer = default;
        var frame = 0;

        using var app = new App(Config.OffscreenFor(200, 200, frames: 40));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            // A panel at the top left, a hundred pixels square, with a smaller one inside it.
            outer = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f), Width = Length.Px(100f), Height = Length.Px(100f) });
            inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Height = Length.Px(80f) });
            ecs.SetParent(inner, outer);

            ecs.Observe<Pointer<Over>>(inner, on => over.Add(on.Event));
            ecs.Observe<Pointer<Click>>(inner, on => clicks.Add(on.Event));
            ecs.Observe<Pointer<Click>>(outer, on => parentClicks.Add(on.Entity));
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) SyntheticInput.MoveTo(40f, 40f);
            if (frame == 14) SyntheticInput.Press(40f, 40f);
            if (frame == 18) SyntheticInput.Release(40f, 40f);
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        Assert.NotEmpty(over);
        Assert.Equal(inner, over[0].Entity);

        var click = Assert.Single(clicks);
        Assert.Equal(inner, click.Entity);
        Assert.Equal(PointerButton.Primary, click.Event.Button);
        Assert.Equal(PointerKind.Mouse, click.PointerId.Kind);
        Assert.InRange(click.Position.X, 39f, 41f);
        Assert.InRange(click.Position.Y, 39f, 41f);

        // Heard at the parent too, as it went up, with the parent as where it had reached.
        Assert.Equal([outer], parentClicks);
    }

    /// <summary>
    /// A click on a cube, in an app that asked for meshes to be picked, is observed at the cube with
    /// where the pointer met its face and which way the face looks.
    /// </summary>
    [SkippableFact]
    public void AClickOnAMeshSaysWhereItMetIt()
    {
        Needs.Renderer();

        var clicks = new List<Pointer<Click>>();
        Entity cube = default;
        var frame = 0;

        var config = Config.OffscreenFor(200, 200, frames: 40);
        config.MeshPicking = true;
        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));
            cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.Identity);
            ecs.Observe<Pointer<Click>>(cube, on => clicks.Add(on.Event));
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) SyntheticInput.MoveTo(100f, 100f);
            if (frame == 14) SyntheticInput.Press(100f, 100f);
            if (frame == 18) SyntheticInput.Release(100f, 100f);
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        var click = Assert.Single(clicks);
        Assert.Equal(cube, click.Entity);
        var (at, facing) = (click.Event.Hit.Position, click.Event.Hit.Normal);
        Assert.NotNull(at);
        Assert.NotNull(facing);
        Assert.InRange(at.Value.Z, 0.49f, 0.51f);
        Assert.InRange(facing.Value.Z, 0.99f, 1.01f);
    }

    /// <summary>A sprite that can be picked is picked where it is drawn, and nowhere else.</summary>
    [SkippableFact]
    public void ASpriteIsPickedWhereItIsDrawn()
    {
        Needs.Renderer();

        var (presses, misses) = (0, 0);
        var frame = 0;

        using var app = new App(Config.OffscreenFor(200, 200, frames: 40));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            // Fifty pixels square in the middle of the picture.
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.Identity);
            var white = Shaders.CreateImage<byte>(1, 1, ShaderImageFormat.Rgba8, [255, 255, 255, 255]);
            Render2d.SetSprite(ecs, sprite, white, new SpriteSettings { Color = (1f, 0f, 0f, 1f), Size = (50f, 50f) });

            // Bevy picks a sprite that says it can be, as its own sprite_picking example says.
            ecs.Insert<Bevy.Reflected.PickableRef>(sprite);
            ecs.Observe<Pointer<Press>>(sprite, _ => presses++);
            ecs.Observe<Pointer<Press>>(on => { if (on.Event.Entity != sprite) misses++; });
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) SyntheticInput.Press(100f, 100f);
            if (frame == 12) SyntheticInput.Release(100f, 100f);
            if (frame == 16) SyntheticInput.Press(10f, 10f);
            if (frame == 18) SyntheticInput.Release(10f, 10f);
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        Assert.Equal(1, presses);
        Assert.Equal(0, misses);
    }

    /// <summary>
    /// The wheel rolled over a node scrolls it as Bevy's <c>Pointer&lt;Scroll&gt;</c>, in lines, and the
    /// scroll goes on up to the node's parent.
    /// </summary>
    [SkippableFact]
    public void TheWheelScrollsTheNodeThePointerIsOver()
    {
        Needs.Renderer();

        var (scrolls, parentScrolls) = (new List<Pointer<Scroll>>(), new List<Entity>());
        Entity inner = default, outer = default;
        var frame = 0;

        using var app = new App(Config.OffscreenFor(200, 200, frames: 30));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            outer = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f), Width = Length.Px(100f), Height = Length.Px(100f) });
            inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Height = Length.Px(80f) });
            ecs.SetParent(inner, outer);

            ecs.Observe<Pointer<Scroll>>(inner, on => scrolls.Add(on.Event));
            ecs.Observe<Pointer<Scroll>>(outer, on => parentScrolls.Add(on.Entity));
        }, "Test.Setup"));

        // Rolled a few frames after the pointer is put there, since what it is over is found a
        // frame after it moves.
        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) SyntheticInput.MoveTo(40f, 40f);
            if (frame == 14) SyntheticInput.Wheel(-2f, 1f);
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        var scroll = Assert.Single(scrolls);
        Assert.Equal(inner, scroll.Entity);
        Assert.Equal(PointerKind.Mouse, scroll.PointerId.Kind);
        Assert.Equal(ScrollUnit.Line, scroll.Event.Unit);
        Assert.Equal(1f, scroll.Event.X);
        Assert.Equal(-2f, scroll.Event.Y);
        Assert.Equal([outer], parentScrolls);
    }

    /// <summary>
    /// A pointer of the game's own, put on an image an interface is drawn into, clicks a node there
    /// where it is put, and the click says which pointer.
    /// </summary>
    [SkippableFact]
    public void APointerOfTheGamesOwnClicksAnInterfaceDrawnIntoAnImage()
    {
        Needs.Renderer();

        var clicks = new List<Pointer<Click>>();
        var (frame, pointer, image) = (0, default(PointerId), AssetHandle.None);

        using var app = new App(Config.OffscreenFor(200, 200, frames: 40));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            // An image a hundred pixels square, which a camera of its own draws an interface into.
            image = Render.CreateTarget(100, 100);
            var camera = Render2d.SpawnCamera2d(order: -1);
            Render.SetCameraTarget(camera, image);
            var node = Ui.SpawnNode(new UiSettings { Camera = camera, Width = Length.Percent(100f), Height = Length.Percent(100f) });
            ecs.Observe<Pointer<Click>>(node, on => clicks.Add(on.Event));

            pointer = Picking.SpawnPointer();
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) Picking.MovePointer(pointer, image, new Vec2(30f, 70f));
            if (frame == 14) Picking.PressPointer(pointer, image, new Vec2(30f, 70f));
            if (frame == 18) Picking.ReleasePointer(pointer, image, new Vec2(30f, 70f));
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        var click = Assert.Single(clicks);
        Assert.Equal(pointer, click.PointerId);
        Assert.Equal(PointerKind.Custom, click.PointerId.Kind);
        Assert.Equal(new Vec2(30f, 70f), click.Position);
    }

    /// <summary>An observer that stops the click keeps it from the parent.</summary>
    [SkippableFact]
    public void AClickStoppedAtANodeDoesNotReachItsParent()
    {
        Needs.Renderer();

        var (heard, parentHeard) = (0, 0);
        var frame = 0;

        using var app = new App(Config.OffscreenFor(200, 200, frames: 40));
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
        {
            var ecs = world.Resource<EcsWorld>();
            Render2d.SpawnCamera2d();

            var outer = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Top = Length.Px(0f), Width = Length.Px(100f), Height = Length.Px(100f) });
            var inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Height = Length.Px(80f) });
            ecs.SetParent(inner, outer);

            ecs.Observe<Pointer<Click>>(inner, on =>
            {
                heard++;
                on.Propagate(false);
            });
            ecs.Observe<Pointer<Click>>(outer, _ => parentHeard++);
        }, "Test.Setup"));

        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            frame++;
            if (frame == 10) SyntheticInput.MoveTo(40f, 40f);
            if (frame == 14) SyntheticInput.Press(40f, 40f);
            if (frame == 18) SyntheticInput.Release(40f, 40f);
        }, "Test.Drive"));

        Assert.Equal(0, app.Run());

        Assert.Equal(1, heard);
        Assert.Equal(0, parentHeard);
    }
}

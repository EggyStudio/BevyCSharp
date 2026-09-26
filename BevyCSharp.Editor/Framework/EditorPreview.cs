using Bevy;
using ImGuiNET;
using System.Numerics;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A picture of the model a person picked, drawn by a camera of its own.
/// </summary>
/// <remarks>
/// <para>
/// A file's name says what it is called and nothing about what it looks like, which for a model is
/// most of what somebody wants to know. A tile cannot show one the way an image tile shows itself,
/// because a model is geometry rather than pixels, so what is left is to draw it.
/// </para>
/// <para>
/// One scene rather than one per tile. A camera drawing into an image costs a pass a frame, and
/// forty tiles would cost forty, so the panel shows what is selected instead of everything at
/// once. The scene lives on a render layer nothing else is on, so the camera looking at the
/// project's own world never sees it and this camera never sees the world.
/// </para>
/// </remarks>
internal static class EditorPreview
{
    /// <summary>How large the picture is, in pixels.</summary>
    private const uint Side = 256;

    /// <summary>
    /// The render layer the preview lives on.
    /// </summary>
    /// <remarks>
    /// High enough to be somewhere nothing else would think to put anything, since a layer is a
    /// bit in a mask and a game reaching for a spare one starts at the bottom.
    /// </remarks>
    private const uint Layer = 1u << 12;

    /// <summary>The image the camera draws into, once there is one.</summary>
    private static AssetHandle _target;

    /// <summary>What draws it.</summary>
    private static Entity _camera;

    /// <summary>What lights it.</summary>
    private static Entity _light;

    /// <summary>What is being shown, and the file it came from.</summary>
    private static Entity _subject;

    private static string? _showing;

    /// <summary>The frame something last asked for a preview.</summary>
    /// <remarks>
    /// A camera drawing into an image costs a pass a frame whether or not anybody is looking at
    /// it, and the panel that asks for this is one tab of several. Nobody asking is how the
    /// preview knows to put itself away.
    /// </remarks>
    private static ulong _asked;

    /// <summary>Everything the preview owns, so a list of the world can leave it out.</summary>
    /// <remarks>
    /// The preview is editor furniture rather than part of the project, the way the interface
    /// camera is. It is in the same world because there is one world, and anything listing what is
    /// in it has to be told which of it somebody put there.
    /// </remarks>
    private static readonly HashSet<Entity> Owned = [];

    /// <summary>Shows the model at <paramref name="path"/>, or nothing when it is null.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="path">The file, under the asset root, or null to show nothing.</param>
    internal static void Show(BehaviorContext ctx, string? path)
    {
        if (!App.HasRenderer) return;

        _asked = EditorShell.Frame;

        if (path != _showing)
        {
            _showing = path;

            if (!_subject.IsNone)
            {
                ctx.Ecs.Despawn(_subject);
                _subject = Entity.None;
            }

            Owned.Clear();
            if (!_camera.IsNone) Owned.Add(_camera);
            if (!_light.IsNone) Owned.Add(_light);

            if (path is { Length: > 0 })
            {
                _subject = ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene(path));
            }
        }

        if (_showing is null) return;

        Build(ctx);
        Place(ctx);
        Frame(ctx);
    }

    /// <summary>The picture, or a handle to nothing before there is one.</summary>
    internal static AssetHandle Target => _target;

    /// <summary>Whether there is something to look at.</summary>
    internal static bool Ready => _showing is { Length: > 0 } && !_subject.IsNone;

    /// <summary>Draws the picture at the cursor, if there is one.</summary>
    /// <param name="size">How large to draw it, in logical pixels.</param>
    internal static void Draw(float size)
    {
        if (!Ready) return;

        var picture = ImGuiTextures.Of(_target);
        if (picture == 0) return;

        ImGui.Image((IntPtr)picture, new Vector2(size, size));
    }

    /// <summary>Makes the camera, the light and the image, the first time one is wanted.</summary>
    private static void Build(BehaviorContext ctx)
    {
        if (!_target.IsValid)
        {
            _target = Render.CreateTarget(Side, Side);
        }

        if (_camera.IsNone)
        {
            _camera = Render.SpawnCamera3d(new CameraSettings
            {
                FieldOfView = 35f,
                Layers = Layer,

                // Its own color rather than the world's, so the picture reads as a swatch of the
                // model rather than as a window onto the scene behind the panel.
                Clear = ClearMode.Custom,
                ClearColor = (0.09f, 0.09f, 0.11f, 1f),

                // Last, so nothing about the order it is drawn in can disturb the main view.
                Order = 8,
            });

            Render.SetCameraTarget(_camera, _target);
            Owned.Add(_camera);
        }

        if (_light.IsNone)
        {
            _light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Directional,
                Intensity = 9_000f,
            });

            Render.SetLayers(ctx.Ecs, _light, Layer);
            ctx.Ecs.Add(_light, Transform.LookingAt(new Vec3(3f, 5f, 4f), Vec3.Zero, Vec3.UnitY));
            Owned.Add(_light);
        }
    }

    /// <summary>Whether an entity is part of the preview rather than of the project.</summary>
    internal static bool Owns(Entity entity) => Owned.Contains(entity);

    /// <summary>
    /// Puts the preview away when nothing asked for one this frame.
    /// </summary>
    /// <remarks>
    /// Called after the panels have drawn, so what decides is whether any of them wanted a
    /// picture. The panel that does is one tab of several, and a camera left pointed at an image
    /// nobody is looking at is a render pass a frame spent on nothing.
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    internal static void Keep(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (_showing is null && _camera.IsNone) return;
        if (_asked == EditorShell.Frame) return;

        if (!_subject.IsNone) ctx.Ecs.Despawn(_subject);
        if (!_camera.IsNone) ctx.Ecs.Despawn(_camera);
        if (!_light.IsNone) ctx.Ecs.Despawn(_light);

        _subject = Entity.None;
        _camera = Entity.None;
        _light = Entity.None;
        _showing = null;

        Owned.Clear();
    }

    /// <summary>Puts everything the scene spawned onto the preview's own layer.</summary>
    private static void Place(BehaviorContext ctx)
    {
        if (_subject.IsNone) return;

        Walk(ctx, _subject);
    }

    /// <summary>Sets the layer on an entity and everything under it, and claims it.</summary>
    private static void Walk(BehaviorContext ctx, Entity entity)
    {
        // Written every frame rather than once, because the same call puts a child on the layer the
        // frame it appears and writing it again costs a component insert on a handful of entities.
        Render.SetLayers(ctx.Ecs, entity, Layer);
        Owned.Add(entity);

        foreach (var child in ctx.Ecs.ChildrenOf(entity)) Walk(ctx, child);
    }

    /// <summary>Points the camera at whatever the model turned out to be.</summary>
    /// <remarks>
    /// Worked out from the bounds rather than fixed, because a model is authored at whatever size
    /// its maker chose and a fixed camera shows a speck or the inside of a wall.
    /// </remarks>
    private static void Frame(BehaviorContext ctx)
    {
        if (_subject.IsNone || _camera.IsNone) return;

        var min = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vec3(float.MinValue, float.MinValue, float.MinValue);
        var any = false;

        Reach(ctx, _subject, ref min, ref max, ref any);

        // Nothing drawn yet, which is ordinary on the frames between asking for a scene and its
        // meshes arriving.
        if (!any) return;

        var middle = (min + max) * 0.5f;
        var size = max - min;
        var across = MathF.Max(size.X, MathF.Max(size.Y, size.Z));

        if (across <= 0f) across = 1f;

        // Far enough back that the whole of it fits the field of view, with a little air.
        var away = across * 1.9f;
        var eye = middle + (new Vec3(0.6f, 0.45f, 1f).Normalized * away);

        ctx.Ecs.Add(_camera, Transform.LookingAt(eye, middle, Vec3.UnitY));
    }

    /// <summary>Grows a box to hold an entity and everything under it.</summary>
    private static void Reach(
        BehaviorContext ctx, Entity entity, ref Vec3 min, ref Vec3 max, ref bool any)
    {
        if (Render.TryGetBounds(entity, out var low, out var high))
        {
            min = new Vec3(MathF.Min(min.X, low.X), MathF.Min(min.Y, low.Y), MathF.Min(min.Z, low.Z));
            max = new Vec3(MathF.Max(max.X, high.X), MathF.Max(max.Y, high.Y), MathF.Max(max.Z, high.Z));
            any = true;
        }

        foreach (var child in ctx.Ecs.ChildrenOf(entity)) Reach(ctx, child, ref min, ref max, ref any);
    }
}

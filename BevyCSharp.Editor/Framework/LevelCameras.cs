using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The cameras a level carries, held off while the level is edited, drawn where they are, and
/// written back on when the level is saved.
/// </summary>
/// <remarks>
/// <para>
/// A camera in a level would draw over the editor's view, since both draw to the window, so each
/// one the level has is turned off as it appears and remembered as turned off by the editor. A
/// save turns those back on for as long as it writes, so the file holds them as the game will run
/// them, and Play and the game start from the level's camera rather than the editor's view. A
/// camera the level holds turned off is left off and written off.
/// </para>
/// <para>
/// Each is drawn as the shape of what it sees, and the editor's view can be moved to look through
/// one, or one moved to where the editor is looking, which is how a camera is placed.
/// </para>
/// </remarks>
public static class LevelCameras
{
    /// <summary>The level's cameras the editor turned off, which a save writes as on.</summary>
    private static readonly HashSet<Entity> Held = [];

    /// <summary>Whether an entity is one of the level's cameras rather than the editor's own.</summary>
    public static bool IsLevelCamera(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.IsAlive(entity) && world.Get<CameraRef>(entity) is not null && !EditorScene.IsEditors(world, entity);
    }

    /// <summary>Every camera the level has.</summary>
    public static IReadOnlyList<Entity> All(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return [.. world.All().Where(entity => IsLevelCamera(world, entity))];
    }

    /// <summary>Turns off each of the level's cameras that is on, and remembers that it did.</summary>
    /// <remarks>Every frame, so a camera spawned, loaded or brought back by an undo is off by the next.</remarks>
    public static void Hold(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        Held.RemoveWhere(entity => !world.IsAlive(entity));

        foreach (var entity in All(world))
        {
            if (world.Get<CameraRef>(entity) is not { } camera || !camera.IsActive) continue;

            camera.IsActive = false;
            Held.Add(entity);
        }
    }

    /// <summary>Runs a save with the cameras the editor holds off turned on, as the level has them.</summary>
    /// <remarks>Turned off again afterward even when the save throws, so a failed write leaves the view as it was.</remarks>
    public static T Saving<T>(EcsWorld world, Func<T> save)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(save);

        var held = Held.Where(world.IsAlive).ToArray();
        foreach (var entity in held) Set(world, entity, true);

        try
        {
            return save();
        }
        finally
        {
            foreach (var entity in held) Set(world, entity, false);
        }
    }

    /// <summary>Draws each of the level's cameras as the shape of what it sees, a selected one brighter.</summary>
    public static void Draw(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var dim = EditorTheme.Linear(new System.Numerics.Vector4(0.55f, 0.75f, 1f, 0.6f));
        var lit = EditorTheme.Linear(new System.Numerics.Vector4(0.7f, 0.85f, 1f, 1f));

        foreach (var entity in All(world))
        {
            var placed = world.TryGet<GlobalTransform>(entity, out var global) ? global.ToTransform() : world.GetOrDefault<Transform>(entity);
            var color = EditorSelection.Holds(entity) ? lit : dim;

            // A cut cone opening the way the camera looks, which is its negative Z, from a point
            // a little in front of it. The shape's own axis is Y, turned onto that.
            const float Depth = 1.2f;
            var forward = placed.Rotation * new Vec3(0f, 0f, -1f);
            var along = placed.Rotation * Quat.FromRotationX(-MathF.PI / 2f);

            Gizmos.Frustum(placed.Translation + forward * (Depth * 0.5f), along, 0.08f, 0.6f, Depth, color, inFront: false);
            Gizmos.Line(placed.Translation, placed.Translation + placed.Rotation * new Vec3(0f, 0.35f, 0f), color, inFront: false);
        }
    }

    /// <summary>Moves the editor's view to stand where a level camera does and look the way it looks.</summary>
    public static void LookThrough(EcsWorld world, Entity camera)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!IsLevelCamera(world, camera)) return;

        Behaviors.FlyCamera.ViewWanted = world.TryGet<GlobalTransform>(camera, out var global)
            ? global.ToTransform()
            : world.GetOrDefault<Transform>(camera);
    }

    /// <summary>Moves a level camera to where the editor is looking from, as a change the history can take back.</summary>
    public static void MoveToView(EcsWorld world, Entity camera)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (!IsLevelCamera(world, camera) || EditorSelection.Camera.IsNone) return;

        var view = world.GetOrDefault<Transform>(EditorSelection.Camera);
        var was = world.GetOrDefault<Transform>(camera);
        var now = was with { Translation = view.Translation, Rotation = view.Rotation };

        world.Set(camera, now);
        EditorHistory.Record(
            $"move {world.NameOf(camera) ?? "camera"} to the view",
            undo => undo.Set(EditorHistory.Resolve(camera), was),
            redo => redo.Set(EditorHistory.Resolve(camera), now));
    }

    private static void Set(EcsWorld world, Entity entity, bool active)
    {
        if (world.Get<CameraRef>(entity) is { } camera) camera.IsActive = active;
    }
}

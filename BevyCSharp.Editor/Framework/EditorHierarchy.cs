using System.Numerics;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Moves an entity under another, or out from under one, where it stands in the world.
/// </summary>
/// <remarks>
/// <para>
/// A transform is kept relative to the parent, so putting a thing under a new parent with its
/// transform untouched moves it, since it is then as far from the new parent as it was from the old
/// one. Every editor keeps a thing where it is when it is dragged in a hierarchy, which takes the
/// transform that puts it in the same place under the new parent, and this works that out.
/// </para>
/// <para>
/// Worked out from the local transforms up each chain rather than from the world transform Bevy
/// keeps, because Bevy works that out once a frame, after the systems that change the hierarchy.
/// A thing spawned this frame has none yet, and a thing moved this frame has last frame's.
/// </para>
/// </remarks>
public static class EditorHierarchy
{
    /// <summary>
    /// Whether <paramref name="child"/> can go under <paramref name="parent"/>, which it cannot
    /// when that would make it its own ancestor.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="child">What would move.</param>
    /// <param name="parent">Where it would go, or none for the top of the world.</param>
    public static bool CanParent(EcsWorld world, Entity child, Entity parent)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (child.IsNone || !world.IsAlive(child)) return false;
        if (parent.IsNone) return true;
        if (parent == child || !world.IsAlive(parent)) return false;

        // Up from the new parent. Meeting the child on the way means the child is above it, and a
        // thing under its own descendant is a loop the hierarchy has no answer for.
        for (var above = world.ParentOf(parent); !above.IsNone; above = world.ParentOf(above))
        {
            if (above == child) return false;
        }

        return true;
    }

    /// <summary>
    /// Puts <paramref name="child"/> under <paramref name="parent"/>, or at the top of the world
    /// when that is none, leaving it where it stands.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="child">What moves.</param>
    /// <param name="parent">Where it goes, or none for the top of the world.</param>
    /// <param name="record">
    /// Whether to record it for undo. Off when it is part of something recorded already, such as a
    /// spawn whose undo despawns the thing wherever it ended up.
    /// </param>
    /// <returns>
    /// Whether it moved. Not when it is already there, or when it would become its own ancestor.
    /// </returns>
    public static bool Reparent(EcsWorld world, Entity child, Entity parent, bool record = true) =>
        Reparent(world, [child], parent, record) > 0;

    /// <summary>
    /// Puts several things under <paramref name="parent"/> at once, each where it stands, as one
    /// step to undo.
    /// </summary>
    /// <remarks>
    /// A thing whose ancestor is among them stays under that ancestor, and the branch moves whole.
    /// Moving each of them on its own would flatten a dragged branch into a row of siblings.
    /// </remarks>
    /// <param name="world">The world.</param>
    /// <param name="children">What moves.</param>
    /// <param name="parent">Where they go, or none for the top of the world.</param>
    /// <param name="record">Whether to record it for undo.</param>
    /// <returns>How many moved.</returns>
    public static int Reparent(EcsWorld world, IReadOnlyCollection<Entity> children, Entity parent, bool record = true)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(children);

        var moving = new HashSet<Entity>(children);
        var steps = new List<Step>();

        foreach (var child in children)
        {
            if (Below(world, child, moving)) continue;
            if (!CanParent(world, child, parent)) continue;

            var previous = world.ParentOf(child);
            if (previous == parent) continue;

            var placed = world.TryGet<Transform>(child, out var before);

            steps.Add(new Step(child, previous, placed, before, placed ? Kept(world, child, parent, before) : before));
        }

        // Every placement worked out before any of them moves, so each is measured against the
        // world as it stood rather than against one half rearranged.
        foreach (var step in steps) Move(world, step.Child, parent, step.Placed, step.After);

        if (steps.Count == 0 || !record) return steps.Count;

        var what = steps.Count > 1
            ? $"{(parent.IsNone ? "unparent" : "parent")} {steps.Count} entities"
            : $"{(parent.IsNone ? "unparent" : "parent")} {Name(world, steps[0].Child)}";

        EditorHistory.Record(
            what,
            undo =>
            {
                foreach (var step in steps)
                    Move(undo, EditorHistory.Resolve(step.Child), EditorHistory.Resolve(step.Previous), step.Placed, step.Before);
            },
            redo =>
            {
                foreach (var step in steps)
                    Move(redo, EditorHistory.Resolve(step.Child), EditorHistory.Resolve(parent), step.Placed, step.After);
            });

        return steps.Count;
    }

    /// <summary>One thing moved: where it was, and the transform it had and has.</summary>
    private readonly record struct Step(Entity Child, Entity Previous, bool Placed, Transform Before, Transform After);

    /// <summary>What a thing is called, for the history.</summary>
    private static string Name(EcsWorld world, Entity entity) =>
        world.NameOf(entity) is { Length: > 0 } called ? called : "entity";

    /// <summary>Whether anything above the thing is also among those moving.</summary>
    private static bool Below(EcsWorld world, Entity entity, HashSet<Entity> moving)
    {
        for (var above = world.ParentOf(entity); !above.IsNone; above = world.ParentOf(above))
        {
            if (moving.Contains(above)) return true;
        }

        return false;
    }

    /// <summary>
    /// The transform that keeps a thing where it stands once it is under the new parent.
    /// </summary>
    private static Transform Kept(EcsWorld world, Entity child, Entity parent, Transform before)
    {
        // Where it is, then what that is as seen from the new parent. Row vectors, as
        // System.Numerics has them, so a point goes through the child's matrix first and the
        // parent's after, and taking the parent back off is multiplying by its inverse on the
        // right.
        var where = InWorld(world, child);
        var under = parent.IsNone ? Matrix4x4.Identity : InWorld(world, parent);

        if (!Matrix4x4.Invert(under, out var back)
            || !Matrix4x4.Decompose(where * back, out var scale, out var rotation, out var translation))
        {
            // A parent scaled to nothing has no inverse, and nothing under it can be placed
            // anywhere, so the thing keeps the transform it had.
            return before;
        }

        return new Transform
        {
            Translation = new Vec3(translation.X, translation.Y, translation.Z),
            Rotation = new Quat(rotation.X, rotation.Y, rotation.Z, rotation.W),
            Scale = new Vec3(scale.X, scale.Y, scale.Z),
        };
    }

    /// <summary>Puts it under a parent, or none, with a transform, as one step either way.</summary>
    private static void Move(EcsWorld world, Entity child, Entity parent, bool placed, Transform transform)
    {
        if (!world.IsAlive(child)) return;

        if (parent.IsNone || !world.IsAlive(parent)) world.ClearParent(child);
        else world.SetParent(child, parent);

        if (placed) world.Set(child, transform);

        // The list is walked again when the number of entities changes, and a move changes where
        // they sit without changing how many there are.
        WorldPanel.Invalidate();
    }

    /// <summary>
    /// Where a thing is in the world, as a matrix made from its transform and every one above it.
    /// </summary>
    private static Matrix4x4 InWorld(EcsWorld world, Entity entity)
    {
        var matrix = Matrix4x4.Identity;

        for (var at = entity; !at.IsNone; at = world.ParentOf(at))
        {
            if (world.TryGet<Transform>(at, out var local)) matrix *= Local(local);
        }

        return matrix;
    }

    /// <summary>A transform as a matrix: scaled, then turned, then moved.</summary>
    private static Matrix4x4 Local(Transform transform) =>
        Matrix4x4.CreateScale(transform.Scale.X, transform.Scale.Y, transform.Scale.Z)
        * Matrix4x4.CreateFromQuaternion(new Quaternion(
            transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W))
        * Matrix4x4.CreateTranslation(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
}

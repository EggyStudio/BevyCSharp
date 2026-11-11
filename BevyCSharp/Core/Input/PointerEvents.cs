using Bevy.Interop;

namespace Bevy;

/// <summary>The kinds of thing a pointer does, by the numbers the bridge reports them under.</summary>
internal static class PointerEvents
{
    /// <summary>Every kind, in the order the bridge numbers them.</summary>
    private static readonly Type[] Kinds =
    [
        typeof(Over), typeof(Out), typeof(Enter), typeof(Leave), typeof(Press), typeof(Release),
        typeof(Click), typeof(Move), typeof(DragStart), typeof(Drag), typeof(DragEnd),
        typeof(DragEnter), typeof(DragOver), typeof(DragLeave), typeof(DragDrop), typeof(Scroll),
        typeof(Cancel),
    ];

    /// <summary>The number the bridge reports <typeparamref name="T"/> under.</summary>
    internal static int KindOf<T>() where T : struct, IPointerEvent => Array.IndexOf(Kinds, typeof(T));

    /// <summary>Runs the observers of what a pointer did, as the bridge reported it.</summary>
    internal static void Trigger(ObserverRegistry registry, in NativePointerEvent reported)
    {
        var entity = new Entity(reported.Entity);
        var pointer = new PointerId((PointerKind)reported.PointerKind, reported.PointerNumber);
        var position = new Vec2(reported.PositionX, reported.PositionY);
        var button = (PointerButton)reported.Button;
        var hit = new PointerHit(
            new Entity(reported.HitCamera),
            reported.HitDepth,
            (reported.HitFlags & 1) != 0 ? new Vec3(reported.HitPositionX, reported.HitPositionY, reported.HitPositionZ) : null,
            (reported.HitFlags & 2) != 0 ? new Vec3(reported.HitNormalX, reported.HitNormalY, reported.HitNormalZ) : null);
        var delta = new Vec2(reported.DeltaX, reported.DeltaY);
        var distance = new Vec2(reported.DistanceX, reported.DistanceY);
        var other = new Entity(reported.Other);
        var count = (int)reported.Count;

        void Run<T>(T happened) where T : struct, IPointerEvent =>
            registry.Trigger(new Pointer<T>(entity, pointer, position, happened));

        switch (reported.Kind)
        {
            case 0: Run(new Over(hit)); break;
            case 1: Run(new Out(hit)); break;
            case 2: Run(new Enter(hit, reported.InBounds != 0)); break;
            case 3: Run(new Leave(hit, reported.InBounds != 0)); break;
            case 4: Run(new Press(button, hit, count)); break;
            case 5: Run(new Release(button, hit)); break;
            case 6: Run(new Click(button, hit, TimeSpan.FromSeconds(reported.Duration), count)); break;
            case 7: Run(new Move(hit, delta)); break;
            case 8: Run(new DragStart(button, hit)); break;
            case 9: Run(new Drag(button, distance, delta)); break;
            case 10: Run(new DragEnd(button, distance)); break;
            case 11: Run(new DragEnter(button, other, hit)); break;
            case 12: Run(new DragOver(button, other, hit)); break;
            case 13: Run(new DragLeave(button, other, hit)); break;
            case 14: Run(new DragDrop(button, other, hit)); break;
            case 15: Run(new Scroll((ScrollUnit)reported.ScrollUnit, reported.ScrollX, reported.ScrollY, hit)); break;
            case 16: Run(new Cancel(hit)); break;
        }
    }
}

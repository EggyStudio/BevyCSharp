using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bevy;

/// <summary>The body of a per-entity behavior method that also takes another of the entity's components.</summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <typeparam name="T1">The other component.</typeparam>
public delegate void BehaviorRunner<T, T1>(ref T component, ref T1 first, Entity entity, BehaviorContext context)
    where T : unmanaged
    where T1 : unmanaged;

/// <summary>The body of a per-entity behavior method that also takes two other of the entity's components.</summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <typeparam name="T1">The first other component.</typeparam>
/// <typeparam name="T2">The second other component.</typeparam>
public delegate void BehaviorRunner<T, T1, T2>(ref T component, ref T1 first, ref T2 second, Entity entity, BehaviorContext context)
    where T : unmanaged
    where T1 : unmanaged
    where T2 : unmanaged;

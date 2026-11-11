using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bevy;

/// <summary>
/// The body of a per-entity behavior method.
/// </summary>
/// <typeparam name="T">The behavior struct, which is also the component type.</typeparam>
/// <param name="component">
/// The entity's component, by reference. Assigning to its fields writes into Bevy's storage.
/// </param>
/// <param name="entity">The entity being processed.</param>
/// <param name="context">Resources for this invocation.</param>
public delegate void BehaviorRunner<T>(ref T component, Entity entity, BehaviorContext context)
    where T : unmanaged;

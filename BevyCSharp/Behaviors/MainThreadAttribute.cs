namespace Bevy;

/// <summary>
/// Runs an instance method for every entity on the main thread, never split across worker threads,
/// for a method that writes a resource, draws interface or plays a sound.
/// </summary>
/// <remarks>
/// An instance method over thousands of entities of one kind is split across the thread pool, a
/// share of the entities on each thread, as <see cref="BehaviorRunners.DefaultParallelThreshold"/>
/// says. That suits a method that only reads and writes the entity's own components and queues
/// commands, and not one reaching what one thread at a time may touch, a resource's fields, the
/// interface being drawn, a sound, or <see cref="BehaviorContext.Ecs"/>, which only the main
/// thread holds. Such a method is marked, and runs on the main thread for every entity, slower
/// for a great many and safe. A static method runs on the main thread already.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MainThreadAttribute : Attribute;

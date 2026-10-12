namespace Bevy;

/// <summary>A system, a function invoked once per stage run with the world.</summary>
/// <param name="world">The world the system operates on.</param>
public delegate void SystemFn(World world);

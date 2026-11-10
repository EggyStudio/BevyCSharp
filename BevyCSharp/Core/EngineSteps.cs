namespace Bevy;

/// <summary>A list of steps an app runs in the order of their places, a lower place first.</summary>
/// <remarks>
/// The areas' plugins add their work in the frame to their app's list (<see cref="App.AreasWork"/>),
/// each step at a place among the others, since an area's work can need another's done first in the
/// same frame, the scene field built before the probes trace through it and the weather's numbers
/// before the clouds, the fog and the rain read them. The places stand a hundred apart, so a step
/// added later finds room between two. A step that needs the renderer, or a window to show it, asks
/// for it itself.
/// </remarks>
internal sealed class EngineSteps<T>
{
    private readonly List<(int Place, Action<T> Step)> _steps = [];
    private Action<T>[] _ordered = [];

    /// <summary>Adds a step at a place among the others.</summary>
    internal void Add(int place, Action<T> step)
    {
        _steps.Add((place, step));
        _ordered = [.. _steps.OrderBy(entry => entry.Place).Select(entry => entry.Step)];
    }

    /// <summary>Runs every step in the order of their places.</summary>
    internal void Run(T on)
    {
        foreach (var step in _ordered) step(on);
    }
}

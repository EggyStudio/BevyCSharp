namespace Bevy;

/// <summary>How <see cref="Gizmos.ShowLights"/> colors the shapes of lights.</summary>
public enum LightGizmoColoring
{
    /// <summary>All in the one color given.</summary>
    Manual = 0,

    /// <summary>A color of its own for each light, chosen from its entity.</summary>
    Varied = 1,

    /// <summary>Each in its light's own color.</summary>
    MatchLight = 2,

    /// <summary>A color for each kind of light, Bevy's choice for each.</summary>
    ByKind = 3,
}

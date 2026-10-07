namespace Bevy;

/// <summary>What an override does to the node it addresses.</summary>
public enum OverrideKind
{
    /// <summary>Writes the fields it names on a component the node has, leaving the rest.</summary>
    Set,

    /// <summary>Puts a component on the node with the fields it names.</summary>
    Add,

    /// <summary>Takes a component off the node.</summary>
    Remove,

    /// <summary>Despawns the node and everything under it.</summary>
    Delete,

    /// <summary>Gives the node another name, which <c>Fields</c> holds.</summary>
    /// <remarks>
    /// Paths keep naming the node by the model's name, so an override recorded after the rename
    /// still finds it when the scene loads and the model spawns it under its own name.
    /// </remarks>
    Rename,

    /// <summary>Puts an entity of the scene the instance is placed in under the node.</summary>
    /// <remarks>
    /// The entity is written in that scene like any of its own, and the override holds its id, so
    /// it goes under the node once the instance has spawned it.
    /// </remarks>
    Child,
}

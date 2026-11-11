using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>What an entity's <see cref="Visibility"/> asks for.</summary>
/// <remarks>
/// The numbers are Bevy's own discriminants, checked against the engine the first time
/// <see cref="Visibility"/> is resolved.
/// </remarks>
public enum VisibilityMode : byte
{
    /// <summary>Take the parent's answer. A root entity set to this is visible.</summary>
    Inherited = 0,

    /// <summary>Hidden, and so is everything below it in the hierarchy.</summary>
    Hidden = 1,

    /// <summary>Visible, even if an ancestor is hidden.</summary>
    Visible = 2,
}

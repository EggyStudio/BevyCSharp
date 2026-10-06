namespace Bevy;

/// <summary>Something a shader's values are set on by name: a material or an instance.</summary>
/// <remarks>
/// Implemented only by <see cref="ShaderMaterial"/> and <see cref="ShaderInstance"/>, so the
/// setters in <see cref="ShaderValues"/> are written once for both.
/// </remarks>
public interface IShaderValues
{
    /// <summary>Which kind of target and which one, as the bridge numbers them.</summary>
    internal (int Kind, long Id) Target { get; }
}

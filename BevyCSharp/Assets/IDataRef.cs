namespace Bevy;

/// <summary>The id of a data asset, whatever its type, as the inspector and a scene read it.</summary>
public interface IDataRef
{
    /// <summary>The asset file's id, or zero for no asset.</summary>
    ulong Id { get; }
}

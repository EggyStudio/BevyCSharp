namespace Bevy;

/// <summary>An ordered collection of plugins added together.</summary>
public interface IPluginGroup
{
    /// <summary>The plugins in this group, with their relative ordering.</summary>
    IEnumerable<(IPlugin Plugin, int Order)> GetPlugins();
}

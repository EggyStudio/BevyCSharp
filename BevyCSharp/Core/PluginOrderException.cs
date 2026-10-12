namespace Bevy;

/// <summary>Raised when a plugin is added before a plugin it depends on.</summary>
public sealed class PluginOrderException : Exception
{
    /// <summary>The plugin that could not be built.</summary>
    public string PluginName { get; }

    /// <summary>The dependency that was missing.</summary>
    public string MissingDependency { get; }

    /// <summary>Creates the exception for a missing dependency.</summary>
    public PluginOrderException(string pluginName, string missingDependency)
        : base($"Plugin '{pluginName}' requires '{missingDependency}' to be added first. "
               + $"Move the AddPlugin call for '{missingDependency}' above it.")
    {
        PluginName = pluginName;
        MissingDependency = missingDependency;
    }
}

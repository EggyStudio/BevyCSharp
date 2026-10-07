using System.Reflection;

namespace Bevy;

/// <summary>What an app lets go of when a script's generation is retired.</summary>
/// <remarks>
/// A generation is an assembly in a context that can be unloaded, and anything of the app's keyed
/// by one of its types keeps the whole of it alive, so a host compiling a script on every save would
/// gather every generation. The tables an app keeps by type drop what such an assembly names.
/// </remarks>
internal static class Retired
{
    /// <summary>
    /// Whether a type is declared by the assembly or is made of one that is, as a generic message
    /// of a script's own type is.
    /// </summary>
    internal static bool Names(Type type, Assembly assembly)
    {
        if (type.Assembly == assembly) return true;
        if (type.HasElementType && type.GetElementType() is { } element) return Names(element, assembly);
        return type.IsGenericType && type.GetGenericArguments().Any(argument => Names(argument, assembly));
    }
}

using System.Reflection;

namespace Bevy;

/// <summary>
/// Where generated behavior registrations announce themselves.
/// </summary>
/// <remarks>
/// <para>
/// The generator emits a module initializer per assembly that calls <see cref="Add"/>. The CLR
/// runs it the first time anything in that assembly is touched, so by the time an
/// <see cref="App"/> is built the registrations are already here, with no assembly
/// scan, no reflection, and nothing for a trimmer to remove by accident.
/// </para>
/// <para>
/// <see cref="BehaviorsPlugin"/> still falls back to scanning for assemblies that are loaded but
/// whose module initializer has not run, as happens when a behavior library is referenced but no
/// code in it has been executed yet. The registry makes the common case fast and trim-safe; the
/// scan makes the uncommon case still work.
/// </para>
/// </remarks>
public static class BehaviorRegistry
{
    private static readonly object Gate = new();
    private static readonly List<Action<App>> Entries = [];
    private static readonly HashSet<string> Sources = [];

    /// <summary>How many registrations have announced themselves.</summary>
    public static int Count
    {
        get
        {
            lock (Gate) return Entries.Count;
        }
    }

    /// <summary>
    /// Records a generated registration. Called from a module initializer; safe to call twice.
    /// </summary>
    /// <remarks>
    /// A script compiled while an app runs loads into a context that can be unloaded, and its host
    /// registers its behaviors into that app alone. Its module initializer calls this as well, and
    /// kept here the delegate would hold the script's generation for the life of the process, every
    /// generation compiled after it beside it, so a registration from such an assembly is passed
    /// over, as <see cref="BehaviorsPlugin"/> passes over one in building an app.
    /// </remarks>
    /// <param name="register">Adds one assembly's behaviors to an app.</param>
    public static void Add(Action<App> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        if (register.Method.Module.Assembly.IsCollectible) return;

        lock (Gate)
        {
            if (!Sources.Add(KeyOf(register.Method))) return;
            Entries.Add(register);
        }
    }

    /// <summary>A snapshot of the recorded registrations.</summary>
    public static IReadOnlyList<Action<App>> Snapshot()
    {
        lock (Gate) return Entries.ToArray();
    }

    /// <summary>True when the registration declared by <paramref name="method"/> is recorded.</summary>
    internal static bool Contains(MethodInfo method)
    {
        lock (Gate) return Sources.Contains(KeyOf(method));
    }

    /// <summary>Identifies a registration method independently of how it was discovered.</summary>
    private static string KeyOf(MethodInfo method) =>
        $"{method.DeclaringType?.Assembly.FullName}|{method.DeclaringType?.FullName}.{method.Name}";
}

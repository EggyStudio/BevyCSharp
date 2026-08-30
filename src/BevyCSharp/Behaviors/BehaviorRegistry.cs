using System.Reflection;

namespace Bevy;

/// <summary>
/// Where generated behaviour registrations announce themselves.
/// </summary>
/// <remarks>
/// <para>
/// The generator emits a module initializer per assembly that calls <see cref="Add"/>. The CLR
/// runs it the first time anything in that assembly is touched, so by the time an
/// <see cref="App"/> is built the registrations are simply sitting here - no assembly scan, no
/// reflection, nothing for a trimmer to remove by accident.
/// </para>
/// <para>
/// <see cref="BehaviorsPlugin"/> still falls back to scanning for assemblies that are loaded
/// but whose module initializer has not run - which is what happens when a behaviour library is
/// referenced but no code in it has been executed yet. The registry is what makes the common
/// case fast and trim-safe; the scan is what makes the uncommon case still work.
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
    /// <param name="register">Adds one assembly's behaviours to an app.</param>
    public static void Add(Action<App> register)
    {
        ArgumentNullException.ThrowIfNull(register);

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

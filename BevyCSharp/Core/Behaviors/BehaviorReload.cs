using System.Reflection;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Bevy.BehaviorReload))]

namespace Bevy;

/// <summary>
/// Registers an assembly's behaviors again once hot reload has changed it, so a system a behavior
/// gains under <c>dotnet watch</c> runs and one it loses stops.
/// </summary>
/// <remarks>
/// <para>
/// Hot reload changes a method's body in place, so a behavior's system runs its new body the frame
/// after with nothing of the library's doing. A method added is another matter. The generator
/// writes it a system and a line in the assembly's registration, and both arrive with the change,
/// but the registration ran once, as the app was built, and Bevy's schedule takes nothing after.
/// The runtime calls <see cref="UpdateApplication"/> after each change it applies, on the thread
/// that applied it, and the change is kept here until each app following changes reads it on its
/// next frame and runs the changed assemblies' registrations again.
/// </para>
/// <para>
/// A change is kept for the process, since several apps may follow, each reading the changes after
/// the last it read. A process makes few, one a save of a file under dotnet watch, so the list
/// stays short.
/// </para>
/// </remarks>
internal static class BehaviorReload
{
    private static readonly object Gate = new();

    // The assembly each change named, or null for a change that named none, which may be any.
    private static readonly List<Assembly?> Changes = [];

    /// <summary>Whether this process can take a change to its code as it runs.</summary>
    /// <remarks>
    /// A program run by <c>dotnet watch</c> can. One run on its own cannot, a shipped game among
    /// them, and an app there follows no changes and keeps the cost of the dispatchers it would
    /// need out of its frames.
    /// </remarks>
    internal static bool Possible => MetadataUpdater.IsSupported;

    /// <summary>How many changes the process has taken.</summary>
    internal static int Count
    {
        get
        {
            lock (Gate) return Changes.Count;
        }
    }

    /// <summary>Keeps the assemblies of the types a change of the program's code touched.</summary>
    /// <remarks>
    /// The runtime calls it by its name after it applies a change. A change it gives no types for
    /// may have touched any assembly, so every one is registered again.
    /// </remarks>
    /// <param name="updated">The types the change touched, or nothing where the runtime does not say.</param>
    internal static void UpdateApplication(Type[]? updated)
    {
        lock (Gate)
        {
            if (updated is null)
            {
                Changes.Add(null);
                return;
            }

            foreach (var assembly in updated.Select(type => type.Assembly).Distinct())
                Changes.Add(assembly);
        }
    }

    /// <summary>
    /// The assemblies changed after the first <paramref name="seen"/> changes, with null among them
    /// for a change that named none, and <paramref name="seen"/> moved past them.
    /// </summary>
    internal static IReadOnlyList<Assembly?> Since(ref int seen)
    {
        lock (Gate)
        {
            if (seen >= Changes.Count) return [];

            var since = Changes.GetRange(seen, Changes.Count - seen);
            seen = Changes.Count;
            return since;
        }
    }

    /// <summary>
    /// Has <paramref name="app"/> run the registrations of each assembly changed while it runs again,
    /// at the start of its next frame.
    /// </summary>
    /// <param name="app">The app, not yet running, since its dispatchers are put in before it runs.</param>
    /// <param name="source">The tag the registrations' systems carry, which those registered again carry too.</param>
    /// <param name="registrations">The registrations the app ran as it was built.</param>
    internal static void Follow(App app, string source, IReadOnlyList<Action<App>> registrations)
    {
        app.EnableDynamicSystems();
        app.FollowsChanges = true;

        var seen = Count;
        app.AddSystem(Stage.First, new SystemDescriptor(_ =>
        {
            var changed = Since(ref seen);
            if (changed.Count == 0) return;

            foreach (var register in registrations)
            {
                var assembly = register.Method.Module.Assembly;
                if (changed.Contains(null) || changed.Contains(assembly)) app.RegisterAgain(assembly, source, register);
            }
        }, "Behaviors.Reload")
        {
            // Its own tag, since one of the library's assemblies registered again would otherwise
            // take this system for one of its own that it registers no more.
            Source = "Core.BehaviorReload",
        });
    }
}

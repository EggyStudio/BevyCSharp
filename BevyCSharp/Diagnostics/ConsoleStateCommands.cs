using System.Globalization;
using Bevy.Interop;

namespace Bevy;

/// <summary>A game's states read and moved from the console, the command line and the editor's console alike.</summary>
/// <remarks>
/// <para>
/// A script playing a game moves it from screen to screen where no key of the game's does, ending a
/// round to start the next, and a person at the console skips to the screen they are working on.
/// A state is named by its enum's name, or by its full name where two namespaces each declare one
/// of that name, and a value by its member's name, both read without regard to case.
/// </para>
/// <para>
/// A move is queued as <see cref="App.SetState{TState}"/> queues it, and applied at the next
/// transition, so a script asks <c>frames.wait</c> for a frame or two before it reads what the
/// move did.
/// </para>
/// </remarks>
internal static unsafe class ConsoleStateCommands
{
    /// <summary>Says the value each state holds, or the value of one.</summary>
    [Command("state.get", "The value each state holds, or one state's: state.get [State]")]
    internal static string Get(string state = "")
    {
        var claimed = StateRegistry.Claimed();
        if (claimed.Count == 0) return "the app has no states";

        if (state.Length == 0)
            return string.Join(
                "\n",
                claimed.OrderBy(each => each.State.Name, StringComparer.Ordinal).Select(each => $"{each.State.Name} {ValueOf(each.State, each.Slot)}"));

        return Find(claimed, state, out var type, out var slot) ?? ValueOf(type!, slot);
    }

    /// <summary>Moves a state to a value at its next transition.</summary>
    [Command("state.set", "Moves a state to a value at its next transition: state.set <State> <Value>")]
    internal static string Set(string state, string value)
    {
        if (Find(StateRegistry.Claimed(), state, out var type, out var slot) is { } missing) return missing;

        if (!Enum.TryParse(type!, value, ignoreCase: true, out var parsed))
            return $"{type!.Name} has no value {value}, only {string.Join(", ", Enum.GetNames(type))}";

        Native.Check(
            Native.bcs_state_set(slot, Convert.ToInt32(parsed, CultureInfo.InvariantCulture)),
            $"setting state {type!.Name}");
        return $"{type.Name} goes to {parsed} at its next transition";
    }

    /// <summary>The state named, or the sentence that says why there is none.</summary>
    private static string? Find(List<(Type State, int Slot)> claimed, string name, out Type? type, out int slot)
    {
        var byName = claimed.Where(each => string.Equals(each.State.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        var named = byName.Count == 1
            ? byName
            : [.. claimed.Where(each => string.Equals(each.State.FullName, name, StringComparison.OrdinalIgnoreCase))];

        if (named.Count == 1)
        {
            (type, slot) = named[0];
            return null;
        }

        (type, slot) = (null, 0);
        return byName.Count > 1
            ? $"more than one state is called {name}, so name it in full"
            : $"no state is called {name}, only {string.Join(", ", claimed.Select(each => each.State.Name).Order(StringComparer.Ordinal))}";
    }

    /// <summary>The member a state's slot holds, or a word for a state that is not there right now.</summary>
    /// <remarks>
    /// A sub-state while its parent holds another value is not there at all, which is an answer
    /// and not a failure, as <see cref="App.TryState{TState}"/> has it.
    /// </remarks>
    private static string ValueOf(Type state, int slot)
    {
        int raw;
        var status = Native.bcs_state_get(slot, &raw);
        if (status == NativeStatus.NotPresent) return "absent";

        Native.Check(status, $"reading state {state.Name}");
        return Enum.ToObject(state, raw).ToString() ?? raw.ToString(CultureInfo.InvariantCulture);
    }
}

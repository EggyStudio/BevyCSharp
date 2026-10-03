using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The rules computed states added with a function are worked out by, and the one callback the
/// bridge calls them through.
/// </summary>
/// <remarks>
/// Bevy works a computed state out through a plain function with the source's value, so the bridge
/// keeps one pointer to a function here, which it calls with the computed state's number, and this
/// keeps the rule for each number. Static, since the callback can hold no context, and kept for the
/// process, since a rule is a function of a value and holds nothing of an app.
/// </remarks>
internal static unsafe class ComputedRules
{
    private static readonly Dictionary<int, Func<int, int?>> Rules = [];
    private static readonly Lock Gate = new();
    private static bool _registered;

    /// <summary>Keeps the rule for a computed state, giving the bridge the callback the first time.</summary>
    internal static void Set(int slot, Func<int, int?> rule)
    {
        lock (Gate)
        {
            Rules[slot] = rule;

            if (_registered) return;

            Native.Check(Native.bcs_computed_rule(&Compute), "giving the bridge the computed state rules");
            _registered = true;
        }
    }

    /// <summary>What the bridge calls, with the state's number, the source's value, and where the answer goes.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Compute(int slot, int source, int* result)
    {
        Func<int, int?>? rule;
        lock (Gate) Rules.TryGetValue(slot, out rule);

        if (rule is null) return 0;

        try
        {
            if (rule(source) is not { } value) return 0;

            *result = value;
            return 1;
        }
        catch (Exception error)
        {
            // Nothing may unwind into Bevy's transition, so a rule that throws answers nothing.
            Console.Error.WriteLine($"[BevyCSharp] the rule for computed state {slot} threw: {error.Message}");
            return 0;
        }
    }
}

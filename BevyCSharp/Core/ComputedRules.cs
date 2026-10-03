using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The rules computed and joint states are worked out by, and the two callbacks the bridge calls
/// them through.
/// </summary>
/// <remarks>
/// <para>
/// Bevy works a computed state out through a plain function with the source's value, so the bridge
/// keeps one pointer to a function here, which it calls with the computed state's number, and this
/// keeps the rule for each number. A joint state is the same with the value of every axis in place
/// of one. Static, since the callback can hold no context.
/// </para>
/// <para>
/// The rules belong to the app that added them and are forgotten when it is disposed
/// (<see cref="Forget"/>), so a rule and whatever it captured last only as long as the app. A
/// later app reaching a slot a forgotten rule held cannot reach the rule anyway, since the bridge
/// marks each computed state as worked out by a rule or by a table when it is added, and a joint
/// is always added with its rule.
/// </para>
/// </remarks>
internal static unsafe class ComputedRules
{
    private static readonly Dictionary<int, Func<int, int?>> Rules = [];
    private static readonly Dictionary<int, Func<int[], uint, int?>> Joints = [];
    private static readonly Lock Gate = new();
    private static bool _registered;
    private static bool _jointsRegistered;

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

    /// <summary>
    /// Keeps the rule for a joint state, giving the bridge the callback the first time.
    /// </summary>
    /// <param name="slot">The joint's number among joints.</param>
    /// <param name="rule">
    /// What the joint is, given the value of every axis in slot order and a bit for each that holds
    /// a state, or nothing.
    /// </param>
    internal static void SetJoint(int slot, Func<int[], uint, int?> rule)
    {
        lock (Gate)
        {
            Joints[slot] = rule;

            if (_jointsRegistered) return;

            Native.Check(Native.bcs_joint_rule(&Joint), "giving the bridge the joint state rules");
            _jointsRegistered = true;
        }
    }

    /// <summary>Forgets every rule, as the app that added them is disposed.</summary>
    internal static void Forget()
    {
        lock (Gate)
        {
            Rules.Clear();
            Joints.Clear();
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

    /// <summary>
    /// What the bridge calls for a joint, with its number, the value of every axis, a bit for each
    /// axis that holds a state, and where the answer goes.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Joint(int slot, int* values, uint present, int* result)
    {
        Func<int[], uint, int?>? rule;
        lock (Gate) Joints.TryGetValue(slot, out rule);

        if (rule is null) return 0;

        try
        {
            var axes = new int[StateRegistry.SlotCount];
            for (var axis = 0; axis < axes.Length; axis++) axes[axis] = values[axis];

            if (rule(axes, present) is not { } value) return 0;

            *result = value;
            return 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"[BevyCSharp] the rule for joint state {slot} threw: {error.Message}");
            return 0;
        }
    }
}

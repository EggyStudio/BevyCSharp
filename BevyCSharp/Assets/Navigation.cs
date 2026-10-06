using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Moves the input focus between interface nodes by direction, with the arrows or a pad, and keeps
/// the edges a game draws where the nearest node is not the one meant, Bevy's directional
/// navigation.
/// </summary>
/// <remarks>
/// <para>
/// A node carrying Bevy's <c>AutoDirectionalNavigation</c>, put on with
/// <c>ecs.Insert&lt;AutoDirectionalNavigationRef&gt;(button)</c>, is reached from its neighbors by
/// where it is on the screen, the nearest in each direction that lines up well enough, as
/// <c>AutoNavigationConfigRef</c> sets. An edge drawn here goes before that search, so a row can
/// wrap to the start of the next, a direction be blocked, or a node far from the others be joined
/// to them.
/// </para>
/// <para>
/// Nothing moves the focus by itself. A game calls <see cref="Move"/> on the keys or buttons it
/// chooses, as Bevy's examples do on the arrows and the pad's directions, and draws the focus as it
/// likes, as <c>InputFocusVisibleRef</c> says whether to. Tab and Shift+Tab move it along the tab
/// order beside this, which <see cref="Ui.Navigate"/> reads.
/// </para>
/// <para>
/// Needs a bridge with the renderer, which <see cref="App.HasRenderer"/> reports, and a call from
/// inside a system, where the world is on loan.
/// </para>
/// </remarks>
public static unsafe class Navigation
{
    /// <summary>
    /// Moves the input focus to the node beside the one holding it in a direction, and answers the
    /// node, or null where nothing lies that way, an edge blocks it or nothing holds the focus.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>AutoDirectionalNavigator::navigate</c>, an edge drawn here first and the nearest
    /// node on the screen otherwise. The focus moves as <see cref="Ui.Focus"/> moves it, navigated
    /// to, so a game reads the node that has it from <c>InputFocusRef</c> as from the answer.
    /// </remarks>
    /// <param name="direction">Which way, north being up the screen.</param>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static Entity? Move(CompassOctant direction)
    {
        ulong moved;
        var status = Native.bcs_nav_move((int)direction, &moved);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Moving the input focus by direction");
        Native.Check(status, $"moving the input focus {direction}");
        return status == 1 ? new Entity(moved) : null;
    }

    /// <summary>Draws an edge from one node to another in a direction, and back the opposite way where asked.</summary>
    /// <remarks>
    /// Bevy's <c>add_edge</c>, or <c>add_symmetrical_edge</c> with <paramref name="bothWays"/>,
    /// which has the second node lead back to the first going south where the first leads to it
    /// going north. An edge drawn again from the same node in the same direction replaces it.
    /// </remarks>
    /// <param name="from">The node the edge leaves.</param>
    /// <param name="to">The node it reaches.</param>
    /// <param name="direction">Which way it leaves.</param>
    /// <param name="bothWays">Whether the second node leads back the opposite way.</param>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static void AddEdge(Entity from, Entity to, CompassOctant direction, bool bothWays = false) =>
        Edge(from, to, direction, bothWays ? 1 : 0, "drawing an edge");

    /// <summary>Blocks a direction a node would otherwise be left by, and the opposite one from another node where asked.</summary>
    /// <remarks>
    /// Bevy's <c>block_edge</c>, so a move that way from the node goes nowhere rather than to the
    /// nearest node, as Bevy's grid that moves only along its rows has it. A blocked north leaves
    /// north-east and north-west open. With <paramref name="other"/> as well,
    /// <c>block_symmetrical_edge</c>, which blocks that node going the opposite way too.
    /// </remarks>
    /// <param name="node">The node not left that way.</param>
    /// <param name="direction">Which way.</param>
    /// <param name="other">A node not left the opposite way either, or none.</param>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static void BlockEdge(Entity node, CompassOctant direction, Entity other = default) =>
        Edge(node, other, direction, other.IsNone ? 2 : 3, "blocking an edge");

    /// <summary>Draws edges between nodes in their order in a direction, each to the next and back, and around from the last where asked.</summary>
    /// <remarks>
    /// Bevy's <c>add_edges</c>, or <c>add_looping_edges</c> with <paramref name="looping"/>, which
    /// joins the last to the first as well, so a column whose north is its south's way round goes
    /// on past either end.
    /// </remarks>
    /// <param name="nodes">The nodes, in the order the direction passes them.</param>
    /// <param name="direction">Which way each leads to the next.</param>
    /// <param name="looping">Whether the last leads on to the first.</param>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static void AddEdges(ReadOnlySpan<Entity> nodes, CompassOctant direction, bool looping = false)
    {
        var bits = new ulong[nodes.Length];
        for (var i = 0; i < nodes.Length; i++) bits[i] = nodes[i].Bits;

        int status;
        fixed (ulong* at = bits)
        {
            status = Native.bcs_nav_edges(at, bits.Length, (int)direction, looping ? 1 : 0);
        }

        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Drawing navigation edges");
        Native.Check(status, $"drawing edges between {nodes.Length} nodes");
    }

    /// <summary>Takes a node's edges out, those from it and to it, as one taken off the screen needs.</summary>
    /// <param name="node">The node.</param>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static void Forget(Entity node)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(node.IsNone, true, nameof(node));
        Forgetting(node, $"taking the edges of {node} out");
    }

    /// <summary>Takes every edge out, leaving the nearest nodes on the screen.</summary>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static void Clear() => Forgetting(Entity.None, "taking every edge out");

    private static void Edge(Entity from, Entity to, CompassOctant direction, int kind, string doing)
    {
        var status = Native.bcs_nav_edge(from.Bits, to.Bits, (int)direction, kind);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Drawing navigation edges");
        Native.Check(status, $"{doing} from {from} {direction}");
    }

    private static void Forgetting(Entity node, string doing)
    {
        var status = Native.bcs_nav_forget(node.Bits);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Taking navigation edges out");
        Native.Check(status, doing);
    }
}

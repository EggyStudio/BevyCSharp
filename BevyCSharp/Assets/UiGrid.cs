namespace Bevy;

using System.Runtime.InteropServices;
using Bevy.Interop;

/// <summary>Laying a node's children out on a grid.</summary>
public static unsafe class UiGrid
{
    /// <summary>
    /// Lays a node's children out on a grid.
    /// </summary>
    /// <remarks>
    /// Applied to a node that already exists rather than passed with one, because the tracks are
    /// lists and a list has no room in the flat settings every other field arrives in. The node is
    /// told to lay out as a grid here, so nothing has to set <see cref="UiSettings.Display"/> as
    /// well.
    /// </remarks>
    /// <param name="node">The node whose children are laid out.</param>
    /// <param name="settings">The tracks and how they are filled.</param>
    /// <exception cref="BevyNativeException">The node is gone, or this build has no renderer.</exception>
    /// <example>
    /// <code>
    /// UiGrid.Set(panel, new GridSettings
    /// {
    ///     Columns = [Track.Px(96f).Filling(), ],
    ///     AutoRows = [Track.Px(96f)],
    /// });
    /// </code>
    /// </example>
    public static void Set(Entity node, GridSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var rows = Described(settings.Rows);
        var columns = Described(settings.Columns);
        var autoRows = Described(settings.AutoRows);
        var autoColumns = Described(settings.AutoColumns);

        // Pinned rather than copied to the stack, because the lists are as long as the caller made
        // them and a grid of a hundred columns is a reasonable thing to ask for.
        fixed (NativeGridTrack* rowsAt = rows)
        fixed (NativeGridTrack* columnsAt = columns)
        fixed (NativeGridTrack* autoRowsAt = autoRows)
        fixed (NativeGridTrack* autoColumnsAt = autoColumns)
        {
            var config = new NativeUiGridConfig
            {
                AutoFlow = (int)settings.Flow,
                Rows = rowsAt,
                RowCount = rows?.Length ?? 0,
                Columns = columnsAt,
                ColumnCount = columns?.Length ?? 0,
                AutoRows = autoRowsAt,
                AutoRowCount = autoRows?.Length ?? 0,
                AutoColumns = autoColumnsAt,
                AutoColumnCount = autoColumns?.Length ?? 0,
                JustifyItems = (int)settings.Align,
            };

            Interop.Native.Check(
                Interop.Native.bcs_ui_set_grid(node.Bits, &config),
                $"laying {node} out as a grid");
        }
    }

    /// <summary>
    /// Places one child on its parent's grid.
    /// </summary>
    /// <remarks>
    /// Only a child of a node that <see cref="Set"/> was called on has a grid to be placed on. A
    /// child with no placement of its own is put wherever the flow reaches next, which suits most
    /// of them.
    /// </remarks>
    /// <param name="node">The child being placed.</param>
    /// <param name="placement">Where it sits.</param>
    /// <exception cref="BevyNativeException">The node is gone, or this build has no renderer.</exception>
    public static void Place(Entity node, GridPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        Interop.Native.Check(
            Interop.Native.bcs_ui_set_grid_placement(
                node.Bits,
                placement.Row,
                placement.RowSpan,
                placement.Column,
                placement.ColumnSpan,
                (int)placement.Align),
            $"placing {node} on a grid");
    }

    /// <summary>One list of tracks as the bridge takes it, or nothing where none was given.</summary>
    private static NativeGridTrack[]? Described(IReadOnlyList<Track>? tracks)
    {
        if (tracks is null || tracks.Count == 0) return null;

        var native = new NativeGridTrack[tracks.Count];

        for (var i = 0; i < tracks.Count; i++)
        {
            native[i] = new NativeGridTrack
            {
                Kind = tracks[i].Kind,
                Value = tracks[i].Value,
                Repeat = tracks[i].Repeat,
            };
        }

        return native;
    }
}

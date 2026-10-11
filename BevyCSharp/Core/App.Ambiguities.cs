using System.Text;

namespace Bevy;

public sealed unsafe partial class App
{
    private readonly Dictionary<Stage, (AmbiguityReport Report, Func<string, bool>? Among)> _ambiguityReports = [];

    /// <summary>
    /// Reports, as the app starts, the systems of <paramref name="stage"/> whose declared access
    /// conflicts and whose order nothing states, as Bevy's <c>ambiguity_detection</c> in a
    /// schedule's build settings does.
    /// </summary>
    /// <param name="stage">The stage to look through.</param>
    /// <param name="report">A warning in the log, the app refusing to start, or nothing.</param>
    /// <param name="among">
    /// The systems a pair is looked for among, by their names, or every system of the stage where
    /// none is given, for a game whose stage also holds what its plugins add.
    /// </param>
    /// <remarks>
    /// <para>
    /// Every C# system takes the whole world, so Bevy runs them one at a time and would report every
    /// pair of them. The report is made here instead, over what each system declares with
    /// <see cref="SystemDescriptor.Read{T}"/> and <see cref="SystemDescriptor.Write{T}"/>, a pair
    /// conflicting where one writes what the other reads or writes. A system that declares nothing
    /// is left out, since nothing is known of what it touches.
    /// </para>
    /// <para>
    /// Two systems are ordered where one runs after the other through
    /// <see cref="SystemDescriptor.After"/>, <see cref="SystemDescriptor.Before"/> or
    /// <see cref="Chain"/>, directly or through others of the stage, and a pair one of which names
    /// the other with <see cref="SystemDescriptor.AmbiguousWith"/> is left out, as in Bevy.
    /// </para>
    /// </remarks>
    public App DetectAmbiguities(Stage stage, AmbiguityReport report = AmbiguityReport.Warn, Func<string, bool>? among = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ambiguityReports[stage] = (report, among);
        return this;
    }

    /// <summary>
    /// The pairs of systems of <paramref name="stage"/> whose declared access conflicts and whose
    /// order nothing states, in the order they were added, which
    /// <see cref="DetectAmbiguities"/> reports.
    /// </summary>
    /// <param name="stage">The stage to look through.</param>
    /// <param name="among">The systems to look for a pair among, by their names, or all of them.</param>
    /// <remarks>An order stated through a system left out still orders the two it stands between.</remarks>
    public IReadOnlyList<SystemAmbiguity> AmbiguitiesIn(Stage stage, Func<string, bool>? among = null)
    {
        var all = _systems.Where(system => system.Stage == stage && system.NativeId >= 0 && !system.IsRemoved).ToList();

        // Which systems each runs before, by the names the order is given in.
        var next = all.ToDictionary(system => system, system => all.Where(other =>
            other != system
            && (other.Descriptor.RunsAfter.Contains(system.Descriptor.Name) || system.Descriptor.RunsBefore.Contains(other.Descriptor.Name))).ToList());

        bool Reaches(RegisteredSystem from, RegisteredSystem to)
        {
            var seen = new HashSet<RegisteredSystem>();
            var left = new Stack<RegisteredSystem>([from]);
            while (left.TryPop(out var at))
            {
                foreach (var after in next[at])
                {
                    if (after == to) return true;
                    if (seen.Add(after)) left.Push(after);
                }
            }

            return false;
        }

        var declared = all.Where(system => system.Descriptor.HasExplicitAccess && among?.Invoke(system.Descriptor.Name) != false).ToList();
        var found = new List<SystemAmbiguity>();
        for (var i = 0; i < declared.Count; i++)
        {
            for (var j = i + 1; j < declared.Count; j++)
            {
                var (a, b) = (declared[i].Descriptor, declared[j].Descriptor);
                if (a.AmbiguousWithNames.Contains(b.Name) || b.AmbiguousWithNames.Contains(a.Name)) continue;

                var conflicts = a.Writes.Where(type => b.Writes.Contains(type) || b.Reads.Contains(type))
                    .Concat(a.Reads.Where(b.Writes.Contains))
                    .Distinct()
                    .ToList();
                if (conflicts.Count == 0 || Reaches(declared[i], declared[j]) || Reaches(declared[j], declared[i])) continue;

                found.Add(new SystemAmbiguity(a.Name, b.Name, conflicts));
            }
        }

        return found;
    }

    /// <summary>Says or refuses the ambiguities of each stage asked for, as the app starts.</summary>
    /// <exception cref="InvalidOperationException">A stage asked to refuse has an ambiguity.</exception>
    private void ReportAmbiguities()
    {
        foreach (var (stage, (report, among)) in _ambiguityReports)
        {
            if (report == AmbiguityReport.Ignore) continue;
            var found = AmbiguitiesIn(stage, among);
            if (found.Count == 0) continue;

            // Bevy's words, the C# names of the calls that order a pair in place of its own.
            var message = new StringBuilder($"{stage} schedule built successfully, however: {found.Count} pairs of systems with "
                + "conflicting data access have indeterminate execution order. Consider adding `After`, `Before`, or "
                + "`AmbiguousWith` relationships between these:\n");
            foreach (var ambiguity in found)
            {
                message.Append($" -- {ambiguity.First} and {ambiguity.Second}\n");
                message.Append($"    conflict on: [{string.Join(", ", ambiguity.Conflicts.Select(type => $"\"{type.Name}\""))}]\n");
            }

            if (report == AmbiguityReport.Error) throw new InvalidOperationException(message.ToString());
            Log.Warn(message.ToString().TrimEnd('\n'));
        }
    }
}

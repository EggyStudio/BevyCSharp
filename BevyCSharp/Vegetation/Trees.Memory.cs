namespace Bevy;

// What the trees hold, read by species, variant and part, for the readings of what a variant and a
// forest hold and for the tests that hold them to it.
public static partial class Trees
{
    /// <summary>
    /// The bytes a variant of a species holds by part, its arrays' elements counted and not the
    /// objects round them, those the process keeps for as long as it runs marked as lasting and
    /// those an app holds while it draws the variant first, growing and shaping it here.
    /// </summary>
    internal static IReadOnlyList<(string Part, bool Lasting, long Bytes)> HeldBy(TreeSpecies species, int variant)
    {
        var grown = Growing.GetOrAdd((species, variant), key => new Lazy<Task<Grown>>(() => Task.Run(() => GrowVariant(key.Species, key.Variant)))).Value.Result;
        return [.. Held(grown).Select(part => (part.Part, true, part.Bytes)), .. Held(ShapeVariant(species, variant, grown.Skeleton)).Select(part => (part.Part, false, part.Bytes))];
    }

    /// <summary>
    /// The bytes the variants grown so far hold for the process, and those the running app holds of
    /// the variants it is drawing first, their shapes and their pictures' bytes, growing none.
    /// </summary>
    internal static (long Process, long Drawing) HeldNow()
    {
        var process = Growing.Values
            .Where(growing => growing.IsValueCreated && growing.Value.IsCompletedSuccessfully)
            .Sum(growing => Held(growing.Value.Result).Sum(part => part.Bytes));
        if (App.Running is not { } app || !TakenPictures.TryGetValue(app, out var all)) return (process, 0);

        var drawing = 0L;
        foreach (var pictures in all.Values)
        {
            if (pictures.Shapes is { } shapes) drawing += Held(shapes).Sum(part => part.Bytes);
            drawing += (pictures.Picture?.Length ?? 0) + (pictures.ImpostorPicture?.Length ?? 0);
            if (pictures.Levels is { IsCompletedSuccessfully: true } levels) drawing += levels.Result.Colors.Length + levels.Result.Normals.Length;
            if (pictures.ImpostorLevels is { IsCompletedSuccessfully: true } frames)
                drawing += frames.Result.Colors.Length + frames.Result.Normals.Length + frames.Result.Faces.Length;
        }

        return (process, drawing);
    }

    /// <summary>The bytes a variant grown holds, by part, a node of its skeleton a parent, a place and a radius.</summary>
    private static IReadOnlyList<(string Part, long Bytes)> Held(Grown grown) =>
    [
        ("skeleton", grown.Skeleton.Nodes.Count * 20L),
        ("capsules", grown.Capsules.Count * 28L),
    ];

    /// <summary>The bytes a variant's shapes hold, by part, the data its two cards of a branch share counted once.</summary>
    private static IReadOnlyList<(string Part, long Bytes)> Held(Shapes shapes)
    {
        var held = new List<(string, long)>
        {
            ("stems, full", Bytes(shapes.Stems[(int)TreeDetail.Full])),
            ("stems, reduced", Bytes(shapes.Stems[(int)TreeDetail.Reduced])),
            ("leaves, full", Bytes(shapes.Leaves[(int)TreeDetail.Full])),
            ("leaves, reduced", Bytes(shapes.Leaves[(int)TreeDetail.Reduced])),
        };
        if (shapes.Cards is { } cards)
        {
            held.Add(("far level's limbs and quads", Bytes(cards.Limbs) + Bytes(cards.Quads)));
            held.Add(("far level's cards' stems", cards.Cards.Select(card => card.Stems).Distinct().Sum(Bytes)));
            held.Add(("far level's cards' leaves", cards.Cards.Select(card => card.Leaves).Distinct().Sum(Bytes)));
        }

        if (shapes.Impostor is { } impostor) held.Add(("impostor's quad", Bytes(impostor.Quad)));
        return held;
    }

    /// <summary>The bytes a mesh's data holds in its arrays, its streams' among them.</summary>
    internal static long Bytes(MeshData mesh) =>
        mesh.Positions.Length * 12L + (mesh.Normals?.Length ?? 0) * 12L + (mesh.Uvs?.Length ?? 0) * 4L + (mesh.SecondUvs?.Length ?? 0) * 4L
        + (mesh.Colors?.Length ?? 0) * 4L + (mesh.Indices?.Length ?? 0) * 4L + (mesh.Streams?.Sum(stream => (long)stream.Data.Length) ?? 0);
}

namespace Bevy;

// The far level of a planted tree, its trunk and thickest limbs as tubes and the foliage each limb
// carries as one card, a picture of that limb's branches and leaves, worked out as a variant grows
// and pictured once an app by a camera of its own.
public static partial class Trees
{
    // The stems the far level draws as tubes, the thickest of the tree's, the trunk first, each with
    // the foliage it carries through stems thinner than all of them drawn as one card.
    private const int CardLimbs = 16;

    // The share of a cell left round a card's picture, so its edge is not filtered into its
    // neighbor's as the atlas's smaller levels are read.
    private const float CardMargin = 0.04f;

    /// <summary>The far level of a variant as its growth works it out, before its pictures are taken.</summary>
    /// <param name="Limbs">The thickest stems as tubes, with a third of their sides.</param>
    /// <param name="Cards">Each card, a rectangle in the tree's space, and what it pictures.</param>
    /// <param name="Across">How many cells a side the atlas is cut into.</param>
    /// <param name="Quads">The cards as the far level draws them, a rectangle each with its cell of the atlas and the way out of the crown.</param>
    internal sealed record CardPlan(MeshData Limbs, IReadOnlyList<PlannedCard> Cards, int Across, MeshData Quads);

    /// <summary>
    /// A card of a variant, a rectangle in the tree's space, its middle, its right, up and face, its
    /// half width and height, the stems and leaves it pictures, and its cell of the atlas, a column
    /// and a row, with the scale from the tree's meters to cells it is pictured at.
    /// </summary>
    internal sealed record PlannedCard(Vec3 Middle, Vec3 Right, Vec3 Up, Vec3 Face, float HalfWidth, float HalfHeight, MeshData Stems, MeshData Leaves, float Scale, int Column, int Row);

    /// <summary>
    /// Works out a variant's far level from its skeleton and its leaves' cards, or nothing where the
    /// species has no cards to picture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CardLimbs"/> thickest stems are kept as tubes with a third of their sides, the
    /// trunk among them. Every other stem belongs to the nearest kept stem it grows from, and every
    /// leaf to the stem its twig is, so each kept stem gathers the branches and leaves it carries
    /// through thinner stems, which become its card. A tree whose limbs are few gathers more into
    /// each card, and a kept stem that carries nothing has none.
    /// </para>
    /// <para>
    /// A card's plane is the one its leaves spread most in, the two longest axes of their spread,
    /// facing out of the crown, upright where it can be, and as large as its stems' and leaves'
    /// outline in it. The pictures are laid in an atlas of square cells a card each, each picture as
    /// large as its cell holds it without stretching.
    /// </para>
    /// </remarks>
    private static CardPlan? PlanCards(TreeSkeleton skeleton, Cards leaves, LimbPlan limbPlan)
    {
        var nodes = skeleton.Nodes;
        if (leaves.Tips.Count == 0) return null;
        var carried = Carried(nodes);
        var (stemOf, starts, kept, owner) = (limbPlan.StemOf, limbPlan.Starts, limbPlan.Kept, limbPlan.Owner);

        var gathered = new Dictionary<int, (List<int> Stems, List<int> Cards)>();
        (List<int> Stems, List<int> Cards) Of(int keptStem) =>
            gathered.TryGetValue(keptStem, out var group) ? group : gathered[keptStem] = ([], []);
        foreach (var start in starts.Where(start => !kept.Contains(start))) Of(owner[start]).Stems.Add(start);
        for (var card = 0; card < leaves.Tips.Count; card++) Of(owner[stemOf[leaves.Tips[card]]]).Cards.Add(card);

        // The middle of every leaf, which a card's face looks away from.
        var middles = Enumerable.Range(0, leaves.Tips.Count).Select(card => CardMiddle(leaves.Positions, card)).ToArray();
        var crown = middles.Aggregate(Vec3.Zero, (sum, place) => sum + place) * (1f / middles.Length);

        var limbs = new EditMesh().SlotRename(EditMesh.DefaultSlot, BarkSlot);
        var runs = new List<(int Start, int First, int Faces)>();
        foreach (var start in kept.Order())
        {
            var first = limbs.FaceCount;
            Stem(limbs, nodes, carried, start, 3);
            runs.Add((start, first, limbs.FaceCount - first));
        }

        var groups = gathered.Where(group => group.Value.Cards.Count > 0).OrderBy(group => group.Key).ToArray();
        var cells = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(groups.Length * 2)));
        var planned = new List<PlannedCard>();
        var quads = new List<(PlannedCard Card, Vec3 Out, int Limb)>();
        foreach (var (keptStem, (stemStarts, cardsOf)) in groups)
        {
            var branches = new EditMesh().SlotRename(EditMesh.DefaultSlot, BarkSlot);
            foreach (var start in stemStarts) Stem(branches, nodes, carried, start, 2);
            var stems = Built(branches);
            var gatheredLeaves = leaves.Subset(cardsOf);

            // The plane the leaves spread most in, facing out of the crown and upright.
            var spread = cardsOf.Select(card => middles[card]).ToArray();
            var mean = spread.Aggregate(Vec3.Zero, (sum, place) => sum + place) * (1f / spread.Length);
            var (first, second) = Spreading(spread, mean);
            var face = Vec3.Cross(first, second);
            face = face.LengthSquared > 1e-8f ? face.Normalized : Vec3.UnitZ;
            if (Vec3.Dot(face, mean - crown) < 0f) face = -face;
            var up = MathF.Abs(first.Y) >= MathF.Abs(second.Y) ? first : second;
            up = (up - face * Vec3.Dot(up, face)).Normalized;
            if (up.Y < 0f) up = -up;
            var right = Vec3.Cross(up, face).Normalized;

            var outward = cardsOf.Aggregate(Vec3.Zero, (sum, leaf) => sum + leaves.WayOut(leaf));
            outward = outward.LengthSquared > 1e-6f ? outward.Normalized : face;

            // The plane and the one across it, turned a quarter round its upright, facing out too,
            // since a branch pictured on one card alone thins to a line seen along it.
            var across = Vec3.Dot(right, mean - crown) >= 0f ? right : -right;
            foreach (var (planeRight, planeFace) in new[] { (right, face), (Vec3.Cross(up, across).Normalized, across) })
            {
                var card = Pictured(mean, planeRight, up, planeFace, stems, gatheredLeaves, planned.Count, cells);
                planned.Add(card);
                quads.Add((card, outward, limbPlan.LimbOf(keptStem)));
            }
        }

        return new CardPlan(Swaying(limbs, runs, limbPlan), planned, cells, Quads(quads, cells, limbPlan));
    }

    /// <summary>
    /// A card in a plane through a branch's middle, as large as its stems' and leaves' outline in
    /// it, pictured in the cell of the atlas its index gives, as large as the cell holds it.
    /// </summary>
    private static PlannedCard Pictured(Vec3 mean, Vec3 right, Vec3 up, Vec3 face, MeshData stems, MeshData leaves, int index, int cells)
    {
        var (left, rightmost, bottom, top) = (float.MaxValue, float.MinValue, float.MaxValue, float.MinValue);
        foreach (var place in leaves.Positions.Concat(stems.Positions))
        {
            var (x, y) = (Vec3.Dot(place - mean, right), Vec3.Dot(place - mean, up));
            (left, rightmost, bottom, top) = (MathF.Min(left, x), MathF.Max(rightmost, x), MathF.Min(bottom, y), MathF.Max(top, y));
        }

        var middle = mean + right * ((left + rightmost) * 0.5f) + up * ((bottom + top) * 0.5f);
        var (halfWidth, halfHeight) = (MathF.Max((rightmost - left) * 0.5f, 1e-3f), MathF.Max((top - bottom) * 0.5f, 1e-3f));
        var scale = (1f - 2f * CardMargin) / (2f * MathF.Max(halfWidth, halfHeight));
        return new PlannedCard(middle, right, up, face, halfWidth, halfHeight, stems, leaves, scale, index % cells, index / cells);
    }

    /// <summary>An edit mesh's one slot as mesh data, or none where it holds no faces.</summary>
    private static MeshData Built(EditMesh mesh) => Modeling.Build(mesh.SelectNone()) is { Length: > 0 } built ? built[0] : new MeshData();

    /// <summary>A grown tree's far level as a planted tree of its species draws it, or nothing where its leaves are fronds.</summary>
    internal static CardPlan? PlanFarLevel(TreeSkeleton skeleton, TreeSpecies species, uint seed) =>
        species.Leaves > 0 ? PlanCards(skeleton, MakeCards(skeleton, species, seed), PlanLimbs(skeleton)) : null;

    /// <summary>The middle of a leaf's card, the mean of its four corners.</summary>
    private static Vec3 CardMiddle(IReadOnlyList<Vec3> positions, int card) =>
        (positions[card * 4] + positions[card * 4 + 1] + positions[card * 4 + 2] + positions[card * 4 + 3]) * 0.25f;

    /// <summary>
    /// The two ways places spread most from their mean, the longest axis of their spread and the
    /// longest across it, found by multiplying a way by their spread until it settles, or the level
    /// ways where they spread along a line or not at all.
    /// </summary>
    private static (Vec3 First, Vec3 Second) Spreading(IReadOnlyList<Vec3> places, Vec3 mean)
    {
        var (xx, xy, xz, yy, yz, zz) = (0f, 0f, 0f, 0f, 0f, 0f);
        foreach (var place in places)
        {
            var d = place - mean;
            (xx, xy, xz, yy, yz, zz) = (xx + d.X * d.X, xy + d.X * d.Y, xz + d.X * d.Z, yy + d.Y * d.Y, yz + d.Y * d.Z, zz + d.Z * d.Z);
        }

        Vec3 Times(Vec3 v) => new(xx * v.X + xy * v.Y + xz * v.Z, xy * v.X + yy * v.Y + yz * v.Z, xz * v.X + yz * v.Y + zz * v.Z);
        Vec3 Settled(Vec3 start, Vec3 across)
        {
            var way = start;
            for (var step = 0; step < 32; step++)
            {
                way -= across * Vec3.Dot(way, across);
                var next = Times(way);
                next -= across * Vec3.Dot(next, across);
                if (next.LengthSquared < 1e-12f) return way.LengthSquared > 1e-12f ? way.Normalized : Vec3.Zero;
                way = next.Normalized;
            }

            return way;
        }

        var first = Settled(new Vec3(0.3f, 0.9f, 0.3f).Normalized, Vec3.Zero);
        if (first == Vec3.Zero) return (Vec3.UnitY, Vec3.UnitX);
        var trial = MathF.Abs(first.X) < 0.9f ? Vec3.UnitX : Vec3.UnitZ;
        var second = Settled((trial - first * Vec3.Dot(trial, first)).Normalized, first);
        if (second == Vec3.Zero) second = Vec3.Cross(first, MathF.Abs(first.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX).Normalized;
        return (first, second);
    }

    /// <summary>
    /// The cards as the far level draws them, a rectangle each in the tree's space facing out of the
    /// crown, with its picture's place in its cell of the atlas, the way out of the crown its leaves
    /// carry for the leaf material, a color of white, and the pivot of the limb whose leaves it
    /// pictures, which it swings about.
    /// </summary>
    private static MeshData Quads(IReadOnlyList<(PlannedCard Card, Vec3 Out, int Limb)> cards, int across, LimbPlan limbPlan)
    {
        var positions = new Vec3[cards.Count * 4];
        var normals = new Vec3[cards.Count * 4];
        var uvs = new float[cards.Count * 8];
        var secondUvs = new float[cards.Count * 8];
        var colors = new float[cards.Count * 16];
        var limbs = new System.Numerics.Vector4[cards.Count * 4];
        var own = new System.Numerics.Vector4[cards.Count * 4];
        var indices = new uint[cards.Count * 6];
        for (var k = 0; k < cards.Count; k++)
        {
            var (card, outward, limb) = cards[k];
            var folded = Folded(outward);
            (float X, float Y)[] corners = [(-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f)];
            for (var corner = 0; corner < 4; corner++)
            {
                var (x, y) = (corners[corner].X * card.HalfWidth, corners[corner].Y * card.HalfHeight);
                var at = k * 4 + corner;
                positions[at] = card.Middle + card.Right * x + card.Up * y;
                normals[at] = card.Face;
                (uvs[at * 2], uvs[at * 2 + 1]) = CellPlace(card, x, y, across);
                (secondUvs[at * 2], secondUvs[at * 2 + 1]) = folded;
                colors.AsSpan(at * 4, 4).Fill(1f);
                limbs[at] = limbPlan.Pivot(limb, positions[at]);
                own[at] = new System.Numerics.Vector4(positions[at].X, positions[at].Y, positions[at].Z, 0f);
            }

            var first = (uint)(k * 4);
            uint[] quad = [first, first + 1, first + 2, first, first + 2, first + 3];
            quad.CopyTo(indices, k * 6);
        }

        return new MeshData { Positions = positions, Normals = normals, Uvs = uvs, SecondUvs = secondUvs, Colors = colors, Indices = indices, Streams = Streams(limbs, own) };
    }

    /// <summary>
    /// Where a point of a card's plane, so far right and up of its middle in meters, falls in the
    /// atlas, a share of its width from the left and of its height from the top, as its picture is
    /// taken at the middle of its cell.
    /// </summary>
    private static (float U, float V) CellPlace(PlannedCard card, float right, float up, int across) =>
        ((card.Column + 0.5f + right * card.Scale) / across, (card.Row + 0.5f - up * card.Scale) / across);
}

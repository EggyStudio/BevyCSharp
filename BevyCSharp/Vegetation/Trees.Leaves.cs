using System.Numerics;

namespace Bevy;

// The leaves, a card a leaf along every twig, cut from the species' leaf set or the generated leaves,
// or a frond along each stem of the last level where the species' leaves are fronds.
public static partial class Trees
{
    // The most leaves a tree's cards hold, past which each is drawn larger to cover the same area,
    // and the most larger it is drawn.
    private const int MostLeaves = 30_000;
    private const float MostLarger = 1.6f;

    // How far a leaf stands off its twig at most, in its own lengths.
    private const float Spread = 1.5f;

    // How dark the deepest leaf of a crown is drawn, the share of its color it keeps where none of
    // the sky reaches it, gently, since the shadow map darkens what the sun does not reach.
    private const float LeafShadeLeast = 0.5f;

    /// <summary>Makes the leaves of a grown tree, a card a leaf along its twigs, or a frond along each where its species' leaves are fronds.</summary>
    /// <param name="skeleton">The tree, as <see cref="Grow(TreeSpecies, uint)"/> grew it.</param>
    /// <param name="species">Its species, whose leaves' count, size, angles and set the cards follow.</param>
    /// <param name="seed">The seed the tree grew from, which places the leaves the same way each time.</param>
    /// <returns>The cards' mesh, drawn with <see cref="LeafMaterial"/>, with no positions where the species has no leaves.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A control of the species is outside what it may be, which the message names.</exception>
    /// <remarks>
    /// <para>
    /// A twig is a stem that carries no other, from the point it leaves its parent to its tip. The
    /// paper's tree carries the species' <see cref="TreeSpecies.Leaves"/> on every stem of its last
    /// level, as many stems as its levels' branches multiplied make, and a grown tree has fewer
    /// twigs where light and room stopped its buds, so the paper's count of leaves is shared among
    /// the twigs there are, at most 30,000 a tree, each leaf larger, up to 1.6 times, where the most
    /// leaves the cards hold is fewer than the paper's, so the crown covers what the paper's covers,
    /// as its quality factor does. A twig shorter than most carries fewer, more of them toward its
    /// tip, each leaving at the leaf level's down angle and turned round the twig from the one before,
    /// standing off the twig by up to one and a half of its lengths, as a leaf on one of the twig's
    /// finer twiglets does, and turned by <see cref="TreeSpecies.LeafBend"/> to face up and away from
    /// the trunk. A card is the leaf's own picture, a box of its leaf set's atlas, its stalk at
    /// the twig, as long as <see cref="TreeSpecies.LeafScale"/> a tenth either way at random and as
    /// wide as its picture's proportions make it.
    /// </para>
    /// <para>
    /// Where the species' leaves are fronds, each stem of the last level is drawn as a strip along its
    /// whole length, as wide as the leaf scale, one picture of the atlas running from its foot to its
    /// tip, so a fern's or a palm's frond bends as its stem does.
    /// </para>
    /// </remarks>
    public static MeshData BuildLeaves(TreeSkeleton skeleton, TreeSpecies species, uint seed) => BuildLeaves(skeleton, species, seed, TreeDetail.Full);

    /// <summary>Makes the leaves of a grown tree at a level of detail.</summary>
    /// <param name="skeleton">The tree, as <see cref="Grow(TreeSpecies, uint)"/> grew it.</param>
    /// <param name="species">Its species, whose leaves' count, size, angles and set the cards follow.</param>
    /// <param name="seed">The seed the tree grew from, which places the leaves the same way each time.</param>
    /// <param name="detail">The level, every leaf, or the reduced one a tree further off is drawn in.</param>
    /// <returns>The cards' mesh, drawn with <see cref="LeafMaterial"/>, with no positions where the species has no leaves.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A control of the species is outside what it may be, or the level is not one of <see cref="TreeDetail"/>'s.</exception>
    /// <remarks>
    /// The full level is <see cref="BuildLeaves(TreeSkeleton, TreeSpecies, uint)"/>'s. The reduced
    /// one is every other card of it, at the same place, each drawn the square root of two larger
    /// about its stalk, so the crown covers as much with half the cards and the two levels show the
    /// same leaves as a tree is dithered from one to the other. Fronds are kept whole, since a frond
    /// is a fern's whole leaf.
    /// </remarks>
    public static MeshData BuildLeaves(TreeSkeleton skeleton, TreeSpecies species, uint seed, TreeDetail detail)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(species);
        Check(species);
        if (!Enum.IsDefined(detail)) throw new ArgumentOutOfRangeException(nameof(detail), detail, "A level of detail is one of TreeDetail's.");

        var cards = MakeCards(skeleton, species, seed);
        return detail == TreeDetail.Reduced && species.Leaves > 0 ? cards.Halved() : cards.Data();
    }

    /// <summary>
    /// A tree's leaves as their cards are made, shaded by the sky each sees, each knowing the twig
    /// it stands on, which the far level's cards of whole branches are drawn from.
    /// </summary>
    private static Cards MakeCards(TreeSkeleton skeleton, TreeSpecies species, uint seed)
    {
        var boxes = LeafBoxes(species, out var aspect);
        var cards = new Cards();
        if (species.Leaves == 0) return cards;

        var random = new Pcg((ulong)seed ^ 0x9E3779B97F4A7C15ul);
        var twigs = Twigs(skeleton);
        var lengths = twigs.Select(twig => Length(twig.Points)).OrderBy(length => length).ToArray();
        var typical = lengths.Length > 0 ? lengths[lengths.Length / 2] : 1f;

        // The paper's count of leaves, shared among the twigs there are, and each leaf's scale.
        var papers = (float)species.Leaves * MathF.Max(PapersTwigs(species), twigs.Count);
        var drawn = MathF.Min(papers, MostLeaves);
        var larger = Math.Clamp(MathF.Sqrt(papers / MathF.Max(drawn, 1f)), 1f, MostLarger);

        // Shared by each twig's length against most twigs', a short one taking fewer, so the leaves
        // drawn come to the count.
        var weights = twigs.Select(twig => Math.Clamp(Length(twig.Points) / typical, 0.3f, 1f)).ToArray();
        var perWeight = weights.Length > 0 ? drawn / weights.Sum() : 0f;
        var owed = 0f;

        for (var t = 0; t < twigs.Count; t++)
        {
            var twig = twigs[t].Points;
            cards.Tip = twigs[t].Tip;
            if (species.Leaves < 0)
            {
                for (var i = 0; i < -species.Leaves; i++) cards.Frond(twig, boxes[random.Below(boxes.Count)], species.LeafScale, aspect, MathF.PI * i / -species.Leaves);
                continue;
            }

            var length = Length(twig);
            owed += perWeight * weights[t];
            var count = (int)MathF.Floor(owed);
            owed -= count;
            var turn = random.Unit() * MathF.Tau;
            var level = species.LeafLevel;
            for (var k = 0; k < count; k++)
            {
                // Toward the tip, as the paper's leaves gather where the light is.
                var along = MathF.Pow((k + 0.5f + 0.4f * random.Signed()) / count, 0.75f) * length;
                var (place, way) = At(twig, along);

                turn += Radians(level.Rotate + level.RotateVariation * random.Signed());
                var round = Rotate(Square(way), way, turn);
                var down = Radians(level.DownAngle + level.DownAngleVariation * random.Signed());
                var leaf = (way * MathF.Cos(down) + round * MathF.Sin(down)).Normalized;

                // Off the twig by up to one and a half of its own lengths, as a leaf on one of the
                // twig's own twiglets, finer than the growth grows, stands off it.
                var box = boxes[random.Below(boxes.Count)];
                var size = species.LeafScale * larger * (1f + 0.1f * random.Signed());
                var stalk = place + Rotate(Square(way), way, random.Unit() * MathF.Tau) * (Spread * size * MathF.Sqrt(random.Unit()));

                // The card's face turned from facing round the twig toward up and out from the trunk.
                var outward = new Vec3(stalk.X, 0f, stalk.Z);
                var light = (Vec3.UnitY + (outward.LengthSquared > 1e-6f ? outward.Normalized * 0.5f : Vec3.Zero)).Normalized;
                var face = Vec3.Cross(leaf, Vec3.Cross(round, leaf)).Normalized;
                var wanted = light - leaf * Vec3.Dot(light, leaf);
                if (wanted.LengthSquared > 1e-6f) face = (face * (1f - species.LeafBend) + wanted.Normalized * species.LeafBend).Normalized;
                if (Vec3.Dot(face, light) < 0f) face = -face;

                cards.Card(stalk, leaf, face, box, size, size * box.Width * aspect / box.Height);
            }
        }

        if (species.Leaves > 0) cards.ShadeBySky();
        return cards;
    }

    /// <summary>
    /// How many stems the paper's tree of a species has at its last level, its trunks' count times
    /// each level's branches, those past the first at six tenths, about the share of its most the
    /// paper gives a stem that is not the longest of its level.
    /// </summary>
    private static float PapersTwigs(TreeSpecies species)
    {
        var levels = species.Levels;
        var stems = (float)(species.BaseSplits + 1);
        for (var i = 1; i < levels.Count; i++) stems *= levels[i].Branches * (i > 1 ? 0.6f : 1f);
        return stems;
    }

    /// <summary>The boxes of the species' leaf set, or of the generated atlas, and the atlas's width against its height.</summary>
    private static IReadOnlyList<(float U, float V, float Width, float Height)> LeafBoxes(TreeSpecies species, out float aspect)
    {
        if (species.LeafSet is { } name && TreeTextures.Find(name) is { Kind: TreeTextureKind.Leaves } set)
        {
            aspect = set.Size is var (width, height) ? (float)width / height : 1f;
            return set.LeafBoxes;
        }

        aspect = 1f;
        return TreeLooks.LeafBoxes;
    }

    /// <summary>The twigs, each its tip's node and its points from the point it leaves its parent to its tip, through every point between, which carries no other stem.</summary>
    private static List<(int Tip, Vec3[] Points)> Twigs(TreeSkeleton skeleton)
    {
        var nodes = skeleton.Nodes;
        var children = new int[nodes.Count];
        for (var i = 1; i < nodes.Count; i++) children[nodes[i].Parent]++;

        var twigs = new List<(int, Vec3[])>();
        for (var tip = 1; tip < nodes.Count; tip++)
        {
            if (children[tip] != 0) continue;
            var points = new List<Vec3> { nodes[tip].Place };
            var at = nodes[tip].Parent;
            while (true)
            {
                points.Add(nodes[at].Place);
                if (at == 0 || children[at] != 1) break;
                at = nodes[at].Parent;
            }

            points.Reverse();
            twigs.Add((tip, [.. points]));
        }

        return twigs;
    }

    private static float Length(Vec3[] twig)
    {
        var length = 0f;
        for (var i = 1; i < twig.Length; i++) length += (twig[i] - twig[i - 1]).Length;
        return length;
    }

    /// <summary>The place a distance along a twig and the way it runs there.</summary>
    private static (Vec3 Place, Vec3 Way) At(Vec3[] twig, float along)
    {
        for (var i = 1; i < twig.Length; i++)
        {
            var step = twig[i] - twig[i - 1];
            var length = step.Length;
            if (along <= length || i == twig.Length - 1)
            {
                var t = length > 0f ? Math.Clamp(along / length, 0f, 1f) : 0f;
                return (twig[i - 1] + step * t, step.Normalized);
            }

            along -= length;
        }

        return (twig[0], Vec3.UnitY);
    }

    private static Vec3 Square(Vec3 way) => Vec3.Cross(way, MathF.Abs(way.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX).Normalized;

    private static Vec3 Rotate(Vec3 vector, Vec3 axis, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return vector * cos + Vec3.Cross(axis, vector) * sin + axis * (Vec3.Dot(axis, vector) * (1f - cos));
    }

    private static float Radians(float degrees) => degrees * (MathF.PI / 180f);

    /// <summary>
    /// A way folded onto the unit square octahedrally, its upper half the diamond in the middle and
    /// its lower half the four corners, as the leaf material unfolds it.
    /// </summary>
    private static (float U, float V) Folded(Vec3 way)
    {
        var sum = MathF.Abs(way.X) + MathF.Abs(way.Y) + MathF.Abs(way.Z);
        var (x, y, z) = (way.X / sum, way.Y / sum, way.Z / sum);
        if (y < 0f) (x, z) = ((1f - MathF.Abs(z)) * (x >= 0f ? 1f : -1f), (1f - MathF.Abs(x)) * (z >= 0f ? 1f : -1f));
        return (x * 0.5f + 0.5f, z * 0.5f + 0.5f);
    }

    /// <summary>The cards' vertices and triangles as they are made.</summary>
    private sealed class Cards
    {
        private readonly List<Vec3> _positions = [];
        private readonly List<Vec3> _normals = [];
        private readonly List<float> _uvs = [];
        private readonly List<uint> _indices = [];
        private float[]? _colors;
        private float[]? _secondUvs;
        private readonly List<int> _tips = [];

        // Each vertex's leaf, the place it flutters about and its twig's tip, and once the limbs
        // are known its two sway streams.
        private readonly List<int> _leafOf = [];
        private readonly List<Vec3> _held = [];
        private readonly List<int> _tipOf = [];
        private Vector4[]? _limbs;
        private Vector4[]? _own;
        private int _leaf;
        private Vec3 _holding;

        /// <summary>The node of the tip of the twig the cards made next stand on.</summary>
        public int Tip { get; set; }

        /// <summary>The node of the tip of the twig each card stands on, a card a strip for fronds.</summary>
        public IReadOnlyList<int> Tips => _tips;

        /// <summary>The cards' vertices, four a card in the order <see cref="Card"/> makes them.</summary>
        public IReadOnlyList<Vec3> Positions => _positions;

        /// <summary>A leaf's card, its stalk at a place, running along a way and facing a way, as long and as wide as given.</summary>
        public void Card(Vec3 place, Vec3 leaf, Vec3 face, (float U, float V, float Width, float Height) box, float length, float width)
        {
            // Wound counter-clockwise seen from the face, as Bevy's front faces are.
            var side = Vec3.Cross(leaf, face).Normalized * (width * 0.5f);
            var first = (uint)_positions.Count;
            (_leaf, _holding) = (_tips.Count, place);
            Vertex(place - side, face, box.U, box.V + box.Height);
            Vertex(place + side, face, box.U + box.Width, box.V + box.Height);
            Vertex(place + side + leaf * length, face, box.U + box.Width, box.V);
            Vertex(place - side + leaf * length, face, box.U, box.V);
            _indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
            _tips.Add(Tip);
        }

        /// <summary>
        /// A frond along a stem, a strip as wide as given from its foot to its tip, its picture's
        /// length along the stem, its long side turned along the strip where it lies across the atlas.
        /// </summary>
        public void Frond(Vec3[] stem, (float U, float V, float Width, float Height) box, float width, float aspect, float twist)
        {
            var length = Length(stem);
            if (length <= 0f) return;
            var lying = box.Width * aspect > box.Height;
            var first = (uint)_positions.Count;
            (_leaf, _holding) = (_leafOf.Count > 0 ? _leafOf[^1] + 1 : 0, stem[0]);
            var along = 0f;
            for (var i = 0; i < stem.Length; i++)
            {
                if (i > 0) along += (stem[i] - stem[i - 1]).Length;
                var way = (stem[Math.Min(i + 1, stem.Length - 1)] - stem[Math.Max(i - 1, 0)]).Normalized;
                var level = Vec3.Cross(way, Vec3.UnitY);
                var side = Rotate(level.LengthSquared > 1e-6f ? level.Normalized : Square(way), way, twist) * (width * 0.5f);
                if (Vec3.Cross(side, way).Y < 0f) side = -side;
                var face = Vec3.Cross(side, way).Normalized;

                var t = along / length;
                if (lying)
                {
                    Vertex(stem[i] - side, face, box.U + t * box.Width, box.V + box.Height);
                    Vertex(stem[i] + side, face, box.U + t * box.Width, box.V);
                }
                else
                {
                    Vertex(stem[i] - side, face, box.U, box.V + box.Height * (1f - t));
                    Vertex(stem[i] + side, face, box.U + box.Width, box.V + box.Height * (1f - t));
                }

                if (i == 0) continue;
                var at = first + (uint)(2 * i);
                _indices.AddRange([at - 2, at - 1, at + 1, at - 2, at + 1, at]);
            }
        }

        private void Vertex(Vec3 place, Vec3 normal, float u, float v)
        {
            _positions.Add(place);
            _normals.Add(normal);
            _uvs.Add(u);
            _uvs.Add(v);
            _leafOf.Add(_leaf);
            _held.Add(_holding);
            _tipOf.Add(Tip);
        }

        public MeshData Data() => new()
        {
            Positions = [.. _positions],
            Normals = [.. _normals],
            Uvs = [.. _uvs],
            Colors = _colors is null ? null : [.. _colors],
            SecondUvs = _secondUvs is null ? null : [.. _secondUvs],
            Indices = [.. _indices],
            Streams = _limbs is null ? null : Streams(_limbs, _own!),
        };

        /// <summary>
        /// Gives each vertex the pivot of the limb its twig grows from and the pivot its leaf
        /// flutters about, a card's stalk or a frond's foot, each leaf with a phase of its own. A
        /// leaf on the trunk itself swings with no limb, its stalk standing for the pivot.
        /// </summary>
        public void Sway(LimbPlan plan)
        {
            _limbs = new Vector4[_positions.Count];
            _own = new Vector4[_positions.Count];
            for (var i = 0; i < _positions.Count; i++)
            {
                var held = _held[i];
                _limbs[i] = plan.Pivot(plan.LimbOf(plan.StemOf[_tipOf[i]]), held);
                _own[i] = new Vector4(held.X, held.Y, held.Z, Phase(0x5EED + _leafOf[i]));
            }
        }

        /// <summary>
        /// Shades the cards as a crown, each darkened by how little of the sky reaches it through
        /// the leaves round it, and gives each the way out of the crown its diffuse light is taken
        /// by.
        /// </summary>
        /// <remarks>
        /// A crown's leaves each lit by their own card read as a heap of flat cards, the deep ones
        /// as bright as those on the rim. Lit as a volume, a leaf deep in the crown takes less of
        /// the sky's light, as the crown's shape and not each card's way decides. The sky a leaf
        /// sees, from <see cref="CrownVolume"/>, is written as its vertices' color, which Bevy
        /// multiplies into the base color, from the whole of it for a leaf the sky reaches to
        /// <see cref="LeafShadeLeast"/> of it for one it does not, so it darkens the ambient and
        /// the indirect light the leaf takes and the light let through it. The direct sun it
        /// darkens a little too, which the shadow map darkens as well, and so the darkening is
        /// gentle, the deepest leaf at half and never black. The normal each card is lit by stays
        /// its own, turned round to face the eye from either side, and the way out of the crown,
        /// from <see cref="CrownVolume"/>, is written in the second texture coordinates folded onto
        /// a square octahedrally, which <see cref="LeafMaterial"/> bends the normal toward for the
        /// leaf's diffuse light alone.
        /// </remarks>
        public void ShadeBySky()
        {
            var cards = _positions.Count / 4;
            var places = new Vec3[cards];
            var areas = new float[cards];
            for (var card = 0; card < cards; card++)
            {
                var corner = _positions[card * 4];
                places[card] = (corner + _positions[card * 4 + 1] + _positions[card * 4 + 2] + _positions[card * 4 + 3]) * 0.25f;
                areas[card] = Vec3.Cross(_positions[card * 4 + 1] - corner, _positions[card * 4 + 3] - corner).Length;
            }

            var (sky, outward) = CrownVolume(places, areas);
            _colors = new float[_positions.Count * 4];
            _secondUvs = new float[_positions.Count * 2];
            for (var card = 0; card < cards; card++)
            {
                var shade = LeafShadeLeast + (1f - LeafShadeLeast) * sky[card];
                for (var corner = card * 4; corner < card * 4 + 4; corner++)
                {
                    _colors[corner * 4] = shade;
                    _colors[corner * 4 + 1] = shade;
                    _colors[corner * 4 + 2] = shade;
                    _colors[corner * 4 + 3] = 1f;
                    (_secondUvs[corner * 2], _secondUvs[corner * 2 + 1]) = Folded(outward[card]);
                }
            }
        }

        /// <summary>The given cards as they are, with their colors and their ways out.</summary>
        public MeshData Subset(IReadOnlyList<int> cards)
        {
            var positions = new Vec3[cards.Count * 4];
            var normals = new Vec3[cards.Count * 4];
            var uvs = new float[cards.Count * 8];
            var colors = _colors is null ? null : new float[cards.Count * 16];
            var indices = new uint[cards.Count * 6];
            for (var k = 0; k < cards.Count; k++)
            {
                var from = cards[k] * 4;
                for (var corner = 0; corner < 4; corner++)
                {
                    positions[k * 4 + corner] = _positions[from + corner];
                    normals[k * 4 + corner] = _normals[from + corner];
                    uvs[(k * 4 + corner) * 2] = _uvs[(from + corner) * 2];
                    uvs[(k * 4 + corner) * 2 + 1] = _uvs[(from + corner) * 2 + 1];
                    if (colors is not null) _colors.AsSpan((from + corner) * 4, 4).CopyTo(colors.AsSpan((k * 4 + corner) * 4, 4));
                }

                var first = (uint)(k * 4);
                uint[] quad = [first, first + 1, first + 2, first, first + 2, first + 3];
                quad.CopyTo(indices, k * 6);
            }

            return new MeshData { Positions = positions, Normals = normals, Uvs = uvs, Colors = colors, Indices = indices };
        }

        /// <summary>The way out of the crown each card carries, unfolded as the leaf material unfolds it.</summary>
        public Vec3 WayOut(int card)
        {
            if (_secondUvs is null) return Vec3.UnitY;
            var (x, z) = (_secondUvs[card * 8] * 2f - 1f, _secondUvs[card * 8 + 1] * 2f - 1f);
            var y = 1f - MathF.Abs(x) - MathF.Abs(z);
            var t = MathF.Max(-y, 0f);
            x += x >= 0f ? -t : t;
            z += z >= 0f ? -t : t;
            return new Vec3(x, y, z).Normalized;
        }

        /// <summary>
        /// Every other card, each the square root of two larger about its stalk, the middle of its
        /// first two corners, so half the cards cover what all of them do.
        /// </summary>
        public MeshData Halved()
        {
            var kept = _positions.Count / 8;
            var positions = new Vec3[kept * 4];
            var normals = new Vec3[kept * 4];
            var uvs = new float[kept * 8];
            var colors = _colors is null ? null : new float[kept * 16];
            var secondUvs = _secondUvs is null ? null : new float[kept * 8];
            var limbs = _limbs is null ? null : new Vector4[kept * 4];
            var own = _own is null ? null : new Vector4[kept * 4];
            var indices = new uint[kept * 6];
            for (var card = 0; card < kept; card++)
            {
                var from = card * 8;
                var stalk = (_positions[from] + _positions[from + 1]) * 0.5f;
                for (var corner = 0; corner < 4; corner++)
                {
                    positions[card * 4 + corner] = stalk + (_positions[from + corner] - stalk) * MathF.Sqrt(2f);
                    normals[card * 4 + corner] = _normals[from + corner];
                    uvs[card * 8 + corner * 2] = _uvs[(from + corner) * 2];
                    uvs[card * 8 + corner * 2 + 1] = _uvs[(from + corner) * 2 + 1];
                    if (colors is not null) _colors.AsSpan((from + corner) * 4, 4).CopyTo(colors.AsSpan((card * 4 + corner) * 4, 4));
                    if (secondUvs is not null) _secondUvs.AsSpan((from + corner) * 2, 2).CopyTo(secondUvs.AsSpan((card * 4 + corner) * 2, 2));
                    if (limbs is not null) (limbs[card * 4 + corner], own![card * 4 + corner]) = (_limbs![from + corner], _own![from + corner]);
                }

                var first = (uint)(card * 4);
                uint[] quad = [first, first + 1, first + 2, first, first + 2, first + 3];
                quad.CopyTo(indices, card * 6);
            }

            return new MeshData { Positions = positions, Normals = normals, Uvs = uvs, SecondUvs = secondUvs, Colors = colors, Indices = indices, Streams = limbs is null ? null : Streams(limbs, own!) };
        }
    }
}

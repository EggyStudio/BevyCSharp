namespace Bevy;

// The growth of one tree from a species and a seed, space colonization after Runions, Lane and
// Prusinkiewicz (2007) with buds and light as Palubicki and others (2009) give them. The paper's
// controls of Weber and Penn are the buds' own tendencies, where a bud is set and which way it
// points, how a stem bends as it lengthens, when its tip forks and how long it may grow, so a stem
// carries itself to the length the paper gives it as long as light reaches its tip. Attractors are
// scattered through the crown's outline, each bud perceives those in a cone ahead of it, and each
// attractor belongs to the nearest bud that perceives it; a side bud breaks only where it perceives
// one, so a new stem starts only where there is room, and every shoot leans toward the attractors
// it perceives. Each new point shades the space under it in a grid of shadow, so a bud under the
// crown gets too little light to break and the crown's inside stays open, and the tree leans away
// from what shades it, its own crown, a wall or a neighbor.
//
// Everything is done in a fixed order, the points, the stems, the buds and the attractors in the
// order they were made, the grids only looked up and never walked, and the numbers from the
// growth's own generator, so a seed grows the same points on every run.
internal sealed partial class TreeGrowth
{
    // A tree's size in internodes, the larger of its trunk and its first branches, so a tree of any
    // size has as many points and a fern is grown as finely as an oak.
    private const float Internodes = 40f;

    // How far a bud perceives an attractor and how near a new point takes one, in internodes, near
    // Palubicki's four to six and two.
    private const float Perception = 5f;
    private const float Occupancy = 2f;

    // The cosine of the half angle of a bud's cone of perception, sixty degrees, so a side bud
    // perceives the room out from its stem and not the room along it, which the stem's own tip
    // takes.
    private const float Cone = 0.5f;

    // The most points a tree has, past which it stops growing, so a species set past what it can
    // afford still ends.
    private const int MostPoints = 40_000;

    // The most cycles, each an internode on every bud that grows, well past the internodes of a
    // trunk and the branches on it end to end.
    private const int MostCycles = 600;

    // How strongly the attractors turn a shoot from its bud's own way, and the light to either side
    // it leans toward, each an internode, both under the turn a stem's curve gives it, so the
    // paper's bends lead and the room and the light steer them.
    private const float Pull = 0.1f;
    private const float Phototropism = 0.05f;

    // How many cycles a side bud waits for room and light before it is lost, so the buds a crown
    // shades for good are not perceived for nothing every cycle after.
    private const int Dormancy = 10;

    // The most side buds a point sets, so a level with hundreds of branches a stem, as the paper's
    // willow twigs, sets them along the stem rather than in heaps at each point.
    private const float MostBuds = 4f;

    // The light, from one, open sky, down, under which a stem's tip stops and a side bud stays a
    // bud. A bud of the first level of branches breaks as it is set, so the trunk carries the
    // branches the paper gives it, and one further out asks for light and room, as a shaded bud on
    // a real tree stays dormant where the tip beside it grows on.
    private const float TipLight = 0.1f;
    private const float SideLight = 0.2f;

    private readonly TreeSpecies _species;
    private readonly IReadOnlyList<TreeLevel> _levels;
    private readonly IReadOnlyList<(Vec3 Low, Vec3 High)> _blockers;
    private Pcg _random;

    // The paper's scale of the tree, its trunk's length, the bare part of the trunk below the crown,
    // and the tree's size, from which the internode is cut.
    private readonly float _scale;
    private readonly float _height;
    private readonly float _bare;
    private readonly float _step;

    private readonly List<Vec3> _places = [];
    private readonly List<int> _parents = [];
    private readonly List<Stem> _stemOf = [];
    private readonly List<Bud> _buds = [];

    /// <summary>A stem, the trunk, a branch or a twig, as it grows from its first point to its tip.</summary>
    private sealed class Stem(int level, float length, float most)
    {
        public readonly int Level = level;

        /// <summary>The most it grows, in meters.</summary>
        public readonly float Length = length;

        /// <summary>The most a stem of its level grows from where it leaves its parent, before the shape and the place along its parent shorten it.</summary>
        public readonly float Most = most;

        /// <summary>How far it has grown.</summary>
        public float Grown;

        /// <summary>
        /// A way square to it, carried along it as it bends, from which the turn of its buds round it
        /// is counted and toward which the trunk bends.
        /// </summary>
        public Vec3 Side;

        /// <summary>The angle round it, in radians from <see cref="Side"/>, at which its next bud is set.</summary>
        public float Turn;

        /// <summary>The share of a bud due and not yet set.</summary>
        public float Due;

        /// <summary>Which side the next bud of an alternating stem goes to.</summary>
        public bool Alternate;

        /// <summary>The share of its level's splits it forks with, halved each time it or its other half forks, as the paper's clones are.</summary>
        public float Splits = 1f;

        /// <summary>Whether the trunk's base splits are behind it.</summary>
        public bool Based;

        /// <summary>
        /// The axis and the angle an internode a stem turns back about after a fork, toward the way it
        /// grew before, until the turn the fork gave it is undone at its tip, as the paper's clones
        /// turn back so a tree that forks often does not spread too far.
        /// </summary>
        public Vec3 BackAxis;
        public float BackAngle;

        /// <summary>Its first point, whose thickness its taper starts from.</summary>
        public int First = -1;

        /// <summary>A stem of the same level and length, as far grown, for the other half of a fork.</summary>
        public Stem Fork()
        {
            Splits *= 0.5f;
            return new Stem(Level, Length, Most) { Grown = Grown, Side = Side, Turn = Turn, Due = Due, Alternate = Alternate, Splits = Splits, Based = Based, BackAxis = BackAxis, BackAngle = BackAngle };
        }
    }

    /// <summary>A bud, a stem's tip or a place on a stem where a stem of the next level may start.</summary>
    private sealed class Bud
    {
        /// <summary>The point it is on.</summary>
        public int Node;

        /// <summary>The stem it lengthens, nothing until a side bud first grows and starts its own.</summary>
        public Stem? Stem;

        /// <summary>The way it points, and so grows when nothing pulls it.</summary>
        public Vec3 Way;

        /// <summary>The level, the most length and the level's own most of the stem a side bud starts.</summary>
        public int Level;
        public float Length;
        public float Most;

        /// <summary>The ways to the attractors it perceived this cycle, summed, and how many.</summary>
        public Vec3 Toward;
        public int Perceived;

        /// <summary>How many cycles a side bud has waited.</summary>
        public int Waited;

        public bool Dead;
    }

    public TreeGrowth(TreeSpecies species, uint seed, IReadOnlyList<(Vec3 Low, Vec3 High)> blockers)
    {
        _species = species;
        _levels = species.Levels;
        _blockers = blockers;
        _random = new Pcg(seed);

        // The paper's scale varies either way at random, kept to a tenth of the scale at least,
        // since a species may vary by as much as its scale, as the paper's oak does.
        var trunk = _levels[0];
        _scale = MathF.Max(species.Scale + species.ScaleVariation * _random.Signed(), 0.1f * species.Scale);
        _height = _scale * (trunk.Length + trunk.LengthVariation * _random.Signed());
        _bare = MathF.Min(species.BaseSize * _scale, 0.95f * _height);
        var size = _levels.Count > 1 ? MathF.Max(_height, _levels[1].Length * _height) : _height;
        _step = size / Internodes;

        Outline();
    }

    /// <summary>Grows the tree and gives its skeleton.</summary>
    public TreeSkeleton Grow()
    {
        var trunk = new Stem(0, _height, _height);
        var heading = _random.Unit() * MathF.Tau;
        trunk.Side = new Vec3(MathF.Cos(heading), 0f, MathF.Sin(heading));
        trunk.Due = _random.Unit();
        AddPoint(-1, Vec3.Zero, trunk);
        _buds.Add(new Bud { Node = 0, Stem = trunk, Way = Vec3.UnitY });

        Scatter();

        for (var cycle = 0; cycle < MostCycles && _places.Count < MostPoints; cycle++)
        {
            Perceive();

            // A stem's tip grows while light reaches it, and the trunk's grows regardless, as the
            // paper's trunk reaches its full height; a side bud of the first level breaks as it is
            // set, and one further out only with room and light.
            var grew = false;
            var count = _buds.Count;
            for (var i = 0; i < count; i++)
            {
                var bud = _buds[i];
                if (bud.Dead) continue;

                var light = Light(_places[bud.Node]);
                var grows = bud.Stem is { } stem ? stem.Level == 0 || light >= TipLight
                    : bud.Level == 1 || (bud.Perceived > 0 && light >= SideLight);
                if (grows) grew |= Lengthen(bud);
                else if (bud.Stem is null && ++bud.Waited > Dormancy) bud.Dead = true;
            }

            _buds.RemoveAll(bud => bud.Dead);
            if (!grew) break;
        }

        return new TreeSkeleton(Radii());
    }

    /// <summary>Grows a bud by an internode, forking where its stem's splits call for it, and says whether it grew.</summary>
    private bool Lengthen(Bud bud)
    {
        var stem = bud.Stem ??= Start(bud);
        if (stem.Grown + _step > stem.Length)
        {
            bud.Dead = true;
            return false;
        }

        var level = _levels[stem.Level];
        var steps = MathF.Max(stem.Length / _step, 1f);
        var from = _places[bud.Node];
        var way = Bend(bud.Way, stem.Level, stem.Side, stem.Grown, stem.Length);
        if (stem.BackAngle != 0f) way = Rotate(way, stem.BackAxis, stem.BackAngle);

        // The random bend, its sum over the stem's internodes spread about as far as the paper's
        // over a stem of three to five segments; the paper's helix below zero is taken as its size.
        if (level.CurveVariation != 0f) way = Rotate(way, Across(way), Radians(MathF.Abs(level.CurveVariation)) * 0.5f * _random.Signed() / MathF.Sqrt(steps));

        var away = AwayFromShadow(from);
        if (away != Vec3.Zero) way = (way + away * Phototropism).Normalized;
        if (bud.Toward.LengthSquared > 1e-8f) way = (way + bud.Toward.Normalized * Pull).Normalized;

        if (stem.Level == 0 && !stem.Based && _species.BaseSplits > 0 && stem.Grown >= _bare)
        {
            // The trunk splits at the crown's foot into stems spread evenly round it, each leaning
            // out by the trunk's split angle.
            stem.Based = true;
            var count = _species.BaseSplits + 1;
            var lean = Radians(level.SplitAngle + level.SplitAngleVariation * _random.Signed());
            var start = _random.Unit() * MathF.Tau;
            var square = Across(way);
            for (var k = 0; k < count; k++)
            {
                var axis = Rotate(square, way, start + k * MathF.Tau / count);
                var target = k == 0 ? stem : stem.Fork();
                TurnBack(target, axis, lean, steps);
                if (k == 0) continue;

                var other = new Bud { Node = bud.Node, Stem = target, Way = Rotate(way, axis, lean) };
                Grow(other, other.Way);
                if (!other.Dead) _buds.Add(other);
            }

            way = Rotate(way, Rotate(square, way, start), lean);
        }
        else if (level.Splits > 0f && _random.Unit() < level.Splits * stem.Splits / steps)
        {
            var across = Across(way);
            var half = Radians(level.SplitAngle + level.SplitAngleVariation * _random.Signed()) * 0.5f;
            var other = new Bud { Node = bud.Node, Stem = stem.Fork(), Way = Rotate(way, across, -half) };
            TurnBack(other.Stem, across, -half, steps);
            TurnBack(stem, across, half, steps);
            way = Rotate(way, across, half);
            Grow(other, other.Way);
            if (!other.Dead) _buds.Add(other);
        }

        Grow(bud, way);
        return true;
    }

    /// <summary>Sets a stem to turn back, over the internodes it has left, by the turn a fork gave it about an axis.</summary>
    private void TurnBack(Stem stem, Vec3 axis, float angle, float steps)
    {
        var left = MathF.Max(steps - stem.Grown / _step, 1f);
        stem.BackAxis = axis;
        stem.BackAngle = -angle / left;
    }

    /// <summary>
    /// A stem's way bent by its level's curve, or the curve back over its second half, and from
    /// the second level of branches on, the species' attraction up, with no randomness.
    /// </summary>
    private Vec3 Bend(Vec3 way, int level, Vec3 side, float grown, float length)
    {
        var stem = _levels[level];
        var steps = MathF.Max(length / _step, 1f);
        var curve = stem.CurveBack == 0f ? stem.Curve / steps : (grown < 0.5f * length ? stem.Curve : stem.CurveBack) / (0.5f * steps);

        // A branch bends about the level line square to it, downward where its curve is above
        // zero, and the trunk, or a branch standing upright, toward its side.
        var levelLine = Vec3.Cross(way, Vec3.UnitY);
        if (level == 0 || levelLine.LengthSquared < 1e-6f) return Rotate(way, Vec3.Cross(way, side).Normalized, Radians(curve));

        var flat = levelLine.Normalized;
        way = Rotate(way, flat, -Radians(curve));

        // The paper's attraction up, a turn toward the vertical by the stem's declination over its
        // length, so at one its last internode stands upright, and never past upright or straight down.
        if (level >= 2 && _species.AttractionUp != 0f)
        {
            var declination = MathF.Acos(Math.Clamp(way.Y, -1f, 1f));
            var up = _species.AttractionUp * declination / steps;
            way = Rotate(way, flat, up > 0f ? MathF.Min(up, declination) : MathF.Max(up, declination - MathF.PI));
        }

        return way;
    }

    /// <summary>Adds the internode a bud grows along a way, its point and the side buds the stem sets there.</summary>
    private void Grow(Bud bud, Vec3 way)
    {
        var stem = bud.Stem!;
        var place = _places[bud.Node] + way * _step;
        if (place.Y < 0f || Blocked(place) || ((stem.Level > 0 || place.Y >= _bare) && Pruned(place)))
        {
            bud.Dead = true;
            return;
        }

        bud.Node = AddPoint(bud.Node, place, stem);
        bud.Way = way;
        Take(place);
        Darken(place);
        SetBuds(stem, bud.Node, way);
    }

    /// <summary>Starts the stem a side bud grows, as the level it was set for has it.</summary>
    private Stem Start(Bud bud) => new(bud.Level, bud.Length, bud.Most)
    {
        Side = Across(bud.Way),
        Turn = _random.Unit() * MathF.Tau,
        Due = _random.Unit(),
    };

    /// <summary>Sets the side buds a stem's new point carries, as many as its next level's branches call for along its length.</summary>
    private void SetBuds(Stem stem, int node, Vec3 way)
    {
        if (stem.Level + 1 >= _levels.Count) return;
        var next = _levels[stem.Level + 1];
        if (next.Branches <= 0) return;

        var bare = stem.Level == 0 ? _bare : 0f;
        if (stem.Grown < bare) return;

        // The trunk carries its branches' count, and a branch fewer the shorter it is than its
        // level's most, as the paper's do.
        var count = stem.Level == 0 ? next.Branches : next.Branches * (0.2f + 0.8f * stem.Length / stem.Most);
        stem.Due = MathF.Min(stem.Due + _step * count / (stem.Length - bare), MostBuds);
        while (stem.Due >= 1f)
        {
            stem.Due -= 1f;
            SetBud(stem, node, way, next, bare);
        }
    }

    /// <summary>Sets one side bud, turned round the stem from the last and leaning out by the next level's down angle.</summary>
    private void SetBud(Stem stem, int node, Vec3 way, TreeLevel next, float bare)
    {
        var side = stem.Side - way * Vec3.Dot(stem.Side, way);
        stem.Side = side.LengthSquared > 1e-8f ? side.Normalized : Across(way);
        var round = Rotate(stem.Side, way, stem.Turn);

        if (next.Rotate >= 0f)
        {
            stem.Turn += Radians(next.Rotate + next.RotateVariation * _random.Signed());
        }
        else
        {
            stem.Turn += Radians(180f + (stem.Alternate ? next.Rotate : -next.Rotate) + next.RotateVariation * _random.Signed());
            stem.Alternate = !stem.Alternate;
        }

        // How far down the stem's length the bud is, one at its foot or the crown's and none at its tip.
        var ratio = Math.Clamp((stem.Length - stem.Grown) / (stem.Length - bare), 0f, 1f);
        var lean = Radians(DownAngle(next, ratio, _random.Signed()));

        var most = next.Length + next.LengthVariation * _random.Signed();
        var length = stem.Level == 0 ? stem.Length * most * ShapeAt(ratio) : most * (stem.Length - 0.6f * stem.Grown);
        if (length < _step) return;

        _buds.Add(new Bud
        {
            Node = node,
            Way = (way * MathF.Cos(lean) + round * MathF.Sin(lean)).Normalized,
            Level = stem.Level + 1,
            Length = length,
            Most = MathF.Max(most * stem.Length, length),
        });
    }

    /// <summary>
    /// A level's down angle in degrees, varied at random by its variation, or below zero along the
    /// stem it leaves by the ratio, one at the stem's foot and none at its tip, as the paper's is.
    /// </summary>
    private static float DownAngle(TreeLevel level, float ratio, float random) => level.DownAngleVariation >= 0f
        ? level.DownAngle + level.DownAngleVariation * random
        : level.DownAngle + level.DownAngleVariation * (1f - 2f * ShapeRatio(TreeShape.Conical, ratio));

    private int AddPoint(int parent, Vec3 place, Stem stem)
    {
        _places.Add(place);
        _parents.Add(parent);
        _stemOf.Add(stem);
        var index = _places.Count - 1;
        if (stem.First < 0) stem.First = index;
        if (parent >= 0) stem.Grown += _step;
        return index;
    }

    /// <summary>
    /// The points with their radii by the pipe model, each point's cross section that of the
    /// points it carries with one more of its own, scaled so the trunk's foot is as thick as the
    /// species' ratio makes it, eased toward each stem's foot by its level's taper, and the trunk
    /// widened over its lowest eighth by the flare, as the paper widens it.
    /// </summary>
    private TreeNode[] Radii()
    {
        var count = _places.Count;
        var area = new float[count];
        Array.Fill(area, 1f);
        for (var i = count - 1; i > 0; i--) area[_parents[i]] += area[i];

        var unit = _species.Ratio * _height / MathF.Sqrt(area[0]);
        var nodes = new TreeNode[count];
        for (var i = 0; i < count; i++)
        {
            var stem = _stemOf[i];
            var pipe = unit * MathF.Sqrt(area[i]);
            var foot = unit * MathF.Sqrt(area[stem.First]);
            var radius = foot + (pipe - foot) * MathF.Min(_levels[stem.Level].Taper, 1f);

            var low = 1f - 8f * _places[i].Y / _height;
            if (stem.Level == 0 && low > 0f) radius *= _species.Flare * (MathF.Pow(100f, low) - 1f) / 100f + 1f;
            nodes[i] = new TreeNode(_parents[i], _places[i], radius);
        }

        return nodes;
    }

    /// <summary>
    /// The paper's shape ratio, the share of the longest first branch a branch leaving the trunk at a
    /// ratio has, from one at the crown's foot to none at its top.
    /// </summary>
    internal static float ShapeRatio(TreeShape shape, float ratio) => shape switch
    {
        TreeShape.Conical => 0.2f + 0.8f * ratio,
        TreeShape.Spherical => 0.2f + 0.8f * MathF.Sin(MathF.PI * ratio),
        TreeShape.Hemispherical => 0.2f + 0.8f * MathF.Sin(0.5f * MathF.PI * ratio),
        TreeShape.Cylindrical => 1f,
        TreeShape.TaperedCylindrical => 0.5f + 0.5f * ratio,
        TreeShape.Flame => ratio <= 0.7f ? ratio / 0.7f : (1f - ratio) / 0.3f,
        TreeShape.InverseConical => 1f - 0.8f * ratio,
        _ => ratio <= 0.7f ? 0.5f + 0.5f * ratio / 0.7f : 0.5f + 0.5f * (1f - ratio) / 0.3f,
    };

    /// <summary>The species' shape ratio at a ratio, its pruning envelope's where its shape is <see cref="TreeShape.Envelope"/>.</summary>
    private float ShapeAt(float ratio) => _species.Shape == TreeShape.Envelope ? PruneEnvelope(ratio) : ShapeRatio(_species.Shape, ratio);

    /// <summary>
    /// The paper's pruning envelope at a ratio, rising from the crown's foot to its widest at the
    /// peak and falling to the top, each side curved by its power, and nothing outside the crown.
    /// </summary>
    private float PruneEnvelope(float ratio)
    {
        if (ratio is < 0f or > 1f) return 0f;
        var peak = _species.PruneWidthPeak;
        return ratio < 1f - peak
            ? MathF.Pow(ratio / (1f - peak), _species.PrunePowerHigh)
            : MathF.Pow((1f - ratio) / peak, _species.PrunePowerLow);
    }

    /// <summary>Whether a branch's new point falls outside the pruning envelope, by the species' prune ratio as a chance.</summary>
    private bool Pruned(Vec3 place)
    {
        if (_species.PruneRatio <= 0f) return false;
        var ratio = (_scale - place.Y) / (_scale * (1f - _species.BaseSize));
        var inside = MathF.Sqrt(place.X * place.X + place.Z * place.Z) / _scale < _species.PruneWidth * PruneEnvelope(ratio);
        return !inside && _random.Unit() < _species.PruneRatio;
    }

    /// <summary>Whether a place is inside one of the boxes the tree grows round.</summary>
    private bool Blocked(Vec3 place)
    {
        foreach (var (low, high) in _blockers)
        {
            if (place.X >= low.X && place.X <= high.X && place.Y >= low.Y && place.Y <= high.Y && place.Z >= low.Z && place.Z <= high.Z) return true;
        }

        return false;
    }

    /// <summary>A way square to another, turned round it at random.</summary>
    private Vec3 Across(Vec3 way)
    {
        var square = Vec3.Cross(way, MathF.Abs(way.Y) < 0.9f ? Vec3.UnitY : Vec3.UnitX).Normalized;
        return Rotate(square, way, _random.Unit() * MathF.Tau);
    }

    /// <summary>A vector turned about a unit axis by an angle in radians, by Rodrigues' formula.</summary>
    private static Vec3 Rotate(Vec3 vector, Vec3 axis, float angle)
    {
        var (sin, cos) = MathF.SinCos(angle);
        return vector * cos + Vec3.Cross(axis, vector) * sin + axis * (Vec3.Dot(axis, vector) * (1f - cos));
    }

    private static float Radians(float degrees) => degrees * (MathF.PI / 180f);
}

namespace Bevy;

// The growth of one tree from a species and a seed, space colonization after Runions, Lane and
// Prusinkiewicz (2007) with buds as Palubicki and others (2009) give it. Attractors are scattered
// through the crown's envelope, each bud perceives those in a cone ahead of it, each attractor
// belongs to the nearest bud that perceives it, and a bud with any grows one internode toward
// them, which takes every attractor near its new point. The paper's controls of Weber and Penn are
// the buds' own tendencies, where a bud is set and which way it points, how a stem bends as it
// lengthens, when its tip forks and how long it may grow. The attractors decide which buds grow
// and how far, so a crown fills its envelope from the outside in, and a bud inside a full crown
// finds nothing and stays a bud.
//
// Everything is done in a fixed order, the points, the stems, the buds and the attractors in the
// order they were made, the grids only looked up and never walked, and the numbers from the
// growth's own generator, so a seed grows the same points on every run.
internal sealed class TreeGrowth
{
    // A tree's height in internodes, so a tree of any size has as many points and a bush is grown
    // as finely as an oak.
    private const float Internodes = 40f;

    // How far a bud perceives an attractor and how near a new point takes one, in internodes, near
    // Palubicki's four to six and two.
    private const float Perception = 5f;
    private const float Occupancy = 2f;

    // The cosine of the half angle of a bud's cone of perception, sixty degrees, so a side bud
    // perceives the room out from its stem and not the room along it, which the stem's own tip
    // takes.
    private const float Cone = 0.5f;

    // How many attractors a cubic internode of the crown holds, and the most a tree has, so a large
    // crown costs no more than the points it can grow.
    private const float Density = 0.5f;
    private const int MostAttractors = 30_000;

    // The most cycles, each an internode on every bud that grows, well past the trunk's
    // internodes, which grow one a cycle.
    private const int MostCycles = 400;

    // How strongly the attractors turn a shoot from its bud's own way, and how strongly a unit of
    // the species' attraction up turns it each internode.
    private const float Pull = 0.3f;
    private const float Lift = 0.1f;

    private readonly TreeSpecies _species;
    private readonly IReadOnlyList<TreeLevel> _levels;
    private Pcg _random;
    private readonly float _height;
    private readonly float _step;
    private readonly float _reach;

    private readonly List<Vec3> _places = [];
    private readonly List<int> _parents = [];
    private readonly List<Stem> _stemOf = [];
    private readonly List<Bud> _buds = [];
    private Vec3[] _attractors = [];
    private bool[] _taken = [];
    private readonly Grid _attractorCells;
    private readonly Grid _budCells;
    private readonly List<List<int>> _around = [];

    /// <summary>A stem, the trunk, a branch or a twig, as it grows from its first point to its tip.</summary>
    private sealed class Stem(int level, float length)
    {
        public readonly int Level = level;

        /// <summary>The most it grows, in meters.</summary>
        public readonly float Length = length;

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

        /// <summary>Its first point, whose thickness its taper starts from.</summary>
        public int First = -1;

        /// <summary>A stem of the same level and length, as far grown, for the other half of a fork.</summary>
        public Stem Fork() => new(Level, Length) { Grown = Grown, Side = Side, Turn = Turn, Due = Due, Alternate = Alternate };
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

        /// <summary>The level and the most length of the stem a side bud starts.</summary>
        public int Level;
        public float Length;

        /// <summary>The ways to the attractors it perceived this cycle, summed, and how many.</summary>
        public Vec3 Toward;
        public int Perceived;

        public bool Dead;
    }

    public TreeGrowth(TreeSpecies species, uint seed)
    {
        _species = species;
        _levels = species.Levels;
        _random = new Pcg(seed);

        var trunk = _levels[0];
        _height = (species.Scale + species.ScaleVariation * _random.Signed()) * (trunk.Length + trunk.LengthVariation * _random.Signed());
        _step = _height / Internodes;
        _reach = _levels.Count > 1 ? _levels[1].Length * _height * MathF.Sin(Radians(Math.Clamp(_levels[1].DownAngle, 30f, 90f))) : 0f;

        // The grids cover the crown and a margin of two cells round it, and a stem that bends out of
        // them is filed in the cell at their edge, which still finds every attractor it perceives,
        // since the attractors are all inside.
        var size = Perception * _step;
        var margin = 2f * size;
        var low = new Vec3(-_reach - margin, -margin, -_reach - margin);
        var high = new Vec3(_reach + margin, _height + margin, _reach + margin);
        _attractorCells = new Grid(low, high, size);
        _budCells = new Grid(low, high, size);
    }

    /// <summary>Grows the tree and gives its skeleton.</summary>
    public TreeSkeleton Grow()
    {
        var trunk = new Stem(0, _height);
        var heading = _random.Unit() * MathF.Tau;
        trunk.Side = new Vec3(MathF.Cos(heading), 0f, MathF.Sin(heading));
        trunk.Due = _random.Unit();
        AddPoint(-1, Vec3.Zero, trunk);
        _buds.Add(new Bud { Node = 0, Stem = trunk, Way = Vec3.UnitY });

        if (_levels.Count > 1) Scatter();

        for (var cycle = 0; cycle < MostCycles; cycle++)
        {
            Perceive();

            // A bud grows when it perceived room, and a trunk's tip grows on regardless, as the
            // paper's trunk reaches its full height, so a crown too sparse for the tip to perceive
            // still has its trunk.
            var grew = false;
            var count = _buds.Count;
            for (var i = 0; i < count; i++)
            {
                var bud = _buds[i];
                if (bud.Dead || (bud.Perceived == 0 && bud.Stem is not { Level: 0 })) continue;
                grew |= Lengthen(bud);
            }

            _buds.RemoveAll(bud => bud.Dead);
            if (!grew) break;
        }

        return new TreeSkeleton(Radii());
    }

    /// <summary>Scatters the attractors through the crown's envelope, evenly through its volume.</summary>
    private void Scatter()
    {
        var foot = _species.BaseSize * _height;
        var tall = _height - foot;
        if (!(tall > 0f) || !(_reach > 0f)) return;

        // The crown's volume by slices, for as many attractors as its internodes hold.
        const int Slices = 64;
        var volume = 0.0;
        for (var slice = 0; slice < Slices; slice++)
        {
            var radius = _reach * Envelope(_species.Shape, 1f - (slice + 0.5f) / Slices);
            volume += Math.PI * radius * radius * tall / Slices;
        }

        var count = (int)Math.Min(MostAttractors, Math.Round(volume * Density / (_step * _step * _step)));
        _attractors = new Vec3[count];
        _taken = new bool[count];

        // A height is kept as often as the square of the crown's width there, so each slice holds
        // attractors by its area, and a place in the slice's disc by the square root of a number,
        // so the disc's rim holds as many as its middle by area.
        for (var made = 0; made < count;)
        {
            var up = _random.Unit();
            var share = Envelope(_species.Shape, 1f - up);
            if (_random.Unit() >= share * share) continue;

            var radius = _reach * share * MathF.Sqrt(_random.Unit());
            var angle = _random.Unit() * MathF.Tau;
            var place = new Vec3(radius * MathF.Cos(angle), foot + tall * up, radius * MathF.Sin(angle));

            _attractorCells.Add(place, made);
            _attractors[made++] = place;
        }
    }

    /// <summary>Gives each attractor to the nearest bud that perceives it.</summary>
    private void Perceive()
    {
        _budCells.Clear();
        for (var i = 0; i < _buds.Count; i++)
        {
            var bud = _buds[i];
            bud.Toward = Vec3.Zero;
            bud.Perceived = 0;
            _budCells.Add(_places[bud.Node], i);
        }

        var reach = Perception * _step;
        for (var a = 0; a < _attractors.Length; a++)
        {
            if (_taken[a]) continue;
            var attractor = _attractors[a];
            var nearest = -1;
            var least = reach * reach;
            var way = Vec3.Zero;

            _budCells.Around(attractor, _around);
            foreach (var held in _around)
            {
                foreach (var i in held)
                {
                    var to = attractor - _places[_buds[i].Node];
                    var squared = to.LengthSquared;
                    if (squared >= least || squared == 0f) continue;
                    if (Vec3.Dot(to, _buds[i].Way) <= Cone * MathF.Sqrt(squared)) continue;
                    nearest = i;
                    least = squared;
                    way = to;
                }
            }

            if (nearest < 0) continue;
            _buds[nearest].Toward += way.Normalized;
            _buds[nearest].Perceived++;
        }
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
        var way = bud.Way;

        // The curve, about the level line square to the stem, or toward the trunk's side.
        var curve = Radians(level.Curve) / steps;
        if (stem.Level == 0)
        {
            way = Rotate(way, Vec3.Cross(way, stem.Side).Normalized, curve);
        }
        else
        {
            var flat = Vec3.Cross(way, Vec3.UnitY);
            if (flat.LengthSquared > 1e-8f) way = Rotate(way, flat.Normalized, -curve);
            way = (way + Vec3.UnitY * (_species.AttractionUp * Lift)).Normalized;
        }

        // The random bend, its sum over the stem's internodes spread about as far as the paper's
        // over a stem of three to five segments.
        if (level.CurveVariation > 0f) way = Rotate(way, Across(way), Radians(level.CurveVariation) * 0.5f * _random.Signed() / MathF.Sqrt(steps));
        if (bud.Toward.LengthSquared > 1e-8f) way = (way + bud.Toward.Normalized * Pull).Normalized;

        if (level.Splits > 0f && level.SplitAngle > 0f && _random.Unit() < level.Splits / steps)
        {
            var across = Across(way);
            var half = Radians(level.SplitAngle) * 0.5f;
            var other = new Bud { Node = bud.Node, Stem = stem.Fork(), Way = Rotate(way, across, -half) };
            way = Rotate(way, across, half);
            Grow(other, other.Way);
            if (!other.Dead) _buds.Add(other);
        }

        Grow(bud, way);
        return true;
    }

    /// <summary>Adds the internode a bud grows along a way, its point and the side buds the stem sets there.</summary>
    private void Grow(Bud bud, Vec3 way)
    {
        var stem = bud.Stem!;
        var place = _places[bud.Node] + way * _step;
        if (place.Y < 0f)
        {
            bud.Dead = true;
            return;
        }

        bud.Node = AddPoint(bud.Node, place, stem);
        bud.Way = way;
        Take(place);
        SetBuds(stem, bud.Node, way);
    }

    /// <summary>Starts the stem a side bud grows, as the level it was set for has it.</summary>
    private Stem Start(Bud bud)
    {
        var stem = new Stem(bud.Level, bud.Length);
        stem.Side = Across(bud.Way);
        stem.Turn = _random.Unit() * MathF.Tau;
        stem.Due = _random.Unit();
        return stem;
    }

    /// <summary>Sets the side buds a stem's new point carries, as many as its next level's branches call for along its length.</summary>
    private void SetBuds(Stem stem, int node, Vec3 way)
    {
        if (stem.Level + 1 >= _levels.Count) return;
        var next = _levels[stem.Level + 1];
        if (next.Branches <= 0) return;

        var bare = stem.Level == 0 ? _species.BaseSize * stem.Length : 0f;
        if (stem.Grown < bare) return;

        stem.Due += _step * next.Branches / (stem.Length - bare);
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
        var down = next.DownAngleVariation >= 0f
            ? next.DownAngle + next.DownAngleVariation * _random.Signed()
            : next.DownAngle + next.DownAngleVariation * (1f - 2f * Envelope(TreeShape.Conical, ratio));
        var lean = Radians(down);

        var most = next.Length + next.LengthVariation * _random.Signed();
        var length = stem.Level == 0 ? stem.Length * most * Envelope(_species.Shape, ratio) : most * (stem.Length - 0.6f * stem.Grown);
        if (length < _step) return;

        _buds.Add(new Bud
        {
            Node = node,
            Way = (way * MathF.Cos(lean) + round * MathF.Sin(lean)).Normalized,
            Level = stem.Level + 1,
            Length = length,
        });
    }

    /// <summary>Marks every attractor near a new point taken, so no bud grows toward it again.</summary>
    private void Take(Vec3 place)
    {
        var near = Occupancy * _step;
        _attractorCells.Around(place, _around);
        foreach (var held in _around)
        {
            foreach (var a in held)
            {
                if (!_taken[a] && (_attractors[a] - place).LengthSquared < near * near) _taken[a] = true;
            }
        }
    }

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
    /// species' ratio makes it, then eased toward each stem's foot by its level's taper.
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
            nodes[i] = new TreeNode(_parents[i], _places[i], foot + (pipe - foot) * _levels[stem.Level].Taper);
        }

        return nodes;
    }

    /// <summary>
    /// The share of the crown's widest reach at a height for a shape, as the paper's shape ratio
    /// gives it, from a ratio of one at the crown's foot to none at its top.
    /// </summary>
    internal static float Envelope(TreeShape shape, float ratio) => shape switch
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

    /// <summary>
    /// Indices filed by place in cells as wide as a bud perceives, over a box, a place outside it
    /// filed in the nearest cell at its edge.
    /// </summary>
    private sealed class Grid
    {
        private readonly Vec3 _low;
        private readonly float _size;
        private readonly int _width, _height, _depth;
        private readonly List<int>?[] _cells;

        public Grid(Vec3 low, Vec3 high, float size)
        {
            _low = low;
            _size = size;
            _width = Math.Max(1, (int)MathF.Ceiling((high.X - low.X) / size));
            _height = Math.Max(1, (int)MathF.Ceiling((high.Y - low.Y) / size));
            _depth = Math.Max(1, (int)MathF.Ceiling((high.Z - low.Z) / size));
            _cells = new List<int>?[_width * _height * _depth];
        }

        public void Add(Vec3 place, int index)
        {
            var (x, y, z) = Of(place);
            (_cells[x + _width * (y + _height * z)] ??= []).Add(index);
        }

        public void Clear()
        {
            foreach (var cell in _cells) cell?.Clear();
        }

        /// <summary>Puts the cells round a place's, its own among them, in a list.</summary>
        public void Around(Vec3 place, List<List<int>> into)
        {
            into.Clear();
            var (x, y, z) = Of(place);
            for (var k = Math.Max(z - 1, 0); k <= Math.Min(z + 1, _depth - 1); k++)
            for (var j = Math.Max(y - 1, 0); j <= Math.Min(y + 1, _height - 1); j++)
            for (var i = Math.Max(x - 1, 0); i <= Math.Min(x + 1, _width - 1); i++)
            {
                if (_cells[i + _width * (j + _height * k)] is { Count: > 0 } cell) into.Add(cell);
            }
        }

        private (int, int, int) Of(Vec3 place) => (
            Math.Clamp((int)MathF.Floor((place.X - _low.X) / _size), 0, _width - 1),
            Math.Clamp((int)MathF.Floor((place.Y - _low.Y) / _size), 0, _height - 1),
            Math.Clamp((int)MathF.Floor((place.Z - _low.Z) / _size), 0, _depth - 1));
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

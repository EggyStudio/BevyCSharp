namespace Bevy;

// The space a tree grows in, the crown's outline, which the attractors fill, the grids the buds and
// the attractors are found in, and the grid of shadow the points cast.
internal sealed partial class TreeGrowth
{
    // How many attractors a cubic internode of the crown holds, and the most a tree has, so a large
    // crown costs no more than the points it can grow.
    private const float Density = 0.5f;
    private const int MostAttractors = 20_000;

    // The branches of the first level grown as the paper draws them, with no randomness, from this
    // many places up the trunk, each with its own branches from a quarter, a half and three
    // quarters along it, whose paths give the crown's outline.
    private const int Probes = 24;

    // Palubicki's shadow, a new point darkening its own voxel by this much and the voxels under it
    // in a pyramid, each layer down by half the layer above, to this many layers.
    private const float Darkening = 0.3f;
    private const int ShadowDepth = 6;

    // The crown's outline, the farthest from the trunk's axis the probes reach in each band of
    // height an internode tall, from the ground up.
    private float[] _outline = [];

    private Vec3[] _attractors = [];
    private bool[] _taken = [];
    private Grid _attractorCells = null!;
    private Grid _budCells = null!;
    private readonly List<List<int>> _around = [];
    private readonly List<int> _near = [];

    // Each bud's place and way, copied once a cycle for the perceiving.
    private Vec3[] _budPlaces = [];
    private Vec3[] _budWays = [];

    // The shadow, a voxel an internode wide, over the same box as the grids.
    private float[] _shadow = [];
    private Vec3 _shadowLow;
    private int _shadowWidth, _shadowHeight, _shadowDepth;

    /// <summary>
    /// Works out the crown's outline from the probes, and makes the grids and the shadow over a box
    /// holding it, a perception's reach round it, with the shade of the boxes the tree grows round.
    /// </summary>
    private void Outline()
    {
        _outline = new float[(int)MathF.Ceiling(2.5f * MathF.Max(_height, _levels.Count > 1 ? _levels[1].Length * _height : 0f) / _step) + 2];
        if (_levels.Count > 1)
        {
            var first = _levels[1];
            for (var probe = 0; probe < Probes; probe++)
            {
                var offset = _bare + (_height - _bare) * (probe + 0.5f) / Probes;
                var ratio = (_height - offset) / (_height - _bare);
                var lean = Radians(DownAngle(first, ratio, 0f));
                Probe(1, new Vec3(0f, offset, 0f), new Vec3(MathF.Sin(lean), MathF.Cos(lean), 0f), first.Length * _height * ShapeAt(ratio));
            }

            // Widened by a band either way and by an internode, so the outline holds the stems that
            // reach it as well as their paths.
            var widened = new float[_outline.Length];
            for (var band = 0; band < _outline.Length; band++)
            {
                var most = 0f;
                for (var near = Math.Max(band - 1, 0); near <= Math.Min(band + 1, _outline.Length - 1); near++) most = MathF.Max(most, _outline[near]);
                widened[band] = most > 0f ? most + _step : 0f;
            }

            _outline = widened;
        }

        var reach = _outline.Length > 0 ? _outline.Max() : 0f;
        var margin = 2f * Perception * _step;
        var low = new Vec3(-reach - margin, -margin, -reach - margin);
        var high = new Vec3(reach + margin, MathF.Max(_height, _outline.Length * _step) + margin, reach + margin);
        _attractorCells = new Grid(low, high, Perception * _step);
        _budCells = new Grid(low, high, Perception * _step);

        _shadowLow = low;
        _shadowWidth = (int)MathF.Ceiling((high.X - low.X) / _step);
        _shadowHeight = (int)MathF.Ceiling((high.Y - low.Y) / _step);
        _shadowDepth = (int)MathF.Ceiling((high.Z - low.Z) / _step);
        _shadow = new float[_shadowWidth * _shadowHeight * _shadowDepth];

        // A box the tree grows round is dark inside, shades what is under it fully and what is beside
        // it below its top as much as half the sky, less by half each internode further off, as a
        // wall or a neighbor's crown takes the sky from that side.
        foreach (var (boxLow, boxHigh) in _blockers)
        {
            for (var z = 0; z < _shadowDepth; z++)
            for (var x = 0; x < _shadowWidth; x++)
            for (var y = 0; y < _shadowHeight; y++)
            {
                var middle = _shadowLow + new Vec3(x + 0.5f, y + 0.5f, z + 0.5f) * _step;
                if (middle.Y > boxHigh.Y) continue;

                var outX = MathF.Max(MathF.Max(boxLow.X - middle.X, middle.X - boxHigh.X), 0f);
                var outZ = MathF.Max(MathF.Max(boxLow.Z - middle.Z, middle.Z - boxHigh.Z), 0f);
                var off = MathF.Sqrt(outX * outX + outZ * outZ) / _step;
                var shade = off > 0f ? 0.5f * MathF.Pow(0.5f, off) : middle.Y >= boxLow.Y ? 100f : 1f;
                if (off <= ShadowDepth) _shadow[x + _shadowWidth * (y + _shadowHeight * z)] += shade;
            }
        }
    }

    /// <summary>
    /// Follows a stem of a level from a place along a way for a length, bent as its level bends it
    /// with no randomness, marking the outline with every point, and from the first level, the
    /// stems of the second leaving it upward and downward.
    /// </summary>
    private void Probe(int level, Vec3 place, Vec3 way, float length)
    {
        var steps = Math.Max(1, (int)(length / _step));
        var quarter = 1;
        for (var i = 0; i < steps; i++)
        {
            way = Bend(way, level, Vec3.UnitZ, i * _step, length);
            place += way * _step;
            var band = Math.Clamp((int)(place.Y / _step), 0, _outline.Length - 1);
            _outline[band] = MathF.Max(_outline[band], MathF.Sqrt(place.X * place.X + place.Z * place.Z));

            if (level != 1 || _levels.Count < 3 || quarter > 3 || (i + 1) * 4 < quarter * steps) continue;
            quarter++;
            var next = _levels[2];
            var childLength = next.Length * (length - 0.6f * (i + 1) * _step);
            if (childLength < _step) continue;
            var lean = Radians(next.DownAngle);
            foreach (var sign in (ReadOnlySpan<float>)[1f, -1f]) Probe(2, place, Rotate(way, Vec3.UnitZ, sign * lean), childLength);
        }
    }

    /// <summary>Scatters the attractors through the outline, evenly through its volume, none in a box the tree grows round.</summary>
    private void Scatter()
    {
        var volume = 0.0;
        var cumulative = new double[_outline.Length];
        for (var band = 0; band < _outline.Length; band++)
        {
            volume += Math.PI * _outline[band] * _outline[band] * _step;
            cumulative[band] = volume;
        }

        var count = volume > 0.0 ? (int)Math.Min(MostAttractors, Math.Round(volume * Density / (_step * _step * _step))) : 0;
        var made = new List<Vec3>(count);
        for (var tries = 0; tries < count; tries++)
        {
            // A band by its share of the volume, a height in it, and a place in its disc by the
            // square root of a number, so the disc's rim holds as many as its middle by area.
            var band = Array.BinarySearch(cumulative, _random.Unit() * volume);
            band = Math.Min(band < 0 ? ~band : band, _outline.Length - 1);
            var radius = _outline[band] * MathF.Sqrt(_random.Unit());
            var angle = _random.Unit() * MathF.Tau;
            var place = new Vec3(radius * MathF.Cos(angle), (band + _random.Unit()) * _step, radius * MathF.Sin(angle));
            if (Blocked(place) || Pruned(place)) continue;

            _attractorCells.Add(place, made.Count);
            made.Add(place);
        }

        _attractors = [.. made];
        _taken = new bool[_attractors.Length];
    }

    /// <summary>
    /// Gives each attractor to the nearest bud that perceives it, looking only in the cells round
    /// those that hold a bud, since an attractor further off is out of every bud's reach.
    /// </summary>
    private void Perceive()
    {
        _budCells.Clear();
        if (_budPlaces.Length < _buds.Count)
        {
            _budPlaces = new Vec3[_buds.Count * 2];
            _budWays = new Vec3[_buds.Count * 2];
        }

        for (var i = 0; i < _buds.Count; i++)
        {
            var bud = _buds[i];
            bud.Toward = Vec3.Zero;
            bud.Perceived = 0;
            _budPlaces[i] = _places[bud.Node];
            _budWays[i] = bud.Way;
            _budCells.Add(_budPlaces[i], i);
        }

        _budCells.Near(_near);
        foreach (var cell in _near)
        {
            if (_attractorCells.At(cell) is not { } attractors) continue;
            foreach (var a in attractors)
            {
                if (!_taken[a]) Give(_attractors[a]);
            }
        }
    }

    /// <summary>Gives an attractor to the nearest bud within reach whose cone it is in, if any.</summary>
    private void Give(Vec3 attractor)
    {
        var reach = Perception * _step;
        var nearest = -1;
        var least = reach * reach;
        var way = Vec3.Zero;

        _budCells.Around(attractor, _around);
        foreach (var held in _around)
        {
            foreach (var i in held)
            {
                var to = attractor - _budPlaces[i];
                var squared = to.LengthSquared;
                if (squared >= least || squared == 0f) continue;
                if (Vec3.Dot(to, _budWays[i]) <= Cone * MathF.Sqrt(squared)) continue;
                nearest = i;
                least = squared;
                way = to;
            }
        }

        if (nearest < 0) return;
        _buds[nearest].Toward += way.Normalized;
        _buds[nearest].Perceived++;
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

    /// <summary>Darkens the voxel of a new point and the pyramid of voxels under it.</summary>
    private void Darken(Vec3 place)
    {
        var (x, y, z) = Voxel(place);
        var shade = Darkening;
        for (var down = 0; down <= ShadowDepth && y - down >= 0; down++, shade *= 0.5f)
        {
            if (y - down >= _shadowHeight) continue;
            for (var k = Math.Max(z - down, 0); k <= Math.Min(z + down, _shadowDepth - 1); k++)
            for (var i = Math.Max(x - down, 0); i <= Math.Min(x + down, _shadowWidth - 1); i++)
            {
                _shadow[i + _shadowWidth * (y - down + _shadowHeight * k)] += shade;
            }
        }
    }

    /// <summary>
    /// The light at a place, one in open sky and less the more shadow its voxel holds, the voxel's
    /// own point not counted, as Palubicki's exposure is, and full outside the grid.
    /// </summary>
    private float Light(Vec3 place) => MathF.Min(1f, 1f - ShadowAt(Voxel(place)) + Darkening);

    /// <summary>
    /// The level way the shadow lessens fastest from a place, or nothing where it is even, level
    /// since the shadow always lessens upward and the attraction up stands for that.
    /// </summary>
    private Vec3 AwayFromShadow(Vec3 place)
    {
        var (x, y, z) = Voxel(place);
        var slope = new Vec3(ShadowAt((x - 1, y, z)) - ShadowAt((x + 1, y, z)), 0f, ShadowAt((x, y, z - 1)) - ShadowAt((x, y, z + 1)));
        return slope.LengthSquared > 0.01f * Darkening * Darkening ? slope.Normalized : Vec3.Zero;
    }

    private (int X, int Y, int Z) Voxel(Vec3 place)
    {
        var at = (place - _shadowLow) * (1f / _step);
        return ((int)MathF.Floor(at.X), (int)MathF.Floor(at.Y), (int)MathF.Floor(at.Z));
    }

    private float ShadowAt((int X, int Y, int Z) voxel) =>
        voxel.X < 0 || voxel.Y < 0 || voxel.Z < 0 || voxel.X >= _shadowWidth || voxel.Y >= _shadowHeight || voxel.Z >= _shadowDepth
            ? 0f
            : _shadow[voxel.X + _shadowWidth * (voxel.Y + _shadowHeight * voxel.Z)];

    /// <summary>
    /// Indices filed by place in cells as wide as a bud perceives, over a box, a place outside it
    /// filed in the nearest cell at its edge, which still finds every attractor it perceives, since
    /// the attractors are all inside.
    /// </summary>
    private sealed class Grid
    {
        private readonly Vec3 _low;
        private readonly float _size;
        private readonly int _width, _height, _depth;
        private readonly List<int>?[] _cells;

        // The cells holding an index, in the order they first did, and for each cell the round in
        // which it was last listed, so a cell is listed once a round.
        private readonly List<int> _held = [];
        private readonly int[] _listed;
        private int _round;

        public Grid(Vec3 low, Vec3 high, float size)
        {
            _low = low;
            _size = size;
            _width = Math.Max(1, (int)MathF.Ceiling((high.X - low.X) / size));
            _height = Math.Max(1, (int)MathF.Ceiling((high.Y - low.Y) / size));
            _depth = Math.Max(1, (int)MathF.Ceiling((high.Z - low.Z) / size));
            _cells = new List<int>?[_width * _height * _depth];
            _listed = new int[_cells.Length];
        }

        public void Add(Vec3 place, int index)
        {
            var (x, y, z) = Of(place);
            var cell = x + _width * (y + _height * z);
            if ((_cells[cell] ??= []).Count == 0) _held.Add(cell);
            _cells[cell]!.Add(index);
        }

        public void Clear()
        {
            foreach (var cell in _held) _cells[cell]!.Clear();
            _held.Clear();
        }

        /// <summary>The indices in a cell, or nothing.</summary>
        public List<int>? At(int cell) => _cells[cell];

        /// <summary>Puts every cell next to a cell holding an index, those among them, in a list, each once, in a fixed order.</summary>
        public void Near(List<int> into)
        {
            into.Clear();
            _round++;
            foreach (var cell in _held)
            {
                var x = cell % _width;
                var y = cell / _width % _height;
                var z = cell / (_width * _height);
                for (var k = Math.Max(z - 1, 0); k <= Math.Min(z + 1, _depth - 1); k++)
                for (var j = Math.Max(y - 1, 0); j <= Math.Min(y + 1, _height - 1); j++)
                for (var i = Math.Max(x - 1, 0); i <= Math.Min(x + 1, _width - 1); i++)
                {
                    var near = i + _width * (j + _height * k);
                    if (_listed[near] == _round) continue;
                    _listed[near] = _round;
                    into.Add(near);
                }
            }
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
}

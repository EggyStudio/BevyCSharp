namespace Bevy;

// The faces' texture coordinates, projected onto the selected faces along a direction, from a box,
// about a cylinder's axis or round a sphere's middle, moved, scaled and turned, set to a texel
// density and put back to the world-aligned default, each a line of the recipe, so a saved model's
// coordinates are made again with its shape.
public sealed partial class EditMesh
{
    /// <summary>
    /// Texture coordinates as an affine function of a point, which a face's own coordinates are
    /// fitted to, so a point a cut or an inset makes on the face takes the coordinates the face had
    /// there.
    /// </summary>
    internal readonly record struct UvMap(Vec3 U, float UOffset, Vec3 V, float VOffset)
    {
        public Vec2 At(Vec3 point) => new(Vec3.Dot(U, point) + UOffset, Vec3.Dot(V, point) + VOffset);
    }

    /// <summary>The texture coordinates of a face's corners, in their order, its own or the world-aligned default.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    /// <returns>A copy, one for each corner.</returns>
    /// <remarks>
    /// The default is each corner's place in meters seen along the axis the face's normal is
    /// nearest, which follows the face wherever it moves, so a moved wall shows the grid of the
    /// place it was moved to. A face given its own by a <c>uv</c> operation keeps them on its
    /// corners as they move, as a texture stuck to the face.
    /// </remarks>
    public Vec2[] UvsOf(int face)
    {
        var known = _faces[face];
        return known.Uvs is { } own ? [.. own] : DefaultUvs(known);
    }

    /// <summary>Whether a face has texture coordinates of its own, given by a <c>uv</c> operation, rather than the world-aligned default.</summary>
    /// <param name="face">Its index, from zero to <see cref="FaceCount"/>.</param>
    public bool HasOwnUvs(int face) => _faces[face].Uvs is not null;

    /// <summary>Projects the selected faces' coordinates along a direction, as a picture shone onto them from that side.</summary>
    /// <param name="facing">
    /// The way from the faces to where the picture is shone from, such as <see cref="Vec3.UnitY"/>
    /// for a picture laid on a floor from above, or the way back toward a camera for its view.
    /// </param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// In meters, as the default is, the picture's first coordinate running to the right as seen
    /// from that side and its second down, the world's up being up in the picture where the
    /// direction is not straight up or down, so a projection along an axis gives a face facing that
    /// axis its default coordinates.
    /// </remarks>
    public EditMesh UvPlanar(Vec3 facing)
    {
        if (facing == Vec3.Zero || !float.IsFinite(facing.X + facing.Y + facing.Z))
            throw new ArgumentException("A direction to project along is finite and not zero.", nameof(facing));

        var (u, v) = PlanarAxes(facing.Normalized);
        foreach (var index in _selectedFaces)
        {
            var face = _faces[index];
            face.Uvs = face.Corners.Select(corner => new Vec2(Vec3.Dot(_positions[corner], u), Vec3.Dot(_positions[corner], v))).ToArray();
        }

        Changed();
        return Did($"uv planar {Facing(facing)}");
    }

    /// <summary>Gives the selected faces the world-aligned default as coordinates of their own, which then follow their corners.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The box projection, each face along the axis its normal is nearest, which is the default,
    /// written onto the faces so a move or a cut after it carries the coordinates with the face, and a
    /// start for <see cref="UvMove"/> and <see cref="UvScale(float, float, bool)"/> on a face that has none of its own.
    /// </remarks>
    public EditMesh UvBox()
    {
        foreach (var index in _selectedFaces) _faces[index].Uvs = DefaultUvs(_faces[index]);

        Changed();
        return Did("uv box");
    }

    /// <summary>Wraps the selected faces' coordinates round an axis, as a label round a can.</summary>
    /// <param name="axis">The axis, any length but zero.</param>
    /// <param name="center">A point the axis passes through, or nothing for the middle of the box holding the selection.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The first coordinate is the distance round the axis at the selection's mean distance from
    /// it, and the second the distance down along it, both in meters, so a cylinder's side shows the
    /// grid's meter as its flat faces do. A face across the seam, where the turn comes back to where
    /// it started, is wrapped from its own middle, so it is never stretched round the whole way.
    /// </remarks>
    public EditMesh UvCylinder(Vec3 axis, Vec3? center = null)
    {
        if (axis == Vec3.Zero || !float.IsFinite(axis.X + axis.Y + axis.Z))
            throw new ArgumentException("An axis to wrap about is finite and not zero.", nameof(axis));

        var along = axis.Normalized;
        var middle = center ?? SelectionMiddle();
        var across = PlanarAxes(along).U;
        var side = Vec3.Cross(along, across);

        (float X, float Y, float V) Place(Vec3 point)
        {
            var offset = point - middle;
            var height = Vec3.Dot(offset, along);
            var radial = offset - along * height;
            return (Vec3.Dot(radial, across), Vec3.Dot(radial, side), -height);
        }

        var radius = MeanRadius(point => { var (x, y, _) = Place(point); return MathF.Sqrt(x * x + y * y); });
        foreach (var index in _selectedFaces) Wrap(_faces[index], Place, radius);

        Changed();
        return Did($"uv cylinder {AxisWords(axis)}{(center is { } at ? " at " + V(at) : "")}");
    }

    /// <summary>Wraps the selected faces' coordinates round a point, as a map of the world round a globe.</summary>
    /// <param name="center">The globe's middle, or nothing for the middle of the box holding the selection.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The first coordinate is the distance round the world's up axis and the second the distance
    /// down from the equator, along the globe at the selection's mean distance from its middle, in
    /// meters. A face across the seam is wrapped from its own middle, as <see cref="UvCylinder"/>
    /// wraps one.
    /// </remarks>
    public EditMesh UvSphere(Vec3? center = null)
    {
        var middle = center ?? SelectionMiddle();
        var radius = MeanRadius(point => (point - middle).Length);

        (float X, float Y, float V) Place(Vec3 point)
        {
            var offset = point - middle;
            var level = MathF.Sqrt(offset.X * offset.X + offset.Z * offset.Z);
            return (offset.X, -offset.Z, -MathF.Atan2(offset.Y, level) * radius);
        }

        foreach (var index in _selectedFaces) Wrap(_faces[index], Place, radius);

        Changed();
        return Did($"uv sphere{(center is { } at ? " at " + V(at) : "")}");
    }

    /// <summary>Moves the selected faces' coordinates.</summary>
    /// <param name="u">How far along the first coordinate, a whole number moving a picture that repeats onto itself.</param>
    /// <param name="v">How far along the second.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh UvMove(float u, float v)
    {
        if (!float.IsFinite(u) || !float.IsFinite(v)) throw new ArgumentException("A move of coordinates is two finite numbers.", nameof(u));

        var by = new Vec2(u, v);
        ChangeUvs(each: false, (uv, _) => uv + by);
        return Did($"uv move {F(u)} {F(v)}");
    }

    /// <summary>Scales the selected faces' coordinates about their middle, so a picture is shown smaller where they grow.</summary>
    /// <param name="u">The factor along the first coordinate, two showing the picture twice as often.</param>
    /// <param name="v">The factor along the second.</param>
    /// <param name="each">Whether each face is scaled about its own middle, rather than the selection as a whole about the middle of all of them.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh UvScale(float u, float v, bool each = false)
    {
        if (!float.IsFinite(u) || !float.IsFinite(v) || u == 0f || v == 0f)
            throw new ArgumentException("A scale of coordinates is two finite numbers, neither zero.", nameof(u));

        ChangeUvs(each, (uv, middle) => middle + new Vec2((uv.X - middle.X) * u, (uv.Y - middle.Y) * v));
        return Did($"uv scale {F(u)}{(u == v ? "" : " " + F(v))}{(each ? " each" : "")}");
    }

    /// <summary>Scales the selected faces' coordinates by one factor about their middle.</summary>
    /// <param name="factor">The factor, two showing the picture twice as often.</param>
    /// <param name="each">Whether each face is scaled about its own middle.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh UvScale(float factor, bool each = false) => UvScale(factor, factor, each);

    /// <summary>Turns the selected faces' coordinates about their middle.</summary>
    /// <param name="degrees">How far, from the first coordinate toward the second.</param>
    /// <param name="each">Whether each face is turned about its own middle, rather than the selection as a whole about the middle of all of them.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh UvRotate(float degrees, bool each = false)
    {
        if (!float.IsFinite(degrees)) throw new ArgumentException("A turn is a finite number of degrees.", nameof(degrees));

        var (sin, cos) = MathF.SinCos(degrees * MathF.PI / 180f);
        ChangeUvs(each, (uv, middle) =>
        {
            var (x, y) = (uv.X - middle.X, uv.Y - middle.Y);
            return middle + new Vec2(x * cos - y * sin, x * sin + y * cos);
        });

        return Did($"uv rotate {F(degrees)}{(each ? " each" : "")}");
    }

    /// <summary>Scales the selected faces' coordinates about their middle so a texture of a size shows so many of its pixels a meter.</summary>
    /// <param name="pixelsPerMeter">How many of the texture's pixels a meter of the faces shows.</param>
    /// <param name="texturePixels">How wide the texture is, in pixels, which the console's command reads from the slot's texture.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// The density the faces have is measured over the selection as a whole, the square root of
    /// the area their coordinates cover over the area they cover in square meters, and one factor
    /// brings it to the one asked, so faces whose coordinates meet go on meeting. The recipe keeps
    /// the texture's width, so the line makes the same coordinates whether the texture is loaded
    /// or not.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The selected faces' coordinates cover no area to measure.</exception>
    public EditMesh UvDensity(float pixelsPerMeter, int texturePixels)
    {
        if (!(pixelsPerMeter > 0f) || !float.IsFinite(pixelsPerMeter)) throw new ArgumentOutOfRangeException(nameof(pixelsPerMeter), pixelsPerMeter, "A density is a finite number of pixels a meter, above zero.");
        if (texturePixels <= 0) throw new ArgumentOutOfRangeException(nameof(texturePixels), texturePixels, "A texture is a pixel wide or more.");

        if (_selectedFaces.Count > 0)
        {
            double covered = 0, area = 0;
            foreach (var index in _selectedFaces)
            {
                var face = _faces[index];
                covered += Math.Abs(Shoelace(face.Uvs ?? DefaultUvs(face)));
                area += Newell(face.Corners).Length / 2.0;
            }

            if (covered < 1e-12 || area < 1e-12) throw new InvalidOperationException("The selected faces' coordinates cover no area to measure a density over.");

            var factor = (float)(pixelsPerMeter / texturePixels / Math.Sqrt(covered / area));
            ChangeUvs(each: false, (uv, middle) => middle + (uv - middle) * factor);
        }

        return Did($"uv density {F(pixelsPerMeter)} {texturePixels}");
    }

    /// <summary>Puts the selected faces back on the world-aligned default.</summary>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh UvReset()
    {
        foreach (var index in _selectedFaces) _faces[index].Uvs = null;

        Changed();
        return Did("uv reset");
    }

    /// <summary>
    /// The two directions of a projection along a direction, the picture's right and down, those of
    /// the default for a direction along an axis.
    /// </summary>
    internal static (Vec3 U, Vec3 V) PlanarAxes(Vec3 facing)
    {
        // Straight up or down, the world's up is no help, and the default's axes are taken.
        if (MathF.Abs(facing.Y) > 0.999f) return BoxAxes(facing);

        var right = Vec3.Cross(Vec3.UnitY, facing).Normalized;
        return (right, Vec3.Cross(right, facing));
    }

    /// <summary>
    /// The two directions a face's default coordinates are measured along, by the axis its normal is
    /// nearest, chosen so the texture reads the right way round seen from in front of the face, its
    /// top up on a wall.
    /// </summary>
    internal static (Vec3 U, Vec3 V) BoxAxes(Vec3 normal)
    {
        float x = MathF.Abs(normal.X), y = MathF.Abs(normal.Y), z = MathF.Abs(normal.Z);

        // A texture's second coordinate grows downward, so a wall's runs down the world's Y.
        if (y >= x && y >= z) return normal.Y >= 0f ? (Vec3.UnitX, Vec3.UnitZ) : (Vec3.UnitX, -Vec3.UnitZ);
        if (x >= z) return normal.X >= 0f ? (-Vec3.UnitZ, -Vec3.UnitY) : (Vec3.UnitZ, -Vec3.UnitY);
        return normal.Z >= 0f ? (Vec3.UnitX, -Vec3.UnitY) : (-Vec3.UnitX, -Vec3.UnitY);
    }

    /// <summary>
    /// A face's coordinates as an affine function of a point in its plane, fitted to its corners by
    /// least squares, which is exact for coordinates a projection along a direction made, or nothing
    /// where the face has none of its own.
    /// </summary>
    private UvMap? FitOf(Face face)
    {
        if (face.Uvs is not { } uvs) return null;

        var corners = face.Corners;
        var origin = _positions[corners[0]];
        var (first, second) = PlanarAxes(Newell(corners).Normalized);

        // The corners as places along two directions in the face's plane, from its first corner,
        // and the sums of the normal equations of u = a x + b y + c, and of v alike.
        double xx = 0, xy = 0, yy = 0, sx = 0, sy = 0, xu = 0, yu = 0, su = 0, xv = 0, yv = 0, sv = 0;
        for (var i = 0; i < corners.Length; i++)
        {
            var offset = _positions[corners[i]] - origin;
            double x = Vec3.Dot(offset, first), y = Vec3.Dot(offset, second), u = uvs[i].X, v = uvs[i].Y;
            xx += x * x; xy += x * y; yy += y * y; sx += x; sy += y;
            xu += x * u; yu += y * u; su += u;
            xv += x * v; yv += y * v; sv += v;
        }

        double n = corners.Length;
        var det = xx * (yy * n - sy * sy) - xy * (xy * n - sy * sx) + sx * (xy * sy - yy * sx);
        if (Math.Abs(det) < 1e-12)
        {
            // A face with no area takes the mean of its coordinates everywhere.
            return new UvMap(Vec3.Zero, (float)(su / n), Vec3.Zero, (float)(sv / n));
        }

        (Vec3 Along, float Offset) Solve(double bx, double by, double b)
        {
            var a = (bx * (yy * n - sy * sy) - xy * (by * n - sy * b) + sx * (by * sy - yy * b)) / det;
            var c = (xx * (by * n - sy * b) - bx * (xy * n - sy * sx) + sx * (xy * b - by * sx)) / det;
            var d = (xx * (yy * b - by * sy) - xy * (xy * b - by * sx) + bx * (xy * sy - yy * sx)) / det;
            var along = first * (float)a + second * (float)c;
            return (along, (float)d - Vec3.Dot(along, origin));
        }

        var (u3, uOffset) = Solve(xu, yu, su);
        var (v3, vOffset) = Solve(xv, yv, sv);
        return new UvMap(u3, uOffset, v3, vOffset);
    }

    private Vec2[] DefaultUvs(Face face)
    {
        var (u, v) = BoxAxes(Newell(face.Corners));
        return face.Corners.Select(corner => new Vec2(Vec3.Dot(_positions[corner], u), Vec3.Dot(_positions[corner], v))).ToArray();
    }

    /// <summary>Changes every selected face's coordinates, the default written onto a face first, about the middle of all of them or of each.</summary>
    private void ChangeUvs(bool each, Func<Vec2, Vec2, Vec2> change)
    {
        var picked = _selectedFaces.Order().ToArray();
        foreach (var index in picked) _faces[index].Uvs ??= DefaultUvs(_faces[index]);

        var whole = UvMiddle(picked);
        foreach (var index in picked)
        {
            var uvs = _faces[index].Uvs!;
            var middle = each ? UvMiddle([index]) : whole;
            for (var i = 0; i < uvs.Length; i++) uvs[i] = change(uvs[i], middle);
        }

        Changed();
    }

    /// <summary>The middle of the box holding some faces' coordinates.</summary>
    private Vec2 UvMiddle(int[] faces)
    {
        var (low, high) = (new Vec2(float.MaxValue), new Vec2(float.MinValue));
        foreach (var index in faces)
        {
            foreach (var uv in _faces[index].Uvs!)
            {
                low = new Vec2(MathF.Min(low.X, uv.X), MathF.Min(low.Y, uv.Y));
                high = new Vec2(MathF.Max(high.X, uv.X), MathF.Max(high.Y, uv.Y));
            }
        }

        return faces.Length == 0 ? default : (low + high) * 0.5f;
    }

    /// <summary>
    /// Gives a face coordinates round an axis or a point, from each corner's place across it and how
    /// far down, the angle taken within half a turn of the face's middle's.
    /// </summary>
    private void Wrap(Face face, Func<Vec3, (float X, float Y, float V)> place, float radius)
    {
        var mean = Vec3.Zero;
        foreach (var corner in face.Corners) mean += _positions[corner];
        var (mx, my, _) = place(mean * (1f / face.Corners.Length));
        var reference = MathF.Atan2(my, mx);

        var uvs = new Vec2[face.Corners.Length];
        for (var i = 0; i < uvs.Length; i++)
        {
            var (x, y, down) = place(_positions[face.Corners[i]]);

            // A corner on the axis, as a cone's tip or a pole is, has no angle, and takes its face's.
            var angle = x * x + y * y < 1e-12f ? reference : MathF.Atan2(y, x);
            if (angle - reference > MathF.PI) angle -= 2f * MathF.PI;
            else if (angle - reference < -MathF.PI) angle += 2f * MathF.PI;

            uvs[i] = new Vec2(angle * radius, down);
        }

        face.Uvs = uvs;
    }

    /// <summary>The mean of a distance over the selected faces' corners, or one where it is none.</summary>
    private float MeanRadius(Func<Vec3, float> distance)
    {
        var corners = _selectedFaces.SelectMany(face => _faces[face].Corners).Distinct().ToArray();
        if (corners.Length == 0) return 1f;

        var mean = corners.Average(corner => distance(_positions[corner]));
        return mean > 1e-6f ? mean : 1f;
    }

    /// <summary>The middle of the box holding the selected faces' corners.</summary>
    private Vec3 SelectionMiddle() => Middle(_selectedFaces.SelectMany(face => _faces[face].Corners).Distinct().ToArray());

    /// <summary>Twice the signed area of a loop of coordinates.</summary>
    private static double Shoelace(Vec2[] loop)
    {
        double sum = 0;
        for (var i = 0; i < loop.Length; i++)
        {
            var (a, b) = (loop[i], loop[(i + 1) % loop.Length]);
            sum += (double)a.X * b.Y - (double)b.X * a.Y;
        }

        return sum / 2.0;
    }

    /// <summary>A direction written as the way along an axis, such as <c>+y</c>, where it is one, and as its three numbers where it is not.</summary>
    private static string Facing(Vec3 facing)
    {
        for (var axis = 0; axis < 3; axis++)
        {
            var unit = new Vec3(axis == 0 ? 1f : 0f, axis == 1 ? 1f : 0f, axis == 2 ? 1f : 0f);
            if (facing.Normalized == unit) return "+" + "xyz"[axis];
            if (facing.Normalized == -unit) return "-" + "xyz"[axis];
        }

        return V(facing);
    }

    /// <summary>An axis written as its letter where it runs along X, Y or Z, and as its three numbers where it does not.</summary>
    private static string AxisWords(Vec3 axis) =>
        axis.Y == 0f && axis.Z == 0f && axis.X > 0f ? "x" : axis.X == 0f && axis.Z == 0f && axis.Y > 0f ? "y" : axis.X == 0f && axis.Y == 0f && axis.Z > 0f ? "z" : V(axis);
}

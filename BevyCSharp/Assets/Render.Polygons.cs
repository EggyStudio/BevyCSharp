using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Builds a convex polygon pushed out into a solid <paramref name="depth"/> deep along Z,
    /// centered on its middle, from its corners in order around it. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>Extrusion</c> of a <c>ConvexPolygon</c>, its faces the polygon front and back and
    /// its sides running straight between their edges, as Bevy's 3d_shapes draws a house's end. A
    /// shape <see cref="MeshShape"/> names by its measures is made with
    /// <see cref="CreateMesh(string, float, float, float)"/>, and a flat polygon of any shape with
    /// <see cref="CreateMesh(MeshData)"/>.
    /// </remarks>
    /// <param name="corners">Three or more corners, in order around the polygon, in either direction.</param>
    /// <param name="depth">How deep the solid is.</param>
    /// <exception cref="ArgumentException">Fewer than three corners.</exception>
    /// <exception cref="BevyNativeException">
    /// The corners do not go around a convex shape, or this build has no renderer.
    /// </exception>
    public static AssetHandle CreateExtrusion(ReadOnlySpan<Vec2> corners, float depth)
    {
        if (corners.Length < 3) throw new ArgumentException("A polygon has three corners or more.", nameof(corners));

        fixed (Vec2* first = corners)
        {
            var key = Native.bcs_mesh_create_convex_extrusion((float*)first, corners.Length, depth);
            Native.Check(key, "extruding a convex polygon");
            return new AssetHandle(key);
        }
    }
}

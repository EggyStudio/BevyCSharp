namespace Bevy;

// The faces a level's brushes are built of, given by their corners and texture coordinates as the
// level file works them out rather than shaped by an operation, so they are no line of the recipe,
// a brush being kept as the level file writes it.
public sealed partial class EditMesh
{
    /// <summary>Adds a vertex where a level's brush has a corner, and answers its index.</summary>
    internal int AddCorner(Vec3 position)
    {
        var vertex = AddVertex(position);
        Changed();
        return vertex;
    }

    /// <summary>
    /// Adds a flat face of the given vertices, wound counter-clockwise seen from the side it faces,
    /// in a slot of the given name, made where the mesh has none, with the texture coordinates of
    /// its corners in their order.
    /// </summary>
    internal int AddPolygon(int[] corners, string slot, Vec2[] uvs)
    {
        OneWord(slot, nameof(slot));
        if (uvs.Length != corners.Length) throw new ArgumentException($"{uvs.Length} texture coordinates for {corners.Length} corners.", nameof(uvs));
        var face = AddFace(corners, SlotIndex(slot), 0f, uvs);
        Changed();
        return face;
    }
}

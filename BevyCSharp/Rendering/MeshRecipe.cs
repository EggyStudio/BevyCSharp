namespace Bevy;

/// <summary>How a primitive mesh was made, which is enough to make it again.</summary>
/// <param name="Shape">One of the constants on <see cref="MeshShape"/>.</param>
/// <param name="A">The first measure, as <see cref="Render.CreateMesh(string, float, float, float)"/> takes it.</param>
/// <param name="B">The second.</param>
/// <param name="C">The third.</param>
/// <remarks>
/// Kept beside the handle when the mesh is made, so a tool can show a cylinder as a cylinder with a
/// radius and a height rather than as a list of vertices, change one and build it again, and a scene
/// can write it down as what it is.
/// </remarks>
public readonly record struct MeshRecipe(string Shape, float A, float B, float C);

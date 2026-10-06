namespace Bevy;

/// <summary>The buffers of a geometry pool. See <see cref="Shaders.CreateGeometryPool"/>.</summary>
/// <param name="Vertices">Every vertex, as <c>bcs_scene::PoolVertex</c>.</param>
/// <param name="Indices">Every triangle's three vertex numbers, counted from its mesh's first vertex.</param>
/// <param name="Meshes">The meshes, as <c>bcs_scene::PoolMesh</c>, in the order they were added.</param>
public readonly record struct GeometryPool(AssetHandle Vertices, AssetHandle Indices, AssetHandle Meshes);

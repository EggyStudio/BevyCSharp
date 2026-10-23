namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Builds a distance field of the scene around the camera, in one to eight cascades, the
    /// finest's cells a given width and a given number built a frame at most, or turns it off with
    /// none, which it is by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The field holds how far each cell of a cascade is from the nearest surface of the meshes
    /// drawn that cast shadows, below zero inside a mesh that culls its back faces, which is the
    /// first of the three parts 3DEngine's global illumination is built from, and what a shader
    /// tracing a ray through the scene steps through. Each cascade is 64 cells a side, twice as
    /// coarse and as wide as the one before, so the default cells of a quarter of a unit reach 16
    /// units across in the first and 128 in the fourth, and each cascade takes 1 MB of the GPU's
    /// memory, twice over, beside the pool of the meshes' triangles. A distance is exact within four
    /// cells of a surface and held at four beyond. A closed wall thinner than a cell is held half a
    /// cell thick on either side of its middle, as a mesh drawn with both its faces is, so a ray
    /// between the cells on either side of it still meets it.
    /// </para>
    /// <para>
    /// The cascades lie around the camera the view is drawn by, a third of their width ahead of it,
    /// and move a few of their cells at a time as it moves. A mesh drawn in the same place for eight
    /// frames is built into the cascades around it from its triangles on the GPU, and one that moves
    /// is stamped each frame as the box around it, a frame behind it, since the engine reads the
    /// meshes before the program's systems move them. A skinned mesh is always stamped. A cascade is
    /// built again where the camera has gone past it or a mesh came or went, <paramref name="budget"/>
    /// a frame at most, the finest first, so a game sets a larger budget for a scene that changes and
    /// a smaller one for a slow GPU.
    /// </para>
    /// <para>
    /// It is built with compute shaders the library carries and compiles as it does a game's, so the
    /// first run on a machine with <c>slangc</c> fills the shader cache a shipped game reads, and
    /// nothing is built until they have compiled. The setting lasts while the app runs. The debug
    /// overlay and the <c>gi.state</c> command say what it holds.
    /// </para>
    /// </remarks>
    /// <param name="cascades">How many cascades, 1 to 8, or 0 for none.</param>
    /// <param name="cellSize">The width of a cell of the finest cascade, in world units.</param>
    /// <param name="budget">How many cascades are built again in one frame at most.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    /// <example>
    /// <code>
    /// Render.SetSceneField(4, cellSize: 0.25f, budget: 1);
    /// </code>
    /// </example>
    public static void SetSceneField(int cascades, float cellSize = 0.25f, int budget = 1)
    {
        if (App.Running is null)
            throw new InvalidOperationException("The scene's distance field belongs to an app, and none is running.");

        SceneField.Ask(Math.Clamp(cascades, 0, 8), Math.Max(1e-3f, cellSize), Math.Max(1, budget));
    }
}

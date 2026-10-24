namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Builds a distance field of the scene around the camera, in one to eight cascades, the
    /// finest's cells a given width and a given number built a frame at most, with a far cascade
    /// after them where asked, or turns it off with none, which it is by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The field holds how far each cell of a cascade is from the nearest surface of the meshes
    /// drawn that cast shadows, below zero inside a mesh that culls its back faces, which is the
    /// first of the three parts 3DEngine's global illumination is built from, and what a shader
    /// tracing a ray through the scene steps through. Each cascade is 64 cells a side, twice as
    /// coarse and as wide as the one before, so the default cells of a quarter of a unit reach 16
    /// units across in the first and 128 in the fourth. A distance is exact within four cells of a
    /// surface and held at four beyond. A closed wall thinner than a cell is held half a cell thick
    /// on either side of its middle, as a mesh drawn with both its faces is, so a ray between the
    /// cells on either side of it still meets it. Each cell is painted the color of its nearest
    /// surface, its material's base color times its vertices' colors, with the way that surface's
    /// face looks, which global illumination reads a ray's hit by. A cascade takes 5 MB of the GPU's
    /// memory, its distances, its colors and its faces with the still meshes' own beside them, apart
    /// from the pool of the meshes' triangles.
    /// </para>
    /// <para>
    /// With <paramref name="farCascade"/> above nought a far cascade follows the counted ones, its
    /// cells as wide as reaching that distance ahead of the camera takes, at least twice the last
    /// counted cascade's, held to the camera's far plane and built under the budget as the others
    /// are, so a ray that leaves the counted cascades, as global illumination's last cascade of
    /// probes, which stands in it, meets what lies far off before the sky.
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
    /// <param name="farCascade">How far ahead of the camera the far cascade reaches, in world units, or 0 for none.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    /// <example>
    /// <code>
    /// Render.SetSceneField(4, cellSize: 0.25f, budget: 1);
    /// Render.SetSceneField(4, farCascade: 400f);    // and a far cascade 400 units ahead
    /// </code>
    /// </example>
    public static void SetSceneField(int cascades, float cellSize = 0.25f, int budget = 1, float farCascade = 0)
    {
        if (App.Running is null)
            throw new InvalidOperationException("The scene's distance field belongs to an app, and none is running.");

        SceneField.Ask(Math.Clamp(cascades, 0, 8), Math.Max(1e-3f, cellSize), Math.Max(1, budget), float.IsFinite(farCascade) ? Math.Max(0, farCascade) : 0);
    }

    /// <summary>
    /// Chooses where the scene's distance field darkens the light of each directional light it
    /// closes a surface off from, past the light's shadow map's reach by default, everywhere, or
    /// nowhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A surface is traced toward the light through the field from a cell and a half out, 32 of
    /// the finest cells at most, and its light is darkened by how near a surface the trace passes
    /// as a share of how far along it is, so a shadow is soft where what casts it is far and hard
    /// where it meets what it falls on. Past the map's reach, beyond its last cascade, the ground
    /// is still shadowed where the map draws nothing, and within it the map's shadows keep their
    /// edges; everywhere, the two darken a surface together.
    /// </para>
    /// <para>
    /// Bevy's lighting takes no share of a light from outside, so the light Bevy gave a surface is
    /// worked out again from the camera's G-buffer as Bevy works it out, and taken back over the
    /// camera's lit picture before anything transparent is drawn over it. That needs the
    /// G-buffer, so the camera the field lies around draws Bevy's materials deferred while the
    /// shadows are on, which turns on <see cref="SetDeferredRendering"/> for every camera with a
    /// prepass. A material's clearcoat and anisotropy, Bevy's atmosphere and a light texture are not
    /// worked out again, so where a surface has them the light taken back differs by as much.
    /// </para>
    /// </remarks>
    /// <param name="shadows">Where the field darkens a directional light.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    public static void SetSceneFieldShadows(FieldShadows shadows)
    {
        if (App.Running is null)
            throw new InvalidOperationException("The scene's distance field belongs to an app, and none is running.");

        SceneField.AskShadows(shadows);
    }

    /// <summary>
    /// Draws the scene's distance field in an ImGui window of its own, what it is built from and
    /// a slice across one of its cascades, with its settings to change while it is looked at.
    /// Call it every frame the window is to be up, inside a system, with ImGui running
    /// (<see cref="Config.Gui"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The window says what <c>gi.state</c> says, sets the cascades, the finest's cells and the
    /// budget as <see cref="SetSceneField"/> does and the contact shadows as
    /// <see cref="SetSceneFieldShadows"/> does, and draws a slice across X, Y or Z of a cascade,
    /// through the eye unless a cell is chosen, four pixels a cell, each pixel reading the field
    /// between the cells around it. A point inside a mesh is red, the deeper the darker, one outside
    /// is gray, lighter the farther it is, in bands a cell apart, and one within a sixth of a cell
    /// of a surface is black, so the meshes' outlines read as lines and a gap the field closes shows
    /// where it does.
    /// </para>
    /// <para>
    /// Seen across Y the slice is from above, X to the right and Z down, so minus Z is up, and
    /// across X or Z from the side, up being up.
    /// </para>
    /// </remarks>
    public static void DrawSceneFieldWindow() => SceneField.DrawWindow();
}

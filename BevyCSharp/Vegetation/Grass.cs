using System.Numerics;

namespace Bevy;

/// <summary>
/// Grass planted over a stretch of ground in clumps of two cards crossed, cut from a grass set of
/// the texture collection and drawn with the trees' leaf material, so its blades take the sun and
/// the sky as the leaves do, let the light behind them through and sway in the trees' wind.
/// </summary>
/// <remarks>
/// <para>
/// The stretch is cut into square patches of <see cref="GrassSettings.PatchSize"/>, each a mesh of
/// its own under the entity <see cref="Plant"/> returns, so the camera culls the patches out of view
/// and a meadow is a draw a patch however many clumps it holds. A patch's mesh is made as it is
/// planted and stands as it was made, the wind moving it in the shader alone, so planting again is
/// the one thing that makes new meshes.
/// </para>
/// <para>
/// Each card's vertices carry its root in both of the streams the leaf material sways a tree's
/// leaves by, each with a phase of the clump's own, so a blade swings and flutters about its root
/// and its root stays where it stands, and the patch's blades lean together by their height. The
/// main pass and the prepass sway a vertex by the same function (<c>render/sway.rs</c>), so a
/// blade's depth and its color agree, as they must for the main pass to draw where the prepass
/// wrote. A blade's diffuse light comes from straight up, the way out the material reads from the
/// second texture coordinates, so it is lit as the ground under it is, and a blade lit from behind
/// lets through the share the set's surface gives, as a leaf does.
/// </para>
/// <para>
/// The blades cast no shadows unless <see cref="GrassSettings.CastsShadows"/> asks, and a run with no
/// renderer plants the patches' entities and draws nothing, as trees do. Grass has no collider.
/// </para>
/// </remarks>
public static class Grass
{
    // How stiff a clump stands in the wind, as the leaf material reads it (render/sway.rs): the
    // patch's blades lean together by their height squared, each card swings along the wind by its
    // reach from its root squared and flutters across it, five radians a second. A blade 0.7 m tall
    // moves its tip about a third of its height in a wind of 15 meters a second and about 6 cm in
    // the feature test's 4.3, as far as the meadow's old blades swayed; softer, the tips swing down
    // past the roots in a gale.
    internal static readonly Trees.TreeStiffness Stiffness = new(0.15f, 0.6f, 0.15f, 5f);

    /// <summary>Plants grass over a stretch of ground and returns the entity its patches stand under.</summary>
    /// <param name="ecs">The world, inside a system.</param>
    /// <param name="center">The middle of the stretch on the ground, where the blades' roots stand.</param>
    /// <param name="size">The stretch's width along X and its depth along Z, in meters.</param>
    /// <param name="settings">The set, the density, the height and the seed, the defaults where none are given.</param>
    /// <returns>The entity named <c>Grass</c> at <paramref name="center"/>, each patch a child of it.</returns>
    /// <remarks>
    /// The patches are square from the stretch's corner, the last of a row and a column cut to what
    /// is left, and a patch's clumps are scattered over it by a seed of its own from
    /// <see cref="GrassSettings.Seed"/>, so a stretch planted twice with the same settings stands
    /// the same. Moving or turning the returned entity moves the grass with it.
    /// </remarks>
    /// <exception cref="ArgumentException">The set is not a grass set the collection has, or a size or a setting is not a positive number.</exception>
    public static Entity Plant(EcsWorld ecs, Vec3 center, Vec2 size, GrassSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        settings ??= new GrassSettings();
        if (!(size.X > 0f && size.Y > 0f) || !float.IsFinite(size.X) || !float.IsFinite(size.Y))
            throw new ArgumentException($"Grass is planted over a stretch of some size, not {size.X} by {size.Y}.", nameof(size));
        if (!(settings.Density > 0f && settings.Height > 0f && settings.PatchSize > 0f) || !(settings.Variation >= 0f && settings.Variation < 1f))
            throw new ArgumentException("Grass needs a density, a height and a patch size above nothing and a variation from nothing up to less than the whole.", nameof(settings));
        var set = TreeTextures.Find(settings.Set) is { Kind: TreeTextureKind.Grass } found
            ? found
            : throw new ArgumentException($"The texture collection has no grass set {settings.Set}.", nameof(settings));
        var aspect = set.Size is { } picture ? picture.Width / (float)picture.Height : 1f;

        var draws = App.HasRenderer && App.Running is { } app && !app.Config.Headless;
        var root = ecs.Spawn();
        ecs.Add(root, Transform.At(center.X, center.Y, center.Z));
        ecs.SetName(root, "Grass");
        if (draws) ecs.Add(root, Visibility.Inherited);
        var material = draws ? Trees.GrassMaterial(set) : AssetHandle.None;

        var (across, along) = ((int)MathF.Ceiling(size.X / settings.PatchSize), (int)MathF.Ceiling(size.Y / settings.PatchSize));
        for (var row = 0; row < along; row++)
        {
            for (var column = 0; column < across; column++)
            {
                var x0 = column * settings.PatchSize;
                var z0 = row * settings.PatchSize;
                var (width, depth) = (MathF.Min(settings.PatchSize, size.X - x0), MathF.Min(settings.PatchSize, size.Y - z0));
                var patch = ecs.Spawn();
                ecs.Add(patch, Transform.At(x0 + width / 2f - size.X / 2f, 0f, z0 + depth / 2f - size.Y / 2f));
                ecs.SetParent(patch, root);
                ecs.SetName(patch, $"Grass patch {column}, {row}");
                if (!draws) continue;

                Render.SetMesh(ecs, patch, Render.CreateMesh(Patch(settings, aspect, width, depth, PatchSeed(settings.Seed, column, row)), kept: false));
                Render.SetMaterial(ecs, patch, material);
                if (!settings.CastsShadows) Render.SetMeshFlags(ecs, patch, MeshFlags.NoShadowCasting);
            }
        }

        return root;
    }

    /// <summary>A patch's own seed from the stretch's and where the patch stands in it.</summary>
    internal static uint PatchSeed(uint seed, int column, int row) => unchecked(seed * 0x9E3779B9u ^ (uint)column * 0x85EBCA6Bu ^ (uint)row * 0xC2B2AE35u);

    /// <summary>
    /// A patch's mesh, its clumps over a width along X and a depth along Z about its middle, each
    /// two cards crossed with its root on the ground at y 0.
    /// </summary>
    /// <remarks>
    /// The clumps fall on a grid of cells a clump each, each placed anywhere in its cell, so the
    /// grass covers its patch evenly without the rows of a grid. A card is as wide as the set's
    /// picture is for the clump's height, its texture's top at the card's top, and both of its sway
    /// streams hold the root with a phase from the clump's seed.
    /// </remarks>
    internal static MeshData Patch(GrassSettings settings, float aspect, float width, float depth, uint seed)
    {
        var cell = 1f / MathF.Sqrt(settings.Density);
        var (columns, rows) = (Math.Max(1, (int)MathF.Round(width / cell)), Math.Max(1, (int)MathF.Round(depth / cell)));
        var clumps = columns * rows;
        var positions = new Vec3[clumps * 8];
        var normals = new Vec3[clumps * 8];
        var uvs = new float[clumps * 16];
        var ways = new float[clumps * 16];
        var limbs = new Vector4[clumps * 8];
        var own = new Vector4[clumps * 8];
        var indices = new uint[clumps * 12];
        var random = new Pcg(seed);

        var at = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var root = new Vec3(
                    (column + random.Unit()) * width / columns - width / 2f,
                    0f,
                    (row + random.Unit()) * depth / rows - depth / 2f);
                var turn = random.Unit() * MathF.PI;
                var tall = settings.Height * (1f + settings.Variation * (2f * random.Unit() - 1f));
                var half = tall * aspect / 2f;
                var (swing, flutter) = (random.Unit(), random.Unit());

                for (var card = 0; card < 2; card++)
                {
                    var angle = turn + card * MathF.PI / 2f;
                    var across = new Vec3(MathF.Cos(angle), 0f, MathF.Sin(angle));
                    var facing = new Vec3(-across.Z, 0f, across.X);
                    var first = at * 4;
                    (positions[first], positions[first + 1], positions[first + 2], positions[first + 3]) =
                        (root - across * half, root + across * half, root + across * half + new Vec3(0f, tall, 0f), root - across * half + new Vec3(0f, tall, 0f));
                    (float U, float V)[] corners = [(0f, 1f), (1f, 1f), (1f, 0f), (0f, 0f)];
                    for (var corner = 0; corner < 4; corner++)
                    {
                        normals[first + corner] = facing;
                        (uvs[(first + corner) * 2], uvs[(first + corner) * 2 + 1]) = corners[corner];
                        // Straight up, folded as the leaf material unfolds its way out.
                        (ways[(first + corner) * 2], ways[(first + corner) * 2 + 1]) = (0.5f, 0.5f);
                        limbs[first + corner] = new Vector4(root.X, root.Y, root.Z, swing);
                        own[first + corner] = new Vector4(root.X, root.Y, root.Z, flutter);
                    }

                    var index = at * 6;
                    (indices[index], indices[index + 1], indices[index + 2]) = ((uint)first, (uint)first + 1, (uint)first + 2);
                    (indices[index + 3], indices[index + 4], indices[index + 5]) = ((uint)first, (uint)first + 2, (uint)first + 3);
                    at++;
                }
            }
        }

        return new MeshData
        {
            Positions = positions,
            Normals = normals,
            ComputeNormals = false,
            Uvs = uvs,
            SecondUvs = ways,
            Indices = indices,
            Streams = Trees.SwayStreams(limbs, own),
        };
    }
}

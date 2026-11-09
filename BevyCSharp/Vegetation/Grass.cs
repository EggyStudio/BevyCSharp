using System.Numerics;
using Vegetation;

namespace Bevy;

/// <summary>
/// Grass planted over a stretch of ground in clumps of two cards crossed, scattered by the
/// Vegetation module's <see cref="GrassScatter"/>, cut from a grass set of the texture collection
/// and drawn with the trees' leaf material, so its blades take the sun and the sky as the leaves do,
/// let the light behind them through and sway in the trees' wind.
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
    // How stiff a clump stands in the wind, as the leaf material reads it (render/sway.rs). The
    // patch's blades lean together by their height squared, each card swings along the wind by its
    // reach from its root squared and flutters across it, five radians a second. A blade 0.7 m tall
    // moves its tip about a third of its height in a wind of 15 meters a second and about 6 cm in a
    // breeze of 4.3, and softer, the tips would swing down past the roots in a gale.
    internal static readonly Trees.TreeStiffness Stiffness = new(0.15f, 0.6f, 0.15f, 5f);

    /// <summary>Plants grass over a stretch of ground and returns the entity its patches stand under.</summary>
    /// <param name="ecs">The world, inside a system.</param>
    /// <param name="center">The middle of the stretch on the ground, where the blades' roots stand.</param>
    /// <param name="size">The stretch's width along X and its depth along Z, in meters.</param>
    /// <param name="settings">The set, the density, the height and the seed, the defaults where none are given.</param>
    /// <returns>The entity named <c>Grass</c> at <paramref name="center"/>, each patch a child of it.</returns>
    /// <remarks>
    /// The patches and their clumps are those <see cref="GrassScatter.Patches"/> and
    /// <see cref="GrassScatter.Clumps"/> give, so a stretch planted twice with the same settings
    /// stands the same. Moving or turning the returned entity moves the grass with it.
    /// </remarks>
    /// <exception cref="ArgumentException">The set is not a grass set the collection has, or a size or a setting is not a positive number.</exception>
    public static Entity Plant(EcsWorld ecs, Vec3 center, Vec2 size, GrassSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        settings ??= new GrassSettings();
        var patches = GrassScatter.Patches(new Vector2(size.X, size.Y), settings);
        var set = TreeTextures.Find(settings.Set)!;
        var aspect = set.Size is { } picture ? picture.Width / (float)picture.Height : 1f;

        var draws = App.HasRenderer && App.Running is { } app && !app.Config.Headless;
        var root = ecs.Spawn();
        ecs.Add(root, Transform.At(center.X, center.Y, center.Z));
        ecs.SetName(root, "Grass");
        if (draws) ecs.Add(root, Visibility.Inherited);
        var material = draws ? Trees.GrassMaterial(set) : AssetHandle.None;

        foreach (var cut in patches)
        {
            var patch = ecs.Spawn();
            ecs.Add(patch, Transform.At(cut.Middle.X, 0f, cut.Middle.Y));
            ecs.SetParent(patch, root);
            ecs.SetName(patch, $"Grass patch {cut.Column}, {cut.Row}");
            if (!draws) continue;

            // The tangents the set's normal map is read along, without which Bevy reads no map and
            // leaves a card seen from behind facing away from the eye, so a blade's back took a sun
            // at the eye's back as light let through it and none on its face.
            var mesh = Render.CreateMesh(Patch(settings, aspect, cut.Size.X, cut.Size.Y, cut.Seed), kept: false);
            Render.GenerateTangents(mesh);
            Render.SetMesh(ecs, patch, mesh);
            Render.SetMaterial(ecs, patch, material);
            if (!settings.CastsShadows) Render.SetMeshFlags(ecs, patch, MeshFlags.NoShadowCasting);
        }

        return root;
    }

    /// <summary>
    /// A patch's mesh, its clumps over a width along X and a depth along Z about its middle, each
    /// two cards crossed with its root on the ground at y 0.
    /// </summary>
    /// <remarks>
    /// The clumps are those <see cref="GrassScatter.Clumps"/> scatters over the patch. A card is as
    /// wide as the set's picture is for the clump's height, its texture's top at the card's top, and
    /// both of its sway streams hold the root with the clump's phase.
    /// </remarks>
    internal static MeshData Patch(GrassSettings settings, float aspect, float width, float depth, uint seed)
    {
        var clumps = GrassScatter.Clumps(settings, new Vector2(width, depth), seed);
        var positions = new Vec3[clumps.Count * 8];
        var normals = new Vec3[clumps.Count * 8];
        var uvs = new float[clumps.Count * 16];
        var ways = new float[clumps.Count * 16];
        var limbs = new Vector4[clumps.Count * 8];
        var own = new Vector4[clumps.Count * 8];
        var indices = new uint[clumps.Count * 12];

        var at = 0;
        foreach (var clump in clumps)
        {
            var root = new Vec3(clump.Root.X, clump.Root.Y, clump.Root.Z);
            var tall = clump.Height;
            var half = tall * aspect / 2f;
            for (var card = 0; card < 2; card++)
            {
                var angle = clump.Turn + card * MathF.PI / 2f;
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
                    limbs[first + corner] = new Vector4(root.X, root.Y, root.Z, clump.Swing);
                    own[first + corner] = new Vector4(root.X, root.Y, root.Z, clump.Flutter);
                }

                var index = at * 6;
                (indices[index], indices[index + 1], indices[index + 2]) = ((uint)first, (uint)first + 1, (uint)first + 2);
                (indices[index + 3], indices[index + 4], indices[index + 5]) = ((uint)first, (uint)first + 2, (uint)first + 3);
                at++;
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

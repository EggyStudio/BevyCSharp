using System.Numerics;
using Vegetation;

namespace Bevy;

/// <summary>
/// Trees built into meshes through the modeling from the skeletons the Vegetation module grows from a
/// species and a seed, with no files to load.
/// </summary>
/// <remarks>
/// <see cref="TreeSkeleton.Grow(TreeSpecies, uint)"/> grows a tree's skeleton, the points its stems
/// pass through with their thickness, and <see cref="Build(TreeSkeleton, TreeDetail)"/> makes the
/// skeleton into an <see cref="EditMesh"/> of tapered tubes, which <see cref="Modeling.Meshes"/>
/// draws as it draws any other. The two are apart so a skeleton can be kept, in a level or a save,
/// and built again without growing it.
/// </remarks>
public static partial class Trees
{
    /// <summary>The material slot of the bark, which every face <see cref="Build(TreeSkeleton, TreeDetail)"/> makes is in.</summary>
    public const string BarkSlot = "bark";

    // How thin a stem the reduced level leaves out, against the trunk, where it starts.
    private const float ThinnestReduced = 1f / 25f;

    /// <summary>Makes a tree's skeleton into a mesh of its stems.</summary>
    /// <param name="skeleton">The skeleton.</param>
    /// <returns>A new mesh, every face in <see cref="BarkSlot"/>, colored as bark.</returns>
    /// <remarks>
    /// <para>
    /// Each stem is one <see cref="EditMesh.Tube"/> through its points, a stem carried on through a
    /// point by the thickest child there and every other child starting a stem of its own at the
    /// point it leaves. A stem's tube starts on its parent's middle, inside its parent, since a
    /// child is never thicker than the stem it leaves, so the joint is closed with no boolean.
    /// </para>
    /// <para>
    /// A stem has more sides the thicker it is against the trunk, from twelve for the trunk down to
    /// three for the thinnest twigs, so the triangles go where they are seen.
    /// </para>
    /// </remarks>
    public static EditMesh Build(TreeSkeleton skeleton) => Build(skeleton, TreeDetail.Full);

    /// <summary>Makes a tree's skeleton into a mesh of its stems at a level of detail.</summary>
    /// <param name="skeleton">The skeleton.</param>
    /// <param name="detail">The level, every stem with all its sides, or the reduced one a tree further off is drawn in.</param>
    /// <returns>A new mesh, every face in <see cref="BarkSlot"/>, colored as bark.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The level is not one of <see cref="TreeDetail"/>'s.</exception>
    /// <remarks>
    /// The full level is <see cref="Build(TreeSkeleton)"/>'s. The reduced one gives each stem half
    /// its sides, three at the fewest, and leaves out each stem thinner where it starts than a
    /// twenty-fifth of the trunk, and so the stems it carries, which are thinner still, as
    /// <see cref="TreeDetail.Reduced"/> says.
    /// </remarks>
    public static EditMesh Build(TreeSkeleton skeleton, TreeDetail detail)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        if (!Enum.IsDefined(detail)) throw new ArgumentOutOfRangeException(nameof(detail), detail, "A level of detail is one of TreeDetail's.");
        return Build(skeleton, detail, null);
    }

    /// <summary>
    /// The stems' mesh at a level of detail, noting where each stem starts, its first face and how
    /// many faces it has, where runs are asked for.
    /// </summary>
    private static EditMesh Build(TreeSkeleton skeleton, TreeDetail detail, List<(int Start, int First, int Faces)>? runs)
    {
        var reduced = detail == TreeDetail.Reduced;
        var nodes = skeleton.Nodes;

        // The child that carries each point's stem on, the thickest, the first of equals.
        var carried = Carried(nodes);

        var mesh = new EditMesh()
            .SlotRename(EditMesh.DefaultSlot, BarkSlot)
            .SlotColor(BarkSlot, 0.40f, 0.34f, 0.28f)
            .SlotRoughness(BarkSlot, 0.9f);

        for (var start = 0; start < nodes.Count; start++)
        {
            var parent = nodes[start].Parent;
            if (parent >= 0 && carried[parent] == start) continue;
            if (reduced && nodes[start].Radius < nodes[0].Radius * ThinnestReduced) continue;
            var first = mesh.FaceCount;
            Stem(mesh, nodes, carried, start, reduced ? 2 : 1);
            runs?.Add((start, first, mesh.FaceCount - first));
        }

        return mesh.SelectNone();
    }

    /// <summary>
    /// The child that carries each point's stem on, the thickest of its children, the first of
    /// equals, or none for a tip, so a stem is the run of points from where it starts through each
    /// point's carried child.
    /// </summary>
    private static int[] Carried(IReadOnlyList<TreeNode> nodes)
    {
        var carried = new int[nodes.Count];
        Array.Fill(carried, -1);
        for (var i = 1; i < nodes.Count; i++)
        {
            var parent = nodes[i].Parent;
            if (carried[parent] < 0 || nodes[i].Radius > nodes[carried[parent]].Radius) carried[parent] = i;
        }

        return carried;
    }

    /// <summary>
    /// Adds the stem that starts at a point as a tube, from its parent's middle through each point
    /// it carries on, with sides by its thickness against the trunk divided by a share.
    /// </summary>
    private static void Stem(EditMesh mesh, IReadOnlyList<TreeNode> nodes, int[] carried, int start, int fewerSides)
    {
        var points = new List<Vec3>();
        var radii = new List<float>();
        var parent = nodes[start].Parent;
        if (parent >= 0)
        {
            points.Add(Of(nodes[parent].Place));
            radii.Add(nodes[start].Radius);
        }

        for (var node = start; node >= 0; node = carried[node])
        {
            points.Add(Of(nodes[node].Place));
            radii.Add(nodes[node].Radius);
        }

        if (points.Count < 2) return;
        var sides = Math.Clamp(2 + (int)MathF.Round(10f * MathF.Sqrt(radii[0] / nodes[0].Radius)), 3, 12);
        mesh.Tube(points, radii, Math.Max(3, sides / fewerSides));
    }

    // The module's places and colors in the bridge's own types, a place as the meshes take it and a
    // color in linear sRGB as the materials do.
    private static Vec3 Of(Vector3 place) => new(place.X, place.Y, place.Z);

    private static Color ColorOf(Vector3 color) => new(color.X, color.Y, color.Z);
}

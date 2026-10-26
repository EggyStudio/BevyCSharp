using System.Numerics;

namespace Bevy;

// A planted tree swaying in the wind, each vertex of its stems and leaves carrying the pivot of the
// limb it hangs from and the pivot a leaf flutters about, which the trees' materials move it by, as
// native/bevy_csharp/src/render/sway.rs says.
public static partial class Trees
{
    // The streams the pivots go in, read by the trees' vertex stages at these locations.
    private const string LimbStream = "bcs_tree_limb";
    private const string OwnStream = "bcs_tree_own";
    private const uint LimbLocation = 8;
    private const uint OwnLocation = 9;

    // How far a tree's top bends at the wind's full push, a limb's tip swings and a leaf's tip
    // flutters, each against the tree's height or the leaf's length, and how fast a leaf flutters,
    // in radians a second.
    private const float TrunkBend = 0.02f;
    private const float LimbSwing = 0.016f;
    private const float LeafFlutter = 0.15f;
    private const float FrondFlutter = 0.1f;
    private const float FlutterSpeed = 9f;

    /// <summary>
    /// The wind the planted trees sway in, in meters a second, or none to take the weather's of the
    /// first camera given one, and still air where none was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A tree's trunk bends downwind by the square of a point's height, each limb swings about
    /// where it leaves its parent, and each leaf flutters about its stalk, each limb and leaf in a
    /// phase of its own and each tree in one from where it stands, so a forest of one species'
    /// variants does not sway in step. The push grows with the square of the wind's speed and gives
    /// way past some ten meters a second, a gale bending a tree's top by about a fiftieth of its
    /// height. A tree's far cards swing with the limbs whose leaves they picture and its impostor
    /// leans with its trunk, and every level's shadow sways with it.
    /// </para>
    /// <para>
    /// <see cref="Render.SetWeather"/> gusts its wind and turns it, so a tree in the weather
    /// sways as it does. A wind set here holds for every app until it is set again.
    /// </para>
    /// </remarks>
    public static Vec3? Wind { get; set; }

    /// <summary>How stiff a species stands in the wind, as the trees' materials hold it.</summary>
    internal readonly record struct TreeStiffness(float Trunk, float Limb, float Flutter, float Frequency);

    /// <summary>
    /// A species' stiffness, its top bending <see cref="TrunkBend"/> of its height, a limb as long
    /// as two fifths of its height swinging <see cref="LimbSwing"/> of it, and a leaf's tip
    /// fluttering <see cref="LeafFlutter"/> of the leaf's length, a frond's of the plant's height.
    /// </summary>
    internal static TreeStiffness Stiffness(TreeSpecies species)
    {
        var height = MathF.Max(species.Scale, 0.1f);
        var flutter = species.Leaves > 0 ? LeafFlutter / MathF.Max(species.LeafScale, 0.02f) : FrondFlutter / height;
        return new TreeStiffness(TrunkBend / height, LimbSwing / (0.16f * height), flutter, FlutterSpeed);
    }

    /// <summary>
    /// Which limb each stem swings with, the tree's <see cref="CardLimbs"/> thickest stems, the
    /// trunk among them, each gathering the thinner stems that grow from it.
    /// </summary>
    /// <param name="StemOf">The stem each point is on, by where the stem starts.</param>
    /// <param name="Starts">Where each stem starts.</param>
    /// <param name="Kept">The thickest stems.</param>
    /// <param name="Owner">The kept stem each stem grows from, itself where it is kept.</param>
    /// <param name="Nodes">The skeleton's points.</param>
    private sealed record LimbPlan(int[] StemOf, IReadOnlyList<int> Starts, HashSet<int> Kept, Dictionary<int, int> Owner, IReadOnlyList<TreeNode> Nodes)
    {
        /// <summary>
        /// The limb a stem swings with, none for the trunk, the kept stem it grows from, or itself
        /// for a stem that grows from the trunk below every kept limb.
        /// </summary>
        public int LimbOf(int start) => start == 0 ? -1 : Owner[start] == 0 ? start : Owner[start];

        /// <summary>
        /// A limb's pivot and phase, where it leaves its parent, or a point's own place for none,
        /// so the point does not swing.
        /// </summary>
        public Vector4 Pivot(int limb, Vec3 at)
        {
            if (limb < 0) return new Vector4(at.X, at.Y, at.Z, 0f);
            var pivot = Nodes[Nodes[limb].Parent].Place;
            return new Vector4(pivot.X, pivot.Y, pivot.Z, Phase(limb));
        }
    }

    /// <summary>The limbs a tree's stems swing with, worked out from its skeleton.</summary>
    private static LimbPlan PlanLimbs(TreeSkeleton skeleton)
    {
        var nodes = skeleton.Nodes;
        var carried = Carried(nodes);
        var stemOf = new int[nodes.Count];
        var starts = new List<int>();
        for (var start = 0; start < nodes.Count; start++)
        {
            var parent = nodes[start].Parent;
            if (parent >= 0 && carried[parent] == start) continue;
            starts.Add(start);
            for (var node = start; node >= 0; node = carried[node]) stemOf[node] = start;
        }

        var kept = starts.OrderByDescending(start => nodes[start].Radius).ThenBy(start => start).Take(CardLimbs).ToHashSet();

        // A stem starts after the one it grows from, so each stem's owner is known by its turn.
        var owner = new Dictionary<int, int>();
        foreach (var start in starts)
            owner[start] = kept.Contains(start) || start == 0 ? start : owner[stemOf[nodes[start].Parent]];

        return new LimbPlan(stemOf, starts, kept, owner, nodes);
    }

    /// <summary>A phase from 0 to 1 for a limb or a leaf, by its number.</summary>
    private static float Phase(int number) => (uint)(number * 2654435761u) / 4294967296f;

    /// <summary>
    /// Mesh data of stems built a tube a stem, each stem's faces from its first in a run, with each
    /// vertex's limb pivot from the stem's limb and its own pivot its own place.
    /// </summary>
    private static MeshData Swaying(EditMesh mesh, IReadOnlyList<(int Start, int First, int Faces)> runs, LimbPlan plan)
    {
        var data = Built(mesh);
        var limbs = new Vector4[data.Positions.Length];
        var own = new Vector4[data.Positions.Length];
        var vertex = 0;
        foreach (var (start, first, faces) in runs)
        {
            var limb = plan.LimbOf(start);
            for (var face = first; face < first + faces; face++)
            {
                for (var corner = 0; corner < mesh.Corners(face).Length; corner++, vertex++)
                {
                    var place = data.Positions[vertex];
                    limbs[vertex] = plan.Pivot(limb, place);
                    own[vertex] = new Vector4(place.X, place.Y, place.Z, 0f);
                }
            }
        }

        // The modeling writes a face's corners in turn, face after face, which the runs count.
        if (vertex != data.Positions.Length) throw new InvalidOperationException($"The stems' {data.Positions.Length} vertices are not the {vertex} their faces' corners count.");
        data.Streams = Streams(limbs, own);
        return data;
    }

    /// <summary>A stem mesh's data at a level of detail, each vertex carrying its limb's pivot.</summary>
    private static MeshData SwayingStems(TreeSkeleton skeleton, TreeDetail detail, LimbPlan plan)
    {
        var runs = new List<(int Start, int First, int Faces)>();
        var mesh = Build(skeleton, detail, runs);
        return Swaying(mesh, runs, plan);
    }

    /// <summary>
    /// A grown tree's full level of stems and leaves and its far level as a planted tree of its
    /// species draws them, each vertex carrying its sway.
    /// </summary>
    internal static (MeshData Stems, MeshData Leaves, CardPlan? Far) Swayed(TreeSkeleton skeleton, TreeSpecies species, uint seed)
    {
        var limbs = PlanLimbs(skeleton);
        var cards = MakeCards(skeleton, species, seed);
        cards.Sway(limbs);
        return (SwayingStems(skeleton, TreeDetail.Full, limbs), cards.Data(), species.Leaves > 0 ? PlanCards(skeleton, cards, limbs) : null);
    }

    /// <summary>The two sway streams of a mesh's vertices.</summary>
    private static MeshStream[] Streams(Vector4[] limbs, Vector4[] own) =>
    [
        MeshStream.From<Vector4>(LimbStream, MeshStreamFormat.Float4, LimbLocation, limbs),
        MeshStream.From<Vector4>(OwnStream, MeshStreamFormat.Float4, OwnLocation, own),
    ];

    /// <summary>
    /// Writes the wind into the trees' materials, the one set on <see cref="Wind"/> or the first
    /// weather's, once a frame.
    /// </summary>
    private static void Blow()
    {
        var wind = Wind ?? SkyWeather.All().Select(pair => (Vec3?)pair.Weather.Wind).FirstOrDefault() ?? Vec3.Zero;
        Render.SetTreeWind(wind);
    }
}

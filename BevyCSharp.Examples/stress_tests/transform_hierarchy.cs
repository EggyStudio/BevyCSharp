// Bevy's transform_hierarchy example, examples/stress_tests/transform_hierarchy.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.StressTests;

// Hierarchies of transforms built to one of nine shapes, some of whose nodes move every frame, to
// measure how transforms are carried down a hierarchy. The shape is named as the first argument,
// as large_tree, wide_tree, deep_tree, chain, update_leaves, update_shallow, humanoids_active,
// humanoids_inactive or humanoids_mixed, and nothing is drawn.
internal static class TransformHierarchy
{
    // A tree of a depth with a number of children a node, or the same leaning to one side, which
    // is far smaller, or rigs of a humanoid, those moving and those not.
    private abstract record TestCase;
    private sealed record Tree(int Depth, int BranchWidth) : TestCase;
    private sealed record NonUniformTree(int Depth, int BranchWidth) : TestCase;
    private sealed record Humanoids(int Active, int Inactive) : TestCase;

    // Which nodes move, by depth and by chance, the chance taken when a node is spawned.
    private sealed record UpdateFilter(float Probability, int MinDepth, int MaxDepth);

    private sealed record Cfg(TestCase TestCase, UpdateFilter UpdateFilter);

    private static readonly (string Name, Cfg Cfg)[] Configs =
    [
        ("large_tree", new Cfg(new NonUniformTree(18, 8), new UpdateFilter(0.5f, 0, int.MaxValue))),
        ("wide_tree", new Cfg(new Tree(3, 500), new UpdateFilter(0.5f, 0, int.MaxValue))),
        ("deep_tree", new Cfg(new NonUniformTree(25, 2), new UpdateFilter(0.5f, 0, int.MaxValue))),
        ("chain", new Cfg(new Tree(2500, 1), new UpdateFilter(0.5f, 0, int.MaxValue))),
        ("update_leaves", new Cfg(new Tree(18, 2), new UpdateFilter(0.5f, 17, int.MaxValue))),
        ("update_shallow", new Cfg(new Tree(18, 2), new UpdateFilter(0.5f, 0, 8))),
        ("humanoids_active", new Cfg(new Humanoids(4000, 0), new UpdateFilter(1f, 0, int.MaxValue))),
        ("humanoids_inactive", new Cfg(new Humanoids(10, 3990), new UpdateFilter(1f, 0, int.MaxValue))),
        ("humanoids_mixed", new Cfg(new Humanoids(2000, 2000), new UpdateFilter(1f, 0, int.MaxValue))),
    ];

    // The parent of each node of a humanoid rig after its root, from Mixamo's.
    private static readonly int[] HumanoidRig =
    [
        0, 1, 2, 3, 4, 5, 6, 6, 6, // hips, spine, spine 1 and 2, neck, head, its top and the eyes
        4, 10, 11, 12, 13, 14, 15, 16, 13, 18, 19, 20, 13, 22, 23, 24, 13, 26, 27, 28, 13, 30, 31, 32, // the left arm and hand
        4, 34, 35, 36, 37, 38, 39, 40, 37, 42, 43, 44, 37, 46, 47, 48, 37, 50, 51, 52, 37, 54, 55, 56, // the right arm and hand
        1, 58, 59, 60, 61, // the left leg
        1, 63, 64, 65, 66, // the right leg
    ];

    private static Cfg? _cfg;

    public static void Configure(Config config) => (config.Vsync, config.HeadlessFps, config.LogFrameTimes) = (false, 0u, true);

    public static void Build(App app)
    {
        var name = ConfigurationName(Environment.GetCommandLineArgs());
        _cfg = Configs.FirstOrDefault(known => known.Name == name).Cfg;

        if (_cfg is null)
        {
            Console.WriteLine(name is null ? "missing argument: <test configuration>\n" : $"test configuration \"{name}\" not found.\n");
            Console.WriteLine("available configurations:");
            foreach (var (known, _) in Configs) Console.WriteLine($"  {known}");
            app.Startup(_ => App.RequestExit(), "transform_hierarchy.Exit");
            return;
        }

        Console.WriteLine($"test configuration: {name}");
        Console.WriteLine($"\n{_cfg}");
        app.Startup(Setup, "transform_hierarchy.Setup");
    }

    // The first argument after the example's own name that is no option of the examples program,
    // as Bevy's program reads its first argument.
    private static string? ConfigurationName(string[] arguments)
    {
        for (var i = Array.IndexOf(arguments, "transform_hierarchy") + 1; i > 0 && i < arguments.Length; i++)
        {
            if (arguments[i] is "--frames" or "--size") i++;
            else if (!arguments[i].StartsWith('-')) return arguments[i];
        }

        return null;
    }

    private static void Setup(BehaviorContext ctx)
    {
        StressTest.Warn();
        var ecs = ctx.Ecs;
        var camera = Render2d.SpawnCamera2d();
        ecs.Set(camera, Transform.At(0f, 0f, 100f));

        var random = new Random();
        var cfg = _cfg!;
        var result = cfg.TestCase switch
        {
            Tree tree => SpawnTree(ecs, GenTree(tree.Depth, tree.BranchWidth), cfg.UpdateFilter, Transform.Identity, random),
            NonUniformTree tree => SpawnTree(ecs, GenNonUniformTree(tree.Depth, tree.BranchWidth), cfg.UpdateFilter, Transform.Identity, random),
            Humanoids humanoids => SpawnHumanoids(ecs, humanoids, cfg.UpdateFilter, random),
            _ => default,
        };

        Console.WriteLine($"\n{result}");
    }

    private static InsertResult SpawnHumanoids(EcsWorld ecs, Humanoids humanoids, UpdateFilter filter, Random random)
    {
        var result = new InsertResult();
        for (var i = 0; i < humanoids.Active; i++)
            result = result.Combine(SpawnTree(ecs, HumanoidRig, filter, Placed(random), random));

        // Forced still by a chance below nothing.
        for (var i = 0; i < humanoids.Inactive; i++)
            result = result.Combine(SpawnTree(ecs, HumanoidRig, filter with { Probability = -1f }, Placed(random), random));

        return result;
    }

    private static Transform Placed(Random random) =>
        Transform.At(random.NextSingle() * 500f - 250f, random.NextSingle() * 500f - 250f, 0f);

    /// <summary>How many nodes were spawned, how many move, and how deep the deepest is.</summary>
    private readonly record struct InsertResult(int InsertedNodes, int ActiveNodes, int MaximumDepth)
    {
        public InsertResult Combine(InsertResult other) =>
            new(InsertedNodes + other.InsertedNodes, ActiveNodes + other.ActiveNodes, Math.Max(MaximumDepth, other.MaximumDepth));
    }

    // A tree from the parent of each node after its root, each parent before its children, laid
    // out on circles by how far each is among its siblings, so a node that never moves is not at
    // the origin with the rest.
    private static InsertResult SpawnTree(EcsWorld ecs, IReadOnlyList<int> parents, UpdateFilter filter, Transform root, Random random)
    {
        var count = parents.Count + 1;
        var childCount = new int[count];
        var depth = new int[count];
        foreach (var parent in parents) childCount[parent]++;

        var entities = new Entity[count];
        entities[0] = ecs.Spawn();
        ecs.Add(entities[0], root);

        var (active, maximumDepth) = (0, 0);
        var childIndex = new int[count];
        for (var i = 0; i < parents.Count; i++)
        {
            var current = i + 1;
            var parent = parents[i];
            var separation = childIndex[parent] / (float)childCount[parent];
            childIndex[parent]++;

            depth[current] = depth[parent] + 1;
            maximumDepth = Math.Max(maximumDepth, depth[current]);

            var child = ecs.Spawn();
            if (random.NextSingle() <= filter.Probability && depth[current] >= filter.MinDepth && depth[current] <= filter.MaxDepth)
            {
                ecs.Add(child, new UpdateValue { Value = separation });
                active++;
            }

            ecs.Add(child, Transform.At(MathF.Cos(separation) * 32f, MathF.Sin(separation) * 32f, 0f));
            ecs.SetParent(child, entities[parent]);
            entities[current] = child;
        }

        return new InsertResult(count, active, maximumDepth);
    }

    // A tree depth levels deep, each node with branchWidth children.
    private static List<int> GenTree(int depth, int branchWidth)
    {
        var count = 0;
        for (var i = 0; i < depth - 1; i++) count += (int)Math.Pow(branchWidth, i);

        var tree = new List<int>(count * branchWidth);
        for (var i = 0; i < count; i++)
            for (var j = 0; j < branchWidth; j++) tree.Add(i);
        return tree;
    }

    // A tree with more nodes on one side than the other, its deepest path depth long and its
    // widest nodes with branchWidth children.
    private static List<int> GenNonUniformTree(int depth, int branchWidth)
    {
        var tree = new List<int>();
        AddChildrenNonUniform(tree, 0, depth, branchWidth);
        return tree;
    }

    private static void AddChildrenNonUniform(List<int> tree, int parent, int depth, int branchWidth)
    {
        for (var i = 0; i < branchWidth; i++)
        {
            tree.Add(parent);
            depth--;
            if (depth == 0) return;
            AddChildrenNonUniform(tree, tree.Count, depth, branchWidth);
        }
    }
}

/// <summary>A node that moves around a circle every frame, by an angle of its own.</summary>
[Behavior]
public partial struct UpdateValue
{
    /// <summary>The angle around the circle, which a tenth of a radian a second moves on.</summary>
    public float Value;

    /// <summary>Moved on, and put where the angle says on a circle of 32 around its parent.</summary>
    [OnUpdate]
    public void Update(BehaviorContext ctx, ref Transform transform)
    {
        Value += ctx.Time.Delta * 0.1f;
        transform.Translation.X = MathF.Cos(Value) * 32f;
        transform.Translation.Y = MathF.Sin(Value) * 32f;
    }
}

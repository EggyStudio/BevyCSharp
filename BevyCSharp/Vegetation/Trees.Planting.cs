using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Bevy.Physics;

namespace Bevy;

// Planting, the registry of species by name, the variants a species grows on a worker, and the
// system that dresses every entity with a Tree component in its variant's stems, leaves and
// colliders once its variant has grown.
public static partial class Trees
{
    /// <summary>How many variants a species grows, which the trees planted from it share, each turned and sized by its own seed.</summary>
    public const int Variants = 8;

    // The radius of the thinnest stem a capsule stands round, twelve centimeters across, and the
    // most capsules a variant has, the thickest stems' first, so a tree stops the player at its
    // trunk and its low limbs and no more.
    private const float ThinnestCollider = 0.06f;
    private const int MostCapsules = 24;

    // Where a tree is dithered from its full level to its reduced one, in its own heights from the
    // camera, which Bevy reads from the tree's foot. The tree fills about half a 45 degree picture's
    // height there, and the reduced level's half as many cards of twice the area cover the crown
    // within a sixth of what the full level's cover and the whole tree within a tenth, as
    // AReducedTreeCoversItsCrownAsTheFullOneDoesWhereTheyChange measures, so the change does not
    // show as the dither fades one level into the other over half a height.
    private const float ReducedFrom = 2.5f;
    private const float ReducedFade = 0.5f;

    // The far end of the furthest level, where nothing yet draws a tree further off.
    private const float Furthest = 1e7f;

    private static readonly Lock Registry = new();
    private static readonly Dictionary<string, TreeSpecies> Named = new(StringComparer.Ordinal)
    {
        ["quaking-aspen"] = TreeSpecies.QuakingAspen,
        ["black-tupelo"] = TreeSpecies.BlackTupelo,
        ["weeping-willow"] = TreeSpecies.WeepingWillow,
        ["california-black-oak"] = TreeSpecies.CaliforniaBlackOak,
        ["bush"] = TreeSpecies.Bush,
        ["fern"] = TreeSpecies.Fern,
    };

    // The variants grown, by species and variant, for the whole process, since a skeleton and its
    // meshes' data belong to no app.
    private static readonly ConcurrentDictionary<(TreeSpecies Species, int Variant), Lazy<Task<Grown>>> Growing = new();

    // Each app's dressed trees, and its meshes of each variant.
    private static readonly ConditionalWeakTable<App, Dictionary<Entity, Dressing>> Dressed = new();
    private static readonly ConditionalWeakTable<App, Dictionary<(TreeSpecies, int), (AssetHandle[] Stems, AssetHandle[] Leaves)>> Meshes = new();

    /// <summary>
    /// A variant grown, its skeleton and height, its stems' and leaves' data at each level of
    /// detail, by <see cref="TreeDetail"/>, and the capsules it stands as.
    /// </summary>
    private sealed record Grown(TreeSkeleton Skeleton, float Height, MeshData[] Stems, MeshData[] Leaves, IReadOnlyList<(Vec3 From, Vec3 To, float Radius)> Capsules);

    /// <summary>What a tree was dressed as, and the child it was dressed under.</summary>
    private sealed class Dressing(string species, uint seed)
    {
        public readonly string Species = species;
        public readonly uint Seed = seed;
        public Entity Body = Entity.None;
        public bool Warned;
    }

    /// <summary>The names species are registered under, the presets' and those registered since, in order.</summary>
    public static IReadOnlyList<string> SpeciesNames
    {
        get
        {
            lock (Registry) return [.. Named.Keys.Order(StringComparer.Ordinal)];
        }
    }

    /// <summary>Registers a species under a name, which a <see cref="Tree"/> component names it by, replacing one registered under it before.</summary>
    /// <param name="name">The name, one word of lower-case letters, digits and hyphens.</param>
    /// <param name="species">The species.</param>
    /// <exception cref="ArgumentException">The name is not one such word.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A control of the species is outside what it may be.</exception>
    /// <remarks>Trees of the name dressed before keep the species they were dressed as until their seed or species changes.</remarks>
    public static void Register(string name, TreeSpecies species)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(species);
        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))
        {
            throw new ArgumentException($"A species' name is one word of lower-case letters, digits and hyphens, and {name} is not.", nameof(name));
        }

        Check(species);
        lock (Registry) Named[name] = species;
    }

    /// <summary>The species registered under a name, or nothing.</summary>
    /// <param name="name">The name, as <c>quaking-aspen</c>.</param>
    public static TreeSpecies? Species(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (Registry) return Named.GetValueOrDefault(name);
    }

    /// <summary>Plants a tree, an entity at a place with a <see cref="Tree"/> component naming its species and seed.</summary>
    /// <param name="ecs">The world.</param>
    /// <param name="species">The species' registered name.</param>
    /// <param name="seed">The seed, which picks its variant and turns and sizes it.</param>
    /// <param name="place">Where its trunk's foot stands.</param>
    /// <returns>The tree's entity, which the library dresses once its variant has grown.</returns>
    /// <exception cref="ArgumentException">No species is registered under the name.</exception>
    public static Entity Plant(EcsWorld ecs, string species, uint seed, Vec3 place)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        if (Species(species) is null) throw new ArgumentException($"No species is registered as {species}, of {string.Join(", ", SpeciesNames)}.", nameof(species));

        var tree = ecs.Spawn();
        ecs.Add(tree, Transform.At(place.X, place.Y, place.Z));
        ecs.Add(tree, new Tree { Species = species, Seed = seed });
        ecs.SetName(tree, $"{species} {seed}");
        return tree;
    }

    /// <summary>Plants a forest, trees of a species spread over a disc on level ground, none nearer another than an even spread allows.</summary>
    /// <param name="ecs">The world.</param>
    /// <param name="species">The species' registered name.</param>
    /// <param name="seed">The seed the trees' places and their own seeds are drawn from.</param>
    /// <param name="middle">The disc's middle, at the height the trees stand at.</param>
    /// <param name="radius">The disc's radius in meters.</param>
    /// <param name="count">How many trees, fewer where they do not fit apart.</param>
    /// <returns>The trees' entities.</returns>
    /// <remarks>
    /// Each tree is drawn at random in the disc and kept where it stands seven tenths of an even
    /// spread's spacing from every tree before it, Poisson disk sampling by trial, so the forest is
    /// neither a grid nor a heap, and the seed grows the same forest each time.
    /// </remarks>
    public static IReadOnlyList<Entity> PlantForest(EcsWorld ecs, string species, uint seed, Vec3 middle, float radius, int count)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        if (!(radius > 0f) || !float.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(radius), radius, "A forest's radius is a finite number above zero.");
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (Species(species) is null) throw new ArgumentException($"No species is registered as {species}, of {string.Join(", ", SpeciesNames)}.", nameof(species));

        var random = new Pcg((ulong)seed);
        var apart = 0.7f * MathF.Sqrt(MathF.PI * radius * radius / MathF.Max(count, 1));
        var places = new List<Vec3>();
        for (var tries = 0; places.Count < count && tries < count * 30; tries++)
        {
            var reach = radius * MathF.Sqrt(random.Unit());
            var angle = random.Unit() * MathF.Tau;
            var place = middle + new Vec3(reach * MathF.Cos(angle), 0f, reach * MathF.Sin(angle));
            if (places.All(other => (other - place).LengthSquared >= apart * apart)) places.Add(place);
        }

        return [.. places.Select(place => Plant(ecs, species, random.Next(), place))];
    }

    /// <summary>
    /// Dresses every tree whose variant has grown, starting the growth of those whose have not, and
    /// dresses again a tree whose species or seed changed. Called once a frame.
    /// </summary>
    internal static void Tick(World world)
    {
        if (App.Running is not { } app) return;
        var ecs = world.Resource<EcsWorld>();
        var dressed = Dressed.GetOrCreateValue(app);
        var draws = App.HasRenderer && !world.Resource<Config>().Headless;

        var trees = new List<(Entity Entity, string Species, uint Seed)>();
        foreach (var row in ecs.Query<Tree>(markChanged: false)) trees.Add((row.Entity, row.Component.Species.Value ?? "", row.Component.Seed));

        foreach (var gone in dressed.Keys.Where(entity => !ecs.IsAlive(entity) || !ecs.Has<Tree>(entity)).ToArray())
        {
            if (ecs.IsAlive(dressed[gone].Body)) ecs.Despawn(dressed[gone].Body);
            dressed.Remove(gone);
        }

        foreach (var (entity, name, seed) in trees)
        {
            if (dressed.TryGetValue(entity, out var dressing) && (dressing.Species != name || dressing.Seed != seed))
            {
                if (ecs.IsAlive(dressing.Body)) ecs.Despawn(dressing.Body);
                dressed.Remove(entity);
                dressing = null;
            }

            if (dressing is null) dressed[entity] = dressing = new Dressing(name, seed);
            if (dressing.Body != Entity.None) continue;

            if (Species(name) is not { } species)
            {
                if (!dressing.Warned) Log.Warn($"The tree {entity} names the species {name}, which is not registered, of {string.Join(", ", SpeciesNames)}, and stands bare.");
                dressing.Warned = true;
                continue;
            }

            var variant = (int)(seed % Variants);
            var growing = Growing.GetOrAdd((species, variant), key => new Lazy<Task<Grown>>(() => Task.Run(() => GrowVariant(key.Species, key.Variant)))).Value;
            if (growing.IsFaulted)
            {
                if (!dressing.Warned) EngineLog.Error(app, "trees", $"[BevyCSharp] The {name} tree's variant {variant} did not grow. {growing.Exception?.InnerException?.Message}", growing.Exception);
                dressing.Warned = true;
                continue;
            }

            if (!growing.IsCompletedSuccessfully) continue;
            dressing.Body = Dress(ecs, app, entity, name, species, variant, seed, growing.Result, draws);
        }
    }

    /// <summary>Grows a variant of a species, its stems and leaves built and its capsules found, on whatever thread runs it.</summary>
    private static Grown GrowVariant(TreeSpecies species, int variant)
    {
        // The variants' own seeds, apart from any seed a game gives, so the same species grows the
        // same eight in every game.
        var seed = (uint)(0x7E57_0000 + variant);
        var skeleton = Grow(species, seed);
        TreeDetail[] levels = [TreeDetail.Full, TreeDetail.Reduced];
        var stems = levels.Select(level => Modeling.Build(Build(skeleton, level))[0]).ToArray();
        var leaves = levels.Select(level => BuildLeaves(skeleton, species, seed, level)).ToArray();
        return new Grown(skeleton, skeleton.Bounds.High.Y, stems, leaves, Capsules(skeleton));
    }

    /// <summary>Dresses a tree under a child of its own, turned and sized by its seed, with its variant's meshes, materials and capsules.</summary>
    private static Entity Dress(EcsWorld ecs, App app, Entity tree, string name, TreeSpecies species, int variant, uint seed, Grown grown, bool draws)
    {
        // A turn and a size of a tenth either way, from the seed.
        var random = new Pcg((ulong)seed * 0x9E3779B97F4A7C15ul + 1);
        var turn = Quat.FromRotationY(random.Unit() * MathF.Tau);
        var size = 0.9f + 0.2f * random.Unit();

        // The tree's own entity takes part in Bevy's visibility, as the parent of a mesh has to, or
        // Bevy warns (B0004) that what is drawn under it is shown inconsistently. Plant's entity and
        // one a scene gives no more than a Tree are given it here alike.
        if (draws && !ecs.Has<Visibility>(tree)) ecs.Add(tree, Visibility.Inherited);

        var body = ecs.Spawn();
        ecs.Add(body, new Transform(Vec3.Zero, turn, new Vec3(size)));
        ecs.SetParent(body, tree);
        ecs.SetName(body, $"{name} {seed}, stems");

        if (draws)
        {
            var meshes = Meshes.GetOrCreateValue(app);
            if (!meshes.TryGetValue((species, variant), out var made))
            {
                meshes[(species, variant)] = made = (
                    [.. grown.Stems.Select(Created)],
                    [.. grown.Leaves.Select(leaves => leaves.Positions.Length > 0 ? Created(leaves) : AssetHandle.None)]);
            }

            // The full level near the camera and the reduced one further off, measured in the
            // tree's own heights so a bush changes as near as it is small, each mesh dithered out
            // over the half height where the other is dithered in.
            var height = grown.Height * size;
            var full = (Start: new FloatRange(0f, 0f), End: new FloatRange(ReducedFrom * height, (ReducedFrom + ReducedFade) * height));
            var reduced = (Start: full.End, End: new FloatRange(Furthest, Furthest));

            Render.SetMesh(ecs, body, made.Stems[(int)TreeDetail.Full]);
            Render.SetMaterial(ecs, body, BarkMaterial(species));
            Ranged(ecs, body, full);
            Part(ecs, body, $"{name} {seed}, stems, reduced", made.Stems[(int)TreeDetail.Reduced], BarkMaterial(species), reduced);
            if (made.Leaves[(int)TreeDetail.Full] != AssetHandle.None)
            {
                Part(ecs, body, $"{name} {seed}, leaves", made.Leaves[(int)TreeDetail.Full], LeafMaterial(species), full);
                Part(ecs, body, $"{name} {seed}, leaves, reduced", made.Leaves[(int)TreeDetail.Reduced], LeafMaterial(species), reduced);
            }
        }

        // A capsule stands along Y, so each is turned from Y onto its run and set at its middle.
        foreach (var (from, to, radius) in grown.Capsules)
        {
            var run = to - from;
            var capsule = ecs.Spawn();
            ecs.Add(capsule, new Transform((from + to) * 0.5f, Turn(Vec3.UnitY, run.Normalized), Vec3.One));
            ecs.Add(capsule, new RigidBody { Kind = BodyKind.Static });
            ecs.Add(capsule, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(2f * radius, run.Length + 2f * radius, 2f * radius) });
            ecs.SetParent(capsule, body);
            ecs.SetName(capsule, $"{name} {seed}, collider");
        }

        return body;
    }

    /// <summary>A mesh made from a variant's data, with the tangents its bark's or leaves' normal map is read along.</summary>
    private static AssetHandle Created(MeshData data)
    {
        var mesh = Render.CreateMesh(data);
        Render.GenerateTangents(mesh);
        return mesh;
    }

    /// <summary>A part of a tree's body, a mesh in a material drawn over a range of distances.</summary>
    private static void Part(EcsWorld ecs, Entity body, string name, AssetHandle mesh, AssetHandle material, (FloatRange Start, FloatRange End) range)
    {
        var part = ecs.Spawn();
        ecs.Add(part, Transform.Identity);
        Render.SetMesh(ecs, part, mesh);
        Render.SetMaterial(ecs, part, material);
        Ranged(ecs, part, range);
        ecs.SetParent(part, body);
        ecs.SetName(part, name);
    }

    /// <summary>
    /// Gives a mesh Bevy's visibility range, drawn from the start's far end to the end's near end
    /// and dithered in and out over each, measured from the entity's place.
    /// </summary>
    private static void Ranged(EcsWorld ecs, Entity entity, (FloatRange Start, FloatRange End) range)
    {
        var visibility = ecs.Insert<Bevy.Reflected.VisibilityRangeRef>(entity);
        visibility.StartMargin = range.Start;
        visibility.EndMargin = range.End;
        visibility.UseAabb = false;
    }

    /// <summary>
    /// The capsules a tree stands as, along each stem thick enough to stop a body, a capsule a nearly
    /// straight run of its points, the thickest stems' first.
    /// </summary>
    private static List<(Vec3 From, Vec3 To, float Radius)> Capsules(TreeSkeleton skeleton)
    {
        var nodes = skeleton.Nodes;
        var carried = new int[nodes.Count];
        Array.Fill(carried, -1);
        for (var i = 1; i < nodes.Count; i++)
        {
            var parent = nodes[i].Parent;
            if (carried[parent] < 0 || nodes[i].Radius > nodes[carried[parent]].Radius) carried[parent] = i;
        }

        var capsules = new List<(Vec3 From, Vec3 To, float Radius)>();
        var starts = Enumerable.Range(0, nodes.Count).Where(i => i == 0 || carried[nodes[i].Parent] != i).OrderByDescending(i => nodes[i].Radius);
        foreach (var start in starts)
        {
            if (nodes[start].Radius < ThinnestCollider || capsules.Count >= MostCapsules) break;

            // A run goes on while its stem is thick enough and turns less than a fifth of a right
            // angle from where the run began.
            var from = start == 0 ? start : nodes[start].Parent;
            var node = start;
            var widest = 0f;
            while (node >= 0 && nodes[node].Radius >= ThinnestCollider && capsules.Count < MostCapsules)
            {
                var way = (nodes[node].Place - nodes[from].Place).Normalized;
                var next = carried[node];
                var turns = next >= 0 && Vec3.Dot((nodes[next].Place - nodes[node].Place).Normalized, way) < MathF.Cos(MathF.PI / 10f);
                widest = MathF.Max(widest, nodes[node].Radius);
                if (next < 0 || turns || nodes[next].Radius < ThinnestCollider)
                {
                    if (node != from) capsules.Add((nodes[from].Place, nodes[node].Place, widest));
                    from = node;
                    widest = 0f;
                }

                node = next;
            }
        }

        return capsules;
    }

    /// <summary>The least rotation taking one unit way onto another.</summary>
    private static Quat Turn(Vec3 from, Vec3 to)
    {
        var axis = Vec3.Cross(from, to);
        var dot = Math.Clamp(Vec3.Dot(from, to), -1f, 1f);
        if (axis.LengthSquared < 1e-10f) return dot > 0f ? Quat.Identity : Quat.FromAxisAngle(Vec3.UnitX, MathF.PI);
        return Quat.FromAxisAngle(axis.Normalized, MathF.Acos(dot));
    }
}

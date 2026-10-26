using System.Runtime.CompilerServices;
using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// What the trees hold, by species, variant and part, the process's for as long as it runs and an
/// app's while it draws a variant first, and what a forest of a thousand trees of three species holds
/// once it is dressed and leaves behind once its app has ended.
/// </summary>
[Collection("engine")]
public sealed class TreeMemoryTests(ITestOutputHelper output)
{
    private const double Megabyte = 1 << 20;

    [Fact]
    public void AVariantKeepsItsSkeletonAndCapsulesAloneForTheProcess()
    {
        // Every preset's eight variants, grown and shaped four at a time, so no more than four
        // variants' shapes, some tens of megabytes each, are held at once.
        TreeSpecies[] presets = [TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak, TreeSpecies.Bush, TreeSpecies.Fern];
        string[] names = ["quaking-aspen", "black-tupelo", "weeping-willow", "california-black-oak", "bush", "fern"];
        var held = new IReadOnlyList<(string Part, bool Lasting, long Bytes)>[presets.Length, Trees.Variants];
        Parallel.For(0, presets.Length * Trees.Variants, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            at => held[at / Trees.Variants, at % Trees.Variants] = Trees.HeldBy(presets[at / Trees.Variants], at % Trees.Variants));

        for (var species = 0; species < presets.Length; species++)
        {
            var parts = new Dictionary<(string, bool), long>();
            for (var variant = 0; variant < Trees.Variants; variant++)
                foreach (var (part, lasting, bytes) in held[species, variant])
                    parts[(part, lasting)] = parts.GetValueOrDefault((part, lasting)) + bytes;

            string Said(bool lasting) => string.Join(", ", parts.Where(pair => pair.Key.Item2 == lasting).Select(pair => $"{pair.Key.Item1} {pair.Value / Megabyte:0.0}"));
            var (kept, drawn) = (parts.Where(pair => pair.Key.Item2).Sum(pair => pair.Value), parts.Where(pair => !pair.Key.Item2).Sum(pair => pair.Value));
            output.WriteLine($"{names[species]}'s eight variants keep {kept / Megabyte:0.00} MB, {Said(true)}, and are drawn from {drawn / Megabyte:0.0} MB, {Said(false)}");

            // A willow's skeleton, the largest, is some seven thousand points of twenty bytes.
            Assert.True(kept < Trees.Variants * 256 * 1024, $"{names[species]}'s variants keep {kept / Megabyte:0.00} MB for the process");
        }

        // What shaping a variant allocates beside what it keeps, its first variant shaped again on
        // this thread alone, which the collector takes back once the variant is pictured.
        for (var species = 0; species < presets.Length; species++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            var shaped = Trees.HeldBy(presets[species], 0).Where(part => !part.Lasting).Sum(part => part.Bytes);
            output.WriteLine($"{names[species]}'s first variant allocates {(GC.GetAllocatedBytesForCurrentThread() - before) / Megabyte:0} MB as it is shaped, for the {shaped / Megabyte:0.0} MB it keeps");
        }
    }

    [SkippableFact]
    public void AThousandTreeForestOfThreeSpeciesHoldsItsMeshesAndAtlasesAlone()
    {
        Needs.Renderer();

        // Read before, while dressed, and once the forest's app and one after it have run, which
        // takes over what the engine keeps of the last app it ran.
        var before = ConsoleMemoryCommands.Collected();
        var (trees, dressed, held) = Forest();
        new PictureRun().Wait(5).Go();
        var after = ConsoleMemoryCommands.Collected();
        output.WriteLine($"before, {before}");
        output.WriteLine($"{trees} trees dressed, their variants keeping {held.Process / Megabyte:0.00} MB for the process and holding {held.Drawing / Megabyte:0.00} MB more in the app, {dressed}");
        output.WriteLine($"after, {after}");

        // Every variant's shapes and pictures let go once its far levels are pictured, and the
        // twenty-four variants' skeletons and capsules kept, a few megabytes.
        Assert.Equal(0, held.Drawing);
        Assert.True(held.Process < 8 * Megabyte, $"the variants keep {held.Process / Megabyte:0.00} MB");
        var (left, was) = (Live(after), Live(before));
        Assert.True(left - was < 64 * Megabyte, $"the forest leaves {(left - was) / Megabyte:0} MB behind");
    }

    /// <summary>
    /// A thousand trees of three species on three discs side by side, a third each, drawn at 640 by
    /// 360, and what is held once they are all dressed, in a frame of its own so nothing of the app
    /// outlives it here.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (int Trees, string Dressed, (long Process, long Drawing) Held) Forest()
    {
        var trees = new List<Entity>();
        var dressed = "";
        (long, long) held = default;
        new PictureRun
        {
            Width = 640,
            Height = 360,
            Frames = 9000,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f, Far = 10_000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 30f, 260f), Vec3.Zero, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-3f, 5f, 2f), Vec3.Zero, Vec3.UnitY));
                trees.AddRange(Trees.PlantForest(ecs, "quaking-aspen", 1, new Vec3(-160f, 0f, 0f), 75f, 334));
                trees.AddRange(Trees.PlantForest(ecs, "black-tupelo", 2, Vec3.Zero, 75f, 333));
                trees.AddRange(Trees.PlantForest(ecs, "california-black-oak", 3, new Vec3(160f, 0f, 0f), 75f, 333));
            },
        }.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
            .Wait(60)
            .Do("read", _ =>
            {
                dressed = ConsoleMemoryCommands.Collected();
                held = Trees.HeldNow();
            })
            .Go();
        return (trees.Count, dressed, held);
    }

    /// <summary>The live managed bytes a line of the memory command says.</summary>
    private static long Live(string said)
    {
        var words = said.Split(' ');
        return long.Parse(words[Array.IndexOf(words, "live") + 1], System.Globalization.CultureInfo.InvariantCulture);
    }
}

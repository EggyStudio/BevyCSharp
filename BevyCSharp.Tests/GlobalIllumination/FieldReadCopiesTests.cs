using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The field's read of one cascade as slangc compiles it to WGSL for the engine, held to copy no
/// table of the cascades into a thread's own memory, which a read of one cascade has no need of,
/// and the kernels held to copy it no more often than their marches do.
/// </summary>
/// <remarks>
/// The sibling engine's first binding of the RadianceCascades module's read held the whole table
/// in the field it handed the module, and the copy each read made of it in a thread's memory was
/// read wrong on macOS, where Vulkan on Linux and Windows took it as it was. A device this suite
/// never runs on would be the first to show such a fault, so the test reads the compiled kernels
/// instead. A march still takes the table, to find the finest cascade a point lies in, and the
/// kernels' copies are those of their marches, counted so a binding that adds one is seen.
/// </remarks>
public sealed class FieldReadCopiesTests
{
    private static readonly string Root = FindRoot();

    // A variable in a function's memory, which WGSL writes with no address space, holding the table
    // of cascades or the field's info that holds it.
    private static readonly Regex ThreadCopy = new(@"\bvar\s+\w+\s*:\s*(?:array<FieldCascade|FieldInfo)");

    // The copies the kernels make of the table, every one a march's, which finds the finest
    // cascade a point lies in through the whole table (fieldCascade), or the table of the probes'
    // cascades that a march builds from the field's (probesOf), the merge's among them, whose
    // marches past a ray's end light the surfaces they meet as the trace does.
    private const int MarchCopies = 24;

    /// <summary>
    /// A kernel that reads the field's distance in one cascade copies no table of the cascades into
    /// a thread's memory.
    /// </summary>
    [SkippableFact]
    public void AReadOfOneCascadeCopiesNoTableOfCascadesIntoAThreadsMemory()
    {
        var slangc = Slangc();
        using var folder = new TestFolder("bevy-field-read-copies-");
        var source = Path.Combine(folder.Path, "read_one.slang");
        File.WriteAllText(source, """
            import bcs_field;

            StructuredBuffer<FieldInfo> info;
            Texture3D<float4> field;
            SamplerState linear;
            RWStructuredBuffer<float> read;

            [shader("compute")]
            [numthreads(8, 1, 1)]
            void main(uint3 id : SV_DispatchThreadID)
            {
                FieldInfo field_info = info[0];
                int c = int(id.x) % max(int(field_info.counts.x), 1);
                read[id.x] = fieldDistanceIn(field, linear, field_info, c, float3(id) * 0.25);
            }
            """);
        var wgsl = Compile(slangc, source, "compute", "main", folder.Path, BridgeModules(folder.Path));
        Assert.Equal(0, ThreadCopies(wgsl));
    }

    /// <summary>
    /// The kernels that read the field copy the table of its cascades no more often than their
    /// marches do.
    /// </summary>
    [SkippableFact]
    public void TheKernelsCopyTheTableOfCascadesNoMoreOftenThanTheirMarchesDo()
    {
        var slangc = Slangc();
        using var folder = new TestFolder("bevy-field-kernel-copies-");
        var modules = BridgeModules(folder.Path);
        var found = new List<string>();
        var copies = 0;
        var kernels = Directory.GetDirectories(Path.Combine(Root, "BevyCSharp"))
            .Select(area => Path.Combine(area, "Kernels"))
            .Where(Directory.Exists)
            .SelectMany(kernels => Directory.GetFiles(kernels, "*.slang"))
            .Order(StringComparer.Ordinal);
        foreach (var file in kernels)
        {
            var text = File.ReadAllText(file);
            // A kernel that traces the GPU's rays through bcs_ray is compiled to SPIR-V, which WGSL
            // has no ray queries for, and reads the field nowhere.
            if (!text.Contains("bcs_field") && !text.Contains("bcs_gi") || text.Contains("import bcs_ray;")) continue;
            foreach (Match entry in Regex.Matches(text, @"\[shader\(""(\w+)""\)\](?:\s*\[[^\]]*\])*\s*\w+\s+(\w+)\s*\("))
            {
                var count = ThreadCopies(Compile(slangc, file, entry.Groups[1].Value, entry.Groups[2].Value, folder.Path, modules));
                copies += count;
                if (count > 0) found.Add($"{Path.GetFileName(file)}'s {entry.Groups[2].Value} {count}");
            }
        }
        Assert.True(copies <= MarchCopies, $"the kernels copy the table of cascades {copies} times, {string.Join(", ", found)}");
    }

    /// <summary>
    /// The copies are counted from the variables of a function's memory alone, of the table or the
    /// info holding it, and not from the buffers a kernel binds.
    /// </summary>
    [Fact]
    public void TheCopiesAreCountedFromAFunctionsVariables()
    {
        const string Wgsl = """
            @binding(0) @group(0) var<storage, read> info_0 : array<FieldInfo_std430_0>;
            fn read_0() -> f32
            {
                var copied_0 : FieldInfo_std430_0 = info_0[i32(0)];
                var table_0 : array<FieldCascade_0, i32(9)> = copied_0.cascades_0.data_0;
                var cell_0 : f32 = table_0[i32(1)].origin_and_cell_0.w;
                return cell_0;
            }
            """;
        Assert.Equal(2, ThreadCopies(Wgsl));
    }

    private static int ThreadCopies(string wgsl) => ThreadCopy.Matches(wgsl).Count;

    // The modules the bridge writes out for import, each file slang.rs lists in MODULES, written
    // into a folder of the scratch one from where it includes it, so a kernel importing a file the
    // bridge leaves out fails here as it does in the engine.
    private static string BridgeModules(string scratch)
    {
        var render = Path.Combine(Root, "native", "bevy_csharp", "src", "render");
        var rust = File.ReadAllText(Path.Combine(render, "slang.rs"));
        var included = Regex.Matches(rust, @"pub const (\w+): &str = include_str!\(""([^""]+)""\);")
            .ToDictionary(found => found.Groups[1].Value, found => found.Groups[2].Value);
        var listed = Regex.Match(rust, @"const MODULES: \[\(&str, &str\); \d+\] = \[(.*?)\];", RegexOptions.Singleline).Groups[1].Value;
        var folder = Directory.CreateDirectory(Path.Combine(scratch, "modules")).FullName;
        foreach (Match module in Regex.Matches(listed, @"\(""([\w.]+)"", (\w+)\)"))
            File.Copy(Path.GetFullPath(Path.Combine(render, included[module.Groups[2].Value])), Path.Combine(folder, module.Groups[1].Value), overwrite: true);
        return folder;
    }

    // A kernel compiled to WGSL as the bridge compiles it, the bridge's modules found first and the
    // kernel's own folder after them.
    private static string Compile(string slangc, string file, string stage, string entry, string scratch, string modules)
    {
        var output = Path.Combine(scratch, $"{Path.GetFileNameWithoutExtension(file)}.{entry}.wgsl");
        var start = new ProcessStartInfo(slangc)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var argument in new[]
        {
            file, "-target", "wgsl", "-stage", stage, "-entry", entry, "-matrix-layout-row-major", "-o", output,
            "-I", modules,
            "-I", Path.GetDirectoryName(file)!,
        })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var said = process.StandardError.ReadToEndAsync();
        process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"slangc failed on {Path.GetFileName(file)}'s {entry}: {said.Result}");
        return File.ReadAllText(output);
    }

    // The compiler the bridge runs, named by BCS_SLANGC or where build/fetch-slang.sh puts it.
    private static string Slangc()
    {
        var named = Environment.GetEnvironmentVariable("BCS_SLANGC");
        var fetched = Path.Combine(Root, "build", "tools", "slang", "bin", OperatingSystem.IsWindows() ? "slangc.exe" : "slangc");
        var slangc = !string.IsNullOrEmpty(named) ? named : fetched;
        Skip.IfNot(File.Exists(slangc), "needs slangc, which build/fetch-slang.sh puts in build/tools/slang");
        return slangc;
    }

    private static string FindRoot()
    {
        for (var at = new DirectoryInfo(AppContext.BaseDirectory); at is not null; at = at.Parent)
            if (File.Exists(Path.Combine(at.FullName, "AGENTS.md")) && Directory.Exists(Path.Combine(at.FullName, ".github")))
                return at.FullName;
        throw new InvalidOperationException("The checkout's root was not found above the test assembly.");
    }
}

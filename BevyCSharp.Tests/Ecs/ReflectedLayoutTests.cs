using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The layout of Bevy's types read from a value of each (<see cref="ReflectedLayouts"/>), held to
/// the layouts the hand mirrors are checked against, and what a byte mirror could not hold said with
/// why.
/// </summary>
/// <remarks>
/// The hand mirrors are checked at startup against the bridge's own exports, which take their
/// offsets with <c>offset_of!</c>. A layout read through reflection has to agree with every one of
/// them before a mirror is generated from it.
/// </remarks>
[Collection("engine")]
public sealed class ReflectedLayoutTests
{
    /// <summary>
    /// Each type mirrored by hand is laid out through reflection as its mirror holds it, a
    /// transform's rotation ahead of its translation, a global transform's four sixteen-byte axes
    /// inside the private affine it wraps, and the three visibility types a byte each.
    /// </summary>
    [SkippableFact]
    public void TheLayoutReadFromAValueIsTheOneTheHandMirrorsHold()
    {
        Needs.Renderer();

        ReflectedLayout? transform = null, global = null, visibility = null, view = null, inherited = null;
        using var harness = new EngineHarness(frames: 1);
        harness.On(Stage.Startup, _ =>
        {
            transform = ReflectedLayouts.Measure("bevy_transform::components::transform::Transform");
            global = ReflectedLayouts.Measure("bevy_transform::components::global_transform::GlobalTransform");
            visibility = ReflectedLayouts.Measure("bevy_camera::visibility::Visibility");
            view = ReflectedLayouts.Measure("bevy_camera::visibility::ViewVisibility");
            inherited = ReflectedLayouts.Measure("bevy_camera::visibility::InheritedVisibility");
        });

        harness.Run();

        Assert.Equal(Transform.NativeSize, transform!.Size);
        Assert.Equal(Transform.RotationOffset, transform.Field(".rotation")!.Value.Offset);
        Assert.Equal(Transform.TranslationOffset, transform.Field(".translation")!.Value.Offset);
        Assert.Equal(Transform.ScaleOffset, transform.Field(".scale")!.Value.Offset);

        Assert.Equal(GlobalTransform.NativeSize, global!.Size);
        Assert.Equal(GlobalTransform.XAxisOffset, global.Field(".0.matrix3.x_axis")!.Value.Offset);
        Assert.Equal(GlobalTransform.YAxisOffset, global.Field(".0.matrix3.y_axis")!.Value.Offset);
        Assert.Equal(GlobalTransform.ZAxisOffset, global.Field(".0.matrix3.z_axis")!.Value.Offset);
        Assert.Equal(GlobalTransform.TranslationOffset, global.Field(".0.translation")!.Value.Offset);

        Assert.Equal(sizeof(VisibilityMode), visibility!.Size);
        Assert.Equal("enum", visibility.Kind);
        Assert.Equal(System.Runtime.CompilerServices.Unsafe.SizeOf<ViewVisibility>(), view!.Size);
        Assert.Equal(0, view.Field(".0")!.Value.Offset);
        Assert.Equal(System.Runtime.CompilerServices.Unsafe.SizeOf<InheritedVisibility>(), inherited!.Size);

        foreach (var layout in new[] { transform, global, visibility, view, inherited })
            Assert.Null(ReflectedLayouts.Unmirrored(layout));
    }

    /// <summary>
    /// A mirror is refused with its reason for a type holding an enum whose variants hold values,
    /// for bytes no reflected field covers between two fields or after the last, and for a field
    /// overlapping another, and allowed where a gap is the padding the next field's alignment needs
    /// or a struct's own, as a vector a SIMD register holds ends four bytes after its last float.
    /// </summary>
    [Fact]
    public void WhatAMirrorCannotHoldIsSaidWithWhy()
    {
        static ReflectedLayout Of(int size, int align, params LayoutField[] fields) => new(size, align, "struct", fields);

        var padded = Of(16, 8, new(".flag", 0, 1, 1, "value"), new(".amount", 8, 8, 8, "value"));
        Assert.Null(ReflectedLayouts.Unmirrored(padded));

        var axes = Of(32, 16,
            new(".axis", 0, 16, 16, "struct"), new(".axis.x", 0, 4, 4, "value"), new(".axis.y", 4, 4, 4, "value"),
            new(".axis.z", 8, 4, 4, "value"), new(".length", 16, 4, 4, "value"));
        Assert.Null(ReflectedLayouts.Unmirrored(axes));

        var colored = Of(20, 4, new LayoutField(".color", 0, 20, 4, "variant"));
        Assert.Contains("'.color'", ReflectedLayouts.Unmirrored(colored));

        var hidden = Of(16, 4, new(".first", 0, 4, 4, "value"), new(".last", 12, 4, 4, "value"));
        Assert.Contains("bytes 4 to 12", ReflectedLayouts.Unmirrored(hidden));

        var trailing = Of(16, 4, new LayoutField(".first", 0, 4, 4, "value"));
        Assert.Contains("bytes 4 to 16", ReflectedLayouts.Unmirrored(trailing));

        var overlapping = Of(8, 4, new(".first", 0, 8, 4, "value"), new(".second", 4, 4, 4, "value"));
        Assert.Contains("'.second' overlaps", ReflectedLayouts.Unmirrored(overlapping));
    }

    /// <summary>
    /// Every layout the description holds is the one the running bridge reads, so a Bevy upgrade
    /// that moves a field fails here, with each component that moved named, before a mirror reads a
    /// game's bytes from where the field was.
    /// </summary>
    [SkippableFact]
    public void EveryLayoutTheDescriptionHoldsIsTheOneTheBridgeReads()
    {
        Needs.Renderer();

        var dumped = File.ReadLines(Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "Generated", "bevy-components.tsv"))
            .Select(line => line.Split('\t'))
            .Where(words => words is ["layout", _, _])
            .ToDictionary(words => words[1], words => words[2], StringComparer.Ordinal);
        var moved = new List<string>();

        using var harness = new EngineHarness(frames: 1);
        harness.On(Stage.Startup, _ =>
        {
            foreach (var (type, layout) in dumped)
            {
                var read = ReflectedLayouts.Measure(type) is { } measured
                    ? ReflectedLayouts.Signature(measured, ReflectedLayouts.PathsOf(layout))
                    : null;
                if (read != layout) moved.Add($"{type}: described {layout}, read {read}");
            }
        });

        harness.Run();

        Assert.True(dumped.Count > 100, $"the description holds {dumped.Count} layouts");
        Assert.True(moved.Count == 0, "Layouts the bridge reads otherwise than the description:\n" + string.Join('\n', moved));
    }

    /// <summary>
    /// A generated mirror reads and writes its component in place, a sharpening pass's strength and
    /// flags where its wrapper reads them and an antialiasing pass's sensitivity as the variant the
    /// wrapper reads.
    /// </summary>
    [SkippableFact]
    public void AGeneratedMirrorReadsAndWritesItsComponentInPlace()
    {
        Needs.Renderer();

        float read = 0f, strength = 0f;
        bool sharpened = false, denoised = true;
        string? threshold = null, written = null;
        bool enabled = false;

        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            var sharpening = ctx.Ecs.Insert<ContrastAdaptiveSharpeningRef>(entity);
            sharpening.SharpeningStrength = 0.25f;
            ref var bytes = ref ctx.Ecs.GetRef<ContrastAdaptiveSharpeningMirror>(entity);
            (read, sharpened, denoised) = (bytes.SharpeningStrength, bytes.Enabled, bytes.Denoise);
            bytes.SharpeningStrength = 0.75f;
            strength = sharpening.SharpeningStrength;

            var fxaa = ctx.Ecs.Insert<FxaaRef>(entity);
            ref var mirror = ref ctx.Ecs.GetRef<FxaaMirror>(entity);
            (enabled, threshold) = (mirror.Enabled, mirror.EdgeThreshold.ToString());
            mirror.EdgeThreshold = FxaaMirror.Sensitivity.Ultra;
            written = fxaa.EdgeThreshold.ToString();
        });

        harness.Run();

        Assert.Equal(0.25f, read);
        Assert.Equal(0.75f, strength);
        Assert.True(sharpened);
        Assert.False(denoised);
        Assert.True(enabled);
        Assert.Equal("High", threshold);
        Assert.Equal("Ultra", written);
    }

    /// <summary>
    /// A mirror whose layout is not the one the bridge reads is refused the first time its id is
    /// asked for, naming the layout read and the one it holds, rather than reading one field's bytes
    /// as another's.
    /// </summary>
    [SkippableFact]
    public void AMirrorWhoseLayoutMovedIsRefusedAtFirstUse()
    {
        Needs.Renderer();

        BevyNativeException? refused = null;
        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.Identity);
            refused = Assert.Throws<BevyNativeException>(() => ctx.Ecs.Has<MovedTransform>(entity));
        });

        harness.Run();

        Assert.Contains("mirrors it as 48/16;.translation@0:12", refused!.Message);
        Assert.Contains("48/16;.translation@16:12", refused.Message);
    }

    /// <summary>
    /// Each of the library's own mirrors carries the layout the description holds for its component,
    /// and declares its fields at the offsets and its size that layout gives, and the visibility its
    /// numbers, so the type a game uses and the description cannot drift apart.
    /// </summary>
    [Fact]
    public void EachOfTheLibrarysMirrorsHoldsItsComponentWhereTheDescriptionSays()
    {
        var described = File.ReadLines(Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "Generated", "bevy-components.tsv"))
            .Select(line => line.Split('\t'))
            .Where(words => words is ["layout", _, _])
            .ToDictionary(words => words[1], words => words[2], StringComparer.Ordinal);

        Hold<Transform>(("Rotation", ".rotation"), ("Translation", ".translation"), ("Scale", ".scale"));
        Hold<GlobalTransform>(
            ("XAxis", ".0.matrix3.x_axis"), ("YAxis", ".0.matrix3.y_axis"), ("ZAxis", ".0.matrix3.z_axis"), ("Translation", ".0.translation"));
        Hold<Visibility>(("Mode", ""));
        Hold<InheritedVisibility>(("Value", ".0"));
        Hold<ViewVisibility>(("Bits", ".0"));

        var visibility = Placed(described["bevy_camera::visibility::Visibility"])[""];
        Assert.Equal(
            Enum.GetValues<VisibilityMode>().ToDictionary(mode => mode.ToString(), mode => (ulong)mode),
            visibility.Variants);

        void Hold<T>(params (string Field, string Path)[] fields) where T : unmanaged, INativeComponent
        {
            var mirror = (INativeComponent)default(T);
            var layout = ((IMirroredLayout)mirror).Layout;
            Assert.Equal(described[mirror.NativeName], layout);

            var placed = Placed(layout);
            Assert.Equal(int.Parse(layout.Split('/')[0], System.Globalization.CultureInfo.InvariantCulture), System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
            foreach (var (field, path) in fields)
                Assert.True(placed[path].Offset == (int)Marshal.OffsetOf<T>(field), $"{typeof(T).Name}.{field} is at {Marshal.OffsetOf<T>(field)}, where the description puts '{path}' at {placed[path].Offset}");
        }
    }

    /// <summary>Each path of a layout signature, its offset and the numbers of its variants.</summary>
    private static Dictionary<string, (int Offset, Dictionary<string, ulong> Variants)> Placed(string layout) =>
        layout.Split(';').Skip(1).ToDictionary(
            entry => entry[..entry.IndexOf('@')],
            entry =>
            {
                var colon = entry.IndexOf(':');
                var brace = entry.IndexOf('{');
                var variants = brace < 0
                    ? new Dictionary<string, ulong>()
                    : entry[(brace + 1)..^1].Split(',').Select(pair => pair.Split('='))
                        .ToDictionary(pair => pair[0], pair => ulong.Parse(pair[1], System.Globalization.CultureInfo.InvariantCulture));
                return (int.Parse(entry[(entry.IndexOf('@') + 1)..colon], System.Globalization.CultureInfo.InvariantCulture), variants);
            },
            StringComparer.Ordinal);

    /// <summary>Bevy's transform as it would be had its translation moved to its start.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    private struct MovedTransform : INativeComponent, IMirroredLayout
    {
        [FieldOffset(0)] public Vec3 Translation;

        readonly string INativeComponent.NativeName => "bevy_transform::components::transform::Transform";

        readonly string IMirroredLayout.Layout => "48/16;.translation@0:12";
    }

    /// <summary>
    /// <c>schema.layouts</c> counts the components of fixed-size values a mirror could hold among
    /// those there are, the ones mirrored by hand among the counted, and names each left out.
    /// </summary>
    [SkippableFact]
    public void TheComponentsAMirrorCouldHoldAreCounted()
    {
        Needs.Renderer();

        string? said = null;
        using var harness = new EngineHarness(frames: 1);
        harness.On(Stage.Startup, world =>
        {
            using (ConsoleHost.Lend(world)) said = ConsoleCommands.Run("schema.layouts");
        });

        harness.Run();

        Assert.NotNull(said);
        Assert.Matches(@"^\d+ of \d+ components of fixed-size values can be mirrored", said);
        foreach (var mirrored in new[] { "Transform:", "GlobalTransform:", "Visibility:", "ViewVisibility:", "InheritedVisibility:" })
            Assert.DoesNotContain("\n" + mirrored, said);
    }
}

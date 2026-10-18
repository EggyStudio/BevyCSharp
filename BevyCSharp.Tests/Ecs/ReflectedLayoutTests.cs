using Bevy;
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

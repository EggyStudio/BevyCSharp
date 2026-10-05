using Bevy;
using Bevy.Interop;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's widgets, put on nodes through their wrappers.</summary>
/// <remarks>
/// A widget changes as the pointer works it, through Bevy's picking, which an offscreen run cannot
/// send a pointer to, so what is covered here is a widget built and set from C#.
/// </remarks>
[Collection("engine")]
public sealed class WidgetTests
{
    /// <summary>
    /// A slider, a checkbox and a radio group are made from their wrappers and made to keep their
    /// own state, the slider's value is written and read through its wrapper, and an entity that is
    /// gone is refused.
    /// </summary>
    [SkippableFact]
    public void WidgetsAreBuiltSetAndMadeToKeepTheirOwnState()
    {
        Needs.Renderer();

        float? value = null;
        bool checkedRead = false;
        BevyNativeException? refused = null;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render2d.SpawnCamera2d();

                var slider = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Height = Length.Px(12f) });
                ecs.Insert<SliderRef>(slider);
                var range = ecs.Insert<SliderRangeRef>(slider);
                (range.Start, range.End) = (0f, 10f);
                ecs.Insert<SliderValueRef>(slider).Value = 7.5f;
                Ui.SelfUpdate(slider, UiWidgetKind.Slider);
                value = ecs.Get<SliderValueRef>(slider)?.Value;

                var checkbox = Ui.SpawnNode(new UiSettings());
                ecs.Insert<CheckboxRef>(checkbox);
                ecs.Insert<CheckedRef>(checkbox);
                Ui.SelfUpdate(checkbox, UiWidgetKind.Checkbox);
                checkedRead = ecs.Get<CheckedRef>(checkbox) is not null;

                var group = Ui.SpawnNode(new UiSettings());
                ecs.Insert<RadioGroupRef>(group);
                Ui.SelfUpdate(group, UiWidgetKind.RadioGroup);

                var gone = ecs.Spawn();
                ecs.Despawn(gone);
                refused = Assert.Throws<BevyNativeException>(() => Ui.SelfUpdate(gone, UiWidgetKind.Slider));
            },
        };

        run.Wait(3).Go();

        Assert.Equal(7.5f, value);
        Assert.True(checkedRead);
        Assert.Equal(NativeStatus.NoEntity, refused!.Status);
    }
}

// Bevy's editable_text_filter example, examples/ui/text/editable_text_filter.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows a text field that takes hex digits only, eight of them at most, focused as it opens.
//
// Bevy's filter is a function of the example's own, which keeps the hex digits. A field here names
// the characters it takes instead, which are the same ones.
internal static class EditableTextFilter
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
        var field = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(240f),
            Border = Sides.All(Length.Px(2f)),
            Padding = Sides.All(Length.Px(8f)),
            Color = Color.FromSrgb8(47, 79, 79),
            BorderColor = Color.FromSrgb8(203, 213, 225),
        });
        Ui.SetEditableText(field, new UiEditableTextSettings { MaxCharacters = 8, Allowed = "0123456789abcdefABCDEF" });
        ecs.Wrap<TextFontRef>(field).FontSize = new FontSize.Px(32f);
        ecs.Insert<AutoFocusRef>(field);
        ecs.SetParent(field, middle);
    }, "editable_text_filter.Setup");
}

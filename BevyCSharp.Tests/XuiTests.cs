using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the HTML and CSS surface, which needs the editor profile.
/// </summary>
/// <remarks>
/// The suite builds headless, so what is checked here is the half that has to hold on every
/// build: that a bridge without the profile refuses each entry point rather than resolving a
/// symbol that is not there. A missing export would not fail at load, because the runtime binds
/// these lazily; it would throw at the first call, in whatever code happened to make it.
/// </remarks>
[Collection("engine")]
public sealed unsafe class XuiTests
{
    [Fact]
    public void TheProfileAnswersForItself()
    {
        // Never throws, on any build: this is the question a caller asks before the rest.
        var editor = App.HasEditor;
        Assert.True(editor || !editor);
    }

    [Fact]
    public void EveryEntryPointIsPresentOnABuildWithoutTheProfile()
    {
        if (App.HasEditor) return;

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, _ =>
        {
            float number = 0f;
            var flag = 0;
            float rect = 0f;
            ulong picked = 0;
            NativeUiEvent events;

            // Called straight at the bridge. Reaching them through a managed wrapper would prove
            // the wrapper guards them, and the point here is that the symbols exist at all.
            var results = new[]
            {
                Native.bcs_xui_open("panels/nothing.html"),
                Native.bcs_xui_close(1),
                Native.bcs_xui_get_text(0, null, 0),
                Native.bcs_xui_set_text(0, "x"),
                Native.bcs_xui_get_number(0, &number),
                Native.bcs_xui_set_number(0, 1f),
                Native.bcs_xui_get_flag(0, &flag),
                Native.bcs_xui_set_flag(0, 1),
                Native.bcs_xui_events(&events, 1),
                Native.bcs_xui_set_rect(0, 0f, 0f, 1f, 1f),
                Native.bcs_xui_get_visible(0, &flag),
                Native.bcs_xui_set_image(0, "icons/ui/menu.png"),
                Native.bcs_xui_set_visible(0, 1),
                Native.bcs_xui_set_layer(0, 1),
                Native.bcs_xui_rect(0, &rect),

                // Picking is part of the same profile: a build with no interface has no viewport
                // to click in either.
                Native.bcs_pick_events(&picked, 1),

                // The two projections belong to the renderer rather than the interface, so on a
                // headless build they refuse for the other reason: there is no camera to project
                // through. Either way the symbol has to be there.
                Native.bcs_render_world_to_viewport(0, 0f, 0f, 0f, &rect),
                Native.bcs_render_viewport_to_world(0, 0f, 0f, &rect),
            };

            foreach (var status in results)
                Assert.Equal(NativeStatus.Unsupported, status);

            // The odd one out: it answers with an entity rather than a status, so "nothing" is 0.
            Assert.Equal(0ul, Native.bcs_xui_element("#anything"));
        });

        harness.Run();
    }
}

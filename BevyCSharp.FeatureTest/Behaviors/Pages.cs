using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The panel's pages, built each time one is opened so each row reads what is now.
/// </summary>
internal static class Pages
{
    private static FeatureSettings S => Settings.Current;

    /// <summary>The first page, of the others.</summary>
    public static Page First() => new("Feature test",
    [
        new PageRow("Player", Player),
        new PageRow("Graphics", Graphics),
        new PageRow("Effects", Effects),
        new PageRow("Audio", Audio),
        new PageRow("Controls", Controls),
        new PageRow("Debug", Debug),
        new PageRow("Teleport", Teleport),
        new PageRow("Spawn", Spawn),
        new PageRow("Time", Time),
        new ActionRow("Console", _ => ImGuiConsole.IsOpen = true),
        new ActionRow("Quit", ctx => ctx.Exit()),
    ]);

    private static Page Player() => new("Player",
    [
        new ChoiceRow<PlayerMode>("Mode (F3 and F4)", () => Behaviors.Player.Mode, Behaviors.Player.Ask),
        new ChoiceRow<PlayerView>("View (F5)", () => Behaviors.Player.View, value => Behaviors.Player.View = value),
        new ActionRow("Back to the zone's start", ctx => Behaviors.Player.Put(ctx, Behaviors.Player.Respawn, "put back")),
    ]);

    private static Page Graphics() => new("Graphics",
    [
        new ChoiceRow<WindowMode>("Window", () => S.Window, value => Settings.Change(s => s with { Window = value })),
        new ToggleRow("Vsync, from the next start", () => S.Vsync, value => Settings.Change(s => s with { Vsync = value })),
        new ChoiceRow<Quality>("Quality", () => S.Quality, value => Settings.Change(s => s.At(value))),
        new ChoiceRow<Smoothing>("Anti-aliasing", () => S.Smoothing, value => Settings.Change(s => s with { Smoothing = value })),
        new ToggleRow("Shadows", () => S.Shadows, value => Settings.Change(s => s with { Shadows = value })),
        new ToggleRow("Bloom", () => S.Bloom, value => Settings.Change(s => s with { Bloom = value })),
        new ToggleRow(Render.RayTracingActive ? "Ray-traced lighting, running" : "Ray-traced lighting, from the next start",
            () => S.RayTraced, value => Settings.Change(s => s with { RayTraced = value })),
        new ToggleRow("Old screen", () => ShaderShowcase.OldScreen, value => ShaderShowcase.OldScreen = value),
        new ToggleRow("Screen-space light", () => ScreenSpaceLight.On, value => ScreenSpaceLight.On = value),
        new ToggleRow("Ray-traced occlusion", () => RayTracedOcclusion.On, value => RayTracedOcclusion.On = value),
    ]);

    private static Page Effects() => new("Effects",
    [
        new ChoiceRow<Tonemapper>("Tonemapper", () => S.Tonemapper, value => Settings.Change(s => s with { Tonemapper = value })),
        new ToggleRow("Ambient occlusion", () => S.AmbientOcclusion, value => Settings.Change(s => s with { AmbientOcclusion = value })),
        new ToggleRow("Screen-space reflections", () => S.Reflections, value => Settings.Change(s => s with { Reflections = value })),
        new ChoiceRow<DepthOfFieldMode>("Depth of field", () => S.DepthOfField, value => Settings.Change(s => s with { DepthOfField = value })),
        new ToggleRow("Motion blur", () => S.MotionBlur, value => Settings.Change(s => s with { MotionBlur = value })),
        new ToggleRow("Chromatic aberration", () => S.Aberration, value => Settings.Change(s => s with { Aberration = value })),
        new ToggleRow("Vignette", () => S.Vignette, value => Settings.Change(s => s with { Vignette = value })),
        new ToggleRow("Auto exposure", () => S.AutoExposure, value => Settings.Change(s => s with { AutoExposure = value })),
        new SliderRow("Sharpening", () => S.Sharpen, value => Settings.Change(s => s with { Sharpen = value }), 0f, 1f, 0.1f),
        new ChoiceRow<Backdrop>("Sky", () => S.Backdrop, value => Settings.Change(s => s with { Backdrop = value })),
    ]);

    private static Page Audio() => new("Audio",
    [
        new SliderRow("Master", () => S.Master, value => Settings.Change(s => s with { Master = value }), 0f, 1f, 0.1f),
        new SliderRow("Music", () => S.Music, value => Settings.Change(s => s with { Music = value }), 0f, 1f, 0.1f),
        new SliderRow("Effects", () => S.Effects, value => Settings.Change(s => s with { Effects = value }), 0f, 1f, 0.1f),
        new ActionRow("Play a chime", Sound.Chime),
    ]);

    private static Page Controls() => new("Controls",
    [
        new SliderRow("Look speed", () => S.LookSpeed, value => Settings.Change(s => s with { LookSpeed = value }), 0.25f, 3f, 0.25f),
        new ToggleRow("Invert Y", () => S.InvertY, value => Settings.Change(s => s with { InvertY = value })),
    ]);

    private static Page Debug() => new("Debug",
    [
        new ToggleRow("Overlay (F3)", () => S.Overlay, value => Settings.Change(s => s with { Overlay = value })),
        new ToggleRow("Colliders", () => S.Colliders, value => Settings.Change(s => s with { Colliders = value })),
        new ToggleRow("Gizmos", () => S.Gizmos, value => Settings.Change(s => s with { Gizmos = value })),
        new ToggleRow("Wireframe", () => S.Wireframe, value => Settings.Change(s => s with { Wireframe = value })),
    ]);

    private static Page Teleport() => new("Teleport",
        [.. Zones.All.Select(zone => (Row)new ActionRow(zone.Name, ctx => Zones.Go(ctx, zone)))]);

    private static Page Spawn() => new("Spawn",
    [
        new ActionRow("Five crates", ctx => Console.WriteLine($"[Spawn] {Crates.Drop(ctx, 5)}")),
        new ActionRow("Five balls", ctx => Console.WriteLine($"[Spawn] {Crates.Drop(ctx, 5, balls: true)}")),
        new ActionRow("Take them away", ctx => Console.WriteLine($"[Spawn] {Crates.Clear(ctx)}")),
    ]);

    private static Page Time() => new("Time",
    [
        new SliderRow("Time scale", () => S.TimeScale, value => Settings.Change(s => s with { TimeScale = value }), 0f, 4f, 0.25f),
    ]);
}

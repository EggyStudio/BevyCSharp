using System.Globalization;
using System.Numerics;
using Bevy;
using BevyCSharp.Editor.Behaviors;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The editor's own view of the scene, and how it looks and moves: its lens, how it flies, and
/// what it draws the picture with, in a card at the scene's top right.
/// </summary>
/// <remarks>
/// <para>
/// The scene view's camera is the editor's, not the game's, so what it looks like is a working
/// preference rather than part of the scene, and it is set here rather than on a component in the
/// world list, where it is not listed. Unity's scene view keeps the same things behind a button in
/// its toolbar: the field of view and the clip planes, how fast the camera flies and how it eases,
/// and whether the view shows the scene's effects.
/// </para>
/// <para>
/// The look is the editor's own plain one unless it is overridden, since a view to work in wants
/// to show surfaces as they are rather than through a grade. Overridden, it takes a tonemapper,
/// bloom, an antialiasing pass and either a fixed exposure or one that follows the light, so a
/// scene can be judged under the look it will ship with. Everything is saved with the editor's
/// preferences, on a page of its own in the settings.
/// </para>
/// </remarks>
public static class SceneCamera
{
    /// <summary>Whether the card is up.</summary>
    public static bool Showing { get; set; }

    /// <summary>Degrees across the picture's height.</summary>
    public static float FieldOfView { get; set; } = 50f;

    /// <summary>The nearest distance drawn, in meters.</summary>
    public static float Near { get; set; } = 0.1f;

    /// <summary>The furthest distance drawn, in meters.</summary>
    public static float Far { get; set; } = 1000f;

    /// <summary>Whether the look below is used rather than the editor's plain one.</summary>
    public static bool Override { get; set; }

    /// <summary>The curve from what is rendered to what is shown, while overridden.</summary>
    public static Tonemapper Tonemapper { get; set; } = Tonemapper.TonyMcMapface;

    /// <summary>Whether bright light spills round what is brighter than white, while overridden.</summary>
    public static bool Bloom { get; set; }

    /// <summary>How strongly, while it does.</summary>
    public static float BloomIntensity { get; set; } = 0.15f;

    /// <summary>A pass over the finished picture that smooths its edges, while overridden.</summary>
    public static AntiAliasPass AntiAlias { get; set; } = AntiAliasPass.None;

    /// <summary>Whether the exposure follows the light in view, while overridden.</summary>
    public static bool AutoExposure { get; set; }

    /// <summary>Whether a fixed exposure of its own is used, while overridden and not following the light.</summary>
    public static bool FixedExposure { get; set; }

    /// <summary>That exposure, in EV100.</summary>
    public static float Exposure { get; set; } = DefaultExposure;

    /// <summary>Bevy's own exposure, which the plain look uses.</summary>
    private const float DefaultExposure = 9.7f;

    /// <summary>What was last given to the camera, so it is only told again on a change.</summary>
    private static string _told = string.Empty;

    /// <summary>Puts the settings on the scene's camera, when any of them changed.</summary>
    internal static void Apply(BehaviorContext ctx, Entity camera)
    {
        if (camera.IsNone || !App.HasRenderer) return;

        var said = string.Create(
            CultureInfo.InvariantCulture,
            $"{camera.Bits}|{FieldOfView}|{Near}|{Far}|{Override}|{Tonemapper}|{Bloom}|{BloomIntensity}|{AntiAlias}|{AutoExposure}|{FixedExposure}|{Exposure}");

        if (said == _told) return;

        _told = said;

        Render.SetPerspective(camera, Math.Clamp(FieldOfView, 5f, 170f), MathF.Max(0.001f, Near), MathF.Max(Near + 0.01f, Far));

        // The plain look is the editor's: a high-range picture, one sample a pixel, the default
        // curve, and nothing else. The one the scene camera was made with.
        var settings = new PostSettings { Hdr = true, Msaa = 1 };

        if (Override)
        {
            settings.Tonemapper = Tonemapper;
            settings.Bloom = Bloom;
            settings.BloomIntensity = BloomIntensity;
            settings.AntiAlias = AntiAlias;
        }

        Render.SetPostProcessing(camera, settings);

        // Following the light is one of the camera's effects rather than its post-processing, and
        // the scene camera has no other effect to keep.
        Render.SetEffects(camera, new EffectSettings { AutoExposure = Override && AutoExposure });

        Render.SetExposure(camera, Override && FixedExposure && !AutoExposure ? Exposure : DefaultExposure);
    }

    /// <summary>
    /// Registers everything on a page of the settings, which is how it is saved with the rest of
    /// the editor's preferences and read back when the editor opens.
    /// </summary>
    internal static void Register()
    {
        const string Page = "Scene camera";

        EditorSettings.Heading(Page, "Lens", 0);
        EditorSettings.Number(Page, "Field of view", static () => FieldOfView, static v => FieldOfView = Math.Clamp(v, 5f, 170f), 1);
        EditorSettings.Number(Page, "Near", static () => Near, static v => Near = MathF.Max(0.001f, v), 2);
        EditorSettings.Number(Page, "Far", static () => Far, static v => Far = MathF.Max(1f, v), 3);

        EditorSettings.Heading(Page, "Flying", 4);
        EditorSettings.Number(Page, "Easing", static () => FlyCamera.Easing, static v => FlyCamera.Easing = Math.Clamp(v, 0f, 1f), 5);
        EditorSettings.Number(Page, "Look sensitivity", static () => FlyCamera.LookScale, static v => FlyCamera.LookScale = Math.Clamp(v, 0.1f, 5f), 6);

        EditorSettings.Heading(Page, "Look", 7);
        EditorSettings.Flag(Page, "Override", static () => Override, static on => Override = on, 8);
        EditorSettings.Choice(Page, "Tonemapper", Enum.GetNames<Tonemapper>(), static () => Tonemapper.ToString(), static chosen =>
        {
            if (Enum.TryParse<Tonemapper>(chosen, out var tonemapper)) Tonemapper = tonemapper;
        }, 9);
        EditorSettings.Flag(Page, "Bloom", static () => Bloom, static on => Bloom = on, 10);
        EditorSettings.Number(Page, "Bloom intensity", static () => BloomIntensity, static v => BloomIntensity = Math.Clamp(v, 0f, 1f), 11);
        EditorSettings.Choice(Page, "Antialiasing", Enum.GetNames<AntiAliasPass>(), static () => AntiAlias.ToString(), static chosen =>
        {
            if (Enum.TryParse<AntiAliasPass>(chosen, out var pass)) AntiAlias = pass;
        }, 12);
        EditorSettings.Flag(Page, "Auto exposure", static () => AutoExposure, static on => AutoExposure = on, 13);
        EditorSettings.Flag(Page, "Fixed exposure", static () => FixedExposure, static on => FixedExposure = on, 14);
        EditorSettings.Number(Page, "Exposure", static () => Exposure, static v => Exposure = Math.Clamp(v, -8f, 20f), 15);
    }

    /// <summary>The card under the buttons at the scene's top right, while it is up.</summary>
    internal static void Draw(BehaviorContext ctx)
    {
        if (!Showing) return;

        var camera = EditorSelection.Camera;

        var at = new Vector2(
            EditorShell.Free.Right - ToolbarView.Inset,
            ToolbarView.Origin.Y + ToolbarView.Inset + ((ToolbarView.RightRow + 1) * (EditorSurface.Tall + EditorSurface.Air)));

        ImGui.SetNextWindowPos(new Vector2(MathF.Round(at.X), MathF.Round(at.Y)), ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(300f, 0f),
            new Vector2(300f, MathF.Max(120f, EditorShell.Free.Bottom - at.Y - ToolbarView.Inset)));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, EditorTheme.LivePanel);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, EditorTheme.Current.ChildRounding);

        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##sceneCamera", flags))
        {
            EditorSurface.Heading("Lens");

            var fov = FieldOfView;
            if (Slider("Field of view", ref fov, 5f, 170f, "%.0f°")) FieldOfView = fov;

            var near = Near;
            if (Number("Near", ref near, 0.001f, 10f, 0.01f, "%.3f m")) Near = near;

            var far = Far;
            if (Number("Far", ref far, 1f, 100000f, 10f, "%.0f m")) Far = far;

            EditorSurface.Heading("Flying");

            if (!camera.IsNone && ctx.Ecs.TryGet<FlyCamera>(camera, out var fly))
            {
                var speed = fly.Speed;

                if (Number("Speed", ref speed, 0.05f, 500f, 0.1f, "%.2f m/s"))
                {
                    ctx.Ecs.GetRef<FlyCamera>(camera).Speed = speed;
                }
            }

            var easing = FlyCamera.Easing;
            if (Slider("Easing", ref easing, 0f, 0.5f, "%.2f s")) FlyCamera.Easing = easing;

            var look = FlyCamera.LookScale;
            if (Slider("Look", ref look, 0.1f, 3f, "%.2fx")) FlyCamera.LookScale = look;

            EditorSurface.Heading("Look");

            var over = Override;
            if (Tick("Override", ref over)) Override = over;

            if (!Override) ImGui.BeginDisabled();

            Choose("Tonemapper", Tonemapper.ToString(), Enum.GetNames<Tonemapper>(), chosen =>
            {
                if (Enum.TryParse<Tonemapper>(chosen, out var tonemapper)) Tonemapper = tonemapper;
            });

            var bloom = Bloom;
            if (Tick("Bloom", ref bloom)) Bloom = bloom;

            if (Bloom)
            {
                var intensity = BloomIntensity;
                if (Slider("Intensity", ref intensity, 0f, 1f, "%.2f")) BloomIntensity = intensity;
            }

            Choose("Antialiasing", AntiAlias.ToString(), Enum.GetNames<AntiAliasPass>(), chosen =>
            {
                if (Enum.TryParse<AntiAliasPass>(chosen, out var pass)) AntiAlias = pass;
            });

            var auto = AutoExposure;
            if (Tick("Auto exposure", ref auto)) AutoExposure = auto;

            if (!AutoExposure)
            {
                var fixedOne = FixedExposure;
                if (Tick("Fixed EV", ref fixedOne)) FixedExposure = fixedOne;

                if (FixedExposure)
                {
                    var ev = Exposure;
                    if (Slider("EV100", ref ev, -2f, 18f, "%.1f")) Exposure = ev;
                }
            }

            if (!Override) ImGui.EndDisabled();

            ImGui.Spacing();

            if (ImGui.Button("Reset", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()))) Reset(ctx, camera);
        }

        ImGui.End();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    /// <summary>Everything back to how the editor comes.</summary>
    private static void Reset(BehaviorContext ctx, Entity camera)
    {
        FieldOfView = 50f;
        Near = 0.1f;
        Far = 1000f;
        FlyCamera.Easing = 0.08f;
        FlyCamera.LookScale = 1f;
        Override = false;
        Tonemapper = Tonemapper.TonyMcMapface;
        Bloom = false;
        BloomIntensity = 0.15f;
        AntiAlias = AntiAliasPass.None;
        AutoExposure = false;
        FixedExposure = false;
        Exposure = DefaultExposure;

        if (!camera.IsNone && ctx.Ecs.TryGet<FlyCamera>(camera, out _)) ctx.Ecs.GetRef<FlyCamera>(camera).Speed = 6f;
    }

    /// <summary>A name and a slider, in the rows every panel uses.</summary>
    private static bool Slider(string name, ref float value, float low, float high, string format)
    {
        var changed = false;

        if (EditorRows.Open($"##{name}"))
        {
            EditorRows.Line(name);

            var held = value;

            changed = EditorWidgets.Sliding(
                $"##{name}s",
                high > low ? (held - low) / (high - low) : 0f,
                () => ImGui.SliderFloat($"##{name}s", ref held, low, high, format));

            value = held;
            EditorRows.Close();
        }

        return changed;
    }

    /// <summary>A name and a number to drag or type.</summary>
    private static bool Number(string name, ref float value, float low, float high, float speed, string format)
    {
        var changed = false;

        if (EditorRows.Open($"##{name}"))
        {
            EditorRows.Line(name);
            changed = ImGui.DragFloat($"##{name}n", ref value, speed, low, high, format, ImGuiSliderFlags.AlwaysClamp);
            EditorRows.Close();
        }

        return changed;
    }

    /// <summary>A name and a box to tick.</summary>
    private static bool Tick(string name, ref bool on)
    {
        var changed = false;

        if (EditorRows.Open($"##{name}"))
        {
            EditorRows.Line(name);
            changed = EditorWidgets.Ticked($"##{name}t", ref on);
            EditorRows.Close();
        }

        return changed;
    }

    /// <summary>A name and a list to choose from.</summary>
    private static void Choose(string name, string held, IReadOnlyList<string> options, Action<string> onto)
    {
        if (!EditorRows.Open($"##{name}")) return;

        EditorRows.Line(name);
        EditorWidgets.Choice($"##{name}c", held, options, onto);
        EditorRows.Close();
    }
}

using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The bridge's denoiser over the ray-traced lighting, from the panel's graphics page or the
/// <c>feature.denoiser</c> command, on wherever the lighting runs and turned off for a picture of
/// the rays' own grain beside the filtered one.
/// </summary>
/// <remarks>
/// <c>build/denoiser-readings.py</c> takes its readings with it, the mean of many grainy frames of
/// a view standing still against one frame filtered and one not.
/// </remarks>
[Behavior]
public partial struct Denoiser
{
    /// <summary>Whether the denoiser filters the picture, never where rays are not traced.</summary>
    internal static bool On => Render.RayTracingActive && Render.RayTracedDenoiserActive;

    /// <summary>Turns the denoiser on or off, where rays are traced, and says what it came to.</summary>
    internal static string Set(bool on)
    {
        if (!Render.RayTracingActive) return "ray-traced lighting is not running, so there is no denoiser";

        Render.SetRayTracedDenoiser(on);
        return on ? "the denoiser filters the ray-traced lighting" : "the denoiser is off, the rays' grain left in the picture";
    }

    [Command("feature.denoiser", "The denoiser over the ray-traced lighting, or the rays' own grain: feature.denoiser <on|off>")]
    internal static string Command(string state) => Set(state is "on" or "1" or "true");
}

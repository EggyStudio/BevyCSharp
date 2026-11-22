using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Marks the planet whose air a sky is scattered through, Bevy's <c>Atmosphere</c>.
/// </summary>
/// <remarks>
/// A name-only handle, so <c>Count&lt;PlanetAtmosphere&gt;()</c> and <c>[With]</c> filters can
/// find the planet <c>Render.SetAtmosphere</c> keeps. The component holds radii and an asset
/// handle, which raw bytes cannot represent, so its settings go through that call rather than
/// through a component write.
/// </remarks>
public readonly struct PlanetAtmosphere : INativeComponent
{
    readonly string INativeComponent.NativeName => "Atmosphere";
    readonly bool INativeComponent.MirrorsLayout => false;
}

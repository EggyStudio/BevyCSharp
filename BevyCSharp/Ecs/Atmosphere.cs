using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Marks the planet whose air a sky is scattered through.
/// </summary>
/// <remarks>
/// A name-only handle, so <c>Count&lt;Atmosphere&gt;()</c> and <c>[With]</c> filters can find the
/// planet <see cref="Render.SetAtmosphere"/> keeps. The component holds radii and an asset
/// handle, which raw bytes cannot represent, so its settings go through that call rather than
/// through a component write.
/// </remarks>
public readonly struct Atmosphere : INativeComponent
{
    readonly string INativeComponent.NativeName => "Atmosphere";
    readonly bool INativeComponent.MirrorsLayout => false;
}

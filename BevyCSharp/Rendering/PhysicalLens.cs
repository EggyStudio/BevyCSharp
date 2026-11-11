namespace Bevy;

/// <summary>
/// A camera's lens, written down once, which its exposure and its depth of field both read.
/// </summary>
/// <remarks>
/// A real lens is three numbers for how much light it lets in and one for the size of what the
/// light lands on, and the same aperture that brightens a picture shortens what is in focus. Set
/// apart, the exposure and the blur drift into describing two different lenses.
/// <see cref="Render.SetLens"/> meters a camera from this, and <see cref="EffectSettings.Through"/>
/// gives the depth of field the same aperture and sensor. Bevy's <c>PhysicalCameraParameters</c>
/// is the same idea.
/// </remarks>
/// <param name="Aperture">The f-stop. A larger number lets less light in and keeps more in focus.</param>
/// <param name="Shutter">How long the shutter is open, in seconds.</param>
/// <param name="Sensitivity">The ISO.</param>
/// <param name="SensorHeight">
/// How tall the sensor is, in meters, which with the field of view fixes the focal length. Bevy's
/// own is the Super 35 cinema format.
/// </param>
public sealed record PhysicalLens(
    float Aperture = 1f,
    float Shutter = 1f / 125f,
    float Sensitivity = 100f,
    float SensorHeight = 0.01866f);

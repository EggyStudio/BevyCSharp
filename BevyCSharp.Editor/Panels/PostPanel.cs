using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The camera's post processing, bound to a panel.
/// </summary>
/// <remarks>
/// The three files this is one of: <c>assets/panels/post.html</c> is the structure,
/// <c>assets/panels/editor.css</c> is the appearance, and this is the behavior. Nothing here
/// looks an element up or dispatches a click; the attributes say what is tied to what and the
/// generator writes the rest.
/// </remarks>
[EditorPanel("panels/post.html")]
public sealed partial class PostPanel(Entity camera)
{
    private readonly PostSettings _settings = new() { Hdr = true, Msaa = 1 };

    /// <summary>Whether the camera scatters light out of its highlights.</summary>
    [Bind("#bloom")]
    public bool Bloom = true;

    /// <summary>How much is scattered.</summary>
    [Bind("#intensity")]
    public float Intensity = 0.3f;

    /// <summary>How much crispness is put back after antialiasing.</summary>
    [Bind("#sharpen")]
    public float Sharpen;

    /// <summary>A note to whoever is looking, which the engine never reads.</summary>
    [Bind("#note")]
    public string Note = string.Empty;

    /// <summary>
    /// What the camera is currently set to.
    /// </summary>
    /// <remarks>
    /// One way, so that the element follows this and an edit on screen is overwritten. A readout
    /// is the case the mode exists for.
    /// </remarks>
    [Bind("#readout", Mode = BindMode.OneWay)]
    public string Readout =>
        Bloom ? $"bloom {Intensity:F2}, sharpen {Sharpen:F2}" : $"off, sharpen {Sharpen:F2}";

    /// <summary>
    /// Puts what the panel holds onto the camera.
    /// </summary>
    /// <remarks>
    /// Runs whenever a value is edited rather than when a button is pressed, which is what makes
    /// dragging a slider show its result while it is being dragged. Once a frame however many
    /// values moved in it.
    /// </remarks>
    [OnChange]
    public void Apply()
    {
        _settings.Bloom = Bloom;
        _settings.BloomIntensity = Intensity;
        _settings.Sharpen = Sharpen;

        Render.SetPostProcessing(camera, _settings);
    }

    /// <summary>Puts the panel back to the values it starts with.</summary>
    /// <remarks>
    /// The same values the fields are declared with, so that resetting and restarting agree.
    /// </remarks>
    [Command("#reset")]
    public void Reset()
    {
        Bloom = true;
        Intensity = 0.3f;
        Sharpen = 0f;

        Apply();
    }
}

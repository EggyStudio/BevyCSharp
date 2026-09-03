using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The camera's post processing, bound to a panel.
/// </summary>
/// <remarks>
/// Written out by hand for now. Every member below the fields is what the generator will emit
/// from the attributes, and this file is what says whether the emitted shape is the right one.
/// </remarks>
public sealed class PostPanel(Entity camera) : IEditorPanel
{
    private readonly PostSettings _settings = new() { Hdr = true, Msaa = 1 };

    private bool _bloom;
    private float _intensity = 0.3f;
    private float _sharpen;
    private string _note = string.Empty;
    private int _applied;

    /// <inheritdoc/>
    public EditorWindow? Window { get; private set; }

    /// <inheritdoc/>
    public void Open() => Window = EditorWindow.Open("panels/post.html");

    /// <inheritdoc/>
    public void Close()
    {
        Window?.Close();
        Window = null;
    }

    /// <inheritdoc/>
    public void Pull()
    {
        if (Window is not { IsOpen: true } window) return;

        PanelBinding.PullFlag(window.Element("bloom"), _bloom);
        PanelBinding.PullNumber(window.Element("intensity"), _intensity);
        PanelBinding.PullNumber(window.Element("sharpen"), _sharpen);
        PanelBinding.PullText(window.Element("note"), _note);
        PanelBinding.PullText(window.Element("readout"), $"applied {_applied} times");
    }

    /// <inheritdoc/>
    public bool Push(Entity element)
    {
        if (Window is not { IsOpen: true } window) return false;

        if (element == window.Element("bloom"))
        {
            _bloom = PanelBinding.PushFlag(element, _bloom);
            return true;
        }

        if (element == window.Element("intensity"))
        {
            _intensity = PanelBinding.PushNumber(element, _intensity);
            return true;
        }

        if (element == window.Element("sharpen"))
        {
            _sharpen = PanelBinding.PushNumber(element, _sharpen);
            return true;
        }

        if (element == window.Element("note"))
        {
            _note = PanelBinding.PushText(element, _note);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool Invoke(Entity element)
    {
        if (Window is not { IsOpen: true } window) return false;
        if (element != window.Element("apply")) return false;

        Apply();
        return true;
    }

    /// <summary>Puts what the panel holds onto the camera.</summary>
    private void Apply()
    {
        _settings.Bloom = _bloom;
        _settings.BloomIntensity = _intensity;
        _settings.Sharpen = _sharpen;

        Render.SetPostProcessing(camera, _settings);
        _applied++;

        Console.WriteLine(
            $"[editor] applied bloom={_bloom} intensity={_intensity:F2} "
            + $"sharpen={_sharpen:F2} note='{_note}'");
    }
}

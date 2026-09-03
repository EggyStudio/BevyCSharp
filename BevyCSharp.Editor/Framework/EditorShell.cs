using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Holds the open panels and hands the interface's reports to them.
/// </summary>
/// <remarks>
/// <para>
/// A window manager rather than a layout. It has no opinion about menubars, docking or where
/// anything sits: a panel is a document, a document positions itself in CSS, and the shell only
/// decides which are open and who hears about a click. That is what makes a different editor a
/// different set of panels rather than a fork of this file.
/// </para>
/// <para>
/// Registration is by instance rather than by type, so a panel can be constructed with whatever
/// it needs to bind against. Two panels of the same type can be open at once, as long as their
/// documents do not use the same CSS ids.
/// </para>
/// </remarks>
public static class EditorShell
{
    private static readonly List<IEditorPanel> Panels = [];

    /// <summary>The panels currently registered, open or not.</summary>
    public static IReadOnlyList<IEditorPanel> Open => Panels;

    /// <summary>Adds a panel and opens its document.</summary>
    public static T Show<T>(T panel) where T : IEditorPanel
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.Open();
        Panels.Add(panel);
        return panel;
    }

    /// <summary>Closes a panel and forgets it.</summary>
    public static void Hide(IEditorPanel panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.Close();
        Panels.Remove(panel);
    }

    /// <summary>Closes everything.</summary>
    public static void HideAll()
    {
        foreach (var panel in Panels) panel.Close();
        Panels.Clear();
    }

    /// <summary>
    /// Drives every open panel for one frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order matters. What the person did is applied first, then the panels are given their
    /// chance to write back, so a value a click changed is on screen the same frame rather than
    /// the next one. Doing it the other way round would overwrite the edit with the old value
    /// before anything had read it.
    /// </para>
    /// <para>
    /// An event is offered to each panel until one claims it, since an element belongs to exactly
    /// one document.
    /// </para>
    /// </remarks>
    public static void Tick()
    {
        if (!App.HasEditor) return;

        foreach (var report in Xui.Drain())
        {
            switch (report.Kind)
            {
                case UiEventKind.Change:
                    foreach (var panel in Panels)
                        if (panel.Push(report.Element)) break;
                    break;

                case UiEventKind.Click:
                    foreach (var panel in Panels)
                        if (panel.Invoke(report.Element)) break;
                    break;

                case UiEventKind.Reloaded:
                    // Everything was respawned, so every kept entity is stale. The values the
                    // panels hold are not: the pull below puts them straight back onto the new
                    // elements, which is what makes editing a document while it is open feel
                    // like editing the document rather than restarting the editor.
                    foreach (var panel in Panels) panel.Window?.Invalidate();
                    break;
            }
        }

        foreach (var panel in Panels) panel.Pull();
    }
}

namespace Bevy;

/// <summary>
/// A file is being dragged over the window, and has not been let go.
/// </summary>
/// <remarks>
/// The chance to show what the drop would do, before it happens. Every hover ends in either a
/// <see cref="FileDropped"/> or a <see cref="FileHoverCanceled"/>.
/// </remarks>
/// <param name="Path">The absolute path of the file being dragged.</param>
public readonly record struct FileHovered(string Path);

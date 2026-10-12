namespace Bevy;

/// <summary>
/// A file was dropped on the window.
/// </summary>
/// <remarks>
/// One message per file, so dropping a selection of three sends three. The path is the one the
/// platform reported, which is absolute and outside the asset directory, so it is read with
/// ordinary file APIs rather than through the asset server.
/// </remarks>
/// <param name="Path">The absolute path of the dropped file.</param>
public readonly record struct FileDropped(string Path);

using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Pictures of model files for the asset browser's tiles, rendered once and kept as files.
/// </summary>
/// <remarks>
/// <para>
/// A model is geometry, so a tile cannot show one as an image tile shows itself, and a live preview
/// a tile would be a render pass a tile every frame. So each model is drawn once by a
/// <see cref="PreviewRenderer"/> slot of its own, captured to a PNG under the player's directory
/// (<c>user://thumbnails/</c>), and the tile draws the PNG from then on, as it draws any picture.
/// Under the player's directory rather than the project's, since a thumbnail is a cache of this
/// machine's and has no place in version control.
/// </para>
/// <para>
/// A thumbnail is named by the file's path and the time it was last written, so a model saved
/// again is drawn again, and an old picture is never shown for a new file. One model is drawn at a
/// time, a few frames each, so a folder of models fills its tiles one after another rather than
/// stalling a frame on all of them.
/// </para>
/// </remarks>
internal static class Thumbnails
{
    /// <summary>The key the drawing slot is asked for under.</summary>
    private const string Key = "thumbnail";

    /// <summary>How many frames a model is shown before it is captured, for its meshes to arrive and be framed.</summary>
    private const int Settle = 30;

    /// <summary>Models waiting for a picture, oldest first.</summary>
    private static readonly List<string> Waiting = [];

    /// <summary>Thumbnails known to be whole on disk, by the asset path they picture.</summary>
    private static readonly Dictionary<string, string> Ready = [];

    /// <summary>The model being drawn, the frames it has been shown, and where its picture goes.</summary>
    private static (string File, int Frames, string Picture)? _drawing;

    /// <summary>
    /// A capture asked for and not yet usable, with how many frames it has waited and whether its
    /// file has been seen.
    /// </summary>
    private static (string File, string Picture, int Frames, bool Seen)? _writing;

    /// <summary>
    /// The picture of a model file to draw on its tile, or nothing yet, asking for one when there
    /// is none.
    /// </summary>
    /// <param name="file">The model, under the asset root.</param>
    /// <returns>A <c>user://</c> path to draw, or nothing while it is being made.</returns>
    internal static string? Of(string file)
    {
        if (!App.HasRenderer) return null;

        var picture = PictureOf(file);
        if (picture is null) return null;

        if (Ready.TryGetValue(file, out var known) && known == picture) return picture;

        // From an earlier run, whole since it was written then.
        if (File.Exists(UserData.Resolve(picture)) && _writing?.Picture != picture)
        {
            Ready[file] = picture;
            return picture;
        }

        if (!Waiting.Contains(file) && _drawing?.File != file && _writing?.File != file) Waiting.Add(file);
        return null;
    }

    /// <summary>Draws the next waiting model, or captures the one being drawn once it has settled.</summary>
    /// <remarks>Called once a frame, before the panels draw.</remarks>
    internal static void Tick(BehaviorContext ctx)
    {
        if (!App.HasRenderer) return;

        Written();

        if (_drawing is null && _writing is null && Waiting.Count > 0)
        {
            var next = Waiting[0];
            Waiting.RemoveAt(0);
            if (PictureOf(next) is { } picture) _drawing = (next, 0, picture);
        }

        if (_drawing is not { } drawing) return;

        PreviewRenderer.Show(ctx, Key, Subject(drawing.File));
        _drawing = drawing with { Frames = drawing.Frames + 1 };
        if (drawing.Frames < Settle) return;

        var full = UserData.Resolve(drawing.Picture);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            Render.Screenshot(full, PreviewRenderer.TargetOf(Key));
            _writing = (drawing.File, drawing.Picture, 0, false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or Bevy.Interop.BevyNativeException)
        {
            // A picture that cannot be written leaves the tile wearing its icon, which it did anyway.
            Console.Error.WriteLine($"[editor] no thumbnail for {drawing.File}: {error.Message}");
        }

        _drawing = null;
    }

    /// <summary>Marks a capture usable once its file has been on disk for a frame.</summary>
    /// <remarks>
    /// The capture is written by the engine in the background a frame or more after it is asked for,
    /// so the file appears, and is only loaded once it has been there a frame, rather than read
    /// half written. One that never appears is given up on after a few seconds, so a model that
    /// cannot be drawn does not hold up the rest.
    /// </remarks>
    private static void Written()
    {
        if (_writing is not { } writing) return;

        var full = UserData.Resolve(writing.Picture);
        if (File.Exists(full) && new FileInfo(full).Length > 0)
        {
            if (writing.Seen)
            {
                Ready[writing.File] = writing.Picture;
                _writing = null;
            }
            else
            {
                _writing = writing with { Seen = true };
            }

            return;
        }

        _writing = writing.Frames > 300 ? null : writing with { Frames = writing.Frames + 1 };
    }

    /// <summary>What a file is drawn as: a model as its scene, a mesh file alone, a material file on a sphere.</summary>
    private static PreviewSubject? Subject(string file)
    {
        if (!MaterialFiles.IsMaterialFile(file) && !MeshFiles.IsMeshFile(file)) return new PreviewSubject.Scene(file);

        try
        {
            return MeshFiles.IsMeshFile(file)
                ? new PreviewSubject.Mesh(MeshFiles.Load(file))
                : new PreviewSubject.Material(MaterialFiles.Load(file));
        }
        catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidDataException)
        {
            // A file that does not read as a material keeps its icon.
            return null;
        }
    }

    /// <summary>The <c>user://</c> path a file's picture has, from its path and when it was last written.</summary>
    private static string? PictureOf(string file)
    {
        var full = EditorAssets.Absolute(file);
        if (!File.Exists(full)) return null;

        var stamp = File.GetLastWriteTimeUtc(full).Ticks;
        var name = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{file}\n{stamp}")))[..16].ToLowerInvariant();

        return $"user://thumbnails/{name}.png";
    }
}

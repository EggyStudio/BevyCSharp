using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Pictures for tiles, rendered once and kept, as files for the asset browser's models, meshes and
/// materials, and in memory for what the picker offers with no file.
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
/// <para>
/// Something with no file, such as one of Bevy's shapes in the picker or a mesh the scene made in
/// code, is drawn the same way but kept as an image in memory rather than written (<see cref="Drawn"/>),
/// since there is no file whose time says when to draw it again and a picture of a mesh that
/// lives only in this run means nothing to the next.
/// </para>
/// </remarks>
internal static class Thumbnails
{
    /// <summary>The key the drawing slot is asked for under.</summary>
    private const string Key = "thumbnail";

    /// <summary>How many frames a model is shown before it is captured, for its meshes to arrive and be framed.</summary>
    private const int Settle = 30;

    /// <summary>
    /// What a thumbnail is drawn on, as hex with alpha, transparent by default so a tile's own
    /// color shows round the model.
    /// </summary>
    /// <remarks>
    /// A setting under Assets. Part of every thumbnail's name, so changing it draws them all again
    /// rather than leaving pictures on the old background.
    /// </remarks>
    internal static string Background { get; set; } = "#00000000";

    /// <summary>The background as a linear color, or transparent for text that is not a color.</summary>
    private static (float R, float G, float B, float A) Clear
    {
        get
        {
            try
            {
                var color = Color.FromHex(Background.Trim());
                return (color.R, color.G, color.B, color.A);
            }
            catch (FormatException)
            {
                return (0f, 0f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// One picture to draw, named by a file or a key, with what to draw and where it goes, a
    /// <c>user://</c> path for a file's and nothing for one kept in memory.
    /// </summary>
    private sealed record Job(string Name, Func<PreviewSubject?> Subject, string? Picture);

    /// <summary>Pictures waiting to be drawn, oldest first.</summary>
    private static readonly List<Job> Waiting = [];

    /// <summary>Thumbnails known to be whole on disk, by the asset path they picture.</summary>
    private static readonly Dictionary<string, string> Ready = [];

    /// <summary>Pictures kept in memory, by the key they were asked for under.</summary>
    private static readonly Dictionary<string, AssetHandle> Kept = new(StringComparer.Ordinal);

    /// <summary>
    /// The picture being drawn, what it shows, and the frames it has been shown. The subject is
    /// asked for once, so a shape made to be pictured is made once rather than every frame.
    /// </summary>
    private static (Job Job, PreviewSubject? Subject, int Frames)? _drawing;

    /// <summary>A capture asked for and not yet back, with how many frames it has waited.</summary>
    private static (Job Job, int Frames, Capture Capture)? _writing;

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
        if (File.Exists(UserData.Resolve(picture)) && _writing?.Job.Picture != picture)
        {
            Ready[file] = picture;
            return picture;
        }

        Ask(new Job(file, () => Subject(file), picture));
        return null;
    }

    /// <summary>
    /// The picture of something with no file, kept in memory, or zero while it is being drawn,
    /// asking for one the first time.
    /// </summary>
    /// <param name="key">
    /// What it is called, which changes when what it shows does, such as a material's key with its
    /// color in it, so a changed one is drawn again.
    /// </param>
    /// <param name="subject">What to draw, asked for once when its turn comes.</param>
    /// <returns>A name ImGui draws with, or zero.</returns>
    internal static ulong Drawn(string key, Func<PreviewSubject?> subject)
    {
        if (!App.HasRenderer) return 0;

        var named = key + "\n" + Background.Trim().ToLowerInvariant();
        if (Kept.TryGetValue(named, out var image)) return ImGuiTextures.Of(image);

        Ask(new Job(named, subject, null));
        return 0;
    }

    /// <summary>Puts a picture in the queue unless it is in it, being drawn or being written.</summary>
    private static void Ask(Job job)
    {
        if (Waiting.Any(waiting => waiting.Name == job.Name)) return;
        if (_drawing?.Job.Name == job.Name || _writing?.Job.Name == job.Name) return;

        Waiting.Add(job);
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
            _drawing = (next, next.Subject(), 0);
        }

        if (_drawing is not { } drawing) return;

        // Nothing to draw, such as a material file that did not read, keeps its icon.
        if (drawing.Subject is null)
        {
            _drawing = null;
            return;
        }

        PreviewRenderer.Show(ctx, Key, drawing.Subject, Clear);
        _drawing = drawing with { Frames = drawing.Frames + 1 };
        if (drawing.Frames < Settle) return;

        try
        {
            // Read back into memory rather than saved by the engine, whose PNG drops the alpha a
            // transparent background needs.
            _writing = (drawing.Job, 0, Render.BeginCapture(PreviewRenderer.TargetOf(Key)));
        }
        catch (Bevy.Interop.BevyNativeException error)
        {
            // A picture that cannot be taken leaves the tile wearing its icon, which it did anyway.
            Console.Error.WriteLine($"[editor] no thumbnail for {drawing.Job.Name}: {error.Message}");
        }

        _drawing = null;
    }

    /// <summary>Writes a capture to its file once it has come back off the GPU.</summary>
    /// <remarks>
    /// A capture is answered a frame or more after it is asked for. One that never is gets given up
    /// on after a few seconds, so a model that cannot be drawn does not hold up the rest.
    /// </remarks>
    private static void Written()
    {
        if (_writing is not { } writing) return;

        if (Render.TryReadCapture(writing.Capture, out var picture) && picture is not null)
        {
            try
            {
                if (writing.Job.Picture is { } file)
                {
                    UserData.WriteAtomically(UserData.Resolve(file), picture.ToPng());
                    Ready[writing.Job.Name] = file;
                }
                else
                {
                    Kept[writing.Job.Name] = Render.CreateImage(picture.Pixels, picture.Width, picture.Height);
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
            {
                Console.Error.WriteLine($"[editor] no thumbnail for {writing.Job.Name}: {error.Message}");
            }

            Render.ReleaseCapture(writing.Capture);
            _writing = null;
            return;
        }

        if (writing.Frames > 300)
        {
            Render.ReleaseCapture(writing.Capture);
            _writing = null;
            return;
        }

        _writing = writing with { Frames = writing.Frames + 1 };
    }

    /// <summary>What a file is drawn as: a model as its scene, a mesh file alone, a material file on a sphere.</summary>
    private static PreviewSubject? Subject(string file)
    {
        try
        {
            return EditorAssets.KindOf(file) switch
            {
                "mesh" => new PreviewSubject.Mesh(EditorAssets.LoadMesh(file)),
                "material" => new PreviewSubject.Material(EditorAssets.LoadMaterial(file)),
                _ => new PreviewSubject.Scene(file),
            };
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
        // A part of a model is drawn again when the model is written again.
        var full = EditorAssets.Absolute(file.Split('#')[0]);
        if (!File.Exists(full)) return null;

        var stamp = File.GetLastWriteTimeUtc(full).Ticks;
        var name = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{file}\n{stamp}\n{Background.Trim().ToLowerInvariant()}")))[..16].ToLowerInvariant();

        return $"user://thumbnails/{name}.png";
    }
}

namespace Bevy;

/// <summary>
/// A picture that was asked for and has not arrived yet.
/// </summary>
/// <remarks>
/// A capture is answered frames after it is asked for, because the picture has to come back off
/// the GPU, so what <see cref="Render.BeginCapture(AssetHandle)"/> can hand over at the time is a
/// name for the answer rather than the answer.
/// </remarks>
/// <param name="Id">What the engine knows this capture by.</param>
public readonly record struct Capture(int Id)
{
    /// <summary>True when this names a capture that was actually asked for.</summary>
    public bool IsValid => Id > 0;

    /// <inheritdoc/>
    public override string ToString() => $"capture {Id}";

    /// <summary>Whether a recording is being made, taking its pictures or waiting for its last ones to come back.</summary>
    public static bool IsRecording => Recorder.IsRecording;

    /// <summary>
    /// Starts recording what this run draws, a picture at each interval of its clock, until
    /// <see cref="StopRecording"/>, as an animated GIF or PNG.
    /// </summary>
    /// <param name="path">
    /// Where the file goes, a GIF where it ends in <c>.gif</c> and an animated PNG where it ends in
    /// <c>.png</c>, relative to the folder the program runs in unless it is a full path.
    /// </param>
    /// <param name="intervalMs">How often a picture is taken, in milliseconds of the app's clock.</param>
    /// <param name="scale">How much smaller each picture is kept, above zero and up to one, which keeps it as drawn.</param>
    /// <returns>A task finished with the file's path once it is written, or failed with why it was not.</returns>
    /// <remarks>
    /// <para>
    /// Each picture is of the frame being drawn when it falls due, read back off the GPU a frame
    /// or two later and kept in memory, so a recording of a minute of a large window holds a few
    /// hundred megabytes until it is written, and a scale of a half holds a quarter of that. Each
    /// frame is shown for the time between it and the next, so a recording plays at the pace the
    /// game ran at by its clock, and on a set clock (<see cref="Config.FrameSeconds"/>) a recording
    /// is the same moments of the game on any device.
    /// </para>
    /// <para>
    /// A GIF holds 256 colors, chosen for the whole recording and dithered by a pattern that stays
    /// still where the picture does, and counts time in hundredths of a second. An animated PNG
    /// keeps every color as drawn and every millisecond, in a larger file, and both are played by
    /// browsers and by GitHub's pages. The console's <c>record</c> command records for a length
    /// given rather than until stopped.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">The path ends in neither <c>.gif</c> nor <c>.png</c>, or a measure is out of range.</exception>
    /// <exception cref="InvalidOperationException">A recording is being made, or this build has no renderer to take pictures with.</exception>
    public static Task<string> StartRecording(string path, int intervalMs = 100, float scale = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(intervalMs, 0);
        if (!(scale > 0f && scale <= 1f)) throw new ArgumentOutOfRangeException(nameof(scale), scale, "A recording's scale is above zero and up to one.");
        if (!App.HasRenderer) throw new InvalidOperationException("This native bridge was built without Bevy's renderer, so there is nothing to record.");

        return Recorder.Start(new Recorder.Request(path, new Recorder.Measure(intervalMs / 1000.0, false), null, null, scale, null));
    }

    /// <summary>
    /// The key that saves a picture of the frame being drawn into <see cref="ScreenshotFolder"/>,
    /// F2 unless set, or nothing for none.
    /// </summary>
    /// <remarks>
    /// As Minecraft's F2, a picture named by the time, so a player or a tester takes one without a
    /// tool, and the log says where it went. The press is read and left alone, so a program reading
    /// the key itself sees it too, and a program using it for something else of its own moves this
    /// to another key or turns it off. A run that draws nothing, headless, takes none.
    /// </remarks>
    public static Key? ScreenshotKey { get; set; } = Key.F2;

    /// <summary>Where a screenshot goes, a folder named <c>screenshots</c> beside the program unless set, made on first use.</summary>
    public static string ScreenshotFolder { get; set; } = Path.Combine(AppContext.BaseDirectory, "screenshots");

    /// <summary>Saves a picture of the frame being drawn into <see cref="ScreenshotFolder"/>, as the key does. Only valid inside a system.</summary>
    /// <returns>The file's full path, where the picture is written a frame or two later, once it is back off the GPU.</returns>
    /// <remarks>
    /// Named by the local time as Minecraft names its own, <c>2026-10-21_03.45.12.png</c>, then
    /// <c>2026-10-21_03.45.12_2.png</c> and <c>_3</c> for a second and a third taken in the same
    /// second, counting the first as one as Minecraft does, the names given out remembered, since a
    /// file is not there until its picture is written. The capture is the one
    /// <c>shot</c> and <see cref="Render.Screenshot(string)"/> take.
    /// </remarks>
    public static string Screenshot()
    {
        var folder = Path.GetFullPath(ScreenshotFolder);
        Directory.CreateDirectory(folder);

        string path;
        lock (Given)
        {
            path = ScreenshotName(folder, DateTime.Now, candidate => Given.Contains(candidate) || File.Exists(candidate));
            Given.Add(path);
        }

        Render.Screenshot(path);
        Log.Info($"[screenshot] saved to {path}");
        return path;
    }

    private static readonly HashSet<string> Given = new(StringComparer.Ordinal);

    /// <summary>A screenshot's name in a folder for a moment, the moment's own or with the first count not taken.</summary>
    internal static string ScreenshotName(string folder, DateTime at, Func<string, bool> taken)
    {
        var stem = at.ToString("yyyy-MM-dd_HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
        var path = Path.Combine(folder, stem + ".png");
        for (var count = 2; taken(path); count++) path = Path.Combine(folder, $"{stem}_{count}.png");
        return path;
    }

    /// <summary>Stops a recording, whose file is written once the pictures it asked for have come back.</summary>
    /// <returns>The recording's task, finished with the file's path once it is written, or nothing where none was being made.</returns>
    public static Task<string>? StopRecording() => Recorder.Stop();
}

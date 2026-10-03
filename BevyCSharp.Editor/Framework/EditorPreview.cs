using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A picture of the model a person picked in the asset browser.
/// </summary>
/// <remarks>
/// <para>
/// A file's name says what it is called and nothing about what it looks like, which for a model is
/// most of what somebody wants to know. A tile cannot show one the way an image tile shows itself,
/// because a model is geometry rather than pixels, so what is left is to draw it.
/// </para>
/// <para>
/// One picture rather than one per tile, drawn by <see cref="PreviewRenderer"/> under a key of its
/// own, since a camera drawing into an image costs a pass a frame and forty tiles would cost forty.
/// </para>
/// </remarks>
internal static class EditorPreview
{
    /// <summary>The key the browser's picture is drawn under.</summary>
    private const string Key = "browser";

    private static bool _ready;

    /// <summary>Shows the model at <paramref name="path"/>, or nothing when it is null.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="path">The file, under the asset root, or null to show nothing.</param>
    internal static void Show(BehaviorContext ctx, string? path) =>
        _ready = PreviewRenderer.Show(ctx, Key, path is { Length: > 0 } ? new PreviewSubject.Scene(path) : null);

    /// <summary>Whether there is something to look at.</summary>
    internal static bool Ready => _ready;

    /// <summary>Draws the picture at the cursor, turned by dragging it.</summary>
    /// <param name="size">How large to draw it, in logical pixels.</param>
    internal static void Draw(float size)
    {
        if (_ready) PreviewRenderer.Draw(Key, size, turnable: true);
    }

    /// <summary>Puts away every preview nothing asked for this frame.</summary>
    internal static void Keep(BehaviorContext ctx)
    {
        PreviewRenderer.Keep(ctx);
        _ready = false;
    }

    /// <summary>Whether an entity is part of a preview rather than of the project.</summary>
    internal static bool Owns(Entity entity) => PreviewRenderer.Owns(entity);
}

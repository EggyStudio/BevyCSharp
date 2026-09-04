namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The glyphs the editor draws instead of pictures.
/// </summary>
/// <remarks>
/// <para>
/// One table, because the font decides what is possible and the answer is the same everywhere. An
/// icon here is a character rather than an image: the interface has no icon set, an image would be
/// another asset to ship and load per button, and a glyph is styled by the same rules as the text
/// beside it.
/// </para>
/// <para>
/// Kept away from the documents on purpose. A button's picture is a thing a person might want to
/// change without rebuilding, and a table of one-line constants is the smallest place to change
/// it.
/// </para>
/// <para>
/// <b>All of them are ASCII</b>, which was not the first choice. The interface draws with a
/// monospace font that has no arrows, no chevrons and no hamburger, and a font that lacks a glyph
/// draws a box and says nothing about why. A person shipping a font with those characters in it
/// changes these lines and gets the better icons; until then, a plain <c>=</c> that draws beats an
/// elegant one that does not.
/// </para>
/// </remarks>
public static class EditorIcons
{
    /// <summary>The hamburger: everything the editor can do.</summary>
    public static string Menu { get; set; } = "=";

    /// <summary>Take back the last change.</summary>
    public static string Undo { get; set; } = "<";

    /// <summary>Put it back.</summary>
    public static string Redo { get; set; } = ">";

    /// <summary>Write the world and the layout.</summary>
    public static string Save { get; set; } = "S";

    /// <summary>Add something.</summary>
    public static string Add { get; set; } = "+";

    /// <summary>Take something away.</summary>
    public static string Remove { get; set; } = "x";

    /// <summary>Go up a level.</summary>
    public static string Up { get; set; } = "^";

    /// <summary>What the editor is doing.</summary>
    public static string Info { get; set; } = "i";

    /// <summary>Keep a panel where it is.</summary>
    public static string Pin { get; set; } = "*";

    /// <summary>Let it go again.</summary>
    public static string Unpin { get; set; } = "o";

    /// <summary>The row the editor is pointed at.</summary>
    public static string Selected { get; set; } = ">";

    /// <summary>Run something.</summary>
    public static string Run { get; set; } = ">";

    /// <summary>What a directory's name ends with, so it reads as one.</summary>
    public static string Directory { get; set; } = "/";
}

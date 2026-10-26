using System.Globalization;
using System.Text;

namespace Bevy;

// Reading map text, entities in braces holding quoted keyvalues and brushes, brushes in braces
// holding a face a line, and comments from two slashes to the end of a line, the whole read token
// by token so a writer that puts two braces on a line is read as one that does not.
public sealed partial class Level
{
    /// <summary>
    /// The game TrenchBroom's first comment names, which a file written again keeps, or
    /// <c>Generic</c> where it names none.
    /// </summary>
    public string Game { get; set; } = "Generic";

    /// <summary>Reads a level from map text.</summary>
    /// <param name="text">The text, in the Valve 220 format.</param>
    /// <param name="name">What the text is called in an error, as its file's path.</param>
    /// <exception cref="FormatException">The text is not map text, the line it fails on named.</exception>
    /// <remarks>
    /// A face is read as Valve 220 writes it, three points in parentheses, a material, two axes in
    /// brackets, each with its offset, a rotation and two scales. A face in Quake's older format,
    /// with no axes, is refused, since its mapping depends on rules this engine does not follow;
    /// TrenchBroom converts a level to the Valve format from its map menu.
    /// </remarks>
    public static Level Parse(string text, string name = "the level")
    {
        ArgumentNullException.ThrowIfNull(text);
        var reader = new Reader(text, name);
        var level = new Level();
        if (reader.Game is { } game) level.Game = game;

        while (reader.Next() is { } token)
        {
            if (token.Text != "{") throw reader.Fault(token, $"an entity's opening brace is meant, where it reads {token.Text}");
            level.Entities.Add(ReadEntity(reader));
        }

        return level;
    }

    private static LevelEntity ReadEntity(Reader reader)
    {
        var entity = new LevelEntity();
        while (true)
        {
            var token = reader.Next() ?? throw reader.Fault(null, "the file ends inside an entity");
            if (token.Text == "}" && !token.Quoted) return entity;
            if (token.Text == "{" && !token.Quoted)
            {
                entity.Brushes.Add(ReadBrush(reader));
                continue;
            }

            if (!token.Quoted) throw reader.Fault(token, $"a quoted key, a brush or the entity's closing brace is meant, where it reads {token.Text}");
            var value = reader.Next() ?? throw reader.Fault(null, $"the file ends after the key {token.Text}");
            if (!value.Quoted) throw reader.Fault(value, $"the key {token.Text} is given {value.Text}, where a quoted value is meant");
            entity.Keys.Add(new(token.Text, value.Text));
        }
    }

    private static LevelBrush ReadBrush(Reader reader)
    {
        var brush = new LevelBrush();
        while (true)
        {
            var token = reader.Next() ?? throw reader.Fault(null, "the file ends inside a brush");
            if (token.Text == "}" && !token.Quoted) return brush;
            if (token.Text != "(") throw reader.Fault(token, $"a face's first point or the brush's closing brace is meant, where it reads {token.Text}");
            brush.Faces.Add(ReadFace(reader, token.Line));
        }
    }

    private static LevelFace ReadFace(Reader reader, int line)
    {
        var first = ReadPoint(reader, opened: true);
        var second = ReadPoint(reader, opened: false);
        var third = ReadPoint(reader, opened: false);
        var material = reader.Next() ?? throw reader.Fault(null, "the file ends before a face's material");
        if (material.Text is "{" or "}" or "(" or "[" || material.Text.Length == 0) throw reader.Fault(material, $"a face's material is meant, where it reads {material.Text}");

        var opening = reader.Next();
        if (opening?.Text != "[") throw reader.Fault(opening, "a face's two axes in brackets are meant after its material, as the Valve 220 format writes them, so this is Quake's older format, which TrenchBroom converts to the Valve format from its map menu");
        var (uAxis, uOffset) = ReadAxis(reader);
        Expect(reader, "[", "a face's second axis");
        var (vAxis, vOffset) = ReadAxis(reader);
        var rotation = reader.Number("a face's rotation");
        var scaleU = reader.Number("a face's first scale");
        var scaleV = reader.Number("a face's second scale");
        if (reader.Peek() is { Line: var after, Quoted: false, Text: var extra } && after == line && extra is not ("(" or "}"))
            throw reader.Fault(reader.Peek(), $"a face ends at its two scales, where it goes on with {extra}");

        return new LevelFace(first, second, third, material.Text, uAxis, uOffset, vAxis, vOffset, rotation, scaleU, scaleV);
    }

    private static LevelPoint ReadPoint(Reader reader, bool opened)
    {
        if (!opened) Expect(reader, "(", "a face's next point");
        var point = new LevelPoint(reader.Number("a point's x"), reader.Number("a point's y"), reader.Number("a point's z"));
        Expect(reader, ")", "a point's closing parenthesis");
        return point;
    }

    private static (LevelPoint Axis, double Offset) ReadAxis(Reader reader)
    {
        var axis = new LevelPoint(reader.Number("an axis's x"), reader.Number("an axis's y"), reader.Number("an axis's z"));
        var offset = reader.Number("an axis's offset");
        Expect(reader, "]", "an axis's closing bracket");
        return (axis, offset);
    }

    private static void Expect(Reader reader, string text, string what)
    {
        var token = reader.Next();
        if (token?.Text != text || token.Quoted) throw reader.Fault(token, $"{what}, {text}, is meant, where it reads {token?.Text ?? "the file's end"}");
    }

    /// <summary>A token of map text, its text, whether it was quoted, and its line.</summary>
    private sealed record Token(string Text, bool Quoted, int Line);

    /// <summary>Reads map text token by token, keeping the line each began on.</summary>
    private sealed class Reader
    {
        private readonly string _text;
        private readonly string _name;
        private int _at;
        private int _line = 1;
        private Token? _peeked;

        public Reader(string text, string name)
        {
            (_text, _name) = (text, name);

            // TrenchBroom's first comment names the game the level was drawn for.
            const string said = "// Game: ";
            var first = text.TrimStart('﻿');
            if (first.StartsWith(said, StringComparison.Ordinal)) Game = first[said.Length..].Split('\n')[0].Trim();
        }

        public string? Game { get; }

        public Token? Peek() => _peeked ??= Read();

        public Token? Next()
        {
            var token = Peek();
            _peeked = null;
            return token;
        }

        public double Number(string what)
        {
            var token = Next();
            if (token is null || token.Quoted || !double.TryParse(token.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
                throw Fault(token, $"{what} is meant as a number, where it reads {token?.Text ?? "the file's end"}");
            return value;
        }

        public FormatException Fault(Token? token, string said) =>
            new($"{_name}, line {token?.Line ?? _line}: {Capital(said)}.");

        private static string Capital(string said) => said.Length > 0 ? char.ToUpperInvariant(said[0]) + said[1..] : said;

        private Token? Read()
        {
            while (_at < _text.Length)
            {
                var c = _text[_at];
                if (c == '\n') _line++;
                if (char.IsWhiteSpace(c) || c == '﻿')
                {
                    _at++;
                    continue;
                }

                if (c == '/' && _at + 1 < _text.Length && _text[_at + 1] == '/')
                {
                    while (_at < _text.Length && _text[_at] != '\n') _at++;
                    continue;
                }

                if (c is '{' or '}' or '(' or ')' or '[' or ']')
                {
                    _at++;
                    return new Token(c.ToString(), false, _line);
                }

                if (c == '"') return Quoted();

                var start = _at;
                while (_at < _text.Length && !char.IsWhiteSpace(_text[_at]) && _text[_at] is not ('"' or '(' or ')' or '[' or ']' or '{' or '}')) _at++;
                return new Token(_text[start.._at], false, _line);
            }

            return null;
        }

        // A quoted string, a quote inside it written with a backslash before it, as TrenchBroom
        // writes one.
        private Token Quoted()
        {
            var line = _line;
            var built = new StringBuilder();
            _at++;
            while (true)
            {
                if (_at >= _text.Length) throw new FormatException($"{_name}, line {line}: A quoted text is never closed.");
                var c = _text[_at++];
                if (c == '"') return new Token(built.ToString(), true, line);
                if (c == '\\' && _at < _text.Length && _text[_at] == '"')
                {
                    built.Append('"');
                    _at++;
                    continue;
                }

                if (c == '\n') _line++;
                built.Append(c);
            }
        }
    }
}

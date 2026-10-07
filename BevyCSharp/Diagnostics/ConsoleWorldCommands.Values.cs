using System.Globalization;

namespace Bevy;

internal static partial class ConsoleWorldCommands
{
    /// <summary>A member of an enum by its name, in any case, and nothing else.</summary>
    /// <remarks>
    /// <c>Enum.TryParse</c> takes a number as well, and names joined by commas, either of which can
    /// give a value no member has, as a gamepad button of 100, which nothing reading buttons was
    /// written for and which stopped 3DEngine's program as a mouse button. A command names a member,
    /// so a word naming none is refused.
    /// </remarks>
    internal static bool TryName<T>(string word, out T value)
        where T : struct, Enum
    {
        var name = Array.Find(Enum.GetNames<T>(), name => name.Equals(word.Trim(), StringComparison.OrdinalIgnoreCase));
        value = name is null ? default : Enum.Parse<T>(name);
        return name is not null;
    }

    /// <summary>
    /// A member of an enum known only by its value's type, by its name, or for flags by names joined
    /// by commas or bars, or nothing for a word naming none.
    /// </summary>
    private static object? Named(Type type, string word, bool flags)
    {
        var names = Enum.GetNames(type);
        var parts = flags ? word.Split([',', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [word.Trim()];
        if (parts.Length == 0) return null;

        var found = new string[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (Array.Find(names, name => name.Equals(part, StringComparison.OrdinalIgnoreCase)) is not { } name) return null;
            found[i] = name;
        }

        return Enum.Parse(type, string.Join(", ", found));
    }

    /// <summary>
    /// A list's items from words split by semicolons, each read as the list keeps its items, or
    /// nothing where one will not go.
    /// </summary>
    /// <remarks>
    /// Read whole and written whole, as the inspector writes a list, so the items given are the
    /// list afterward and an empty value empties it. An item of an enum is one of the names the
    /// field lists, an entity is a name or <c>#index</c> as <c>entity.set</c> finds one, and a
    /// vector or a color is its numbers split by commas, so a semicolon is the one mark free to part
    /// the items. A struct, an asset or a map has no word to be written as, and is refused.
    /// </remarks>
    private static ListValue? Items(EcsWorld world, string value, ComponentField field)
    {
        var words = value.Trim().Length == 0 ? [] : value.Split(';', StringSplitOptions.TrimEntries);
        var items = new List<object?>(words.Length);
        foreach (var word in words)
        {
            if (Item(world, word, field) is not { } item) return null;
            items.Add(item);
        }

        return new ListValue(items);
    }

    private static object? Item(EcsWorld world, string word, ComponentField field) => field.ElementKind switch
    {
        FieldKind.Bool => Parse(word, false),
        FieldKind.Int => Parse(word, 0),
        FieldKind.Float => Parse(word, 0f),
        FieldKind.Double => Parse(word, 0d),
        FieldKind.String => word,
        FieldKind.Vec3 => Vector(word),
        FieldKind.Quat => Rotation(word),
        FieldKind.Vec2 => Numbers(word, 2) is { } two ? new Vec2(two[0], two[1]) : null,
        FieldKind.Vec4 => Numbers(word, 4) is { } four ? new Vec4(four[0], four[1], four[2], four[3]) : null,
        FieldKind.Color => Numbers(word, 4) is { } rgba ? new Color(rgba[0], rgba[1], rgba[2], rgba[3]) : null,
        FieldKind.Enum or FieldKind.Flags => field.Options.FirstOrDefault(option => option.Equals(word, StringComparison.OrdinalIgnoreCase)),
        FieldKind.Entity => Find(world, word) is { } entity ? entity : null,
        _ => null,
    };

    /// <summary>A number of numbers split by commas, or nothing where there are not that many or one is not a number.</summary>
    private static float[]? Numbers(string word, int count)
    {
        var parts = word.Trim('(', ')', ' ').Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != count) return null;

        var numbers = new float[count];
        for (var i = 0; i < count; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i])) return null;
        }

        return numbers;
    }
}

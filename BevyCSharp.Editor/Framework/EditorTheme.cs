using System.Globalization;
using System.Numerics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor looks like: a ladder of greys, one accent, and the shapes they are drawn in.
/// </summary>
/// <remarks>
/// <para>
/// Hierarchy comes from fill and from how bright the text is, not from lines. A panel is a shade
/// above the ground, a card a shade above the panel, and what is under the pointer a shade above
/// that; nothing is outlined. Borders were what the old look leaned on, and a bordered box inside a
/// bordered box inside a bordered panel is three lines saying what one gap says better.
/// </para>
/// <para>
/// The accent means one thing only: this is what is selected, or what is in force. A colour that
/// means two things means neither.
/// </para>
/// <para>
/// Every value lives here and nowhere else, so the whole editor changes by changing this, and a
/// theme dialled in by hand can be written to a file and read back.
/// </para>
/// </remarks>
public sealed record EditorTheme
{
    /// <summary>What the theme is called, which is what a picker shows.</summary>
    public string Name { get; init; } = "Modern";

    /// <summary>The window behind everything.</summary>
    public Vector4 Ground { get; init; } = Rgb(0x0B, 0x0B, 0x0D);

    /// <summary>A floating panel, seen through at <see cref="PanelAlpha"/>.</summary>
    public Vector4 Panel { get; init; } = Rgb(0x14, 0x14, 0x16);

    /// <summary>A group inside a panel.</summary>
    public Vector4 Card { get; init; } = Rgb(0x1A, 0x1A, 0x1D);

    /// <summary>Under the pointer.</summary>
    public Vector4 Hover { get; init; } = Rgb(0x23, 0x23, 0x27);

    /// <summary>Held down.</summary>
    public Vector4 Active { get; init; } = Rgb(0x2C, 0x2C, 0x31);

    /// <summary>A separator, for the rare place a gap will not do.</summary>
    public Vector4 Line { get; init; } = Rgb(0x24, 0x24, 0x28);

    /// <summary>What is being read.</summary>
    public Vector4 Text { get; init; } = Rgb(0xE8, 0xE8, 0xEA);

    /// <summary>What a value is called, and what it is measured in.</summary>
    public Vector4 Dim { get; init; } = Rgb(0x8A, 0x8A, 0x8E);

    /// <summary>What is switched off.</summary>
    public Vector4 Faint { get; init; } = Rgb(0x5A, 0x5A, 0x5F);

    /// <summary>Selected, in force, checked. Nothing else.</summary>
    public Vector4 Accent { get; init; } = Rgb(0x2B, 0x6C, 0xF6);

    /// <summary>Something worth a second look.</summary>
    public Vector4 Warn { get; init; } = Rgb(0xE0, 0xB0, 0x4C);

    /// <summary>Something that went wrong.</summary>
    public Vector4 Bad { get; init; } = Rgb(0xE0, 0x6C, 0x63);

    /// <summary>How solid a floating panel is over the scene.</summary>
    public float PanelAlpha { get; init; } = 0.85f;

    /// <summary>How round a floating panel is.</summary>
    public float WindowRounding { get; init; } = 10f;

    /// <summary>How round a card inside one is.</summary>
    public float ChildRounding { get; init; } = 8f;

    /// <summary>How round a box, a button or a field is.</summary>
    public float FrameRounding { get; init; } = 6f;

    /// <summary>How round a tab is.</summary>
    public float TabRounding { get; init; } = 8f;

    /// <summary>How much air a panel keeps inside its edge.</summary>
    public Vector2 WindowPadding { get; init; } = new(12f, 10f);

    /// <summary>How much air a box keeps around what it holds.</summary>
    public Vector2 FramePadding { get; init; } = new(8f, 5f);

    /// <summary>How far apart two things on a row are, and two rows.</summary>
    public Vector2 ItemSpacing { get; init; } = new(8f, 7f);

    /// <summary>How wide a line is, where there is one at all.</summary>
    public float Borders { get; init; }

    /// <summary>Whether this is the stock ImGui look rather than the editor's own.</summary>
    /// <remarks>
    /// The stock look is somebody else's decisions, taken whole: applying it means asking ImGui for
    /// its colours rather than writing ours over them, so it stays what ImGui says it is.
    /// </remarks>
    public bool Stock { get; init; }

    /// <summary>The editor's own look.</summary>
    public static EditorTheme Modern { get; } = new();

    /// <summary>ImGui's own, so the editor can be seen as what it is built on.</summary>
    public static EditorTheme Native { get; } = new()
    {
        Name = "Native",
        Stock = true,
        PanelAlpha = 0.94f,
        WindowRounding = 6f,
        ChildRounding = 4f,
        FrameRounding = 4f,
        TabRounding = 4f,
        WindowPadding = new Vector2(8f, 8f),
        FramePadding = new Vector2(6f, 4f),
        ItemSpacing = new Vector2(8f, 4f),
        Borders = 1f,
    };

    /// <summary>Both of them, in the order a picker offers them.</summary>
    public static IReadOnlyList<EditorTheme> All { get; } = [Modern, Native];

    /// <summary>What is in force.</summary>
    public static EditorTheme Current { get; private set; } = Modern;

    /// <summary>Puts a theme into the running context.</summary>
    public static void Apply(EditorTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        Current = theme;

        var style = ImGui.GetStyle();

        style.WindowRounding = theme.WindowRounding;
        style.ChildRounding = theme.ChildRounding;
        style.FrameRounding = theme.FrameRounding;
        style.PopupRounding = theme.ChildRounding;
        style.GrabRounding = theme.FrameRounding;
        style.TabRounding = theme.TabRounding;
        style.ScrollbarRounding = theme.FrameRounding;

        style.WindowBorderSize = theme.Borders;
        style.ChildBorderSize = theme.Borders;
        style.PopupBorderSize = theme.Borders;
        style.FrameBorderSize = 0f;
        style.TabBorderSize = 0f;

        style.WindowPadding = theme.WindowPadding;
        style.FramePadding = theme.FramePadding;
        style.ItemSpacing = theme.ItemSpacing;
        style.ItemInnerSpacing = new Vector2(6f, 4f);
        style.CellPadding = new Vector2(6f, 3f);
        style.IndentSpacing = 18f;
        style.ScrollbarSize = 10f;
        style.GrabMinSize = 10f;

        style.WindowTitleAlign = new Vector2(0f, 0.5f);
        style.SeparatorTextBorderSize = 1f;
        style.SeparatorTextPadding = new Vector2(14f, 4f);
        style.SeparatorTextAlign = new Vector2(0f, 0.5f);

        // The stock look is ImGui's to decide. Ours is written over it.
        if (theme.Stock)
        {
            ImGui.StyleColorsDark();
            return;
        }

        theme.Paint(style);
    }

    /// <summary>Writes the ladder into every colour ImGui asks about.</summary>
    private void Paint(ImGuiStylePtr style)
    {
        Set(style, ImGuiCol.Text, Text);
        Set(style, ImGuiCol.TextDisabled, Faint);

        Set(style, ImGuiCol.WindowBg, Panel);
        Set(style, ImGuiCol.ChildBg, Card);
        Set(style, ImGuiCol.PopupBg, Card);
        Set(style, ImGuiCol.MenuBarBg, Card);

        Set(style, ImGuiCol.Border, Line);
        Set(style, ImGuiCol.BorderShadow, Clear);

        // A box to type in, and what it does under a hand.
        Set(style, ImGuiCol.FrameBg, Card);
        Set(style, ImGuiCol.FrameBgHovered, Hover);
        Set(style, ImGuiCol.FrameBgActive, Active);

        Set(style, ImGuiCol.TitleBg, Panel);
        Set(style, ImGuiCol.TitleBgActive, Panel);
        Set(style, ImGuiCol.TitleBgCollapsed, Panel);

        Set(style, ImGuiCol.Button, Card);
        Set(style, ImGuiCol.ButtonHovered, Hover);
        Set(style, ImGuiCol.ButtonActive, Active);

        // A header is what a component's fold wears, and what a row wears when it is chosen. The
        // accent is the second, so the first is grey and the second is written over it where it is
        // drawn.
        Set(style, ImGuiCol.Header, Hover);
        Set(style, ImGuiCol.HeaderHovered, Active);
        Set(style, ImGuiCol.HeaderActive, Active);

        Set(style, ImGuiCol.Separator, Line);
        Set(style, ImGuiCol.SeparatorHovered, Accent);
        Set(style, ImGuiCol.SeparatorActive, Accent);

        Set(style, ImGuiCol.CheckMark, Accent);
        Set(style, ImGuiCol.SliderGrab, Dim);
        Set(style, ImGuiCol.SliderGrabActive, Accent);

        Set(style, ImGuiCol.ResizeGrip, Clear);
        Set(style, ImGuiCol.ResizeGripHovered, Hover);
        Set(style, ImGuiCol.ResizeGripActive, Accent);

        Set(style, ImGuiCol.Tab, Clear);
        Set(style, ImGuiCol.TabHovered, Hover);
        Set(style, ImGuiCol.TabSelected, Card);
        Set(style, ImGuiCol.TabSelectedOverline, Accent);
        Set(style, ImGuiCol.TabDimmed, Clear);
        Set(style, ImGuiCol.TabDimmedSelected, Card);

        Set(style, ImGuiCol.ScrollbarBg, Clear);
        Set(style, ImGuiCol.ScrollbarGrab, Hover);
        Set(style, ImGuiCol.ScrollbarGrabHovered, Active);
        Set(style, ImGuiCol.ScrollbarGrabActive, Dim);

        Set(style, ImGuiCol.TableHeaderBg, Card);
        Set(style, ImGuiCol.TableBorderStrong, Line);
        Set(style, ImGuiCol.TableBorderLight, Line);
        Set(style, ImGuiCol.TableRowBg, Clear);
        Set(style, ImGuiCol.TableRowBgAlt, Clear);

        Set(style, ImGuiCol.TextSelectedBg, Alpha(Accent, 0.35f));
        Set(style, ImGuiCol.NavCursor, Accent);
        Set(style, ImGuiCol.DragDropTarget, Accent);
        Set(style, ImGuiCol.ModalWindowDimBg, Alpha(Ground, 0.65f));
        Set(style, ImGuiCol.PlotLines, Dim);
        Set(style, ImGuiCol.PlotLinesHovered, Accent);
        Set(style, ImGuiCol.PlotHistogram, Dim);
        Set(style, ImGuiCol.PlotHistogramHovered, Accent);
    }

    /// <summary>The theme as text, one value a line.</summary>
    /// <remarks>
    /// The shape <see cref="EditorSettings.Describe"/> uses, for the same reason: a file somebody
    /// can read, diff and ship, rather than a format that needs a tool to look at.
    /// </remarks>
    public string Describe()
    {
        var lines = new List<string>
        {
            $"name\t{Name}",
            $"ground\t{Hex(Ground)}",
            $"panel\t{Hex(Panel)}",
            $"card\t{Hex(Card)}",
            $"hover\t{Hex(Hover)}",
            $"active\t{Hex(Active)}",
            $"line\t{Hex(Line)}",
            $"text\t{Hex(Text)}",
            $"dim\t{Hex(Dim)}",
            $"faint\t{Hex(Faint)}",
            $"accent\t{Hex(Accent)}",
            $"warn\t{Hex(Warn)}",
            $"bad\t{Hex(Bad)}",
            $"alpha\t{Say(PanelAlpha)}",
            $"window-rounding\t{Say(WindowRounding)}",
            $"child-rounding\t{Say(ChildRounding)}",
            $"frame-rounding\t{Say(FrameRounding)}",
            $"tab-rounding\t{Say(TabRounding)}",
            $"window-padding\t{Say(WindowPadding.X)}\t{Say(WindowPadding.Y)}",
            $"frame-padding\t{Say(FramePadding.X)}\t{Say(FramePadding.Y)}",
            $"item-spacing\t{Say(ItemSpacing.X)}\t{Say(ItemSpacing.Y)}",
            $"borders\t{Say(Borders)}",
            $"stock\t{(Stock ? "1" : "0")}",
        };

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Reads one back, keeping this theme's value for anything the text does not name.
    /// </summary>
    /// <remarks>
    /// A file outlives the version that wrote it. A line naming something this build does not have
    /// is skipped rather than reported, and a value it cannot read leaves the one that was there.
    /// </remarks>
    public static EditorTheme Restore(string text, EditorTheme? from = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var theme = from ?? Modern;

        foreach (var line in text.Split('\n'))
        {
            var parts = line.Split('\t');
            if (parts.Length < 2) continue;

            var value = parts[1].Trim();

            theme = parts[0].Trim() switch
            {
                "name" => theme with { Name = value },
                "ground" => theme with { Ground = Read(value, theme.Ground) },
                "panel" => theme with { Panel = Read(value, theme.Panel) },
                "card" => theme with { Card = Read(value, theme.Card) },
                "hover" => theme with { Hover = Read(value, theme.Hover) },
                "active" => theme with { Active = Read(value, theme.Active) },
                "line" => theme with { Line = Read(value, theme.Line) },
                "text" => theme with { Text = Read(value, theme.Text) },
                "dim" => theme with { Dim = Read(value, theme.Dim) },
                "faint" => theme with { Faint = Read(value, theme.Faint) },
                "accent" => theme with { Accent = Read(value, theme.Accent) },
                "warn" => theme with { Warn = Read(value, theme.Warn) },
                "bad" => theme with { Bad = Read(value, theme.Bad) },
                "alpha" => theme with { PanelAlpha = Number(value, theme.PanelAlpha) },
                "window-rounding" => theme with { WindowRounding = Number(value, theme.WindowRounding) },
                "child-rounding" => theme with { ChildRounding = Number(value, theme.ChildRounding) },
                "frame-rounding" => theme with { FrameRounding = Number(value, theme.FrameRounding) },
                "tab-rounding" => theme with { TabRounding = Number(value, theme.TabRounding) },
                "window-padding" => theme with { WindowPadding = Pair(parts, theme.WindowPadding) },
                "frame-padding" => theme with { FramePadding = Pair(parts, theme.FramePadding) },
                "item-spacing" => theme with { ItemSpacing = Pair(parts, theme.ItemSpacing) },
                "borders" => theme with { Borders = Number(value, theme.Borders) },
                "stock" => theme with { Stock = value == "1" },
                _ => theme,
            };
        }

        return theme;
    }

    /// <summary>
    /// What colour an icon is drawn in.
    /// </summary>
    /// <remarks>
    /// The icons are shapes cut out of white, so what one appears in is what it means: the accent
    /// while it is the one in force, and the ordinary text colour otherwise.
    /// </remarks>
    public static Vector4 IconTint(bool active) => active ? Current.Accent : Current.Text;

    /// <summary>Nothing at all, which is what a surface with no fill is.</summary>
    private static Vector4 Clear => new(0f, 0f, 0f, 0f);

    /// <summary>A colour from the bytes a palette is written in.</summary>
    private static Vector4 Rgb(int red, int green, int blue) =>
        new(red / 255f, green / 255f, blue / 255f, 1f);

    /// <summary>The same colour, seen through.</summary>
    public static Vector4 Alpha(Vector4 color, float alpha) =>
        new(color.X, color.Y, color.Z, alpha);

    /// <summary>How a colour is written down.</summary>
    private static string Hex(Vector4 color) => string.Create(
        CultureInfo.InvariantCulture,
        $"#{(int)MathF.Round(color.X * 255f):X2}{(int)MathF.Round(color.Y * 255f):X2}{(int)MathF.Round(color.Z * 255f):X2}");

    /// <summary>And how one is read.</summary>
    private static Vector4 Read(string text, Vector4 fallback)
    {
        var hex = text.TrimStart('#');
        if (hex.Length < 6) return fallback;

        if (!int.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
            || !int.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
            || !int.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return fallback;
        }

        return Rgb(red, green, blue);
    }

    private static string Say(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static float Number(string text, float fallback) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    private static Vector2 Pair(string[] parts, Vector2 fallback) => parts.Length < 3
        ? fallback
        : new Vector2(Number(parts[1], fallback.X), Number(parts[2], fallback.Y));

    private static void Set(ImGuiStylePtr style, ImGuiCol which, Vector4 color) =>
        style.Colors[(int)which] = color;
}

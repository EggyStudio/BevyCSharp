using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for Bevy's UI, its nodes, text, editable text, images and grids, focus
/// and navigation, and Feathers.
/// </summary>
internal static unsafe partial class NativeUi
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeUi() => NativeLoader.Initialize();

    // -- Nodes, text and images

    /// <summary>Spawns a UI rectangle, returning its entity or 0.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_node(NativeUiNodeConfig* config);

    /// <summary>Spawns a run of UI text, returning its entity or 0.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_text(
        string text, NativeUiNodeConfig* config, NativeUiTextConfig* style);

    /// <summary>Adds a run of text to an existing one, returning its entity or 0.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_ui_spawn_text_span(
        ulong parent, string text, NativeUiTextConfig* style, float* color);

    /// <summary>Lays a node's children out on a grid.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_grid(ulong entity, NativeUiGridConfig* config);

    /// <summary>Places one child on its parent's grid.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_grid_placement(
        ulong entity, int row, int rowSpan, int column, int columnSpan, int justifySelf);

    /// <summary>Replaces what a text entity says.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_text(ulong entity, string text);

    /// <summary>Writes how many font atlases there are, then the bytes their images hold.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_atlases(ulong* counts);

    /// <summary>Writes each font atlas's picture key and font size, and answers how many there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_atlas_pictures(int* keys, float* sizes, int capacity);

    /// <summary>Empties the font atlases.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_clear_font_atlases();

    /// <summary>Reads a node's interaction, 0 for none, 1 for hovered and 2 for pressed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_interaction(ulong entity);

    /// <summary>Draws a picture inside a node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_image(ulong entity, NativeUiImageConfig* config);

    /// <summary>Moves a scrolling node's contents inside it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_scroll(ulong entity, float x, float y);

    /// <summary>Makes a UI node a text field.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_editable_text(ulong entity, NativeEditableTextConfig* config, string text, string? allowed);

    /// <summary>Writes what a text field holds.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_editable_text(ulong entity, byte* output, int capacity);

    /// <summary>Replaces what a text field holds.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_editable_value(ulong entity, string text);

    /// <summary>Gives a text field a cursor and selection of its own.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_text_cursor(ulong entity, NativeTextCursor* cursor);

    /// <summary>Writes the part of a field's text it shows, offset across and down, then width and height.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_editable_viewport(ulong entity, float* output);

    /// <summary>Scrolls a text field to show its text from a point of its layout.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_scroll_editable(ulong entity, float x, float y);

    /// <summary>Sets how many lines tall a text field is, its text as it was.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_set_editable_lines(ulong entity, float lines);

    /// <summary>Queues one of Bevy's text edits on a field.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_edit_text(ulong entity, int kind, int select, float x, float y, byte* text, int length);

    /// <summary>Makes one of Bevy's widgets keep its own state.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_widget_self_update(ulong entity, int kind);

    /// <summary>Gives an entity the input focus, as Bevy's <c>InputFocus::set</c>.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_focus(ulong entity, int cause);

    /// <summary>Writes where the focus would move along the tab order, answering one where there is somewhere.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_navigate(int action, ulong* next);

    /// <summary>Puts a line under or through a run of text, or takes it off, each 1, 0 or -1 to leave it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_text_lines(ulong entity, int underline, int strikethrough);

    /// <summary>Sets a run's OpenType features (kind 0) or variable axes (kind 1) from tagged values.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_tags(ulong entity, int kind, NativeFontTag* tags, int count);

    // -- Navigation

    /// <summary>Moves the input focus by direction, writing where it moved, answering 1 where it did.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_move(int direction, ulong* moved);

    /// <summary>Draws or blocks an edge of the navigation map, one way or both.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_edge(ulong from, ulong to, int direction, int kind);

    /// <summary>Draws edges between nodes in their order, looping where asked.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_edges(ulong* entities, int count, int direction, int looping);

    /// <summary>Takes a node's edges out of the navigation map, or all of them for none.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_forget(ulong entity);

    // -- Feathers

    /// <summary>Spawns a Feathers control of a kind under a parent, with its props and caption.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_spawn(int kind, ulong parent, NativeFeathersProps* props, byte* text, int length, ulong* entity);

    /// <summary>Colors a node from the theme by a token.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_theme_color(ulong entity, int slot, byte* token, int length);

    /// <summary>Writes a number input's value of a kind.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_set(ulong entity, int kind, double value, long whole);

    /// <summary>Reads a number input's value and its kind.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_get(ulong entity, int* kind, double* value, long* whole);

    /// <summary>Bounds a number input's value, hard or soft.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_limit(ulong entity, int hard, int kind, double min, double max, long minWhole, long maxWhole);

    /// <summary>Has a number input show its value in units, by the units' id.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_number_units(ulong entity, byte* units, int length);

    /// <summary>Writes the node a control's contents go under.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_contents(ulong entity, ulong* contents);

    /// <summary>Replaces the theme with token colors alone, or with Bevy's dark theme for none.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_feathers_set_theme(byte* tokens, int length, float* colors, int count);
}

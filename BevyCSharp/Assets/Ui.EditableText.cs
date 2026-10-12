using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Ui
{
    /// <summary>Makes a node a text field the player types into.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's own <c>EditableText</c>: a cursor moved by the arrow keys and the pointer, a selection,
    /// copy, cut and paste, and the text laid out in the node's <c>TextFont</c>, with Bevy's cursor
    /// style. Typing reaches the field that has the input focus, which a click on it gives it, as
    /// does Tab within a <c>TabGroup</c> by each field's <c>TabIndex</c>, and Bevy's <c>AutoFocus</c>
    /// gives it on the frame the field is spawned, and <see cref="Focus"/> gives it from C#. Bevy
    /// hands each key to the focused field only where there is a primary window, and the bridge
    /// hands them out itself in a run with none, so an offscreen run's fields are typed into too,
    /// and Tab moves the focus there as in a window.
    /// </para>
    /// <para>
    /// A field reports each frame's edits as <see cref="TextEditChange"/>, and
    /// <see cref="UiEditableTextSettings.Mode"/> makes one read only or shown alone.
    /// </para>
    /// <para>
    /// Setting a node's field again changes it where it stands, its text replaced with the
    /// settings' text and the rest of the settings taken. What the player typed is read with
    /// <see cref="EditableTextOf"/>, and replaced with <see cref="SetEditableValue"/>, which also
    /// clears it.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var name = Ui.SpawnNode(new UiSettings { Width = Length.Px(240f), Padding = Sides.All(Length.Px(8f)) });
    /// Ui.SetEditableText(name, new UiEditableTextSettings { MaxCharacters = 16 });
    /// ctx.Ecs.Insert&lt;AutoFocusRef&gt;(name);
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetEditableText(Entity node, UiEditableTextSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeEditableTextConfig
        {
            MaxCharacters = Math.Max(0, settings.MaxCharacters),
            VisibleWidth = settings.VisibleWidth,
            VisibleLines = settings.VisibleLines,
            AllowNewlines = settings.AllowNewlines ? 1 : 0,
            Mode = (int)settings.Mode,
        };

        var status = Native.bcs_ui_set_editable_text(node.Bits, &native, settings.Text ?? string.Empty, settings.Allowed);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Making a text field");
        Native.Check(status, $"making {node} a text field");
    }

    /// <summary>What a text field holds, or null for a node that is no field.</summary>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static string? EditableTextOf(Entity node)
    {
        if (Native.bcs_ui_editable_text(node.Bits, null, 0) == NativeStatus.NotPresent) return null;
        return Native.ReadText((buffer, capacity) => Native.bcs_ui_editable_text(node.Bits, buffer, capacity), $"reading the text field {node}");
    }

    /// <summary>Gives a text field a cursor and selection of its own.</summary>
    /// <remarks>
    /// Bevy's <c>TextCursorStyle</c>, which Bevy does not reflect, so no wrapper reaches it. A
    /// field made with <see cref="SetEditableText"/> has Bevy's defaults until this is called, and
    /// a call replaces them all.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone or is no text field, or this build has no renderer.</exception>
    public static void SetTextCursor(Entity field, UiTextCursorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeTextCursor { HasSelectedText = settings.SelectedText is null ? 0 : 1, SelectionRadius = settings.SelectionRadius };
        Write(native.Color, settings.Color);
        Write(native.Selection, settings.Selection);
        Write(native.UnfocusedSelection, settings.UnfocusedSelection);
        Write(native.SelectedText, settings.SelectedText ?? default);

        var status = Native.bcs_ui_set_text_cursor(field.Bits, &native);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Styling a text field's cursor");
        Native.Check(status, $"styling the cursor of the text field {field}");

        static void Write(float* into, Color color) => (into[0], into[1], into[2], into[3]) = (color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// The part of a text field's text it shows, or null for a node that is no field.
    /// </summary>
    /// <remarks>
    /// Bevy keeps a field's scroll here rather than as a node's scroll position, so Bevy's
    /// scrollbar widget cannot drive it. A field's own scrollbar sizes its thumb from this and the
    /// text's laid out size, its <c>TextLayoutInfoRef.Size</c>, and moves the text with
    /// <see cref="ScrollText"/>. It is as Bevy last laid the field out, so a frame behind an edit.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static TextViewport? TextViewportOf(Entity field)
    {
        var values = stackalloc float[4];
        var status = Native.bcs_ui_editable_viewport(field.Bits, values);
        if (status == NativeStatus.NotPresent) return null;
        Native.Check(status, $"reading the viewport of the text field {field}");
        return new TextViewport(new Vec2(values[0], values[1]), new Vec2(values[2], values[3]));
    }

    /// <summary>Scrolls a text field to show its text from a point of its layout.</summary>
    /// <remarks>
    /// The point is the viewport's new top left corner (<see cref="TextViewport.Offset"/>). Bevy
    /// moves it again to keep the cursor in sight as the cursor moves, so a scroll here holds until
    /// the player types or moves through the text.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone or is no text field, or this build has no renderer.</exception>
    public static void ScrollText(Entity field, Vec2 offset) =>
        Native.Check(Native.bcs_ui_scroll_editable(field.Bits, offset.X, offset.Y), $"scrolling the text field {field}");

    /// <summary>
    /// Sets how many lines tall a text field is, its text and cursor left as they are.
    /// </summary>
    /// <remarks>
    /// Setting the field again with <see cref="SetEditableText"/> changes its height as well, and
    /// replaces its text with the settings' text, where this changes the height alone.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone or is no text field, or this build has no renderer.</exception>
    public static void SetVisibleLines(Entity field, float lines) =>
        Native.Check(Native.bcs_ui_set_editable_lines(field.Bits, lines), $"setting the lines of the text field {field}");

    /// <summary>Replaces what a text field holds, its cursor put at the end. An empty string clears it.</summary>
    /// <exception cref="BevyNativeException">The entity is gone or is no text field, or this build has no renderer.</exception>
    public static void SetEditableValue(Entity node, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Native.Check(Native.bcs_ui_set_editable_value(node.Bits, text), $"setting the text field {node}");
    }
}

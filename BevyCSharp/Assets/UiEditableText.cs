using Bevy.Interop;

namespace Bevy;

/// <summary>How a text field behaves, for <see cref="Ui.SetEditableText"/>.</summary>
public sealed class UiEditableTextSettings
{
    /// <summary>What it holds to begin with.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The most characters it holds, or zero for no limit.</summary>
    public int MaxCharacters { get; set; }

    /// <summary>
    /// How many glyphs wide it is, measured by the width of a zero in its font, or zero to be as
    /// wide as its node is laid out.
    /// </summary>
    public float VisibleWidth { get; set; }

    /// <summary>How many lines tall it is. One by default, a single line.</summary>
    public float VisibleLines { get; set; } = 1f;

    /// <summary>Whether Enter starts a new line rather than being left to the game.</summary>
    public bool AllowNewlines { get; set; }

    /// <summary>
    /// The only characters it takes, or null for any. A character typed or pasted that is not
    /// among them is refused, and a paste holding one is refused whole.
    /// </summary>
    /// <remarks>
    /// Bevy's filter is a function of the game's own, which cannot cross from C#, so a field here
    /// names the characters it takes, which covers the numbers, the hex digits and the names a
    /// field is usually limited to. What is already in the field is not checked.
    /// </remarks>
    public string? Allowed { get; set; }
}

public static unsafe partial class Ui
{
    /// <summary>Makes a node a text field the player types into.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's own <c>EditableText</c>: a cursor moved by the arrow keys and the pointer, a selection,
    /// copy, cut and paste, and the text laid out in the node's <c>TextFont</c>, with Bevy's cursor
    /// style. Typing reaches the field that has the input focus, which a click on it gives it, as
    /// does Tab within a <c>TabGroup</c> by each field's <c>TabIndex</c>, and Bevy's <c>AutoFocus</c>
    /// gives it on the frame the field is spawned. Bevy hands each key to the focused field through
    /// the primary window, so a field in an offscreen run, which has none, takes no keys.
    /// </para>
    /// <para>
    /// Setting a node's field again replaces it, its text with the settings' text. What the player
    /// typed is read with <see cref="EditableTextOf"/>, and replaced with
    /// <see cref="SetEditableValue"/>, which also clears it.
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

    /// <summary>Replaces what a text field holds, its cursor put at the end. An empty string clears it.</summary>
    /// <exception cref="BevyNativeException">The entity is gone or is no text field, or this build has no renderer.</exception>
    public static void SetEditableValue(Entity node, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Native.Check(Native.bcs_ui_set_editable_value(node.Bits, text), $"setting the text field {node}");
    }
}

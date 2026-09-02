using Bevy.Interop;

namespace Bevy;

/// <summary>What a length is measured in.</summary>
public enum LengthUnit
{
    /// <summary>Worked out from the layout, which is the default for most fields.</summary>
    Auto = 0,

    /// <summary>Logical pixels, which are scaled with the display rather than raw device ones.</summary>
    Px = 1,

    /// <summary>A share of the parent's size on the same axis.</summary>
    Percent = 2,
}

/// <summary>
/// A distance in a UI layout.
/// </summary>
/// <remarks>
/// A number alone cannot say whether it means pixels, a share of the parent, or "work it out", so
/// the unit travels with it.
/// </remarks>
/// <param name="Value">The magnitude, ignored when the unit is <see cref="LengthUnit.Auto"/>.</param>
/// <param name="Unit">What the magnitude is measured in.</param>
public readonly record struct Length(float Value, LengthUnit Unit)
{
    /// <summary>Left to the layout.</summary>
    public static Length Auto => new(0f, LengthUnit.Auto);

    /// <summary>A distance in logical pixels.</summary>
    public static Length Px(float value) => new(value, LengthUnit.Px);

    /// <summary>A share of the parent, where 100 is all of it.</summary>
    public static Length Percent(float value) => new(value, LengthUnit.Percent);

    /// <inheritdoc/>
    public override string ToString() => Unit switch
    {
        LengthUnit.Px => $"{Value}px",
        LengthUnit.Percent => $"{Value}%",
        _ => "auto",
    };
}

/// <summary>
/// How the pointer stands on a node.
/// </summary>
/// <remarks>
/// Only a node spawned with <see cref="UiSettings.Interactive"/> reports one. Asking a node that
/// was not is refused rather than answered <see cref="None"/>, because a button that quietly
/// never fires is the harder mistake to find.
/// </remarks>
public enum UiInteraction
{
    /// <summary>The pointer is elsewhere.</summary>
    None = 0,

    /// <summary>The pointer is over the node.</summary>
    Hovered = 1,

    /// <summary>The pointer is over the node and its primary button is down.</summary>
    Pressed = 2,
}

/// <summary>
/// Where a UI node sits and how large it is.
/// </summary>
/// <remarks>
/// Every length defaults to <see cref="Length.Auto"/>, so a node with nothing set takes the size
/// of its contents and sits where the layout puts it.
/// </remarks>
public sealed class UiSettings
{
    /// <summary>
    /// Place the node against its parent's edges rather than in the flow of its siblings.
    /// </summary>
    /// <remarks>
    /// What a HUD wants: pinned to a corner, ignoring whatever else is on screen. A node left
    /// relative is laid out beside its siblings instead.
    /// </remarks>
    public bool Absolute { get; set; }

    /// <summary>
    /// Report the pointer over this node, which is what makes it a button.
    /// </summary>
    /// <remarks>
    /// An interactive node also captures the pointer, so nothing behind it is hovered through it.
    /// A node left plain carries nothing to update, which is why this is off by default: a HUD is
    /// mostly nodes that never react, and every one of them would otherwise be tested against the
    /// pointer each frame. Read the result with <see cref="Ui.InteractionOf"/>.
    /// </remarks>
    public bool Interactive { get; set; }

    /// <summary>Distance from the parent's left edge.</summary>
    public Length Left { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's top edge.</summary>
    public Length Top { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's right edge.</summary>
    public Length Right { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's bottom edge.</summary>
    public Length Bottom { get; set; } = Length.Auto;

    /// <summary>How wide the node is.</summary>
    public Length Width { get; set; } = Length.Auto;

    /// <summary>How tall the node is.</summary>
    public Length Height { get; set; } = Length.Auto;

    /// <summary>Space between the node's edge and its contents, on every side.</summary>
    public Length Padding { get; set; } = Length.Auto;

    /// <summary>
    /// The node's background, or the text's colour for a run of text. Linear RGBA.
    /// </summary>
    /// <remarks>Transparent by default, so a plain node is a layout box that draws nothing.</remarks>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 0f);
}

/// <summary>
/// Builds Bevy's UI: panels, and the text on them.
/// </summary>
/// <remarks>
/// <para>
/// Nodes are entities, so everything the ECS already does applies to them.
/// <see cref="EcsWorld.SetParent"/> nests one inside another, which is what lays a screen out,
/// and <see cref="EcsWorld.Despawn"/> takes one away.
/// </para>
/// <para>
/// Needs a render build with a window. A windowless run has nothing to draw on and says so
/// rather than spawning entities that would never appear.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hud = Ui.SpawnText("Score: 0", new UiSettings
/// {
///     Absolute = true,
///     Left = Length.Px(16f),
///     Top = Length.Px(16f),
///     Color = (1f, 1f, 1f, 1f),
/// });
///
/// Ui.SetText(hud, $"Score: {score}");
/// </code>
/// </example>
public static unsafe class Ui
{
    /// <summary>Spawns a rectangle and returns it.</summary>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static Entity SpawnNode(UiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = ToNative(settings);
        var bits = Native.bcs_ui_spawn_node(&native);
        if (bits == 0) throw NoUi("Spawning a UI node");

        return new Entity(bits);
    }

    /// <summary>
    /// Spawns a run of text and returns it.
    /// </summary>
    /// <remarks>
    /// The font is Bevy's own, compiled into the engine, so nothing has to be loaded to put words
    /// on the screen. <see cref="UiSettings.Color"/> is the colour of the text itself.
    /// </remarks>
    /// <param name="text">What it says.</param>
    /// <param name="settings">Where it sits.</param>
    /// <param name="fontSize">Height in logical pixels.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static Entity SpawnText(string text, UiSettings settings, float fontSize = 20f)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(settings);

        var native = ToNative(settings);
        var bits = Native.bcs_ui_spawn_text(text, &native, fontSize);
        if (bits == 0) throw NoUi("Spawning UI text");

        return new Entity(bits);
    }

    /// <summary>
    /// Replaces what a text entity says.
    /// </summary>
    /// <remarks>
    /// Written in place rather than by respawning, because a score or a timer changes every frame
    /// and the entity behind it should not.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or carries no text.</exception>
    public static void SetText(Entity entity, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Native.Check(Native.bcs_ui_set_text(entity.Bits, text), $"setting the text of {entity}");
    }

    /// <summary>
    /// Reports how the pointer stands on an interactive node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="UiInteraction.Pressed"/> lasts from the frame the pointer goes down until it is
    /// released, so a click is the edge into it: keep the previous answer and compare. A release
    /// over the node reads as <see cref="UiInteraction.Hovered"/> again in the same frame.
    /// </para>
    /// <para>
    /// The pointer is tracked by a system that comes with the window, so a windowless run leaves
    /// every node at <see cref="UiInteraction.None"/> rather than failing.
    /// </para>
    /// </remarks>
    /// <param name="entity">A node spawned with <see cref="UiSettings.Interactive"/>.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, or is not a node that reports interaction.
    /// </exception>
    /// <example>
    /// <code>
    /// var state = Ui.InteractionOf(button);
    /// if (state == UiInteraction.Pressed &amp;&amp; Previous != UiInteraction.Pressed) Fire();
    /// Previous = state;
    /// </code>
    /// </example>
    public static UiInteraction InteractionOf(Entity entity)
    {
        var value = Native.bcs_ui_interaction(entity.Bits);
        Native.Check(value, $"reading the interaction of {entity}");

        return (UiInteraction)value;
    }

    private static NativeUiNodeConfig ToNative(UiSettings settings) => new()
    {
        Absolute = settings.Absolute ? 1 : 0,
        Interactive = settings.Interactive ? 1 : 0,
        Left = settings.Left.Value,
        LeftUnit = (int)settings.Left.Unit,
        Top = settings.Top.Value,
        TopUnit = (int)settings.Top.Unit,
        Right = settings.Right.Value,
        RightUnit = (int)settings.Right.Unit,
        Bottom = settings.Bottom.Value,
        BottomUnit = (int)settings.Bottom.Unit,
        Width = settings.Width.Value,
        WidthUnit = (int)settings.Width.Unit,
        Height = settings.Height.Value,
        HeightUnit = (int)settings.Height.Unit,
        Padding = settings.Padding.Value,
        PaddingUnit = (int)settings.Padding.Unit,
        ColorR = settings.Color.R,
        ColorG = settings.Color.G,
        ColorB = settings.Color.B,
        ColorA = settings.Color.A,
    };

    private static BevyNativeException NoUi(string operation) =>
        new(NativeStatus.Unsupported,
            $"{operation} failed: this native build has no renderer, so there is no UI to build "
            + "on. Rebuild the bridge with build/build-native.sh --render.");
}

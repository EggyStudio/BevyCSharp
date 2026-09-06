namespace Bevy;

/// <summary>
/// Draws a number as a slider between two ends.
/// </summary>
/// <remarks>
/// What a field with a sensible range wants: a bar you can throw to one end is faster than a box
/// you have to type into, and it says what the ends are without a word of documentation. A value
/// outside the range is still shown; the slider simply sits at the end it is past.
/// </remarks>
/// <param name="minimum">The low end.</param>
/// <param name="maximum">The high end.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class RangeAttribute(double minimum, double maximum) : Attribute
{
    /// <summary>The low end.</summary>
    public double Minimum { get; } = minimum;

    /// <summary>The high end.</summary>
    public double Maximum { get; } = maximum;
}

/// <summary>What a field is called on screen, when its own name is not the right words.</summary>
/// <param name="text">The words to show.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class LabelAttribute(string text) : Attribute
{
    /// <summary>The words to show.</summary>
    public string Text { get; } = text;
}

/// <summary>
/// A sentence about what a field is for.
/// </summary>
/// <remarks>
/// Shown while the pointer is over the row rather than in a box that appears after a wait. A
/// tooltip nobody sees until they hover for a second is documentation that is only found by
/// accident.
/// </remarks>
/// <param name="text">The sentence.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class TooltipAttribute(string text) : Attribute
{
    /// <summary>The sentence.</summary>
    public string Text { get; } = text;
}

/// <summary>Shown, and not editable.</summary>
/// <remarks>
/// For something worked out rather than set. It is drawn like any other row so it can be read and
/// copied, and what it will not do is take an edit.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ReadOnlyAttribute : Attribute;

/// <summary>Not shown at all.</summary>
/// <remarks>
/// For a field that is a component's own working state. It is still a field of the struct and
/// still saved; it is simply not somebody else's business.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class HiddenAttribute : Attribute;

/// <summary>A heading above a field, which groups the ones under it.</summary>
/// <param name="text">What the heading says.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class HeaderAttribute(string text) : Attribute
{
    /// <summary>What the heading says.</summary>
    public string Text { get; } = text;
}

/// <summary>A blank row above a field, for a break without a word.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class SpaceAttribute : Attribute;

/// <summary>
/// How much one pixel of a drag on the handle is worth.
/// </summary>
/// <remarks>
/// The editor's own step is right for a position in metres and wrong for a count of bullets. This
/// says which scale the field is on.
/// </remarks>
/// <param name="amount">What a pixel is worth.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StepAttribute(double amount) : Attribute
{
    /// <summary>What a pixel is worth.</summary>
    public double Amount { get; } = amount;
}

/// <summary>What the number is measured in, shown after the box.</summary>
/// <param name="suffix">The unit, such as <c>m</c> or <c>deg</c>.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UnitAttribute(string suffix) : Attribute
{
    /// <summary>The unit.</summary>
    public string Suffix { get; } = suffix;
}

/// <summary>Three numbers that are a colour rather than a place.</summary>
/// <remarks>
/// The rows are the same three boxes; what this adds is the patch of colour beside them, which is
/// the only way to tell 0.8, 0.2, 0.1 from a shade of red at a glance.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ColourAttribute : Attribute;

/// <summary>
/// Shown only when another field of the same component is on.
/// </summary>
/// <remarks>
/// What keeps a component with three modes from showing the settings of all three at once. The
/// named field has to be one that reads as true or false.
/// </remarks>
/// <param name="field">The field that decides.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ShowIfAttribute(string field) : Attribute
{
    /// <summary>The field that decides.</summary>
    public string Field { get; } = field;

    /// <summary>Whether the sense is reversed, so the row shows while the other is off.</summary>
    public bool Not { get; init; }
}

/// <summary>Where a field or a button sits among its neighbours. Lower is first.</summary>
/// <param name="order">The place.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class OrderAttribute(int order) : Attribute
{
    /// <summary>The place.</summary>
    public int Order { get; } = order;
}

/// <summary>
/// A method a tool can offer as a button.
/// </summary>
/// <remarks>
/// Every method that takes nothing is offered already, so this is for saying what the button
/// should be called and where it should sit rather than for making one appear.
/// </remarks>
/// <param name="label">What the button says, or nothing for the method's own name.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ButtonAttribute(string label = "") : Attribute
{
    /// <summary>What the button says.</summary>
    public string Label { get; } = label;
}

/// <summary>
/// Which sort of asset a field holds, so a tool can offer the right files.
/// </summary>
/// <remarks>
/// A handle is a handle whatever is behind it, so the field's type cannot say whether a mesh or a
/// sound belongs in it. This does, and a field without it is offered every file there is.
/// </remarks>
/// <param name="kind">One of the names on <see cref="AssetKind"/>.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetAttribute(string kind) : Attribute
{
    /// <summary>The asset type, as <see cref="AssetKind"/> names it.</summary>
    public string Kind { get; } = kind;

    /// <summary>
    /// Which file extensions to offer, separated by spaces, or nothing for the usual ones.
    /// </summary>
    /// <remarks>
    /// For an asset type whose files are not named after it: a scene in a glTF file, a material
    /// in one, anything a game's own loader reads.
    /// </remarks>
    public string Extensions { get; init; } = string.Empty;
}

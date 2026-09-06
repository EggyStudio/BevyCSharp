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

    /// <summary>What sits beside the bar, if anything.</summary>
    public SliderReadout Readout { get; init; } = SliderReadout.Box;
}

/// <summary>What a slider shows beside its bar.</summary>
/// <remarks>
/// A bar with a box beside it is two ways to set one value, which is worth the width when the
/// number is worth typing and a waste of it when the number means nothing on its own. A weight
/// between nought and one is dragged; a field of view is typed.
/// </remarks>
public enum SliderReadout
{
    /// <summary>A box that can be typed into.</summary>
    Box,

    /// <summary>The number, read only, after the bar.</summary>
    Number,

    /// <summary>Nothing. The bar is the whole of it.</summary>
    None,
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
/// Shown only while another field of the same component says so.
/// </summary>
/// <remarks>
/// <para>
/// What keeps a component with three modes from showing the settings of all three at once. With a
/// value it compares: a field shown only while a mode is one particular one. Without, it asks
/// whether the other field is on, which is the same question of a flag.
/// </para>
/// <para>
/// Several of them can sit on one field, and all of them have to hold. A condition naming a field
/// that is not there is ignored rather than obeyed, because a row that vanishes because an
/// attribute has a typo in it is worse than one that should not have been there.
/// </para>
/// </remarks>
/// <param name="field">The field that decides.</param>
/// <param name="value">
/// What it has to read as, or nothing for "is on". An enum is named by its name, and everything
/// else compares as it is written.
/// </param>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ShowIfAttribute(string field, object? value = null) : Attribute
{
    /// <summary>The field that decides.</summary>
    public string Field { get; } = field;

    /// <summary>What it has to read as, or nothing for "is on".</summary>
    public object? Value { get; } = value;

    /// <summary>Whether the sense is reversed, so the row shows while the other does not.</summary>
    public bool Not { get; init; }
}

/// <summary>Hidden while another field says so, which is <see cref="ShowIfAttribute"/> reversed.</summary>
/// <remarks>
/// The same thing written the way somebody means it. Half of these conditions are naturally
/// phrased as "not while", and spelling that as a show with a flag on it reads backwards at the
/// point where it matters.
/// </remarks>
/// <param name="field">The field that decides.</param>
/// <param name="value">What it has to read as for the row to go away, or nothing for "is on".</param>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class HideIfAttribute(string field, object? value = null) : Attribute
{
    /// <summary>The field that decides.</summary>
    public string Field { get; } = field;

    /// <summary>What it has to read as for the row to go away.</summary>
    public object? Value { get; } = value;
}

/// <summary>
/// Puts a field inside a fold, which can be opened and shut.
/// </summary>
/// <remarks>
/// <para>
/// A component with twenty fields is unreadable however well it is ordered, and the answer every
/// editor arrives at is the same: put the eight that are wanted at the top and fold the rest away.
/// Consecutive fields naming the same fold share it.
/// </para>
/// <para>
/// Folds nest, written as a path: <c>[Foldout("Advanced/Debug")]</c> is a fold inside a fold, and
/// it goes as deep as somebody writes slashes. Whether a fold is open is remembered per component
/// rather than per entity, because somebody who shut one meant it about the component.
/// </para>
/// </remarks>
/// <param name="path">What the fold is called, with slashes between the levels.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class FoldoutAttribute(string path) : Attribute
{
    /// <summary>What the fold is called, with slashes between the levels.</summary>
    public string Path { get; } = path;

    /// <summary>Whether it starts open the first time it is seen.</summary>
    public bool Open { get; init; } = true;
}

/// <summary>A line across the panel above a field.</summary>
/// <remarks>
/// For a break that is not worth a word. A heading says what comes next; a line only says that
/// what comes next is something else.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class SeparatorAttribute : Attribute;

/// <summary>What a note beside a field is saying.</summary>
public enum NoteKind
{
    /// <summary>A word over a group of fields.</summary>
    Heading,

    /// <summary>Something worth knowing.</summary>
    Info,

    /// <summary>Something that will probably go wrong.</summary>
    Warning,

    /// <summary>Something that is already wrong.</summary>
    Error,
}

/// <summary>
/// A sentence or two above a field, in the panel rather than on hover.
/// </summary>
/// <remarks>
/// Not a tooltip. A tooltip answers somebody who already suspects there is something to know; this
/// is for what has to be read before the field below it is touched, which is the difference
/// between "what does this do" and "this is in metres, not centimetres".
/// </remarks>
/// <param name="text">What it says.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class InfoAttribute(string text) : Attribute
{
    /// <summary>What it says.</summary>
    public string Text { get; } = text;

    /// <summary>How loudly it says it.</summary>
    public NoteKind Kind { get; init; } = NoteKind.Info;
}

/// <summary>
/// Calls the named methods when the field is changed.
/// </summary>
/// <remarks>
/// <para>
/// For a value something else is derived from: a radius a collider is rebuilt from, a count a pool
/// is resized to. Without it the derived thing is only right after whatever recomputes it happens
/// to run, which in an editor with nothing playing may be never.
/// </para>
/// <para>
/// The methods are named on the same component and take nothing. They are called after the write
/// has landed, so what they read is the new value.
/// </para>
/// </remarks>
/// <param name="methods">The methods to call.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class OnValueChangedAttribute(params string[] methods) : Attribute
{
    /// <summary>The methods to call.</summary>
    public string[] Methods { get; } = methods ?? [];
}

/// <summary>
/// Drawn across the whole panel, with no name beside it.
/// </summary>
/// <remarks>
/// For a value the name column has nothing to add to: a sentence of text, a script, a long path.
/// Every other row keeps its name in its column, so the one that gives it up has to say so.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class WideAttribute : Attribute;

/// <summary>
/// Draws the parts of a value beside each other rather than one per row.
/// </summary>
/// <remarks>
/// Three numbers on one line is what a position wants: it is one value, it is read left to right,
/// and three rows of it costs three times the height for no more information. Long numbers are cut
/// rather than allowed to wrap, which is the trade, and a value whose numbers matter to five digits
/// is one to leave stacked.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InlineAttribute : Attribute;

/// <summary>Where a field or a button sits among its neighbours. Lower is first.</summary>
/// <param name="order">The place.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class OrderAttribute(int order) : Attribute
{
    /// <summary>The place.</summary>
    public int Order { get; } = order;
}

/// <summary>Where a button sits when several of them share a line.</summary>
public enum ButtonLine
{
    /// <summary>On a line of its own.</summary>
    Alone,

    /// <summary>The first of a row of buttons.</summary>
    Start,

    /// <summary>One of the middle ones.</summary>
    Middle,

    /// <summary>The last one, after which the line is drawn.</summary>
    End,
}

/// <summary>
/// A method a tool can offer as a button.
/// </summary>
/// <remarks>
/// <para>
/// Every method that takes nothing is offered already, so this is for saying what the button should
/// be called, how wide it is, and whether it shares a line.
/// </para>
/// <para>
/// A row of buttons is written as a start, any number of middles and an end. Save, Load and Reset
/// are one line of three because they are one decision; three lines of one is a column of buttons
/// that says they are unrelated.
/// </para>
/// </remarks>
/// <param name="label">What the button says, or nothing for the method's own name.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ButtonAttribute(string label = "") : Attribute
{
    /// <summary>What the button says.</summary>
    public string Label { get; } = label;

    /// <summary>Whether it shares a line, and where in it.</summary>
    public ButtonLine Line { get; init; } = ButtonLine.Alone;

    /// <summary>
    /// How much of the line it takes against the others on it.
    /// </summary>
    /// <remarks>
    /// One each is even. Two against one is twice as wide, which is what a line reading Apply,
    /// Cancel wants: they are not the same size decision.
    /// </remarks>
    public double Weight { get; init; } = 1d;
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

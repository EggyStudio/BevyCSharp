namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Marks a class as a panel backed by an HTML document.
/// </summary>
/// <remarks>
/// <para>
/// A panel is three files. The structure is HTML, the appearance is CSS, and this class is the
/// behavior: what the elements are bound to and what the buttons do. The class has to be
/// <see langword="partial"/>, because the wiring between the three is generated into it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EditorPanel("panels/settings.html", Root = "#settings", Dock = EditorDock.Right)]
/// public sealed partial class SettingsPanel
/// {
///     [Bind("#bloom")]     public bool Bloom;
///     [Bind("#intensity")] public float Intensity;
///     [Command("#apply")]  public void Apply() { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EditorPanelAttribute(string document) : Attribute
{
    /// <summary>The document's path, relative to the asset root.</summary>
    public string Document { get; } = document;

    /// <summary>
    /// The CSS id of the panel's outermost element, which is what gets placed.
    /// </summary>
    /// <remarks>
    /// Ids are global across every open document, so this has to be unique in the whole editor
    /// rather than in its own file. A panel that names none is left wherever its stylesheet puts
    /// it, which is the right answer for a panel that fills the screen or that is placed by hand.
    /// </remarks>
    public string? Root { get; init; }

    /// <summary>
    /// The CSS id of the element the panel is dragged by, usually its title bar.
    /// </summary>
    /// <remarks>
    /// A drag moves the panel by writing the layout, so dragging a window is the same operation
    /// as loading a saved arrangement, and what a person drags into place is what a saved layout
    /// records.
    /// </remarks>
    public string? Handle { get; init; }

    /// <summary>Which part of the window the panel belongs to.</summary>
    public EditorDock Dock { get; init; } = EditorDock.Floating;

    /// <summary>Where it sits among the other panels of its dock. Lower is first.</summary>
    public int Order { get; init; }

    /// <summary>
    /// Whether it takes whatever height its dock has left over.
    /// </summary>
    /// <remarks>
    /// What a list wants: as long as the screen allows, without the panel knowing how tall the
    /// screen is or what else is open. Two panels that both fill share what is left equally.
    /// </remarks>
    public bool Fill { get; init; }

    /// <summary>Its offset inside that dock, or its left edge when the panel floats.</summary>
    public float X { get; init; }

    /// <summary>The same, vertically.</summary>
    public float Y { get; init; }

    /// <summary>How wide it is, or nothing to be as wide as its contents.</summary>
    public float Width { get; init; } = float.NaN;

    /// <summary>How tall it is, or nothing to be as tall as its contents.</summary>
    public float Height { get; init; } = float.NaN;

    /// <summary>What makes it go away.</summary>
    public PanelDismiss Dismiss { get; init; } = PanelDismiss.Never;

    /// <summary>Which panels it draws in front of. Higher is nearer.</summary>
    public int Layer { get; init; }
}

/// <summary>Which way a binding carries a value.</summary>
public enum BindMode
{
    /// <summary>
    /// The field follows the element and the element follows the field.
    /// </summary>
    /// <remarks>The default wherever the member can be written to.</remarks>
    TwoWay = 0,

    /// <summary>The element follows the field, and an edit on screen is overwritten.</summary>
    /// <remarks>What a readout wants: a frame counter, a status line, a computed total.</remarks>
    OneWay = 1,
}

/// <summary>
/// Ties a field or property to the element carrying a CSS id.
/// </summary>
/// <remarks>
/// <para>
/// Which of the element's values is used follows the member's type: a <see cref="bool"/> ticks a
/// checkbox, a number moves a slider, and a string is text. Anything else is refused when the
/// panel is compiled rather than ignored when it runs.
/// </para>
/// <para>
/// A get-only property is one way whatever the mode says, since there is nowhere to put an edit.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BindAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;

    /// <summary>Which way the value travels.</summary>
    public BindMode Mode { get; init; } = BindMode.TwoWay;

    /// <summary>
    /// How many elements the id stands for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Left alone, one member is one element. Given a count, the id is a prefix and the elements
    /// are numbered from it: <c>row</c> with a count of eight binds <c>row-0</c> through
    /// <c>row-7</c>, and the member is an array holding a value for each.
    /// </para>
    /// <para>
    /// This is how a document with a fixed set of elements shows a list whose length nobody knew
    /// when the document was written. The elements are a pool: the panel decides which of them
    /// stand for what, and <see cref="ShowAttribute"/> takes the leftovers off screen. It is
    /// also what a long list wants anyway, since a hierarchy of ten thousand entities is drawn
    /// by however many rows fit on the screen.
    /// </para>
    /// </remarks>
    public int Count { get; init; }
}

/// <summary>
/// Ties a <see cref="bool"/> to whether an element is on screen.
/// </summary>
/// <remarks>
/// Its own attribute rather than a binding mode, because visibility is done to an element rather
/// than held by it: an element can have its value bound and its visibility bound at the same time,
/// and neither is the other's business. Always one way, since an element that is not drawn has
/// nothing to say back.
///
/// It may be written more than once on one member, which is how the three boxes of a vector row
/// appear and disappear together without three fields saying the same thing.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ShowAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;

    /// <summary>How many elements the id stands for, exactly as on <see cref="BindAttribute"/>.</summary>
    public int Count { get; init; }
}

/// <summary>
/// Runs a method after anything the person edited has been read back into the panel.
/// </summary>
/// <remarks>
/// <para>
/// What makes a panel apply as it is used rather than when a button is pressed. The method takes
/// no arguments: every bound member already holds the new value by the time it runs.
/// </para>
/// <para>
/// Called once per frame however many elements changed in it, so dragging a slider does the work
/// once a frame rather than once per binding. A frame in which nothing was edited does not call
/// it at all, which is what stops a panel writing engine state sixty times a second for nothing.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnChangeAttribute : Attribute;

/// <summary>
/// Runs a method when the element carrying a CSS id is clicked with the secondary button.
/// </summary>
/// <remarks>
/// What offers a context menu. Kept apart from <see cref="CommandAttribute"/> because asking what
/// can be done to a thing is a different gesture from doing it, and a row usually wants both: a
/// left click selects, a right click offers the list.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ContextAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;

    /// <summary>How many elements the id stands for, exactly as on <see cref="BindAttribute"/>.</summary>
    public int Count { get; init; }
}

/// <summary>
/// Runs a method once a frame, before the panel's values are written to its elements.
/// </summary>
/// <remarks>
/// <para>
/// Where a panel that shows the world reads it. A hierarchy fills its list of names here and an
/// inspector fills its rows from the selection, and by the time the bindings run there is nothing
/// left to do but write ordinary values out.
/// </para>
/// <para>
/// The method takes no arguments. What it needs is the world, and that is
/// <c>EditorShell.Ecs</c>, valid for exactly as long as this call is.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnRefreshAttribute : Attribute;

/// <summary>
/// Runs a method when the element carrying a CSS id is clicked.
/// </summary>
/// <remarks>
/// The method takes no arguments and returns nothing, because which element was clicked is
/// already known: it is the one the attribute names. A command over repeated elements takes one
/// <see cref="int"/> instead, for which of them it was.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(string element) : Attribute
{
    /// <summary>The element's CSS id, with or without the leading hash.</summary>
    public string Element { get; } = element;

    /// <summary>
    /// How many elements the id stands for, exactly as on <see cref="BindAttribute"/>.
    /// </summary>
    /// <remarks>
    /// A command over repeated elements is handed which of them was clicked, as an
    /// <see cref="int"/>, since that is the one thing the attribute cannot say for it.
    /// </remarks>
    public int Count { get; init; }
}

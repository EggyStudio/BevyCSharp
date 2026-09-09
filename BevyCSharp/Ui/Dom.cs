using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>What the page reported.</summary>
public enum DomEventKind
{
    /// <summary>An element was clicked.</summary>
    Click = 0,

    /// <summary>An element's value changed.</summary>
    Input = 1,

    /// <summary>An element took the keyboard.</summary>
    Focus = 2,
}

/// <summary>One element of the page.</summary>
/// <remarks>
/// A handle, not a widget. Nothing is known about what an element is: it is markup, and what it
/// looks like and where it sits are the stylesheet's business.
/// </remarks>
/// <param name="Value">What the engine calls this element, or 0 for none.</param>
public readonly record struct Element(ulong Value)
{
    /// <summary>No element.</summary>
    public static Element None => new(0);

    /// <summary>Whether this names an element at all.</summary>
    public bool Exists => Value != 0;
}

/// <summary>A box, in physical pixels, as the page laid it out.</summary>
/// <param name="Left">Distance from the left edge of the page.</param>
/// <param name="Top">Distance from the top edge of the page.</param>
/// <param name="Width">How wide the box is.</param>
/// <param name="Height">How tall the box is.</param>
public readonly record struct Rect(float Left, float Top, float Width, float Height)
{
    /// <summary>Whether a point is inside the box.</summary>
    public bool Contains(float x, float y) =>
        x >= Left && y >= Top && x < Left + Width && y < Top + Height;
}

/// <summary>Something the page reported, and where.</summary>
/// <param name="Kind">What happened.</param>
/// <param name="Target">Which element it happened to.</param>
public readonly record struct DomEvent(DomEventKind Kind, Element Target);

/// <summary>
/// The page.
/// </summary>
/// <remarks>
/// <para>
/// One document holds the whole interface, the way a page holds a whole application. A panel is a
/// piece of markup put into it, not a document with a layer and a position of its own, which is
/// why nothing here computes where anything goes.
/// </para>
/// <para>
/// Behind this is Stylo, the CSS engine Firefox ships, with Taffy for layout and Parley for text.
/// Cascade, specificity, inheritance, <c>@layer</c>, <c>:where</c>, nesting, custom properties,
/// <c>oklch</c> and <c>color-mix</c> are that engine's, so a stylesheet written for a browser is a
/// stylesheet that works here.
/// </para>
/// </remarks>
public static unsafe class Dom
{
    /// <summary>Opens the page from markup.</summary>
    /// <remarks>
    /// Markup rather than a path, because the caller knows where its assets live and because an
    /// interface a game builds at runtime never was a file.
    /// </remarks>
    public static void Open(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        Native.Check(Native.bcs_dom_open(html), nameof(Native.bcs_dom_open));
    }

    /// <summary>Takes the page down.</summary>
    public static void Close() => Native.Check(Native.bcs_dom_close(), nameof(Native.bcs_dom_close));

    /// <summary>The element carrying an id.</summary>
    public static Element Element(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return new Element(Native.bcs_dom_element(id));
    }

    /// <summary>The first element a CSS selector matches.</summary>
    public static Element Select(string selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new Element(Native.bcs_dom_select(selector));
    }

    /// <summary>Makes an element, which shows nothing until it is appended.</summary>
    public static Element Create(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return new Element(Native.bcs_dom_create(tag));
    }

    /// <summary>Puts an element inside another, at the end.</summary>
    public static void Append(Element parent, Element child) =>
        Native.Check(Native.bcs_dom_append(parent.Value, child.Value), nameof(Native.bcs_dom_append));

    /// <summary>Takes an element out of the page.</summary>
    public static void Remove(Element element) =>
        Native.Check(Native.bcs_dom_remove(element.Value), nameof(Native.bcs_dom_remove));

    /// <summary>Puts markup inside an element, replacing what was there.</summary>
    /// <remarks>
    /// How a panel arrives, and how it leaves: <c>SetHtml(host, "")</c> is closing one.
    /// </remarks>
    public static void SetHtml(Element element, string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        Native.Check(Native.bcs_dom_set_html(element.Value, html), nameof(Native.bcs_dom_set_html));
    }

    /// <summary>An element's text.</summary>
    public static string GetText(Element element) =>
        Native.ReadText(
            (buffer, capacity) => Native.bcs_dom_get_text(element.Value, buffer, capacity),
            nameof(Native.bcs_dom_get_text));

    /// <summary>Sets an element's text.</summary>
    public static void SetText(Element element, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Native.Check(Native.bcs_dom_set_text(element.Value, text), nameof(Native.bcs_dom_set_text));
    }

    /// <summary>Sets an attribute.</summary>
    /// <remarks>
    /// The whole of how state reaches the interface. A class goes on, and every rule written
    /// against that class applies; nothing on this side works out what that should look like.
    /// </remarks>
    public static void SetAttribute(Element element, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        Native.Check(
            Native.bcs_dom_set_attribute(element.Value, name, value),
            nameof(Native.bcs_dom_set_attribute));
    }

    /// <summary>Takes an attribute off.</summary>
    public static void ClearAttribute(Element element, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Native.Check(
            Native.bcs_dom_clear_attribute(element.Value, name),
            nameof(Native.bcs_dom_clear_attribute));
    }

    /// <summary>Sets the class list.</summary>
    public static void SetClass(Element element, string classes) =>
        SetAttribute(element, "class", classes);

    /// <summary>Puts a class on or takes it off.</summary>
    public static void ToggleClass(Element element, string name, bool on)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var current = Attribute(element, "class");
        var parts = current.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var has = parts.Contains(name);

        if (on == has) return;

        if (on) parts.Add(name);
        else parts.Remove(name);

        SetClass(element, string.Join(' ', parts));
    }

    /// <summary>What an attribute says, or the empty string.</summary>
    /// <remarks>
    /// Read back out of the page rather than remembered here, so that markup replaced wholesale
    /// answers for itself rather than from a copy that is one panel out of date.
    /// </remarks>
    public static string Attribute(Element element, string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return Native.ReadText(
            (buffer, capacity) => Native.bcs_dom_get_attribute(element.Value, name, buffer, capacity),
            nameof(Native.bcs_dom_get_attribute));
    }

    /// <summary>The box an element was laid out in.</summary>
    public static bool TryRect(Element element, out Rect rect)
    {
        var values = stackalloc float[4];
        var status = Native.bcs_dom_rect(element.Value, values);

        if (status < 0)
        {
            rect = default;
            return false;
        }

        rect = new Rect(values[0], values[1], values[2], values[3]);
        return true;
    }

    /// <summary>Gives an element the keyboard.</summary>
    public static void Focus(Element element) =>
        Native.Check(Native.bcs_dom_focus(element.Value), nameof(Native.bcs_dom_focus));

    /// <summary>Takes the keyboard away from whatever holds it.</summary>
    public static void Blur() => Native.Check(Native.bcs_dom_blur(), nameof(Native.bcs_dom_blur));

    /// <summary>Whatever holds the keyboard.</summary>
    public static Element Focused() => new(Native.bcs_dom_focused());

    /// <summary>The element at a point, in physical pixels.</summary>
    public static Element Hit(float x, float y) => new(Native.bcs_dom_hit(x, y));

    /// <summary>Takes what the page reported since the last call.</summary>
    public static int Drain(Span<DomEvent> into)
    {
        if (into.IsEmpty) return 0;

        var raw = stackalloc NativeDomEvent[into.Length];
        var count = Native.Check(
            Native.bcs_dom_events(raw, into.Length), nameof(Native.bcs_dom_events));

        for (var index = 0; index < count; index++)
        {
            into[index] = new DomEvent((DomEventKind)raw[index].Kind, new Element(raw[index].Node));
        }

        return count;
    }
}

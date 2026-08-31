using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// This frame's keyboard and mouse state, mirrored from Bevy.
/// </summary>
/// <remarks>
/// Refreshed once per frame during <see cref="Stage.FrameSync"/>, before any user system runs,
/// so every system in a frame observes the same input. Reads are plain managed field accesses,
/// no call crosses into the engine.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// public static void Move(BehaviorContext ctx)
/// {
///     if (ctx.Input.KeyDown(Key.W)) Advance(ctx);
///     if (ctx.Input.KeyPressed(Key.Space)) Jump(ctx);
/// }
/// </code>
/// </example>
public sealed class Input
{
    private readonly ulong[] _down = new ulong[NativeInput.KeyWords];
    private readonly ulong[] _pressed = new ulong[NativeInput.KeyWords];
    private readonly ulong[] _released = new ulong[NativeInput.KeyWords];

    private uint _mouseDown;
    private uint _mousePressed;
    private string _text = string.Empty;
    private readonly Touch[] _touches = new Touch[NativeInput.TouchCapacity];
    private int _touchCount;
    private uint _mouseReleased;

    /// <summary>Cursor X in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseX { get; private set; }

    /// <summary>Cursor Y in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseY { get; private set; }

    /// <summary>Cursor position.</summary>
    public (float X, float Y) MousePosition => (MouseX, MouseY);

    /// <summary>Cursor X movement since the previous frame.</summary>
    public float MouseDeltaX { get; private set; }

    /// <summary>Cursor Y movement since the previous frame.</summary>
    public float MouseDeltaY { get; private set; }

    /// <summary>Cursor movement since the previous frame.</summary>
    public (float X, float Y) MouseDelta => (MouseDeltaX, MouseDeltaY);

    /// <summary>Horizontal scroll since the previous frame.</summary>
    public float WheelX { get; private set; }

    /// <summary>Vertical scroll since the previous frame.</summary>
    public float WheelY { get; private set; }

    /// <summary>True while <paramref name="key"/> is held down.</summary>
    public bool KeyDown(Key key) => TestBit(_down, key);

    /// <summary>True on the single frame <paramref name="key"/> went down.</summary>
    public bool KeyPressed(Key key) => TestBit(_pressed, key);

    /// <summary>True on the single frame <paramref name="key"/> went up.</summary>
    public bool KeyReleased(Key key) => TestBit(_released, key);

    /// <summary>True while any key at all is held.</summary>
    public bool AnyKeyDown() => AnyBit(_down);

    /// <summary>True when any key at all went down this frame.</summary>
    public bool AnyKeyPressed() => AnyBit(_pressed);

    /// <summary>True while at least one of <paramref name="keys"/> is held.</summary>
    /// <remarks>
    /// Mirrors Bevy's <c>ButtonInput::any_pressed</c>. This is how you express a side-agnostic
    /// modifier by hand: <c>AnyKeyDown([Key.ControlLeft, Key.ControlRight])</c>.
    /// </remarks>
    public bool AnyKeyDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_down, key))
                return true;

        return false;
    }

    /// <summary>True while every one of <paramref name="keys"/> is held.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::all_pressed</c>. An empty span is true.</remarks>
    public bool AllKeysDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (!TestBit(_down, key))
                return false;

        return true;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went down this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_pressed</c>.</remarks>
    public bool AnyKeyPressed(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_pressed, key))
                return true;

        return false;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went up this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_released</c>.</remarks>
    public bool AnyKeyReleased(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_released, key))
                return true;

        return false;
    }

    /// <summary>True while <paramref name="button"/> is held down.</summary>
    public bool MouseDown(MouseButton button) => (_mouseDown & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went down.</summary>
    public bool MousePressed(MouseButton button) => (_mousePressed & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went up.</summary>
    public bool MouseReleased(MouseButton button) => (_mouseReleased & Bit(button)) != 0;

    /// <summary>True while any mouse button is held.</summary>
    public bool AnyMouseDown() => _mouseDown != 0;

    /// <summary>True when any mouse button went down this frame.</summary>
    public bool AnyMousePressed() => _mousePressed != 0;

    private static uint Bit(MouseButton button) => 1u << (int)button;

    private static bool TestBit(ulong[] bits, Key key)
    {
        var index = (int)key;
        if ((uint)index >= KeyTable.Count) return false;
        return (bits[index / 64] & (1UL << (index % 64))) != 0;
    }

    private static bool AnyBit(ulong[] bits)
    {
        foreach (var word in bits)
            if (word != 0)
                return true;

        return false;
    }

    /// <summary>Copies a native snapshot into this frame's state.</summary>
    internal unsafe void Update(NativeInput snapshot)
    {
        MouseX = snapshot.MouseX;
        MouseY = snapshot.MouseY;
        MouseDeltaX = snapshot.MouseDeltaX;
        MouseDeltaY = snapshot.MouseDeltaY;
        WheelX = snapshot.WheelX;
        WheelY = snapshot.WheelY;
        _mouseDown = snapshot.MouseDown;
        _mousePressed = snapshot.MousePressed;
        _mouseReleased = snapshot.MouseReleased;

        for (var word = 0; word < NativeInput.KeyWords; word++)
        {
            _down[word] = snapshot.KeysDown[word];
            _pressed[word] = snapshot.KeysPressed[word];
            _released[word] = snapshot.KeysReleased[word];
        }

        UpdateTextAndTouches(snapshot);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"Input(mouse=({MouseX:F0},{MouseY:F0}), keysDown={(AnyKeyDown() ? "yes" : "no")})";

    /// <summary>
    /// What the keyboard produced this frame, as text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The characters a text field should insert, in the order they were typed. This is not the
    /// same question as which keys are down: it is the layout's answer rather than the hardware's,
    /// so a French keyboard gives what its user expects, and a dead key followed by a vowel gives
    /// one accented character.
    /// </para>
    /// <para>
    /// Control characters are left out. Backspace and Enter arrive as text on some platforms, and
    /// a field that inserted them as characters would be wrong on all of them; read those with
    /// <see cref="KeyPressed"/> instead.
    /// </para>
    /// <para>
    /// Empty on most frames, and never null. A frame carrying more than 32 bytes of text loses
    /// the rest, which typing cannot reach but a paste could.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// name += ctx.Input.Text;
    /// if (ctx.Input.KeyPressed(Key.Backspace) &amp;&amp; name.Length > 0)
    ///     name = name[..^1];
    /// </code>
    /// </example>
    public string Text => _text;

    /// <summary>The touches in progress this frame.</summary>
    /// <remarks>
    /// A touch that ended is reported once, on the frame it ends, with
    /// <see cref="TouchPhase.Ended"/>. Nothing else reports it, so a release is missed by a
    /// system that skips a frame.
    /// </remarks>
    public ReadOnlySpan<Touch> Touches => _touches.AsSpan(0, _touchCount);

    /// <summary>Copies the typed text and touches out of a native snapshot.</summary>
    private unsafe void UpdateTextAndTouches(in NativeInput snapshot)
    {
        var length = (int)Math.Min(snapshot.TextLength, NativeInput.TextCapacity);
        if (length == 0)
        {
            // The common case by far, and the one worth not allocating for.
            _text = string.Empty;
        }
        else
        {
            fixed (byte* text = snapshot.Text) _text = Encoding.UTF8.GetString(text, length);
        }

        _touchCount = (int)Math.Min(snapshot.TouchCount, NativeInput.TouchCapacity);
        for (var i = 0; i < _touchCount; i++)
        {
            var touch = snapshot.Touches[i];
            _touches[i] = new Touch(touch.Id, touch.X, touch.Y, (TouchPhase)touch.Phase);
        }
    }
}

/// <summary>Where a touch is in its life.</summary>
public enum TouchPhase
{
    /// <summary>Still down, and not new this frame.</summary>
    Held = 0,

    /// <summary>Went down this frame.</summary>
    Started = 1,

    /// <summary>Came up this frame. Reported once, then gone.</summary>
    Ended = 2,
}

/// <summary>One finger on a touchscreen.</summary>
/// <param name="Id">Identifies this finger while it stays down.</param>
/// <param name="X">Position in physical window pixels.</param>
/// <param name="Y">Position in physical window pixels.</param>
/// <param name="Phase">Where the touch is in its life.</param>
public readonly record struct Touch(ulong Id, float X, float Y, TouchPhase Phase);

using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// This frame's keyboard and mouse state, mirrored from Bevy.
/// </summary>
/// <remarks>
/// <para>
/// Refreshed once per frame during <see cref="Stage.FrameSync"/>, before any user system runs,
/// so every system in a frame observes the same input. Reads are plain managed field accesses,
/// no call crosses into the engine.
/// </para>
/// <para>
/// Escape goes to what is open first. While the console or the settings panel is open, or a field
/// of the interface has the caret, a press of Escape is theirs, the settings panel going back a
/// page with it, and the program reads it neither here nor as a <see cref="KeyboardInput"/> until
/// the key comes up. With none of them open it is the program's, which an example may end on, and
/// the overlay takes no key, so it holds none.
/// </para>
/// <para>
/// While the console or the settings panel is open, every read here answers as though nothing were
/// pressed or moved, the pointer standing where it stood as the panel opened, the pads at rest and
/// no touch down, so a key or a stick that changes a row does not move the scene behind it as
/// well. The input's messages keep to it, no press, move or value past rest coming while it lasts
/// and a release coming for each press the program read. The overlay, which only shows, holds
/// nothing, and the keys that open and close the panels act as ever. A key, a button, a touch or a
/// stick a hand holds as a panel closes stays unseen until it is let go, a stick until it comes
/// back within a quarter of its travel of rest, so it is no fresh press. Bevy's own camera
/// controllers, which read the input in the bridge, are not held.
/// </para>
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
public sealed partial class Input
{
    private readonly ulong[] _down = new ulong[NativeInput.KeyBitsetUlongs];
    private readonly ulong[] _pressed = new ulong[NativeInput.KeyBitsetUlongs];
    private readonly ulong[] _released = new ulong[NativeInput.KeyBitsetUlongs];
    private readonly ulong[] _held = new ulong[NativeInput.KeyBitsetUlongs];
    private bool _escapeTaken;
    private readonly HashSet<LogicalKey> _logicalDown = [];
    private readonly HashSet<LogicalKey> _logicalPressed = [];
    private readonly HashSet<LogicalKey> _logicalReleased = [];

    private uint _mouseDown;
    private uint _mousePressed;
    private string _text = string.Empty;
    private readonly Touch[] _touches = new Touch[NativeInput.TouchCapacity];
    private Gamepad[] _gamepads = [];
    private int _touchCount;
    private uint _mouseReleased;
    private float _mouseX, _mouseY, _mouseDeltaX, _mouseDeltaY, _wheelX, _wheelY;

    /// <summary>Cursor X in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseX => Held ? _heldAt.X : _mouseX;

    /// <summary>Cursor Y in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseY => Held ? _heldAt.Y : _mouseY;

    /// <summary>Cursor position.</summary>
    public (float X, float Y) MousePosition => (MouseX, MouseY);

    /// <summary>Cursor X movement since the previous frame.</summary>
    public float MouseDeltaX => Held ? 0f : _mouseDeltaX;

    /// <summary>Cursor Y movement since the previous frame.</summary>
    public float MouseDeltaY => Held ? 0f : _mouseDeltaY;

    /// <summary>Cursor movement since the previous frame.</summary>
    public (float X, float Y) MouseDelta => (MouseDeltaX, MouseDeltaY);

    /// <summary>Horizontal scroll since the previous frame.</summary>
    public float WheelX => Held ? 0f : _wheelX;

    /// <summary>Vertical scroll since the previous frame.</summary>
    public float WheelY => Held ? 0f : _wheelY;

    /// <summary>True while <paramref name="key"/> is held down.</summary>
    public bool KeyDown(Key key) => Seen(_down, key);

    /// <summary>
    /// True on the single frame <paramref name="key"/> went down, Escape not while the console, the
    /// settings panel or a field takes it.
    /// </summary>
    public bool KeyPressed(Key key) => Seen(_pressed, key);

    /// <summary>True on the single frame <paramref name="key"/> went up.</summary>
    public bool KeyReleased(Key key) => Seen(_released, key, _releasesHidden);

    /// <summary>True while the key that reads as <paramref name="key"/> is held down.</summary>
    /// <remarks>
    /// Bevy's <c>ButtonInput&lt;Key&gt;</c>, a key as the keyboard's layout reads it rather than
    /// where it is, as <see cref="LogicalKey"/> says.
    /// </remarks>
    public bool KeyDown(LogicalKey key) => !Held && _logicalDown.Contains(key) && !_logicalHeldOver.Contains(key);

    /// <summary>True on the single frame the key that reads as <paramref name="key"/> went down.</summary>
    public bool KeyPressed(LogicalKey key) => !Held && _logicalPressed.Contains(key) && !_logicalHeldOver.Contains(key);

    /// <summary>True on the single frame the key that reads as <paramref name="key"/> went up.</summary>
    public bool KeyReleased(LogicalKey key) => !Held && _logicalReleased.Contains(key) && !_logicalReleasesHidden.Contains(key);

    /// <summary>True while any key at all is held.</summary>
    public bool AnyKeyDown() => AnySeen(_down);

    /// <summary>True when any key at all went down this frame.</summary>
    public bool AnyKeyPressed() => AnySeen(_pressed);

    /// <summary>True while at least one of <paramref name="keys"/> is held.</summary>
    /// <remarks>
    /// Mirrors Bevy's <c>ButtonInput::any_pressed</c>. This is how you express a side-agnostic
    /// modifier by hand, as <c>AnyKeyDown([Key.ControlLeft, Key.ControlRight])</c>.
    /// </remarks>
    public bool AnyKeyDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (Seen(_down, key))
                return true;

        return false;
    }

    /// <summary>True while every one of <paramref name="keys"/> is held.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::all_pressed</c>. An empty span is true.</remarks>
    public bool AllKeysDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (!Seen(_down, key))
                return false;

        return true;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went down this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_pressed</c>.</remarks>
    public bool AnyKeyPressed(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (Seen(_pressed, key))
                return true;

        return false;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went up this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_released</c>.</remarks>
    public bool AnyKeyReleased(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (Seen(_released, key, _releasesHidden))
                return true;

        return false;
    }

    /// <summary>True while <paramref name="button"/> is held down.</summary>
    public bool MouseDown(MouseButton button) => (SeenMouse(_mouseDown, _mouseHeldOver) & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went down.</summary>
    public bool MousePressed(MouseButton button) => (SeenMouse(_mousePressed, _mouseHeldOver) & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went up.</summary>
    public bool MouseReleased(MouseButton button) => (SeenMouse(_mouseReleased, _mouseReleasesHidden) & Bit(button)) != 0;

    /// <summary>True while any mouse button is held.</summary>
    public bool AnyMouseDown() => SeenMouse(_mouseDown, _mouseHeldOver) != 0;

    /// <summary>True when any mouse button went down this frame.</summary>
    public bool AnyMousePressed() => SeenMouse(_mousePressed, _mouseHeldOver) != 0;

    // The buttons the program reads, none while a panel holds its input and none held over.
    private uint SeenMouse(uint buttons, uint hidden) => Held ? 0u : buttons & ~hidden;

    private static uint Bit(MouseButton button) => 1u << (int)button;

    private static bool TestBit(ulong[] bits, Key key)
    {
        var index = (int)key;
        if ((uint)index >= KeyTable.Count) return false;
        return (bits[index / 64] & (1UL << (index % 64))) != 0;
    }

    private static void SetBit(ulong[] bits, Key key, bool on)
    {
        var index = (int)key;
        if ((uint)index >= KeyTable.Count) return;
        if (on) bits[index / 64] |= 1UL << (index % 64);
        else bits[index / 64] &= ~(1UL << (index % 64));
    }

    // A key the interface took reads to the program as a key that was never touched, as every key
    // does while a panel holds the program's input and one held over as it ended, the key down or
    // pressed while it is held over and its release in the frame it comes up.
    private bool Seen(ulong[] bits, Key key) => Seen(bits, key, _heldOver);

    private bool Seen(ulong[] bits, Key key, ulong[] hidden) =>
        !Held && TestBit(bits, key) && !TestBit(_held, key) && !TestBit(hidden, key);

    private bool AnySeen(ulong[] bits)
    {
        if (Held) return false;
        for (var slot = 0; slot < bits.Length; slot++)
            if ((bits[slot] & ~_held[slot] & ~_heldOver[slot]) != 0)
                return true;

        return false;
    }

    /// <summary>Copies a native snapshot into this frame's state.</summary>
    internal unsafe void Update(NativeInput snapshot)
    {
        _mouseX = snapshot.MouseX;
        _mouseY = snapshot.MouseY;
        _mouseDeltaX = snapshot.MouseDeltaX;
        _mouseDeltaY = snapshot.MouseDeltaY;
        _wheelX = snapshot.WheelX;
        _wheelY = snapshot.WheelY;
        _mouseDown = snapshot.MouseDown;
        _mousePressed = snapshot.MousePressed;
        _mouseReleased = snapshot.MouseReleased;

        for (var slot = 0; slot < NativeInput.KeyBitsetUlongs; slot++)
        {
            _down[slot] = snapshot.KeysDown[slot];
            _pressed[slot] = snapshot.KeysPressed[slot];
            _released[slot] = snapshot.KeysReleased[slot];
        }

        UpdateTextAndTouches(snapshot);
    }

    /// <summary>Reads the logical keys held, pressed and released, at the same moment as the rest of the frame's input.</summary>
    /// <remarks>
    /// One call into the bridge a frame for them all. A keyboard holds a few keys down at once,
    /// so thirty-two is more than a hand presses, and any past them would be read as up.
    /// </remarks>
    internal unsafe void UpdateLogicalKeys()
    {
        const int Capacity = 32;
        var natives = stackalloc NativeLogicalKey[Capacity];
        var count = Math.Clamp(NativeCore.bcs_logical_keys(natives, Capacity), 0, Capacity);

        _logicalDown.Clear();
        _logicalPressed.Clear();
        _logicalReleased.Clear();

        for (var i = 0; i < count; i++)
        {
            var native = natives[i];
            var text = Encoding.UTF8.GetString(native.Text, Math.Min((int)native.Length, NativeLogicalKey.TextCapacity));
            var key = (LogicalKeyKind)native.Kind switch
            {
                LogicalKeyKind.Character => LogicalKey.Character(text),
                LogicalKeyKind.Dead => LogicalKey.Dead(text),
                _ => LogicalKey.Named(text),
            };

            if ((native.Flags & 1) != 0) _logicalDown.Add(key);
            if ((native.Flags & 2) != 0) _logicalPressed.Add(key);
            if ((native.Flags & 4) != 0) _logicalReleased.Add(key);
        }
    }

    /// <summary>
    /// Whether the interface takes Escape as it is pressed, the console or the settings panel open
    /// or a field with the caret, which the diagnostics area answers in each app it is built into.
    /// </summary>
    internal static Func<bool> InterfaceTakesEscape { get; set; } = () => false;

    /// <summary>
    /// Whether a keyboard message is kept from the program, as Escape's is while the interface
    /// takes it, its press deciding and its repeats and its release keeping to that, and a press
    /// while a panel holds the program's input, with the repeats and the release of a press the
    /// program did not read.
    /// </summary>
    /// <remarks>
    /// The press's message comes a frame before the key's state says it went down, so it decides
    /// for the state as well (<see cref="HoldForInterface"/>), and a panel the press closes in the
    /// frame between changes neither.
    /// </remarks>
    internal bool HoldsMessage(Key? key, ButtonState state, bool repeat)
    {
        if (key == Key.Escape)
        {
            if (state == ButtonState.Pressed && !repeat) _escapeTaken = InterfaceTakesEscape();
            if (_escapeTaken) return true;
        }

        return key is { } known ? HoldsKeyMessage(known, state, repeat) : Held;
    }

    /// <summary>
    /// True while <paramref name="key"/> is held down, a key the program is kept from included, for
    /// the interface that took it.
    /// </summary>
    internal bool InterfaceKeyDown(Key key) => TestBit(_down, key);

    /// <summary>
    /// Keeps a press of Escape from the program where the interface takes it, from the frame it
    /// went down to the frame it came up, its logical key with it, and reads whether a panel holds
    /// the program's input, letting go first of what was held over as each came up.
    /// </summary>
    /// <remarks>
    /// Escape's is decided once, as the key goes down, by its message where one came
    /// (<see cref="HoldsMessage"/>), so a panel the press closes does not hand the release to the
    /// program, which would read a key come up that it never saw go down, and a key held into a
    /// panel opening stays the program's. Called once the logical keys and the pads are read and
    /// before the frame's messages are.
    /// </remarks>
    internal void HoldForInterface()
    {
        HoldEscape();
        SettleHeldOver();
        SyncHold();
        SeeHeld();
    }

    // Escape's part of the hold for the interface.
    private void HoldEscape()
    {
        if (TestBit(_pressed, Key.Escape))
        {
            SetBit(_held, Key.Escape, _escapeTaken || InterfaceTakesEscape());
        }
        else if (!TestBit(_down, Key.Escape) && !TestBit(_released, Key.Escape))
        {
            SetBit(_held, Key.Escape, false);
            _escapeTaken = false;
        }

        if (!TestBit(_held, Key.Escape)) return;

        _logicalDown.Remove(LogicalKey.Escape);
        _logicalPressed.Remove(LogicalKey.Escape);
        _logicalReleased.Remove(LogicalKey.Escape);
    }

    /// <summary>The connected gamepads, in the order Bevy found them, as they stood at the start of this frame.</summary>
    /// <remarks>
    /// <para>
    /// Empty where none is connected, and never null. A pad connected or disconnected is also
    /// sent that frame as a <see cref="GamepadConnected"/> or a <see cref="GamepadDisconnected"/>,
    /// for a game that gives each player a pad as it arrives.
    /// </para>
    /// <para>
    /// The first pad is <c>Gamepads[0]</c> where there is one, and a game read by one player
    /// takes that and the keyboard together.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var pad in ctx.Input.Gamepads)
    /// {
    ///     move += pad.LeftStick;
    ///     if (pad.Pressed(GamepadButton.South)) Jump();
    /// }
    /// </code>
    /// </example>
    public IReadOnlyList<Gamepad> Gamepads => _seenGamepads;

    /// <summary>Reads the connected pads and sends what connected and disconnected since the last frame.</summary>
    /// <remarks>
    /// One call into the bridge a frame, for every pad at once. Sixteen is more pads than any
    /// platform connects to one machine, and a seventeenth would be read as missing rather than
    /// overrun anything.
    /// </remarks>
    internal unsafe void UpdateGamepads(MessageBus messages)
    {
        const int Capacity = 16;
        var natives = stackalloc NativeGamepad[Capacity];
        var count = NativeCore.bcs_gamepads(natives, Capacity);
        if (count < 0) count = 0;

        var now = new Gamepad[Math.Min(count, Capacity)];
        for (var i = 0; i < now.Length; i++) now[i] = new Gamepad(natives[i]);

        foreach (var pad in now)
        {
            if (!_gamepads.Any(was => was.Entity == pad.Entity)) messages.Send(new GamepadConnected(pad.Entity, pad.Name));
        }

        foreach (var was in _gamepads)
        {
            if (!now.Any(pad => pad.Entity == was.Entity)) messages.Send(new GamepadDisconnected(was.Entity));
        }

        _gamepads = now;
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
    /// same question as which keys are down, because it is the layout's answer rather than the
    /// hardware's, so a French keyboard gives what its user expects, and a dead key followed by a
    /// vowel gives one accented character.
    /// </para>
    /// <para>
    /// Control characters are left out. Backspace and Enter arrive as text on some platforms, and
    /// a field that inserted them as characters would be wrong on all of them; read those with
    /// <see cref="KeyPressed(Key)"/> instead.
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
    public string Text => Held ? string.Empty : _text;

    /// <summary>The touches in progress this frame.</summary>
    /// <remarks>
    /// A touch that ended is reported once, on the frame it ends, with
    /// <see cref="TouchPhase.Ended"/>, and one the platform canceled once with
    /// <see cref="TouchPhase.Canceled"/>. Nothing else reports either, so a release is missed by a
    /// system that skips a frame.
    /// </remarks>
    public ReadOnlySpan<Touch> Touches => _seenTouches.AsSpan(0, _seenTouchCount);

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

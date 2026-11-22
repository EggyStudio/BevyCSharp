using Bevy.Interop;

namespace Bevy;

public sealed partial class Input
{
    // While a panel that takes input is open, the console or the settings panel, every read a
    // program makes answers as though nothing were pressed or moved, the pointer standing where it
    // stood as the hold began, the pads at rest and no touch down, and the input's messages keep to
    // it, as below. The state as it came stays for the engine's own readers, the interface's ImGui
    // and the diagnostics' keys. As the hold ends, the keys, the buttons and the touches still down
    // stay unseen until they are let go, their releases with them, and an axis pushed past rest
    // until it comes back, so what a hand holds as a panel closes is no fresh press of the
    // program's.
    private readonly ulong[] _heldOver = new ulong[NativeInput.KeyBitsetUlongs];
    private readonly ulong[] _releasesHidden = new ulong[NativeInput.KeyBitsetUlongs];
    private readonly HashSet<LogicalKey> _logicalHeldOver = [];
    private readonly HashSet<LogicalKey> _logicalReleasesHidden = [];
    private uint _mouseHeldOver;
    private uint _mouseReleasesHidden;
    private readonly Dictionary<Entity, PadHeldOver> _padsHeldOver = [];
    private readonly HashSet<ulong> _touchesHeldOver = [];
    private Gamepad[] _seenGamepads = [];
    private readonly Touch[] _seenTouches = new Touch[NativeInput.TouchCapacity];
    private int _seenTouchCount;
    private (float X, float Y) _heldAt;

    // The messages keep to what the program read of them, the messages being a stream a program
    // builds its own state from where the reads are a picture of each frame. A press, a touch's
    // start or a value past rest is kept from the program while the hold lasts. A release or a
    // touch's end comes where the program read its press, at any time, and never where it did not,
    // and a value back at rest always comes, so a program reading the messages holds no key, button
    // or stick down that the hand has let go of.
    private readonly ulong[] _keysSent = new ulong[NativeInput.KeyBitsetUlongs];
    private uint _mouseSent;
    private readonly Dictionary<Entity, uint> _padButtonsSent = [];
    private readonly HashSet<ulong> _touchesSent = [];

    // How near rest an axis held as a panel closes comes back before the program reads it again, a
    // quarter of its travel, past a stick's drift and short of a push meant to move.
    private const float Rest = 0.25f;

    // Every axis of a pad at rest, as a pad reads while the hold lasts.
    private static readonly bool[] EveryAxis = [.. Enumerable.Repeat(true, Gamepad.AxisCount)];

    /// <summary>
    /// Whether a panel that takes input is open, the console or the settings panel and not the
    /// overlay, which the diagnostics area answers in each app it is built into.
    /// </summary>
    internal static Func<bool> InterfaceHoldsInput { get; set; } = () => false;

    /// <summary>Whether a panel holds the program's input, as last read.</summary>
    internal bool Held { get; private set; }

    /// <summary>
    /// Reads whether a panel holds the program's input, as the frame's input comes in and again
    /// once the areas' work has opened or closed a panel, so the stages after read one answer, and
    /// as the hold ends keeps what is still down from the program.
    /// </summary>
    /// <remarks>
    /// The bridge is told as well, since Bevy's own camera controllers read Bevy's input there and
    /// not this, and it holds them as this holds the program.
    /// </remarks>
    internal void SyncHold()
    {
        var held = InterfaceHoldsInput();
        if (held == Held) return;

        Held = held;
        _ = NativeCore.bcs_input_hold(held ? 1 : 0);
        if (held)
        {
            _heldAt = (_mouseX, _mouseY);
        }
        else
        {
            for (var slot = 0; slot < _heldOver.Length; slot++)
            {
                _heldOver[slot] |= _down[slot] | _pressed[slot];
                _releasesHidden[slot] |= _released[slot];
            }

            _logicalHeldOver.UnionWith(_logicalDown);
            _logicalHeldOver.UnionWith(_logicalPressed);
            _logicalReleasesHidden.UnionWith(_logicalReleased);
            _mouseHeldOver |= _mouseDown | _mousePressed;
            _mouseReleasesHidden |= _mouseReleased;
            foreach (var pad in _gamepads)
            {
                if (!_padsHeldOver.TryGetValue(pad.Entity, out var heldOver)) _padsHeldOver[pad.Entity] = heldOver = new PadHeldOver();
                heldOver.HoldOver(pad);
            }

            for (var i = 0; i < _touchCount; i++) _touchesHeldOver.Add(_touches[i].Id);
        }

        SeeHeld();
    }

    // Once a frame as its input comes in, before the hold is read, lets go of what was held over as
    // each comes up, its release kept from the program in the frame it comes up.
    private void SettleHeldOver()
    {
        for (var slot = 0; slot < _heldOver.Length; slot++)
        {
            _releasesHidden[slot] = _heldOver[slot] & _released[slot];
            _heldOver[slot] &= _down[slot];
        }

        _logicalReleasesHidden.Clear();
        foreach (var key in _logicalHeldOver)
            if (_logicalReleased.Contains(key)) _logicalReleasesHidden.Add(key);
        _logicalHeldOver.IntersectWith(_logicalDown);

        _mouseReleasesHidden = _mouseHeldOver & _mouseReleased;
        _mouseHeldOver &= _mouseDown;

        foreach (var (entity, heldOver) in _padsHeldOver)
        {
            var pad = Array.Find(_gamepads, pad => pad.Entity == entity);
            if (pad is null || heldOver.Settle(pad)) _padsHeldOver.Remove(entity);
        }

        foreach (var entity in _padButtonsSent.Keys)
            if (!Array.Exists(_gamepads, pad => pad.Entity == entity)) _padButtonsSent.Remove(entity);

        if (_touchesHeldOver.Count > 0) _touchesHeldOver.RemoveWhere(id => !InProgress(id));
    }

    // Whether a touch of this id is down this frame.
    private bool InProgress(ulong id)
    {
        for (var i = 0; i < _touchCount; i++)
            if (_touches[i].Id == id) return true;
        return false;
    }

    // The pads and the touches as the program reads them, at rest and none while the hold lasts,
    // and without what was held over once it ends.
    private void SeeHeld()
    {
        if (Held)
        {
            _seenGamepads = [.. _gamepads.Select(pad => pad.Seen(uint.MaxValue, uint.MaxValue, EveryAxis))];
            _seenTouchCount = 0;
            return;
        }

        _seenGamepads = _padsHeldOver.Count == 0
            ? _gamepads
            : [.. _gamepads.Select(pad => _padsHeldOver.TryGetValue(pad.Entity, out var heldOver) ? pad.Seen(heldOver.Down, heldOver.Releases, heldOver.Axes) : pad)];

        _seenTouchCount = 0;
        for (var i = 0; i < _touchCount; i++)
            if (!_touchesHeldOver.Contains(_touches[i].Id)) _seenTouches[_seenTouchCount++] = _touches[i];
    }

    // Whether a press, a start or a release is kept from the program, as the messages keep to what
    // the program read of them, the press noted where it reached the program.
    private bool HoldsTransition(bool released, bool sent, Action<bool> note)
    {
        if (released)
        {
            note(false);
            return !sent;
        }

        if (Held) return true;
        note(true);
        return false;
    }

    /// <summary>Whether a message of a mouse button is kept from the program.</summary>
    internal bool HoldsMouseMessage(MouseButton button, ButtonState state) =>
        HoldsTransition(state == ButtonState.Released, (_mouseSent & Bit(button)) != 0,
            on => _mouseSent = on ? _mouseSent | Bit(button) : _mouseSent & ~Bit(button));

    /// <summary>
    /// Whether a message of a pad's button going down or coming up is kept from the program.
    /// </summary>
    internal bool HoldsPadMessage(Entity pad, GamepadButton button, ButtonState state)
    {
        var bit = (uint)button < 32 ? 1u << (int)button : 0u;
        var sent = _padButtonsSent.GetValueOrDefault(pad);
        return HoldsTransition(state == ButtonState.Released, (sent & bit) != 0,
            on => _padButtonsSent[pad] = on ? sent | bit : sent & ~bit);
    }

    /// <summary>
    /// Whether a message of a pad's button's value is kept from the program, one past rest while
    /// the hold lasts or the button is held over as it ended.
    /// </summary>
    internal bool HoldsPadValue(Entity pad, GamepadButton button, float value)
    {
        if (MathF.Abs(value) <= Rest) return false;
        var bit = (uint)button < 32 ? 1u << (int)button : 0u;
        return Held || _padsHeldOver.TryGetValue(pad, out var heldOver) && (heldOver.Down & bit) != 0;
    }

    /// <summary>
    /// Whether a message of a pad's axis is kept from the program, one past rest while the hold
    /// lasts or the axis is held over as it ended.
    /// </summary>
    internal bool HoldsPadValue(Entity pad, GamepadAxis axis, float value)
    {
        if (MathF.Abs(value) <= Rest) return false;
        return Held || _padsHeldOver.TryGetValue(pad, out var heldOver) && (uint)axis < Gamepad.AxisCount && heldOver.Axes[(int)axis];
    }

    /// <summary>
    /// Whether a message of a touch is kept from the program, its moves while the hold lasts and
    /// every one of a touch whose start the program did not read.
    /// </summary>
    internal bool HoldsTouchMessage(ulong id, TouchPhase phase) => phase switch
    {
        TouchPhase.Started => HoldsTransition(false, false, on => _touchesSent.Add(id)),
        TouchPhase.Moved => Held || !_touchesSent.Contains(id),
        _ => HoldsTransition(true, _touchesSent.Contains(id), _ => _touchesSent.Remove(id)),
    };

    // A keyboard message of a key the platform names, past Escape's part, which keeps to what the
    // program read, a repeat coming only while the program reads the key down.
    private bool HoldsKeyMessage(Key key, ButtonState state, bool repeat)
    {
        if (repeat) return Held || !TestBit(_keysSent, key) || TestBit(_heldOver, key);
        return HoldsTransition(state == ButtonState.Released, TestBit(_keysSent, key), on => SetBit(_keysSent, key, on));
    }

    // The state as it came, for the interface and the diagnostics' keys, which a panel holding the
    // program's input leaves to them.

    /// <summary>The cursor where it is, a panel holding the program's input or not.</summary>
    internal (float X, float Y) InterfaceMouse => (_mouseX, _mouseY);

    /// <summary>
    /// True while <paramref name="button"/> is held down, a panel holding the program's input or
    /// not.
    /// </summary>
    internal bool InterfaceMouseDown(MouseButton button) => (_mouseDown & Bit(button)) != 0;

    /// <summary>
    /// The scroll since the previous frame, a panel holding the program's input or not.
    /// </summary>
    internal (float X, float Y) InterfaceWheel => (_wheelX, _wheelY);

    /// <summary>
    /// What the keyboard typed this frame, a panel holding the program's input or not.
    /// </summary>
    internal string InterfaceText => _text;

    /// <summary>
    /// True on the frame <paramref name="key"/> went down, a panel holding the program's input or
    /// not, and Escape not while the interface takes it, as the diagnostics' keys read it.
    /// </summary>
    internal bool UnheldKeyPressed(Key key) => TestBit(_pressed, key) && !TestBit(_held, key);

    /// <summary>
    /// True on the frame <paramref name="key"/> went up, a panel holding the program's input or
    /// not, and Escape not while the interface takes it, as the diagnostics' keys read it.
    /// </summary>
    internal bool UnheldKeyReleased(Key key) => TestBit(_released, key) && !TestBit(_held, key);

    /// <summary>
    /// What a pad held as a panel closed keeps from the program, its buttons until each is let
    /// go, their releases in the frame they come up, and its axes until each comes back to rest.
    /// </summary>
    private sealed class PadHeldOver
    {
        internal uint Down;
        internal uint Releases;
        internal readonly bool[] Axes = new bool[Gamepad.AxisCount];

        internal void HoldOver(Gamepad pad)
        {
            Down |= pad.DownButtons | pad.PressedButtons;
            Releases |= pad.ReleasedButtons;
            for (var i = 0; i < Axes.Length; i++) Axes[i] |= MathF.Abs(pad.Axis((GamepadAxis)i)) > Rest;
        }

        // Lets go of what came up this frame, and answers whether nothing is left.
        internal bool Settle(Gamepad pad)
        {
            Releases = Down & pad.ReleasedButtons;
            Down &= pad.DownButtons;
            for (var i = 0; i < Axes.Length; i++) Axes[i] &= MathF.Abs(pad.Axis((GamepadAxis)i)) > Rest;
            return Down == 0 && Releases == 0 && !Axes.Contains(true);
        }
    }
}

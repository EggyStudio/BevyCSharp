using System.Text;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class App
{
    /// <summary>Moves Bevy's input messages onto the message bus, each as its own message.</summary>
    /// <remarks>
    /// <para>
    /// Bevy reports each change of the keyboard, the mouse, a touch and a pad as a buffered
    /// message read through a cursor, which a C# system cannot hold, so the bridge drains them and
    /// they are posted here as the window's are, read with <c>ctx.Read&lt;KeyboardInput&gt;()</c>
    /// as Bevy's examples read theirs.
    /// </para>
    /// <para>
    /// A frame can carry more than one buffer's worth, a mouse polled a thousand times a second
    /// moving a dozen times in it, so the buffer is filled again until the bridge has no more.
    /// </para>
    /// </remarks>
    private static void PostInputMessages(MessageBus bus)
    {
        const int Capacity = 64;
        NativeInputMessage* buffer = stackalloc NativeInputMessage[Capacity];

        int count;
        do
        {
            count = Native.bcs_input_messages(buffer, Capacity);
            for (var i = 0; i < count; i++) Post(bus, ref buffer[i]);
        }
        while (count == Capacity);
    }

    // The bridge's flags, which optional fields hold something.
    private const uint Repeat = 1, HasText = 2, HasDelta = 4, HasForce = 8, HasVendor = 16, HasProduct = 32, Calibrated = 64, HasAltitude = 128;

    private static void Post(MessageBus bus, ref NativeInputMessage m)
    {
        var state = m.State == 1 ? ButtonState.Pressed : ButtonState.Released;
        var entity = new Entity(m.Entity);
        switch (m.Kind)
        {
            case 0:
                bus.Send(new KeyboardInput(
                    m.Code >= 0 && m.Code < KeyTable.Count ? (Key)m.Code : null,
                    LogicalOf(ref m),
                    state,
                    (m.Flags & HasText) != 0 ? Utf8(ref m.Text[0], m.TextLength) : null,
                    (m.Flags & Repeat) != 0));
                break;
            case 1:
                if (m.Code >= 0) bus.Send(new MouseButtonInput((MouseButton)m.Code, state, entity));
                break;
            case 2:
                bus.Send(new MouseMotion(new Vec2(m.X, m.Y)));
                break;
            case 3:
                bus.Send(new CursorMoved(entity, new Vec2(m.X, m.Y), (m.Flags & HasDelta) != 0 ? new Vec2(m.Z, m.W) : null));
                break;
            case 4:
                bus.Send(new MouseWheel(m.Code == 1 ? ScrollUnit.Pixel : ScrollUnit.Line, m.X, m.Y, entity, PhaseOf(m.State)));
                break;
            case 5:
                bus.Send(new PinchGesture(m.X));
                break;
            case 6:
                bus.Send(new RotationGesture(m.X));
                break;
            case 7:
                bus.Send(new DoubleTapGesture());
                break;
            case 8:
                TouchForce? force = (m.Flags & HasForce) == 0 ? null
                    : new TouchForce(m.Z, (m.Flags & Calibrated) != 0 ? m.W : null, (m.Flags & HasAltitude) != 0 ? m.U : null);
                bus.Send(new TouchInput(PhaseOf(m.Code), new Vec2(m.X, m.Y), entity, force, m.Id));
                break;
            case 9:
                bus.Send(ConnectionOf(ref m));
                break;
            case 10:
                if (m.Code >= 0) bus.Send(new GamepadAxisChangedEvent(entity, (GamepadAxis)m.Code, m.X));
                break;
            case 11:
                if (m.Code >= 0) bus.Send(new GamepadButtonChangedEvent(entity, (GamepadButton)m.Code, state, m.X));
                break;
            case 12:
                if (m.Code >= 0) bus.Send(new GamepadButtonStateChangedEvent(entity, (GamepadButton)m.Code, state));
                break;
            case 13:
                bus.Send(new GamepadEvent(ConnectionOf(ref m), null, null));
                break;
            case 14:
                if (m.Code >= 0) bus.Send(new GamepadEvent(null, new GamepadButtonChangedEvent(entity, (GamepadButton)m.Code, state, m.X), null));
                break;
            case 15:
                if (m.Code >= 0) bus.Send(new GamepadEvent(null, null, new GamepadAxisChangedEvent(entity, (GamepadAxis)m.Code, m.X)));
                break;
        }
    }

    // Bevy's TouchPhase as the bridge numbers it, started, moved, ended and canceled.
    private static TouchPhase PhaseOf(int phase) => phase switch
    {
        0 => TouchPhase.Started,
        1 => TouchPhase.Moved,
        2 => TouchPhase.Ended,
        _ => TouchPhase.Canceled,
    };

    // A key's logical key, one the platform could not identify reading as a named key with an
    // empty name, as a focused key's does.
    private static LogicalKey LogicalOf(ref NativeInputMessage m)
    {
        var logical = Utf8(ref m.Logical[0], m.LogicalLength);
        return m.LogicalKind switch
        {
            1 => LogicalKey.Character(logical),
            2 => LogicalKey.Dead(logical),
            3 => LogicalKey.Named(string.Empty),
            _ => LogicalKey.Named(logical),
        };
    }

    private static GamepadConnectionEvent ConnectionOf(ref NativeInputMessage m) => new(
        new Entity(m.Entity),
        m.Code == 1,
        m.Code == 1 ? Utf8(ref m.Name[0], m.NameLength) : null,
        (m.Flags & HasVendor) != 0 ? (ushort)(m.Id >> 16) : null,
        (m.Flags & HasProduct) != 0 ? (ushort)m.Id : null);

    private static string Utf8(ref byte first, byte length)
    {
        fixed (byte* at = &first) return Encoding.UTF8.GetString(at, length);
    }
}

using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// A fixed-size run of <see cref="NativeTouch"/>, laid out inline in the input snapshot.
/// </summary>
/// <remarks>
/// C# has no <c>fixed</c> buffer of a struct type, so the slots are spelled out. The array is
/// short by design, so this stays smaller than the machinery to avoid it.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTouchArray
{
#pragma warning disable CS0649 // Written by the bridge, never by C#.
    private NativeTouch _0;
    private NativeTouch _1;
    private NativeTouch _2;
    private NativeTouch _3;
    private NativeTouch _4;
    private NativeTouch _5;
    private NativeTouch _6;
    private NativeTouch _7;
#pragma warning restore CS0649

    /// <summary>The touch at <paramref name="index"/>.</summary>
    public NativeTouch this[int index]
    {
        get
        {
            if ((uint)index >= NativeInput.TouchCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));

            return System.Runtime.CompilerServices.Unsafe.Add(
                ref System.Runtime.CompilerServices.Unsafe.AsRef(in _0), index);
        }
    }
}

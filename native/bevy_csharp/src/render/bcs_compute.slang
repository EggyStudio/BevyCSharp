// What a Slang compute shader sees from the bridge, which is time and a few helpers.
//
// A shader reaches it with `import bcs_compute;`, and everything in it is under the `bcs_compute`
// namespace. What a dispatch carries is whatever the shader declares: numbers, buffers read and
// written, images read and written, samplers, in any number, each set from C# by its name. Time is
// in space 101, which the bridge moves to group one after the compile, so the shader's own globals
// keep group zero to themselves.
//
// A raw buffer (`RWByteAddressBuffer`) is packed the way C packs a struct, which is the way a C#
// struct with sequential layout is packed, so `element` and `store` read and write the same bytes
// `Shaders.CreateBuffer` and `Shaders.ReadBuffer` hand across. A `RWStructuredBuffer<T>` is laid
// out as WGSL lays out an array, where a `float3` takes sixteen bytes.

module bcs_compute;

public namespace bcs_compute
{

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

[[vk::binding(0, 101)]] public ConstantBuffer<Globals> globals;

/// The element at `index` of a raw buffer, read as an array of `T`.
public T element<T>(RWByteAddressBuffer buffer, uint index)
{
    return buffer.Load<T>(index * sizeof(T));
}

/// Writes `value` as the element at `index` of a raw buffer, treated as an array of `T`.
public void store<T>(RWByteAddressBuffer buffer, uint index, T value)
{
    buffer.Store<T>(index * sizeof(T), value);
}

/// How many elements of `T` a raw buffer holds.
public uint count<T>(RWByteAddressBuffer buffer)
{
    uint size;
    buffer.GetDimensions(size);
    return size / sizeof(T);
}

}

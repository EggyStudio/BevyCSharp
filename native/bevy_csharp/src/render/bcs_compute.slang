// What a Slang compute shader sees: the numbers it was dispatched with, four buffers it reads and
// writes, two images it writes, two it reads, and time.
//
// A shader reaches it with `import bcs_compute;`, and everything in it is under the `bcs_compute`
// namespace. Like `bcs_pass` it has its own copies of the structs it shares with `bcs`, because a
// material's bindings overlap these in number and Slang warns about each one on every compile.
//
// The buffers are raw, read and written with `element` and `store` or with the buffer's own
// `Load<T>` and `Store<T>`. Slang packs a raw buffer the way C packs a struct, which is the way a
// C# struct with sequential layout is packed, so a struct declared the same way on both sides
// agrees on every offset, and what `Shaders.ReadBuffer` hands back is those structs.

module bcs_compute;

public namespace bcs_compute
{

/// The sixty-four numbers a dispatch carries, as sixteen rows of four.
public struct Parameters
{
    public float4 rows[16];
};

/// Time, in the layout Bevy's `Globals` has.
public struct Globals
{
    /// Seconds since startup, wrapping to zero after an hour.
    public float time;
    /// Seconds since the previous frame.
    public float delta_time;
    public uint frame_count;
};

[[vk::binding(0, 0)]] public ConstantBuffer<Parameters> parameters;

[[vk::binding(1, 0)]] public RWByteAddressBuffer buffer0;
[[vk::binding(2, 0)]] public RWByteAddressBuffer buffer1;
[[vk::binding(3, 0)]] public RWByteAddressBuffer buffer2;
[[vk::binding(4, 0)]] public RWByteAddressBuffer buffer3;

[[vk::binding(5, 0)]] public ConstantBuffer<Globals> globals;

/// The images a dispatch writes, with `Store`: eight bits a channel, and a half float a channel.
[[vk::binding(6, 0)]] [[vk::image_format("rgba8")]] public WTexture2D<float4> image0;
[[vk::binding(7, 0)]] [[vk::image_format("rgba16f")]] public WTexture2D<float4> image1;

/// The images a dispatch reads, and the linear sampler for them.
[[vk::binding(8, 0)]] public Texture2D texture0;
[[vk::binding(9, 0)]] public Texture2D texture1;
[[vk::binding(10, 0)]] public SamplerState sampler0;

/// One row of four numbers, in the order the dispatch was given them.
public float4 row(uint index)
{
    return parameters.rows[index];
}

/// One number, counting across the rows.
public float number(uint index)
{
    return parameters.rows[index / 4][index % 4];
}

/// The element at `index` of a buffer, read as an array of `T`.
public T element<T>(RWByteAddressBuffer buffer, uint index)
{
    return buffer.Load<T>(index * sizeof(T));
}

/// Writes `value` as the element at `index` of a buffer, treated as an array of `T`.
public void store<T>(RWByteAddressBuffer buffer, uint index, T value)
{
    buffer.Store<T>(index * sizeof(T), value);
}

/// How many elements of `T` a buffer holds.
public uint count<T>(RWByteAddressBuffer buffer)
{
    uint size;
    buffer.GetDimensions(size);
    return size / sizeof(T);
}

}

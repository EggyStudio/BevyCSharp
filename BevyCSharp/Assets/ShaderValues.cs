namespace Bevy;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

/// <summary>Sets and reads a shader's values by the names it declares.</summary>
/// <remarks>
/// <para>
/// Every setter answers the target, so they chain. Each is only valid inside a system, and a value
/// reaches the shader on the next frame, costing a new bind group rather than a new pipeline.
/// Something that changes every frame for every material is cheaper computed from time in the
/// shader.
/// </para>
/// <para>
/// Numbers are checked for kind and shape, so a <c>float3</c> is set from a <see cref="Vector3"/>,
/// an <c>int</c> from an <see cref="int"/>, a <c>float4x4</c> from a <see cref="Matrix4x4"/>, and
/// an array from a span of its elements, which may be shorter than the array. A C# matrix is laid
/// out by rows, as a Slang <c>float4x4</c> is, so <c>mul(m, v)</c> in the shader is
/// <see cref="Vector4.Transform(Vector4, Matrix4x4)"/> with the matrix transposed.
/// </para>
/// </remarks>
public static unsafe class ShaderValues
{
    extension<T>(T target) where T : struct, IShaderValues
    {
        /// <summary>Sets a <c>float</c>.</summary>
        public T Set(string name, float value) => target.Numbers(name, ShaderScalar.Float, 1, &value, 1);

        /// <summary>Sets an <c>int</c>.</summary>
        public T Set(string name, int value) => target.Numbers(name, ShaderScalar.Int, 1, &value, 1);

        /// <summary>Sets a <c>uint</c>.</summary>
        public T Set(string name, uint value) => target.Numbers(name, ShaderScalar.UInt, 1, &value, 1);

        /// <summary>Sets a <c>bool</c>.</summary>
        public T Set(string name, bool value)
        {
            var word = value ? 1u : 0u;
            return target.Numbers(name, ShaderScalar.UInt, 1, &word, 1);
        }

        /// <summary>Sets a <c>float2</c>.</summary>
        public T Set(string name, Vector2 value) => target.Numbers(name, ShaderScalar.Float, 2, &value, 1);

        /// <summary>Sets a <c>float3</c>.</summary>
        public T Set(string name, Vector3 value) => target.Numbers(name, ShaderScalar.Float, 3, &value, 1);

        /// <summary>Sets a <c>float4</c>, which is also what a color is.</summary>
        public T Set(string name, Vector4 value) => target.Numbers(name, ShaderScalar.Float, 4, &value, 1);

        /// <summary>Sets a <c>float4</c> from a quaternion, as x, y, z and w.</summary>
        public T Set(string name, Quaternion value) => target.Numbers(name, ShaderScalar.Float, 4, &value, 1);

        /// <summary>Sets a <c>float4x4</c>.</summary>
        public T Set(string name, Matrix4x4 value) => target.Numbers(name, ShaderScalar.Float, 16, &value, 1);

        /// <summary>
        /// Sets an array of numbers, vectors or matrices from its first elements.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="TItem"/> is <see cref="float"/>, <see cref="int"/>,
        /// <see cref="uint"/>, one of the <see cref="Vector2"/> to <see cref="Vector4"/>,
        /// <see cref="Quaternion"/> or <see cref="Matrix4x4"/>. A struct is set with
        /// <c>SetBytes</c> instead, because its layout in the shader is not
        /// necessarily its layout in C#.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <typeparamref name="TItem"/> is not a number, a vector or a matrix, or the shader declares
        /// something else under the name.
        /// </exception>
        public T Set<TItem>(string name, ReadOnlySpan<TItem> items) where TItem : unmanaged
        {
            var (scalar, components) = ShapeOf<TItem>()
                ?? throw new ArgumentException(
                    $"{typeof(TItem).Name} is not a number, a vector or a matrix. A struct is set "
                    + "with SetBytes, laid out the way the shader lays it out.",
                    nameof(items));

            fixed (TItem* at = items)
            {
                return target.Numbers(name, scalar, components, at, items.Length);
            }
        }

        /// <summary>Sets an array of numbers, vectors or matrices from its first elements.</summary>
        public T Set<TItem>(string name, TItem[] items) where TItem : unmanaged =>
            target.Set(name, (ReadOnlySpan<TItem>)items);

        /// <summary>
        /// Sets numbers <paramref name="components"/> to an element, for the shapes C# has no type
        /// for: an <c>int2</c>, a <c>uint3</c>, a <c>float3x3</c>, or an array of any of them.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="TItem"/> is <see cref="float"/>, <see cref="int"/> or
        /// <see cref="uint"/>, and says which kind of number the shader holds. A matrix is given by
        /// rows, each padded to four, which is how a uniform holds it, so a <c>float3x3</c> is
        /// twelve numbers.
        /// </remarks>
        public T SetNumbers<TItem>(string name, int components, ReadOnlySpan<TItem> numbers) where TItem : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(components, 1);

            var scalar = ShapeOf<TItem>() is (var kind, 1)
                ? kind
                : throw new ArgumentException(
                    $"{typeof(TItem).Name} is not a float, an int or a uint.",
                    nameof(numbers));

            if (numbers.Length % components != 0)
            {
                throw new ArgumentException(
                    $"{numbers.Length} numbers are not a whole number of elements of {components}.",
                    nameof(numbers));
            }

            fixed (TItem* at = numbers)
            {
                return target.Numbers(name, scalar, components, at, numbers.Length / components);
            }
        }

        /// <summary>
        /// Copies bytes, as they are, to where a name is: a struct, an array of structs, or a whole
        /// <c>ConstantBuffer</c>.
        /// </summary>
        /// <remarks>
        /// A uniform is laid out with rules C# does not follow by itself. A <c>float3</c> starts on
        /// sixteen bytes, an array element takes a multiple of sixteen, and a struct is rounded up to
        /// sixteen. A C# struct matching it spells that padding out, and
        /// <see cref="ShaderMaterial.Program"/>'s <see cref="ShaderProgram.Layout"/> says where each
        /// field is.
        /// </remarks>
        public T SetBytes<TItem>(string name, ReadOnlySpan<TItem> items) where TItem : unmanaged
        {
            var bytes = MemoryMarshal.AsBytes(items);
            var (kind, id) = target.Target;

            fixed (byte* at = bytes)
            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_bytes(kind, id, named, at, bytes.Length),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>Sets a struct, as its bytes. See <c>SetBytes</c>.</summary>
        public T SetStruct<TItem>(string name, TItem value) where TItem : unmanaged =>
            target.SetBytes(name, new ReadOnlySpan<TItem>(&value, 1));

        /// <summary>
        /// Puts an image on a texture, at <paramref name="index"/> in an array of them, or takes it
        /// off again with <see cref="AssetHandle.None"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The image need not have finished loading, because the target holds a handle rather than
        /// pixels. Until it has, the texture reads a stand-in of its shape. The same goes for a
        /// texture nothing was put on, so a shader need not know which ones were given.
        /// </para>
        /// <para>
        /// An image of the wrong shape for the texture (a flat image on a <c>TextureCube</c>, say),
        /// or of a format the texture cannot read, is replaced by the stand-in with a warning in the
        /// log, because binding it would stop the renderer. A cubemap is made with
        /// <see cref="Render.MakeCubemap"/>, and an array or 3D texture with
        /// <see cref="Render.MakeTextureArray"/> and <see cref="Render.MakeVolume"/>. A
        /// <c>RWTexture</c> takes an image from <see cref="Shaders.CreateImage"/>.
        /// </para>
        /// <para>
        /// <paramref name="mip"/> binds one mip level of the image rather than all of them, so a
        /// shader building a pyramid can read the level above and write the next.
        /// </para>
        /// </remarks>
        public T SetTexture(string name, AssetHandle image, int index = 0, int mip = -1)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_image(kind, id, named, index, image.Key, mip),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>
        /// Puts a <see cref="RayScene"/> on an acceleration structure the shader traces, or takes it
        /// off again with <c>default</c>.
        /// </summary>
        public T SetRayScene(string name, RayScene scene)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_ray_scene(kind, id, named, scene.Key),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>
        /// Puts a buffer from <see cref="Shaders.CreateBuffer(int)"/> on a storage buffer, or takes
        /// it off again with <see cref="AssetHandle.None"/>.
        /// </summary>
        /// <remarks>
        /// One nothing was put on reads sixteen bytes of zeros, so a shader that reads its length
        /// sees an empty buffer rather than failing.
        /// </remarks>
        public T SetBuffer(string name, AssetHandle buffer)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_buffer(kind, id, named, buffer.Key),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>Says how a sampler reads, at <paramref name="index"/> in an array of them.</summary>
        /// <remarks>A sampler nothing was said about reads linear and repeating.</remarks>
        public T SetSampler(string name, SamplerSettings settings, int index = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            var config = new NativeSamplerConfig { Anisotropy = Math.Clamp(settings.Anisotropy, 1, 16) };
            config.Address[0] = AddressOf(settings.AddressU);
            config.Address[1] = AddressOf(settings.AddressV);
            config.Address[2] = AddressOf(settings.AddressW);
            config.Linear[0] = settings.Magnify == SamplerFilter.Linear ? 1 : 0;
            config.Linear[1] = settings.Minify == SamplerFilter.Linear ? 1 : 0;
            config.Linear[2] = settings.Mipmaps == SamplerFilter.Linear ? 1 : 0;

            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_sampler(kind, id, named, index, &config),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>
        /// Takes the value set under a name off, so the shader reads zeros or a stand-in there
        /// again.
        /// </summary>
        public T Unset(string name)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                Native.Check(Native.bcs_shader_unset(kind, id, named), $"taking {name} off a shader");
            }

            return target;
        }

        /// <summary>
        /// The floats set under a name, or an empty array where nothing was.
        /// </summary>
        /// <remarks>
        /// What was set rather than what the shader holds, so a name set as a
        /// <see cref="Vector4"/> reads back as four floats, and one never set reads back empty
        /// although the shader reads zeros there.
        /// </remarks>
        public float[] GetFloats(string name) => ShaderValues.Read<float>(target.Target, name);

        /// <summary>The signed integers set under a name. See <c>GetFloats</c>.</summary>
        public int[] GetInts(string name) => ShaderValues.Read<int>(target.Target, name);

        /// <summary>The unsigned integers set under a name. See <c>GetFloats</c>.</summary>
        public uint[] GetUInts(string name) => ShaderValues.Read<uint>(target.Target, name);

        /// <summary>
        /// What the target's program declares, one entry per name, or none before it has compiled.
        /// </summary>
        /// <remarks>What an inspector draws a widget for each of.</remarks>
        public IReadOnlyList<ShaderParameter> Parameters
        {
            get
            {
                var (kind, id) = target.Target;
                var text = Native.ReadText(
                    (buffer, capacity) => Native.bcs_shader_target_names(kind, id, buffer, capacity),
                    "reading what a shader declares");

                return ShaderParameter.Parse(text);
            }
        }

        /// <summary>
        /// Sets numbers under a name from memory: <paramref name="count"/> elements of
        /// <paramref name="components"/> four-byte numbers each.
        /// </summary>
        internal T Numbers(string name, ShaderScalar scalar, int components, void* data, int count)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_numbers(kind, id, named, (int)scalar, components, (byte*)data, count),
                    name,
                    target);
            }

            return target;
        }
    }

    /// <summary>Reads back the four-byte numbers set under a name.</summary>
    internal static TItem[] Read<TItem>((int Kind, long Id) target, string name) where TItem : unmanaged
    {
        var named = Utf8(name);

        fixed (byte* at = named)
        {
            var length = Native.Check(
                Native.bcs_shader_get_numbers(target.Kind, target.Id, at, null, 0),
                $"reading {name} from a shader");

            var items = new TItem[length / sizeof(TItem)];

            fixed (TItem* into = items)
            {
                Native.Check(
                    Native.bcs_shader_get_numbers(target.Kind, target.Id, at, (byte*)into, items.Length * sizeof(TItem)),
                    $"reading {name} from a shader");
            }

            return items;
        }
    }

    /// <summary>The number of four-byte numbers in one <typeparamref name="TItem"/>, and their kind.</summary>
    internal static (ShaderScalar, int)? ShapeOf<TItem>() where TItem : unmanaged
    {
        if (typeof(TItem) == typeof(float)) return (ShaderScalar.Float, 1);
        if (typeof(TItem) == typeof(int)) return (ShaderScalar.Int, 1);
        if (typeof(TItem) == typeof(uint)) return (ShaderScalar.UInt, 1);
        if (typeof(TItem) == typeof(Vector2)) return (ShaderScalar.Float, 2);
        if (typeof(TItem) == typeof(Vector3)) return (ShaderScalar.Float, 3);
        if (typeof(TItem) == typeof(Vector4)) return (ShaderScalar.Float, 4);
        if (typeof(TItem) == typeof(Quaternion)) return (ShaderScalar.Float, 4);
        if (typeof(TItem) == typeof(Matrix4x4)) return (ShaderScalar.Float, 16);
        return null;
    }

    /// <summary>A name as NUL-terminated UTF-8.</summary>
    internal static byte[] Utf8(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var bytes = new byte[Encoding.UTF8.GetByteCount(name) + 1];
        Encoding.UTF8.GetBytes(name, bytes);
        return bytes;
    }

    /// <summary>
    /// Turns a refused value into an exception that says why, in the bridge's words, and any other
    /// failure into the usual one.
    /// </summary>
    internal static void Refuse<T>(int status, string name, T target) where T : IShaderValues
    {
        if (status == NativeStatus.InvalidState)
        {
            var reason = Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_last_error(buffer, capacity),
                "reading why a shader value was refused");

            throw new ArgumentException(reason.Length > 0 ? reason : $"{name} was not set.", nameof(name));
        }

        if (status == NativeStatus.NoComponent)
        {
            throw new BevyNativeException(
                status,
                target is ShaderMaterial { IsEntity: true }
                    ? $"Setting {name} found no shader material on the entity."
                    : $"Setting {name} found nothing to set it on. A material or an instance belongs "
                    + "to the app that made it.");
        }

        Native.Check(status, $"setting {name} on a shader");
    }

    private static int AddressOf(SamplerAddress address) => address switch
    {
        SamplerAddress.Clamp => 0,
        SamplerAddress.Mirror => 2,
        _ => 1,
    };
}

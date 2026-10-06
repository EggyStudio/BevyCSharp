namespace Bevy;

using System.Runtime.InteropServices;
using Bevy.Interop;

public static unsafe partial class Shaders
{
    /// <summary>
    /// Makes a buffer of <paramref name="size"/> bytes that shaders read and write, holding zeros.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A buffer lives on the GPU. A compute shader writes it, the next frame's dispatch reads what
    /// the last one wrote, and a material or a pass bound to it draws from it, all without anything
    /// crossing back to the CPU. <see cref="BeginBufferRead"/> brings it back when something on the
    /// CPU needs to know. It is bound to any <c>StructuredBuffer</c>, <c>RWStructuredBuffer</c> or
    /// <c>ByteAddressBuffer</c> by name.
    /// </para>
    /// <para>
    /// Its size is fixed, rounded up to a whole number of four-byte words and never less than
    /// sixteen, because a buffer that grew would be a new GPU buffer and whatever was bound to the
    /// old one would go on reading it.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateBuffer(int size) => CreateBuffer<byte>([], size);

    /// <summary>
    /// Makes a buffer holding <paramref name="items"/>, at least <paramref name="size"/> bytes long.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A <c>StructuredBuffer&lt;T&gt;</c> is laid out with the storage rules, where a
    /// <c>float3</c> takes sixteen bytes, and a <c>ByteAddressBuffer</c> read with <c>Load&lt;T&gt;</c>
    /// packs the way a C# struct with sequential layout does. A struct shared with a structured
    /// buffer therefore spells its padding out, and one shared with a byte address buffer needs
    /// nothing.
    /// </remarks>
    public static AssetHandle CreateBuffer<T>(ReadOnlySpan<T> items, int size = 0) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        var bytes = MemoryMarshal.AsBytes(items);

        fixed (byte* at = bytes)
        {
            return new AssetHandle(Native.Check(
                Native.bcs_shader_buffer_create(at, bytes.Length, size),
                $"making a shader buffer of {Math.Max(size, bytes.Length)} bytes"));
        }
    }

    /// <summary>
    /// Replaces a buffer's contents with <paramref name="items"/>, padded with zeros to its size.
    /// Only valid inside a system.
    /// </summary>
    /// <exception cref="ArgumentException">The items are larger than the buffer.</exception>
    public static void WriteBuffer<T>(AssetHandle buffer, ReadOnlySpan<T> items) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(items);
        int status;

        fixed (byte* at = bytes)
        {
            status = Native.bcs_shader_buffer_write(buffer.Key, at, bytes.Length);
        }

        if (status == NativeStatus.BufferTooSmall)
        {
            throw new ArgumentException(
                $"{bytes.Length} bytes do not fit a buffer of {BufferSize(buffer)}, and a buffer's "
                + "size is fixed when it is made.",
                nameof(items));
        }

        Native.Check(status, "writing a shader buffer");
    }

    /// <summary>
    /// Makes a buffer at least <paramref name="size"/> bytes, keeping what it holds, and answers
    /// its size. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A GPU buffer cannot grow in place, so this is a new one with the old one's contents copied
    /// to its start on the GPU, and the rest zeros. What the GPU wrote since the last
    /// <see cref="WriteBuffer{T}"/> is kept, since the copy is made there rather than from here.
    /// </para>
    /// <para>
    /// Every material and every shader instance holding the buffer is built against the new one, so
    /// there is nothing to hand out again. A size no larger than the buffer's leaves it as it is,
    /// since a buffer never shrinks.
    /// </para>
    /// </remarks>
    public static int GrowBuffer(AssetHandle buffer, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        return Native.Check(Native.bcs_shader_buffer_grow(buffer.Key, size), "growing a shader buffer");
    }

    /// <summary>How many bytes one slot of an instance buffer takes.</summary>
    /// <remarks>Two matrices of four columns: this frame's transform, then the previous frame's.</remarks>
    public const int InstanceSlotBytes = 128;

    /// <summary>
    /// Makes a buffer with <paramref name="capacity"/> slots that the engine fills every frame with
    /// the transforms of the entities put in them, this frame's and the previous frame's. Only
    /// valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Culling instances on the GPU, voxelizing a scene, drawing geometry a shader places and
    /// giving it motion each need every instance's transform in a buffer, with last frame's beside
    /// it. The engine writes both once transforms have been worked out each frame, and only when
    /// something moved.
    /// </para>
    /// <para>
    /// A shader reads it as a <c>StructuredBuffer&lt;bcs_scene::Instance&gt;</c> after
    /// <c>import bcs_scene;</c>, with <c>to_world</c>, <c>to_previous_world</c> and
    /// <c>position</c> to read a slot. It is bound by name like any other buffer.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateInstanceBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        return new AssetHandle(Native.Check(
            Native.bcs_shader_instance_buffer_create(capacity),
            $"making an instance buffer of {capacity} slots"));
    }

    /// <summary>
    /// Makes an empty geometry pool, buffers holding the vertices and triangles of every mesh added
    /// to it, which any shader reaches by a mesh's number and a triangle's. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ray traced in a compute shader, or a scene voxelized by one, meets triangles of whatever
    /// mesh is there, so every mesh it might meet has to be in buffers it can index rather than in
    /// the vertex buffers only a draw of that mesh binds. A shader reads
    /// <see cref="GeometryPool.Vertices"/> as a <c>StructuredBuffer&lt;bcs_scene::PoolVertex&gt;</c>,
    /// <see cref="GeometryPool.Indices"/> as a <c>StructuredBuffer&lt;uint&gt;</c> and
    /// <see cref="GeometryPool.Meshes"/> as a <c>StructuredBuffer&lt;bcs_scene::PoolMesh&gt;</c>, and
    /// <c>bcs_scene::pool_corner</c>, <c>intersect_triangle</c> and <c>intersect_box</c> do the rest.
    /// </para>
    /// <para>
    /// Put an entity's mesh in a pool, and the entity in the same slot of an instance buffer and a
    /// material buffer, and a shader has where its triangles are, how they have moved and what they
    /// are made of, which shading a ray's hit takes.
    /// </para>
    /// </remarks>
    public static GeometryPool CreateGeometryPool()
    {
        var keys = stackalloc int[3];
        Native.Check(Native.bcs_shader_geometry_pool_create(keys), "making a geometry pool");
        return new GeometryPool(new AssetHandle(keys[0]), new AssetHandle(keys[1]), new AssetHandle(keys[2]));
    }

    /// <summary>
    /// Adds a mesh to a geometry pool and answers its number there, counted from zero, which is the
    /// index of its <c>bcs_scene::PoolMesh</c>. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The mesh has to be triangles and has to have loaded; one still loading is refused rather than
    /// waited for, so a mesh from a file is added once <see cref="AssetServer"/> says it has loaded.
    /// Its normals and texture coordinates come along where it has them. The pool's buffers grow as
    /// meshes are added, and everything holding them is built against the grown ones.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The mesh has not loaded, is not triangles, or the handle names no mesh.
    /// </exception>
    public static int AddToGeometryPool(GeometryPool pool, AssetHandle mesh) =>
        Native.Check(Native.bcs_shader_geometry_pool_add(pool.Meshes.Key, mesh.Key), "adding a mesh to a geometry pool");

    /// <summary>
    /// Whether this device can build a <see cref="RayScene"/> and trace rays through one, which
    /// takes hardware ray tracing and a Vulkan backend. Only valid inside a system.
    /// </summary>
    public static bool SupportsRayQueries => Native.bcs_shader_ray_queries_supported() != 0;

    /// <summary>
    /// Makes a scene rays are traced through, over the meshes of a geometry pool, with
    /// <paramref name="capacity"/> slots for the entities in it. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compute shader compiled to SPIR-V (<see cref="ShaderProgramSettings.ComputeTarget"/>)
    /// declares a <c>RaytracingAccelerationStructure</c>, is handed the scene by name with
    /// <c>SetRayScene</c>, and traces it with <c>bcs_ray::trace_in</c> and <c>visible_in</c>. It is
    /// independent of the scene Solari keeps, so it runs without ray-traced lighting, and it can
    /// hold whatever a technique needs its rays to meet: simpler stand-ins for what is drawn, a
    /// coarser level of detail, or only what should cast a shadow.
    /// </para>
    /// <para>
    /// Each pool mesh is built into a structure of its own once, the first frame the pool's
    /// buffers hold it, and the scene is built again every frame from where its entities are, so
    /// moving an entity moves what rays meet with nothing else to do. A hit's
    /// <c>instance</c> is the slot, which an instance buffer or a material buffer with the same
    /// entities in the same slots describes, and its <c>mesh</c> is the pool mesh, whose triangle
    /// <c>bcs_scene::pool_corner</c> reads.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The device cannot trace rays (see <see cref="SupportsRayQueries"/>), or the pool is gone.
    /// </exception>
    public static RayScene CreateRayScene(GeometryPool pool, int capacity) =>
        new(Native.Check(
            Native.bcs_shader_ray_scene_create(pool.Meshes.Key, capacity),
            $"making a ray scene of {capacity} slots"));

    /// <summary>
    /// Puts an entity made of pool mesh <paramref name="mesh"/> in a slot of a ray scene, where
    /// rays meet it at its transform every frame, or empties the slot with
    /// <see cref="Entity.None"/>. Only valid inside a system.
    /// </summary>
    /// <param name="scene">The scene.</param>
    /// <param name="slot">Which slot, which a hit on it reports as its instance.</param>
    /// <param name="entity">The entity whose transform places it.</param>
    /// <param name="mesh">What <see cref="AddToGeometryPool"/> answered for its mesh.</param>
    public static void SetRaySceneInstance(RayScene scene, int slot, Entity entity, int mesh) =>
        Native.Check(
            Native.bcs_shader_ray_scene_set(scene.Key, slot, entity.Bits, entity == Entity.None ? -1 : mesh),
            $"putting {entity} in slot {slot} of a ray scene");

    /// <summary>
    /// Builds a ray scene's pool mesh again from what the pool's buffers hold now, or every mesh
    /// with <see langword="null"/>. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Each mesh is built into a structure of its own once, and that structure keeps its own copy of
    /// the triangles, so a mesh a compute shader deforms in the pool is traced as it was until this
    /// is asked. It happens while the next frame is prepared, before that frame's dispatches, so it
    /// takes in what the dispatches of the frame it was asked in wrote. Building is the slow part of
    /// a ray scene, so a mesh that moves every frame is worth asking for only as often as its rays
    /// need to see it move.
    /// </remarks>
    /// <exception cref="BevyNativeException">The scene is gone, or the pool has no such mesh.</exception>
    public static void RebuildRayScene(RayScene scene, int? mesh = null) =>
        Native.Check(
            Native.bcs_shader_ray_scene_rebuild(scene.Key, mesh ?? -1),
            "building a ray scene's meshes again");

    /// <summary>How many bytes one slot of a material buffer takes.</summary>
    /// <remarks>
    /// Base color and emissive color, four floats each, then roughness, metallic, reflectance and
    /// one for unlit.
    /// </remarks>
    public const int MaterialSlotBytes = 48;

    /// <summary>
    /// Makes a buffer with <paramref name="capacity"/> slots that the engine fills every frame with
    /// what the entities put in them are made of, from their standard materials. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ray that hits something has to know its color to bounce light off it, and world-space GI
    /// and traced reflections shade their hits with it. The engine reads it from each entity's
    /// material rather than a package guessing it, and writes it only when something changed. Slots
    /// are set with <see cref="SetInstance"/>, and putting an entity in the same slot of an
    /// instance buffer and a material buffer gives a shader its transform and its material by one
    /// index.
    /// </para>
    /// <para>
    /// A shader reads it as a <c>StructuredBuffer&lt;bcs_scene::Material&gt;</c>. Textures are not
    /// in it, since the base color is the factor the material multiplies its texture by, and an
    /// entity drawn by a shader program, or with no material, holds zeros.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateMaterialBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        return new AssetHandle(Native.Check(
            Native.bcs_shader_material_buffer_create(capacity),
            $"making a material buffer of {capacity} slots"));
    }

    /// <summary>
    /// Puts an entity in a slot of an instance or material buffer, or with
    /// <see cref="Entity.None"/> empties the slot. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// An entity put in a slot starts with no motion, so its previous transform is its current
    /// one, rather than wherever the slot's last entity was. An empty slot holds zeros.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The slot is past the buffer's capacity.</exception>
    public static void SetInstance(AssetHandle buffer, int slot, Entity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        var status = Native.bcs_shader_instance_buffer_set(buffer.Key, slot, entity == Entity.None ? 0 : entity.Bits);

        if (status == NativeStatus.NotPresent)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot {slot} is past the instance buffer's capacity.");
        }

        Native.Check(status, $"putting entity {entity} in slot {slot} of an instance buffer");
    }

    /// <summary>A buffer's size in bytes. Only valid inside a system.</summary>
    public static int BufferSize(AssetHandle buffer) =>
        Native.Check(Native.bcs_shader_buffer_size(buffer.Key), "reading a shader buffer's size");

    /// <summary>
    /// Starts copying a buffer back from the GPU. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The copy is of the buffer as it stands once this frame's dispatches have run, and it arrives
    /// a frame or two later, which <see cref="TryReadBuffer"/> is asked each frame until it says
    /// so. Any readback costs that latency, so work whose answer is needed every frame is better
    /// kept on the GPU.
    /// </remarks>
    public static BufferRead BeginBufferRead(AssetHandle buffer) =>
        new(Native.Check(Native.bcs_shader_buffer_read(buffer.Key), "reading a shader buffer back"));

    /// <summary>
    /// Starts copying an image back from the GPU, as <see cref="BeginBufferRead"/> copies a buffer.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The texels arrive through <see cref="TryReadBuffer"/> a frame or two later, row after row in
    /// the image's own format, four bytes a texel for <see cref="ShaderImageFormat.Rgba8"/> or
    /// <see cref="ShaderImageFormat.R32UInt"/>, with no padding between rows. For what a compute
    /// shader wrote into an image of its own, which lives on the GPU alone.
    /// </para>
    /// <para>
    /// A picture a camera drew is read with <see cref="Render.BeginCapture()"/>, which hands it
    /// over as colors. A block-compressed image has no size a texel and is refused.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The image is not loaded, or is block-compressed.</exception>
    public static BufferRead BeginImageRead(AssetHandle image) =>
        new(Native.Check(Native.bcs_shader_image_read(image.Key), "reading an image back"));

    /// <summary>
    /// The bytes a read brought back, once they have arrived. Only valid inside a system.
    /// </summary>
    /// <returns>True once the bytes have arrived, after which the read is spent.</returns>
    public static bool TryReadBuffer(BufferRead read, out byte[]? bytes)
    {
        bytes = null;

        const int Probe = 4096;
        byte* probe = stackalloc byte[Probe];
        var length = Native.bcs_shader_buffer_take(read.Ticket, probe, Probe);

        if (length == NativeStatus.NotPresent) return false;
        Native.Check(length, "taking what a shader buffer read brought back");

        if (length <= Probe)
        {
            bytes = new ReadOnlySpan<byte>(probe, length).ToArray();
            return true;
        }

        var whole = new byte[length];

        fixed (byte* target = whole)
        {
            Native.Check(
                Native.bcs_shader_buffer_take(read.Ticket, target, whole.Length),
                "taking what a shader buffer read brought back");
        }

        bytes = whole;
        return true;
    }

    /// <summary>
    /// The elements a read brought back, once they have arrived. Only valid inside a system.
    /// </summary>
    public static bool TryReadBuffer<T>(BufferRead read, out T[]? items) where T : unmanaged
    {
        items = null;
        if (!TryReadBuffer(read, out var bytes) || bytes is null) return false;

        items = MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        return true;
    }

    /// <summary>
    /// Makes an image a compute shader writes and anything samples. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compute shader declares it as a <c>RWTexture2D</c> or <c>RWTexture3D</c> with a
    /// <c>[format(...)]</c> matching <paramref name="format"/>, and it is bound there by name. The
    /// same image put on a material or a pass as a texture draws what the dispatch wrote, which is
    /// how a compute shader paints a water surface, a noise field or a simulation into a picture.
    /// </para>
    /// <para>
    /// It starts transparent black, and its pixels live on the GPU, so a dispatch that writes it
    /// every frame costs nothing crossing the boundary.
    /// </para>
    /// <para>
    /// With <paramref name="mips"/> above one it has that many mip levels, as many as its size
    /// allows, and a level is bound on its own by the <c>mip</c> of <c>SetTexture</c>, so one
    /// dispatch reads a level while the next writes the level below, which is how a depth pyramid
    /// or a blur chain is built. Bound without a level, a texture reads every level and a storage
    /// image writes the first.
    /// </para>
    /// </remarks>
    /// <param name="width">Its width in pixels.</param>
    /// <param name="height">Its height in pixels.</param>
    /// <param name="format">What it holds per pixel.</param>
    /// <param name="depth">Above one, a 3D image this many slices deep.</param>
    /// <param name="mips">How many mip levels, one for the image alone.</param>
    public static AssetHandle CreateImage(
        uint width,
        uint height,
        ShaderImageFormat format = ShaderImageFormat.Rgba8,
        uint depth = 1,
        uint mips = 1)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(depth);

        return new AssetHandle(Native.Check(
            Native.bcs_shader_image_create(width, height, depth, (int)format, Math.Max(1u, mips)),
            $"making a {width}x{height}x{depth} image for a compute shader"));
    }

    /// <summary>
    /// Makes an image as <see cref="CreateImage(uint, uint, ShaderImageFormat, uint, uint)"/> does,
    /// starting with <paramref name="texels"/> rather than zeros. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The texels are in the format's own layout, row after row and slice after slice, so a
    /// heightmap in <see cref="ShaderImageFormat.R32Float"/> is a float per texel and one in
    /// <see cref="ShaderImageFormat.Rgba16Float"/> is four halves. A picture whose numbers are not
    /// colors needs this, where eight bits a channel would lose them.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The texels are not exactly the image's size in bytes.
    /// </exception>
    public static AssetHandle CreateImage<T>(
        uint width,
        uint height,
        ShaderImageFormat format,
        ReadOnlySpan<T> texels,
        uint depth = 1) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(depth);

        var bytes = MemoryMarshal.AsBytes(texels);
        // A compressed image is counted in four by four blocks, which is how its data is laid out.
        var wanted = format >= ShaderImageFormat.Bc1
            ? (long)(width / 4) * (height / 4) * depth * TexelBytes(format)
            : (long)width * height * depth * TexelBytes(format);

        if (bytes.Length != wanted)
        {
            throw new ArgumentException(
                $"A {width}x{height}x{depth} {format} image is {wanted} bytes, and {bytes.Length} were given.",
                nameof(texels));
        }

        fixed (byte* at = bytes)
        {
            return new AssetHandle(Native.Check(
                Native.bcs_shader_image_create_from(width, height, depth, (int)format, at, bytes.Length),
                $"making a {width}x{height}x{depth} {format} image"));
        }
    }

    /// <summary>
    /// Writes texels into a region of an image, <paramref name="width"/> by
    /// <paramref name="height"/> at <paramref name="x"/>, <paramref name="y"/>, on the GPU before
    /// this frame's work runs. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A texture streamer uploads a tile into its cache with this, and an image changed a piece at
    /// a time uses it rather than being made again. The texels are in the image format's own
    /// layout, row after row and slice after slice, as for <see cref="CreateImage{T}"/>, and
    /// <paramref name="depth"/> slices from <paramref name="z"/> reach into a 3D image or the
    /// layers of an array. <paramref name="mip"/> picks the level, whose size is the image's halved
    /// that many times.
    /// </para>
    /// <para>
    /// Only the GPU's copy changes, which is all a shader reads. Writes land in the order they are
    /// asked for, and one made before the image is on the GPU waits for it.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The region is outside the level, or the texels are not exactly its size in bytes.
    /// </exception>
    public static void WriteImage<T>(
        AssetHandle image,
        ReadOnlySpan<T> texels,
        uint x,
        uint y,
        uint width,
        uint height,
        uint z = 0,
        uint depth = 1,
        uint mip = 0) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(texels);

        fixed (byte* at = bytes)
        {
            Native.Check(
                Native.bcs_shader_image_write(image.Key, x, y, z, width, height, depth, mip, at, bytes.Length),
                $"writing a {width}x{height}x{depth} region of an image");
        }
    }

    /// <summary>
    /// How many bytes one texel of <paramref name="format"/> takes, or for a block-compressed
    /// format one four by four block.
    /// </summary>
    public static int TexelBytes(ShaderImageFormat format) => format switch
    {
        ShaderImageFormat.Rgba8 or ShaderImageFormat.R32Float or ShaderImageFormat.R32UInt
            or ShaderImageFormat.R32Int or ShaderImageFormat.Rgba8UInt => 4,
        ShaderImageFormat.Rgba16Float or ShaderImageFormat.Rg32Float => 8,
        ShaderImageFormat.Rgba32Float or ShaderImageFormat.Rgba32UInt => 16,
        ShaderImageFormat.R16Float => 2,
        ShaderImageFormat.Bc1 or ShaderImageFormat.Bc4 => 8,
        ShaderImageFormat.Bc5 or ShaderImageFormat.Bc7 or ShaderImageFormat.Bc7Srgb or ShaderImageFormat.Bc6hFloat => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private static void RequireProgram(ShaderProgram program, string parameter)
    {
        if (!program.IsValid)
        {
            throw new ArgumentException(
                "No shader program was given. Make one with Shaders.CreateProgram.",
                parameter);
        }
    }

    private static void ThrowIfNoProgram(int status, ShaderProgram program)
    {
        if (status == NativeStatus.InvalidState)
        {
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"There is no shader program {program.Id} in the running app. A program belongs "
                + "to the app that made it.");
        }
    }
}

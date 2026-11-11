namespace Bevy;

/// <summary>A compute shader a camera runs every frame. See <see cref="Shaders.SetViewDispatches"/>.</summary>
/// <remarks>Made with <see cref="PerPixel"/>, <see cref="Fixed"/> or <see cref="Indirect"/>.</remarks>
public readonly record struct ViewDispatch
{
    /// <summary>The instance whose program and values run.</summary>
    public ShaderInstance Instance { get; init; }

    /// <summary>Where in the camera's frame.</summary>
    public FramePoint Point { get; init; }

    /// <summary>How the workgroups are counted.</summary>
    public ViewDispatchMode Mode { get; init; }

    /// <summary>The workgroup's width in pixels, or the workgroups across.</summary>
    public uint X { get; init; }

    /// <summary>The workgroup's height in pixels, or the workgroups down.</summary>
    public uint Y { get; init; }

    /// <summary>The workgroups deep, for a fixed dispatch.</summary>
    public uint Z { get; init; }

    /// <summary>The fraction of the picture a per-pixel dispatch covers.</summary>
    public float Scale { get; init; }

    /// <summary>The buffer an indirect dispatch reads its counts from.</summary>
    public AssetHandle Buffer { get; init; }

    /// <summary>Where in the buffer the counts start, in bytes.</summary>
    public uint Offset { get; init; }

    /// <summary>
    /// Enough workgroups of <paramref name="groupX"/> by <paramref name="groupY"/> pixels to cover
    /// <paramref name="scale"/> of the picture, for a shader working a pixel at a time.
    /// </summary>
    /// <remarks>
    /// The workgroup size here matches the shader's <c>numthreads</c>, and the shader checks that
    /// its pixel is inside the picture, since the last workgroups across and down reach past it.
    /// </remarks>
    public static ViewDispatch PerPixel(
        ShaderInstance instance,
        FramePoint point,
        uint groupX = 8,
        uint groupY = 8,
        float scale = 1f) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.PerPixel,
        X = groupX,
        Y = groupY,
        Z = 1,
        Scale = scale,
    };

    /// <summary>Exactly this many workgroups.</summary>
    public static ViewDispatch Fixed(ShaderInstance instance, FramePoint point, uint x, uint y = 1, uint z = 1) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.Fixed,
        X = x,
        Y = y,
        Z = z,
        Scale = 1f,
    };

    /// <summary>As many workgroups as three unsigned integers in a buffer say, when it runs.</summary>
    public static ViewDispatch Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.Indirect,
        Buffer = buffer,
        Offset = offset,
        Scale = 1f,
    };
}

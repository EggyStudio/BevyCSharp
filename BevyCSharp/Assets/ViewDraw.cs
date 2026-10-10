namespace Bevy;

/// <summary>Geometry a camera draws every frame out of buffers. See <see cref="Shaders.SetViewDraws"/>.</summary>
/// <remarks>Made with <see cref="Fixed"/>, <see cref="Meshes"/> or <see cref="Indirect"/>.</remarks>
public readonly record struct ViewDraw
{
    /// <summary>The instance whose program and values draw.</summary>
    public ShaderInstance Instance { get; init; }

    /// <summary>Where in the camera's frame.</summary>
    public FramePoint Point { get; init; }

    /// <summary>Whether the counts are read from <see cref="Buffer"/> when it runs.</summary>
    public bool FromBuffer { get; init; }

    /// <summary>How many vertices each instance has, for a fixed draw.</summary>
    public uint Vertices { get; init; }

    /// <summary>How many instances, for a fixed draw.</summary>
    public uint Instances { get; init; }

    /// <summary>
    /// How many workgroups run across, down and deep, for a program drawing with mesh shaders
    /// (<see cref="ShaderProgramSettings.DrawMesh"/>).
    /// </summary>
    public (uint X, uint Y, uint Z) Groups { get; init; }

    /// <summary>The buffer an indirect draw reads its counts from.</summary>
    public AssetHandle Buffer { get; init; }

    /// <summary>Where in the buffer the counts start, in bytes.</summary>
    public uint Offset { get; init; }

    /// <summary>How it combines with the picture.</summary>
    public DrawBlend Blend { get; init; }

    /// <summary>
    /// Whether it writes depth, as opaque geometry does, or only tests against it, as anything
    /// see-through does.
    /// </summary>
    public bool WritesDepth { get; init; }

    /// <summary>
    /// Whether it is drawn into the shadow maps of the lights that cast shadows as well, so it
    /// shadows everything they light, Bevy's own geometry included.
    /// </summary>
    /// <remarks>
    /// Drawn again for each shadow view, depth alone, after Bevy has drawn its own casters there:
    /// each of the camera's directional cascades, each spot light, and each face of each point
    /// light's cube. Its vertex shader runs with that view in <c>bcs_pass::view</c>, so placing
    /// geometry from the view as it always does places it as the light sees it, and its fragment
    /// shader does not run. A program with a <see cref="ShaderProgramSettings.DrawShadow"/> stage
    /// runs that there instead, which writes the depth itself, for geometry its fragment shader
    /// finds rather than its vertex shader places. A spot or point light's map is shared by every
    /// camera, so every camera's casting draws are drawn into it.
    /// </remarks>
    public bool CastsShadows { get; init; }

    /// <summary>
    /// One of the camera's images (<see cref="Shaders.SetViewImages"/>) to draw into instead of the
    /// picture, or null for the picture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A visibility buffer is made this way, as geometry drawn into an
    /// <see cref="ShaderImageFormat.R32UInt"/> image, each pixel keeping which cluster and triangle
    /// is nearest, for a later pass to shade. The fragment shader returns what the image holds, an
    /// unsigned integer for an integer image.
    /// </para>
    /// <para>
    /// It is drawn once a pixel, and tested against the camera's depth, and writes it if
    /// <see cref="WritesDepth"/> says so, where the image is the picture's size and the camera
    /// draws once a pixel too; otherwise it draws without depth. An integer image cannot be
    /// blended, so a draw into one replaces what is there. Consecutive draws into the same target
    /// share one render pass.
    /// </para>
    /// </remarks>
    public string? Into { get; init; }

    /// <summary>
    /// Several of the camera's images to draw into at once, one for each of the fragment shader's
    /// outputs in order, instead of <see cref="Into"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a draw writing more than one thing a pixel, such as a visibility buffer's ids and the
    /// barycentrics beside them, or a G-buffer of its own. Every image is drawn once a pixel; they
    /// are tested against the camera's depth only where all of them are the picture's size, and
    /// none is blended where any is an integer image.
    /// </para>
    /// <para>
    /// The camera's prepass is drawn into by the same names: <c>normals</c> and <c>motion</c>
    /// where it draws them, and on a camera drawing deferred, the G-buffer as <c>gbuffer</c> and
    /// which lighting pass lights each pixel as <c>lighting_pass</c>. A draw into those two runs
    /// at <see cref="FramePoint.InPrepass"/>, and Bevy's deferred lighting then lights what it
    /// wrote as it lights a standard material. <c>bcs_pass::deferred</c> returns both and the
    /// depth, packed as Bevy packs its own.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string>? Targets { get; init; }

    /// <summary><paramref name="vertices"/> vertices, <paramref name="instances"/> times.</summary>
    public static ViewDraw Fixed(
        ShaderInstance instance,
        FramePoint point,
        uint vertices,
        uint instances = 1,
        DrawBlend blend = DrawBlend.Opaque,
        bool writesDepth = true) => new()
    {
        Instance = instance,
        Point = point,
        Vertices = vertices,
        Instances = instances,
        Blend = blend,
        WritesDepth = writesDepth,
    };

    /// <summary>
    /// <paramref name="x"/> by <paramref name="y"/> by <paramref name="z"/> workgroups of a
    /// program's task shader, or of its mesh shader where it has no task shader, for a program
    /// drawing with mesh shaders (<see cref="ShaderProgramSettings.DrawMesh"/>).
    /// </summary>
    public static ViewDraw Meshes(
        ShaderInstance instance,
        FramePoint point,
        uint x,
        uint y = 1,
        uint z = 1,
        DrawBlend blend = DrawBlend.Opaque,
        bool writesDepth = true) => new()
    {
        Instance = instance,
        Point = point,
        Groups = (x, y, z),
        Blend = blend,
        WritesDepth = writesDepth,
    };

    /// <summary>
    /// As many vertices and instances as four unsigned integers in a buffer say when it runs:
    /// vertices, instances, the first vertex and the first instance. For a program drawing with mesh
    /// shaders, as many workgroups as three unsigned integers there say instead.
    /// </summary>
    public static ViewDraw Indirect(
        ShaderInstance instance,
        FramePoint point,
        AssetHandle buffer,
        uint offset = 0,
        DrawBlend blend = DrawBlend.Opaque,
        bool writesDepth = true) => new()
    {
        Instance = instance,
        Point = point,
        FromBuffer = true,
        Buffer = buffer,
        Offset = offset,
        Blend = blend,
        WritesDepth = writesDepth,
    };
}

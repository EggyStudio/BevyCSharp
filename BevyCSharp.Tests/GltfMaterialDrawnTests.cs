using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a glTF file's own material reaching the picture, which <see cref="DrawnTests"/> checks for
/// a material made in code.
/// </summary>
/// <remarks>
/// <para>
/// A model's materials are translated by Bevy's glTF loader into its own, as labeled assets of the
/// file, and a scene spawned from it is drawn with them. A material that the translation dropped,
/// or that never reached the render world, draws as Bevy's default white or as nothing, and either
/// reads as a model that loaded.
/// </para>
/// <para>
/// The file is written by the test: one square facing the camera, with an unlit green material
/// (<c>KHR_materials_unlit</c>), so the color is the material's alone and not a question about the
/// lighting. The camera clears to blue, which nothing in the file is.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class GltfMaterialDrawnTests : IDisposable
{
    private const ulong Settled = 150;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-gltf-material-" + Guid.NewGuid().ToString("n"));

    public GltfMaterialDrawnTests()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "square.gltf"), Square());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [SkippableFact]
    public void AModelIsDrawnWithItsOwnMaterial()
    {
        Needs.Renderer();

        CapturedImage? picture = null;
        Capture? ticket = null;

        var config = Config.OffscreenFor(64, 64, frames: (uint)Settled + 60);
        config.AssetRoot = _root;

        using var app = new App(config);
        app.AddPlugin(new EnginePlugin());

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();

                var camera = Render.SpawnCamera3d(new CameraSettings
                {
                    FieldOfView = 50f,
                    Clear = ClearMode.Custom,
                    ClearColor = (0f, 0f, 1f, 1f),
                });

                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
                ecs.SpawnScene(AssetServer.LoadGltfScene("square.gltf"));
            },
            "Test.Scene"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount == Settled) ticket = Render.BeginCapture();
                if (picture is null && ticket is { } asked && Render.TryReadCapture(asked, out var arrived)) picture = arrived;
            },
            "Test.Read"));

        Assert.Equal(0, app.Run());
        Assert.NotNull(picture);

        var middle = picture.At(32, 32);
        var corner = picture.At(1, 1);

        Assert.True(
            middle.G > 180 && middle.R < 90 && middle.B < 90,
            $"the square should be the file's green in the middle of the picture, and it is {middle}");

        Assert.True(
            corner.B > 180 && corner.G < 90,
            $"the camera's clear color should show in the corner, and it is {corner}");
    }

    /// <summary>A glTF file holding one square two units across, facing +Z, in an unlit green.</summary>
    private static string Square()
    {
        float[] positions = [-1f, -1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f, -1f, 1f, 0f];
        ushort[] indices = [0, 1, 2, 0, 2, 3];

        var bytes = new byte[(positions.Length * 4) + (indices.Length * 2)];
        Buffer.BlockCopy(positions, 0, bytes, 0, positions.Length * 4);
        Buffer.BlockCopy(indices, 0, bytes, positions.Length * 4, indices.Length * 2);

        var gltf = new Dictionary<string, object>
        {
            ["asset"] = new Dictionary<string, object> { ["version"] = "2.0" },
            ["extensionsUsed"] = new[] { "KHR_materials_unlit" },
            ["scene"] = 0,
            ["scenes"] = new[] { new Dictionary<string, object> { ["nodes"] = new[] { 0 } } },
            ["nodes"] = new[] { new Dictionary<string, object> { ["mesh"] = 0 } },
            ["meshes"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["primitives"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["attributes"] = new Dictionary<string, object> { ["POSITION"] = 0 },
                            ["indices"] = 1,
                            ["material"] = 0,
                        },
                    },
                },
            },
            ["materials"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["pbrMetallicRoughness"] = new Dictionary<string, object> { ["baseColorFactor"] = new[] { 0f, 1f, 0f, 1f } },
                    ["extensions"] = new Dictionary<string, object> { ["KHR_materials_unlit"] = new Dictionary<string, object>() },
                },
            },
            ["buffers"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["byteLength"] = bytes.Length,
                    ["uri"] = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes),
                },
            },
            ["bufferViews"] = new[]
            {
                new Dictionary<string, object> { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = positions.Length * 4 },
                new Dictionary<string, object> { ["buffer"] = 0, ["byteOffset"] = positions.Length * 4, ["byteLength"] = indices.Length * 2 },
            },
            ["accessors"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 4, ["type"] = "VEC3",
                    ["min"] = new[] { -1f, -1f, 0f }, ["max"] = new[] { 1f, 1f, 0f },
                },
                new Dictionary<string, object> { ["bufferView"] = 1, ["componentType"] = 5123, ["count"] = 6, ["type"] = "SCALAR" },
            },
        };

        return JsonSerializer.Serialize(gltf);
    }
}

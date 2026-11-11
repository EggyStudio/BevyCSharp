using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The names a glTF file gives a mesh's morph targets, which Bevy's morph_targets prints as each
/// mesh arrives. <c>assets/models/morph.gltf</c> is one triangle with two targets named in its
/// mesh's extras, as a modeling tool writes them.
/// </summary>
[Collection("engine")]
public sealed class MorphTargetTests
{
    [SkippableFact]
    public void AMeshNamesItsMorphTargetsOnceItHasArrived()
    {
        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        AssetHandle morph = AssetHandle.None, plain = AssetHandle.None;
        string[]? names = null, none = null;

        harness.OnContext(Stage.Startup, _ =>
        {
            morph = AssetServer.LoadGltfMesh("models/morph.gltf");
            plain = AssetServer.LoadGltfMesh("models/triangle.gltf");
        });
        harness.OnContext(Stage.Update, ctx =>
        {
            // Nothing while each loads, then its names.
            names ??= Render.MorphTargetNames(morph);
            none ??= Render.MorphTargetNames(plain);
            if (names is not null && none is not null) ctx.Exit();
        });

        harness.Run();

        Assert.NotNull(names);
        Assert.Equal(["Smile", "Frown"], names);
        Assert.NotNull(none);
        Assert.Empty(none);
    }
}

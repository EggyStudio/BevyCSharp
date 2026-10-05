using Bevy;

namespace BevyCSharp.Examples.Shading;

// A cube whose every face is sixteen tiles, each drawn from its own texture of an array of sixteen
// that the shader indexes by where on the face a pixel is. Bevy first checks that the GPU can index
// such an array by a value that differs from pixel to pixel, a check left out here.
internal static class TextureBindingArray
{
    private static readonly int[] TileId = [19, 23, 4, 33, 12, 69, 30, 48, 10, 65, 40, 47, 57, 41, 44, 46];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.Camera(Transform.LookingAt(new Vec3(2f, 2f, 2f), Vec3.Zero, Vec3.UnitY));

            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/texture_binding_array.slang"))
                .SetSampler("nearest_sampler", SamplerSettings.Nearest);
            for (var i = 0; i < TileId.Length; i++)
                material.SetTexture("textures", AssetServer.Load(AssetKind.Image, $"textures/rpg/tiles/generic-rpg-tile{TileId[i]:00}.png"), i);

            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.Identity);
        }, "texture_binding_array.Setup");
    }
}

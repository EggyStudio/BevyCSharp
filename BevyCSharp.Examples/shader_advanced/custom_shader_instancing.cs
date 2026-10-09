// Bevy's custom_shader_instancing example, examples/shader_advanced/custom_shader_instancing.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A hundred cubes in a grid of ten by ten, drawn by one draw call of a hundred instances, each
// placed and colored by its entry in a buffer, their hue across and saturation up.
//
// Bevy adds a render command and a pipeline of its own to draw its mesh with an instance buffer.
// Here the camera draws a program out of the buffer at a point of its frame, with the cube's
// corners worked out in the shader, since a camera's draw reads buffers rather than a mesh.
internal static class CustomShaderInstancing
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceData
    {
        public Vector4 PositionScale;
        public Vector4 Color;
    }

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var instances = new List<InstanceData>();
            for (var xi = 1; xi <= 10; xi++)
            {
                for (var yi = 1; yi <= 10; yi++)
                {
                    var (x, y) = (xi / 10f, yi / 10f);
                    var (r, g, b, a) = Color.FromHsl(x * 360f, y, 0.5f);
                    instances.Add(new InstanceData { PositionScale = new Vector4(x * 10f - 5f, y * 10f - 5f, 0f, 1f), Color = new Vector4(r, g, b, a) });
                }
            }

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 15f), Vec3.Zero, Vec3.UnitY));
            var draw = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
            {
                DrawVertex = "shaders/instancing.slang",
                DrawFragment = "shaders/instancing.slang",
            })).SetBuffer("instances", Shaders.CreateBuffer<InstanceData>(instances.ToArray()));
            Shaders.SetViewDraws(camera, ViewDraw.Fixed(draw, FramePoint.AfterOpaque, vertices: 36, instances: (uint)instances.Count));
        }, "custom_shader_instancing.Setup");
    }
}

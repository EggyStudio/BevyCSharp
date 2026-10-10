// Bevy's pipeline_constants example, examples/shader/pipeline_constants.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// One shader's gradient drawn in two, four and eight steps, the number of steps a pipeline
// constant each quad's material sets, so each is compiled into pipelines of its own where a
// uniform would be read as the quad is drawn.
internal static class PipelineConstants
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/pipeline_constants.slang" });
        var quad = Render.CreateMesh(MeshShape.Rectangle, 200f, 150f);

        foreach (var (i, levels) in new[] { 2, 4, 8 }.Index())
        {
            var x = (i - 1f) * 220f;

            var square = ecs.Spawn();
            ecs.Add(square, Transform.At(x, 20f, 0f));
            Render2d.SetMesh(ecs, square, quad);
            Render2d.SetMaterial(ecs, square, Shaders.CreateMaterial2d(program).Set("LEVELS", (float)levels));

            var label = ecs.Spawn();
            ecs.Add(label, Transform.At(x, -65f, 0f));
            ecs.Insert<Text2dRef>(label).Value = $"{levels} levels";
        }
    }, "pipeline_constants.Setup");
}

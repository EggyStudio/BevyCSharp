// bevy_csharp slang cache 3
// source 2db1b97f71a2268d
// end
struct Corner_0
{
    @builtin(position) position_0 : vec4<f32>,
};

@vertex
fn vertex(@builtin(vertex_index) vertex_id_0 : u32) -> Corner_0
{
    var output_0 : Corner_0;
    output_0.position_0 = vec4<f32>(vec2<f32>(f32((((vertex_id_0 << (u32(1)))) & (u32(2)))), f32((vertex_id_0 & (u32(2))))) * vec2<f32>(2.0f) - vec2<f32>(1.0f), 1.0f, 1.0f);
    return output_0;
}


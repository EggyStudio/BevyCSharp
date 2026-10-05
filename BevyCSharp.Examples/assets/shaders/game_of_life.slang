// Bevy's game_of_life shader in Slang, Conway's game of life run on the GPU, each pixel a cell
// whose alpha says whether it lives, read from one image and written to the other.
import bcs_compute;

Texture2D<float4> input;
[format("rgba32f")] RWTexture2D<float4> output;

struct GameOfLifeUniforms
{
    float4 alive_color;
};

uniform GameOfLifeUniforms config;

uint hash(uint value)
{
    var state = value;
    state = state ^ 2747636419u;
    state = state * 2654435769u;
    state = state ^ (state >> 16u);
    state = state * 2654435769u;
    state = state ^ (state >> 16u);
    state = state * 2654435769u;
    return state;
}

float random_float(uint value)
{
    return float(hash(value)) / 4294967295.0;
}

// A cell in ten alive, at random.
[shader("compute")]
[numthreads(8, 8, 1)]
void init(uint3 invocation_id : SV_DispatchThreadID)
{
    let location = int2(invocation_id.xy);
    let alive = random_float((invocation_id.y << 16u) | invocation_id.x) > 0.9;
    output[location] = float4(config.alive_color.rgb, alive ? 1.0 : 0.0);
}

int is_alive(int2 location, int offset_x, int offset_y)
{
    return int(input.Load(int3(location + int2(offset_x, offset_y), 0)).a);
}

int count_alive(int2 location)
{
    return is_alive(location, -1, -1) + is_alive(location, -1, 0) + is_alive(location, -1, 1)
        + is_alive(location, 0, -1) + is_alive(location, 0, 1)
        + is_alive(location, 1, -1) + is_alive(location, 1, 0) + is_alive(location, 1, 1);
}

// A cell with three neighbors lives, one with two stays as it was, and any other dies.
[shader("compute")]
[numthreads(8, 8, 1)]
void update(uint3 invocation_id : SV_DispatchThreadID)
{
    let location = int2(invocation_id.xy);
    let n_alive = count_alive(location);

    bool alive;
    if (n_alive == 3)
        alive = true;
    else if (n_alive == 2)
        alive = is_alive(location, 0, 0) != 0;
    else
        alive = false;

    output[location] = float4(config.alive_color.rgb, alive ? 1.0 : 0.0);
}

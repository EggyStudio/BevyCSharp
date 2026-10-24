// A path-traced reference of the camera's picture, which global illumination is measured against,
// 3DEngine's gi_reference.slang over a ray scene of the meshes the camera draws.
//
// From each pixel it traces paths through the ray scene (`Shaders.CreateRayScene`), each face a
// path meets its material's base color, from the material buffer the engine fills, and its emissive
// light. The first face is lit as Bevy's lighting lights it, each light's diffuse share by Burley's
// term and its specular share by the material's roughness, metallic and reflectance, and every face
// after it as global illumination's probes light one, its color times what arrives. A light that
// casts shadows reaches a face only where a ray toward it gets through. A path goes on in a
// direction drawn by the cosine about the face's normal, ends at what Russian roulette draws after
// its third bounce, and brings back the light from all around, as the probes have it, where it
// meets nothing. The lights are the probes' own (bcs_gi_lights), the light a white surface facing
// one returns times the camera's exposure, so a face's diffuse light is its color times what
// arrives, with no pi, in the units of the picture before tonemapping.
//
// Each frame adds its samples to an image of sums, the count in alpha, so a frame the dispatch
// missed while its program compiled is no sample short, and a ray through each pixel's middle writes
// what it met into the image of regions, the slot's number times eight and the axis its face turns
// toward most, plus one, or 0 for nothing. Both images start at zero for each reference. Compiled
// to SPIR-V, since ray queries exist nowhere else. A mesh's vertices' colors are not in the pool,
// so a face is its material's color alone, and a texture's light is left to its base color.
import bcs_pass;
import bcs_ray;
import bcs_scene;
import bcs_gi;

RaytracingAccelerationStructure scene;
StructuredBuffer<PoolVertex> vertices;
StructuredBuffer<uint> indices;
StructuredBuffer<PoolMesh> meshes;
StructuredBuffer<Instance> instances;
StructuredBuffer<Material> materials;
StructuredBuffer<GiLights> lights;

[format("rgba32f")] RWTexture2D<float4> sums;
[format("r32ui")] RWTexture2D<uint> regions;

// x: the samples a pixel this frame adds. y: the number of the first. z: the most faces a path
// meets. w: unused.
uniform uint4 counts;

// How far from a point or spot light its shadows start, as Bevy's shadow maps start a tenth of a
// unit from the light (PointLight::DEFAULT_SHADOW_MAP_NEAR_Z), so a mesh nearer the light than that,
// as a bulb's own sphere around it, casts no shadow.
static const float NearShadow = 0.1;

// A random number from 0 to 1 and the state moved on, by the PCG hash.
float random(inout uint state)
{
    state = state * 747796405u + 2891336453u;
    uint packed = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return float((packed >> 22u) ^ packed) / 4294967296.0;
}

// A way about n drawn by the cosine.
float3 cosineAbout(float3 n, inout uint state)
{
    float u = random(state), w = random(state);
    float r = sqrt(u);
    float phi = 2.0 * Pi * w;
    float3 side = abs(n.y) < 0.99 ? normalize(cross(n, float3(0.0, 1.0, 0.0))) : float3(1.0, 0.0, 0.0);
    float3 up = cross(n, side);
    return normalize(side * (r * cos(phi)) + up * (r * sin(phi)) + n * sqrt(max(0.0, 1.0 - u)));
}

float3 schlick(float3 f0, float f90, float cosine)
{
    return f0 + (f90 - f0) * pow(1.0 - cosine, 5.0);
}

// What a light along l sends toward v off a face facing n, over the light a white surface facing
// it returns, as Bevy's point_light works it out: Burley's diffuse term times pi and the specular
// lobe times pi, the lights being given over pi.
float3 bevyReflected(float3 n, float3 v, float3 l, float3 diffuse, float3 f0, float roughness)
{
    float3 h = normalize(v + l);
    float nl = saturate(dot(n, l));
    float nv = max(dot(n, v), 1e-4);
    float nh = saturate(dot(n, h));
    float lh = saturate(dot(l, h));

    float f90 = 0.5 + 2.0 * roughness * lh * lh;
    float burley = schlick(float3(1.0), f90, nl).x * schlick(float3(1.0), f90, nv).x;

    float a2 = roughness * roughness;
    float d = nh * nh * (a2 - 1.0) + 1.0;
    float distribution = a2 / (Pi * d * d);
    float visibility = 0.5 / (nl * sqrt(nv * nv * (1.0 - a2) + a2) + nv * sqrt(nl * nl * (1.0 - a2) + a2));
    float3 fresnel = schlick(f0, saturate(dot(f0, float3(50.0 * 0.33))), lh);
    return diffuse * burley + distribution * visibility * fresnel * Pi;
}

// The light the sun and the lamps send back from p on a face facing n, toward v where the face is
// the path's first, lit as Bevy lights it, and as the probes light a face otherwise.
float3 direct(GiLights given, float3 p, float3 n, float3 v, bool first, float3 color, Material material)
{
    float metallic = saturate(material.metallic);
    float roughness = clamp(material.perceptual_roughness, 0.089, 1.0);
    roughness *= roughness;
    float3 diffuse = first ? color * (1.0 - metallic) : color;
    float reflectance = material.reflectance;
    float3 f0 = 0.16 * reflectance * reflectance * (1.0 - metallic) + color * metallic;
    float3 start = p + n * 1e-3;
    float3 sum = float3(0.0);

    if (given.sunDirection.w > 0.5)
    {
        float3 l = given.sunDirection.xyz;
        float nl = dot(n, l);
        if (nl > 0.0 && (given.sunColor.w < 0.5 || rayVisibleIn(scene, start, start + l * 1e5)))
            sum += given.sunColor.rgb * nl * (first ? bevyReflected(n, v, l, diffuse, f0, roughness) : diffuse);
    }
    for (int i = 0; i < min(int(given.counts.x), 16); i++)
    {
        GiLight lamp = given.lights[i];
        float arrived = lampLight(lamp, p, n);
        if (arrived <= 0.0)
            continue;
        float3 l = normalize(lamp.positionAndKind.xyz - p);
        if (lamp.colorAndShadow.w > 0.5 && !rayVisibleIn(scene, start, lamp.positionAndKind.xyz - l * NearShadow))
            continue;
        sum += lamp.colorAndShadow.rgb * arrived * (first ? bevyReflected(n, v, l, diffuse, f0, roughness) : diffuse);
    }
    return sum;
}

// The axis a face facing n turns toward most, +x, -x, +y, -y, +z and -z in turn.
uint axisOf(float3 n)
{
    float3 m = abs(n);
    return m.x >= m.y && m.x >= m.z ? (n.x > 0.0 ? 0u : 1u) : m.y >= m.z ? (n.y > 0.0 ? 2u : 3u) : (n.z > 0.0 ? 4u : 5u);
}

// The face's normal in the world, toward the side the ray came from.
float3 normalAt(RayHit hit, float3 way)
{
    PoolMesh mesh = meshes[hit.mesh];
    Instance placed = instances[hit.instance];
    float3 a = instanceToWorld(placed, poolCorner(vertices, indices, mesh, hit.triangle, 0u).position.xyz);
    float3 b = instanceToWorld(placed, poolCorner(vertices, indices, mesh, hit.triangle, 1u).position.xyz);
    float3 c = instanceToWorld(placed, poolCorner(vertices, indices, mesh, hit.triangle, 2u).position.xyz);
    float3 n = normalize(cross(b - a, c - a));
    return dot(n, way) > 0.0 ? -n : n;
}

// The light one path brings back from origin along way.
float3 trace(GiLights given, float3 origin, float3 way, inout uint state)
{
    float3 throughput = float3(1.0);
    float3 light = float3(0.0);

    for (uint bounce = 0u; bounce < counts.z; bounce++)
    {
        RayHit hit = rayTraceIn(scene, origin, way, 0.0, 1e5);
        if (!hit.hit)
        {
            light += throughput * given.sky.rgb;
            break;
        }

        float3 n = normalAt(hit, way);
        float3 p = origin + way * hit.t;

        Material material = materials[hit.instance];
        float3 color = material.base_color.rgb;
        if (material.unlit > 0.5)
        {
            light += throughput * color;
            break;
        }

        // Emissive light, times the exposure by the share of it its alpha sets, as Bevy shows it.
        bool first = bounce == 0u;
        float3 emitted = material.emissive.rgb * lerp(1.0, passView.exposure, material.emissive.a);
        light += throughput * (emitted + direct(given, p, n, -way, first, color, material));

        // The diffuse share goes on, the first face's past its metal, as the indirect light reaching
        // a pixel lights it.
        throughput *= first ? color * (1.0 - saturate(material.metallic)) : color;
        if (bounce >= 3u)
        {
            float keep = clamp(max(throughput.r, max(throughput.g, throughput.b)), 0.05, 0.95);
            if (random(state) > keep)
                break;
            throughput /= keep;
        }
        if (all(throughput <= float3(0.0)))
            break;
        origin = p + n * 1e-3;
        way = cosineAbout(n, state);
    }
    return light;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    uint width, height;
    sums.GetDimensions(width, height);
    if (id.x >= width || id.y >= height)
        return;

    // What the ray through the pixel's middle meets names what the pixel shows.
    float2 middle = (float2(id.xy) + 0.5) / float2(width, height);
    float3 way = passRayDirection(middle);
    RayHit met = rayTraceIn(scene, passRayOrigin(middle), way, 0.0, 1e5);
    regions[id.xy] = met.hit ? met.instance * 8u + axisOf(normalAt(met, way)) + 1u : 0u;

    GiLights given = lights[0];
    float3 sum = float3(0.0);
    for (uint s = 0u; s < counts.x; s++)
    {
        uint index = counts.y + s;
        uint state = (id.y * width + id.x) * 9781u + index * 6271u + 1u;
        random(state);
        float2 uv = (float2(id.xy) + float2(random(state), random(state))) / float2(width, height);
        sum += trace(given, passRayOrigin(uv), passRayDirection(uv), state);
    }
    sums[id.xy] = sums[id.xy] + float4(sum, float(counts.x));
}

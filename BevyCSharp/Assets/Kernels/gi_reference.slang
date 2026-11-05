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
// its third bounce, and brings back the light from all around where it meets nothing, the ambient
// light as the probes have it and the camera's environment map's along its way. The lights are the probes' own (bcs_gi_lights), the light a white surface facing
// one returns times the camera's exposure, so a face's diffuse light is its color times what
// arrives, with no pi, in the units of the picture before tonemapping.
//
// Each frame adds its samples to an image of sums, the count in alpha, so a frame the dispatch
// missed while its program compiled is no sample short, and a ray through each pixel's middle writes
// what it met into the image of regions, the slot's number times eight and the axis its face turns
// toward most, plus one, or 0 for nothing. Both images start at zero for each reference. Compiled
// to SPIR-V, since ray queries exist nowhere else. A mesh's vertices' colors are not in the pool,
// so a face is its material's color alone, and a texture's light is left to its base color, but for
// a cut-out material's, a leaf's or a grass card's, whose base color texture is one of the material
// buffer's masks, which colors the face and lets a path or a shadow ray through where its alpha is
// under the cutoff, as the forward pass discards those texels, its metallic and roughness texture
// another mask. A tree's impostor, which stands in
// for a far tree turned to the eye as it draws, is passed, so the reference holds no far trees.
//
// Where counts.w gives an octahedron's texels a side, it traces from a world probe's place instead
// (`gi.probe`), a texel of the octahedron a thread, the paths of the left half of the image along
// each texel's middle way and those of the right half each along a way drawn across the texel,
// every face met lit as the probes light one, and the image of regions holds the first face's
// distance along the middle way, so what one probe's rays bring can be read against what its
// texels' ways bring.
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
// meets. w: the texels a side of the probe's octahedron traced from, or 0 for the camera's picture.
uniform uint4 counts;

// xyz: the place of the probe traced from where counts.w is above 0. w: 1 where it is a screen
// probe's, traced along the ways given in `ways`, and 0 where it is a world probe's.
uniform float4 probe;

// A screen probe's sixteen ways (gi.screenprobe), then the hemisphere's two ways across its surface
// and its normal, as gi_screen.slang lays its rays out, read where probe.w is 1.
StructuredBuffer<float4> ways;

// The material buffer's masks (Shaders.MaterialBufferMask), the base color textures and the
// metallic and roughness textures of the cut-out materials its slots hold, a white texel where it
// has fewer.
Texture2D mask0;
Texture2D mask1;
Texture2D mask2;
Texture2D mask3;
Texture2D mask4;
Texture2D mask5;
Texture2D mask6;
Texture2D mask7;
Texture2D mask8;
Texture2D mask9;
Texture2D mask10;
Texture2D mask11;
Texture2D mask12;
Texture2D mask13;
Texture2D mask14;
Texture2D mask15;
SamplerState mask_sampler;

// The most cut-out faces a ray passes before it is taken as meeting the next, as through a forest's
// crowns, where each card it crosses lets it through where its leaves are cut out.
static const uint MostCutouts = 32u;

// Mask `index` read at uv, at its largest level.
float4 maskAt(int index, float2 uv)
{
    switch (index)
    {
    case 0: return mask0.SampleLevel(mask_sampler, uv, 0.0);
    case 1: return mask1.SampleLevel(mask_sampler, uv, 0.0);
    case 2: return mask2.SampleLevel(mask_sampler, uv, 0.0);
    case 3: return mask3.SampleLevel(mask_sampler, uv, 0.0);
    case 4: return mask4.SampleLevel(mask_sampler, uv, 0.0);
    case 5: return mask5.SampleLevel(mask_sampler, uv, 0.0);
    case 6: return mask6.SampleLevel(mask_sampler, uv, 0.0);
    case 7: return mask7.SampleLevel(mask_sampler, uv, 0.0);
    case 8: return mask8.SampleLevel(mask_sampler, uv, 0.0);
    case 9: return mask9.SampleLevel(mask_sampler, uv, 0.0);
    case 10: return mask10.SampleLevel(mask_sampler, uv, 0.0);
    case 11: return mask11.SampleLevel(mask_sampler, uv, 0.0);
    case 12: return mask12.SampleLevel(mask_sampler, uv, 0.0);
    case 13: return mask13.SampleLevel(mask_sampler, uv, 0.0);
    case 14: return mask14.SampleLevel(mask_sampler, uv, 0.0);
    case 15: return mask15.SampleLevel(mask_sampler, uv, 0.0);
    default: return float4(1.0);
    }
}

// The texture coordinates where a ray met its face, from its corners.
float2 uvAt(RayHit hit)
{
    PoolMesh mesh = meshes[hit.mesh];
    float3 weights = float3(1.0 - hit.barycentrics.x - hit.barycentrics.y, hit.barycentrics);
    float2 uv = float2(0.0);
    for (uint corner = 0u; corner < 3u; corner++)
        uv += poolCorner(vertices, indices, mesh, hit.triangle, corner).uv.xy * weights[corner];
    return uv;
}

// The color a ray met, its material's base color times its mask's where it has one.
float3 colorAt(RayHit hit, Material material)
{
    int index = int(material.mask);
    return material.base_color.rgb * (material.alpha_cutoff >= 0.0 && index >= 0 ? maskAt(index, uvAt(hit)).rgb : float3(1.0));
}

// The material a ray met as its metallic and roughness texture says it is there, its green scaling
// the roughness and its blue the metallic as Bevy reads it, a leaf's and a grass card's metallic of
// one their texture's to say.
Material surfacedAt(RayHit hit, Material material)
{
    int index = int(material.surface_mask);
    if (material.alpha_cutoff < 0.0 || index < 0)
        return material;
    float4 surface = maskAt(index, uvAt(hit));
    material.perceptual_roughness *= surface.g;
    material.metallic *= surface.b;
    return material;
}

// Whether a ray passes the face it met, a cut-out material's where its alpha is under the cutoff,
// as the forward pass discards a leaf's or a grass card's texel, and a tree's impostor's
// everywhere, since an impostor turned to the eye as it draws is no shape a ray meets.
bool passesAt(RayHit hit)
{
    Material material = materials[hit.instance];
    if (material.alpha_cutoff < 0.0)
        return false;
    if (material.alpha_cutoff > 1.0)
        return true;
    int index = int(material.mask);
    float alpha = material.base_color.a * (index >= 0 ? maskAt(index, uvAt(hit)).a : 1.0);
    return alpha < material.alpha_cutoff;
}

// What a ray meets from origin along way between tMin and tMax, passing the faces cut out where it
// met them (passesAt), up to MostCutouts of them.
RayHit traceThrough(float3 origin, float3 way, float tMin, float tMax)
{
    float t = tMin;
    RayHit hit;
    for (uint passed = 0u; passed <= MostCutouts; passed++)
    {
        hit = rayTraceIn(scene, origin, way, t, tMax);
        if (!hit.hit || !passesAt(hit))
            return hit;
        t = hit.t + 1e-4 * (1.0 + hit.t);
    }
    hit.hit = false;
    hit.t = tMax;
    return hit;
}

// Whether nothing between two points stops a shadow ray, the faces cut out where it met them
// passed as a path passes them.
bool visibleThrough(float3 from, float3 to)
{
    float3 offset = to - from;
    float distance = length(offset);
    if (distance <= 0.0)
        return true;
    return !traceThrough(from, offset / distance, 0.001, distance * 0.999).hit;
}

// The way of the hemisphere's square at uv, and how far its point lies from the middle, as
// gi_screen.slang's hemisphere.
float4 hemisphereAt(float2 uv)
{
    float2 a = uv * 2.0 - 1.0;
    float x = (a.x + a.y) * 0.5, y = (a.x - a.y) * 0.5;
    float3 d = float3(x, y, 1.0 - abs(x) - abs(y));
    return float4(normalize(d), length(d));
}

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
// it returns, as Bevy's point_light works it out, Burley's diffuse term times pi and the specular
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
        if (nl > 0.0 && (given.sunColor.w < 0.5 || visibleThrough(start, start + l * 1e5)))
            sum += given.sunColor.rgb * nl * (first ? bevyReflected(n, v, l, diffuse, f0, roughness) : diffuse);
    }
    for (int i = 0; i < min(int(given.counts.x), 16); i++)
    {
        GiLight lamp = given.lights[i];
        float arrived = lampLight(lamp, p, n);
        if (arrived <= 0.0)
            continue;
        float3 l = normalize(lamp.positionAndKind.xyz - p);
        if (lamp.colorAndShadow.w > 0.5 && !visibleThrough(start, lamp.positionAndKind.xyz - l * NearShadow))
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

// The light one path brings back from origin along way, its first face lit as Bevy lights it where
// the path leaves the camera and as the probes light one where it leaves a probe.
float3 trace(GiLights given, float3 origin, float3 way, bool fromCamera, inout uint state)
{
    float3 throughput = float3(1.0);
    float3 light = float3(0.0);

    for (uint bounce = 0u; bounce < counts.z; bounce++)
    {
        RayHit hit = traceThrough(origin, way, 0.0, 1e5);
        if (!hit.hit)
        {
            light += throughput * (given.sky.rgb + passEnvironmentSpecular(way, 0.0) * passView.exposure);
            break;
        }

        float3 n = normalAt(hit, way);
        float3 p = origin + way * hit.t;

        Material material = surfacedAt(hit, materials[hit.instance]);
        float3 color = colorAt(hit, material);
        if (material.unlit > 0.5)
        {
            light += throughput * color;
            break;
        }

        // Emissive light, times the exposure by the share of it its alpha sets, as Bevy shows it.
        bool first = fromCamera && bounce == 0u;
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
    // The sun as Bevy's lighting gives the picture, dimmed and reddened through the camera's air
    // toward it, read at the camera as Bevy reads it and before any thread returns, as the clouds
    // read it. The lights give the sun as it is above the air, which lit the reference's sunlit faces
    // a third over the picture's at a low sun and whiter.
    GiLights given = lights[0];
    if (given.sunDirection.w > 0.5)
        given.sunColor.rgb *= passAtmosphereTransmittance(passView.world_position, given.sunDirection.xyz);

    uint width, height;
    sums.GetDimensions(width, height);
    if (id.x >= width || id.y >= height)
        return;

    if (counts.w > 0u && probe.w > 0.5)
    {
        // A screen probe's rays, those of the left half along its sixteen ways and those of the
        // right half each along a way drawn across its texel of the hemisphere's square, four by
        // four.
        if (id.x >= 32u || id.y > 0u)
            return;
        bool drawn = id.x >= 16u;
        uint ray = id.x % 16u;
        float3 along = ways[ray].xyz;
        RayHit first = traceThrough(probe.xyz, along, 0.0, 1e5);
        regions[id.xy] = first.hit ? asuint(first.t) : 0u;
        float3 brought = float3(0.0);
        for (uint s = 0u; s < counts.x; s++)
        {
            uint index = counts.y + s;
            uint state = id.x * 9781u + index * 6271u + 1u;
            random(state);
            float4 local = hemisphereAt((float2(float(ray % 4u), float(ray / 4u)) + float2(random(state), random(state))) / 4.0);
            float3 way = drawn ? normalize(ways[16].xyz * local.x + ways[17].xyz * local.y + ways[18].xyz * local.z) : along;
            brought += trace(given, probe.xyz, way, false, state);
        }
        sums[id.xy] = (counts.y == 0u ? float4(0.0) : sums[id.xy]) + float4(brought, float(counts.x));
        return;
    }

    if (counts.w > 0u)
    {
        uint texels = counts.w;
        if (id.x >= 2u * texels || id.y >= texels)
            return;
        bool drawn = id.x >= texels;
        uint2 texel = uint2(id.x % texels, id.y);
        float3 through = fromOctahedron((float2(texel) + 0.5) / float(texels));
        RayHit first = traceThrough(probe.xyz, through, 0.0, 1e5);
        regions[id.xy] = first.hit ? asuint(first.t) : 0u;
        float3 brought = float3(0.0);
        for (uint s = 0u; s < counts.x; s++)
        {
            uint index = counts.y + s;
            uint state = (id.y * width + id.x) * 9781u + index * 6271u + 1u;
            random(state);
            float3 way = drawn ? fromOctahedron((float2(texel) + float2(random(state), random(state))) / float(texels)) : through;
            brought += trace(given, probe.xyz, way, false, state);
        }
        sums[id.xy] = (counts.y == 0u ? float4(0.0) : sums[id.xy]) + float4(brought, float(counts.x));
        return;
    }

    // What the ray through the pixel's middle meets names what the pixel shows.
    float2 middle = (float2(id.xy) + 0.5) / float2(width, height);
    float3 way = passRayDirection(middle);
    RayHit met = traceThrough(passRayOrigin(middle), way, 0.0, 1e5);
    regions[id.xy] = met.hit ? met.instance * 8u + axisOf(normalAt(met, way)) + 1u : 0u;

    float3 sum = float3(0.0);
    for (uint s = 0u; s < counts.x; s++)
    {
        uint index = counts.y + s;
        uint state = (id.y * width + id.x) * 9781u + index * 6271u + 1u;
        random(state);
        float2 uv = (float2(id.xy) + float2(random(state), random(state))) / float2(width, height);
        sum += trace(given, passRayOrigin(uv), passRayDirection(uv), true, state);
    }
    // The first share stores where the rest add, since an image is made holding whatever its memory
    // held, and a reference traced after another started from the other's paths, 64 a pixel asked
    // for reading 128 and the two lights averaged.
    sums[id.xy] = (counts.y == 0u ? float4(0.0) : sums[id.xy]) + float4(sum, float(counts.x));
}

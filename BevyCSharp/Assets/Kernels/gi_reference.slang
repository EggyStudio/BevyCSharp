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
// It honors the materials' own models rather than a physical surface's. A cut-out material's
// normal map bends the face's normal as Bevy's does. A face lets through from behind the share its
// diffuse transmission gives, times its texture's alpha, lit by the face's back, and keeps the rest
// as its diffuse share, as Bevy lights it, and a leaf's and a grass card's diffuse share is lit by
// its normal bent half way to its way out of the crown, as the leaf material lights it, whatever a
// real leaf's light would be. A mesh Bevy draws no shadow of, as the meadow's
// grass, is met by the camera's own rays alone, every other ray passing it, as Bevy's shadow maps
// and the scene's distance field the probes trace leave it out.
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

// The material buffer's masks (Shaders.MaterialBufferMask), the base color textures, the metallic
// and roughness textures, the diffuse transmission textures and the normal maps of the cut-out
// materials its slots hold, a white texel where it has fewer. MASKS of them are bound, 32 or as
// many of 16 and 8 as the device's sampled textures a stage leave room for, and a mask past them
// reads as white.
#ifndef MASKS
#define MASKS 32
#endif
Texture2D mask0;
Texture2D mask1;
Texture2D mask2;
Texture2D mask3;
Texture2D mask4;
Texture2D mask5;
Texture2D mask6;
Texture2D mask7;
#if MASKS > 8
Texture2D mask8;
Texture2D mask9;
Texture2D mask10;
Texture2D mask11;
Texture2D mask12;
Texture2D mask13;
Texture2D mask14;
Texture2D mask15;
#endif
#if MASKS > 16
Texture2D mask16;
Texture2D mask17;
Texture2D mask18;
Texture2D mask19;
Texture2D mask20;
Texture2D mask21;
Texture2D mask22;
Texture2D mask23;
Texture2D mask24;
Texture2D mask25;
Texture2D mask26;
Texture2D mask27;
Texture2D mask28;
Texture2D mask29;
Texture2D mask30;
Texture2D mask31;
#endif
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
#if MASKS > 8
    case 8: return mask8.SampleLevel(mask_sampler, uv, 0.0);
    case 9: return mask9.SampleLevel(mask_sampler, uv, 0.0);
    case 10: return mask10.SampleLevel(mask_sampler, uv, 0.0);
    case 11: return mask11.SampleLevel(mask_sampler, uv, 0.0);
    case 12: return mask12.SampleLevel(mask_sampler, uv, 0.0);
    case 13: return mask13.SampleLevel(mask_sampler, uv, 0.0);
    case 14: return mask14.SampleLevel(mask_sampler, uv, 0.0);
    case 15: return mask15.SampleLevel(mask_sampler, uv, 0.0);
#endif
#if MASKS > 16
    case 16: return mask16.SampleLevel(mask_sampler, uv, 0.0);
    case 17: return mask17.SampleLevel(mask_sampler, uv, 0.0);
    case 18: return mask18.SampleLevel(mask_sampler, uv, 0.0);
    case 19: return mask19.SampleLevel(mask_sampler, uv, 0.0);
    case 20: return mask20.SampleLevel(mask_sampler, uv, 0.0);
    case 21: return mask21.SampleLevel(mask_sampler, uv, 0.0);
    case 22: return mask22.SampleLevel(mask_sampler, uv, 0.0);
    case 23: return mask23.SampleLevel(mask_sampler, uv, 0.0);
    case 24: return mask24.SampleLevel(mask_sampler, uv, 0.0);
    case 25: return mask25.SampleLevel(mask_sampler, uv, 0.0);
    case 26: return mask26.SampleLevel(mask_sampler, uv, 0.0);
    case 27: return mask27.SampleLevel(mask_sampler, uv, 0.0);
    case 28: return mask28.SampleLevel(mask_sampler, uv, 0.0);
    case 29: return mask29.SampleLevel(mask_sampler, uv, 0.0);
    case 30: return mask30.SampleLevel(mask_sampler, uv, 0.0);
    case 31: return mask31.SampleLevel(mask_sampler, uv, 0.0);
#endif
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

// How a face's material lights it, the normal its normal map gives it toward the ray's side, the
// normal its diffuse share is lit by and the share of its light it lets through from behind.
struct Lit
{
    float3 shadingNormal;
    float3 diffuseNormal;
    float through;
};

// The face's normal as its normal map bends it, toward the side the ray came from as n is. The map
// is read in the frame the triangle's own texture coordinates give, Bevy's fallback where a mesh
// has no tangents, which a flat card's tangents equal and a curved mesh's interpolated ones would
// not, which no reading has needed. A face met from behind takes the map's normal turned round, as
// Bevy turns it for a double-sided face.
float3 mappedAt(RayHit hit, Material material, float3 n)
{
    int index = int(material.normal_mask);
    if (material.alpha_cutoff < 0.0 || index < 0)
        return n;
    PoolMesh mesh = meshes[hit.mesh];
    Instance placed = instances[hit.instance];
    PoolVertex a = poolCorner(vertices, indices, mesh, hit.triangle, 0u);
    PoolVertex b = poolCorner(vertices, indices, mesh, hit.triangle, 1u);
    PoolVertex c = poolCorner(vertices, indices, mesh, hit.triangle, 2u);
    float3 pa = instanceToWorld(placed, a.position.xyz);
    float3 e1 = instanceToWorld(placed, b.position.xyz) - pa, e2 = instanceToWorld(placed, c.position.xyz) - pa;
    float2 d1 = b.uv.xy - a.uv.xy, d2 = c.uv.xy - a.uv.xy;
    float det = d1.x * d2.y - d2.x * d1.y;
    if (abs(det) < 1e-12)
        return n;
    float3 front = normalize(cross(e1, e2));
    float3 along = (e1 * d2.y - e2 * d1.y) / det;
    float3 up = (e2 * d1.x - e1 * d2.x) / det;
    float3 tangent = normalize(along - front * dot(front, along));
    float3 bitangent = (dot(cross(front, tangent), up) < 0.0 ? -1.0 : 1.0) * cross(front, tangent);

    uint flags = uint(material.flags + 0.5);
    float4 texel = maskAt(index, uvAt(hit));
    float3 nt = texel.rgb * 2.0 - 1.0;
    if ((flags & 2u) != 0u)
        nt.z = sqrt(saturate(1.0 - dot(nt.xy, nt.xy)));
    if ((flags & 4u) != 0u)
        nt.y = -nt.y;
    float3 mapped = normalize(nt.x * tangent + nt.y * bitangent + nt.z * front);
    return dot(front, n) < 0.0 ? -mapped : mapped;
}

// A leaf's way out of its crown where a ray met it, from its corners' second texture coordinates
// unfolded off the square as the leaf material unfolds them (leaves.rs) and turned with its
// instance, or zero where its mesh has none.
float3 wayOutAt(RayHit hit)
{
    PoolMesh mesh = meshes[hit.mesh];
    float3 weights = float3(1.0 - hit.barycentrics.x - hit.barycentrics.y, hit.barycentrics);
    float2 folded = float2(0.0);
    for (uint corner = 0u; corner < 3u; corner++)
    {
        PoolVertex vertex = poolCorner(vertices, indices, mesh, hit.triangle, corner);
        if (vertex.normal.w < 0.5)
            return float3(0.0);
        folded += vertex.uv.zw * weights[corner];
    }
    float2 f = folded * 2.0 - 1.0;
    float3 n = float3(f.x, 1.0 - abs(f.x) - abs(f.y), f.y);
    float t = max(-n.y, 0.0);
    n.x += n.x >= 0.0 ? -t : t;
    n.z += n.z >= 0.0 ? -t : t;
    float3 turned = sceneTransform(instances[hit.instance].world_from_local, float4(normalize(n), 0.0)).xyz;
    return normalize(turned);
}

// How the material lights the face a ray met facing n, as Bevy and the leaf material light it: the
// share let through its diffuse transmission times its texture's alpha, lit by the face's back,
// and the diffuse share by the face's normal, or a leaf's by its normal on the side its way out is
// on bent half way to the way out, as the leaf material bends it (leaves.rs). The reference honors
// the materials' own models, so a leaf is lit as its material defines it, not as a physical leaf.
Lit litAt(RayHit hit, Material material, float3 n)
{
    Lit lit;
    lit.shadingNormal = mappedAt(hit, material, n);
    lit.diffuseNormal = lit.shadingNormal;
    float through = material.diffuse_transmission;
    int index = int(material.transmission_mask);
    if (through > 0.0 && material.alpha_cutoff >= 0.0 && index >= 0)
        through *= maskAt(index, uvAt(hit)).a;
    lit.through = saturate(through);
    // The side the way out is on, or the eye's where the way lies along the card, as a grass
    // blade's straight up does, the leaf material taking the same.
    if ((uint(material.flags + 0.5) & 1u) != 0u)
    {
        float3 way = wayOutAt(hit);
        float3 face = lit.shadingNormal;
        if (dot(way, way) > 0.5)
            lit.diffuseNormal = normalize(lerp(dot(face, way) >= -1e-3 ? face : -face, way, 0.5));
    }
    return lit;
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

// The instances every ray but the eye's first meets, a shadow ray's, a path's past its first face
// and a probe's, those whose entities cast shadows, as Bevy's shadow maps and the scene's distance
// field the probes trace hold them, so a mesh Bevy draws no shadow of, as the meadow's grass, is
// seen by the camera alone.
static const uint PastTheEye = RayCastsShadows;

// No face a ray leaves from, for a ray that passes none.
static const uint2 NoFace = uint2(0xFFFFFFFFu, 0xFFFFFFFFu);

// What a ray meets from origin along way between tMin and tMax among the instances of mask,
// passing the faces cut out where it met them (passesAt), up to MostCutouts of them, and the face
// it leaves from, its instance and its triangle, which a leaf's bent normal can turn a ray back
// across.
RayHit traceThrough(float3 origin, float3 way, float tMin, float tMax, uint mask, uint2 leaving)
{
    float t = tMin;
    RayHit hit;
    for (uint passed = 0u; passed <= MostCutouts; passed++)
    {
        hit = rayTraceIn(scene, origin, way, t, tMax, mask);
        if (!hit.hit || (!passesAt(hit) && any(uint2(hit.instance, hit.triangle) != leaving)))
            return hit;
        t = hit.t + 1e-4 * (1.0 + hit.t);
    }
    hit.hit = false;
    hit.t = tMax;
    return hit;
}

RayHit traceThrough(float3 origin, float3 way, float tMin, float tMax, uint mask)
{
    return traceThrough(origin, way, tMin, tMax, mask, NoFace);
}

// Whether nothing that casts a shadow stops a shadow ray between two points, the faces cut out
// where it met them passed as a path passes them, and the face it leaves from.
bool visibleThrough(float3 from, float3 to, uint2 leaving)
{
    float3 offset = to - from;
    float distance = length(offset);
    if (distance <= 0.0)
        return true;
    return !traceThrough(from, offset / distance, 0.001, distance * 0.999, RayCastsShadows, leaving).hit;
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

// What a light along l sends toward v off a face facing n as its diffuse share, over the light a
// white surface facing it returns, as Bevy's point_light works it out, Burley's diffuse term times
// pi, the lights being given over pi.
float3 bevyDiffuse(float3 n, float3 v, float3 l, float3 diffuse, float roughness)
{
    float3 h = normalize(v + l);
    float nl = saturate(dot(n, l));
    float nv = max(dot(n, v), 1e-4);
    float lh = saturate(dot(l, h));
    float f90 = 0.5 + 2.0 * roughness * lh * lh;
    return diffuse * schlick(float3(1.0), f90, nl).x * schlick(float3(1.0), f90, nv).x;
}

// The same light's specular share, the specular lobe times pi.
float3 bevySpecular(float3 n, float3 v, float3 l, float3 f0, float roughness)
{
    float3 h = normalize(v + l);
    float nl = saturate(dot(n, l));
    float nv = max(dot(n, v), 1e-4);
    float nh = saturate(dot(n, h));
    float lh = saturate(dot(l, h));
    float a2 = roughness * roughness;
    float d = nh * nh * (a2 - 1.0) + 1.0;
    float distribution = a2 / (Pi * d * d);
    float visibility = 0.5 / (nl * sqrt(nv * nv * (1.0 - a2) + a2) + nv * sqrt(nl * nl * (1.0 - a2) + a2));
    float3 fresnel = schlick(f0, saturate(dot(f0, float3(50.0 * 0.33))), lh);
    return distribution * visibility * fresnel * Pi;
}

// What one light along l arriving as given at the face facing n sends toward v, the diffuse share
// by its own normal and the specular by the face's where the face is the path's first, as Bevy
// lights it, and the diffuse share alone as the probes light a face otherwise. The arrivals are
// the light's falloff and its cosine on the diffuse normal, on the face and on its back.
float3 litBy(float3 l, float3 n, float3 v, Lit lit, bool first, float3 diffuse, float3 through, float3 f0, float roughness, float3 arrives)
{
    float3 sum = float3(0.0);
    if (arrives.x > 0.0)
        sum += arrives.x * (first ? bevyDiffuse(lit.diffuseNormal, v, l, diffuse, roughness) : diffuse);
    if (first && arrives.y > 0.0)
        sum += arrives.y * bevySpecular(n, v, l, f0, roughness);
    // The share let through, lit from behind as Bevy lights a transmissive face, by its back and
    // toward the eye's side through it.
    if (arrives.z > 0.0)
        sum += arrives.z * (first ? bevyDiffuse(-n, -v, l, through, roughness) : through);
    return sum;
}

// The light the sun and the lamps send back from p on a face facing n, which a ray left from at
// face, toward v where the face is the path's first, lit as Bevy lights it, and as the probes light
// a face otherwise, the share it lets through lit from behind.
float3 direct(GiLights given, float3 p, float3 n, float3 v, bool first, float3 color, Material material, Lit lit, uint2 face)
{
    float metallic = saturate(material.metallic);
    float roughness = clamp(material.perceptual_roughness, 0.089, 1.0);
    roughness *= roughness;
    float3 base = first ? color * (1.0 - metallic) : color;
    float3 diffuse = base * (1.0 - lit.through), through = base * lit.through;
    float reflectance = material.reflectance;
    float3 f0 = 0.16 * reflectance * reflectance * (1.0 - metallic) + color * metallic;
    float3 start = p + n * 1e-3, back = p - n * 1e-3;
    float3 sum = float3(0.0);

    if (given.sunDirection.w > 0.5)
    {
        float3 l = given.sunDirection.xyz;
        float3 arrives = float3(dot(lit.diffuseNormal, l), dot(lit.shadingNormal, l), lit.through > 0.0 ? dot(-lit.shadingNormal, l) : 0.0);
        bool front = max(arrives.x, arrives.y) > 0.0 && (given.sunColor.w < 0.5 || visibleThrough(start, start + l * 1e5, face));
        bool behind = arrives.z > 0.0 && (given.sunColor.w < 0.5 || visibleThrough(back, back + l * 1e5, face));
        sum += given.sunColor.rgb * litBy(l, lit.shadingNormal, v, lit, first, diffuse, through, f0, roughness, arrives * float3(front ? 1.0 : 0.0, front ? 1.0 : 0.0, behind ? 1.0 : 0.0));
    }
    for (int i = 0; i < min(int(given.counts.x), 16); i++)
    {
        GiLight lamp = given.lights[i];
        float3 arrives = float3(lampLight(lamp, p, lit.diffuseNormal), lampLight(lamp, p, lit.shadingNormal), lit.through > 0.0 ? lampLight(lamp, p, -lit.shadingNormal) : 0.0);
        if (all(arrives <= float3(0.0)))
            continue;
        float3 l = normalize(lamp.positionAndKind.xyz - p);
        float3 to = lamp.positionAndKind.xyz - l * NearShadow;
        bool shadowed = lamp.colorAndShadow.w > 0.5;
        bool front = max(arrives.x, arrives.y) > 0.0 && (!shadowed || visibleThrough(start, to, face));
        bool behind = arrives.z > 0.0 && (!shadowed || visibleThrough(back, to, face));
        sum += lamp.colorAndShadow.rgb * litBy(l, lit.shadingNormal, v, lit, first, diffuse, through, f0, roughness, arrives * float3(front ? 1.0 : 0.0, front ? 1.0 : 0.0, behind ? 1.0 : 0.0));
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

    uint2 leaving = NoFace;
    for (uint bounce = 0u; bounce < counts.z; bounce++)
    {
        RayHit hit = traceThrough(origin, way, 0.0, 1e5, fromCamera && bounce == 0u ? RayEveryInstance : PastTheEye, leaving);
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
        Lit lit = litAt(hit, material, n);
        uint2 face = uint2(hit.instance, hit.triangle);
        float3 emitted = material.emissive.rgb * lerp(1.0, passView.exposure, material.emissive.a);
        light += throughput * (emitted + direct(given, p, n, -way, first, color, material, lit, face));

        // The diffuse share goes on about its normal and the share let through about the face's
        // back, one drawn by how much each is, the first face's past its metal, as the indirect
        // light reaching a pixel lights it.
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
        bool behind = lit.through > 0.0 && random(state) < lit.through;
        origin = behind ? p - n * 1e-3 : p + n * 1e-3;
        way = cosineAbout(behind ? -lit.shadingNormal : lit.diffuseNormal, state);
        leaving = face;
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
        RayHit first = traceThrough(probe.xyz, along, 0.0, 1e5, PastTheEye);
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
        RayHit first = traceThrough(probe.xyz, through, 0.0, 1e5, PastTheEye);
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
    RayHit met = traceThrough(passRayOrigin(middle), way, 0.0, 1e5, RayEveryInstance);
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

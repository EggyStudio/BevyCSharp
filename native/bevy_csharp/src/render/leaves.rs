//! The material a tree's leaves are drawn with, Bevy's standard material lit twice a pixel, its
//! diffuse light by a normal bent out of the crown and the rest by the leaf's own.
//!
//! A crown's leaves each lit by their own card read as a heap of flat cards. Lit as a volume, the
//! side of the crown toward the sun is bright and the far side dark as the crown's shape decides,
//! which bending each leaf's normal out of the crown gives. Bevy lights a surface by one normal, for
//! its diffuse light, its specular and the light let through it, and a normal bent out of the crown
//! and used for all three mirrors the sky across the crown from below, where it points up at the
//! sky, and lights the light let through a leaf from the wrong side. So the leaf is lit twice from
//! Bevy's own lighting of its standard material, once by the bent normal with no specular and
//! nothing let through, for its diffuse light, and once by its own normal, turned round to face the
//! eye as Bevy turns a double-sided card's, with no diffuse light, for its specular and the light
//! let through it, and the two are added.
//!
//! The way out of the crown comes with each vertex in the mesh's second texture coordinates, Bevy's
//! `UV_1`, folded onto a square octahedrally, which the leaves are built with. A mesh without them
//! is lit by its own normal throughout, as the standard material lights it.
//!
//! Lit twice, a leaf's pixel costs about twice the standard material's lighting, every light and
//! shadow looked at twice, and the leaves are drawn forward, never into the deferred buffers, which
//! hold one normal.
//!
//! The leaves sway in the wind as the bark does, through the tree's vertex stages (see
//! [`super::sway`]), so a leaf stays on its twig.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use crate::state::with_world;

/// What the leaf material adds to the standard material, its shaders, the wind it sways in and
/// global illumination's probes, which light it as the light pass lights the G-buffer's surfaces
/// ([`super::forward_probes`]).
#[cfg(feature = "render")]
#[derive(bevy::asset::Asset, bevy::render::render_resource::AsBindGroup, bevy::reflect::Reflect, Debug, Clone, Default)]
#[bind_group_data(LeafKey)]
pub struct LeafLighting {
    #[uniform(100)]
    pub wind: super::sway::TreeWind,
    #[texture(101, dimension = "3d")]
    pub probe_cubes: bevy::asset::Handle<bevy::image::Image>,
    #[texture(102, dimension = "3d")]
    pub probe_reach: bevy::asset::Handle<bevy::image::Image>,
    #[storage(103, read_only)]
    pub probe_field: bevy::asset::Handle<bevy::render::storage::ShaderBuffer>,
    #[storage(104, read_only)]
    pub probe_lights: bevy::asset::Handle<bevy::render::storage::ShaderBuffer>,
    #[storage(105, read_only)]
    pub probe_air: bevy::asset::Handle<bevy::render::storage::ShaderBuffer>,
    #[uniform(106)]
    pub probe_counts: bevy::math::Vec4,
}

/// What a leaf material's pipeline is made for beyond the standard material's, whether it reads
/// the probes, whose reading is compiled into its fragment shader only then, since compiled in and
/// passed over it took a forest's opaque pass from 7.6 ms to 8.1 with global illumination off.
#[cfg(feature = "render")]
#[derive(Clone, Copy, PartialEq, Eq, Hash)]
pub struct LeafKey {
    probed: bool,
}

#[cfg(feature = "render")]
impl From<&LeafLighting> for LeafKey {
    fn from(lighting: &LeafLighting) -> Self {
        Self { probed: lighting.probe_counts.w > 0.5 }
    }
}

/// Whether a leaf material reads the probes as given.
#[cfg(feature = "render")]
pub(crate) fn probed(lighting: &LeafLighting, probes: &super::forward_probes::ForwardProbes) -> bool {
    lighting.probe_cubes == probes.cubes
        && lighting.probe_reach == probes.reach
        && lighting.probe_field == probes.field
        && lighting.probe_lights == probes.lights
        && lighting.probe_air == probes.air
        && lighting.probe_counts == probes.counts
}

/// Has a leaf material read the probes as given.
#[cfg(feature = "render")]
pub(crate) fn probe(lighting: &mut LeafLighting, probes: &super::forward_probes::ForwardProbes) {
    lighting.probe_cubes = probes.cubes.clone();
    lighting.probe_reach = probes.reach.clone();
    lighting.probe_field = probes.field.clone();
    lighting.probe_lights = probes.lights.clone();
    lighting.probe_air = probes.air.clone();
    lighting.probe_counts = probes.counts;
}

#[cfg(feature = "render")]
impl bevy::pbr::MaterialExtension for LeafLighting {
    fn vertex_shader() -> bevy::shader::ShaderRef {
        super::sway::TREE_MAIN.into()
    }

    fn fragment_shader() -> bevy::shader::ShaderRef {
        LEAF_FRAGMENT.into()
    }

    fn prepass_vertex_shader() -> bevy::shader::ShaderRef {
        super::sway::TREE_PREPASS.into()
    }

    fn specialize(
        _pipeline: &bevy::pbr::MaterialExtensionPipeline,
        descriptor: &mut bevy::render::render_resource::RenderPipelineDescriptor,
        layout: &bevy::mesh::MeshVertexBufferLayoutRef,
        key: bevy::pbr::MaterialExtensionKey<Self>,
    ) -> Result<(), bevy::render::render_resource::SpecializedMeshPipelineError> {
        if key.bind_group_data.probed
            && let Some(fragment) = descriptor.fragment.as_mut()
        {
            fragment.shader_defs.push("LEAF_PROBES".into());
        }
        // The vertex stage turns a leaf's way out into the world, which the bark's leaves alone.
        descriptor.vertex.shader_defs.push("LEAF_WAY_OUT".into());
        super::sway::specialize(descriptor, layout)
    }
}

/// The standard material lit as a leaf of a crown.
#[cfg(feature = "render")]
pub type LeafMaterial = bevy::pbr::ExtendedMaterial<bevy::pbr::StandardMaterial, LeafLighting>;

/// The fragment shader a [`LeafMaterial`] draws with.
#[cfg(feature = "render")]
const LEAF_FRAGMENT: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("5b6f2a7e-3c41-4b8e-9d0a-7f1e2c64a913");

/// The leaf's fragment shader's imports, which the probes' reading
/// ([`super::forward_probes::PROBES`]) and the leaf's lighting after it draw on.
#[cfg(feature = "render")]
const LEAF_FRAGMENT_IMPORTS: &str = r#"import bevy_pbr::render::{
    pbr_functions::{alpha_discard, apply_pbr_lighting, main_pass_post_lighting_processing},
    pbr_fragment::pbr_input_from_standard_material,
    forward_io::{VertexOutput, FragmentOutput},
    pbr_types::PbrInput,
    mesh_view_bindings as view_bindings,
    pbr_ambient as ambient,
    pbr_lighting as lighting,
    pbr_lighting::{LAYER_BASE, LAYER_CLEARCOAT},
    clustered_forward as clustering,
    shadows,
};

@if(VISIBILITY_RANGE_DITHER)
import bevy_pbr::render::pbr_functions::visibility_range_dither;

@if(ENVIRONMENT_MAP)
import bevy_pbr::light_probe::environment_map;
"#;

/// The leaf's two lightings. The way out is unfolded from the octahedron, the leaf's own normal is
/// taken on the side of its card the way out is on, by the card's own normal, the eye's where the
/// way lies along the card, and the diffuse light's normal is bent half way from it to the way out.
/// A grass blade's way out is straight up and its card stands upright, so the side was a rounding
/// error's sign and lit many blades from behind, the grass under a sun at the eye's back 32% under
/// its path-traced reference. The first lighting takes the diffuse share of the leaf's color, which
/// Bevy's diffuse transmission leaves it, with no reflectance, so no specular, and nothing let
/// through. The second takes the transmitted share as all it lets through, so no diffuse light, by
/// the leaf's own normal, with its reflectance for the specular and no emission, which the first
/// counted. The occlusion and the leaf's gray scale both, as they scale the standard material.
///
/// While global illumination's probes light the scene, the leaf takes back the light Bevy's
/// lighting gave it from all around and gives itself the probes' in its place, as the light pass
/// does for the G-buffer's surfaces. The first lighting's goes through its occlusion, which Bevy's
/// ambient term, its environment map and an irradiance volume are times, and the probes' is given
/// at the bent normal times that occlusion, as the pass gives it. Bevy gives the second its light
/// from all around through the leaf at an occlusion of one, its ambient term and its environment
/// map's facing away from the leaf's normal, so that is worked out again as bevy_pbr 0.20's
/// `apply_pbr_lighting` works it out (`pbr_functions.wesl`) and taken back, and the probes' facing
/// the same way given, which a Bevy past 0.20 is read against.
#[cfg(feature = "render")]
const LEAF_FRAGMENT_SOURCE: &str = r#"
fn way_out(folded: vec2<f32>) -> vec3<f32> {
    let f = folded * 2.0 - 1.0;
    var n = vec3<f32>(f.x, 1.0 - abs(f.x) - abs(f.y), f.y);
    let t = max(-n.y, 0.0);
    n.x += select(t, -t, n.x >= 0.0);
    n.z += select(t, -t, n.z >= 0.0);
    return normalize(n);
}

// What Bevy's lighting gives a share let through of `transmissive` color from all around, exposed.
fn through_given(in: PbrInput, transmissive: vec3<f32>) -> vec3<f32> {
    var light = ambient::ambient_light(in.world_position, -in.N, -in.V, 1.0, transmissive, vec3<f32>(0.0), 1.0, vec3<f32>(1.0));
@if(ENVIRONMENT_MAP) {
    let view_z = dot(vec4<f32>(
        view_bindings::view.view_from_world[0].z,
        view_bindings::view.view_from_world[1].z,
        view_bindings::view.view_from_world[2].z,
        view_bindings::view.view_from_world[3].z
    ), in.world_position);
    let cluster_index = clustering::view_fragment_cluster_index(in.frag_coord.xy, view_z, in.is_orthographic);
    var ranges = clustering::unpack_clusterable_object_index_ranges(cluster_index);
    let perceptual_roughness = in.material.perceptual_roughness;
    let T = -normalize(in.V + refract(in.V, -in.N, 1.0 / in.material.ior) * in.material.thickness);
    var input: lighting::LightingInput;
    input.diffuse_color = vec3(1.0);
    input.layers[LAYER_BASE].NdotV = 1.0;
    input.P = in.world_position.xyz;
    input.layers[LAYER_BASE].N = -in.N;
    input.V = in.V;
    input.layers[LAYER_BASE].R = T;
    input.layers[LAYER_BASE].perceptual_roughness = perceptual_roughness;
    input.layers[LAYER_BASE].roughness = lighting::perceptualRoughnessToRoughness(perceptual_roughness);
    input.metallic = 0.0;
    input.F0_dielectric = vec3<f32>(1.0);
    input.F0_metallic = vec3<f32>(0.0);
    input.F_ab = vec2(0.1);
@if(STANDARD_MATERIAL_CLEARCOAT) {
    input.clearcoat_strength = 0.0;
    input.layers[LAYER_CLEARCOAT].NdotV = 0.0;
    input.layers[LAYER_CLEARCOAT].N = in.N;
    input.layers[LAYER_CLEARCOAT].R = vec3(0.0);
    input.layers[LAYER_CLEARCOAT].perceptual_roughness = 0.0;
    input.layers[LAYER_CLEARCOAT].roughness = 0.0;
}
    light += environment_map::environment_map_light(&input, &ranges, false).diffuse * transmissive;
}
    return light * view_bindings::view.exposure;
}

@fragment
fn fragment(in: VertexOutput, @builtin(front_facing) is_front: bool) -> FragmentOutput {
@if(VISIBILITY_RANGE_DITHER)
    visibility_range_dither(in.position, in.visibility_range_dither);

    var pbr_input = pbr_input_from_standard_material(in, is_front);
    pbr_input.material.base_color = alpha_discard(pbr_input.material.flags, pbr_input.material.alpha_cutoff, pbr_input.material.base_color);
    // The card's own normal turned to the eye. A blade seen edge on near the eye reads its normal
    // map's smallest levels, which average its normals to nothing, and Bevy's normalizing nothing
    // gives no number, which spread a few black pixels through the picture, so the card's normal
    // stands in where the mapped one has no length.
    let card = select(-pbr_input.world_normal, pbr_input.world_normal, is_front);
    pbr_input.N = select(card, pbr_input.N, dot(pbr_input.N, pbr_input.N) > 0.5);
    let through = pbr_input.material.diffuse_transmission;

    var diffuse = pbr_input;
@if(VERTEX_UVS_B) {
    let way = way_out(in.uv_b);
    // The side the way out is on, by the card's own normal, or the eye's where the way lies along
    // the card, as a grass blade's straight up does, whose dot is a rounding error's sign, and
    // whose mapped normal leans up and down with its normal map.
    let face = select(-pbr_input.N, pbr_input.N, dot(card, way) >= -1e-3);
    // A normal its map turns straight against the way out bends to nothing, which normalized is no
    // number and spreads through the picture, so it takes the way out itself.
    let bent = mix(face, way, 0.5);
    diffuse.N = select(way, normalize(bent), dot(bent, bent) > 1e-8);
}
    diffuse.material.base_color = vec4<f32>(pbr_input.material.base_color.rgb * (1.0 - through), pbr_input.material.base_color.a);
    diffuse.material.reflectance = vec3<f32>(0.0);
    diffuse.material.diffuse_transmission = 0.0;

    var rest = pbr_input;
    rest.material.base_color = vec4<f32>(pbr_input.material.base_color.rgb * through, pbr_input.material.base_color.a);
    rest.material.diffuse_transmission = 1.0;
    rest.material.emissive = vec4<f32>(0.0);

    var given = vec3<f32>(0.0);
@if(LEAF_PROBES)
    if probes_light() {
        let kept = (1.0 - pbr_input.material.metallic) * (1.0 - pbr_input.material.specular_transmission);
        let occlusion = diffuse.diffuse_occlusion;
        diffuse.diffuse_occlusion = vec3<f32>(0.0);
        var behind = diffuse.N;
        var transmissive = vec3<f32>(0.0);
@if(STANDARD_MATERIAL_DIFFUSE_TRANSMISSION) {
        behind = -rest.N;
        transmissive = rest.material.base_color.rgb * kept;
}
        let probed = probes_given(pbr_input.world_position.xyz, diffuse.N, diffuse.material.base_color.rgb * kept, behind, transmissive, pbr_input.frag_coord.xy);
        given += probed.front * occlusion;
@if(STANDARD_MATERIAL_DIFFUSE_TRANSMISSION) {
        given += probed.through - through_given(rest, transmissive);
}
    }

    var out: FragmentOutput;
    out.color = vec4<f32>(apply_pbr_lighting(diffuse).rgb + apply_pbr_lighting(rest).rgb + given, pbr_input.material.base_color.a);
    out.color = main_pass_post_lighting_processing(pbr_input, out.color);
    return out;
}
"#;

/// Adds the leaf material's plugin and its shader.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    app.add_plugins(bevy::pbr::MaterialPlugin::<LeafMaterial>::default());
    super::forward_probes::install(app);
    if let Some(mut shaders) = app.world_mut().get_resource_mut::<bevy::asset::Assets<bevy::shader::Shader>>() {
        // A UUID handle, whose insertion cannot fail.
        let _ = shaders.insert(
            LEAF_FRAGMENT.id(),
            bevy::shader::Shader::from_wesl(format!("{LEAF_FRAGMENT_IMPORTS}{}{LEAF_FRAGMENT_SOURCE}", super::forward_probes::PROBES), "bcs/leaf_fragment.wesl"),
        );
    }
}

/// Puts a leaf material on an entity when `material` is one, answering whether it was.
///
/// The asset table holds handles untyped, so an attach that is not of the standard material asks
/// each kind it might be, and this is one.
#[cfg(feature = "render")]
pub fn attach(entity: &mut bevy::ecs::world::EntityWorldMut, material: &bevy::asset::UntypedHandle) -> bool {
    match material.clone().try_typed::<LeafMaterial>() {
        Ok(handle) => {
            entity.insert(bevy::pbr::MeshMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

/// Makes a leaf material from the settings a standard material is made with and the tree's
/// stiffness in the wind, as [`super::sway::TreeWind`] holds it, and returns its key.
///
/// The standard material is drawn forward whatever the camera, since the deferred buffers hold one
/// normal and the leaf is lit by two.
///
/// # Safety
///
/// `config` must be null or point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create_leaf(
    config: *const BcsMaterialConfig,
    trunk: f32,
    limb: f32,
    flutter: f32,
    frequency: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (config, trunk, limb, flutter, frequency);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let mut base = match super::assets::standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                base.opaque_render_method = bevy::material::OpaqueRendererMethod::Forward;
                let wind = super::sway::TreeWind::new(world, trunk, limb, flutter, frequency);
                let Some(probes) = super::forward_probes::current(world) else {
                    return status::UNSUPPORTED;
                };
                let mut extension = LeafLighting { wind, ..Default::default() };
                probe(&mut extension, &probes);
                let material = LeafMaterial { base, extension };

                let Some(mut materials) = world.get_resource_mut::<Assets<LeafMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

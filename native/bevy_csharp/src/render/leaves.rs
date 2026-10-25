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

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use crate::state::with_world;

/// What the leaf material adds to the standard material, nothing but its shader.
#[cfg(feature = "render")]
#[derive(bevy::asset::Asset, bevy::render::render_resource::AsBindGroup, bevy::reflect::Reflect, Debug, Clone, Default)]
pub struct LeafLighting {}

#[cfg(feature = "render")]
impl bevy::pbr::MaterialExtension for LeafLighting {
    fn fragment_shader() -> bevy::shader::ShaderRef {
        LEAF_FRAGMENT.into()
    }
}

/// The standard material lit as a leaf of a crown.
#[cfg(feature = "render")]
pub type LeafMaterial = bevy::pbr::ExtendedMaterial<bevy::pbr::StandardMaterial, LeafLighting>;

/// The fragment shader a [`LeafMaterial`] draws with.
#[cfg(feature = "render")]
const LEAF_FRAGMENT: bevy::asset::Handle<bevy::shader::Shader> =
    bevy::asset::uuid_handle!("5b6f2a7e-3c41-4b8e-9d0a-7f1e2c64a913");

/// The leaf's two lightings. The way out is unfolded from the octahedron, the leaf's own normal is
/// taken on the side of its card the way out is on, and the diffuse light's normal is bent half way
/// from it to the way out. The first lighting takes the diffuse share of the leaf's color, which
/// Bevy's diffuse transmission leaves it, with no reflectance, so no specular, and nothing let
/// through. The second takes the transmitted share as all it lets through, so no diffuse light, by
/// the leaf's own normal, with its reflectance for the specular and no emission, which the first
/// counted. The occlusion and the leaf's gray scale both, as they scale the standard material.
#[cfg(feature = "render")]
const LEAF_FRAGMENT_SOURCE: &str = r#"import bevy_pbr::render::{
    pbr_functions::{alpha_discard, apply_pbr_lighting, main_pass_post_lighting_processing},
    pbr_fragment::pbr_input_from_standard_material,
    forward_io::{VertexOutput, FragmentOutput},
};

@if(VISIBILITY_RANGE_DITHER)
import bevy_pbr::render::pbr_functions::visibility_range_dither;

fn way_out(folded: vec2<f32>) -> vec3<f32> {
    let f = folded * 2.0 - 1.0;
    var n = vec3<f32>(f.x, 1.0 - abs(f.x) - abs(f.y), f.y);
    let t = max(-n.y, 0.0);
    n.x += select(t, -t, n.x >= 0.0);
    n.z += select(t, -t, n.z >= 0.0);
    return normalize(n);
}

@fragment
fn fragment(in: VertexOutput, @builtin(front_facing) is_front: bool) -> FragmentOutput {
@if(VISIBILITY_RANGE_DITHER)
    visibility_range_dither(in.position, in.visibility_range_dither);

    var pbr_input = pbr_input_from_standard_material(in, is_front);
    pbr_input.material.base_color = alpha_discard(pbr_input.material.flags, pbr_input.material.alpha_cutoff, pbr_input.material.base_color);
    let through = pbr_input.material.diffuse_transmission;

    var diffuse = pbr_input;
@if(VERTEX_UVS_B) {
    let way = way_out(in.uv_b);
    let face = select(-pbr_input.N, pbr_input.N, dot(pbr_input.N, way) >= 0.0);
    diffuse.N = normalize(mix(face, way, 0.5));
}
    diffuse.material.base_color = vec4<f32>(pbr_input.material.base_color.rgb * (1.0 - through), pbr_input.material.base_color.a);
    diffuse.material.reflectance = vec3<f32>(0.0);
    diffuse.material.diffuse_transmission = 0.0;

    var rest = pbr_input;
    rest.material.base_color = vec4<f32>(pbr_input.material.base_color.rgb * through, pbr_input.material.base_color.a);
    rest.material.diffuse_transmission = 1.0;
    rest.material.emissive = vec4<f32>(0.0);

    var out: FragmentOutput;
    out.color = vec4<f32>(apply_pbr_lighting(diffuse).rgb + apply_pbr_lighting(rest).rgb, pbr_input.material.base_color.a);
    out.color = main_pass_post_lighting_processing(pbr_input, out.color);
    return out;
}
"#;

/// Adds the leaf material's plugin and its shader.
#[cfg(feature = "render")]
pub(crate) fn install(app: &mut bevy::app::App) {
    app.add_plugins(bevy::pbr::MaterialPlugin::<LeafMaterial>::default());
    if let Some(mut shaders) = app.world_mut().get_resource_mut::<bevy::asset::Assets<bevy::shader::Shader>>() {
        // A UUID handle, whose insertion cannot fail.
        let _ = shaders.insert(
            LEAF_FRAGMENT.id(),
            bevy::shader::Shader::from_wesl(LEAF_FRAGMENT_SOURCE, "bcs/leaf_fragment.wesl"),
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

/// Makes a leaf material from the settings a standard material is made with and returns its key.
///
/// The standard material is drawn forward whatever the camera, since the deferred buffers hold one
/// normal and the leaf is lit by two.
///
/// # Safety
///
/// `config` must be null or point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create_leaf(config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
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
                let material = LeafMaterial { base, extension: LeafLighting {} };

                let Some(mut materials) = world.get_resource_mut::<Assets<LeafMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

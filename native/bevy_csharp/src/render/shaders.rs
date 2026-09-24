//! Materials whose fragment shader is a file the caller names.
//!
//! Bevy decides a material's shader from its Rust type, through an associated function with no
//! `self`, so one type draws with one shader and a game that wants two wants two types. C# cannot
//! declare a Rust type, which is the same wall the state slots hit, and the answer is the same: the
//! bridge declares a fixed set of material types and the managed side says what each one is for
//! before the app runs.
//!
//! What a slot carries is deliberately plain. Sixteen floats and one picture cover a great deal
//! (a color, a scroll, a threshold, a mask) and anything past that is a bind group layout the
//! caller would have to describe, which is a second language to learn rather than a shader to
//! write.

#![cfg(feature = "render")]

use std::sync::OnceLock;

use bevy::app::App;
use bevy::asset::{Asset, AssetPath, Handle};
use bevy::image::Image;
use bevy::math::Vec4;
use bevy::pbr::{Material, MaterialPlugin};
use bevy::reflect::TypePath;
use bevy::render::render_resource::{AsBindGroup, ShaderType};
use bevy::shader::ShaderRef;

use crate::interop::status;

/// How many floats one material carries, as four vectors of four.
pub const PARAM_COUNT: usize = 16;

/// The numbers a shader material hands its shader.
///
/// Four vectors rather than sixteen floats, because a uniform is laid out in sixteen-byte rows and
/// a shader reading `vec4` arrays is reading exactly what is written.
#[derive(Clone, Copy, Default, ShaderType)]
pub struct ShaderParams {
    /// The numbers, in the order the caller gave them.
    pub values: [Vec4; PARAM_COUNT / 4],
}

/// Declares the material slots and the dispatch that reaches them by index.
macro_rules! shader_slots {
    ($($ty:ident = $slot:literal),+ $(,)?) => {
        $(
            /// One material slot, drawn with whatever shader was named for it.
            #[derive(Asset, AsBindGroup, TypePath, Clone, Default)]
            pub struct $ty {
                /// The numbers, at binding zero.
                #[uniform(0)]
                pub params: ShaderParams,
                /// The picture, at bindings one and two.
                #[texture(1)]
                #[sampler(2)]
                pub texture: Option<Handle<Image>>,
                /// How the renderer treats what this material draws where it is not opaque.
                pub alpha: bevy::material::AlphaMode,
            }

            impl Material for $ty {
                fn fragment_shader() -> ShaderRef {
                    // Read from a static, because Bevy asks the type rather than the instance.
                    // The managed side writes it before the app runs, and a slot with nothing
                    // written is a slot whose plugin was never added.
                    match SHADERS[$slot].get() {
                        Some(path) => ShaderRef::Path(AssetPath::from(path.clone())),
                        None => ShaderRef::Default,
                    }
                }

                fn vertex_shader() -> ShaderRef {
                    // Optional, unlike the fragment one, because most materials want the mesh
                    // where the mesh is and only some displace it.
                    match VERTEX_SHADERS[$slot].get() {
                        Some(path) => ShaderRef::Path(AssetPath::from(path.clone())),
                        None => ShaderRef::Default,
                    }
                }

                // Per material rather than per slot, because two things drawn by one shader can
                // still want different answers, and Bevy asks the instance for this one.
                fn alpha_mode(&self) -> bevy::material::AlphaMode {
                    self.alpha
                }
            }
        )+

        /// How many shader materials this bridge provides.
        pub const SLOT_COUNT: i32 = 0 $(+ { let _ = $slot; 1 })+;

        /// Which shader each slot draws with.
        ///
        /// Process-wide rather than per app, because the function Bevy asks is a static one. A
        /// second app in the same process keeps what the first wrote, and writing a different
        /// shader to a slot that already has one is refused rather than silently ignored.
        static SHADERS: [OnceLock<String>; SLOT_COUNT as usize] =
            [const { OnceLock::new() }; SLOT_COUNT as usize];

        /// Which vertex shader each slot draws with, where one was named.
        static VERTEX_SHADERS: [OnceLock<String>; SLOT_COUNT as usize] =
            [const { OnceLock::new() }; SLOT_COUNT as usize];

        /// Points a slot at a shader and installs what draws with it.
        pub fn install(app: &mut App, slot: i32, path: String, vertex: Option<String>) -> i32 {
            match slot {
                $($slot => {
                    // One shader for the life of the process, because it is baked into what Bevy
                    // asks the type for and a second answer would apply to materials already made
                    // from the first. Saying the same thing again is a caller repeating itself,
                    // which is what a second app in one process does and is not a mistake.
                    match SHADERS[$slot].get() {
                        Some(already) if *already != path => return status::INVALID_STATE,
                        Some(_) => {}
                        None => {
                            let _ = SHADERS[$slot].set(path);

                            if let Some(vertex) = vertex {
                                let _ = VERTEX_SHADERS[$slot].set(vertex);
                            }
                        }
                    }

                    app.add_plugins(MaterialPlugin::<$ty>::default());
                    status::OK
                })+
                _ => status::NULL_ARG,
            }
        }

        /// Makes a material in `slot` and answers its asset key.
        pub fn create(
            world: &mut bevy::ecs::world::World,
            slot: i32,
            params: ShaderParams,
            texture: Option<Handle<Image>>,
            alpha: bevy::material::AlphaMode,
        ) -> i32 {
            match slot {
                $($slot => {
                    // A slot nothing was said about has no plugin, so an asset made for it would
                    // be a handle nothing draws.
                    if SHADERS[$slot].get().is_none() {
                        return status::INVALID_STATE;
                    }

                    let material = $ty { params, texture, alpha };

                    let Some(mut assets) =
                        world.get_resource_mut::<bevy::asset::Assets<$ty>>()
                    else {
                        return status::INVALID_STATE;
                    };

                    let handle = assets.add(material);
                    crate::assets::insert_handle(world, handle.untyped())
                })+
                _ => status::NULL_ARG,
            }
        }

        /// Gives an entity a shader material, if the handle names one.
        ///
        /// Tried in turn because the asset table is untyped, so what a key stands for is only
        /// known by asking each type whether it is theirs.
        pub fn attach(
            entity: &mut bevy::ecs::world::EntityWorldMut,
            untyped: &bevy::asset::UntypedHandle,
        ) -> bool {
            $(
                if let Ok(handle) = untyped.clone().try_typed::<$ty>() {
                    entity.insert(bevy::pbr::MeshMaterial3d(handle));
                    return true;
                }
            )+

            false
        }
    };
}

shader_slots!(
    BcsShader0 = 0,
    BcsShader1 = 1,
    BcsShader2 = 2,
    BcsShader3 = 3,
);

/// Reports how many shader material slots this bridge provides.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_slots() -> i32 {
    SLOT_COUNT
}

/// Points a slot at a fragment shader, and installs what draws with it.
///
/// Must happen before the app runs, because the plugin it adds brings a render pipeline and the
/// systems that feed it. A slot takes one shader for the life of the process, since Bevy asks the
/// material's type rather than the material, and a different second answer would apply to
/// everything already made from the first.
///
/// `path` is a `.wgsl` file under the asset root. Its fragment entry point is called `fragment`,
/// and the material's own bind group is group three, where binding zero is sixteen floats as four
/// `vec4`, binding one is a texture and binding two its sampler.
///
/// `vertex` names a vertex shader as well, or is null to leave the mesh where the mesh is, which
/// is what all but a material that displaces its own geometry wants. Its entry point is called
/// `vertex`.
///
/// Returns [`status::INVALID_STATE`] where the slot already has a different shader, and
/// [`status::NULL_ARG`] where there is no such slot. Naming the same shader again is allowed, so
/// a second app in one process can install the same slots.
///
/// # Safety
/// `handle` must be a live app, `path` a NUL-terminated UTF-8 string, and `vertex` null or one.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_slot(
    handle: *mut crate::state::BcsApp,
    slot: i32,
    path: *const core::ffi::c_char,
    vertex: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { crate::state::app_mut(handle) }) else {
            return status::NULL_ARG;
        };

        if app.running {
            return status::ALREADY_RUNNING;
        }

        let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };

        let vertex = if vertex.is_null() {
            None
        } else {
            unsafe { crate::interop::cstr_to_string(vertex) }
        };

        install(&mut app.app, slot, path, vertex)
    })
}

/// Makes a material drawn by one of the slots, and answers its asset key.
///
/// `params` points at up to sixteen floats, which reach the shader as four `vec4` in the order
/// they were given, and anything past what was passed is zero. A negative `texture` leaves the
/// picture unbound, which a shader that does not sample one does not notice.
///
/// `alpha` is `0` opaque, `1` masked at half, `2` blended and sorted, `3` added to what is behind.
/// A shader writing anything but one in its alpha channel wants one of the last two, since an
/// opaque material's alpha is not read at all.
///
/// Returns a negative where the slot has no shader, the key names no image, or there is no
/// renderer.
///
/// # Safety
/// `params` must point at `count` readable floats, or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_material_create(
    slot: i32,
    params: *const f32,
    count: i32,
    texture: i32,
    alpha: i32,
) -> i32 {
    crate::interop::guard_with(-1, || {
        if count < 0 || count as usize > PARAM_COUNT || (params.is_null() && count > 0) {
            return status::NULL_ARG;
        }

        let mut values = ShaderParams::default();

        if count > 0 {
            let given = unsafe { core::slice::from_raw_parts(params, count as usize) };

            for (index, number) in given.iter().enumerate() {
                values.values[index / 4][index % 4] = *number;
            }
        }

        crate::state::with_world(|world| {
            let texture = match crate::render::image_handle(world, texture) {
                Err(refusal) => return refusal,
                Ok(handle) => handle,
            };

            let alpha = match alpha {
                1 => bevy::material::AlphaMode::Mask(0.5),
                2 => bevy::material::AlphaMode::Blend,
                3 => bevy::material::AlphaMode::Add,
                _ => bevy::material::AlphaMode::Opaque,
            };

            create(world, slot, values, texture, alpha)
        })
    })
}

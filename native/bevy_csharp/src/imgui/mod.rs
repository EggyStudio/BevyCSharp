//! Dear ImGui, drawn by Bevy.
//!
//! The interface itself lives on the managed side: it owns the ImGui context, builds the windows
//! and asks ImGui for the triangles they came to. What is here is the other half of that bargain:
//! the triangles arrive over the ABI once a frame, and a pass in Bevy's renderer draws them over
//! whatever the scene drew.
//!
//! Nothing in this module knows what a window or a widget is. It draws clipped, textured triangles
//! in screen space, which is the whole of what an ImGui backend has to do.

#[cfg(feature = "editor")]
pub mod render;

#[cfg(feature = "editor")]
use crate::interop::status;

/// One vertex, laid out exactly as `ImDrawVert` is.
///
/// The managed side hands over ImGui's own buffers rather than a copy in another shape, so this
/// must stay byte for byte what ImGui writes: two floats of position, two of texture coordinate,
/// and a packed RGBA byte colour.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default, bytemuck::Pod, bytemuck::Zeroable)]
pub struct BcsImGuiVertex {
    /// Where the vertex is, in logical pixels from the top left.
    pub position: [f32; 2],

    /// Where it reads from its texture.
    pub uv: [f32; 2],

    /// Its colour, as ImGui packs one: red in the low byte, alpha in the high one.
    pub color: u32,
}

/// One draw call: a run of indices, clipped to a rectangle, reading from one texture.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct BcsImGuiCommand {
    /// Which texture the triangles read from, as [`bcs_imgui_texture`] answered.
    pub texture: u64,

    /// Left, top, right and bottom of what may be drawn, in logical pixels.
    pub clip: [f32; 4],

    /// Where in the index buffer this call's indices start.
    pub index: u32,

    /// What to add to every index, which is where this call's vertices start.
    pub vertex: u32,

    /// How many indices it draws.
    pub elements: u32,
}

/// Everything ImGui produced for one frame.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct BcsImGuiFrame {
    /// The vertices, and how many there are.
    pub vertices: *const BcsImGuiVertex,

    /// How many vertices `vertices` holds.
    pub vertex_count: u32,

    /// The indices, two bytes each, as ImGui writes them.
    pub indices: *const u16,

    /// How many indices `indices` holds.
    pub index_count: u32,

    /// The draw calls, in the order they are to be made.
    pub commands: *const BcsImGuiCommand,

    /// How many draw calls `commands` holds.
    pub command_count: u32,

    /// Where the top left of the interface is, which ImGui calls the display position.
    pub display_position: [f32; 2],

    /// How large the interface is, in logical pixels.
    pub display_size: [f32; 2],

    /// How many physical pixels a logical one is, per axis.
    pub framebuffer_scale: [f32; 2],
}

/// Takes this frame's triangles.
///
/// Copied rather than kept: the pointers are ImGui's own buffers, and ImGui is free to reuse them
/// the moment this returns.
///
/// # Safety
/// `frame` must point to a [`BcsImGuiFrame`] whose buffers hold the counts it declares.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_imgui_frame(frame: *const BcsImGuiFrame) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = frame;
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            if frame.is_null() {
                return status::NULL_ARG;
            }

            let frame = unsafe { &*frame };

            let vertices = if frame.vertex_count == 0 {
                Vec::new()
            } else {
                if frame.vertices.is_null() {
                    return status::NULL_ARG;
                }

                unsafe {
                    std::slice::from_raw_parts(frame.vertices, frame.vertex_count as usize).to_vec()
                }
            };

            let indices = if frame.index_count == 0 {
                Vec::new()
            } else {
                if frame.indices.is_null() {
                    return status::NULL_ARG;
                }

                unsafe {
                    std::slice::from_raw_parts(frame.indices, frame.index_count as usize).to_vec()
                }
            };

            let commands = if frame.command_count == 0 {
                Vec::new()
            } else {
                if frame.commands.is_null() {
                    return status::NULL_ARG;
                }

                unsafe {
                    std::slice::from_raw_parts(frame.commands, frame.command_count as usize).to_vec()
                }
            };

            crate::state::with_world(|world| {
                let Some(mut drawn) = world.get_resource_mut::<render::Drawn>() else {
                    return status::INVALID_STATE;
                };

                drawn.vertices = vertices;
                drawn.indices = indices;
                drawn.commands = commands;
                drawn.display_position = frame.display_position;
                drawn.display_size = frame.display_size;
                drawn.framebuffer_scale = frame.framebuffer_scale;

                status::OK
            })
        }
    })
}

/// Takes a picture the interface draws with, and answers what to call it.
///
/// The font atlas arrives this way like anything else: ImGui gives it as pixels and asks for a
/// name to put in its draw calls, and nothing about it is special to this side.
///
/// # Safety
/// `pixels` must point to `width * height * 4` bytes of RGBA.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_imgui_texture(pixels: *const u8, width: u32, height: u32) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = (pixels, width, height);
            0
        }

        #[cfg(feature = "editor")]
        {
            if pixels.is_null() || width == 0 || height == 0 {
                return 0;
            }

            let count = width as usize * height as usize * 4;
            let pixels = unsafe { std::slice::from_raw_parts(pixels, count) }.to_vec();

            crate::state::with_world_opt(|world| {
                let image = render::picture(pixels, width, height);
                let handle = world
                    .get_resource_mut::<bevy::asset::Assets<bevy::image::Image>>()?
                    .add(image);

                let mut pictures = world.get_resource_mut::<render::Pictures>()?;
                Some(pictures.add(handle))
            })
            .flatten()
            .unwrap_or(0)
        }
    })
}

/// Takes a picture from a file under the asset root, and answers what to call it.
///
/// What an icon is: a file the editor ships, loaded the way every other asset is, so it is decoded
/// by the engine rather than by the managed side. It is not there for a frame or two, and a draw
/// call naming a picture that has not arrived draws nothing rather than something wrong.
///
/// # Safety
/// `path` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_imgui_picture(path: *const core::ffi::c_char) -> u64 {
    crate::interop::guard_with(0, || {
        #[cfg(not(feature = "editor"))]
        {
            let _ = path;
            0
        }

        #[cfg(feature = "editor")]
        {
            let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
                return 0;
            };

            crate::state::with_world_opt(|world| {
                let handle = world
                    .get_resource::<bevy::asset::AssetServer>()?
                    .load::<bevy::image::Image>(path);

                let mut pictures = world.get_resource_mut::<render::Pictures>()?;
                Some(pictures.add(handle))
            })
            .flatten()
            .unwrap_or(0)
        }
    })
}

/// Forgets a picture, so the memory behind it can go.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_imgui_drop_texture(texture: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "editor"))]
        {
            let _ = texture;
            crate::interop::status::UNSUPPORTED
        }

        #[cfg(feature = "editor")]
        {
            crate::state::with_world(|world| {
                let Some(mut pictures) = world.get_resource_mut::<render::Pictures>() else {
                    return status::INVALID_STATE;
                };

                pictures.remove(texture);
                status::OK
            })
        }
    })
}

/// Puts the interface into an app.
#[cfg(feature = "editor")]
pub fn install(app: &mut bevy::app::App) {
    render::install(app);
}

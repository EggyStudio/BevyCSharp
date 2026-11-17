//! The scene's information a camera's shaders read among its inputs, in buffers kept from one frame
//! to the next.
//!
//! A camera binds its inputs at each point of its frame where something of its runs, and the
//! information is the same at each of them, and from one frame to the next while the scene and the
//! camera stand still, so a buffer holding it is bound again rather than made again. A buffer is
//! written only where it was not bound this frame, since a write lands before anything the frame
//! runs, and a shader bound to it earlier in the frame would read the later information.

use std::sync::Mutex;

use bevy::ecs::system::Res;
use bevy::render::render_resource::{Buffer, BufferInitDescriptor, BufferUsages};
use bevy::render::renderer::{RenderDevice, RenderQueue};

use super::ViewInputs;

/// The scene's information as the inputs bind it, at binding 13.
pub(super) type Info = [u32; 32];

/// The buffers of the scene's information, each with what it holds and whether it was bound this
/// frame.
pub(super) struct SceneInfo {
    queue: RenderQueue,
    buffers: Mutex<Vec<(Buffer, Info, bool)>>,
}

impl SceneInfo {
    pub(super) fn new(queue: RenderQueue) -> Self {
        Self {
            queue,
            buffers: Mutex::new(Vec::new()),
        }
    }

    /// A buffer holding `info`, the one that holds it where there is one, else one not bound this
    /// frame written with it, else a new one.
    pub(super) fn buffer(&self, device: &RenderDevice, info: &Info) -> Buffer {
        let mut buffers = self.buffers.lock().unwrap_or_else(|poisoned| poisoned.into_inner());

        if let Some((buffer, _, bound)) = buffers.iter_mut().find(|(_, held, _)| held == info) {
            *bound = true;
            return buffer.clone();
        }

        if let Some((buffer, held, bound)) = buffers.iter_mut().find(|(_, _, bound)| !*bound) {
            self.queue.write_buffer(buffer, 0, bytemuck::cast_slice(info));
            (*held, *bound) = (*info, true);
            return buffer.clone();
        }

        crate::render::values::made_buffer();

        let buffer = device.create_buffer_with_data(&BufferInitDescriptor {
            label: Some("bcs_view_scene_info"),
            contents: bytemuck::cast_slice(info),
            usage: BufferUsages::UNIFORM | BufferUsages::COPY_DST,
        });

        buffers.push((buffer.clone(), *info, true));
        buffer
    }
}

/// Lets each buffer of the scene's information be written again, before any camera of the frame
/// binds its inputs.
pub(super) fn unbind_scene_info(inputs: Res<ViewInputs>) {
    let mut buffers = inputs.scene_info.buffers.lock().unwrap_or_else(|poisoned| poisoned.into_inner());

    for (_, _, bound) in buffers.iter_mut() {
        *bound = false;
    }
}

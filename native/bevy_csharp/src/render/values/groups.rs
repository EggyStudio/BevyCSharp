//! The bind group values become, made once for a material or a pass, or kept from one frame to the
//! next for a list of dispatches or draws asked for again every frame.
//!
//! A dispatch or a draw is asked for anew each frame, by the game or by the camera that holds it,
//! and made into a group of new buffers each time would ask the GPU every frame for buffers and
//! groups that differ from the last frame's in nothing. [`KeptGroups`] keeps each one's
//! group and the buffers of its numbers by its place in its list, writes numbers that moved into
//! the buffer it has, and makes the group again only where something it binds is another than the
//! last frame's, as a camera's history image is from one frame to the next.

use std::sync::atomic::{AtomicU64, Ordering};

use bevy::render::render_resource::{
    BindGroup, BindGroupEntry, BindGroupLayout, BindGroupLayoutId, BindingResource, Buffer,
    BufferId, BufferInitDescriptor, BufferUsages, SamplerId, TextureId, TextureViewId,
};
use bevy::render::renderer::{RenderDevice, RenderQueue};

use super::Packed;

/// How many buffers of a shader's numbers the bridge has made since the process started.
static BUFFERS_MADE: AtomicU64 = AtomicU64::new(0);

/// How many bind groups of a shader's values it has made.
static GROUPS_MADE: AtomicU64 = AtomicU64::new(0);

/// The buffers of a shader's numbers and the bind groups of its values made so far, which the
/// `memory` command says.
pub fn made() -> (u64, u64) {
    (BUFFERS_MADE.load(Ordering::Relaxed), GROUPS_MADE.load(Ordering::Relaxed))
}

/// Counts one more buffer of a shader's numbers, for a buffer made outside this module.
pub fn made_buffer() {
    BUFFERS_MADE.fetch_add(1, Ordering::Relaxed);
}

/// A buffer holding one block of numbers.
fn numbers_buffer(device: &RenderDevice, bytes: &[u8]) -> Buffer {
    made_buffer();

    device.create_buffer_with_data(&BufferInitDescriptor {
        label: Some("bcs_shader_numbers"),
        contents: bytes,
        // Uniform as well, for a shader handed over as SPIR-V, which keeps its blocks of numbers as
        // the uniform buffers Slang declared, and written in place where a group is kept.
        usage: BufferUsages::STORAGE | BufferUsages::UNIFORM | BufferUsages::COPY_DST,
    })
}

/// What a group binds beside its numbers, each at its binding and by what it is.
#[derive(PartialEq)]
enum Mark {
    Buffer(u32, BufferId),
    View(u32, TextureViewId),
    /// A view of one mip level, which is made again on each call, so known by its texture and
    /// level.
    Level(u32, TextureId, u32),
    Sampler(u32, SamplerId),
}

impl Packed {
    /// Makes the bind group, against the layout the pipeline was made with, with a new buffer for
    /// each block of numbers.
    pub fn bind_group(&self, device: &RenderDevice, label: &str, layout: &BindGroupLayout) -> BindGroup {
        let numbers: Vec<(u32, Buffer)> = self
            .numbers
            .iter()
            .map(|(number, bytes)| (*number, numbers_buffer(device, bytes)))
            .collect();

        self.group(device, label, layout, numbers.iter().map(|(number, buffer)| (*number, buffer)))
    }

    /// Makes the bind group with `numbers` as the buffers of its numbers.
    fn group<'a>(
        &self,
        device: &RenderDevice,
        label: &str,
        layout: &BindGroupLayout,
        numbers: impl Iterator<Item = (u32, &'a Buffer)>,
    ) -> BindGroup {
        GROUPS_MADE.fetch_add(1, Ordering::Relaxed);

        let views: Vec<(u32, Vec<&bevy::render::render_resource::WgpuTextureView>)> = self
            .views
            .iter()
            .map(|(number, views)| (*number, views.iter().map(|view| &**view).collect()))
            .collect();

        let samplers: Vec<(u32, Vec<&bevy::render::render_resource::WgpuSampler>)> = self
            .samplers
            .iter()
            .map(|(number, samplers)| (*number, samplers.iter().map(|sampler| &**sampler).collect()))
            .collect();

        let mut entries = Vec::new();

        for (number, buffer) in numbers {
            entries.push(BindGroupEntry {
                binding: number,
                resource: buffer.as_entire_binding(),
            });
        }

        for (number, buffer) in &self.buffers {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: buffer.as_entire_binding(),
            });
        }

        for (number, list) in &views {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: if list.len() == 1 && !self.is_array(*number) {
                    BindingResource::TextureView(list[0])
                } else {
                    BindingResource::TextureViewArray(list)
                },
            });
        }

        for (number, tlas) in &self.structures {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: BindingResource::AccelerationStructure(tlas),
            });
        }

        for (number, list) in &samplers {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: if list.len() == 1 && !self.is_array(*number) {
                    BindingResource::Sampler(list[0])
                } else {
                    BindingResource::SamplerArray(list)
                },
            });
        }

        device.create_bind_group(label, layout, &entries)
    }

    fn is_array(&self, number: u32) -> bool {
        self.arrays.contains(&number)
    }

    /// What the group binds beside its numbers, or `None` where it binds a ray scene, which wgpu
    /// gives no identity to compare, so its group is made again each frame.
    fn marks(&self) -> Option<Vec<Mark>> {
        if !self.structures.is_empty() {
            return None;
        }

        let level = |view: &bevy::render::render_resource::TextureView| {
            self.levels.iter().find(|(id, ..)| *id == view.id()).map(|(_, texture, mip)| (*texture, *mip))
        };

        let buffers = self.buffers.iter().map(|(number, buffer)| Mark::Buffer(*number, buffer.id()));
        let views = self.views.iter().flat_map(|(number, views)| {
            views.iter().map(move |view| match level(view) {
                Some((texture, mip)) => Mark::Level(*number, texture, mip),
                None => Mark::View(*number, view.id()),
            })
        });
        let samplers = self
            .samplers
            .iter()
            .flat_map(|(number, samplers)| samplers.iter().map(|sampler| Mark::Sampler(*number, sampler.id())));

        Some(buffers.chain(views).chain(samplers).collect())
    }
}

/// One dispatch's or draw's group, kept with the buffers of its numbers and what they last held.
struct Kept {
    group: BindGroup,
    layout: BindGroupLayoutId,
    marks: Option<Vec<Mark>>,
    numbers: Vec<(u32, Buffer, Vec<u8>)>,
}

/// The groups of a list of dispatches or draws, kept from one frame to the next by each one's place
/// in its list.
///
/// A place is one dispatch or draw of the frame, so its buffers are written for that one alone, and
/// a write lands before anything the frame runs, after the last frame's work that read them.
#[derive(Default)]
pub struct KeptGroups(Vec<Option<Kept>>);

impl KeptGroups {
    /// What was kept for a list of `length`, taken from `last` where there was one, the places past
    /// the list's end let go of.
    pub fn taken(last: Option<&mut KeptGroups>, length: usize) -> KeptGroups {
        let mut kept = last.map(std::mem::take).unwrap_or_default();
        kept.0.truncate(length);
        kept
    }

    /// The group for the one at `place`, the one kept there where its blocks of numbers are the
    /// same sizes and it binds the same layout and the same things, with numbers that moved written
    /// into their buffers, and a group made again over the same buffers where something else it
    /// binds is another.
    pub fn group(
        &mut self,
        place: usize,
        packed: &Packed,
        (device, queue): (&RenderDevice, &RenderQueue),
        label: &str,
        layout: &BindGroupLayout,
    ) -> BindGroup {
        if self.0.len() <= place {
            self.0.resize_with(place + 1, || None);
        }

        let marks = packed.marks();

        let same_blocks = |kept: &Kept| {
            kept.numbers.len() == packed.numbers.len()
                && kept
                    .numbers
                    .iter()
                    .zip(&packed.numbers)
                    .all(|((at, _, held), (number, bytes))| at == number && held.len() == bytes.len())
        };

        let kept = match self.0[place].take() {
            Some(mut kept) if same_blocks(&kept) => {
                for ((_, buffer, held), (_, bytes)) in kept.numbers.iter_mut().zip(&packed.numbers) {
                    if held != bytes {
                        queue.write_buffer(buffer, 0, bytes);
                        held.clone_from(bytes);
                    }
                }

                if kept.layout != layout.id() || marks.is_none() || kept.marks != marks {
                    let numbers = kept.numbers.iter().map(|(number, buffer, _)| (*number, buffer));
                    kept.group = packed.group(device, label, layout, numbers);
                    (kept.layout, kept.marks) = (layout.id(), marks);
                }

                kept
            }
            _ => {
                let numbers: Vec<(u32, Buffer, Vec<u8>)> = packed
                    .numbers
                    .iter()
                    .map(|(number, bytes)| (*number, numbers_buffer(device, bytes), bytes.clone()))
                    .collect();

                Kept {
                    group: packed.group(device, label, layout, numbers.iter().map(|(number, buffer, _)| (*number, buffer))),
                    layout: layout.id(),
                    marks,
                    numbers,
                }
            }
        };

        let group = kept.group.clone();
        self.0[place] = Some(kept);
        group
    }
}

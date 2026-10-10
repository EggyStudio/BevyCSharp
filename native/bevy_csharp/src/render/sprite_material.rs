//! A sprite drawn with a 2D material a Slang program draws, as Bevy's `SpriteMaterial` draws one
//! with a material extension.
//!
//! Bevy draws every sprite as a quad mesh with a `SpriteMeshMaterial` made from the sprite. A
//! `SpriteMaterial<M>` on the sprite has `SpriteMaterialPlugin<M>` draw it with that material
//! extended by `M` instead, and the plugin asks `M`'s type for its one layout and its one shader,
//! which a type of the bridge's cannot answer for every program. So the bridge does the plugin's
//! part for [`BcsMaterial2d`] here. The component on the sprite is still Bevy's own, because adding
//! one counts the sprite's materials, Bevy gives the sprite none of its own while the count is
//! above zero, and it gives the sprite's own back when the last one goes.
//!
//! What draws the sprite is a copy of the material holding what the sprite says, prepared in
//! [`super::material2d`] with every other 2D material, the sprite's numbers, image and sampler
//! bound at [`SPRITE_BINDINGS`] of the material's group, where `bcs_sprite.slang` declares them.
//! Sprites that say the same thing share a copy, as Bevy's share a material, which keeps them in
//! one batch.

#![cfg(feature = "render")]

use std::sync::Weak;

use bevy::asset::{AssetEvent, AssetId, Assets, Handle, StrongHandle, uuid_handle};
use bevy::ecs::entity::Entity;
use bevy::ecs::message::MessageReader;
use bevy::ecs::query::{Changed, Or};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Commands, Query, Res, ResMut, SystemParamItem};
use bevy::image::TextureAtlasLayout;
use bevy::material::AlphaMode;
use bevy::platform::collections::HashMap;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::encase::StorageBuffer;
use bevy::render::render_resource::{
    AsBindGroup, AsBindGroupError, AsBindGroupShaderType, BindGroupBuilder, BindGroupLayout,
    BindGroupLayoutEntry, BindingType, BufferBindingType, BufferInitDescriptor, BufferUsages,
    SamplerBindingType, ShaderStages, ShaderType, TextureSampleType, TextureViewDimension,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::texture::GpuImage;
use bevy::shader::Shader;
use bevy::sprite::{Anchor, Sprite};
use bevy::sprite_render::{
    AlphaMode2d, MaterialExtension2d, MeshMaterial2d, SpriteMaterial, SpriteMeshMaterial,
    SpriteMeshMaterialUniform,
};

use super::material2d::{BcsMaterial2d, BcsMeshMaterial2d};
use super::reflect::SPRITE_BINDINGS;
use super::values::Packed;

/// The binding of the sprite's numbers, its image's and its sampler's.
const NUMBERS: u32 = *SPRITE_BINDINGS.start();
const IMAGE: u32 = NUMBERS + 1;
const SAMPLER: u32 = NUMBERS + 2;

/// The vertex shader a sprite's material draws with, Bevy's own for a sprite reading the sprite
/// where the bridge binds it.
pub(super) const VERTEX: Handle<Shader> = uuid_handle!("e2e81486-8ea6-48bc-b647-1b8a958e9f02");

/// Bevy's `sprite_material.wesl` vertex shader, which scales its quad of one unit a side to the
/// sprite's size and moves it by the anchor, with its numbers read as a storage buffer at
/// [`NUMBERS`], since that is how the bridge binds every block of numbers in a material's group
/// (see `numbers_in_storage`). Bevy's own struct, so the layout is the one Bevy writes.
const VERTEX_SOURCE: &str = r#"import bevy_sprite_render::mesh2d::functions as mesh_functions;
import bevy_sprite_render::mesh2d::vertex_input::{Vertex, decompress_vertex};
import bevy_sprite_render::mesh2d::vertex_output::VertexOutput;
import bevy_sprite_render::sprite_mesh::types::SpriteMaterial;

@group(2) @binding(100) var<storage, read> sprite: SpriteMaterial;

@vertex
fn vertex(vertex_in: Vertex) -> VertexOutput {
    let vertex = decompress_vertex(vertex_in, vertex_in.instance_index);
    var out: VertexOutput;
    let world_from_local = mesh_functions::get_world_from_local(vertex.instance_index);
    let position = vertex.position * vec3<f32>(sprite.vertex_scale, 1.0) + vec3<f32>(sprite.vertex_offset, 0.0);
    out.world_position = mesh_functions::mesh2d_position_local_to_world(world_from_local, vec4<f32>(position, 1.0));
    out.position = mesh_functions::mesh2d_position_world_to_clip(out.world_position);
    @if(VERTEX_NORMALS) {
        out.world_normal = mesh_functions::mesh2d_normal_local_to_world(vertex.normal, vertex.instance_index);
    }
    @if(VERTEX_UVS) {
        out.uv = vertex.uv;
    }
    @if(VERTEX_TANGENTS) {
        out.world_tangent = mesh_functions::mesh2d_tangent_local_to_world(world_from_local, vertex.tangent);
    }
    @if(VERTEX_COLORS) {
        out.color = vertex.color;
    }
    out.instance_index = vertex.instance_index;
    return out;
}
"#;

// -- What Bevy's `SpriteMaterial` asks of a material's type

/// What `SpriteMaterial<BcsMaterial2d>` asks of the material's type, which nothing calls.
///
/// Bevy's component takes only a type that could extend a sprite's material, and such a type makes
/// its own bind group. A material of the bridge's is prepared in [`super::material2d`] with the
/// layout its program declares, which no single answer here could give, and
/// `SpriteMaterialPlugin`, the one thing that would ask, is never added. So these say there is
/// nothing to bind.
impl AsBindGroup for BcsMaterial2d {
    type Data = ();
    type Param = ();

    fn label() -> &'static str {
        "bcs_material_2d"
    }

    fn bind_group_data(&self) -> Self::Data {}

    fn build_bind_group(
        &self,
        _layout: &BindGroupLayout,
        _render_device: &RenderDevice,
        _param: &mut SystemParamItem<'_, '_, Self::Param>,
        _force_no_bindless: bool,
        _output: &mut BindGroupBuilder,
    ) -> Result<(), AsBindGroupError> {
        Err(AsBindGroupError::CreateBindGroupDirectly)
    }

    fn bind_group_layout_entries(
        _render_device: &RenderDevice,
        _force_no_bindless: bool,
    ) -> Vec<BindGroupLayoutEntry> {
        Vec::new()
    }
}

impl MaterialExtension2d for BcsMaterial2d {}

// -- The copies sprites are drawn with

/// The copies of each material sprites are drawn with, each beside the sprite part it was made
/// for, kept while some sprite draws with it.
///
/// The part as the sprite says it, before the material's alpha mode is put in, so a material whose
/// alpha mode changes finds its copies again.
#[derive(Resource, Default)]
pub(super) struct SpriteCopies(HashMap<AssetId<BcsMaterial2d>, Vec<(SpriteMeshMaterial, Weak<StrongHandle>)>>);

/// What a sprite says, as Bevy makes a sprite's own material from it.
fn sprite_part(
    sprite: &Sprite,
    anchor: &Anchor,
    layouts: &Assets<TextureAtlasLayout>,
) -> SpriteMeshMaterial {
    let mut part = SpriteMeshMaterial::from_sprite(sprite.clone());
    part.anchor = anchor.0;

    if let Some(atlas) = &sprite.texture_atlas
        && let Some(layout) = layouts.get(atlas.layout.id())
    {
        part.texture_atlas_layout = Some(layout.clone());
        part.texture_atlas_index = atlas.index;
    }

    part
}

/// A copy of `material` that draws a sprite saying `part`.
///
/// Blending as the material's alpha mode says where it was given one, the sprite's mask cutoff
/// included, so `bcs_sprite`'s functions apply what the pipeline draws by.
fn copy_for(material: &BcsMaterial2d, part: &SpriteMeshMaterial) -> BcsMaterial2d {
    let mut part = part.clone();

    if material.material.alpha_given {
        part.alpha_mode = match material.material.alpha {
            AlphaMode::Opaque => AlphaMode2d::Opaque,
            AlphaMode::Mask(cutoff) => AlphaMode2d::Mask(cutoff),
            _ => AlphaMode2d::Blend,
        };
    }

    BcsMaterial2d {
        material: material.material.clone(),
        sprite: Some(Box::new(part)),
    }
}

/// Draws each sprite given a 2D shader material with a copy of it holding the sprite, again
/// whenever the sprite or the material changes, in place of the material Bevy gives the sprite.
///
/// Before Bevy's asset events, so a sprite changed in this frame is drawn as it now is in this
/// frame. A material's values changed in this frame reach its copies in the next, as Bevy's
/// `SpriteMaterial` has them, since a change is known by its event.
pub(super) fn give_sprites_their_materials(
    sprites: Query<
        (Entity, &Sprite, &Anchor, &SpriteMaterial<BcsMaterial2d>),
        Or<(
            Changed<SpriteMaterial<BcsMaterial2d>>,
            Changed<Sprite>,
            Changed<Anchor>,
        )>,
    >,
    mut events: MessageReader<AssetEvent<BcsMaterial2d>>,
    layouts: Res<Assets<TextureAtlasLayout>>,
    mut materials: ResMut<Assets<BcsMaterial2d>>,
    mut copies: ResMut<SpriteCopies>,
    mut commands: Commands,
) {
    copies.0.retain(|_, list| {
        list.retain(|(_, copy)| copy.strong_count() > 0);
        !list.is_empty()
    });

    // A material whose values or alpha mode changed, into each of its copies. A copy's own change
    // is no key here, so this does not go round again.
    for event in events.read() {
        let AssetEvent::Modified { id } = event else {
            continue;
        };
        let (Some(list), Some(material)) = (copies.0.get(id), materials.get(*id).cloned()) else {
            continue;
        };

        let live: Vec<_> = list
            .iter()
            .filter_map(|(part, copy)| Some((part.clone(), Handle::Strong(copy.upgrade()?))))
            .collect();

        for (part, handle) in live {
            if let Some(mut slot) = materials.get_mut(&handle) {
                *slot = copy_for(&material, &part);
            }
        }
    }

    for (entity, sprite, anchor, sprite_material) in &sprites {
        let Some(material) = materials.get(&sprite_material.0).cloned() else {
            continue;
        };

        let part = sprite_part(sprite, anchor, &layouts);
        let list = copies.0.entry(sprite_material.0.id()).or_default();

        let found = list
            .iter()
            .find(|(made_for, _)| *made_for == part)
            .and_then(|(_, copy)| copy.upgrade());

        let handle = match found {
            Some(strong) => Handle::Strong(strong),
            None => {
                let handle = materials.add(copy_for(&material, &part));
                if let Handle::Strong(strong) = &handle {
                    list.push((part, std::sync::Arc::downgrade(strong)));
                }
                handle
            }
        };

        commands
            .entity(entity)
            .remove::<MeshMaterial2d<SpriteMeshMaterial>>()
            .insert(BcsMeshMaterial2d(handle));
    }
}

// -- Binding the sprite

/// The layout entries of what a sprite says, added to a sprite's copy's own.
pub(super) fn entries() -> [BindGroupLayoutEntry; 3] {
    [
        BindGroupLayoutEntry {
            binding: NUMBERS,
            visibility: ShaderStages::VERTEX_FRAGMENT,
            ty: BindingType::Buffer {
                ty: BufferBindingType::Storage { read_only: true },
                has_dynamic_offset: false,
                min_binding_size: Some(SpriteMeshMaterialUniform::min_size()),
            },
            count: None,
        },
        BindGroupLayoutEntry {
            binding: IMAGE,
            visibility: ShaderStages::VERTEX_FRAGMENT,
            ty: BindingType::Texture {
                sample_type: TextureSampleType::Float { filterable: true },
                view_dimension: TextureViewDimension::D2,
                multisampled: false,
            },
            count: None,
        },
        BindGroupLayoutEntry {
            binding: SAMPLER,
            visibility: ShaderStages::VERTEX_FRAGMENT,
            ty: BindingType::Sampler(SamplerBindingType::Filtering),
            count: None,
        },
    ]
}

/// Binds what `sprite` says beside a copy's values, or answers false where its image has not
/// reached the GPU.
///
/// The numbers are the ones Bevy works out for the sprite's own material, written as a storage
/// buffer is laid out, which for this struct is the layout of the uniform Bevy writes.
pub(super) fn bind(
    packed: &mut Packed,
    sprite: &SpriteMeshMaterial,
    device: &RenderDevice,
    images: &RenderAssets<GpuImage>,
) -> bool {
    let Some(image) = images.get(sprite.image.id()) else {
        return false;
    };

    let numbers: SpriteMeshMaterialUniform = sprite.as_bind_group_shader_type(images);
    let mut bytes = StorageBuffer::new(Vec::new());
    if bytes.write(&numbers).is_err() {
        return false;
    }

    let buffer = device.create_buffer_with_data(&BufferInitDescriptor {
        label: Some("bcs_sprite"),
        contents: &bytes.into_inner(),
        usage: BufferUsages::STORAGE,
    });

    packed.buffers.push((NUMBERS, buffer));
    packed.views.push((IMAGE, vec![image.texture_view.clone()]));
    packed.samplers.push((SAMPLER, vec![image.sampler.clone()]));
    true
}

/// Adds the sprite's vertex shader and the record of copies. The system itself is ordered with the
/// rest of the 2D materials' in [`super::material2d::install`].
pub(super) fn install(app: &mut bevy::app::App) {
    app.init_resource::<SpriteCopies>();

    if let Some(mut shaders) = app.world_mut().get_resource_mut::<Assets<Shader>>() {
        // A UUID handle, whose insertion cannot fail.
        let _ = shaders.insert(
            VERTEX.id(),
            Shader::from_wesl(VERTEX_SOURCE, "bcs/sprite_vertex.wesl"),
        );
    }
}

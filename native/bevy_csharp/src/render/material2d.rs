//! The material a Slang program draws a 2D mesh with, laid out the way the program declares.
//!
//! The 2D counterpart of [`super::material`]. Bevy's `Material2d` trait asks a material's type for
//! its bind group layout, and its 2D renderer below that trait draws from a `MaterialProperties`
//! per material, as the 3D one does. So this is a material type that implements that layer
//! directly, every material bringing the layout its program's 2D stages declare, and the systems
//! below do what `Material2dPlugin` does for a type: an allocator entry, extraction of which entity
//! is drawn with which material, and telling the specializer which entities changed.
//!
//! The asset holds a [`BcsMaterial`], so values are set on a 2D material by name exactly as on a
//! 3D one, and is a type of its own because Bevy prepares every asset of a type for the renderer
//! the type belongs to, which would prepare each 3D material for 2D as well.

#![cfg(feature = "render")]

use std::any::TypeId;
use std::sync::Arc;

use bevy::asset::{AsAssetId, Asset, AssetId, Handle};
use bevy::camera::visibility::ViewVisibility;
use bevy::core_pipeline::core_2d::{AlphaMask2d, Opaque2d, Transparent2d};
use bevy::ecs::change_detection::DetectChangesMut;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::lifecycle::RemovedComponents;
use bevy::ecs::query::{Changed, Or, With};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::lifetimeless::{SRes, SResMut};
use bevy::ecs::system::{Query, Res, ResMut, SystemParamItem};
use bevy::material::key::{ErasedMaterialKey, ErasedMeshPipelineKey};
use bevy::material::labels::{DrawFunctionLabel as _, ShaderLabel as _};
use bevy::material::{AlphaMode, MaterialProperties, OpaqueRendererMethod, RenderPhaseType};
use bevy::mesh::Mesh2d;
use bevy::platform::collections::hash_map::Entry;
use bevy::reflect::TypePath;
use bevy::render::camera::DirtySpecializations;
use bevy::render::erased_render_asset::{ErasedRenderAsset, PrepareAssetError};
use bevy::render::material_bind_groups::{
    MaterialBindGroupAllocator, MaterialBindGroupAllocators, RenderMaterialBindings,
};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_phase::{DrawFunctions, SetItemPipeline};
use bevy::render::render_resource::{
    BindGroupLayoutDescriptor, BindingResources, PipelineCache, PreparedBindGroup, ShaderStages,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::storage::GpuShaderBuffer;
use bevy::render::sync_world::MainEntity;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::Extract;
use bevy::sprite_render::{
    alpha_mode_pipeline_key_2d, base_specialize, DrawMesh2d, Material2dFragmentShader,
    Material2dVertexShader, Mesh2dPipelineKey, Pass2dAlphaMaskDrawFunction,
    Pass2dOpaqueDrawFunction, Pass2dTransparentDrawFunction, PreparedMaterial2d,
    RenderMaterial2dInstances, SetMaterial2dBindGroup, SetMesh2dBindGroup, SetMesh2dViewBindGroup,
};

use super::material::{BcsMaterial, BcsMaterialKey, say_once};
use super::programs::{self, Role};
use super::values::{PackContext, PackError, Stand, pack};

/// A material drawn on a 2D mesh by a program the game wrote.
#[derive(Asset, TypePath, Clone, Debug)]
pub struct BcsMaterial2d(pub BcsMaterial);

/// Draws an entity's 2D mesh with a [`BcsMaterial2d`].
#[derive(Component, Clone, Debug, PartialEq, Eq)]
pub struct BcsMeshMaterial2d(pub Handle<BcsMaterial2d>);

impl AsAssetId for BcsMeshMaterial2d {
    type Asset = BcsMaterial2d;

    fn as_asset_id(&self) -> AssetId<Self::Asset> {
        self.0.id()
    }
}

/// Bevy's own draw function for 2D materials, which Bevy registers under a name of its crate's.
/// A type alias names the tuple it stands for, so this is the same type and finds the same
/// function.
type DrawMaterial2d = (
    SetItemPipeline,
    SetMesh2dViewBindGroup<0>,
    SetMesh2dBindGroup<1>,
    SetMaterial2dBindGroup<2>,
    DrawMesh2d,
);

impl ErasedRenderAsset for BcsMeshMaterial2d {
    type SourceAsset = BcsMaterial2d;
    type ErasedAsset = PreparedMaterial2d;

    type Param = (
        SRes<RenderDevice>,
        SRes<PipelineCache>,
        SResMut<MaterialBindGroupAllocators>,
        SResMut<RenderMaterialBindings>,
        (SRes<DrawFunctions<Opaque2d>>, SRes<DrawFunctions<AlphaMask2d>>, SRes<DrawFunctions<Transparent2d>>),
        SRes<RenderAssets<GpuImage>>,
        SRes<RenderAssets<GpuShaderBuffer>>,
        SRes<FallbackImage>,
        SRes<Stand>,
    );

    fn prepare_asset(
        source: Self::SourceAsset,
        material_id: AssetId<Self::SourceAsset>,
        (
            render_device,
            pipeline_cache,
            bind_group_allocators,
            render_material_bindings,
            (opaque, alpha_mask, transparent),
            images,
            buffers,
            fallback,
            stand,
        ): &mut SystemParamItem<Self::Param>,
    ) -> Result<Self::ErasedAsset, PrepareAssetError<Self::SourceAsset>> {
        let material = &source.0;

        let Some(program) = programs::lookup(material.program) else {
            say_once(format!(
                "A 2D material names shader program {}, which does not exist.",
                material.program
            ));
            return Err(PrepareAssetError::RetryNextUpdate(source));
        };

        // Not compiled yet, which the program's 2D stages are once each has a result.
        let Some(layout) = program.material2d.clone() else {
            return Err(PrepareAssetError::RetryNextUpdate(source));
        };

        if program.stages[Role::Fragment2d as usize].is_none() {
            say_once(format!(
                "Shader program {} has no 2D fragment shader, so it cannot draw a 2D material.",
                material.program
            ));
            return Err(PrepareAssetError::RetryNextUpdate(source));
        }

        let descriptor = BindGroupLayoutDescriptor::new(
            "bcs_material_2d",
            &layout.entries(ShaderStages::VERTEX_FRAGMENT),
        );

        let context = PackContext {
            device: render_device,
            images,
            buffers,
            fallback,
            stand,
            view: None,
        };

        let packed = match pack(&layout, &material.values, &context) {
            Ok(packed) => packed,
            Err(PackError::NotReady) => return Err(PrepareAssetError::RetryNextUpdate(source)),
            Err(PackError::Missing(message)) => {
                say_once(format!("A 2D material of shader program {}: {message}", material.program));
                return Err(PrepareAssetError::RetryNextUpdate(source));
            }
        };

        for problem in &packed.problems {
            say_once(format!("A 2D material of shader program {}: {problem}", material.program));
        }

        let bind_group = packed.bind_group(
            render_device,
            "bcs_material_2d",
            &pipeline_cache.get_bind_group_layout(&descriptor),
        );

        let prepared = PreparedBindGroup {
            bindings: BindingResources(Vec::new()),
            bind_group,
        };

        let Some(allocator) = bind_group_allocators.get_mut(&TypeId::of::<BcsMaterial2d>()) else {
            return Err(PrepareAssetError::RetryNextUpdate(source));
        };

        let binding = match render_material_bindings.entry(material_id.into()) {
            Entry::Occupied(mut occupied) => {
                allocator.free(*occupied.get());
                let binding = allocator.allocate_prepared(prepared, descriptor.clone());
                *occupied.get_mut() = binding;
                binding
            }
            Entry::Vacant(vacant) => {
                *vacant.insert(allocator.allocate_prepared(prepared, descriptor.clone()))
            }
        };

        // 2D draws opaque, masked or blended, which is all a 2D material is made with.
        let render_phase_type = match material.alpha {
            AlphaMode::Opaque => RenderPhaseType::Opaque,
            AlphaMode::Mask(_) => RenderPhaseType::AlphaMask,
            _ => RenderPhaseType::Transparent,
        };

        let mut mesh_key = Mesh2dPipelineKey::empty();
        mesh_key.insert(alpha_mode_pipeline_key_2d(material.alpha));

        let mut properties = MaterialProperties {
            alpha_mode: material.alpha,
            depth_bias: material.depth_bias,
            reads_view_transmission_texture: false,
            render_phase_type,
            render_method: OpaqueRendererMethod::Forward,
            mesh_pipeline_key_bits: ErasedMeshPipelineKey::new(mesh_key),
            material_layout: Some(descriptor),
            draw_functions: Default::default(),
            shaders: Default::default(),
            bindless: false,
            base_specialize: Some(base_specialize),
            prepass_specialize: None,
            user_specialize: None,
            material_key: ErasedMaterialKey::new(BcsMaterialKey {
                program: material.program,
                generation: program.generation,
                cull: 2,
            }),
            shadows_enabled: false,
            prepass_enabled: false,
            oit_enabled: false,
        };

        properties.draw_functions.extend([
            (Pass2dOpaqueDrawFunction.intern(), opaque.read().id::<DrawMaterial2d>()),
            (Pass2dAlphaMaskDrawFunction.intern(), alpha_mask.read().id::<DrawMaterial2d>()),
            (Pass2dTransparentDrawFunction.intern(), transparent.read().id::<DrawMaterial2d>()),
        ]);

        // Without a 2D vertex shader of the program's own, Bevy's own draws the mesh.
        for (role, label) in [
            (Role::Vertex2d, Material2dVertexShader.intern()),
            (Role::Fragment2d, Material2dFragmentShader.intern()),
        ] {
            if let Some(stage) = &program.stages[role as usize] {
                properties.shaders.push((label, stage.shader.clone()));
            }
        }

        Ok(PreparedMaterial2d {
            binding,
            properties: Arc::new(properties),
        })
    }

    fn unload_asset(
        source_asset: AssetId<Self::SourceAsset>,
        (_, _, bind_group_allocators, render_material_bindings, ..): &mut SystemParamItem<Self::Param>,
    ) {
        let Some(binding) = render_material_bindings.remove(&source_asset.untyped()) else {
            return;
        };

        if let Some(allocator) = bind_group_allocators.get_mut(&TypeId::of::<BcsMaterial2d>()) {
            allocator.free(binding);
        }
    }
}

// -- What Material2dPlugin would do for a material type

/// Entities whose 2D mesh or material changed, which the specializer has to look at again.
#[derive(Resource, Default)]
struct EntitiesNeedingSpecialization2d {
    changed: Vec<Entity>,
    removed: Vec<Entity>,
}

/// Collects the entities whose 2D mesh or material changed this frame.
fn check_entities_needing_specialization(
    changed: Query<
        Entity,
        (
            Or<(
                Changed<Mesh2d>,
                bevy::asset::prelude::AssetChanged<Mesh2d>,
                Changed<BcsMeshMaterial2d>,
                bevy::asset::prelude::AssetChanged<BcsMeshMaterial2d>,
            )>,
            With<BcsMeshMaterial2d>,
        ),
    >,
    mut needing: ResMut<EntitiesNeedingSpecialization2d>,
    mut removed_meshes: RemovedComponents<Mesh2d>,
    mut removed_materials: RemovedComponents<BcsMeshMaterial2d>,
) {
    needing.changed.clear();
    needing.removed.clear();
    needing.changed.extend(changed.iter());
    needing
        .removed
        .extend(removed_meshes.read().chain(removed_materials.read()));
}

/// Marks an entity's 2D mesh changed when its material did, because that has Bevy extract the mesh
/// again with the new material's bind group slot.
fn mark_meshes_as_changed_if_their_materials_changed(
    mut changed: Query<
        &mut Mesh2d,
        Or<(
            Changed<BcsMeshMaterial2d>,
            bevy::asset::prelude::AssetChanged<BcsMeshMaterial2d>,
        )>,
    >,
) {
    for mut mesh in &mut changed {
        mesh.set_changed();
    }
}

/// Records which material each visible entity is drawn with, where Bevy's 2D renderer looks it up.
fn extract_materials(
    mut instances: ResMut<RenderMaterial2dInstances>,
    changed: Extract<
        Query<
            (Entity, &ViewVisibility, &BcsMeshMaterial2d),
            Or<(Changed<ViewVisibility>, Changed<BcsMeshMaterial2d>)>,
        >,
    >,
    mut removed: Extract<RemovedComponents<BcsMeshMaterial2d>>,
) {
    for (entity, visibility, material) in &changed {
        if visibility.get() {
            instances.insert(entity.into(), material.0.id().untyped());
        } else {
            instances.remove(&MainEntity::from(entity));
        }
    }

    // Only where it was not added again in the same frame, which a removal followed by an
    // insertion would be.
    for entity in removed.read() {
        if !changed.contains(entity) {
            instances.remove(&MainEntity::from(entity));
        }
    }
}

fn extract_entities_needing_specialization(
    needing: Extract<Res<EntitiesNeedingSpecialization2d>>,
    mut dirty: ResMut<DirtySpecializations>,
) {
    for entity in &needing.changed {
        dirty.changed_renderables.insert(MainEntity::from(*entity));
    }
}

fn extract_entities_needing_specializations_removed(
    needing: Extract<Res<EntitiesNeedingSpecialization2d>>,
    mut dirty: ResMut<DirtySpecializations>,
) {
    for entity in &needing.removed {
        dirty.removed_renderables.insert(MainEntity::from(*entity));
    }
}

fn add_bind_group_allocator(
    render_device: Res<RenderDevice>,
    mut allocators: ResMut<MaterialBindGroupAllocators>,
) {
    // Non-bindless, so each bind group is made against the layout it is given, as for 3D.
    allocators.insert(
        TypeId::of::<BcsMaterial2d>(),
        MaterialBindGroupAllocator::new(
            &render_device,
            "bcs_material_2d",
            None,
            BindGroupLayoutDescriptor::new("bcs_material_2d_unused", &[]),
            None,
        ),
    );
}

/// Adds what draws 2D meshes with shader materials. Called from [`super::material::install`].
pub(super) fn install(app: &mut bevy::app::App) {
    use bevy::app::PostUpdate;
    use bevy::asset::AssetApp;
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::render::camera::DirtySpecializationSystems;
    use bevy::render::erased_render_asset::ErasedRenderAssetPlugin;
    use bevy::render::{ExtractSchedule, RenderApp, RenderStartup};

    app.init_asset::<BcsMaterial2d>()
        .init_resource::<EntitiesNeedingSpecialization2d>()
        .add_plugins(ErasedRenderAssetPlugin::<BcsMeshMaterial2d>::default())
        .add_systems(
            PostUpdate,
            (
                mark_meshes_as_changed_if_their_materials_changed,
                check_entities_needing_specialization.after(bevy::asset::AssetEventSystems),
            )
                .chain(),
        );

    if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
        render_app
            .add_systems(RenderStartup, add_bind_group_allocator)
            .add_systems(
                ExtractSchedule,
                (
                    extract_materials.before(bevy::sprite_render::extract_2d_meshes),
                    extract_entities_needing_specialization
                        .in_set(DirtySpecializationSystems::CheckForChanges),
                    extract_entities_needing_specializations_removed
                        .in_set(DirtySpecializationSystems::CheckForRemovals),
                ),
            );
    }
}

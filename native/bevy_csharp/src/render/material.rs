//! The material a Slang program draws with, laid out the way the program declares.
//!
//! Bevy's `Material` trait asks a material's *type* for its bind group layout, which would make
//! every material of one type take the same numbers and textures. Bevy's renderer below that trait
//! does not need that, because what it draws from is a `MaterialProperties` per material, which
//! carries the layout of group three and the shaders, and its bind group allocator takes a layout
//! with every bind group. So this is a material type that skips the trait and implements the layer under it
//! directly, the way Bevy's own `MeshMaterial3d<M>` does, and every material brings the layout
//! its program's reflection describes (see [`super::reflect`]).
//!
//! What that takes, besides preparing the material, is what `MaterialPlugin<M>` does for a type:
//! an allocator entry, extraction of which entity is drawn with which material, and telling the
//! specializer which entities changed. All of it is below, over Bevy's own public resources, and
//! Bevy's queue, specialize and draw systems then draw these materials like any other.
//!
//! **The prepass.** Bevy draws depth for shadows, and normals and motion for the effects that read
//! them, in a pass of its own. A program may name a prepass vertex shader, so a material that moves
//! its own geometry casts the shadow of the shape it drew, and a prepass fragment shader, so one
//! that discards pixels casts a shadow with the same holes.

#![cfg(feature = "render")]

use std::any::TypeId;
use std::collections::HashSet;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};

use bevy::asset::{AsAssetId, Asset, AssetApp, AssetId, Assets, Handle};
use bevy::camera::visibility::ViewVisibility;
use bevy::core_pipeline::core_3d::{AlphaMask3d, Opaque3d, Transparent3d};
use bevy::core_pipeline::deferred::{AlphaMask3dDeferred, Opaque3dDeferred};
use bevy::core_pipeline::prepass::{AlphaMask3dPrepass, Opaque3dPrepass};
use bevy::ecs::change_detection::DetectChangesMut;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::lifecycle::RemovedComponents;
use bevy::ecs::query::{Changed, Or, With};
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::lifetimeless::{SRes, SResMut};
use bevy::ecs::system::{Commands, Query, Res, ResMut, SystemParamItem};
use bevy::ecs::world::{Mut, World};
use bevy::material::labels::{DrawFunctionLabel as _, ShaderLabel as _};
use bevy::material::key::{ErasedMaterialKey, ErasedMaterialPipelineKey, ErasedMeshPipelineKey};
use bevy::material::{AlphaMode, MaterialProperties, OpaqueRendererMethod, RenderPhaseType};
use bevy::mesh::{Mesh, Mesh3d, MeshVertexBufferLayoutRef};
use bevy::pbr::{
    DeferredAlphaMaskDrawFunction, DeferredOpaqueDrawFunction, DrawDepthOnlyPrepass,
    DrawMaterial, DrawPrepass, MainPassAlphaMaskDrawFunction, MainPassOpaqueDrawFunction,
    MainPassTransmissiveDrawFunction, MainPassTransparentDrawFunction,
    MaterialBindGroupAllocator, MaterialBindGroupAllocators, MaterialExtractionSystems,
    MaterialFragmentShader, MaterialVertexShader, PrepassAlphaMaskDrawFunction,
    PrepassFragmentShader, PrepassOpaqueDepthOnlyDrawFunction, PrepassOpaqueDrawFunction,
    PrepassPipeline, PrepassPipelineSpecializer, PrepassVertexShader, PreparedMaterial,
    RenderMaterialBindings, RenderMaterialInstance, RenderMaterialInstances, Shadow,
    ShadowsDepthOnlyDrawFunction, ShadowsDrawFunction, Transmissive3d, base_specialize,
    late_sweep_material_instances,
};
use bevy::platform::collections::hash_map::Entry;
use bevy::reflect::TypePath;
use bevy::render::camera::{DirtySpecializationSystems, DirtySpecializations};
use bevy::render::erased_render_asset::{
    ErasedRenderAsset, ErasedRenderAssetPlugin, PrepareAssetError,
};
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_phase::DrawFunctions;
use bevy::render::render_resource::{
    BindGroupLayoutDescriptor, BindingResources, CachedRenderPipelineId, Face, FragmentState,
    PipelineCache, PreparedBindGroup, RenderPipelineDescriptor, ShaderStages,
    SpecializedMeshPipelineError, SpecializedMeshPipelines,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::storage::GpuShaderBuffer;
use bevy::render::sync_world::MainEntity;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::render::{Extract, ExtractSchedule, RenderApp, RenderStartup};
use bevy::shader::ShaderDefVal;

use super::programs::{self, Role};
use super::values::{PackContext, PackError, Stand, Values, pack};

/// A material drawn by a program the game wrote.
#[derive(Asset, TypePath, Clone, Debug)]
pub struct BcsMaterial {
    /// Which program draws it, as a number from [`programs`].
    pub program: u32,
    /// What the game has said about the names the program declares.
    pub values: Values,
    pub alpha: AlphaMode,
    pub cull: Option<Face>,
    pub depth_bias: f32,
}

impl BcsMaterial {
    /// A material for `program` with nothing set.
    pub fn new(program: u32) -> Self {
        Self {
            program,
            values: Values::default(),
            alpha: AlphaMode::Opaque,
            cull: Some(Face::Back),
            depth_bias: 0.0,
        }
    }
}

/// Draws an entity's mesh with a [`BcsMaterial`].
#[derive(Component, Clone, Debug, PartialEq, Eq)]
pub struct BcsMaterial3d(pub Handle<BcsMaterial>);

impl AsAssetId for BcsMaterial3d {
    type Asset = BcsMaterial;

    fn as_asset_id(&self) -> AssetId<Self::Asset> {
        self.0.id()
    }
}

/// What decides a material's pipelines: the program, which version of it, and the faces culled.
///
/// The version, because a program whose shader was edited has a new layout as well as new code,
/// and a pipeline built for the old one must not be reused for the new.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub struct BcsMaterialKey {
    pub program: u32,
    pub generation: u32,
    /// `0` back, `1` front, `2` neither.
    pub cull: u8,
}

type DrawFunctionParams = (
    SRes<DrawFunctions<Opaque3d>>,
    SRes<DrawFunctions<AlphaMask3d>>,
    SRes<DrawFunctions<Transmissive3d>>,
    SRes<DrawFunctions<Transparent3d>>,
    SRes<DrawFunctions<Opaque3dPrepass>>,
    SRes<DrawFunctions<AlphaMask3dPrepass>>,
    SRes<DrawFunctions<Opaque3dDeferred>>,
    SRes<DrawFunctions<AlphaMask3dDeferred>>,
    SRes<DrawFunctions<Shadow>>,
);

/// Messages already logged, so a material that cannot be prepared says why once rather than every
/// frame it is retried.
static SAID: Mutex<Option<HashSet<String>>> = Mutex::new(None);

pub(crate) fn say_once(message: String) {
    let mut said = SAID.lock().unwrap_or_else(|poisoned| poisoned.into_inner());

    if said.get_or_insert_with(HashSet::new).insert(message.clone()) {
        bevy::log::warn!("{message}");
    }
}

impl ErasedRenderAsset for BcsMaterial3d {
    type SourceAsset = BcsMaterial;
    type ErasedAsset = PreparedMaterial;

    type Param = (
        SRes<RenderDevice>,
        SRes<PipelineCache>,
        SResMut<MaterialBindGroupAllocators>,
        SResMut<RenderMaterialBindings>,
        DrawFunctionParams,
        SRes<RenderAssets<GpuImage>>,
        SRes<RenderAssets<GpuShaderBuffer>>,
        SRes<FallbackImage>,
        SRes<Stand>,
    );

    fn prepare_asset(
        material: Self::SourceAsset,
        material_id: AssetId<Self::SourceAsset>,
        (
            render_device,
            pipeline_cache,
            bind_group_allocators,
            render_material_bindings,
            draw_functions,
            images,
            buffers,
            fallback,
            stand,
        ): &mut SystemParamItem<Self::Param>,
    ) -> Result<Self::ErasedAsset, PrepareAssetError<Self::SourceAsset>> {
        let Some(program) = programs::lookup(material.program) else {
            say_once(format!(
                "A material names shader program {}, which does not exist.",
                material.program
            ));
            return Err(PrepareAssetError::RetryNextUpdate(material));
        };

        // Not compiled yet. A material is prepared once its program has a layout, which is the
        // first frame after every stage has a result.
        let Some(layout) = program.material.clone() else {
            return Err(PrepareAssetError::RetryNextUpdate(material));
        };

        if program.stages[Role::Fragment as usize].is_none() {
            say_once(format!(
                "Shader program {} has no fragment shader, so it cannot draw a material.",
                material.program
            ));
            return Err(PrepareAssetError::RetryNextUpdate(material));
        }

        let descriptor = BindGroupLayoutDescriptor::new(
            "bcs_material",
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
            Err(PackError::NotReady) => return Err(PrepareAssetError::RetryNextUpdate(material)),
            Err(PackError::Missing(message)) => {
                say_once(format!(
                    "A material of shader program {}: {message}",
                    material.program
                ));
                return Err(PrepareAssetError::RetryNextUpdate(material));
            }
        };

        for problem in &packed.problems {
            say_once(format!(
                "A material of shader program {}: {problem}",
                material.program
            ));
        }

        let bind_group = packed.bind_group(
            render_device,
            "bcs_material",
            &pipeline_cache.get_bind_group_layout(&descriptor),
        );

        let prepared = PreparedBindGroup {
            bindings: BindingResources(Vec::new()),
            bind_group,
        };

        let Some(allocator) = bind_group_allocators.get_mut(&TypeId::of::<BcsMaterial>()) else {
            return Err(PrepareAssetError::RetryNextUpdate(material));
        };

        let binding = match render_material_bindings.entry(material_id.into()) {
            Entry::Occupied(mut occupied) => {
                allocator.free(*occupied.get());
                let binding = allocator.allocate_prepared(prepared);
                *occupied.get_mut() = binding;
                binding
            }
            Entry::Vacant(vacant) => *vacant.insert(allocator.allocate_prepared(prepared)),
        };

        let (
            opaque,
            alpha_mask,
            transmissive,
            transparent,
            prepass,
            alpha_mask_prepass,
            deferred,
            alpha_mask_deferred,
            shadow,
        ) = draw_functions;

        let draw_functions = [
            (
                MainPassOpaqueDrawFunction.intern(),
                opaque.read().id::<DrawMaterial>(),
            ),
            (
                MainPassAlphaMaskDrawFunction.intern(),
                alpha_mask.read().id::<DrawMaterial>(),
            ),
            (
                MainPassTransmissiveDrawFunction.intern(),
                transmissive.read().id::<DrawMaterial>(),
            ),
            (
                MainPassTransparentDrawFunction.intern(),
                transparent.read().id::<DrawMaterial>(),
            ),
            (
                PrepassOpaqueDrawFunction.intern(),
                prepass.read().id::<DrawPrepass>(),
            ),
            (
                PrepassAlphaMaskDrawFunction.intern(),
                alpha_mask_prepass.read().id::<DrawPrepass>(),
            ),
            (
                PrepassOpaqueDepthOnlyDrawFunction.intern(),
                prepass.read().id::<DrawDepthOnlyPrepass>(),
            ),
            (
                DeferredOpaqueDrawFunction.intern(),
                deferred.read().id::<DrawPrepass>(),
            ),
            (
                DeferredAlphaMaskDrawFunction.intern(),
                alpha_mask_deferred.read().id::<DrawPrepass>(),
            ),
            (ShadowsDrawFunction.intern(), shadow.read().id::<DrawPrepass>()),
            (
                ShadowsDepthOnlyDrawFunction.intern(),
                shadow.read().id::<DrawDepthOnlyPrepass>(),
            ),
        ];

        let render_phase_type = match material.alpha {
            AlphaMode::Blend | AlphaMode::Premultiplied | AlphaMode::Add | AlphaMode::Multiply => {
                RenderPhaseType::Transparent
            }
            AlphaMode::Opaque | AlphaMode::AlphaToCoverage => RenderPhaseType::Opaque,
            AlphaMode::Mask(_) => RenderPhaseType::AlphaMask,
        };

        let mut properties = MaterialProperties {
                alpha_mode: material.alpha,
                depth_bias: material.depth_bias,
                reads_view_transmission_texture: false,
                render_phase_type,
                // Forward, whatever the camera prefers, because a program writes a color rather
                // than the surface description a deferred lighting pass reads.
                render_method: OpaqueRendererMethod::Forward,
                mesh_pipeline_key_bits: ErasedMeshPipelineKey::new(
                    bevy::pbr::MeshPipelineKey::empty(),
                ),
                material_layout: Some(descriptor),
                draw_functions: Default::default(),
                shaders: Default::default(),
                bindless: false,
                base_specialize: Some(base_specialize),
                prepass_specialize: Some(prepass_specialize),
                user_specialize: Some(user_specialize),
                material_key: ErasedMaterialKey::new(BcsMaterialKey {
                    program: material.program,
                    generation: program.generation,
                    cull: match material.cull {
                        Some(Face::Back) => 0,
                        Some(Face::Front) => 1,
                        None => 2,
                    },
                }),
                shadows_enabled: true,
                prepass_enabled: true,
        };

        properties.draw_functions.extend(draw_functions);

        for (role, label) in [
            (Role::Vertex, MaterialVertexShader.intern()),
            (Role::Fragment, MaterialFragmentShader.intern()),
            (Role::PrepassVertex, PrepassVertexShader.intern()),
            (Role::PrepassFragment, PrepassFragmentShader.intern()),
        ] {
            if let Some(stage) = &program.stages[role as usize] {
                properties.shaders.push((label, stage.shader.clone()));
            }
        }

        Ok(PreparedMaterial {
            binding,
            properties: Arc::new(properties),
        })
    }

    fn unload_asset(
        source_asset: AssetId<Self::SourceAsset>,
        (_, _, bind_group_allocators, render_material_bindings, ..): &mut SystemParamItem<
            Self::Param,
        >,
    ) {
        let Some(binding) = render_material_bindings.remove(&source_asset.untyped()) else {
            return;
        };

        if let Some(allocator) = bind_group_allocators.get_mut(&TypeId::of::<BcsMaterial>()) {
            allocator.free(binding);
        }
    }
}

/// Builds a prepass pipeline, the same way Bevy does for its own materials.
fn prepass_specialize(
    world: &mut World,
    key: ErasedMaterialPipelineKey,
    layout: &MeshVertexBufferLayoutRef,
    properties: &Arc<MaterialProperties>,
) -> Result<CachedRenderPipelineId, SpecializedMeshPipelineError> {
    world.resource_scope(
        |world, mut pipelines: Mut<SpecializedMeshPipelines<PrepassPipelineSpecializer>>| {
            let prepass_pipeline = world.resource::<PrepassPipeline>().clone();
            let pipeline_cache = world.resource::<PipelineCache>();

            let specializer = PrepassPipelineSpecializer {
                pipeline: prepass_pipeline,
                properties: properties.clone(),
            };

            pipelines.specialize(pipeline_cache, &specializer, key, layout)
        },
    )
}

/// What every pipeline of a material gets on top of what Bevy builds: the faces culled, and the
/// vertex attributes a prepass vertex shader of a program's own reads.
fn user_specialize(
    _pipeline: &dyn std::any::Any,
    descriptor: &mut RenderPipelineDescriptor,
    layout: &MeshVertexBufferLayoutRef,
    key: ErasedMaterialPipelineKey,
) -> Result<(), SpecializedMeshPipelineError> {
    let material: BcsMaterialKey = key.material_key.to_key();

    descriptor.primitive.cull_mode = match material.cull {
        0 => Some(Face::Back),
        1 => Some(Face::Front),
        _ => None,
    };

    let prepass = descriptor
        .vertex
        .shader_defs
        .iter()
        .any(|def| matches!(def, ShaderDefVal::Bool(name, true) if name == "PREPASS_PIPELINE"));

    if !prepass {
        return Ok(());
    }

    let Some(program) = programs::lookup(material.program) else {
        return Ok(());
    };

    if program.stages[Role::PrepassVertex as usize].is_some() {
        // Every attribute the mesh has, at the locations Bevy's prepass uses. Bevy hands a shadow
        // pass the position alone, which is all its own shader reads, and a shader that moves the
        // mesh along its normal needs the normal there too.
        descriptor.vertex.buffers = vec![layout.0.get_layout(&prepass_attributes(layout))?];
    }

    if let Some(stage) = &program.stages[Role::PrepassFragment as usize]
        && descriptor.fragment.is_none()
    {
        // A depth-only pass has no fragment stage, and one that discards needs one, so it gets one
        // that writes to no target.
        descriptor.fragment = Some(FragmentState {
            shader: stage.shader.clone(),
            shader_defs: descriptor.vertex.shader_defs.clone(),
            entry_point: None,
            targets: Vec::new(),
        });
    }

    Ok(())
}

/// The attributes a prepass vertex shader of a program's own is handed, where the mesh has them.
fn prepass_attributes(
    layout: &MeshVertexBufferLayoutRef,
) -> Vec<bevy::mesh::VertexAttributeDescriptor> {
    let mut attributes = vec![Mesh::ATTRIBUTE_POSITION.at_shader_location(0)];

    for (attribute, location) in [
        (Mesh::ATTRIBUTE_UV_0, 1),
        (Mesh::ATTRIBUTE_UV_1, 2),
        (Mesh::ATTRIBUTE_NORMAL, 3),
        (Mesh::ATTRIBUTE_TANGENT, 4),
        (Mesh::ATTRIBUTE_JOINT_INDEX, 5),
        (Mesh::ATTRIBUTE_JOINT_WEIGHT, 6),
        (Mesh::ATTRIBUTE_COLOR, 7),
    ] {
        if layout.0.contains(attribute.id) {
            attributes.push(attribute.at_shader_location(location));
        }
    }

    attributes
}

// -- What MaterialPlugin would do for a material type

/// Entities whose material or mesh changed, which the specializer has to look at again.
#[derive(Resource, Default)]
struct EntitiesNeedingSpecialization {
    changed: Vec<Entity>,
    removed: Vec<Entity>,
}

/// Collects the entities whose mesh or material changed this frame.
fn check_entities_needing_specialization(
    changed: Query<
        Entity,
        (
            Or<(
                Changed<Mesh3d>,
                bevy::asset::prelude::AssetChanged<Mesh3d>,
                Changed<BcsMaterial3d>,
                bevy::asset::prelude::AssetChanged<BcsMaterial3d>,
            )>,
            With<BcsMaterial3d>,
        ),
    >,
    mut needing: ResMut<EntitiesNeedingSpecialization>,
    mut removed_meshes: RemovedComponents<Mesh3d>,
    mut removed_materials: RemovedComponents<BcsMaterial3d>,
) {
    needing.changed.clear();
    needing.removed.clear();
    needing.changed.extend(changed.iter());
    needing
        .removed
        .extend(removed_meshes.read().chain(removed_materials.read()));
}

/// Marks an entity's mesh changed when its material did, because that is what has Bevy extract the
/// mesh again with the new material's bind group slot.
fn mark_meshes_as_changed_if_their_materials_changed(
    mut changed: Query<
        &mut Mesh3d,
        Or<(
            Changed<BcsMaterial3d>,
            bevy::asset::prelude::AssetChanged<BcsMaterial3d>,
        )>,
    >,
) {
    for mut mesh in &mut changed {
        mesh.set_changed();
    }
}

/// Marks every material of a program whose shaders were replaced as changed, so each builds its
/// bind group again in the new layout, keeping its values by name.
fn refresh_materials_of_changed_programs(
    mut programs: ResMut<programs::ShaderPrograms>,
    mut materials: ResMut<Assets<BcsMaterial>>,
) {
    let changed = programs.take_changed();

    if changed.is_empty() {
        return;
    }

    let stale: Vec<AssetId<BcsMaterial>> = materials
        .iter()
        .filter(|(_, material)| changed.contains(&material.program))
        .map(|(id, _)| id)
        .collect();

    for id in stale {
        // `into_inner` rather than only asking for it mutably, because the guard `get_mut` hands
        // back reports a change only when it is written through, and nothing here writes.
        if let Some(material) = materials.get_mut(id) {
            material.into_inner();
        }
    }
}

fn extract_materials(
    mut instances: ResMut<RenderMaterialInstances>,
    changed: Extract<
        Query<
            (Entity, &ViewVisibility, &BcsMaterial3d),
            Or<(Changed<ViewVisibility>, Changed<BcsMaterial3d>)>,
        >,
    >,
) {
    let tick = instances.current_change_tick;

    for (entity, visibility, material) in &changed {
        if visibility.get() {
            instances.instances.insert(
                entity.into(),
                RenderMaterialInstance {
                    asset_id: material.0.id().untyped(),
                    last_change_tick: tick,
                },
            );
        } else {
            instances.instances.remove(&MainEntity::from(entity));
        }
    }
}

fn early_sweep_materials(
    mut instances: ResMut<RenderMaterialInstances>,
    mut removed: Extract<RemovedComponents<BcsMaterial3d>>,
) {
    let tick = instances.current_change_tick;

    for entity in removed.read() {
        if let Entry::Occupied(occupied) = instances.instances.entry(entity.into())
            && occupied.get().last_change_tick != tick
        {
            occupied.remove();
        }
    }
}

fn extract_entities_needing_specialization(
    needing: Extract<Res<EntitiesNeedingSpecialization>>,
    mut dirty: ResMut<DirtySpecializations>,
) {
    for entity in &needing.changed {
        dirty.changed_renderables.insert(MainEntity::from(*entity));
    }
}

fn extract_entities_needing_specializations_removed(
    needing: Extract<Res<EntitiesNeedingSpecialization>>,
    mut dirty: ResMut<DirtySpecializations>,
) {
    for entity in &needing.removed {
        dirty.removed_renderables.insert(MainEntity::from(*entity));
    }
}

fn add_bind_group_allocator(
    render_device: Res<RenderDevice>,
    mut allocators: ResMut<MaterialBindGroupAllocators>,
    mut commands: Commands,
) {
    // Non-bindless, so each bind group is made against the layout it is given rather than one the
    // type decides. The layout passed here is never used, for that reason.
    allocators.insert(
        TypeId::of::<BcsMaterial>(),
        MaterialBindGroupAllocator::new(
            &render_device,
            "bcs_material",
            None,
            BindGroupLayoutDescriptor::new("bcs_material_unused", &[]),
            None,
        ),
    );

    commands.insert_resource(Stand::new(&render_device));
}

// -- Errors the renderer reports

/// Whether a validation error leaves the app running rather than closing it.
static KEEP_RENDERING: AtomicBool = AtomicBool::new(false);

/// The last error the renderer reported, for whoever asks.
static LAST_ERROR: Mutex<String> = Mutex::new(String::new());

/// Says whether a validation error closes the app, which is Bevy's answer, or is logged and
/// survived.
pub fn keep_rendering_after_errors(keep: bool) {
    KEEP_RENDERING.store(keep, Ordering::Relaxed);
}

/// The last error the renderer reported, or an empty string.
pub fn last_error() -> String {
    LAST_ERROR.lock().map(|text| text.clone()).unwrap_or_default()
}

/// Decides what a render error does to the app.
///
/// A shader that compiles can still disagree with the pipeline it is put in, by reading an input
/// its vertex shader never wrote for instance. That is a validation error, and Bevy's answer to any
/// of those is to close the app, which is right for a shipped game and wrong for one somebody is
/// editing a shader in. Surviving means the frames that use the broken pipeline are not drawn until
/// the shader is fixed and reloads, and every other error still closes the app.
fn on_render_error(
    error: &bevy::render::error_handler::RenderError,
    main_world: &mut World,
    _render_world: &mut World,
) -> bevy::render::error_handler::RenderErrorPolicy {
    use bevy::render::error_handler::{ErrorType, RenderErrorPolicy};

    if let Ok(mut last) = LAST_ERROR.lock() {
        *last = error.description.clone();
    }

    if KEEP_RENDERING.load(Ordering::Relaxed) && matches!(error.ty, ErrorType::Validation) {
        return RenderErrorPolicy::Ignore;
    }

    bevy::log::error!("Quitting the application due to {:?} RenderError", error.ty);
    main_world.write_message(bevy::app::AppExit::error());
    RenderErrorPolicy::StopRendering
}

/// Adds what draws shader materials and shader passes, what runs compute shaders, and what
/// compiles and reloads the programs all three are made of.
///
/// Every app that draws gets it, because a program is made while the app runs and there is no
/// saying beforehand whether one will be.
pub fn install(app: &mut bevy::app::App, root: std::path::PathBuf) {
    use bevy::app::{First, PostUpdate};

    programs::forget_all();

    app.init_asset::<BcsMaterial>()
        .init_resource::<EntitiesNeedingSpecialization>()
        .add_plugins(ErasedRenderAssetPlugin::<BcsMaterial3d>::default())
        .insert_resource(programs::ShaderPrograms::new(root))
        .init_resource::<super::shaders::ShaderInstances>()
        .add_systems(
            First,
            (programs::update, refresh_materials_of_changed_programs).chain(),
        )
        .add_systems(
            PostUpdate,
            (super::shaders::sync_passes, super::shaders::sync_view_dispatches),
        )
        .add_systems(
            PostUpdate,
            (
                mark_meshes_as_changed_if_their_materials_changed,
                check_entities_needing_specialization.after(bevy::asset::AssetEventSystems),
            )
                .after(bevy::mesh::mark_3d_meshes_as_changed_if_their_assets_changed),
        )
        .insert_resource(bevy::render::error_handler::RenderErrorHandler(
            on_render_error,
        ));

    if let Some(render_app) = app.get_sub_app_mut(RenderApp) {
        render_app
            .add_systems(RenderStartup, add_bind_group_allocator)
            .add_systems(
                ExtractSchedule,
                (
                    extract_materials.in_set(MaterialExtractionSystems),
                    early_sweep_materials
                        .after(MaterialExtractionSystems)
                        .before(late_sweep_material_instances),
                    extract_entities_needing_specialization
                        .in_set(DirtySpecializationSystems::CheckForChanges),
                    extract_entities_needing_specializations_removed
                        .in_set(DirtySpecializationSystems::CheckForRemovals),
                ),
            );
    }

    super::views::install(app);
    super::passes::install(app);
    super::compute::install(app);
}

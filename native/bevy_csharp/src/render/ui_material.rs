//! The material a Slang program draws an interface node with, laid out the way the program
//! declares.
//!
//! Bevy draws a node with a material through its `UiMaterialPlugin`, which takes the material's
//! bind group layout and its shaders from the material's type, once, as it makes the pipeline the
//! type draws with. Every program here declares a layout of its own, so this type hands Bevy an
//! empty layout and Bevy's own shaders there, and its `specialize`, which Bevy calls with the
//! material's key as it makes each pipeline, puts the program's layout and shaders in their place.
//! The key carries both, so a pipeline is made with the layout of the very version of the program
//! the material's bind group was made against, and a program edited since makes a new key rather
//! than finding an old pipeline.
//!
//! Everything else is Bevy's, extracting the nodes, their quads with their clipping, the order
//! among everything else an interface draws, and the draw itself, so a node drawn this way takes
//! outlines, shadows and stacking as any other node does.

#![cfg(feature = "render")]

use std::cell::RefCell;

use bevy::asset::{Asset, Handle};
use bevy::ecs::system::SystemParamItem;
use bevy::ecs::system::lifetimeless::SRes;
use bevy::reflect::TypePath;
use bevy::render::material_bind_groups::FallbackBuffer;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    AsBindGroup, AsBindGroupError, BindGroupLayout, BindGroupLayoutDescriptor, BindGroupLayoutEntry,
    BindingResources, PipelineCache, PreparedBindGroup, RenderPipelineDescriptor, ShaderStages,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::storage::GpuShaderBuffer;
use bevy::render::texture::{FallbackImage, GpuImage};
use bevy::shader::Shader;
use bevy::ui_render::ui_material::{MaterialNode, UiMaterial, UiMaterialKey};

use super::material::{BcsMaterial, BcsMaterialKey, say_once};
use super::programs::{self, PipelineProgram, Role};
use super::values::{PackContext, PackError, Stand, pack};

/// A material drawn on an interface node by a program the game wrote.
#[derive(Asset, TypePath, Clone, Debug)]
pub struct BcsUiMaterial {
    /// The program and its values, set by name as a 3D material's are.
    pub material: BcsMaterial,
}

/// What decides an interface material's pipeline, the program, its version and its constants, as
/// for a 3D material, and the layout and shaders of that version, which the pipeline is made with.
#[derive(Clone, PartialEq, Eq, Hash, Debug)]
pub struct BcsUiMaterialKey {
    material: BcsMaterialKey,
    layout: BindGroupLayoutDescriptor,
    vertex: Option<Handle<Shader>>,
    fragment: Option<Handle<Shader>>,
}

thread_local! {
    /// The version of a program the key was last made from on this thread, by the program's
    /// number.
    ///
    /// Bevy asks a material for its key and then for its bind group, one straight after the other,
    /// and keeps the two together, the key choosing the pipeline the bind group is drawn with. A
    /// compile finishing on another thread may replace the program's table entry between the two
    /// calls, and a bind group packed against the new version's layout, drawn with a pipeline made
    /// from the old version's, is refused by the GPU. So the key hands the version it read over
    /// here, and [`version_to_pack`] decides from it what the bind group is packed against.
    static KEYED: RefCell<Option<(u32, PipelineProgram)>> = const { RefCell::new(None) };
}

/// What a material's bind group is packed against.
#[derive(Debug)]
enum Version {
    Pack(PipelineProgram),
    /// The program does not exist.
    Missing,
    /// A compile replaced the version the key was made from, so the bind group waits a frame.
    Stale,
}

/// Which version of its program a material's bind group is packed against, given the version its
/// key was just made from, where one was handed over, and the table's now.
///
/// The key's version while the table still holds it. Where a compile has replaced it since, not the
/// table's either, since Bevy keeps the key it already has beside whatever this packs. The material
/// waits a frame instead, and Bevy asks for its key and its bind group again, both of the new
/// version.
fn version_to_pack(keyed: Option<PipelineProgram>, current: Option<PipelineProgram>) -> Version {
    match (keyed, current) {
        (Some(keyed), Some(current)) if keyed.generation == current.generation => Version::Pack(keyed),
        (Some(_), _) => Version::Stale,
        (None, Some(current)) => Version::Pack(current),
        (None, None) => Version::Missing,
    }
}

/// The layout of a program's interface stages.
fn descriptor(program: &PipelineProgram) -> BindGroupLayoutDescriptor {
    let entries = program
        .material_ui
        .as_ref()
        .map(|layout| layout.entries(ShaderStages::VERTEX_FRAGMENT))
        .unwrap_or_default();

    BindGroupLayoutDescriptor::new("bcs_material_ui", &entries)
}

impl AsBindGroup for BcsUiMaterial {
    type Data = BcsUiMaterialKey;
    type Param = (SRes<RenderAssets<GpuImage>>, SRes<FallbackImage>, SRes<Stand>);

    fn label() -> &'static str {
        "bcs_material_ui"
    }

    fn bind_group_data(&self) -> Self::Data {
        let found = programs::lookup(self.material.program);

        // Only a program that exists is handed over, so one that does not is reported as missing
        // rather than as replaced.
        KEYED.with_borrow_mut(|keyed| {
            *keyed = found.clone().map(|program| (self.material.program, program));
        });

        let program = found.unwrap_or_default();
        let stage = |role: Role| program.stages[role as usize].as_ref().map(|stage| stage.shader.clone());

        let key = BcsUiMaterialKey {
            material: BcsMaterialKey {
                program: self.material.program,
                generation: program.generation,
                cull: 2,
                sprite: false,
                constants: program
                    .material_ui
                    .as_ref()
                    .map(|layout| super::constants::of(layout, &self.material.values))
                    .unwrap_or_else(|| Vec::new().into()),
            },
            layout: descriptor(&program),
            vertex: stage(Role::VertexUi),
            fragment: stage(Role::FragmentUi),
        };

        key
    }

    /// Packs the material's values into a bind group of its program's layout, rather than the empty
    /// one Bevy hands it, which is the layout [`UiMaterial::specialize`] makes its pipeline with.
    fn as_bind_group(
        &self,
        _layout: &BindGroupLayoutDescriptor,
        render_device: &RenderDevice,
        pipeline_cache: &PipelineCache,
        _fallback_buffer: &FallbackBuffer,
        buffers: &RenderAssets<GpuShaderBuffer>,
        (images, fallback, stand): &mut SystemParamItem<'_, '_, Self::Param>,
    ) -> Result<PreparedBindGroup, AsBindGroupError> {
        let material = &self.material;

        let keyed = KEYED
            .with_borrow_mut(Option::take)
            .filter(|(program, _)| *program == material.program)
            .map(|(_, program)| program);

        let program = match version_to_pack(keyed, programs::lookup(material.program)) {
            Version::Pack(program) => program,
            Version::Stale => return Err(AsBindGroupError::RetryNextUpdate),
            Version::Missing => {
                say_once(format!(
                    "An interface material names shader program {}, which does not exist.",
                    material.program
                ));
                return Err(AsBindGroupError::RetryNextUpdate);
            }
        };

        // Not compiled yet, which the program's interface stages are once each has a result.
        let Some(layout) = program.material_ui.clone() else {
            return Err(AsBindGroupError::RetryNextUpdate);
        };

        if program.stages[Role::FragmentUi as usize].is_none() {
            say_once(format!(
                "Shader program {} has no interface fragment shader, so it cannot draw an interface \
                 material.",
                material.program
            ));
            return Err(AsBindGroupError::RetryNextUpdate);
        }

        let context = PackContext {
            device: render_device,
            images,
            buffers,
            fallback,
            stand,
            view: None,
            constants: true,
        };

        let packed = match pack(&layout, &material.values, &context) {
            Ok(packed) => packed,
            Err(PackError::NotReady) => return Err(AsBindGroupError::RetryNextUpdate),
            Err(PackError::Missing(message)) => {
                say_once(format!("An interface material of shader program {}: {message}", material.program));
                return Err(AsBindGroupError::RetryNextUpdate);
            }
        };

        for problem in &packed.problems {
            say_once(format!("An interface material of shader program {}: {problem}", material.program));
        }

        let bind_group = packed.bind_group(
            render_device,
            "bcs_material_ui",
            &pipeline_cache.get_bind_group_layout(&descriptor(&program)),
        );

        Ok(PreparedBindGroup {
            bindings: BindingResources(Vec::new()),
            bind_group,
        })
    }

    /// Never reached, since [`Self::as_bind_group`] makes the whole bind group itself.
    fn build_bind_group(
        &self,
        _layout: &BindGroupLayout,
        _render_device: &RenderDevice,
        _param: &mut SystemParamItem<'_, '_, Self::Param>,
        _force_no_bindless: bool,
        _output: &mut bevy::render::render_resource::BindGroupBuilder,
    ) -> Result<(), AsBindGroupError> {
        Err(AsBindGroupError::CreateBindGroupDirectly)
    }

    /// Empty, the layout Bevy makes the type's pipeline with before any material's key replaces it.
    fn bind_group_layout_entries(_render_device: &RenderDevice, _force_no_bindless: bool) -> Vec<BindGroupLayoutEntry> {
        Vec::new()
    }
}

impl UiMaterial for BcsUiMaterial {
    /// Puts the layout and shaders of the program's version the key names in place of the type's,
    /// and hands each stage the pipeline constants it declares.
    ///
    /// Without an interface vertex shader of the program's own, Bevy's own places the node.
    fn specialize(descriptor: &mut RenderPipelineDescriptor, key: UiMaterialKey<Self>) {
        let key = key.bind_group_data;

        // Group zero is the view, group one the material.
        if let Some(material) = descriptor.layout.get_mut(1) {
            *material = key.layout;
        }
        if let Some(vertex) = key.vertex {
            descriptor.vertex.shader = vertex;
        }
        if let (Some(fragment), Some(state)) = (key.fragment, descriptor.fragment.as_mut()) {
            state.shader = fragment;
        }

        if let Some(program) = programs::lookup(key.material.program) {
            super::constants::apply(descriptor, &program, &key.material.constants);
        }
    }
}

/// Adds what draws interface nodes with shader materials, where the app draws an interface at all.
/// Called from [`super::material::install`].
pub(super) fn install(app: &mut bevy::app::App) {
    use bevy::render::RenderApp;
    use bevy::render::render_phase::DrawFunctions;
    use bevy::ui_render::{TransparentUi, UiMaterialPlugin};

    let draws_interface = app
        .get_sub_app(RenderApp)
        .is_some_and(|render_app| render_app.world().contains_resource::<DrawFunctions<TransparentUi>>());

    if draws_interface {
        app.add_plugins(UiMaterialPlugin::<BcsUiMaterial>::default());
    }
}

/// Gives a node an interface shader material, if the handle names one.
pub fn attach(entity: &mut bevy::ecs::world::EntityWorldMut, untyped: &bevy::asset::UntypedHandle) -> bool {
    match untyped.clone().try_typed::<BcsUiMaterial>() {
        Ok(handle) => {
            entity.insert(MaterialNode(handle));
            true
        }
        Err(_) => false,
    }
}

#[cfg(test)]
mod tests {
    use super::{PipelineProgram, Version, version_to_pack};

    fn at(generation: u32) -> PipelineProgram {
        PipelineProgram { generation, ..Default::default() }
    }

    /// A bind group is packed against the version its key was made from while the table holds it,
    /// waits where a compile landed between the two, which no harness can time, and reads the
    /// table where nothing was handed over.
    #[test]
    fn a_bind_group_is_packed_against_its_keys_version_or_waits() {
        assert!(matches!(version_to_pack(Some(at(3)), Some(at(3))), Version::Pack(p) if p.generation == 3));
        assert!(matches!(version_to_pack(Some(at(3)), Some(at(4))), Version::Stale));
        assert!(matches!(version_to_pack(Some(at(3)), None), Version::Stale));
        assert!(matches!(version_to_pack(None, Some(at(4))), Version::Pack(p) if p.generation == 4));
        assert!(matches!(version_to_pack(None, None), Version::Missing));
    }
}

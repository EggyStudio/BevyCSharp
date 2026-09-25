//! Checking a compiled shader's bindings against the layouts the bridge gives it.
//!
//! A shader that compiles can still declare a binding as something other than what the bridge
//! binds there, a texture where a material holds its floats for instance. naga does not catch that,
//! because it sees the shader and not the layout, and wgpu catches it only when the pipeline is
//! built, which Bevy does not scope, so the error is the device's and every frame drawn with that
//! pipeline is lost. Checking here turns it into what it is, a shader that did not compile, with a
//! message naming the binding, and the last version that compiled stays on screen.
//!
//! Only the groups the bridge lays out are checked: group three of a material, and group zero of a
//! compute shader. Bevy's own groups are Bevy's to get right, and a fragment shader may be drawn
//! as a material or run as a pass, whose group zero differs, so a fragment shader's group zero is
//! left alone.

#![cfg(feature = "render")]

use naga::{
    AddressSpace, ImageClass, ImageDimension, ScalarKind, StorageAccess, StorageFormat, TypeInner,
};

use super::slang::Stage;

/// What the bridge binds at one binding.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
enum Expected {
    Uniform,
    StorageRead,
    StorageReadWrite,
    Texture(ImageDimension, bool),
    Sampler,
    /// A 2D image written to, in this format.
    Written(StorageFormat),
}

impl Expected {
    fn describe(self) -> String {
        match self {
            Expected::Uniform => "a uniform buffer".into(),
            Expected::StorageRead => "a read-only storage buffer".into(),
            Expected::StorageReadWrite => "a storage buffer read and written".into(),
            Expected::Texture(dimension, arrayed) => format!(
                "a {}{:?} texture of floats",
                if arrayed { "array of " } else { "" },
                dimension
            ),
            Expected::Sampler => "a filtering sampler".into(),
            Expected::Written(format) => format!("a 2D {format:?} image written to"),
        }
    }
}

/// What a material binds in group three.
fn material_binding(binding: u32) -> Option<Expected> {
    Some(match binding {
        0 => Expected::Uniform,
        3 => Expected::StorageRead,
        1 | 4 | 6 | 8 | 10 | 12 | 14 | 16 => Expected::Texture(ImageDimension::D2, false),
        2 | 5 | 7 | 9 | 11 | 13 | 15 | 17 => Expected::Sampler,
        18 | 19 => Expected::Texture(ImageDimension::Cube, false),
        20 | 21 => Expected::Texture(ImageDimension::D2, true),
        22 | 23 => Expected::Texture(ImageDimension::D3, false),
        _ => return None,
    })
}

/// What a dispatch binds in group zero.
fn compute_binding(binding: u32) -> Option<Expected> {
    Some(match binding {
        0 | 5 => Expected::Uniform,
        1..=4 => Expected::StorageReadWrite,
        6 => Expected::Written(StorageFormat::Rgba8Unorm),
        7 => Expected::Written(StorageFormat::Rgba16Float),
        8 | 9 => Expected::Texture(ImageDimension::D2, false),
        10 => Expected::Sampler,
        _ => return None,
    })
}

/// Checks the bindings of WGSL compiled for `stage` against what the bridge binds, and says what
/// disagrees.
pub fn check_bindings(wgsl: &str, stage: Stage) -> Result<(), String> {
    let module = naga::front::wgsl::parse_str(wgsl)
        .map_err(|error| format!("the compiled WGSL does not parse: {error}"))?;

    let (group, table, what): (u32, fn(u32) -> Option<Expected>, &str) = match stage {
        Stage::Vertex | Stage::Fragment => (3, material_binding, "a material"),
        Stage::Compute => (0, compute_binding, "a dispatch"),
    };

    let mut problems = Vec::new();

    for (_, global) in module.global_variables.iter() {
        let Some(binding) = &global.binding else {
            continue;
        };

        if binding.group != group {
            continue;
        }

        let name = global.name.as_deref().unwrap_or("a binding");

        let Some(expected) = table(binding.binding) else {
            problems.push(format!(
                "{name} is at binding {} of group {group}, where {what} binds nothing",
                binding.binding
            ));
            continue;
        };

        let inner = &module.types[global.ty].inner;

        let found = match (global.space, inner) {
            (AddressSpace::Uniform, _) => Some(Expected::Uniform),
            (AddressSpace::Storage { access }, _) => Some(if access.contains(StorageAccess::STORE) {
                Expected::StorageReadWrite
            } else {
                Expected::StorageRead
            }),
            (
                AddressSpace::Handle,
                TypeInner::Image {
                    dim,
                    arrayed,
                    class:
                        ImageClass::Sampled {
                            kind: ScalarKind::Float,
                            multi: false,
                        },
                },
            ) => Some(Expected::Texture(*dim, *arrayed)),
            (AddressSpace::Handle, TypeInner::Sampler { comparison: false }) => {
                Some(Expected::Sampler)
            }
            (
                AddressSpace::Handle,
                TypeInner::Image {
                    dim: ImageDimension::D2,
                    arrayed: false,
                    class: ImageClass::Storage { format, access },
                },
            ) if !access.contains(StorageAccess::LOAD) => Some(Expected::Written(*format)),
            _ => None,
        };

        if found != Some(expected) {
            problems.push(format!(
                "{name} is declared at binding {} of group {group} as {}, and {what} binds {} there",
                binding.binding,
                found
                    .map(Expected::describe)
                    .unwrap_or_else(|| "something else".into()),
                expected.describe()
            ));
        }
    }

    if problems.is_empty() {
        Ok(())
    } else {
        Err(format!(
            "The shader compiled, but its bindings disagree with what is bound, and a pipeline built \
             from it would fail.\n{}",
            problems.join("\n")
        ))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bindings_that_match_pass() {
        let wgsl = "
            @group(3) @binding(0) var<uniform> params: array<vec4<f32>, 16>;
            @group(3) @binding(3) var<storage, read> data: array<u32>;
            @group(3) @binding(16) var last: texture_2d<f32>;
            @group(3) @binding(17) var last_sampler: sampler;
            @group(3) @binding(20) var layers: texture_2d_array<f32>;
            @group(0) @binding(0) var<uniform> not_ours: vec4<f32>;
            @fragment fn fragment() -> @location(0) vec4<f32> { return vec4<f32>(0.0); }
        ";

        assert_eq!(check_bindings(wgsl, Stage::Fragment), Ok(()));
    }

    #[test]
    fn a_texture_where_the_floats_are_is_named() {
        let wgsl = "
            @group(3) @binding(0) var wrong: texture_2d<f32>;
            @fragment fn fragment() -> @location(0) vec4<f32> { return vec4<f32>(0.0); }
        ";

        let error = check_bindings(wgsl, Stage::Fragment).unwrap_err();
        assert!(error.contains("wrong"), "{error}");
        assert!(error.contains("uniform"), "{error}");
    }

    #[test]
    fn a_material_cannot_write_its_data() {
        let wgsl = "
            @group(3) @binding(3) var<storage, read_write> data: array<u32>;
            @fragment fn fragment() -> @location(0) vec4<f32> { return vec4<f32>(0.0); }
        ";

        assert!(check_bindings(wgsl, Stage::Fragment).is_err());
    }

    #[test]
    fn a_dispatch_reads_and_writes_its_buffers() {
        let reads = "
            @group(0) @binding(1) var<storage, read> numbers: array<f32>;
            @compute @workgroup_size(1) fn main() { }
        ";

        let writes = "
            @group(0) @binding(1) var<storage, read_write> numbers: array<f32>;
            @compute @workgroup_size(1) fn main() { }
        ";

        assert!(check_bindings(reads, Stage::Compute).is_err());
        assert_eq!(check_bindings(writes, Stage::Compute), Ok(()));
    }

    #[test]
    fn a_dispatch_writes_its_images_in_their_formats() {
        let right = "
            @group(0) @binding(6) var small: texture_storage_2d<rgba8unorm, write>;
            @group(0) @binding(7) var wide: texture_storage_2d<rgba16float, write>;
            @compute @workgroup_size(1) fn main() { }
        ";

        let swapped = "
            @group(0) @binding(6) var wide: texture_storage_2d<rgba16float, write>;
            @compute @workgroup_size(1) fn main() { }
        ";

        assert_eq!(check_bindings(right, Stage::Compute), Ok(()));
        assert!(check_bindings(swapped, Stage::Compute).is_err());
    }

    #[test]
    fn a_binding_past_the_layout_is_named() {
        let wgsl = "
            @group(3) @binding(40) var extra: texture_2d<f32>;
            @fragment fn fragment() -> @location(0) vec4<f32> { return vec4<f32>(0.0); }
        ";

        assert!(check_bindings(wgsl, Stage::Fragment).unwrap_err().contains("binds nothing"));
    }
}

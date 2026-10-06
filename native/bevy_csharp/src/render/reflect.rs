//! What a compiled shader declares, and the bind group layout that follows from it.
//!
//! A shader declares whatever it needs, of any kind and at any size. Nothing here has a table of
//! what is allowed. The layout is read from the shader, so the only limits are the device's.
//!
//! Two sources, because each is right about a different thing:
//!
//! - The WGSL `slangc` wrote, read with naga, says which bindings exist and what they are: uniform
//! or storage, texture of which shape, sampler of which kind, array of how many. The pipeline will
//! be checked against it, so the layout is built from it, and a binding the entry point does not
//! use is not in it at all.
//! - Slang's reflection says what each binding is called and where each number goes inside a
//! uniform buffer, including through structs and arrays. It lets C# set `lights[3].color` by name.
//!
//! **Groups.** Bevy owns groups zero to two of a material's pipeline, and the bridge owns group one
//! of a pass's and a compute shader's. The bridge's Slang modules declare those bindings in spaces
//! 100 and up, and a shader's own globals, declared without a binding, land in space zero. After
//! the compile the groups are renumbered (see [`Family::remap`]), which moves the shader's own to
//! the group they belong in without it having to say so.

#![cfg(feature = "render")]

use std::collections::{BTreeMap, HashMap};

use bevy::render::render_resource::{
    BindGroupLayoutEntry, BindingType, BufferBindingType, SamplerBindingType, ShaderStages,
    StorageTextureAccess, TextureFormat, TextureSampleType, TextureViewDimension,
};
use naga::{
    AddressSpace, ArraySize, ImageClass, ImageDimension, ScalarKind, StorageAccess,
    StorageFormat, TypeInner,
};
use serde_json::Value;

mod layout;
mod spirv;

#[cfg(test)]
mod tests;

pub use layout::*;
pub use spirv::{SCENE_GROUP, reflect_spirv, remap_descriptor_sets};

// -- Reading what slangc wrote

/// What reading a compile produced: the WGSL or the SPIR-V with its groups where they belong, and
/// the layout of the shader's own group.
#[derive(Clone, Debug)]
pub struct Reflected {
    /// Empty where the compile was to SPIR-V.
    pub wgsl: String,
    /// Empty where the compile was to WGSL.
    pub spirv: Vec<u8>,
    pub layout: Layout,
}

/// Takes out the `enable subgroups;` Slang writes ahead of a shader using wave operations.
///
/// WGSL asks for the directive, and naga, which reads the text here and again in Bevy, refuses it
/// as not yet supported while accepting every subgroup builtin and function without it, gated by
/// the device having subgroups instead. So the line goes and the operations stay, and a device
/// without subgroups refuses the pipeline rather than the parse.
fn drop_subgroup_enable(wgsl: &str) -> String {
    if !wgsl.contains("enable subgroups;") {
        return wgsl.to_string();
    }

    wgsl.lines()
        .filter(|line| line.trim() != "enable subgroups;")
        .collect::<Vec<_>>()
        .join("\n")
}

/// Makes the lane of every subgroup shuffle unsigned where Slang wrote it signed.
///
/// WGSL takes either for the lane, and Slang writes `i32(...)`, which naga refuses, since it takes
/// only `u32`. So in a call to one of the shuffles, a second argument that starts with `i32(` is
/// made `u32(` instead, found by counting brackets so the first argument can be any expression.
fn unsigned_shuffle_lanes(wgsl: &str) -> String {
    const CALLS: [&str; 4] = ["subgroupShuffle(", "subgroupShuffleXor(", "subgroupShuffleUp(", "subgroupShuffleDown("];

    if !CALLS.iter().any(|call| wgsl.contains(call)) {
        return wgsl.to_string();
    }

    let mut text = wgsl.to_string();

    for call in CALLS {
        let mut from = 0;

        while let Some(found) = text[from..].find(call) {
            let open = from + found + call.len();
            let bytes = text.as_bytes();
            let mut depth = 1;
            let mut at = open;

            // The comma between the value and the lane, at the call's own depth.
            while at < bytes.len() && depth > 0 {
                match bytes[at] {
                    b'(' => depth += 1,
                    b')' => depth -= 1,
                    b',' if depth == 1 => break,
                    _ => {}
                }
                at += 1;
            }

            if at < bytes.len() && bytes[at] == b',' {
                let lane = at + 1 + (text[at + 1..].len() - text[at + 1..].trim_start().len());

                if text[lane..].starts_with("i32(") {
                    text.replace_range(lane..lane + 3, "u32");
                }
            }

            from = open;
        }
    }

    text
}

/// Renumbers the groups of WGSL `slangc` wrote, and reads the shader's own group.
pub fn reflect(wgsl: &str, reflection: &str, family: Family) -> Result<Reflected, String> {
    let wgsl = unsigned_shuffle_lanes(&drop_subgroup_enable(&remap_groups(wgsl, family)));

    let json: Value = serde_json::from_str(reflection)
        .map_err(|error| format!("slangc's reflection does not parse: {error}"))?;

    let parameters = json
        .get("parameters")
        .and_then(Value::as_array)
        .cloned()
        .unwrap_or_default();

    // A shader lit by Bevy, or reading its decals, calls functions the bridge puts in front of it
    // only once it is compiled, over Bevy's imports, which naga cannot follow here. So it is read
    // with stand-ins of the same signatures, which add no binding and so change nothing this reads.
    let parse = |wgsl: &str| {
        let text = match super::programs::stand_ins(wgsl) {
            Some(stand_ins) => std::borrow::Cow::Owned(format!("{wgsl}\n{stand_ins}")),
            None => std::borrow::Cow::Borrowed(wgsl),
        };
        naga::front::wgsl::parse_str(&text)
            .map_err(|error| format!("the compiled WGSL does not parse: {error}"))
    };

    let mut module = parse(&wgsl)?;
    let mut info = validate(&module);

    // Read again where storage images were declared differently from what Slang wrote, so what
    // follows sees them as the shader meant them.
    let fixed = fix_storage_images(&wgsl, &module, info.as_ref(), &parameters);
    let wgsl = if fixed != wgsl {
        module = parse(&fixed)?;
        info = validate(&module);
        fixed
    } else {
        wgsl
    };

    let mut layouter = naga::proc::Layouter::default();
    layouter
        .update(module.to_ctx())
        .map_err(|error| format!("the compiled WGSL could not be laid out: {error}"))?;

    let sampled = info.as_ref().map(|info| sampled_images(&module, info));

    let group = family.own_group();
    let mut layout = Layout {
        group,
        ..Default::default()
    };

    for (handle, global) in module.global_variables.iter() {
        let Some(binding) = &global.binding else {
            continue;
        };

        if !family.allowed().contains(&binding.group) {
            return Err(format!(
                "{} is bound to group {}, which nothing binds. Leave a shader's own globals \
                 without a binding and the bridge puts them where they belong.",
                global.name.as_deref().unwrap_or("a global"),
                binding.group
            ));
        }

        if binding.group != group {
            // Time is at binding two of a compute shader's inputs group, and anything else there
            // is one of the camera's inputs.
            if matches!(family, Family::Compute) && binding.group == 1 && binding.binding != 2 {
                layout.reads_view = true;
            }
            continue;
        }

        let emitted = global.name.clone().unwrap_or_default();
        let name = source_name(&emitted);

        let mut inner = &module.types[global.ty].inner;
        let mut count = None;

        // An array of textures or samplers is a binding array. An array anywhere else is the
        // contents of a buffer.
        if let TypeInner::BindingArray { base, size } | TypeInner::Array { base, size, .. } = inner
            && global.space == AddressSpace::Handle
        {
            count = Some(match size {
                ArraySize::Constant(size) => size.get(),
                _ => {
                    return Err(format!(
                        "{name} is an array of unknown length. Give it a length, as in \
                         `Texture2D {name}[64]`."
                    ));
                }
            });
            inner = &module.types[*base].inner;
        }

        let kind = match (global.space, inner) {
            (AddressSpace::Uniform, _) => {
                let size = layouter[global.ty].size as u64;
                let fields = if name == "globalParams" {
                    loose_fields(&parameters)
                } else {
                    block_fields(&parameters, &name)
                };

                BindingKind::Uniform { size, fields }
            }
            (AddressSpace::Storage { access }, _) => BindingKind::Storage {
                read_only: !access.contains(StorageAccess::STORE),
            },
            (AddressSpace::Handle, TypeInner::Sampler { comparison }) => BindingKind::Sampler {
                comparison: *comparison,
            },
            (AddressSpace::Handle, TypeInner::Image { dim, arrayed, class }) => {
                let dimension = view_dimension(*dim, *arrayed);

                match class {
                    ImageClass::Sampled { kind, multi } => BindingKind::Texture {
                        dimension,
                        sample: match kind {
                            ScalarKind::Sint => TextureSampleType::Sint,
                            ScalarKind::Uint => TextureSampleType::Uint,
                            // Filterable only where a sampler reads it, because a float
                            // format of 32 bits a channel binds only where the layout says it
                            // is not, and a texture read with `Load` alone has no reason to ask.
                            _ => TextureSampleType::Float {
                                filterable: !*multi && sampled.as_ref().is_none_or(|set| set.contains(&handle)),
                            },
                        },
                        multisampled: *multi,
                    },
                    ImageClass::Depth { multi } => BindingKind::Texture {
                        dimension,
                        sample: TextureSampleType::Depth,
                        multisampled: *multi,
                    },
                    ImageClass::Storage { format, access } => BindingKind::StorageTexture {
                        dimension,
                        format: texture_format(*format).ok_or_else(|| {
                            format!("{name} is an image in {format:?}, which the bridge cannot bind")
                        })?,
                        access: if access.contains(StorageAccess::LOAD)
                            && access.contains(StorageAccess::STORE)
                        {
                            StorageTextureAccess::ReadWrite
                        } else if access.contains(StorageAccess::STORE) {
                            StorageTextureAccess::WriteOnly
                        } else {
                            StorageTextureAccess::ReadOnly
                        },
                    },
                    ImageClass::External => {
                        return Err(format!("{name} is an external texture, which is not bound"));
                    }
                }
            }
            _ => {
                return Err(format!(
                    "{name} is a kind of global the bridge cannot bind ({inner:?})"
                ));
            }
        };

        let name = if name == "globalParams" {
            LOOSE.to_string()
        } else {
            name
        };

        layout.bindings.insert(binding.binding, Binding { name, kind, count });
    }

    Ok(Reflected {
        wgsl: numbers_in_storage(&wgsl, group),
        spirv: Vec::new(),
        layout,
    })
}

/// Declares every block of numbers in the shader's own group as a read-only storage buffer
/// rather than a uniform one.
///
/// wgpu refuses a bind group holding both a binding array and a uniform buffer, because Vulkan
/// cannot update one kind of descriptor after binding where the other is present. A shader with an
/// array of textures and a single loose number would otherwise have no bind group it could be
/// given. WGSL lays a type out the same way whichever address space holds it (a uniform only adds
/// checks), and Slang writes its uniform structs with every alignment spelled out, so the bytes the
/// reflection describes are the bytes the storage buffer holds, and nothing else changes.
///
/// Every block rather than only those beside an array of textures, because each stage is compiled
/// on its own and one that reads no textures cannot know another stage of the same program does.
pub fn numbers_in_storage(wgsl: &str, own_group: u32) -> String {
    let group = format!("@group({own_group})");
    let mut out = String::with_capacity(wgsl.len() + 64);

    for line in wgsl.split_inclusive('\n') {
        if line.trim_start().starts_with("@binding(") && line.contains(&group) {
            out.push_str(&line.replacen("var<uniform>", "var<storage, read>", 1));
        } else {
            out.push_str(line);
        }
    }

    out
}

/// Every texture the module reads through a sampler, or `None` where naga cannot say, in which
/// case every texture is taken to be sampled.
fn sampled_images(
    module: &naga::Module,
    info: &naga::valid::ModuleInfo,
) -> std::collections::HashSet<naga::Handle<naga::GlobalVariable>> {
    let mut sampled = std::collections::HashSet::new();

    for function in function_infos(module, info) {
        sampled.extend(function.sampling_set.iter().map(|key| key.image));
    }

    sampled
}

/// What naga worked out about a module, or `None` where it did not validate, in which case
/// whatever depends on it takes the cautious answer.
fn validate(module: &naga::Module) -> Option<naga::valid::ModuleInfo> {
    use naga::valid::{Capabilities, ValidationFlags, Validator};

    Validator::new(ValidationFlags::all(), Capabilities::all())
        .validate(module)
        .ok()
}

/// Every function's and every entry point's analysis.
fn function_infos<'a>(
    module: &'a naga::Module,
    info: &'a naga::valid::ModuleInfo,
) -> impl Iterator<Item = &'a naga::valid::FunctionInfo> {
    module
        .functions
        .iter()
        .map(move |(handle, _)| &info[handle])
        .chain((0..module.entry_points.len()).map(move |index| info.get_entry_point(index)))
}

/// The WGSL name of a storage format Slang reports by its own name, as `[format(...)]` spells it.
fn storage_format_name(slang: &str) -> Option<String> {
    let special = match slang {
        "r11f_g11f_b10f" => Some("rg11b10ufloat"),
        "rgb10_a2" => Some("rgb10a2unorm"),
        "rgb10_a2ui" => Some("rgb10a2uint"),
        "bgra8" => Some("bgra8unorm"),
        _ => None,
    };

    if let Some(name) = special {
        return Some(name.to_string());
    }

    let (base, kind) = if let Some(base) = slang.strip_suffix("_snorm") {
        (base, "snorm")
    } else if let Some(base) = slang.strip_suffix("ui") {
        (base, "uint")
    } else if let Some(base) = slang.strip_suffix('i') {
        (base, "sint")
    } else if let Some(base) = slang.strip_suffix('f') {
        (base, "float")
    } else {
        (slang, "unorm")
    };

    let channels = base.trim_end_matches(|c: char| c.is_ascii_digit());
    let bits = &base[channels.len()..];

    if !matches!(channels, "r" | "rg" | "rgba") || bits.is_empty() {
        return None;
    }

    Some(format!("{base}{kind}"))
}

/// Puts back what Slang's WGSL output changed about a storage image.
///
/// Slang writes only the storage formats core WGSL has, so an image declared
/// `[format("r16f")]` comes out as `rgba32float`, which a pipeline then refuses to bind an
/// `R16Float` texture to. wgpu and naga take the wider set where the adapter supports it, and the
/// reflection still says what the shader declared, so the declared format goes back in.
///
/// Slang also declares every `RWTexture` as read-write. Only some formats can be read and written in
/// one binding, and a shader that never reads an image has no need to, so an image the shader only
/// writes (or asks the size of) is declared write-only.
fn fix_storage_images(
    wgsl: &str,
    module: &naga::Module,
    info: Option<&naga::valid::ModuleInfo>,
    parameters: &[Value],
) -> String {
    use naga::valid::GlobalUse;

    let mut changes: HashMap<String, (Option<String>, bool)> = HashMap::new();

    for (handle, global) in module.global_variables.iter() {
        let TypeInner::Image {
            class: ImageClass::Storage { .. },
            ..
        } = &module.types[global.ty].inner
        else {
            continue;
        };

        let Some(emitted) = global.name.clone() else {
            continue;
        };

        let name = source_name(&emitted);

        let format = parameters
            .iter()
            .find(|parameter| parameter.get("name").and_then(Value::as_str) == Some(name.as_str()))
            .and_then(|parameter| parameter.get("format").and_then(Value::as_str))
            .and_then(storage_format_name);

        let read = info.is_none_or(|info| {
            function_infos(module, info).any(|function| function[handle].contains(GlobalUse::READ))
        });

        changes.insert(emitted, (format, !read));
    }

    if changes.is_empty() {
        return wgsl.to_string();
    }

    let mut out = String::with_capacity(wgsl.len());

    for line in wgsl.split_inclusive('\n') {
        let trimmed = line.trim_start();
        let found = trimmed
            .starts_with("@binding(")
            .then(|| changes.iter().find(|(emitted, _)| line.contains(&format!("var {emitted} :"))))
            .flatten();

        let (Some((_, (format, write_only))), Some(open), Some(close)) =
            (found, line.find("texture_storage_"), line.rfind('>'))
        else {
            out.push_str(line);
            continue;
        };

        let Some(angle) = line[open..].find('<').map(|at| open + at) else {
            out.push_str(line);
            continue;
        };

        let inside = &line[angle + 1..close];
        let (old_format, old_access) = inside.split_once(',').unwrap_or((inside, " read_write"));

        let format = format.as_deref().unwrap_or(old_format.trim());
        let access = if *write_only { "write" } else { old_access.trim() };

        out.push_str(&line[..angle + 1]);
        out.push_str(&format!("{format}, {access}"));
        out.push_str(&line[close..]);
    }

    out
}

/// Changes every `@group(n)` in WGSL `slangc` wrote to where the family puts it, and spells an
/// array of textures or samplers the way WGSL does.
///
/// Textual, because Slang writes each global on a line of its own starting with its binding, and
/// nothing else in its output looks like one, and rewriting the text keeps everything else exactly
/// as Slang wrote it, which a round trip through naga would not.
///
/// Slang writes an array of textures as `array<texture_2d<f32>, i32(64)>`, which WGSL only
/// accepts for plain values. An array of resources is a `binding_array`, whose length is a plain
/// number, so a global's declaration is rewritten to that.
pub fn remap_groups(wgsl: &str, family: Family) -> String {
    let mut out = String::with_capacity(wgsl.len() + 64);

    for line in wgsl.split_inclusive('\n') {
        if !line.trim_start().starts_with("@binding(") {
            out.push_str(line);
            continue;
        }

        let mut line = line.to_string();

        if let Some(start) = line.find("@group(") {
            let digits = start + "@group(".len();
            if let Some(length) = line[digits..].find(')')
                && let Ok(group) = line[digits..digits + length].trim().parse::<u32>()
            {
                line.replace_range(digits..digits + length, &family.remap(group).to_string());
            }
        }

        for resource in ["texture", "sampler"] {
            let array = format!(": array<{resource}");
            if let Some(at) = line.find(&array) {
                line.replace_range(at + 2..at + 2 + "array<".len(), "binding_array<");

                // The length, as `i32(64)`, becomes `64`.
                if let Some(cast) = line.rfind("i32(")
                    && let Some(close) = line[cast..].find(')')
                {
                    let number = line[cast + 4..cast + close].to_string();
                    line.replace_range(cast..cast + close + 1, &number);
                }
            }
        }

        out.push_str(&line);
    }

    out
}

/// The name a global had in the source, from what Slang called it in the WGSL.
///
/// Slang adds `_` and a number to every name it writes, so taking that off leaves the source's
/// name.
fn source_name(emitted: &str) -> String {
    match emitted.rsplit_once('_') {
        Some((head, tail)) if !head.is_empty() && tail.chars().all(|c| c.is_ascii_digit()) => {
            head.to_string()
        }
        _ => emitted.to_string(),
    }
}

fn view_dimension(dimension: ImageDimension, arrayed: bool) -> TextureViewDimension {
    match (dimension, arrayed) {
        (ImageDimension::D1, _) => TextureViewDimension::D1,
        (ImageDimension::D2, false) => TextureViewDimension::D2,
        (ImageDimension::D2, true) => TextureViewDimension::D2Array,
        (ImageDimension::D3, _) => TextureViewDimension::D3,
        (ImageDimension::Cube, false) => TextureViewDimension::Cube,
        (ImageDimension::Cube, true) => TextureViewDimension::CubeArray,
    }
}

/// The texture format an image written by a shader is declared in.
pub fn texture_format(format: StorageFormat) -> Option<TextureFormat> {
    Some(match format {
        StorageFormat::R8Unorm => TextureFormat::R8Unorm,
        StorageFormat::R8Snorm => TextureFormat::R8Snorm,
        StorageFormat::R8Uint => TextureFormat::R8Uint,
        StorageFormat::R8Sint => TextureFormat::R8Sint,
        StorageFormat::R16Uint => TextureFormat::R16Uint,
        StorageFormat::R16Sint => TextureFormat::R16Sint,
        StorageFormat::R16Float => TextureFormat::R16Float,
        StorageFormat::Rg8Unorm => TextureFormat::Rg8Unorm,
        StorageFormat::Rg8Snorm => TextureFormat::Rg8Snorm,
        StorageFormat::Rg8Uint => TextureFormat::Rg8Uint,
        StorageFormat::Rg8Sint => TextureFormat::Rg8Sint,
        StorageFormat::R32Uint => TextureFormat::R32Uint,
        StorageFormat::R32Sint => TextureFormat::R32Sint,
        StorageFormat::R32Float => TextureFormat::R32Float,
        StorageFormat::Rg16Uint => TextureFormat::Rg16Uint,
        StorageFormat::Rg16Sint => TextureFormat::Rg16Sint,
        StorageFormat::Rg16Float => TextureFormat::Rg16Float,
        StorageFormat::Rgba8Unorm => TextureFormat::Rgba8Unorm,
        StorageFormat::Rgba8Snorm => TextureFormat::Rgba8Snorm,
        StorageFormat::Rgba8Uint => TextureFormat::Rgba8Uint,
        StorageFormat::Rgba8Sint => TextureFormat::Rgba8Sint,
        StorageFormat::Bgra8Unorm => TextureFormat::Bgra8Unorm,
        StorageFormat::Rgb10a2Uint => TextureFormat::Rgb10a2Uint,
        StorageFormat::Rgb10a2Unorm => TextureFormat::Rgb10a2Unorm,
        StorageFormat::Rg11b10Ufloat => TextureFormat::Rg11b10Ufloat,
        StorageFormat::Rg32Uint => TextureFormat::Rg32Uint,
        StorageFormat::Rg32Sint => TextureFormat::Rg32Sint,
        StorageFormat::Rg32Float => TextureFormat::Rg32Float,
        StorageFormat::Rgba16Uint => TextureFormat::Rgba16Uint,
        StorageFormat::Rgba16Sint => TextureFormat::Rgba16Sint,
        StorageFormat::Rgba16Float => TextureFormat::Rgba16Float,
        StorageFormat::Rgba32Uint => TextureFormat::Rgba32Uint,
        StorageFormat::Rgba32Sint => TextureFormat::Rgba32Sint,
        StorageFormat::Rgba32Float => TextureFormat::Rgba32Float,
        StorageFormat::R16Unorm => TextureFormat::R16Unorm,
        StorageFormat::R16Snorm => TextureFormat::R16Snorm,
        StorageFormat::Rg16Unorm => TextureFormat::Rg16Unorm,
        StorageFormat::Rg16Snorm => TextureFormat::Rg16Snorm,
        StorageFormat::Rgba16Unorm => TextureFormat::Rgba16Unorm,
        StorageFormat::Rgba16Snorm => TextureFormat::Rgba16Snorm,
        _ => return None,
    })
}

/// The loose globals' fields, every parameter whose binding is an offset in the shared buffer.
fn loose_fields(parameters: &[Value]) -> Vec<Field> {
    parameters
        .iter()
        .filter(|parameter| {
            parameter
                .pointer("/binding/kind")
                .and_then(Value::as_str)
                == Some("uniform")
        })
        .filter_map(field_from)
        .collect()
}

/// The fields of the `ConstantBuffer` a parameter called `name` is.
fn block_fields(parameters: &[Value], name: &str) -> Vec<Field> {
    parameters
        .iter()
        .find(|parameter| parameter.get("name").and_then(Value::as_str) == Some(name))
        .and_then(|parameter| parameter.pointer("/type/elementType/fields"))
        .and_then(Value::as_array)
        .map(|fields| fields.iter().filter_map(field_from).collect())
        .unwrap_or_default()
}

/// One field, from a reflected parameter or struct member with a uniform offset.
fn field_from(value: &Value) -> Option<Field> {
    Some(Field {
        name: value.get("name")?.as_str()?.to_string(),
        offset: value.pointer("/binding/offset")?.as_u64()? as u32,
        ty: field_type(value.get("type")?)?,
    })
}

fn field_type(ty: &Value) -> Option<FieldType> {
    let scalar_of = |value: &Value| {
        value
            .get("scalarType")
            .and_then(Value::as_str)
            .and_then(Scalar::from_reflection)
    };

    Some(match ty.get("kind")?.as_str()? {
        "scalar" => FieldType::Scalar(scalar_of(ty)?),
        "vector" => FieldType::Vector(
            scalar_of(ty.get("elementType")?)?,
            ty.get("elementCount")?.as_u64()? as u32,
        ),
        "matrix" => FieldType::Matrix(
            scalar_of(ty.get("elementType")?)?,
            ty.get("rowCount")?.as_u64()? as u32,
            ty.get("columnCount")?.as_u64()? as u32,
        ),
        "array" => {
            let element = field_type(ty.get("elementType")?)?;

            FieldType::Array {
                count: ty.get("elementCount")?.as_u64()? as u32,
                stride: ty
                    .get("uniformStride")
                    .and_then(Value::as_u64)
                    .unwrap_or(16) as u32,
                element: Box::new(element),
            }
        }
        "struct" => FieldType::Struct(
            ty.get("fields")?
                .as_array()?
                .iter()
                .filter_map(field_from)
                .collect(),
        ),
        _ => return None,
    })
}

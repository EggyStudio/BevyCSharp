//! Reading what slangc wrote as SPIR-V.

use super::*;

// -- Reading what slangc wrote as SPIR-V

/// The group `bcs_ray` declares Solari's scene in, in SPIR-V and in a pipeline alike.
pub const SCENE_GROUP: u32 = 2;

/// Renumbers the groups of SPIR-V `slangc` wrote, and reads the shader's own group from Slang's
/// reflection alone.
///
/// naga cannot read what SPIR-V is used for here (it has no ray query instructions), and the
/// binary goes to the driver untouched, so nothing checks it against a layout the way a WGSL
/// shader is checked. The layout comes from the reflection instead, which names every binding,
/// its kind, its format and whether the entry point uses it, and a binding the entry point does not
/// use is left out, as naga leaves it out of WGSL.
///
/// Two things the reflection cannot say are settled the safe way. A comparison sampler reads as a
/// plain one, so it is bound as a filtering sampler. Whether a texture of floats is sampled with
/// filtering is not said either, so it is bound as unfilterable, which takes an image of any float
/// format. Nothing checks a sampler against the texture it samples in SPIR-V passed through, and
/// the driver filters whatever the format allows.
pub fn reflect_spirv(spirv: &[u8], reflection: &str, family: Family) -> Result<Reflected, String> {
    let json: Value = serde_json::from_str(reflection)
        .map_err(|error| format!("slangc's reflection does not parse: {error}"))?;

    let parameters = json
        .get("parameters")
        .and_then(Value::as_array)
        .cloned()
        .unwrap_or_default();

    // Which parameters the entry point uses, by name. A parameter the entry point list does not
    // mention at all is kept, since leaving out something used would be the worse mistake.
    let used: HashMap<String, bool> = json
        .pointer("/entryPoints/0/bindings")
        .and_then(Value::as_array)
        .map(|bindings| {
            bindings
                .iter()
                .filter_map(|binding| {
                    let name = binding.get("name")?.as_str()?.to_string();
                    let used = binding
                        .pointer("/binding/used")
                        .and_then(Value::as_u64)
                        .is_none_or(|used| used != 0);
                    Some((name, used))
                })
                .collect()
        })
        .unwrap_or_default();

    let group = family.own_group();
    let mut layout = Layout {
        group,
        spirv: true,
        ..Default::default()
    };

    // The loose globals, which Slang gathers into one uniform buffer of its own in space zero.
    let loose = loose_fields(&parameters);

    if !loose.is_empty() {
        let binding = json
            .pointer("/globalScope/binding/index")
            .and_then(Value::as_u64)
            .unwrap_or(0) as u32;

        let size = parameters
            .iter()
            .filter(|parameter| {
                parameter.pointer("/binding/kind").and_then(Value::as_str) == Some("uniform")
            })
            .filter_map(|parameter| {
                let offset = parameter.pointer("/binding/offset")?.as_u64()?;
                let size = parameter.pointer("/binding/size")?.as_u64()?;
                Some(offset + size)
            })
            .max()
            .unwrap_or(0);

        layout.bindings.insert(
            binding,
            Binding {
                name: LOOSE.to_string(),
                kind: BindingKind::Uniform {
                    size: size.div_ceil(16) * 16,
                    fields: loose,
                },
                count: None,
            },
        );
    }

    for parameter in &parameters {
        let Some(name) = parameter.get("name").and_then(Value::as_str) else {
            continue;
        };

        if parameter.pointer("/binding/kind").and_then(Value::as_str) != Some("descriptorTableSlot") {
            continue;
        }

        let space = parameter.pointer("/binding/space").and_then(Value::as_u64).unwrap_or(0) as u32;
        let index = parameter.pointer("/binding/index").and_then(Value::as_u64).unwrap_or(0) as u32;
        let target = family.remap(space);

        if target == SCENE_GROUP && matches!(family, Family::Compute) {
            if used.get(name).copied().unwrap_or(true) {
                layout.traces_scene = true;
            }
            continue;
        }

        if !family.allowed().contains(&target) {
            return Err(format!(
                "{name} is bound to group {target}, which nothing binds. Leave a shader's own globals \
                 without a binding and the bridge puts them where they belong."
            ));
        }

        if target != group {
            if matches!(family, Family::Compute)
                && target == 1
                && index != 2
                && used.get(name).copied().unwrap_or(true)
            {
                layout.reads_view = true;
            }
            continue;
        }

        if !used.get(name).copied().unwrap_or(true) {
            continue;
        }

        let ty = parameter.get("type").cloned().unwrap_or(Value::Null);
        let (ty, count) = match ty.get("kind").and_then(Value::as_str) {
            Some("array") => (
                ty.get("elementType").cloned().unwrap_or(Value::Null),
                Some(ty.get("elementCount").and_then(Value::as_u64).unwrap_or(0) as u32),
            ),
            _ => (ty, None),
        };

        if count == Some(0) {
            return Err(format!(
                "{name} is an array of unknown length. Give it a length, as in \
                 `Texture2D {name}[64]`."
            ));
        }

        let kind = spirv_binding_kind(name, parameter, &ty)?;
        layout.bindings.insert(index, Binding { name: name.to_string(), kind, count });
    }

    Ok(Reflected {
        wgsl: String::new(),
        spirv: remap_descriptor_sets(spirv, family)?,
        layout,
    })
}

/// What one binding of a SPIR-V shader's own group is, from its reflected type.
fn spirv_binding_kind(name: &str, parameter: &Value, ty: &Value) -> Result<BindingKind, String> {
    let kind = ty.get("kind").and_then(Value::as_str).unwrap_or("");

    if kind == "constantBuffer" {
        let size = ty
            .pointer("/elementType/sizes")
            .and_then(Value::as_array)
            .and_then(|sizes| {
                sizes
                    .iter()
                    .find(|size| size.get("kind").and_then(Value::as_str) == Some("uniform"))
            })
            .and_then(|size| size.get("value"))
            .and_then(Value::as_u64)
            .unwrap_or(16);

        let fields = ty
            .pointer("/elementType/fields")
            .and_then(Value::as_array)
            .map(|fields| fields.iter().filter_map(field_from).collect())
            .unwrap_or_default();

        return Ok(BindingKind::Uniform {
            size: size.div_ceil(16) * 16,
            fields,
        });
    }

    if kind == "samplerState" {
        return Ok(BindingKind::Sampler { comparison: false });
    }

    if kind != "resource" {
        return Err(format!("{name} is a kind of global the bridge cannot bind ({kind})"));
    }

    let shape = ty.get("baseShape").and_then(Value::as_str).unwrap_or("");
    let written = ty.get("access").and_then(Value::as_str).is_some_and(|access| access != "read");

    match shape {
        "structuredBuffer" | "byteAddressBuffer" => {
            return Ok(BindingKind::Storage { read_only: !written });
        }
        // A ray scene the game builds (see `super::rays`), handed over by name.
        "accelerationStructure" => return Ok(BindingKind::AccelerationStructure),
        _ => {}
    }

    let arrayed = ty.get("array").and_then(Value::as_bool).unwrap_or(false);
    let dimension = match (shape, arrayed) {
        ("texture1D", _) => TextureViewDimension::D1,
        ("texture2D", false) => TextureViewDimension::D2,
        ("texture2D", true) => TextureViewDimension::D2Array,
        ("texture3D", _) => TextureViewDimension::D3,
        ("textureCube", false) => TextureViewDimension::Cube,
        ("textureCube", true) => TextureViewDimension::CubeArray,
        _ => return Err(format!("{name} is a kind of resource the bridge cannot bind ({shape})")),
    };

    // What one texel holds, which picks the sample type and an unformatted image's format.
    let result = ty.get("resultType").cloned().unwrap_or(Value::Null);
    let (scalar, channels) = match result.get("kind").and_then(Value::as_str) {
        Some("vector") => (
            result.pointer("/elementType/scalarType").and_then(Value::as_str).unwrap_or("float32"),
            result.get("elementCount").and_then(Value::as_u64).unwrap_or(4),
        ),
        _ => (result.get("scalarType").and_then(Value::as_str).unwrap_or("float32"), 1),
    };

    if written {
        let declared = parameter
            .get("format")
            .and_then(Value::as_str)
            .and_then(storage_format_name);

        // Slang writes an image with no `[format]` in the format its texel type suggests, which
        // is the widest of each kind.
        let format = match declared {
            Some(format) => wgsl_storage_format(&format)
                .ok_or_else(|| format!("{name} is an image in {format}, which the bridge cannot bind"))?,
            None => match (scalar, channels) {
                ("uint32", 1) => TextureFormat::R32Uint,
                ("uint32", 2) => TextureFormat::Rg32Uint,
                ("uint32", _) => TextureFormat::Rgba32Uint,
                ("int32", 1) => TextureFormat::R32Sint,
                ("int32", 2) => TextureFormat::Rg32Sint,
                ("int32", _) => TextureFormat::Rgba32Sint,
                (_, 1) => TextureFormat::R32Float,
                (_, 2) => TextureFormat::Rg32Float,
                _ => TextureFormat::Rgba32Float,
            },
        };

        return Ok(BindingKind::StorageTexture {
            dimension,
            format,
            access: StorageTextureAccess::ReadWrite,
        });
    }

    let multisampled = ty.get("multisample").and_then(Value::as_bool).unwrap_or(false);

    Ok(BindingKind::Texture {
        dimension,
        sample: match scalar {
            "uint32" => TextureSampleType::Uint,
            "int32" => TextureSampleType::Sint,
            _ => TextureSampleType::Float { filterable: false },
        },
        multisampled,
    })
}

/// A storage format by the name WGSL spells it, as [`storage_format_name`] answers it.
fn wgsl_storage_format(name: &str) -> Option<TextureFormat> {
    let wgsl = format!("@group(0) @binding(0) var image: texture_storage_2d<{name}, write>;");
    let module = naga::front::wgsl::parse_str(&wgsl).ok()?;

    module.global_variables.iter().find_map(|(_, global)| match module.types[global.ty].inner {
        TypeInner::Image {
            class: ImageClass::Storage { format, .. },
            ..
        } => texture_format(format),
        _ => None,
    })
}

/// Moves every descriptor set in SPIR-V to the group the family puts it in.
///
/// A set is a decoration on a variable, `OpDecorate %variable DescriptorSet n`, one word for `n`,
/// so the binary is walked instruction by instruction and that one word changed. Nothing else
/// moves, which keeps the rest exactly as Slang wrote it.
pub fn remap_descriptor_sets(spirv: &[u8], family: Family) -> Result<Vec<u8>, String> {
    const MAGIC: u32 = 0x0723_0203;
    const OP_DECORATE: u32 = 71;
    const DESCRIPTOR_SET: u32 = 34;

    if spirv.len() % 4 != 0 || spirv.len() < 20 {
        return Err("slangc's SPIR-V is not a whole number of words".into());
    }

    let mut words: Vec<u32> = spirv
        .chunks_exact(4)
        .map(|word| u32::from_le_bytes([word[0], word[1], word[2], word[3]]))
        .collect();

    if words[0] != MAGIC {
        return Err("slangc's output is not SPIR-V".into());
    }

    // Five words of header, then instructions, each with its length in its first word's high half.
    let mut at = 5;

    while at < words.len() {
        let length = (words[at] >> 16) as usize;
        let opcode = words[at] & 0xFFFF;

        if length == 0 || at + length > words.len() {
            return Err("slangc's SPIR-V has an instruction that runs past its end".into());
        }

        if opcode == OP_DECORATE && length == 4 && words[at + 2] == DESCRIPTOR_SET {
            words[at + 3] = family.remap(words[at + 3]);
        }

        at += length;
    }

    Ok(words.iter().flat_map(|word| word.to_le_bytes()).collect())
}

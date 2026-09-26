//! What a compiled shader declares, and the bind group layout that follows from it.
//!
//! A shader declares whatever it needs, of any kind and at any size. Nothing here has a table of
//! what is allowed. The layout is read from the shader, so the only limits are the device's.
//!
//! Two sources, because each is right about a different thing:
//!
//! - The WGSL `slangc` wrote, read with naga, says which bindings exist and what they are:
//!   uniform or storage, texture of which shape, sampler of which kind, array of how many. It is
//!   what the pipeline will be checked against, so it is what the layout is built from, and a
//!   binding the entry point does not use is not in it at all.
//! - Slang's reflection says what each binding is called and where each number goes inside a
//!   uniform buffer, including through structs and arrays. It is what lets C# set `lights[3].color`
//!   by name.
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

/// What a shader is compiled for, which decides where its own globals go.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Family {
    /// Drawn on a mesh, in Bevy's pipeline. Its own globals are group three.
    Material,
    /// Run over a camera's picture. Its own globals are group zero, the bridge's inputs group one.
    Pass,
    /// Dispatched over buffers and images. Group zero is its own, group one the bridge's.
    Compute,
}

impl Family {
    /// The group a shader's own globals end up in.
    pub fn own_group(self) -> u32 {
        match self {
            Family::Material => 3,
            Family::Pass | Family::Compute => 0,
        }
    }

    /// Where a group `slangc` wrote goes.
    ///
    /// Space zero is the shader's own. Spaces 100 to 102 are Bevy's groups zero to two, which only a
    /// material has, and space 101 is also where the pass and compute modules put the bridge's
    /// inputs, which are group one there.
    pub fn remap(self, group: u32) -> u32 {
        match (self, group) {
            (_, 0) => self.own_group(),
            (Family::Material, 100..=102) => group - 100,
            (Family::Pass | Family::Compute, 101) => 1,
            (_, other) => other,
        }
    }

    /// The groups a shader of this family may use, and nothing else binds.
    fn allowed(self) -> &'static [u32] {
        match self {
            Family::Material => &[0, 1, 2, 3],
            Family::Pass | Family::Compute => &[0, 1],
        }
    }
}

/// A scalar a uniform holds.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Scalar {
    F32,
    I32,
    U32,
    Bool,
}

impl Scalar {
    fn from_reflection(name: &str) -> Option<Scalar> {
        Some(match name {
            "float32" => Scalar::F32,
            "int32" => Scalar::I32,
            "uint32" => Scalar::U32,
            "bool" => Scalar::Bool,
            _ => return None,
        })
    }

    /// What C# calls it, for messages.
    pub fn describe(self) -> &'static str {
        match self {
            Scalar::F32 => "float",
            Scalar::I32 => "int",
            Scalar::U32 => "uint",
            Scalar::Bool => "bool",
        }
    }
}

/// The shape of a number, or of a group of them, inside a uniform buffer.
#[derive(Clone, PartialEq, Debug)]
pub enum FieldType {
    Scalar(Scalar),
    Vector(Scalar, u32),
    /// Rows and columns. Stored a row at a time, each row padded to sixteen bytes, which is the
    /// order a C# `Matrix4x4` holds its numbers in.
    Matrix(Scalar, u32, u32),
    Array {
        element: Box<FieldType>,
        count: u32,
        /// Bytes from one element to the next, which in a uniform is at least sixteen.
        stride: u32,
    },
    Struct(Vec<Field>),
}

impl FieldType {
    /// The scalar at the bottom of it, where there is one kind.
    pub fn scalar(&self) -> Option<Scalar> {
        match self {
            FieldType::Scalar(scalar)
            | FieldType::Vector(scalar, _)
            | FieldType::Matrix(scalar, _, _) => Some(*scalar),
            FieldType::Array { element, .. } => element.scalar(),
            FieldType::Struct(_) => None,
        }
    }

    /// How many numbers one of it is, where it is a scalar, a vector or a matrix.
    pub fn components(&self) -> Option<u32> {
        match self {
            FieldType::Scalar(_) => Some(1),
            FieldType::Vector(_, count) => Some(*count),
            FieldType::Matrix(_, rows, columns) => Some(rows * columns),
            _ => None,
        }
    }

    /// What it is, as the shader would write it, for messages.
    pub fn describe(&self) -> String {
        match self {
            FieldType::Scalar(scalar) => scalar.describe().into(),
            FieldType::Vector(scalar, count) => format!("{}{count}", scalar.describe()),
            FieldType::Matrix(scalar, rows, columns) => {
                format!("{}{rows}x{columns}", scalar.describe())
            }
            FieldType::Array { element, count, .. } => format!("{}[{count}]", element.describe()),
            FieldType::Struct(_) => "struct".into(),
        }
    }
}

/// A named number, or group of them, at an offset in a uniform buffer.
#[derive(Clone, PartialEq, Debug)]
pub struct Field {
    pub name: String,
    pub offset: u32,
    pub ty: FieldType,
}

/// What one binding in a shader's own group is.
#[derive(Clone, PartialEq, Debug)]
pub enum BindingKind {
    /// A uniform buffer of `size` bytes, holding the named fields.
    Uniform { size: u64, fields: Vec<Field> },
    Storage { read_only: bool },
    Texture {
        dimension: TextureViewDimension,
        sample: TextureSampleType,
        multisampled: bool,
    },
    StorageTexture {
        dimension: TextureViewDimension,
        format: TextureFormat,
        access: StorageTextureAccess,
    },
    Sampler { comparison: bool },
}

/// One binding in a shader's own group.
#[derive(Clone, PartialEq, Debug)]
pub struct Binding {
    /// What the shader calls it. The loose globals' buffer is called `$globals`.
    pub name: String,
    pub kind: BindingKind,
    /// How many, where it is an array of textures, samplers or buffers.
    pub count: Option<u32>,
}

impl Binding {
    /// What it is, as the shader would write it, for messages.
    pub fn describe(&self) -> String {
        let one = match &self.kind {
            BindingKind::Uniform { .. } => "uniform buffer".to_string(),
            BindingKind::Storage { read_only: true } => "buffer read".to_string(),
            BindingKind::Storage { read_only: false } => "buffer read and written".to_string(),
            BindingKind::Texture { dimension, .. } => format!("{dimension:?} texture"),
            BindingKind::StorageTexture {
                dimension, format, ..
            } => format!("{dimension:?} {format:?} image written"),
            BindingKind::Sampler { comparison: true } => "comparison sampler".to_string(),
            BindingKind::Sampler { comparison: false } => "sampler".to_string(),
        };

        match self.count {
            Some(count) => format!("{one} [{count}]"),
            None => one,
        }
    }
}

/// Everything a shader's own group holds, by binding number.
#[derive(Clone, PartialEq, Debug, Default)]
pub struct Layout {
    pub group: u32,
    pub bindings: BTreeMap<u32, Binding>,
    /// Whether the shader reads a camera's inputs (the picture, the view, depth, motion) beside
    /// time, which is what a compute shader importing `bcs_pass` does, and what decides that it
    /// can only run on a camera.
    pub reads_view: bool,
}

/// The name the loose globals' uniform buffer goes by, which no global can have.
pub const LOOSE: &str = "$globals";

/// Where a named value goes.
#[derive(Clone, PartialEq, Debug)]
pub enum Target<'a> {
    /// Some number of bytes inside the uniform buffer at `binding`.
    Uniform {
        binding: u32,
        offset: u32,
        ty: &'a FieldType,
    },
    /// A whole binding: a texture, a sampler, a buffer, or a uniform buffer set as bytes.
    Resource { binding: u32, info: &'a Binding },
}

impl Layout {
    /// Finds where a name goes: `roughness`, `lights[3].color`, `sun.power`, `albedo`.
    ///
    /// A loose global is found by its own name, and a field of a `ConstantBuffer` by the buffer's
    /// name and the field's.
    pub fn find(&self, path: &str) -> Option<Target<'_>> {
        let (head, rest) = split_head(path);

        // A binding named outright: a resource, or a uniform buffer as a whole.
        for (number, binding) in &self.bindings {
            if binding.name != head || binding.name == LOOSE {
                continue;
            }

            return match (&binding.kind, rest) {
                (_, "") => Some(Target::Resource {
                    binding: *number,
                    info: binding,
                }),
                (BindingKind::Uniform { fields, .. }, rest) => {
                    let rest = rest.strip_prefix('.').unwrap_or(rest);
                    find_field(fields, rest).map(|(offset, ty)| Target::Uniform {
                        binding: *number,
                        offset,
                        ty,
                    })
                }
                _ => None,
            };
        }

        // Otherwise a loose global, in the buffer they share.
        for (number, binding) in &self.bindings {
            if let (true, BindingKind::Uniform { fields, .. }) = (binding.name == LOOSE, &binding.kind)
                && let Some((offset, ty)) = find_field(fields, path)
            {
                return Some(Target::Uniform {
                    binding: *number,
                    offset,
                    ty,
                });
            }
        }

        None
    }

    /// Every name a value can be given under, for a message saying what there is.
    pub fn names(&self) -> Vec<String> {
        let mut names = Vec::new();

        for binding in self.bindings.values() {
            match &binding.kind {
                BindingKind::Uniform { fields, .. } if binding.name == LOOSE => {
                    names.extend(fields.iter().map(|field| field.name.clone()));
                }
                BindingKind::Uniform { fields, .. } => {
                    names.extend(
                        fields
                            .iter()
                            .map(|field| format!("{}.{}", binding.name, field.name)),
                    );
                }
                _ => names.push(binding.name.clone()),
            }
        }

        names
    }

    /// Adds another stage's view of the same group, which is what a material with a vertex and a
    /// fragment shader has.
    ///
    /// One source compiled for two entry points numbers its globals the same way both times, so a
    /// binding the two share has to be the same thing. When it is not, the stages were written in
    /// different files that disagree, and the material could not be bound for both.
    pub fn merge(&mut self, other: &Layout) -> Result<(), String> {
        for (number, binding) in &other.bindings {
            match self.bindings.get_mut(number) {
                None => {
                    self.bindings.insert(*number, binding.clone());
                }
                Some(existing) => {
                    let same = existing.name == binding.name
                        && existing.count == binding.count
                        && std::mem::discriminant(&existing.kind)
                            == std::mem::discriminant(&binding.kind);

                    if !same {
                        return Err(format!(
                            "binding {number} is {} ({}) in one stage and {} ({}) in another. \
                             Declare a material's globals once, in one file every stage imports.",
                            existing.name,
                            existing.describe(),
                            binding.name,
                            binding.describe()
                        ));
                    }

                    // Filterable where any stage samples it, which is the one that needs it.
                    if let (
                        BindingKind::Texture {
                            sample: TextureSampleType::Float { filterable },
                            ..
                        },
                        BindingKind::Texture {
                            sample: TextureSampleType::Float { filterable: other },
                            ..
                        },
                    ) = (&mut existing.kind, &binding.kind)
                    {
                        *filterable |= *other;
                    }

                    // The larger view of a uniform buffer, since a stage may leave out the tail
                    // it does not read.
                    if let (
                        BindingKind::Uniform { size, fields },
                        BindingKind::Uniform {
                            size: other_size,
                            fields: other_fields,
                        },
                    ) = (&mut existing.kind, &binding.kind)
                        && other_size > size
                    {
                        *size = *other_size;
                        *fields = other_fields.clone();
                    }
                }
            }
        }

        Ok(())
    }

    /// The bind group layout entries, visible to `stages`.
    pub fn entries(&self, stages: ShaderStages) -> Vec<BindGroupLayoutEntry> {
        self.bindings
            .iter()
            .map(|(number, binding)| BindGroupLayoutEntry {
                binding: *number,
                visibility: stages,
                ty: match &binding.kind {
                    // Storage rather than uniform, which is what `numbers_in_storage` made of
                    // the shader's declaration. See there for why.
                    BindingKind::Uniform { size, .. } => BindingType::Buffer {
                        ty: BufferBindingType::Storage { read_only: true },
                        has_dynamic_offset: false,
                        min_binding_size: std::num::NonZeroU64::new(*size),
                    },
                    BindingKind::Storage { read_only } => BindingType::Buffer {
                        ty: BufferBindingType::Storage {
                            read_only: *read_only,
                        },
                        has_dynamic_offset: false,
                        min_binding_size: None,
                    },
                    BindingKind::Texture {
                        dimension,
                        sample,
                        multisampled,
                    } => BindingType::Texture {
                        sample_type: *sample,
                        view_dimension: *dimension,
                        multisampled: *multisampled,
                    },
                    BindingKind::StorageTexture {
                        dimension,
                        format,
                        access,
                    } => BindingType::StorageTexture {
                        access: *access,
                        format: *format,
                        view_dimension: *dimension,
                    },
                    BindingKind::Sampler { comparison } => BindingType::Sampler(if *comparison {
                        SamplerBindingType::Comparison
                    } else {
                        SamplerBindingType::Filtering
                    }),
                },
                count: binding.count.and_then(std::num::NonZeroU32::new),
            })
            .collect()
    }
}

/// Splits `lights[3].color` into `lights` and `[3].color`.
fn split_head(path: &str) -> (&str, &str) {
    let end = path.find(['.', '[']).unwrap_or(path.len());
    (&path[..end], &path[end..])
}

/// Walks a path through fields, answering the offset it reaches and the type there.
fn find_field<'a>(fields: &'a [Field], path: &str) -> Option<(u32, &'a FieldType)> {
    let (head, mut rest) = split_head(path);
    let field = fields.iter().find(|field| field.name == head)?;

    let mut offset = field.offset;
    let mut ty = &field.ty;

    loop {
        if rest.is_empty() {
            return Some((offset, ty));
        }

        if let Some(after) = rest.strip_prefix('[') {
            let close = after.find(']')?;
            let index: u32 = after[..close].trim().parse().ok()?;

            let FieldType::Array {
                element,
                count,
                stride,
            } = ty
            else {
                return None;
            };

            if index >= *count {
                return None;
            }

            offset += index * stride;
            ty = element;
            rest = &after[close + 1..];
        } else if let Some(after) = rest.strip_prefix('.') {
            let FieldType::Struct(inner) = ty else {
                return None;
            };

            let (name, next) = split_head(after);
            let field = inner.iter().find(|field| field.name == name)?;

            offset += field.offset;
            ty = &field.ty;
            rest = next;
        } else {
            return None;
        }
    }
}

// -- Reading what slangc wrote

/// What reading a compile produced: the WGSL with its groups where they belong, and the layout of
/// the shader's own group.
#[derive(Clone, Debug)]
pub struct Reflected {
    pub wgsl: String,
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

/// Renumbers the groups of WGSL `slangc` wrote, and reads the shader's own group.
pub fn reflect(wgsl: &str, reflection: &str, family: Family) -> Result<Reflected, String> {
    let wgsl = drop_subgroup_enable(&remap_groups(wgsl, family));

    let json: Value = serde_json::from_str(reflection)
        .map_err(|error| format!("slangc's reflection does not parse: {error}"))?;

    let parameters = json
        .get("parameters")
        .and_then(Value::as_array)
        .cloned()
        .unwrap_or_default();

    let parse = |wgsl: &str| {
        naga::front::wgsl::parse_str(wgsl)
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
        bindings: BTreeMap::new(),
        reads_view: false,
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
/// Slang adds `_` and a number to every name it writes, so the source's name is what is left when
/// that is taken off.
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

/// The loose globals' fields: every parameter whose binding is an offset in the shared buffer.
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

#[cfg(test)]
mod tests {
    use super::*;

    const FRAGMENT_WGSL: &str = include_str!("fixtures/material_fragment.wgsl");
    const FRAGMENT_JSON: &str = include_str!("fixtures/material_fragment.json");
    const VERTEX_WGSL: &str = include_str!("fixtures/material_vertex.wgsl");
    const VERTEX_JSON: &str = include_str!("fixtures/material_vertex.json");

    fn fragment() -> Reflected {
        reflect(FRAGMENT_WGSL, FRAGMENT_JSON, Family::Material).expect("the fixture reflects")
    }

    #[test]
    fn the_groups_move_to_where_they_belong() {
        let wgsl = fragment().wgsl;

        assert!(wgsl.contains("@binding(1) @group(3) var albedo_0"));
        assert!(wgsl.contains("var layers_0 : binding_array<texture_2d<f32>, 64>"), "{wgsl}");
        assert!(wgsl.contains("@binding(11) @group(0) var<uniform> globals_0"));
        assert!(!wgsl.contains("@group(100)"));
    }

    #[test]
    fn slangs_format_names_become_wgsls() {
        assert_eq!(storage_format_name("r16f").as_deref(), Some("r16float"));
        assert_eq!(storage_format_name("rgba8").as_deref(), Some("rgba8unorm"));
        assert_eq!(storage_format_name("rg32ui").as_deref(), Some("rg32uint"));
        assert_eq!(storage_format_name("r32i").as_deref(), Some("r32sint"));
        assert_eq!(storage_format_name("rgba8_snorm").as_deref(), Some("rgba8snorm"));
        assert_eq!(storage_format_name("r11f_g11f_b10f").as_deref(), Some("rg11b10ufloat"));
        assert_eq!(storage_format_name("unknown"), None);
    }

    /// An image declared in a format core WGSL lacks gets that format back, and one the shader
    /// only writes is bound write-only.
    #[test]
    fn a_storage_image_is_declared_as_the_shader_meant_it() {
        let wgsl = "@binding(0) @group(0) var picture_0 : texture_storage_2d<rgba32float, read_write>;\n\
                    @compute @workgroup_size(1) fn main() { textureStore(picture_0, vec2<i32>(0), vec4<f32>(1.0)); }\n";
        let json = r#"{"parameters":[{"name":"picture","format":"r16f","binding":{"kind":"descriptorTableSlot","index":0},"type":{"kind":"resource","baseShape":"texture2D","access":"readWrite"}}]}"#;

        let reflected = reflect(wgsl, json, Family::Compute).expect("it reflects");

        assert!(
            reflected.wgsl.contains("texture_storage_2d<r16float, write>"),
            "{}",
            reflected.wgsl
        );

        let Some(Binding {
            kind: BindingKind::StorageTexture { format, access, .. },
            ..
        }) = reflected.layout.bindings.get(&0)
        else {
            panic!("the image is not a storage binding");
        };

        assert_eq!(*format, TextureFormat::R16Float);
        assert_eq!(*access, StorageTextureAccess::WriteOnly);
    }

    /// Bevy's own uniforms stay uniforms, and the material's become storage, which is what lets
    /// them share a bind group with its array of textures.
    #[test]
    fn the_materials_numbers_are_read_from_storage() {
        let wgsl = fragment().wgsl;

        assert!(wgsl.contains("@group(3) var<storage, read> globalParams_0"), "{wgsl}");
        assert!(wgsl.contains("@group(3) var<storage, read> sun_0"));
        assert!(!wgsl.contains("@group(3) var<uniform>"));
    }

    #[test]
    fn every_global_the_shader_reads_is_bound_by_name() {
        let layout = fragment().layout;

        let names: Vec<_> = layout.bindings.values().map(|b| b.name.as_str()).collect();
        assert_eq!(
            names,
            [LOOSE, "albedo", "albedo_sampler", "layers", "skies", "fog", "points", "raw", "sun"]
        );

        assert_eq!(layout.bindings[&3].count, Some(64));
        assert_eq!(layout.bindings[&4].count, Some(16));
        assert!(matches!(
            layout.bindings[&4].kind,
            BindingKind::Texture {
                dimension: TextureViewDimension::Cube,
                ..
            }
        ));
        assert!(matches!(
            layout.bindings[&5].kind,
            BindingKind::Texture {
                dimension: TextureViewDimension::D3,
                ..
            }
        ));
        assert!(matches!(
            layout.bindings[&6].kind,
            BindingKind::Storage { read_only: true }
        ));
    }

    #[test]
    fn a_loose_number_is_found_at_its_offset() {
        let layout = fragment().layout;

        let Some(Target::Uniform { binding, offset, ty }) = layout.find("tint") else {
            panic!("tint was not found");
        };
        assert_eq!((binding, offset), (0, 16));
        assert_eq!(ty, &FieldType::Vector(Scalar::F32, 4));

        let Some(Target::Uniform { offset, .. }) = layout.find("weights[999]") else {
            panic!("weights[999] was not found");
        };
        assert_eq!(offset, 32 + 999 * 16);

        let Some(Target::Uniform { offset, ty, .. }) = layout.find("lights[3].power") else {
            panic!("lights[3].power was not found");
        };
        assert_eq!(offset, 16032 + 3 * 16 + 12);
        assert_eq!(ty, &FieldType::Scalar(Scalar::F32));

        assert!(matches!(
            layout.find("twist"),
            Some(Target::Uniform {
                ty: FieldType::Matrix(Scalar::F32, 4, 4),
                ..
            })
        ));
        assert!(layout.find("weights[1000]").is_none());
        assert!(layout.find("nothing").is_none());
    }

    #[test]
    fn a_constant_buffer_field_is_found_under_the_buffer() {
        let layout = fragment().layout;

        assert!(matches!(
            layout.find("sun.power"),
            Some(Target::Uniform {
                binding: 8,
                offset: 12,
                ..
            })
        ));
        assert!(matches!(
            layout.find("albedo"),
            Some(Target::Resource { binding: 1, .. })
        ));
    }

    #[test]
    fn the_stages_merge_into_one_layout() {
        let mut layout = reflect(VERTEX_WGSL, VERTEX_JSON, Family::Material)
            .expect("the vertex fixture reflects")
            .layout;

        layout.merge(&fragment().layout).expect("the stages agree");

        assert_eq!(layout.bindings.len(), 9);
        assert_eq!(
            layout.entries(ShaderStages::VERTEX_FRAGMENT).len(),
            layout.bindings.len()
        );
    }

    #[test]
    fn a_binding_to_a_group_nothing_binds_is_refused() {
        let wgsl = "@group(7) @binding(0) var<uniform> stray: vec4<f32>;\n@fragment fn fragment() -> @location(0) vec4<f32> { return stray; }";
        let error = reflect(wgsl, "{}", Family::Material).unwrap_err();
        assert!(error.contains("group 7"), "{error}");
    }

    #[test]
    fn the_rewritten_wgsl_is_valid() {
        use naga::valid::{Capabilities, ValidationFlags, Validator};

        let module = naga::front::wgsl::parse_str(&fragment().wgsl).expect("it parses");

        Validator::new(ValidationFlags::all(), Capabilities::all())
            .validate(&module)
            .expect("naga accepts it");
    }

    #[test]
    fn a_name_loses_the_number_slang_adds() {
        assert_eq!(source_name("albedo_0"), "albedo");
        assert_eq!(source_name("albedo_sampler_12"), "albedo_sampler");
        assert_eq!(source_name("plain"), "plain");
    }
}

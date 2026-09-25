//! What a compiled shader declares, and the bind group layout that follows from it.
//!
//! A shader declares whatever it needs: a thousand numbers, sixty-four textures, sixteen cubemaps,
//! buffers of any size. Nothing here has a table of what is allowed. The layout is read from the
//! shader, so the only limits are the device's.
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

use std::collections::BTreeMap;

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
                    BindingKind::Uniform { size, .. } => BindingType::Buffer {
                        ty: BufferBindingType::Uniform,
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

/// Renumbers the groups of WGSL `slangc` wrote, and reads the shader's own group.
pub fn reflect(wgsl: &str, reflection: &str, family: Family) -> Result<Reflected, String> {
    let wgsl = remap_groups(wgsl, family);

    let module = naga::front::wgsl::parse_str(&wgsl)
        .map_err(|error| format!("the compiled WGSL does not parse: {error}"))?;

    let mut layouter = naga::proc::Layouter::default();
    layouter
        .update(module.to_ctx())
        .map_err(|error| format!("the compiled WGSL could not be laid out: {error}"))?;

    let json: Value = serde_json::from_str(reflection)
        .map_err(|error| format!("slangc's reflection does not parse: {error}"))?;

    let parameters = json
        .get("parameters")
        .and_then(Value::as_array)
        .cloned()
        .unwrap_or_default();

    let group = family.own_group();
    let mut layout = Layout {
        group,
        bindings: BTreeMap::new(),
    };

    for (_, global) in module.global_variables.iter() {
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
                            _ => TextureSampleType::Float {
                                filterable: !*multi,
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

    Ok(Reflected { wgsl, layout })
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

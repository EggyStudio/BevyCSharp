//! The layout a shader's own globals have, read from what it declares.

use super::*;

/// What a shader is compiled for, which decides where its own globals go.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Family {
    /// Drawn on a mesh, in Bevy's pipeline. Its own globals are group three.
    Material,
    /// Drawn on a 2D mesh, in Bevy's 2D pipeline, whose view is group zero, mesh group one and
    /// material group two. Its own globals are group two.
    Material2d,
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
            Family::Material2d => 2,
            Family::Pass | Family::Compute => 0,
        }
    }

    /// Where a group `slangc` wrote goes.
    ///
    /// Space zero is the shader's own. Spaces 100 to 102 are Bevy's groups zero to two, which only a
    /// material has. A 2D material's view and mesh are spaces 100 and 101, groups zero and one, and
    /// what a sprite says is space 102, the material's own group two at bindings past the shader's
    /// own ([`SPRITE_BINDINGS`]). Space 101 is also where the pass and compute modules put the
    /// bridge's inputs, which are group one there.
    pub fn remap(self, group: u32) -> u32 {
        match (self, group) {
            (_, 0) => self.own_group(),
            (Family::Material | Family::Material2d, 100..=102) => group - 100,
            (Family::Pass | Family::Compute, 101) => 1,
            (_, other) => other,
        }
    }

    /// The groups a shader of this family may use, and nothing else binds.
    ///
    /// A compute shader in SPIR-V may use group two as well, for Solari's scene, which
    /// [`reflect_spirv`] checks before asking this.
    pub(super) fn allowed(self) -> &'static [u32] {
        match self {
            Family::Material => &[0, 1, 2, 3],
            Family::Material2d => &[0, 1, 2],
            Family::Pass | Family::Compute => &[0, 1],
        }
    }
}

/// Where `bcs_sprite.slang` declares what a sprite says, in a 2D material's own group: the sprite's
/// numbers, its image and the image's sampler.
///
/// Bound by the bridge from the sprite a material draws rather than from values, so reflection
/// leaves them out of the layout values are set against and notes that the shader reads them.
/// Past any number a shader's own globals reach, which `slangc` numbers from zero.
pub const SPRITE_BINDINGS: std::ops::RangeInclusive<u32> = 100..=102;

/// A scalar a uniform holds.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Scalar {
    F32,
    I32,
    U32,
    Bool,
}

impl Scalar {
    pub(super) fn from_reflection(name: &str) -> Option<Scalar> {
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
    /// A ray scene's top-level acceleration structure, which only SPIR-V can declare.
    AccelerationStructure,
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
            BindingKind::AccelerationStructure => "acceleration structure".to_string(),
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
    /// time, as a compute shader importing `bcs_pass` does, which means it can only run on a
    /// camera.
    pub reads_view: bool,
    /// Whether the shader was compiled to SPIR-V and is handed over untouched. Its blocks of numbers
    /// are then uniform buffers as Slang declared them, since nothing rewrites the declaration the
    /// way [`numbers_in_storage`] rewrites WGSL.
    pub spirv: bool,
    /// Whether the shader reads Solari's scene through `bcs_ray`, in group two, which only a
    /// dispatch in an app running Solari can bind.
    pub traces_scene: bool,
    /// Whether a 2D shader reads the sprite it draws through `bcs_sprite`, at
    /// [`SPRITE_BINDINGS`], which only a material drawing a sprite can bind.
    pub reads_sprite: bool,
    /// The pipeline constants the shader declares, by the names it gives them.
    pub constants: BTreeMap<String, Constant>,
}

/// A constant a pipeline is made with, a Slang `[SpecializationConstant]`, which WGSL calls an
/// `override`.
///
/// Set by name on a material as any value is, and compiled into its pipelines rather than bound,
/// so each value builds pipelines of its own, which the driver can fold the constant into.
#[derive(Clone, PartialEq, Debug)]
pub struct Constant {
    /// What a pipeline names it by, the number Slang gave it, as its `@id` in WGSL.
    pub key: String,
    pub scalar: Scalar,
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
    /// A pipeline constant, which takes one number.
    Constant(&'a Constant),
}

impl Layout {
    /// Finds where a name goes: `roughness`, `lights[3].color`, `sun.power`, `albedo`.
    ///
    /// A loose global is found by its own name, and a field of a `ConstantBuffer` by the buffer's
    /// name and the field's.
    pub fn find(&self, path: &str) -> Option<Target<'_>> {
        if let Some(constant) = self.constants.get(path) {
            return Some(Target::Constant(constant));
        }

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

        names.extend(self.constants.keys().cloned());
        names
    }

    /// Adds another stage's view of the same group, as a material with a vertex and a fragment
    /// shader has.
    ///
    /// One source compiled for two entry points numbers its globals the same way both times, so a
    /// binding the two share has to be the same thing. When it is not, the stages were written in
    /// different files that disagree, and the material could not be bound for both.
    pub fn merge(&mut self, other: &Layout) -> Result<(), String> {
        self.reads_sprite |= other.reads_sprite;

        // A constant one stage kept and another left out is still the material's to set.
        for (name, constant) in &other.constants {
            self.constants.entry(name.clone()).or_insert_with(|| constant.clone());
        }

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
                    // Storage rather than uniform, as `numbers_in_storage` made the shader's
                    // declaration, except in SPIR-V, which nothing rewrites. See there for why.
                    BindingKind::Uniform { size, .. } => BindingType::Buffer {
                        ty: if self.spirv {
                            BufferBindingType::Uniform
                        } else {
                            BufferBindingType::Storage { read_only: true }
                        },
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
                    BindingKind::AccelerationStructure => BindingType::AccelerationStructure {
                        vertex_return: false,
                    },
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

//! What a shader is handed: values by name, and the bind group they become.
//!
//! A material, a pass and a dispatch all hold the same thing, namely whatever the game has said
//! about the names its shader declares. A number or a group of them goes into a uniform buffer at
//! the offset reflection gives, an image into a texture binding or one slot of an array of them, a
//! buffer into a storage binding, and sampler settings into a sampler. Nothing is laid out in
//! advance, because the layout is the shader's (see [`super::reflect`]).
//!
//! Values are kept by name rather than by offset, so a shader that is edited while the game runs,
//! and moves a number or adds one, keeps every value whose name it still declares.

#![cfg(feature = "render")]

use std::collections::{BTreeMap, HashMap};

use bevy::asset::Handle;
use bevy::ecs::resource::Resource;
use bevy::image::Image;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    AddressMode, BindGroup, BindGroupEntry, BindGroupLayout, BindingResource, Buffer,
    BufferDescriptor, BufferInitDescriptor, BufferUsages, CompareFunction, Extent3d, FilterMode,
    MipmapFilterMode, Sampler, SamplerDescriptor, TextureDescriptor, TextureDimension,
    TextureFormat, TextureSampleType, TextureUsages, TextureView, TextureViewDescriptor,
    TextureViewDimension,
};
use bevy::render::renderer::RenderDevice;
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::texture::{FallbackImage, GpuImage};

use super::reflect::{Binding, BindingKind, FieldType, Layout, Scalar, Target};

/// How a sampler reads, as a game describes it.
#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub struct SamplerSettings {
    /// `0` clamp to the edge, `1` repeat, `2` mirror, for U, V and W.
    pub address: [u8; 3],
    /// `true` linear, `false` nearest, for magnifying, minifying and between mip levels.
    pub linear: [bool; 3],
    /// Anisotropic samples, one to turn it off.
    pub anisotropy: u16,
}

impl Default for SamplerSettings {
    /// Linear and repeating, which usually suits a texture a material samples.
    fn default() -> Self {
        Self {
            address: [1, 1, 1],
            linear: [true, true, true],
            anisotropy: 1,
        }
    }
}

/// One value a game gave a name.
#[derive(Clone, PartialEq, Debug)]
pub enum Value {
    /// Numbers, packed tight, `components` of them to an element: one for a scalar, four for a
    /// `float4`, sixteen for a `float4x4`. More than one element is an array.
    Numbers {
        scalar: Scalar,
        components: u32,
        data: Vec<u8>,
    },
    /// Bytes copied as they are, for a struct laid out as the shader lays it out.
    Bytes(Vec<u8>),
    /// An image, whole, or one mip level of it, which building a pyramid a level at a time reads
    /// and writes.
    Image(Handle<Image>, Option<u32>),
    Buffer(Handle<ShaderBuffer>),
    Sampler(SamplerSettings),
}

impl Value {
    fn describe(&self) -> String {
        match self {
            Value::Numbers {
                scalar,
                components,
                data,
            } => {
                let count = data.len() / 4 / (*components).max(1) as usize;
                let one = if *components == 1 {
                    scalar.describe().to_string()
                } else {
                    format!("{}{components}", scalar.describe())
                };
                if count == 1 {
                    one
                } else {
                    format!("{count} of {one}")
                }
            }
            Value::Bytes(bytes) => format!("{} bytes", bytes.len()),
            Value::Image(_, None) => "an image".into(),
            Value::Image(_, Some(mip)) => format!("mip level {mip} of an image"),
            Value::Buffer(_) => "a buffer".into(),
            Value::Sampler(_) => "sampler settings".into(),
        }
    }
}

/// Everything a material, a pass or a dispatch has been handed, by name.
///
/// An image in an array of textures is kept under the array's name and its index, as `layers[3]`.
#[derive(Clone, PartialEq, Debug, Default)]
pub struct Values {
    pub entries: BTreeMap<String, Value>,
}

/// The name an element of an array of resources is kept under.
pub fn element_name(name: &str, index: u32) -> String {
    if index == 0 {
        name.to_string()
    } else {
        format!("{name}[{index}]")
    }
}

/// Splits `layers[3]` into `layers` and `3`, and leaves a plain name as index zero.
fn split_index(key: &str) -> (&str, u32) {
    match key.strip_suffix(']').and_then(|rest| rest.rsplit_once('[')) {
        Some((name, index)) => match index.parse() {
            Ok(index) => (name, index),
            Err(_) => (key, 0),
        },
        None => (key, 0),
    }
}

/// Says whether `value` can go where `name` is, and why not when it cannot.
///
/// What the entry points ask before storing a value, so a wrong name or a wrong type is refused
/// where the call was made rather than found later on the render side.
pub fn check(layout: &Layout, name: &str, value: &Value) -> Result<(), String> {
    let (base, index) = match value {
        Value::Image(..) | Value::Sampler(_) | Value::Buffer(_) => split_index(name),
        _ => (name, 0),
    };

    let Some(target) = layout.find(base) else {
        return Err(format!(
            "the shader declares nothing called {base}. It declares {}.",
            listing(layout)
        ));
    };

    match (target, value) {
        (Target::Uniform { ty, .. }, Value::Numbers { scalar, components, data }) => {
            fits(ty, *scalar, *components, data.len() / 4)
        }
        (Target::Uniform { .. }, Value::Bytes(_)) => Ok(()),
        (Target::Resource { info, .. }, Value::Bytes(_))
            if matches!(info.kind, BindingKind::Uniform { .. }) =>
        {
            Ok(())
        }
        (Target::Resource { info, .. }, value) => {
            let wanted = match (&info.kind, value) {
                (BindingKind::Texture { .. } | BindingKind::StorageTexture { .. }, Value::Image(..))
                | (BindingKind::Storage { .. }, Value::Buffer(_))
                | (BindingKind::Sampler { .. }, Value::Sampler(_)) => true,
                _ => false,
            };

            if !wanted {
                return Err(format!(
                    "{base} is a {}, and {} cannot go there",
                    info.describe(),
                    value.describe()
                ));
            }

            if index >= info.count.unwrap_or(1) {
                return Err(format!(
                    "{base} holds {}, so there is no element {index}",
                    info.count.unwrap_or(1)
                ));
            }

            Ok(())
        }
        (Target::Uniform { ty, .. }, value) => Err(format!(
            "{name} is a {}, and {} cannot go there",
            ty.describe(),
            value.describe()
        )),
    }
}

/// Says whether `count` elements of `components` numbers of `scalar` fit a field of type `ty`.
fn fits(ty: &FieldType, scalar: Scalar, components: u32, numbers: usize) -> Result<(), String> {
    let (element, capacity) = match ty {
        FieldType::Array { element, count, .. } => (element.as_ref(), *count as usize),
        other => (other, 1),
    };

    let Some(field_scalar) = element.scalar() else {
        return Err(format!(
            "a {} is set field by field, or as bytes laid out as the shader lays it out",
            ty.describe()
        ));
    };

    // A bool is four bytes in a uniform, so any of the integer kinds sets one.
    let same_kind = field_scalar == scalar
        || (field_scalar == Scalar::Bool && matches!(scalar, Scalar::U32 | Scalar::I32));

    if !same_kind {
        return Err(format!(
            "it holds {} and was given {}",
            field_scalar.describe(),
            scalar.describe()
        ));
    }

    let Some(wanted) = element.components() else {
        return Err(format!("a {} cannot be set from numbers", ty.describe()));
    };

    if wanted != components {
        return Err(format!(
            "it is a {} and was given {} numbers an element",
            element.describe(),
            components
        ));
    }

    let elements = numbers / components.max(1) as usize;

    if elements > capacity {
        return Err(format!(
            "it holds {capacity} and was given {elements}"
        ));
    }

    Ok(())
}

/// Every name in a layout, for a message.
pub fn listing(layout: &Layout) -> String {
    let names = layout.names();

    if names.is_empty() {
        "nothing".into()
    } else {
        names.join(", ")
    }
}

/// Writes numbers where a field is, element by element at the field's stride.
fn write_numbers(buffer: &mut [u8], offset: u32, ty: &FieldType, components: u32, data: &[u8]) {
    let (element, stride) = match ty {
        FieldType::Array { element, stride, .. } => (element.as_ref(), *stride as usize),
        other => (other, 0),
    };

    let element_bytes = components as usize * 4;

    for (index, chunk) in data.chunks(element_bytes).enumerate() {
        let at = offset as usize + index * stride;

        match element {
            // A row at a time, each row starting on a sixteen-byte boundary.
            FieldType::Matrix(_, rows, columns) => {
                for row in 0..*rows as usize {
                    let from = row * *columns as usize * 4;
                    let to = from + *columns as usize * 4;
                    let place = at + row * 16;

                    if to <= chunk.len() && place + (to - from) <= buffer.len() {
                        buffer[place..place + (to - from)].copy_from_slice(&chunk[from..to]);
                    }
                }
            }
            _ => {
                if at + chunk.len() <= buffer.len() {
                    buffer[at..at + chunk.len()].copy_from_slice(chunk);
                }
            }
        }

        // A single value has no stride, and anything past the first element of one is ignored.
        if stride == 0 {
            break;
        }
    }
}

// -- Turning values into a bind group

/// What a bind group is built from, owned so the entries can borrow it.
#[derive(Default)]
pub struct Packed {
    buffers: Vec<(u32, Buffer)>,
    views: Vec<(u32, Vec<TextureView>)>,
    samplers: Vec<(u32, Vec<Sampler>)>,
    /// The bindings that are arrays, which are bound as arrays even when they hold one.
    arrays: Vec<u32>,
    /// What was set that the shader does not declare, or declares as something else.
    pub problems: Vec<String>,
}

/// Why a bind group could not be built this frame.
#[derive(Debug)]
pub enum PackError {
    /// An image or a buffer has not reached the GPU yet.
    NotReady,
    /// Something the shader writes to was not given, and there is nothing to stand in for it.
    Missing(String),
}

/// What stands in for a texture or a buffer nobody set, made once.
#[derive(Resource)]
pub struct Stand {
    empty_buffer: Buffer,
    depth: TextureView,
    unsigned: TextureView,
    signed: TextureView,
    samplers: std::sync::Mutex<HashMap<(SamplerSettings, bool), Sampler>>,
}

impl Stand {
    pub fn new(device: &RenderDevice) -> Self {
        let texture = |format: TextureFormat| {
            device
                .create_texture(&TextureDescriptor {
                    label: Some("bcs_shader_stand_in"),
                    size: Extent3d {
                        width: 1,
                        height: 1,
                        depth_or_array_layers: 1,
                    },
                    mip_level_count: 1,
                    sample_count: 1,
                    dimension: TextureDimension::D2,
                    format,
                    usage: TextureUsages::TEXTURE_BINDING,
                    view_formats: &[],
                })
                .create_view(&TextureViewDescriptor::default())
        };

        Self {
            empty_buffer: device.create_buffer(&BufferDescriptor {
                label: Some("bcs_shader_empty_buffer"),
                size: 16,
                usage: BufferUsages::STORAGE,
                mapped_at_creation: false,
            }),
            depth: texture(TextureFormat::Depth32Float),
            unsigned: texture(TextureFormat::R32Uint),
            signed: texture(TextureFormat::R32Sint),
            samplers: std::sync::Mutex::new(HashMap::new()),
        }
    }

    /// A sampler reading as `settings` say, made once and kept.
    pub fn sampler(&self, device: &RenderDevice, settings: SamplerSettings, comparison: bool) -> Sampler {
        let mut cache = self.samplers.lock().unwrap_or_else(|poisoned| poisoned.into_inner());

        cache
            .entry((settings, comparison))
            .or_insert_with(|| {
                let address = |mode: u8| match mode {
                    0 => AddressMode::ClampToEdge,
                    2 => AddressMode::MirrorRepeat,
                    _ => AddressMode::Repeat,
                };
                let filter = |linear: bool| if linear { FilterMode::Linear } else { FilterMode::Nearest };

                // Anisotropy with anything but linear filtering is a validation error rather than
                // a request wgpu ignores, so it is dropped where the filters would not allow it.
                let anisotropy = if settings.linear.iter().all(|linear| *linear) {
                    settings.anisotropy.max(1)
                } else {
                    1
                };

                device.create_sampler(&SamplerDescriptor {
                    label: Some("bcs_shader_sampler"),
                    address_mode_u: address(settings.address[0]),
                    address_mode_v: address(settings.address[1]),
                    address_mode_w: address(settings.address[2]),
                    mag_filter: filter(settings.linear[0]),
                    min_filter: filter(settings.linear[1]),
                    mipmap_filter: if settings.linear[2] {
                        MipmapFilterMode::Linear
                    } else {
                        MipmapFilterMode::Nearest
                    },
                    anisotropy_clamp: anisotropy,
                    compare: comparison.then_some(CompareFunction::GreaterEqual),
                    ..Default::default()
                })
            })
            .clone()
    }
}

/// What a GPU image is sampled as, which a binding has to match.
fn view_dimension(image: &GpuImage) -> TextureViewDimension {
    if let Some(dimension) = image
        .texture_view_descriptor
        .as_ref()
        .and_then(|descriptor| descriptor.dimension)
    {
        return dimension;
    }

    match image.texture_descriptor.dimension {
        TextureDimension::D1 => TextureViewDimension::D1,
        TextureDimension::D2 if image.texture_descriptor.size.depth_or_array_layers > 1 => {
            TextureViewDimension::D2Array
        }
        TextureDimension::D2 => TextureViewDimension::D2,
        TextureDimension::D3 => TextureViewDimension::D3,
    }
}

/// Whether a texture of `format` can be bound where `wanted` is sampled.
fn sample_matches(format: TextureFormat, wanted: TextureSampleType) -> bool {
    let Some(actual) = format.sample_type(None, None) else {
        return false;
    };

    match (actual, wanted) {
        // A filterable texture can be read where an unfilterable one is expected, and so can a
        // depth texture, which is how a shader reads last frame's depth as plain numbers.
        (TextureSampleType::Float { .. }, TextureSampleType::Float { filterable: false }) => true,
        (TextureSampleType::Depth, TextureSampleType::Float { filterable: false }) => true,
        (actual, wanted) => actual == wanted,
    }
}

/// An image a camera owns, as a name in a shader resolves to it (see [`super::views`]).
#[derive(Clone, Debug)]
pub struct ViewTexture {
    /// Every mip level, which a shader sampling it reads.
    pub view: TextureView,
    /// The first mip level alone, which a shader writing it is bound to, since a storage binding
    /// holds exactly one level.
    pub level: TextureView,
    pub format: TextureFormat,
}

/// Everything that turns values into GPU resources.
pub struct PackContext<'a> {
    pub device: &'a RenderDevice,
    pub images: &'a RenderAssets<GpuImage>,
    pub buffers: &'a RenderAssets<GpuShaderBuffer>,
    pub fallback: &'a FallbackImage,
    pub stand: &'a Stand,
    /// The images of the camera this runs for, by the names a shader reads them by, where it runs
    /// for one. A name found here wins over a value set under the same name, because the shader was
    /// written against the camera's image.
    pub view: Option<&'a HashMap<String, ViewTexture>>,
}

/// Builds what each binding of `layout` holds from `values`.
///
/// Anything that could not go where it was put is reported in [`Packed::problems`] and left out,
/// so a bad value costs that value rather than the whole material.
pub fn pack(layout: &Layout, values: &Values, context: &PackContext) -> Result<Packed, PackError> {
    let mut packed = Packed::default();

    // Checked once here as well as when each value was set, because the shader may have been
    // edited since, and a value it no longer declares is reported rather than silently dropped.
    for (name, value) in &values.entries {
        if let Err(problem) = check(layout, name, value) {
            packed.problems.push(format!("{name}: {problem}"));
        }
    }

    for (number, binding) in &layout.bindings {
        if binding.count.is_some() {
            packed.arrays.push(*number);
        }

        match &binding.kind {
            BindingKind::Uniform { size, .. } => {
                let mut bytes = vec![0u8; (*size as usize).max(16)];
                fill_uniform(layout, *number, binding, values, &mut bytes);

                let buffer = context.device.create_buffer_with_data(&BufferInitDescriptor {
                    label: Some("bcs_shader_numbers"),
                    contents: &bytes,
                    usage: BufferUsages::STORAGE | BufferUsages::COPY_DST,
                });

                packed.buffers.push((*number, buffer));
            }

            BindingKind::Storage { .. } => {
                let buffer = match values.entries.get(&binding.name) {
                    Some(Value::Buffer(handle)) => match context.buffers.get(handle) {
                        Some(gpu) => gpu.buffer.clone(),
                        None => return Err(PackError::NotReady),
                    },
                    _ => context.stand.empty_buffer.clone(),
                };

                packed.buffers.push((*number, buffer));
            }

            BindingKind::Texture {
                dimension, sample, ..
            } if binding.count.is_none()
                && context.view.is_some_and(|view| view.contains_key(&binding.name)) =>
            {
                let texture = &context.view.expect("checked")[&binding.name];

                let view = if *dimension == TextureViewDimension::D2
                    && sample_matches(texture.format, *sample)
                {
                    texture.view.clone()
                } else {
                    packed.problems.push(format!(
                        "{}: the camera's {:?} image cannot be read as a {:?} {:?} texture",
                        binding.name, texture.format, dimension, sample
                    ));
                    stand_in_texture(context, *dimension, *sample, &binding.name)?
                };

                packed.views.push((*number, vec![view]));
            }

            BindingKind::StorageTexture { format, dimension, .. }
                if binding.count.is_none()
                    && context.view.is_some_and(|view| view.contains_key(&binding.name)) =>
            {
                let texture = &context.view.expect("checked")[&binding.name];

                if texture.format != *format || *dimension != TextureViewDimension::D2 {
                    return Err(PackError::Missing(format!(
                        "{} is written as a {dimension:?} {format:?} image, and the camera's image \
                         of that name is a 2D {:?} one. Declare it with the format the camera's \
                         image was made in.",
                        binding.name, texture.format
                    )));
                }

                packed.views.push((*number, vec![texture.level.clone()]));
            }

            BindingKind::Texture {
                dimension, sample, ..
            } => {
                let count = binding.count.unwrap_or(1);
                let mut views = Vec::with_capacity(count as usize);

                for index in 0..count {
                    let view = match values.entries.get(&element_name(&binding.name, index)) {
                        Some(Value::Image(handle, mip)) => {
                            let Some(image) = context.images.get(handle) else {
                                return Err(PackError::NotReady);
                            };

                            if view_dimension(image) == *dimension
                                && sample_matches(image.texture_descriptor.format, *sample)
                            {
                                match mip {
                                    None => Some(image.texture_view.clone()),
                                    Some(mip) => level_of(image, *mip, &mut packed.problems, &binding.name),
                                }
                            } else {
                                packed.problems.push(format!(
                                    "{}: a {:?} {:?} image cannot be read as a {:?} texture",
                                    element_name(&binding.name, index),
                                    view_dimension(image),
                                    image.texture_descriptor.format,
                                    dimension
                                ));
                                None
                            }
                        }
                        _ => None,
                    };

                    views.push(match view {
                        Some(view) => view,
                        None => stand_in_texture(context, *dimension, *sample, &binding.name)?,
                    });
                }

                packed.views.push((*number, views));
            }

            BindingKind::StorageTexture { format, dimension, .. } => {
                let count = binding.count.unwrap_or(1);
                let mut views = Vec::with_capacity(count as usize);

                for index in 0..count {
                    let key = element_name(&binding.name, index);

                    let Some(Value::Image(handle, mip)) = values.entries.get(&key) else {
                        return Err(PackError::Missing(format!(
                            "{key} is a {format:?} image the shader writes, and none was given"
                        )));
                    };

                    let Some(image) = context.images.get(handle) else {
                        return Err(PackError::NotReady);
                    };

                    let writable = image
                        .texture_descriptor
                        .usage
                        .contains(TextureUsages::STORAGE_BINDING);

                    if image.texture_descriptor.format != *format
                        || view_dimension(image) != *dimension
                        || !writable
                    {
                        return Err(PackError::Missing(format!(
                            "{key} is written as a {dimension:?} {format:?} image, and the image \
                             given is a {:?} {:?} one{}. Make it with Shaders.CreateImage in \
                             that format.",
                            view_dimension(image),
                            image.texture_descriptor.format,
                            if writable { "" } else { " that cannot be written" }
                        )));
                    }

                    // A storage binding holds exactly one level, so an image with several is bound
                    // at the level asked for, or its first.
                    let level = match (mip, image.texture_descriptor.mip_level_count) {
                        (None, 1) => Some(image.texture_view.clone()),
                        (mip, _) => level_of(image, mip.unwrap_or(0), &mut packed.problems, &key),
                    };

                    let Some(level) = level else {
                        return Err(PackError::Missing(format!(
                            "{key} is written at a mip level the image does not have"
                        )));
                    };

                    views.push(level);
                }

                packed.views.push((*number, views));
            }

            BindingKind::Sampler { comparison } => {
                let count = binding.count.unwrap_or(1);
                let mut samplers = Vec::with_capacity(count as usize);

                for index in 0..count {
                    let settings = match values.entries.get(&element_name(&binding.name, index)) {
                        Some(Value::Sampler(settings)) => *settings,
                        _ => SamplerSettings::default(),
                    };

                    samplers.push(context.stand.sampler(context.device, settings, *comparison));
                }

                packed.samplers.push((*number, samplers));
            }
        }
    }

    Ok(packed)
}

/// A view of one mip level of an image, or `None` with a problem noted where it has no such level.
fn level_of(image: &GpuImage, mip: u32, problems: &mut Vec<String>, name: &str) -> Option<TextureView> {
    if mip >= image.texture_descriptor.mip_level_count {
        problems.push(format!(
            "{name}: mip level {mip} was asked for, and the image has {}",
            image.texture_descriptor.mip_level_count
        ));
        return None;
    }

    Some(image.texture.create_view(&TextureViewDescriptor {
        base_mip_level: mip,
        mip_level_count: Some(1),
        ..image
            .texture_view_descriptor
            .clone()
            .unwrap_or_default()
    }))
}

/// What stands in for a texture nobody set.
fn stand_in_texture(
    context: &PackContext,
    dimension: TextureViewDimension,
    sample: TextureSampleType,
    name: &str,
) -> Result<TextureView, PackError> {
    let fallback = context.fallback;

    Ok(match (sample, dimension) {
        (TextureSampleType::Depth, TextureViewDimension::D2) => context.stand.depth.clone(),
        (TextureSampleType::Uint, TextureViewDimension::D2) => context.stand.unsigned.clone(),
        (TextureSampleType::Sint, TextureViewDimension::D2) => context.stand.signed.clone(),
        (TextureSampleType::Float { .. }, TextureViewDimension::D1) => {
            fallback.d1.texture_view.clone()
        }
        (TextureSampleType::Float { .. }, TextureViewDimension::D2) => {
            fallback.d2.texture_view.clone()
        }
        (TextureSampleType::Float { .. }, TextureViewDimension::D2Array) => {
            fallback.d2_array.texture_view.clone()
        }
        (TextureSampleType::Float { .. }, TextureViewDimension::Cube) => {
            fallback.cube.texture_view.clone()
        }
        (TextureSampleType::Float { .. }, TextureViewDimension::CubeArray) => {
            fallback.cube_array.texture_view.clone()
        }
        (TextureSampleType::Float { .. }, TextureViewDimension::D3) => {
            fallback.d3.texture_view.clone()
        }
        _ => {
            return Err(PackError::Missing(format!(
                "{name} is a {dimension:?} {sample:?} texture, which has no stand-in, so it has \
                 to be given"
            )));
        }
    })
}

/// Writes every value that lands in one uniform buffer.
fn fill_uniform(layout: &Layout, number: u32, binding: &Binding, values: &Values, bytes: &mut [u8]) {
    for (name, value) in &values.entries {
        match (layout.find(name), value) {
            (
                Some(Target::Uniform {
                    binding: at,
                    offset,
                    ty,
                }),
                Value::Numbers {
                    scalar,
                    components,
                    data,
                },
            ) if at == number && fits(ty, *scalar, *components, data.len() / 4).is_ok() => {
                write_numbers(bytes, offset, ty, *components, data);
            }

            (Some(Target::Uniform { binding: at, offset, .. }), Value::Bytes(data))
                if at == number =>
            {
                let end = (offset as usize + data.len()).min(bytes.len());
                let length = end.saturating_sub(offset as usize);
                bytes[offset as usize..end].copy_from_slice(&data[..length]);
            }

            // The whole buffer as bytes, which is how a `ConstantBuffer<T>` is set from a struct.
            (Some(Target::Resource { binding: at, .. }), Value::Bytes(data))
                if at == number && matches!(binding.kind, BindingKind::Uniform { .. }) =>
            {
                let length = data.len().min(bytes.len());
                bytes[..length].copy_from_slice(&data[..length]);
            }

            _ => {}
        }
    }
}

impl Packed {
    /// Makes the bind group, against the layout the pipeline was made with.
    pub fn bind_group(&self, device: &RenderDevice, label: &str, layout: &BindGroupLayout) -> BindGroup {
        let views: Vec<(u32, Vec<&bevy::render::render_resource::WgpuTextureView>)> = self
            .views
            .iter()
            .map(|(number, views)| (*number, views.iter().map(|view| &**view).collect()))
            .collect();

        let samplers: Vec<(u32, Vec<&bevy::render::render_resource::WgpuSampler>)> = self
            .samplers
            .iter()
            .map(|(number, samplers)| (*number, samplers.iter().map(|sampler| &**sampler).collect()))
            .collect();

        let mut entries = Vec::new();

        for (number, buffer) in &self.buffers {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: buffer.as_entire_binding(),
            });
        }

        for (number, list) in &views {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: if list.len() == 1 && !self.is_array(*number) {
                    BindingResource::TextureView(list[0])
                } else {
                    BindingResource::TextureViewArray(list)
                },
            });
        }

        for (number, list) in &samplers {
            entries.push(BindGroupEntry {
                binding: *number,
                resource: if list.len() == 1 && !self.is_array(*number) {
                    BindingResource::Sampler(list[0])
                } else {
                    BindingResource::SamplerArray(list)
                },
            });
        }

        device.create_bind_group(label, layout, &entries)
    }

    fn is_array(&self, number: u32) -> bool {
        self.arrays.contains(&number)
    }
}

#[cfg(test)]
mod tests {
    use super::super::reflect::{Family, reflect};
    use super::*;

    fn layout() -> Layout {
        reflect(
            include_str!("fixtures/material_fragment.wgsl"),
            include_str!("fixtures/material_fragment.json"),
            Family::Material,
        )
        .expect("the fixture reflects")
        .layout
    }

    fn floats(values: &[f32]) -> Vec<u8> {
        values.iter().flat_map(|value| value.to_ne_bytes()).collect()
    }

    fn numbers(components: u32, values: &[f32]) -> Value {
        Value::Numbers {
            scalar: Scalar::F32,
            components,
            data: floats(values),
        }
    }

    #[test]
    fn a_value_of_the_right_shape_is_accepted() {
        let layout = layout();

        assert_eq!(check(&layout, "roughness", &numbers(1, &[0.5])), Ok(()));
        assert_eq!(check(&layout, "tint", &numbers(4, &[1.0; 4])), Ok(()));
        assert_eq!(check(&layout, "weights", &numbers(1, &[1.0; 1000])), Ok(()));
        assert_eq!(check(&layout, "twist", &numbers(16, &[0.0; 16])), Ok(()));
        assert_eq!(check(&layout, "lights[2].color", &numbers(3, &[1.0; 3])), Ok(()));
    }

    #[test]
    fn a_wrong_name_says_what_there_is() {
        let error = check(&layout(), "rougness", &numbers(1, &[0.5])).unwrap_err();
        assert!(error.contains("rougness") && error.contains("roughness"), "{error}");
    }

    #[test]
    fn a_wrong_shape_is_refused() {
        let layout = layout();

        assert!(check(&layout, "tint", &numbers(3, &[1.0; 3])).is_err());
        assert!(check(&layout, "weights", &numbers(1, &[1.0; 1001])).is_err());
        assert!(
            check(
                &layout,
                "mode",
                &Value::Numbers {
                    scalar: Scalar::F32,
                    components: 1,
                    data: floats(&[1.0])
                }
            )
            .is_err()
        );
        assert!(check(&layout, "albedo", &numbers(1, &[1.0])).is_err());
    }

    #[test]
    fn an_array_is_written_at_its_stride() {
        let layout = layout();
        let Some(Target::Uniform { offset, ty, .. }) = layout.find("weights") else {
            panic!("weights was not found");
        };

        let mut bytes = vec![0u8; 16200];
        write_numbers(&mut bytes, offset, ty, 1, &floats(&[1.0, 2.0, 3.0]));

        let at = |index: usize| {
            let start = offset as usize + index * 16;
            f32::from_ne_bytes(bytes[start..start + 4].try_into().unwrap())
        };

        assert_eq!((at(0), at(1), at(2)), (1.0, 2.0, 3.0));
    }

    #[test]
    fn a_matrix_is_written_a_padded_row_at_a_time() {
        let ty = FieldType::Matrix(Scalar::F32, 3, 3);
        let mut bytes = vec![0u8; 48];

        write_numbers(&mut bytes, 0, &ty, 9, &floats(&[1., 2., 3., 4., 5., 6., 7., 8., 9.]));

        let at = |byte: usize| f32::from_ne_bytes(bytes[byte..byte + 4].try_into().unwrap());
        assert_eq!((at(0), at(8), at(16), at(32), at(40)), (1.0, 3.0, 4.0, 7.0, 9.0));
    }

    #[test]
    fn an_element_of_an_array_of_textures_is_named_by_index() {
        assert_eq!(element_name("layers", 0), "layers");
        assert_eq!(element_name("layers", 3), "layers[3]");
        assert_eq!(split_index("layers[3]"), ("layers", 3));
        assert_eq!(split_index("albedo"), ("albedo", 0));
    }
}

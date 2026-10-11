//! Vertex streams of a game's own: an attribute Bevy names nothing for, carried on a mesh beside the
//! ones it does and read by a vertex shader of the game's at a location the game chose.
//!
//! Bevy takes such an attribute as a `MeshVertexAttribute`, a name, an id that orders it among the
//! mesh's attributes and a format, which a Rust game declares as a constant. A stream from C# comes
//! with a name, a format and a location, and the registry here makes the attribute for a name the
//! first time a mesh brings it and hands the same one back after, so every mesh carrying a stream of
//! that name carries Bevy's one attribute and a pipeline made for one mesh suits the next. The
//! location travels with the name, since a material's pipeline places each stream the mesh's layout
//! holds where the registry says, and a name that came back in another format or at another
//! location would make two meshes disagree about the one attribute, which is refused.
//!
//! Process-wide, as the programs' table is, because the render side builds pipelines with no main
//! world to ask, and emptied when an app is built so a name means what the running app said. The
//! names themselves are kept for the process, since Bevy's attribute holds its name as a `'static`
//! string, and each distinct name is kept once.

use std::collections::{HashMap, HashSet};
use std::sync::{Mutex, RwLock};

use bevy::mesh::{
    Mesh, MeshVertexAttribute, MeshVertexBufferLayoutRef, VertexAttributeDescriptor, VertexAttributeValues,
    VertexFormat,
};

use crate::interop::status;

/// The first location a stream may take. Bevy's mesh pipeline places its own attributes at zero to
/// five and a skin's at six and seven, and its prepass uses zero to seven.
pub const FIRST_LOCATION: u32 = 8;

/// The last location a stream may take, wgpu's default limit being sixteen vertex attributes.
pub const LAST_LOCATION: u32 = 15;

/// One stream of a mesh, as C# hands it over.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsMeshStream {
    /// The stream's name, UTF-8, `name_len` bytes with no terminator.
    pub name: *const u8,
    pub name_len: i32,
    /// `0` one `uint`, `1` two, `2` one `float`, `3` four, `4` four bytes read as 0 to 1.
    pub format: i32,
    /// Where the vertex shader reads it, from [`FIRST_LOCATION`] to [`LAST_LOCATION`].
    pub location: u32,
    /// The stream's bytes, the format's size times the mesh's vertices.
    pub data: *const u8,
}

/// A stream's format, the five a stream may have.
#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Format {
    UInt,
    UInt2,
    Float,
    Float4,
    Unorm8x4,
}

impl Format {
    pub fn from_code(code: i32) -> Option<Format> {
        Some(match code {
            0 => Format::UInt,
            1 => Format::UInt2,
            2 => Format::Float,
            3 => Format::Float4,
            4 => Format::Unorm8x4,
            _ => return None,
        })
    }

    /// The number C# names it by, the inverse of [`Format::from_code`].
    pub fn code(self) -> i32 {
        match self {
            Format::UInt => 0,
            Format::UInt2 => 1,
            Format::Float => 2,
            Format::Float4 => 3,
            Format::Unorm8x4 => 4,
        }
    }

    /// Bytes a vertex.
    pub fn size(self) -> usize {
        match self {
            Format::UInt | Format::Float | Format::Unorm8x4 => 4,
            Format::UInt2 => 8,
            Format::Float4 => 16,
        }
    }

    fn vertex_format(self) -> VertexFormat {
        match self {
            Format::UInt => VertexFormat::Uint32,
            Format::UInt2 => VertexFormat::Uint32x2,
            Format::Float => VertexFormat::Float32,
            Format::Float4 => VertexFormat::Float32x4,
            Format::Unorm8x4 => VertexFormat::Unorm8x4,
        }
    }

    /// Bevy's values for `bytes`, a whole number of vertices of this format.
    pub fn values(self, bytes: &[u8]) -> VertexAttributeValues {
        let uints = || bytes.chunks_exact(4).map(|four| u32::from_ne_bytes([four[0], four[1], four[2], four[3]]));
        let floats = || uints().map(f32::from_bits);

        match self {
            Format::UInt => VertexAttributeValues::Uint32(uints().collect()),
            Format::UInt2 => {
                let uints: Vec<u32> = uints().collect();
                VertexAttributeValues::Uint32x2(uints.chunks_exact(2).map(|pair| [pair[0], pair[1]]).collect())
            }
            Format::Float => VertexAttributeValues::Float32(floats().collect()),
            Format::Float4 => {
                let floats: Vec<f32> = floats().collect();
                VertexAttributeValues::Float32x4(floats.chunks_exact(4).map(|f| [f[0], f[1], f[2], f[3]]).collect())
            }
            Format::Unorm8x4 => VertexAttributeValues::Unorm8x4(
                bytes.chunks_exact(4).map(|b| [b[0], b[1], b[2], b[3]]).collect(),
            ),
        }
    }
}

/// A name's attribute, its format and its location.
#[derive(Clone, Copy)]
pub struct Stream {
    pub attribute: MeshVertexAttribute,
    pub format: Format,
    pub location: u32,
}

/// Every stream the running app has named, by name.
static REGISTRY: RwLock<Option<HashMap<&'static str, Stream>>> = RwLock::new(None);

/// Every name a stream was given in this process, kept once each for Bevy's `'static` name.
static NAMES: Mutex<Option<HashSet<&'static str>>> = Mutex::new(None);

/// The id a stream's attribute is given, its name's FNV-1a hash in the low 48 bits above a one in
/// bit 48, so it sorts after Bevy's own attributes, numbered from zero to seven, and is the same on
/// every run.
fn id_of(name: &str) -> u64 {
    let hash = name.bytes().fold(0xcbf2_9ce4_8422_2325u64, |hash, byte| {
        (hash ^ u64::from(byte)).wrapping_mul(0x0000_0100_0000_01b3)
    });
    (1 << 48) | (hash & 0xffff_ffff_ffff)
}

/// The name kept for the process, the same `'static` string each time it comes.
fn kept(name: &str) -> &'static str {
    let mut names = NAMES.lock().unwrap_or_else(|poisoned| poisoned.into_inner());
    let names = names.get_or_insert_with(HashSet::new);

    if let Some(kept) = names.get(name) {
        return kept;
    }

    let leaked: &'static str = Box::leak(name.to_owned().into_boxed_str());
    names.insert(leaked);
    leaked
}

/// The stream a name stands for, made the first time the name comes, or
/// [`status::INVALID_STATE`] where it came before in another format or at another location.
pub fn register(name: &str, format: Format, location: u32) -> Result<Stream, i32> {
    let mut registry = REGISTRY.write().unwrap_or_else(|poisoned| poisoned.into_inner());
    let registry = registry.get_or_insert_with(HashMap::new);

    if let Some(stream) = registry.get(name) {
        return if stream.format == format && stream.location == location {
            Ok(*stream)
        } else {
            Err(status::INVALID_STATE)
        };
    }

    let name = kept(name);
    let stream = Stream {
        attribute: MeshVertexAttribute::new(name, id_of(name), format.vertex_format()),
        format,
        location,
    };
    registry.insert(name, stream);
    Ok(stream)
}

/// The stream a name stands for, where the running app has named it.
pub fn lookup(name: &str) -> Option<Stream> {
    REGISTRY.read().ok()?.as_ref()?.get(name).copied()
}

/// Every stream the running app has named.
pub fn all() -> Vec<Stream> {
    REGISTRY
        .read()
        .ok()
        .and_then(|registry| registry.as_ref().map(|registry| registry.values().copied().collect()))
        .unwrap_or_default()
}

/// Forgets every stream, so a new app starts from none.
pub fn forget_all() {
    if let Ok(mut registry) = REGISTRY.write() {
        *registry = None;
    }
}

// -- What a pipeline reads

/// Whether a mesh's vertex layout holds a stream of the game's own.
pub fn held(layout: &MeshVertexBufferLayoutRef) -> bool {
    all().iter().any(|stream| layout.0.contains(stream.attribute.id))
}

/// Puts every stream of the game's own that `layout` holds after `attributes`, each at its
/// location.
pub fn append(layout: &MeshVertexBufferLayoutRef, attributes: &mut Vec<VertexAttributeDescriptor>) {
    for stream in all() {
        if layout.0.contains(stream.attribute.id) {
            attributes.push(stream.attribute.at_shader_location(stream.location));
        }
    }
}

/// What a 3D mesh's own vertex shader is handed where its mesh carries a stream: Bevy's attributes
/// at the locations Bevy's mesh pipeline gives them, where the layout holds them, a skin's among
/// them, and then the streams.
///
/// Bevy's pipeline builds its vertex buffers from its own attributes alone before a material's
/// specialize runs, so a stream reaches the shader only where they are built again with it. wgpu
/// takes an attribute a stage does not read, so each Bevy attribute the mesh has is handed over
/// whether the stage reads it or not.
pub fn main_3d(layout: &MeshVertexBufferLayoutRef) -> Vec<VertexAttributeDescriptor> {
    with_bevys(
        layout,
        &[
            (Mesh::ATTRIBUTE_POSITION, 0),
            (Mesh::ATTRIBUTE_NORMAL, 1),
            (Mesh::ATTRIBUTE_UV_0, 2),
            (Mesh::ATTRIBUTE_UV_1, 3),
            (Mesh::ATTRIBUTE_TANGENT, 4),
            (Mesh::ATTRIBUTE_COLOR, 5),
            (Mesh::ATTRIBUTE_JOINT_INDEX, 6),
            (Mesh::ATTRIBUTE_JOINT_WEIGHT, 7),
        ],
    )
}

/// The same for a 2D mesh, at the locations Bevy's 2D mesh pipeline gives its attributes.
pub fn main_2d(layout: &MeshVertexBufferLayoutRef) -> Vec<VertexAttributeDescriptor> {
    with_bevys(
        layout,
        &[
            (Mesh::ATTRIBUTE_POSITION, 0),
            (Mesh::ATTRIBUTE_NORMAL, 1),
            (Mesh::ATTRIBUTE_UV_0, 2),
            (Mesh::ATTRIBUTE_TANGENT, 3),
            (Mesh::ATTRIBUTE_COLOR, 4),
        ],
    )
}

fn with_bevys(layout: &MeshVertexBufferLayoutRef, bevys: &[(MeshVertexAttribute, u32)]) -> Vec<VertexAttributeDescriptor> {
    let mut attributes: Vec<VertexAttributeDescriptor> = bevys
        .iter()
        .filter(|(attribute, _)| layout.0.contains(attribute.id))
        .map(|(attribute, location)| attribute.at_shader_location(*location))
        .collect();
    append(layout, &mut attributes);
    attributes
}

/// Reads the format and location a name was registered with, for the message a refusal gives.
///
/// Answers [`status::NOT_PRESENT`] where the running app never named it.
///
/// # Safety
/// `name` must be readable for `name_len` bytes, and `format` and `location` writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_stream_of(name: *const u8, name_len: i32, format: *mut i32, location: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if name.is_null() || name_len < 0 || format.is_null() || location.is_null() {
            return status::NULL_ARG;
        }

        let bytes = unsafe { core::slice::from_raw_parts(name, name_len as usize) };
        let Ok(name) = core::str::from_utf8(bytes) else {
            return status::NULL_ARG;
        };

        match lookup(name) {
            Some(stream) => {
                unsafe {
                    format.write(stream.format.code());
                    location.write(stream.location);
                }
                status::OK
            }
            None => status::NOT_PRESENT,
        }
    })
}

#[cfg(test)]
mod tests {
    use super::{Format, register};

    /// A name comes back as the attribute it was first given, with an id above Bevy's own that is
    /// the same every run, and the same name in another format or at another location is refused.
    #[test]
    fn a_name_keeps_its_attribute_and_refuses_another_shape() {
        let first = register("TestStreamKept", Format::UInt, 8).expect("a new name registers");
        let again = register("TestStreamKept", Format::UInt, 8).expect("the same shape comes back");

        assert_eq!(first.attribute.id, again.attribute.id);
        assert_eq!(first.attribute.id, super::id_of("TestStreamKept").into_attribute_id());
        assert!(super::id_of("TestStreamKept") >= 1 << 48);
        assert!(register("TestStreamKept", Format::Float, 8).is_err());
        assert!(register("TestStreamKept", Format::UInt, 9).is_err());
    }

    /// Four bytes a vertex in each of the four-byte formats, read as Bevy's values of that format.
    #[test]
    fn bytes_become_the_formats_values() {
        let bytes: Vec<u8> = [7u32, 9].iter().flat_map(|value| value.to_ne_bytes()).collect();

        assert!(matches!(Format::UInt.values(&bytes), bevy::mesh::VertexAttributeValues::Uint32(v) if v == [7, 9]));
        assert!(matches!(Format::UInt2.values(&bytes), bevy::mesh::VertexAttributeValues::Uint32x2(v) if v == [[7, 9]]));
        assert!(matches!(Format::Unorm8x4.values(&bytes[..4]), bevy::mesh::VertexAttributeValues::Unorm8x4(v) if v.len() == 1));
    }

    trait IntoAttributeId {
        fn into_attribute_id(self) -> bevy::mesh::MeshVertexAttributeId;
    }

    impl IntoAttributeId for u64 {
        fn into_attribute_id(self) -> bevy::mesh::MeshVertexAttributeId {
            bevy::mesh::MeshVertexAttribute::new("", self, bevy::mesh::VertexFormat::Uint32).id
        }
    }
}

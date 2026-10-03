//! Building the meshes and materials a picture is made of, and attaching them to entities.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use super::image_handle;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Builds a mesh primitive and returns an asset handle for it.
///
/// `kind` selects the shape and decides what the three dimensions mean:
///
/// | kind      | a       | b      | c     |
/// |-----------|---------|--------|-------|
/// | `Cuboid`  | width   | height | depth |
/// | `Sphere`  | radius  |        |       |
/// | `Plane`   | width   | depth  |       |
/// | `Capsule` | radius  | length |       |
///
/// # Safety
/// `kind` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_create(
    kind: *const core::ffi::c_char,
    a: f32,
    b: f32,
    c: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, a, b, c);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::Mesh;

            let Some(kind) = (unsafe { crate::interop::cstr_to_string(kind) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(mesh) = primitive(&kind, a, b, c) else {
                    return status::NO_COMPONENT;
                };

                let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                    return status::UNSUPPORTED;
                };
                let handle = meshes.add(mesh).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// One of Bevy's primitives as a mesh, by its name and up to three measures, or nothing for a name
/// that is none of them.
#[cfg(feature = "render")]
fn primitive(kind: &str, a: f32, b: f32, c: f32) -> Option<bevy::mesh::Mesh> {
    use bevy::math::primitives::{
        Annulus, Capsule3d, Circle, Cone, ConicalFrustum, Cuboid, Cylinder, Plane3d, Rectangle,
        Sphere, Tetrahedron, Torus, Triangle3d,
    };
    use bevy::mesh::{Mesh, Meshable};

    let mesh: Mesh = match kind {
        "Cuboid" => Cuboid::new(a, b, c).mesh().into(),
        "Sphere" => Sphere::new(a).mesh().into(),
        "Plane" => Plane3d::default()
            .mesh()
            .size(a, b)
            .into(),
        "Capsule" => Capsule3d::new(a, b).mesh().into(),
        "Cylinder" => Cylinder::new(a, b).mesh().into(),
        "Cone" => Cone::new(a, b).mesh().into(),
        "ConicalFrustum" => ConicalFrustum {
            radius_top: a,
            radius_bottom: b,
            height: c,
        }
        .mesh()
        .into(),
        "Torus" => Torus::new(a, b).mesh().into(),
        "Circle" => Circle::new(a).mesh().into(),
        "Annulus" => Annulus::new(a, b).mesh().into(),
        "Rectangle" => Rectangle::new(a, b).mesh().into(),
        // The two shapes made of points rather than measures are the default ones, a
        // unit across, scaled by the first number.
        "Triangle" => {
            let [first, second, third] = Triangle3d::default().vertices;
            Triangle3d::new(first * a, second * a, third * a).mesh().into()
        }
        "Tetrahedron" => {
            let unit = Tetrahedron::default();
            Tetrahedron::new(
                unit.vertices[0] * a,
                unit.vertices[1] * a,
                unit.vertices[2] * a,
                unit.vertices[3] * a,
            )
            .mesh()
            .into()
        }
        _ => return None,
    };

    Some(mesh)
}

/// Builds a primitive again with new measures in place of the mesh a handle names, so everything
/// drawn with it changes without being pointed at a new one.
///
/// Reports [`status::NO_COMPONENT`] for a shape that is not one of Bevy's primitives, and
/// [`status::INVALID_STATE`] for a handle to something other than a mesh.
///
/// # Safety
/// `kind` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_rebuild(
    handle: i32,
    kind: *const core::ffi::c_char,
    a: f32,
    b: f32,
    c: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, kind, a, b, c);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::Mesh;

            let Some(kind) = (unsafe { crate::interop::cstr_to_string(kind) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<Mesh>() else {
                    return status::INVALID_STATE;
                };
                let Some(mesh) = primitive(&kind, a, b, c) else {
                    return status::NO_COMPONENT;
                };

                let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                    return status::UNSUPPORTED;
                };
                // Written through the guard, which is what marks the mesh changed for the renderer.
                let Some(mut held) = meshes.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                *held = mesh;
                status::OK
            })
        }
    })
}

/// What a mesh holds, without its vertices.
///
/// The counts a tool shows and the attributes a shader can rely on, read in one call rather than
/// by copying every vertex across to count them.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsMeshInfo {
    /// How many vertices.
    pub vertices: u32,
    /// How many indices, or `0` for a mesh drawn without them.
    pub indices: u32,
    /// `16` or `32` for the width of an index, or `0` for none.
    pub index_bits: u32,
    /// `0` triangles, `1` a triangle strip, `2` lines, `3` a line strip, `4` points.
    pub topology: u32,
    /// Which attributes it has, one bit each: `1` normals, `2` tangents, `4` UVs, `8` a second UV
    /// set, `16` vertex colors, `32` joint indices, `64` joint weights.
    pub attributes: u32,
    /// The corner of its bounds with the smallest coordinates.
    pub min: [f32; 3],
    /// The corner with the largest.
    pub max: [f32; 3],
}

/// Reads what a mesh holds: its counts, its attributes and its bounds.
///
/// # Safety
/// `out` must be writable for one [`BcsMeshInfo`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_mesh_info(handle: i32, out: *mut BcsMeshInfo) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        // Every profile has meshes, so this is not behind the renderer as the rest of the file is.
        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::{Indices, Mesh, PrimitiveTopology};

            let Some(handle) = crate::assets::clone_handle(world, handle) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(meshes) = world.get_resource::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };
            // A mesh still loading, or one kept only in the render world, has nothing to read here.
            let Some(mesh) = meshes.get(&handle) else {
                return status::NOT_PRESENT;
            };

            let mut info = BcsMeshInfo {
                vertices: mesh.count_vertices() as u32,
                ..Default::default()
            };

            (info.indices, info.index_bits) = match mesh.indices() {
                Some(Indices::U16(indices)) => (indices.len() as u32, 16),
                Some(Indices::U32(indices)) => (indices.len() as u32, 32),
                None => (0, 0),
            };

            info.topology = match mesh.primitive_topology() {
                PrimitiveTopology::TriangleList => 0,
                PrimitiveTopology::TriangleStrip => 1,
                PrimitiveTopology::LineList => 2,
                PrimitiveTopology::LineStrip => 3,
                PrimitiveTopology::PointList => 4,
            };

            let attributes = [
                (Mesh::ATTRIBUTE_NORMAL, 1),
                (Mesh::ATTRIBUTE_TANGENT, 2),
                (Mesh::ATTRIBUTE_UV_0, 4),
                (Mesh::ATTRIBUTE_UV_1, 8),
                (Mesh::ATTRIBUTE_COLOR, 16),
                (Mesh::ATTRIBUTE_JOINT_INDEX, 32),
                (Mesh::ATTRIBUTE_JOINT_WEIGHT, 64),
            ];
            for (attribute, bit) in attributes {
                if mesh.contains_attribute(attribute) {
                    info.attributes |= bit;
                }
            }

            let positions = mesh.attribute(Mesh::ATTRIBUTE_POSITION).and_then(|p| p.as_float3());
            if let Some(positions) = positions {
                let mut min = [f32::MAX; 3];
                let mut max = [f32::MIN; 3];
                for position in positions {
                    for axis in 0..3 {
                        min[axis] = min[axis].min(position[axis]);
                        max[axis] = max[axis].max(position[axis]);
                    }
                }
                if !positions.is_empty() {
                    (info.min, info.max) = (min, max);
                }
            }

            unsafe { out.write(info) };
            status::OK
        })
    })
}

/// A mesh described vertex by vertex.
///
/// Every array but `positions` may be null. Positions and normals are three floats a vertex, UVs
/// two and colors four, in linear RGBA.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsMeshData {
    pub positions: *const f32,
    pub vertex_count: i32,
    pub normals: *const f32,
    pub uvs: *const f32,
    pub colors: *const f32,
    /// Indices into the vertices, or null to take them in order.
    pub indices: *const u32,
    pub index_count: i32,
    /// `0` triangles, `1` lines, `2` points, `3` a line strip, `4` a triangle strip.
    pub topology: i32,
}

/// Builds a mesh from vertices and answers its asset key.
///
/// A triangle mesh given no normals has them worked out, smooth where it is indexed and flat
/// where it is not, because every lit material and every shader reading a normal would otherwise
/// read zeros. Built in any profile, since a mesh is data until something draws it.
///
/// Returns [`status::NULL_ARG`] where a count is negative, a pointer the count needs is null, or
/// an index names no vertex.
///
/// # Safety
/// `data` must point to a readable [`BcsMeshData`] whose arrays hold what their counts say.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_create_from(data: *const BcsMeshData) -> i32 {
    crate::interop::guard(|| {
        use bevy::asset::{Assets, RenderAssetUsages};
        use bevy::mesh::{Indices, Mesh, PrimitiveTopology};

        if data.is_null() {
            return status::NULL_ARG;
        }

        let data = unsafe { *data };

        if data.vertex_count <= 0
            || data.positions.is_null()
            || data.index_count < 0
            || (data.indices.is_null() && data.index_count > 0)
        {
            return status::NULL_ARG;
        }

        let count = data.vertex_count as usize;

        let floats = |pointer: *const f32, width: usize| -> Option<&[f32]> {
            (!pointer.is_null())
                .then(|| unsafe { core::slice::from_raw_parts(pointer, count * width) })
        };

        let topology = match data.topology {
            1 => PrimitiveTopology::LineList,
            2 => PrimitiveTopology::PointList,
            3 => PrimitiveTopology::LineStrip,
            4 => PrimitiveTopology::TriangleStrip,
            _ => PrimitiveTopology::TriangleList,
        };

        let positions = floats(data.positions, 3)
            .map(|all| all.chunks_exact(3).map(|p| [p[0], p[1], p[2]]).collect::<Vec<_>>())
            .unwrap_or_default();

        let mut mesh = Mesh::new(topology, RenderAssetUsages::default())
            .with_inserted_attribute(Mesh::ATTRIBUTE_POSITION, positions);

        if let Some(normals) = floats(data.normals, 3) {
            let normals: Vec<[f32; 3]> =
                normals.chunks_exact(3).map(|n| [n[0], n[1], n[2]]).collect();
            mesh.insert_attribute(Mesh::ATTRIBUTE_NORMAL, normals);
        }

        if let Some(uvs) = floats(data.uvs, 2) {
            let uvs: Vec<[f32; 2]> = uvs.chunks_exact(2).map(|t| [t[0], t[1]]).collect();
            mesh.insert_attribute(Mesh::ATTRIBUTE_UV_0, uvs);
        }

        if let Some(colors) = floats(data.colors, 4) {
            let colors: Vec<[f32; 4]> =
                colors.chunks_exact(4).map(|c| [c[0], c[1], c[2], c[3]]).collect();
            mesh.insert_attribute(Mesh::ATTRIBUTE_COLOR, colors);
        }

        if data.index_count > 0 {
            let indices =
                unsafe { core::slice::from_raw_parts(data.indices, data.index_count as usize) };

            if indices.iter().any(|index| *index as usize >= count) {
                return status::NULL_ARG;
            }

            mesh.insert_indices(Indices::U32(indices.to_vec()));
        }

        if data.normals.is_null() && topology == PrimitiveTopology::TriangleList {
            // Smooth normals need indices, and flat ones need there to be none, so the mesh is
            // asked for whichever its shape allows.
            if mesh.indices().is_some() {
                mesh.compute_smooth_normals();
            } else {
                mesh.compute_flat_normals();
            }
        }

        crate::state::with_world(|world| {
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };

            let handle = meshes.add(mesh).untyped();
            crate::assets::insert_handle(world, handle)
        })
    })
}

/// Copies a mesh's triangles out: its positions, three floats each, and its indices, three a
/// triangle.
///
/// For whatever needs the shape rather than the picture, a physics engine building a collision
/// shape from a level above all. `counts` receives the number of positions and of indices first,
/// so a call with null buffers learns the sizes and a second copies. A mesh with no indices is
/// answered with its vertices taken in order.
///
/// Returns [`status::NOT_PRESENT`] while the mesh is still loading, [`status::NULL_ARG`] for one
/// that is not a list of triangles or has no positions, and [`status::BUFFER_TOO_SMALL`] where a
/// buffer is given that cannot hold it.
///
/// # Safety
/// `counts` must be writable for two integers. `positions` must be null or writable for
/// `position_capacity` floats, and `indices` null or writable for `index_capacity` integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_mesh_triangles(
    mesh: i32,
    positions: *mut f32,
    position_capacity: i32,
    indices: *mut u32,
    index_capacity: i32,
    counts: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (mesh, positions, position_capacity, indices, index_capacity, counts);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::{Indices, Mesh, PrimitiveTopology, VertexAttributeValues};

            if counts.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|handle| handle.try_typed::<Mesh>().ok())
                else {
                    return status::NO_COMPONENT;
                };

                let Some(found) = world.resource::<Assets<Mesh>>().get(&handle) else {
                    return status::NOT_PRESENT;
                };

                if found.primitive_topology() != PrimitiveTopology::TriangleList {
                    return status::NULL_ARG;
                }

                let Some(VertexAttributeValues::Float32x3(corners)) = found.attribute(Mesh::ATTRIBUTE_POSITION) else {
                    return status::NULL_ARG;
                };

                let order: Vec<u32> = match found.indices() {
                    Some(Indices::U16(list)) => list.iter().map(|index| *index as u32).collect(),
                    Some(Indices::U32(list)) => list.clone(),
                    None => (0..corners.len() as u32).collect(),
                };

                unsafe {
                    counts.write(corners.len() as i32);
                    counts.add(1).write(order.len() as i32);
                }

                if positions.is_null() && indices.is_null() {
                    return status::OK;
                }

                if (position_capacity as usize) < corners.len() * 3 || (index_capacity as usize) < order.len() {
                    return status::BUFFER_TOO_SMALL;
                }

                unsafe {
                    core::ptr::copy_nonoverlapping(corners.as_ptr() as *const f32, positions, corners.len() * 3);
                    core::ptr::copy_nonoverlapping(order.as_ptr(), indices, order.len());
                }

                status::OK
            })
        }
    })
}

/// Builds an empty image sized for a camera to draw into.
///
/// The usages separate a texture that can be drawn into and copied out of from one that can only be
/// sampled. Without `RENDER_ATTACHMENT` a camera cannot target it, and without `COPY_SRC` a capture
/// finds nothing to read back.
#[cfg(feature = "render")]
pub(crate) fn target_image(width: u32, height: u32) -> bevy::image::Image {
    target_image_in(width, height, false)
}

/// The same, in half floats where `float` is set, which keeps what a camera draws brighter than
/// white as it is rather than clamping it to one, for a reflection or a picture a shader reads on.
#[cfg(feature = "render")]
pub(crate) fn target_image_in(width: u32, height: u32, float: bool) -> bevy::image::Image {
    target_image_layers(width, height, float, 1)
}

/// The same with `layers` layers, which cameras draw into one at a time (see [`super::layers`]).
///
/// Six square layers are viewed as a cube, as a material's or a probe's cube slot reads them, and
/// any other count as an array.
#[cfg(feature = "render")]
pub(crate) fn target_image_layers(width: u32, height: u32, float: bool, layers: u32) -> bevy::image::Image {
    use bevy::asset::RenderAssetUsages;
    use bevy::image::Image;
    use bevy::render::render_resource::{Extent3d, TextureDimension, TextureFormat, TextureUsages};

    let size = Extent3d {
        width: width.max(1),
        height: height.max(1),
        depth_or_array_layers: layers.max(1),
    };

    // Opaque black rather than transparent, because a picture of a scene with nothing in front of
    // the camera should look like an empty scene rather than like a failure. The format is named
    // outright, because Bevy deprecated its default in favor of asking the view, and a target
    // created before there is a view to ask has to choose one.
    // Half-float one, for the opaque black.
    const HALF_ONE: [u8; 2] = 0x3C00u16.to_le_bytes();

    let (black, format): (&[u8], TextureFormat) = if float {
        (&[0, 0, 0, 0, 0, 0, HALF_ONE[0], HALF_ONE[1]], TextureFormat::Rgba16Float)
    } else {
        (&[0, 0, 0, 255], TextureFormat::Rgba8UnormSrgb)
    };

    let mut image = Image::new_fill(size, TextureDimension::D2, black, format, RenderAssetUsages::default());

    image.texture_descriptor.usage =
        TextureUsages::COPY_SRC | TextureUsages::RENDER_ATTACHMENT | TextureUsages::TEXTURE_BINDING;

    if layers > 1 {
        use bevy::render::render_resource::{TextureViewDescriptor, TextureViewDimension};

        // Written by copies from the cameras' companions rather than drawn into.
        image.texture_descriptor.usage |= TextureUsages::COPY_DST;
        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(if layers == 6 && width == height {
                TextureViewDimension::Cube
            } else {
                TextureViewDimension::D2Array
            }),
            ..Default::default()
        });
    }

    image
}

/// Creates an image a camera can draw into, and returns an asset handle for it.
///
/// The other half of [`crate::render::scene::bcs_render_set_camera_target`], and what a portal, a
/// security monitor or a second viewport is built from. A camera draws into this image, and a
/// material sampling the same handle shows what that camera sees.
///
/// The image is empty until something draws into it. Nothing loads, so the handle is usable on the
/// frame it is returned. `format` is `0` for eight-bit sRGB and `1` for half floats, which keep
/// light brighter than white as it was drawn. `layers` above one makes an image cameras draw into a
/// layer at a time, viewed as a cube where there are six square layers and as an array otherwise.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_create_target(width: u32, height: u32, format: i32, layers: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, format, layers);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::image::Image;

            if !(0..=1).contains(&format) || layers == 0 {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let image = target_image_layers(width, height, format == 1, layers);

                let Some(mut images) = world.get_resource_mut::<Assets<Image>>() else {
                    return status::UNSUPPORTED;
                };

                let handle = images.add(image).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// Builds a physically based material and returns an asset handle for it.
///
/// Color components are linear sRGB in the range zero to one. `metallic` and `roughness` follow
/// the usual convention: zero metallic for a dielectric, roughness near zero for a mirror.
///
/// A texture is named by the asset key of an already-loaded image, which is how the two halves of
/// the asset surface meet: `bcs_asset_load` produces the key, this consumes it. The image does
/// not have to have finished loading; the material picks it up when it arrives, which is the
/// behavior a Bevy handle has anyway.
///
/// # Safety
/// `config` must point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create(config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let material = match standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<StandardMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// A standard material built from the settings a material is made with, its textures resolved
/// from the keys the program holds, or the status a key that names no image gives.
#[cfg(feature = "render")]
fn standard_material(
    world: &mut bevy::ecs::world::World,
    config: &BcsMaterialConfig,
) -> Result<bevy::pbr::StandardMaterial, i32> {
    use bevy::asset::Handle;
    use bevy::color::{Color, LinearRgba};
    use bevy::image::Image;
    use bevy::material::AlphaMode;
    use bevy::pbr::StandardMaterial;

    // Resolved before the material is built, because each one needs the world and
    // building it needs the world back to insert the result.
    let mut textures: [Option<Handle<Image>>; 5] = Default::default();
    let keys = [
        config.base_color_texture,
        config.normal_map,
        config.metallic_roughness_texture,
        config.emissive_texture,
        config.occlusion_texture,
    ];

    for (slot, key) in keys.iter().enumerate() {
        match image_handle(world, *key) {
            Ok(handle) => textures[slot] = handle,
            Err(status) => return Err(status),
        }
    }

    let [
        base_color_texture,
        normal_map_texture,
        metallic_roughness_texture,
        emissive_texture,
        occlusion_texture,
    ] = textures;

    let alpha_mode = match config.alpha_mode {
        1 => AlphaMode::Mask(config.alpha_cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        _ => AlphaMode::Opaque,
    };

    let material = StandardMaterial {
        base_color: Color::linear_rgba(
            config.base_color[0],
            config.base_color[1],
            config.base_color[2],
            config.base_color[3],
        ),
        metallic: config.metallic,
        perceptual_roughness: config.roughness,
        emissive: LinearRgba::new(
            config.emissive[0],
            config.emissive[1],
            config.emissive[2],
            config.emissive[3],
        ),
        alpha_mode,
        double_sided: config.double_sided != 0,
        // A double-sided material still culls unless the back faces are kept, which
        // is a separate field and the one people actually mean.
        cull_mode: if config.double_sided != 0 {
            None
        } else {
            Some(bevy::render::render_resource::Face::Back)
        },
        unlit: config.unlit != 0,
        // Scale first, then rotate, then shift, which is the order that makes a
        // scale of eight mean "eight tiles" whatever the other two are set to.
        uv_transform: bevy::math::Affine2::from_scale_angle_translation(
            bevy::math::Vec2::new(config.uv_scale[0], config.uv_scale[1]),
            config.uv_rotation,
            bevy::math::Vec2::new(config.uv_offset[0], config.uv_offset[1]),
        ),
        base_color_texture,
        normal_map_texture,
        metallic_roughness_texture,
        emissive_texture,
        occlusion_texture,
        ..Default::default()
    };

    Ok(material)
}

/// Writes settings over an existing standard material, so everything drawn with it changes.
///
/// The settings [`bcs_material_create`] takes, laid over the asset in place rather than made into
/// a new one, which is how a tool edits a material that a scene or a file already shares. Reports
/// [`status::INVALID_STATE`] for a handle to something other than a standard material.
///
/// # Safety
/// `config` must point at one readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_material_write(handle: i32, config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<StandardMaterial>() else {
                    return status::INVALID_STATE;
                };
                let material = match standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<StandardMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                // Written through the asset's guard, since a write through it marks the asset
                // changed for the renderer, and a guard taken and dropped without one marks nothing.
                let Some(mut held) = materials.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                *held = material;
                status::OK
            })
        }
    })
}

/// Reads a standard material's settings back, in the form [`bcs_material_create`] takes them.
///
/// The inverse of making one, so a tool can show what a material is, whether code built it or a
/// glTF file brought it, and a scene can write it down as how to make it again. Each texture
/// comes back as the key the program already holds for it, or a new one.
///
/// # Safety
/// `out` must be writable for one [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_material_read(handle: i32, out: *mut BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::{Assets, Handle};
            use bevy::image::Image;
            use bevy::material::AlphaMode;
            use bevy::pbr::StandardMaterial;

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<StandardMaterial>() else {
                    return status::INVALID_STATE;
                };
                let Some(material) = world
                    .get_resource::<Assets<StandardMaterial>>()
                    .and_then(|materials| materials.get(&handle))
                    .cloned()
                else {
                    return status::NOT_PRESENT;
                };

                let mut key = |texture: Option<Handle<Image>>| match texture {
                    Some(texture) => crate::assets::key_for(world, texture.untyped()),
                    None => -1,
                };

                let (alpha_mode, alpha_cutoff) = match material.alpha_mode {
                    AlphaMode::Mask(cutoff) => (1, cutoff),
                    AlphaMode::Blend => (2, 0.5),
                    AlphaMode::Add => (3, 0.5),
                    AlphaMode::Multiply => (4, 0.5),
                    AlphaMode::Premultiplied => (5, 0.5),
                    _ => (0, 0.5),
                };

                let base = material.base_color.to_linear();
                let (scale, rotation, offset) = material.uv_transform.to_scale_angle_translation();

                let config = BcsMaterialConfig {
                    base_color: [base.red, base.green, base.blue, base.alpha],
                    metallic: material.metallic,
                    roughness: material.perceptual_roughness,
                    emissive: [
                        material.emissive.red,
                        material.emissive.green,
                        material.emissive.blue,
                        material.emissive.alpha,
                    ],
                    alpha_mode,
                    alpha_cutoff,
                    double_sided: i32::from(material.double_sided),
                    unlit: i32::from(material.unlit),
                    base_color_texture: key(material.base_color_texture),
                    normal_map: key(material.normal_map_texture),
                    metallic_roughness_texture: key(material.metallic_roughness_texture),
                    emissive_texture: key(material.emissive_texture),
                    occlusion_texture: key(material.occlusion_texture),
                    uv_scale: [scale.x, scale.y],
                    uv_rotation: rotation,
                    uv_offset: [offset.x, offset.y],
                };

                unsafe { out.write(config) };
                status::OK
            })
        }
    })
}

/// Attaches an asset to an entity through one of the components that carry a handle.
///
/// `component` is `Mesh3d` or `MeshMaterial3d`. These go through Bevy's own insert rather than a
/// byte copy, both because the handle has to be retyped and because inserting them pulls in the
/// components Bevy requires alongside, such as `Transform` and `Visibility`.
///
/// # Safety
/// `component` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_insert_asset(
    entity: u64,
    component: *const core::ffi::c_char,
    handle: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, component, handle);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::mesh::{Mesh, Mesh3d};
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};

            let Some(component) = (unsafe { crate::interop::cstr_to_string(component) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(untyped) = crate::assets::clone_handle(world, handle) else {
                    return status::NO_ENTITY;
                };

                let entity = Entity::from_bits(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                // try_typed rather than typed, because asking for a mesh component with a material
                // handle is a mistake the managed side can make, and it should be an error rather
                // than a panic crossing the boundary.
                match component.as_str() {
                    "Mesh3d" => match untyped.try_typed::<Mesh>() {
                        Ok(handle) => {
                            entity_mut.insert(Mesh3d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    #[cfg(feature = "meshlet")]
                    "MeshletMesh3d" => match untyped
                        .try_typed::<bevy::pbr::experimental::meshlet::MeshletMesh>()
                    {
                        Ok(handle) => {
                            crate::render::meshlets::attach(entity_mut.into_world_mut(), entity, handle);
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    "MeshMaterial3d" => match untyped.clone().try_typed::<StandardMaterial>() {
                        Ok(handle) => {
                            entity_mut.insert(MeshMaterial3d(handle));
                            status::OK
                        }

                        // Not the standard one, so it may be a material drawn by a shader the
                        // caller wrote. The asset table is untyped, so which it is can only be
                        // found by asking.
                        Err(_) => {
                            if crate::render::shaders::attach(&mut entity_mut, &untyped) {
                                status::OK
                            } else {
                                status::NO_COMPONENT
                            }
                        }
                    },
                    _ => status::NO_COMPONENT,
                }
            })
        }
    })
}

/// Writes where an entity's mesh or material was loaded from, and returns its length in bytes.
///
/// `which` is `0` for the mesh and `1` for the material. The answer is the asset path, which an
/// editor can show and a person can point at a different file. An asset built in memory rather than
/// loaded has no path and answers an empty string, which is the honest answer rather than a made-up
/// name.
///
/// The usual text convention: pass null with a capacity of zero to learn the length, then call
/// again with a buffer that size.
///
/// Returns [`status::NO_COMPONENT`] where the entity carries no such thing.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_asset_path(
    entity: u64,
    which: i32,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, which, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::AssetServer;
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};
            use bevy::render::mesh::Mesh3d;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                let id = match which {
                    0 => world.get::<Mesh3d>(entity).map(|mesh| mesh.0.id().untyped()),
                    1 => world
                        .get::<MeshMaterial3d<StandardMaterial>>(entity)
                        .map(|material| material.0.id().untyped()),
                    _ => return status::NULL_ARG,
                };

                let Some(id) = id else {
                    return status::NO_COMPONENT;
                };

                let Some(server) = world.get_resource::<AssetServer>() else {
                    return status::UNSUPPORTED;
                };

                // An asset made in memory has no path, which is most of what this project's own
                // meshes and materials are, so an empty answer is ordinary rather than a failure.
                let path = server
                    .get_path(id)
                    .map(|path| path.to_string())
                    .unwrap_or_default();

                unsafe { crate::interop::write_text(&path, out, capacity) }
            })
        }
    })
}

// -- Reshaping images

/// Images asked to become array or 3D textures, waiting for their pixels to arrive.
///
/// The same wait a cubemap has, because an image loads as one tall picture, and how it divides into
/// layers or slices can only be applied once it has been decoded.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct PendingReshapes(Vec<PendingReshape>);

/// One image waiting to be reshaped.
#[cfg(feature = "render")]
pub struct PendingReshape {
    image: bevy::asset::Handle<bevy::image::Image>,
    /// How many layers or slices it is stacked into.
    count: u32,
    /// A 3D texture rather than an array of 2D ones.
    volume: bool,
}

/// Reshapes each loaded image on the list, and forgets it.
///
/// Both shapes read the picture as `count` equal parts stacked from top to bottom, which is the
/// order their bytes are already in, so nothing is copied.
#[cfg(feature = "render")]
pub fn reshape_images(
    mut pending: bevy::ecs::system::ResMut<PendingReshapes>,
    mut images: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::image::Image>>,
) {
    use bevy::render::render_resource::{
        Extent3d, TextureDimension, TextureViewDescriptor, TextureViewDimension,
    };

    pending.0.retain(|waiting| {
        let Some(mut image) = images.get_mut(&waiting.image) else {
            return true;
        };

        let size = image.texture_descriptor.size;

        // Asked twice, which is not worth refusing.
        let already = if waiting.volume {
            image.texture_descriptor.dimension == TextureDimension::D3
        } else {
            size.depth_or_array_layers == waiting.count && waiting.count > 1
        };

        if !already {
            if size.depth_or_array_layers != 1 || !size.height.is_multiple_of(waiting.count) {
                bevy::log::warn!(
                    "An image {} pixels tall cannot be cut into {} equal {}, so whatever samples \
                     it as one will not.",
                    size.height,
                    waiting.count,
                    if waiting.volume { "slices" } else { "layers" }
                );
                return false;
            }

            let reshaped = Extent3d {
                width: size.width,
                height: size.height / waiting.count,
                depth_or_array_layers: waiting.count,
            };

            if image.reinterpret_size(reshaped).is_err() {
                return false;
            }

            if waiting.volume {
                image.texture_descriptor.dimension = TextureDimension::D3;
            }
        }

        // Named outright, because an array of one layer, or of six, would otherwise be taken for a
        // plain picture or a cube.
        image.texture_view_descriptor = Some(TextureViewDescriptor {
            dimension: Some(if waiting.volume {
                TextureViewDimension::D3
            } else {
                TextureViewDimension::D2Array
            }),
            ..Default::default()
        });

        false
    });
}

/// Asks for an image to be treated as a cubemap once it has loaded, and answers at once.
///
/// Six square faces stacked vertically, which is the layout the skybox takes. Suits a shader
/// material's cube slots, since a flat picture there is replaced by the fallback.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_make_cubemap(image: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = image;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                world
                    .get_resource_or_init::<crate::render::post::PendingCubemaps>()
                    .push(handle);

                status::OK
            })
        }
    })
}

/// Asks for an image to be cut into `count` layers or slices once it has loaded.
///
/// `volume` non-zero makes a 3D texture of `count` slices, and zero an array of `count` 2D layers.
/// Either way the parts are stacked from top to bottom in the picture.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_reshape_image(image: i32, count: i32, volume: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, count, volume);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 1 {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let handle = match crate::render::image_handle(world, image) {
                    Ok(Some(handle)) => handle,
                    Ok(None) => return status::NULL_ARG,
                    Err(refusal) => return refusal,
                };

                world
                    .get_resource_or_init::<PendingReshapes>()
                    .0
                    .push(PendingReshape {
                        image: handle,
                        count: count as u32,
                        volume: volume != 0,
                    });

                status::OK
            })
        }
    })
}

/// Says how an entity's mesh is treated beyond what it looks like.
///
/// A bit each: `1` is never culled for being out of view, `2` casts no shadow, `4` receives none.
/// A bit left clear takes that behavior off again, so the flags are the whole answer rather than
/// additions to it.
///
/// A mesh drawn somewhere its own bounds do not say needs to escape culling, which covers any mesh
/// a vertex shader moves far from where it was built, and one whose vertices a buffer places. Bevy
/// culls by the bounds it worked out from the mesh, so such a mesh vanishes whenever those stale
/// bounds leave the view.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_mesh_flags(entity: u64, flags: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, flags);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::NoFrustumCulling;
            use bevy::light::{NotShadowCaster, NotShadowReceiver};

            with_world(|world| {
                let Ok(mut entity) = world.get_entity_mut(bevy::ecs::entity::Entity::from_bits(entity))
                else {
                    return status::NO_ENTITY;
                };

                if flags & 1 != 0 {
                    entity.insert(NoFrustumCulling);
                } else {
                    entity.remove::<NoFrustumCulling>();
                }

                if flags & 2 != 0 {
                    entity.insert(NotShadowCaster);
                } else {
                    entity.remove::<NotShadowCaster>();
                }

                if flags & 4 != 0 {
                    entity.insert(NotShadowReceiver);
                } else {
                    entity.remove::<NotShadowReceiver>();
                }

                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::app::App;
    use bevy::asset::Assets;
    use bevy::mesh::{Mesh, MeshBuilder, Meshable};

    use crate::state::loan_world;

    fn app() -> App {
        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        crate::assets::init_asset_once::<Mesh>(&mut app);
        app
    }

    #[test]
    fn a_cuboid_is_twenty_four_vertices_and_twelve_triangles() {
        // Four corners a face rather than eight a box, because each face has normals of its own.
        let mut app = app();
        let mesh = bevy::math::primitives::Cuboid::new(1.0, 2.0, 3.0).mesh().build();
        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh).untyped();
        let key = crate::assets::key_for(app.world_mut(), handle);

        let mut info = BcsMeshInfo::default();
        let read = || unsafe { bcs_render_mesh_info(key, &mut info) };
        let code = loan_world(app.world_mut(), read);

        assert_eq!(status::OK, code);
        assert_eq!(24, info.vertices);
        assert_eq!(36, info.indices);
        assert_eq!(0, info.topology);
        assert_eq!(1 | 4, info.attributes & (1 | 4), "normals and UVs");
        assert_eq!([-0.5, -1.0, -1.5], info.min);
        assert_eq!([0.5, 1.0, 1.5], info.max);
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_primitive_rebuilt_in_place_keeps_its_key() {
        let mut app = app();
        let mesh = bevy::math::primitives::Cuboid::new(1.0, 1.0, 1.0).mesh().build();
        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh).untyped();
        let key = crate::assets::key_for(app.world_mut(), handle);

        let mut info = BcsMeshInfo::default();
        let code = loan_world(app.world_mut(), || unsafe {
            let shape = c"Cuboid";
            let rebuilt = bcs_mesh_rebuild(key, shape.as_ptr(), 4.0, 2.0, 6.0);
            assert_eq!(status::OK, rebuilt);

            // A shape that is not one of Bevy's is refused and leaves the mesh as it was.
            assert_eq!(status::NO_COMPONENT, bcs_mesh_rebuild(key, c"Blob".as_ptr(), 1.0, 1.0, 1.0));

            bcs_render_mesh_info(key, &mut info)
        });

        assert_eq!(status::OK, code);
        assert_eq!([-2.0, -1.0, -3.0], info.min);
        assert_eq!([2.0, 1.0, 3.0], info.max);
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_material_is_read_back_as_it_was_made() {
        use bevy::pbr::StandardMaterial;

        let mut app = app();
        crate::assets::init_asset_once::<StandardMaterial>(&mut app);
        crate::assets::init_asset_once::<bevy::image::Image>(&mut app);

        let made = BcsMaterialConfig {
            base_color: [0.25, 0.5, 0.75, 1.0],
            metallic: 0.8,
            roughness: 0.3,
            emissive: [2.0, 1.0, 0.0, 1.0],
            alpha_mode: 1,
            alpha_cutoff: 0.4,
            double_sided: 1,
            unlit: 0,
            base_color_texture: -1,
            normal_map: -1,
            metallic_roughness_texture: -1,
            emissive_texture: -1,
            occlusion_texture: -1,
            uv_scale: [4.0, 2.0],
            uv_rotation: 0.5,
            uv_offset: [0.25, 0.0],
        };

        loan_world(app.world_mut(), || {
            let key = unsafe { bcs_material_create(&made) };
            assert!(key >= 0, "making the material failed with {key}");

            let mut read = made;
            read.metallic = 0.0;
            assert_eq!(status::OK, unsafe { bcs_render_material_read(key, &mut read) });

            assert_eq!(made.base_color, read.base_color);
            assert_eq!(made.metallic, read.metallic);
            assert_eq!(made.roughness, read.roughness);
            assert_eq!(made.emissive, read.emissive);
            assert_eq!((1, 0.4), (read.alpha_mode, read.alpha_cutoff));
            assert_eq!(1, read.double_sided);
            assert_eq!(-1, read.base_color_texture);
            let close = |a: f32, b: f32| (a - b).abs() < 1e-5;
            for (made, read) in [(made.uv_scale, read.uv_scale), (made.uv_offset, read.uv_offset)] {
                assert!(close(made[0], read[0]) && close(made[1], read[1]));
            }
            assert!((made.uv_rotation - read.uv_rotation).abs() < 1e-5);

            // Written over in place, so the same key reads back the new settings.
            let mut changed = made;
            changed.base_color = [1.0, 0.0, 0.0, 1.0];
            changed.roughness = 0.9;
            assert_eq!(status::OK, unsafe { bcs_render_material_write(key, &changed) });
            assert_eq!(status::OK, unsafe { bcs_render_material_read(key, &mut read) });
            assert_eq!([1.0, 0.0, 0.0, 1.0], read.base_color);
            assert_eq!(0.9, read.roughness);

            // Something other than a material is refused rather than overwritten.
            assert_eq!(status::NOT_PRESENT, unsafe { bcs_render_material_write(9999, &changed) });
        });
    }
}

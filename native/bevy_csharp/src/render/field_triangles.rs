//! A mesh's triangles as the scene's distance field reads them from Bevy's copy of the mesh, the
//! box around its places, how many triangles it has, the area they cover, and the corners the
//! field's pool holds, each with its vertex's color and whether its edge to the next corner is
//! open.

use std::collections::HashMap;

use bevy::mesh::{Indices, Mesh, PrimitiveTopology, VertexAttributeValues};

use crate::render::scene_field::OPEN_EDGE;

/// The box around a mesh's places in its own space, or none for a mesh with none the bridge
/// can read, as one whose data went to the render world alone.
pub fn bounds_of(mesh: &Mesh) -> Option<[f32; 6]> {
    let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
        return None;
    };

    let mut b = [f32::MAX, f32::MAX, f32::MAX, f32::MIN, f32::MIN, f32::MIN];
    for p in positions {
        for axis in 0..3 {
            b[axis] = b[axis].min(p[axis]);
            b[axis + 3] = b[axis + 3].max(p[axis]);
        }
    }

    (!positions.is_empty()).then_some(b)
}

/// What a mesh emits from as global illumination carries it, a light of the box around its
/// places, the surface it sends its light out of the box from and which of the box's faces send it.
#[derive(Clone, Copy, Default, Debug, PartialEq)]
pub struct Emitting {
    /// The area of the triangles facing out of the box, away from its middle, in the mesh's own
    /// space, or of all its triangles where none does. A lantern's glass of two layers sends from
    /// the outer alone, the inner facing in and its light kept within the glass, as a path tracer
    /// following each triangle's front keeps it.
    pub surface: f32,
    /// A bit for each face of the box a triangle facing out leans toward most, two an axis, the
    /// side toward the axis's way the higher, or none where no triangle faces out, which names
    /// every face. A lantern's upright panels name its four sides and not its top and bottom.
    pub faces: u32,
}

/// What a mesh emits from ([`Emitting`]), as [`corners_of`] reads its triangles, or nothing for a
/// mesh of no triangles the bridge can read.
pub fn emitting_of(mesh: &Mesh) -> Emitting {
    use bevy::math::Vec3;

    if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
        return Emitting::default();
    }

    let Some(bounds) = bounds_of(mesh) else {
        return Emitting::default();
    };
    let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
        return Emitting::default();
    };

    let indices: Vec<u32> = match mesh.try_indices_option() {
        Ok(Some(Indices::U16(indices))) => indices.iter().map(|&index| index as u32).collect(),
        Ok(Some(Indices::U32(indices))) => indices.clone(),
        Ok(None) => (0..positions.len() as u32).collect(),
        Err(_) => return Emitting::default(),
    };

    let count = indices.len() / 3 * 3;
    if indices[..count].iter().any(|&index| index as usize >= positions.len()) {
        return Emitting::default();
    }

    let middle = (Vec3::new(bounds[0], bounds[1], bounds[2]) + Vec3::new(bounds[3], bounds[4], bounds[5])) / 2.0;
    let (mut all, mut out, mut faces) = (0.0, 0.0, 0u32);
    for corner in indices[..count].chunks_exact(3) {
        let [a, b, c] = [0, 1, 2].map(|k| Vec3::from_array(positions[corner[k] as usize]));
        let normal = (b - a).cross(c - a);
        let area = normal.length() / 2.0;
        all += area;
        if normal.length_squared() <= 1e-24 || normal.dot((a + b + c) / 3.0 - middle) < 0.0 {
            continue;
        }
        out += area;
        let lean = normal.abs();
        let axis = if lean.x >= lean.y && lean.x >= lean.z { 0 } else if lean.y >= lean.z { 1 } else { 2 };
        faces |= 1 << (2 * axis + usize::from(normal[axis] > 0.0));
    }

    if faces == 0 { Emitting { surface: all, faces: 0 } } else { Emitting { surface: out, faces } }
}

/// How many triangles [`corners_of`] reads from a mesh, counted from its indices or its
/// vertices alone. An index past the vertices, which leaves a mesh out of the pool, is not
/// looked for, so such a mesh is weighed by the triangles it would have had.
pub fn triangles_of(mesh: &Mesh) -> u32 {
    if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
        return 0;
    }

    let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
        return 0;
    };

    let corners = match mesh.try_indices_option() {
        Ok(Some(indices)) => indices.len(),
        Ok(None) => positions.len(),
        Err(_) => 0,
    };
    (corners / 3) as u32
}

/// A mesh's triangles as the pool's corners, or none for a mesh of no triangles the bridge can
/// read. The reads that answer rather than panic, since a mesh whose data went to the render
/// world keeps a husk here.
pub fn corners_of(mesh: &Mesh) -> Option<Vec<[u32; 4]>> {
    if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
        return None;
    }

    let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
        return None;
    };

    let colors = match mesh.try_attribute_option(Mesh::ATTRIBUTE_COLOR) {
        Ok(Some(VertexAttributeValues::Float32x4(colors))) if colors.len() == positions.len() => Some(colors),
        _ => None,
    };

    let indices: Vec<u32> = match mesh.try_indices_option() {
        Ok(Some(Indices::U16(indices))) => indices.iter().map(|&index| index as u32).collect(),
        Ok(Some(Indices::U32(indices))) => indices.clone(),
        Ok(None) => (0..positions.len() as u32).collect(),
        Err(_) => return None,
    };

    let count = indices.len() / 3 * 3;
    if count == 0 || indices[..count].iter().any(|&index| index as usize >= positions.len()) {
        return None;
    }

    let places: Vec<[u32; 3]> = indices[..count].iter().map(|&index| positions[index as usize].map(f32::to_bits)).collect();
    let open = open_edges(&places);

    Some(
        (0..count)
            .map(|i| {
                let color = colors.map_or(0x00FF_FFFF, |colors| srgb_bytes(colors[indices[i] as usize]));
                let [x, y, z] = places[i];
                [x, y, z, color | if open[i] { OPEN_EDGE } else { 0 }]
            })
            .collect(),
    )
}

/// Whether each edge of a mesh's triangles is open, its edge from each corner to the next, which
/// no other triangle shares by the places of its ends, so a mesh whose faces keep vertices of
/// their own, as a cube's do, is closed where its faces meet and a ground plane open at its rim.
pub fn open_edges(places: &[[u32; 3]]) -> Vec<bool> {
    let edge = |i: usize| {
        let (a, b) = (places[i], places[i / 3 * 3 + (i % 3 + 1) % 3]);
        if a <= b { (a, b) } else { (b, a) }
    };

    let mut shared: HashMap<([u32; 3], [u32; 3]), u32> = HashMap::with_capacity(places.len());
    for i in 0..places.len() {
        *shared.entry(edge(i)).or_insert(0) += 1;
    }

    (0..places.len()).map(|i| shared[&edge(i)] == 1).collect()
}

/// A color in linear light as three sRGB bytes, red lowest.
fn srgb_bytes(linear: [f32; 4]) -> u32 {
    let byte = |l: f32| {
        let l = l.clamp(0.0, 1.0);
        let s = if l <= 0.003_130_8 { l * 12.92 } else { 1.055 * l.powf(1.0 / 2.4) - 0.055 };
        (s * 255.0 + 0.5) as u32
    };
    byte(linear[0]) | (byte(linear[1]) << 8) | (byte(linear[2]) << 16)
}

#[cfg(test)]
mod tests {
    /// A quad of two triangles is open at its four sides and closed along its diagonal, whether
    /// the triangles share vertices or keep their own.
    #[test]
    fn an_edge_is_open_where_no_other_triangle_shares_its_ends() {
        let at = |x: f32, z: f32| [x.to_bits(), 0f32.to_bits(), z.to_bits()];
        let quad = [at(0.0, 0.0), at(1.0, 0.0), at(1.0, 1.0), at(0.0, 0.0), at(1.0, 1.0), at(0.0, 1.0)];
        assert_eq!(super::open_edges(&quad), [true, true, false, false, true, true]);
    }

    /// A mesh is weighed by as many triangles as the pool reads from it, with indices or without.
    #[test]
    fn a_mesh_weighs_the_triangles_the_pool_reads() {
        use bevy::shape::{Cuboid, Sphere};
        use bevy::mesh::Mesh;

        for mesh in [Mesh::from(Cuboid::default()), Mesh::from(Sphere::default()), Mesh::from(Cuboid::default()).with_duplicated_vertices()] {
            let read = super::corners_of(&mesh).map_or(0, |corners| corners.len() / 3);
            assert!(read > 0);
            assert_eq!(super::triangles_of(&mesh) as usize, read);
        }
    }

    /// An emitter's surface is the area of its triangles facing out of its box, a cube's six faces
    /// and a ball's sphere within its tessellation's few hundredths, whether its vertices are
    /// shared or not, each from every face of its box.
    #[test]
    fn a_mesh_emits_from_the_area_of_its_triangles() {
        use bevy::mesh::Mesh;
        use bevy::shape::{Cuboid, Sphere};

        for cube in [Mesh::from(Cuboid::default()), Mesh::from(Cuboid::default()).with_duplicated_vertices()] {
            let emitting = super::emitting_of(&cube);
            assert!((emitting.surface - 6.0).abs() < 1e-4);
            assert_eq!(emitting.faces, 63);
        }
        let ball = super::emitting_of(&Mesh::from(Sphere::new(0.6)));
        let sphere = 4.0 * std::f32::consts::PI * 0.36;
        assert!(ball.surface < sphere && ball.surface > 0.98 * sphere, "a ball of 0.6 emits from {}, a sphere's surface {sphere}", ball.surface);
        assert_eq!(ball.faces, 63);
    }

    /// A lantern's glass of two layers, four upright panels facing out around four facing in and
    /// open above and below, emits from its outer panels alone and from the four upright faces of
    /// its box, not its top and bottom.
    #[test]
    fn a_lanterns_glass_emits_from_its_outer_panels_and_upright_faces() {
        use bevy::asset::RenderAssetUsages;
        use bevy::math::Vec3;
        use bevy::mesh::{Indices, Mesh, PrimitiveTopology};

        let mut positions = Vec::new();
        let mut indices = Vec::new();
        for (half, outward) in [(0.5f32, true), (0.45, false)] {
            // Each upright panel at the side its way points to, wound to face out or in.
            for way in [Vec3::X, Vec3::NEG_X, Vec3::Z, Vec3::NEG_Z] {
                let across = Vec3::Y.cross(way);
                let first = positions.len() as u32;
                for (u, v) in [(-1.0, -1.0), (1.0, -1.0), (1.0, 1.0), (-1.0, 1.0)] {
                    positions.push((way * half + across * (u * half) + Vec3::Y * v).to_array());
                }
                let quad = [first, first + 1, first + 2, first, first + 2, first + 3];
                indices.extend(if outward { quad } else { [quad[0], quad[2], quad[1], quad[3], quad[5], quad[4]] });
            }
        }
        let glass = Mesh::new(PrimitiveTopology::TriangleList, RenderAssetUsages::default())
            .with_inserted_attribute(Mesh::ATTRIBUTE_POSITION, positions)
            .with_inserted_indices(Indices::U32(indices));

        let emitting = super::emitting_of(&glass);
        assert!((emitting.surface - 4.0 * 1.0 * 2.0).abs() < 1e-4, "the glass emits from {}", emitting.surface);
        assert_eq!(emitting.faces, 0b11_00_11, "the glass emits from the faces {:06b}", emitting.faces);
    }
}

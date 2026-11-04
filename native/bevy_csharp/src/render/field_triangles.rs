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

/// The area of a mesh's triangles in its own space, or none for a mesh of no triangles the bridge
/// can read, as [`corners_of`] reads them.
pub fn surface_of(mesh: &Mesh) -> f32 {
    if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
        return 0.0;
    }

    let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
        return 0.0;
    };

    let indices: Vec<u32> = match mesh.try_indices_option() {
        Ok(Some(Indices::U16(indices))) => indices.iter().map(|&index| index as u32).collect(),
        Ok(Some(Indices::U32(indices))) => indices.clone(),
        Ok(None) => (0..positions.len() as u32).collect(),
        Err(_) => return 0.0,
    };

    let count = indices.len() / 3 * 3;
    if indices[..count].iter().any(|&index| index as usize >= positions.len()) {
        return 0.0;
    }

    indices[..count]
        .chunks_exact(3)
        .map(|corner| {
            let [a, b, c] = [0, 1, 2].map(|k| bevy::math::Vec3::from_array(positions[corner[k] as usize]));
            (b - a).cross(c - a).length() / 2.0
        })
        .sum()
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

    /// An emitter's surface is the area of its triangles, a cube's six faces and a ball's sphere
    /// within its tessellation's few hundredths, whether its vertices are shared or not.
    #[test]
    fn a_mesh_emits_from_the_area_of_its_triangles() {
        use bevy::mesh::Mesh;
        use bevy::shape::{Cuboid, Sphere};

        for cube in [Mesh::from(Cuboid::default()), Mesh::from(Cuboid::default()).with_duplicated_vertices()] {
            assert!((super::surface_of(&cube) - 6.0).abs() < 1e-4);
        }
        let ball = super::surface_of(&Mesh::from(Sphere::new(0.6)));
        let sphere = 4.0 * std::f32::consts::PI * 0.36;
        assert!(ball < sphere && ball > 0.98 * sphere, "a ball of 0.6 emits from {ball}, a sphere's surface {sphere}");
    }
}

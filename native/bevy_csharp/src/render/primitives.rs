//! Meshes built from Bevy's primitives by name, and built again in place with new measures.

use crate::interop::status;
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

/// Builds a convex polygon pushed out into a solid `depth` deep along Z, centered on its middle,
/// from `count` corners of two numbers each in order around it, and returns an asset handle for it.
///
/// Bevy's `Extrusion` of a `ConvexPolygon`. Corners that do not go around a convex shape in one
/// direction are refused with [`status::INVALID_STATE`], as Bevy refuses them, since a concave one
/// would fill across its dent.
///
/// # Safety
/// `corners` must be readable for `count * 2` floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_create_convex_extrusion(corners: *const f32, count: i32, depth: f32) -> i32 {
    crate::interop::guard(|| {
        if corners.is_null() || count < 3 {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            let _ = depth;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::math::Vec2;
            use bevy::mesh::{Mesh, Meshable};
            use bevy::shape::{ConvexPolygon, Extrusion};

            let numbers = unsafe { core::slice::from_raw_parts(corners, count as usize * 2) };
            let Ok(polygon) = ConvexPolygon::new(numbers.chunks_exact(2).map(|corner| Vec2::new(corner[0], corner[1]))) else {
                return status::INVALID_STATE;
            };
            let mesh: Mesh = Extrusion::new(polygon, depth).mesh().into();

            with_world(|world| {
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
    use bevy::shape::{
        Annulus, Capsule2d, Capsule3d, Circle, CircularSector, CircularSegment, Cone,
        ConicalFrustum, Cuboid, Cylinder, Ellipse, Plane3d, Rectangle, RegularPolygon, Rhombus,
        Sphere, Tetrahedron, Torus, Triangle3d,
    };
    use bevy::mesh::{Mesh, Meshable};

    if let Some(outline) = kind.strip_prefix("Ring(").and_then(|rest| rest.strip_suffix(')')) {
        return ring(outline, a, b, c);
    }

    if let Some(arc) = kind.strip_prefix("UvAngle(").and_then(|rest| rest.strip_suffix(')')) {
        return turned_image(arc, a, b, c);
    }

    if let Some(outline) = kind.strip_prefix("Extrusion(").and_then(|rest| rest.strip_suffix(')')) {
        return extrusion(outline, a, b, c);
    }

    let mesh: Mesh = match kind {
        "Cuboid" => Cuboid::new(a, b, c).mesh().into(),
        "Sphere" => Sphere::new(a).mesh().into(),
        // Bevy's default sphere subdivided as many times as asked rather than its five, which
        // Bevy refuses at eighty and over for the vertices it would take, as Bevy's many_lights
        // subdivides its sphere nine times to light its inside evenly.
        "IcoSphere" => match Sphere::new(a).mesh().ico(b as u32) {
            Ok(mesh) => mesh,
            Err(error) => {
                bevy::log::warn!("{error}, so the sphere is subdivided five times, as Bevy's default is");
                Sphere::new(a).mesh().into()
            }
        },
        // A sphere of rings and slices rather than Bevy's default of subdivided triangles, which
        // maps an image around it the way a globe is drawn: the radius, then how many slices go
        // around it and how many rings from pole to pole, at least three and two.
        "UvSphere" => Sphere::new(a)
            .mesh()
            .uv((b as u32).max(3), (c as u32).max(2))
            .into(),
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
        // The flat shapes a 2D camera draws, in the plane a rectangle is, by Bevy's own measures:
        // a sector or a segment by its radius and the half angle it spans, an ellipse by its half
        // width and half height, a capsule by its radius and the length between its ends, a
        // rhombus by its two diagonals, and a regular polygon by its circumradius and sides.
        "CircularSector" => CircularSector::new(a, b).mesh().into(),
        "CircularSegment" => CircularSegment::new(a, b).mesh().into(),
        "Ellipse" => Ellipse::new(a, b).mesh().into(),
        "Capsule2d" => Capsule2d::new(a, b).mesh().into(),
        "Rhombus" => Rhombus::new(a, b).mesh().into(),
        "RegularPolygon" => RegularPolygon::new(a, (b.max(3.0)) as u32).mesh().into(),
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

/// A sector or a segment whose texture coordinates are turned by `angle`, in radians, Bevy's
/// `CircularMeshUvMode::Mask { angle }`, by the sector's or the segment's radius and half angle.
///
/// Bevy maps a sector or a segment onto its image as a mask over the circle it is cut from, the
/// circle's center at the image's, and the angle turns the vertices as they are mapped rather than
/// the image, so a shape turned one way by its transform shows its image upright when the angle is
/// the turn the other way. Nothing for a name that is neither.
#[cfg(feature = "render")]
fn turned_image(arc: &str, radius: f32, half_angle: f32, angle: f32) -> Option<bevy::mesh::Mesh> {
    use bevy::shape::{CircularSector, CircularSegment};
    use bevy::mesh::{CircularMeshUvMode, Meshable};

    let uv_mode = CircularMeshUvMode::Mask { angle };
    match arc {
        "CircularSector" => Some(CircularSector::new(radius, half_angle).mesh().uv_mode(uv_mode).into()),
        "CircularSegment" => Some(CircularSegment::new(radius, half_angle).mesh().uv_mode(uv_mode).into()),
        _ => None,
    }
}

/// The band a flat shape's outline makes, `thickness` wide on the inside of the shape `a` and `b`
/// measure, or nothing for a shape with no inside.
///
/// Bevy insets most shapes by the thickness on every side. A sector and an ellipse it cannot inset
/// evenly, since the curve inside an ellipse is no ellipse, so they are given the inner shape its
/// examples give them, the same sector of a smaller radius and an ellipse smaller on each axis.
#[cfg(feature = "render")]
fn ring(outline: &str, a: f32, b: f32, thickness: f32) -> Option<bevy::mesh::Mesh> {
    let (outline, thickness) = ring_outline(outline, thickness);
    ring_of(outline, a, b, thickness, None)
}

/// A ring's outline and its thickness, where the name gives one after a comma, as
/// `Ring(Rectangle, 0.1)` does inside an extrusion whose third measure is its depth, or the
/// thickness given.
#[cfg(feature = "render")]
fn ring_outline(outline: &str, thickness: f32) -> (&str, f32) {
    match outline.split_once(',') {
        Some((name, given)) => (name.trim(), given.trim().parse().unwrap_or(thickness)),
        None => (outline, thickness),
    }
}

/// The band along the inside of a flat shape's outline, `thickness` wide, flat or, given a depth,
/// pushed out into a solid that deep, as Bevy extrudes a ring.
#[cfg(feature = "render")]
fn ring_of(outline: &str, a: f32, b: f32, thickness: f32, depth: Option<f32>) -> Option<bevy::mesh::Mesh> {
    use bevy::shape::{
        Capsule2d, Circle, CircularSector, CircularSegment, Ellipse, Extrusion, Rectangle,
        RegularPolygon, Rhombus, Ring, ToRing, Triangle2d,
    };
    use bevy::mesh::{Extrudable, Meshable};
    use bevy::shape::Primitive2d;

    fn made<P>(ring: Ring<P>, depth: Option<f32>) -> bevy::mesh::Mesh
    where
        P: Primitive2d + Meshable + Clone,
        P::Output: Extrudable,
        Ring<P>: Primitive2d,
    {
        match depth {
            Some(depth) => Extrusion::new(ring, depth).mesh().into(),
            None => ring.mesh().into(),
        }
    }

    let mesh = match outline {
        "Circle" => made(Circle::new(a).to_ring(thickness), depth),
        "CircularSector" => made(Ring::new(CircularSector::new(a, b), CircularSector::new(a - thickness, b)), depth),
        "CircularSegment" => made(CircularSegment::new(a, b).to_ring(thickness), depth),
        "Ellipse" => made(Ring::new(Ellipse::new(a, b), Ellipse::new(a - thickness, b - thickness)), depth),
        "Capsule2d" => made(Capsule2d::new(a, b).to_ring(thickness), depth),
        "Rhombus" => made(Rhombus::new(a, b).to_ring(thickness), depth),
        "Rectangle" => made(Rectangle::new(a, b).to_ring(thickness), depth),
        "RegularPolygon" => made(RegularPolygon::new(a, (b.max(3.0)) as u32).to_ring(thickness), depth),
        // Bevy's default flat triangle stands where its default triangle in space does, so this is
        // the band around the one Triangle draws.
        "Triangle" => {
            let [first, second, third] = Triangle2d::default().vertices;
            made(Triangle2d::new(first * a, second * a, third * a).to_ring(thickness), depth)
        }
        _ => return None,
    };

    Some(mesh)
}

/// A flat shape pushed out into a solid `depth` deep along Z, centered on its middle, by the
/// measures `a` and `b` give the flat shape, or nothing for a shape Bevy does not extrude.
#[cfg(feature = "render")]
fn extrusion(outline: &str, a: f32, b: f32, depth: f32) -> Option<bevy::mesh::Mesh> {
    // A ring pushed out into a tube, its thickness in its name since the third measure is the
    // depth, as Bevy's 3d_shapes extrudes each flat shape's ring.
    if let Some(ring) = outline.strip_prefix("Ring(").and_then(|rest| rest.strip_suffix(')')) {
        let (ring, thickness) = ring_outline(ring, 0.1);
        return ring_of(ring, a, b, thickness, Some(depth));
    }

    use bevy::shape::{
        Annulus, Capsule2d, Circle, CircularSector, CircularSegment, Ellipse, Extrusion, Rectangle,
        RegularPolygon, Rhombus, Triangle2d,
    };
    use bevy::mesh::Meshable;

    let mesh = match outline {
        "Circle" => Extrusion::new(Circle::new(a), depth).mesh().into(),
        "Annulus" => Extrusion::new(Annulus::new(a, b), depth).mesh().into(),
        "CircularSector" => Extrusion::new(CircularSector::new(a, b), depth).mesh().into(),
        "CircularSegment" => Extrusion::new(CircularSegment::new(a, b), depth).mesh().into(),
        "Ellipse" => Extrusion::new(Ellipse::new(a, b), depth).mesh().into(),
        "Capsule2d" => Extrusion::new(Capsule2d::new(a, b), depth).mesh().into(),
        "Rhombus" => Extrusion::new(Rhombus::new(a, b), depth).mesh().into(),
        "Rectangle" => Extrusion::new(Rectangle::new(a, b), depth).mesh().into(),
        "RegularPolygon" => Extrusion::new(RegularPolygon::new(a, (b.max(3.0)) as u32), depth).mesh().into(),
        // Bevy's default flat triangle scaled by the first number, as Triangle draws it.
        "Triangle" => {
            let [first, second, third] = Triangle2d::default().vertices;
            Extrusion::new(Triangle2d::new(first * a, second * a, third * a), depth).mesh().into()
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
                // Written through the guard, which marks the mesh changed for the renderer.
                let Some(mut held) = meshes.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                *held = mesh;
                status::OK
            })
        }
    })
}

// Every test here draws on the renderer, so the module is left out of a build without it.
#[cfg(all(test, feature = "render"))]
mod tests {
    use super::*;
    use crate::render::meshes::{bcs_render_mesh_info, BcsMeshInfo};
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

    #[cfg(feature = "render")]
    #[test]
    fn a_primitive_rebuilt_in_place_keeps_its_key() {
        let mut app = app();
        let mesh = bevy::shape::Cuboid::new(1.0, 1.0, 1.0).mesh().build();
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

    #[test]
    fn a_ring_is_extruded_with_the_thickness_its_name_gives() {
        let flat = super::primitive("Ring(Rectangle)", 1.0, 1.0, 0.1).expect("a flat ring");
        let named = super::primitive("Ring(Rectangle, 0.1)", 1.0, 1.0, 0.5).expect("a ring named with its thickness");
        let tube = super::primitive("Extrusion(Ring(Rectangle, 0.1))", 1.0, 1.0, 1.0).expect("an extruded ring");

        // The thickness the name gives stands for the third measure, which an extrusion spends on
        // its depth, so the flat ring is the same either way and the tube has its sides as well.
        assert_eq!(flat.count_vertices(), named.count_vertices());
        assert!(tube.count_vertices() > 2 * flat.count_vertices(), "{} beside {}", tube.count_vertices(), flat.count_vertices());
        assert!(super::primitive("Extrusion(Ring(Tetrahedron, 0.1))", 1.0, 1.0, 1.0).is_none());
    }

    #[test]
    fn a_convex_polygon_is_extruded_and_a_concave_one_refused() {
        let mut world = bevy::prelude::World::new();
        world.init_resource::<Assets<Mesh>>();

        let pentagon = [0.0, 0.8, -0.47, 0.25, -0.47, -0.65, 0.47, -0.65, 0.47, 0.25];
        let dented = [0.0, 0.0, 1.0, 1.0, 0.0, 0.5, -1.0, 1.0];

        let (made, refused) = crate::state::loan_world(&mut world, || unsafe {
            (
                super::bcs_mesh_create_convex_extrusion(pentagon.as_ptr(), 5, 1.0),
                super::bcs_mesh_create_convex_extrusion(dented.as_ptr(), 4, 1.0),
            )
        });

        assert!(made > 0, "the pentagon was answered {made}");
        assert_eq!(refused, crate::interop::status::INVALID_STATE);
        assert_eq!(world.resource::<Assets<Mesh>>().len(), 1);
    }
}

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

/// One of Bevy's primitives as a mesh, by its name and up to three measures, or nothing for a name
/// that is none of them.
#[cfg(feature = "render")]
fn primitive(kind: &str, a: f32, b: f32, c: f32) -> Option<bevy::mesh::Mesh> {
    use bevy::math::primitives::{
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
    use bevy::math::primitives::{CircularSector, CircularSegment};
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
    use bevy::math::primitives::{
        Capsule2d, Circle, CircularSector, CircularSegment, Ellipse, Rectangle, RegularPolygon,
        Rhombus, Ring, ToRing, Triangle2d,
    };

    let mesh = match outline {
        "Circle" => Circle::new(a).to_ring(thickness).into(),
        "CircularSector" => {
            Ring::new(CircularSector::new(a, b), CircularSector::new(a - thickness, b)).into()
        }
        "CircularSegment" => CircularSegment::new(a, b).to_ring(thickness).into(),
        "Ellipse" => Ring::new(Ellipse::new(a, b), Ellipse::new(a - thickness, b - thickness)).into(),
        "Capsule2d" => Capsule2d::new(a, b).to_ring(thickness).into(),
        "Rhombus" => Rhombus::new(a, b).to_ring(thickness).into(),
        "Rectangle" => Rectangle::new(a, b).to_ring(thickness).into(),
        "RegularPolygon" => RegularPolygon::new(a, (b.max(3.0)) as u32).to_ring(thickness).into(),
        // Bevy's default flat triangle stands where its default triangle in space does, so this is
        // the band around the one Triangle draws.
        "Triangle" => {
            let [first, second, third] = Triangle2d::default().vertices;
            Triangle2d::new(first * a, second * a, third * a).to_ring(thickness).into()
        }
        _ => return None,
    };

    Some(mesh)
}

/// A flat shape pushed out into a solid `depth` deep along Z, centered on its middle, by the
/// measures `a` and `b` give the flat shape, or nothing for a shape Bevy does not extrude.
#[cfg(feature = "render")]
fn extrusion(outline: &str, a: f32, b: f32, depth: f32) -> Option<bevy::mesh::Mesh> {
    use bevy::math::primitives::{
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
}

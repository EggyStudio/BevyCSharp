//! The camera a shadow's levels of detail are measured from.
//!
//! A mesh with a visibility range is drawn by its distance from the camera, and its shadow is meant
//! to be drawn by the same distance, from the camera a directional light's cascades belong to. Bevy
//! 0.20 looks that camera up among the render world's views by its main world entity, which finds
//! nothing, and measures every cascade from its shadow origin instead, the entity carrying
//! `ShadowLodOrigin`, or else the first camera drawing to a window, or else the first camera of
//! all. An offscreen run has no camera drawing to a window, so the origin was the first of a light
//! probe's face cameras. A tree near the camera then cast no shadow at all. The cascades' casters
//! were chosen by the camera's distance, which counted the tree's full level in and its reduced
//! level out, and the shader dithered the full level away by the probe's distance. In a window the
//! origin is the first camera drawing to it, the scene's only where it was spawned before the
//! interface's.
//!
//! So each 3D camera drawing the game's picture, to the primary window or to the image an offscreen
//! run draws in its place, carries `ShadowLodOrigin`, which Bevy takes before anything else, and
//! the shadows are measured from where the picture is seen from in either run. One given here is
//! taken off a camera pointed elsewhere, and every one given here gives way while the game gives
//! the component to an entity of its own, since Bevy takes the first of all that carry it.
//!
//! A mesh of a visibility range casts over the half of each margin nearer its own distance. Bevy
//! draws a mesh into a light's shadow map wherever its range holds the origin, both margins whole,
//! and its shadow pass takes no dither, so where two levels of a tree cross over a margin both
//! cast whole and the crown shades itself twice, a lone oak's leaves there 12 to 19% under the
//! path-traced reference where either level alone reads within 5% under and 8% over. So a mesh
//! carries `NotShadowCaster` while the origin stands outside the span from the middle of the
//! margin it fades in over to the middle of the one it fades out over, the span the reference
//! traces it on, and one level casts at any distance, the shadow changing level at the margin's
//! middle in the frame the origin crosses it. One given here is taken off as the origin comes back
//! into the span, and a game's own is left as it is.

use bevy::app::App;
use bevy::camera::primitives::Aabb;
use bevy::camera::visibility::VisibilityRange;
use bevy::camera::{Camera, Camera3d, RenderTarget, ShadowLodOrigin};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{Has, Or, With, Without};
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Commands, Query, Res};
use bevy::light::{
    get_shadow_lod_origin, NotShadowCaster, PointLight, SimulationLightSystems, SpotLight,
};
use bevy::transform::components::GlobalTransform;
use bevy::window::WindowRef;

/// Marks a `ShadowLodOrigin` given here rather than by the game.
#[derive(Component)]
struct Given;

/// Marks a `NotShadowCaster` given here, the origin outside the half of the mesh's margins it
/// casts over, rather than by the game.
#[derive(Component)]
struct OutOfItsHalf;

pub(crate) fn install(app: &mut App) {
    app.add_systems(
        bevy::app::PostUpdate,
        (
            keep_origins,
            cast_one_level
                .after(bevy::transform::TransformSystems::Propagate)
                .before(SimulationLightSystems::CheckLightVisibility),
        ),
    );
}

/// Gives each 3D camera drawing the game's picture `ShadowLodOrigin`, and takes the one given here
/// off a camera drawing anything else, or off every camera while the game gives one of its own.
fn keep_origins(
    mut commands: Commands,
    offscreen: Option<Res<crate::offscreen::OffscreenTarget>>,
    cameras: Query<(Entity, &RenderTarget, Has<ShadowLodOrigin>, Has<Given>), With<Camera3d>>,
    the_games: Query<(), (With<ShadowLodOrigin>, Without<Given>)>,
) {
    let games_own = !the_games.is_empty();
    for (entity, target, origin, given) in &cameras {
        let picture = !games_own && match target {
            RenderTarget::Window(WindowRef::Primary) => true,
            RenderTarget::Image(image) => offscreen
                .as_ref()
                .is_some_and(|offscreen| image.handle == offscreen.image),
            _ => false,
        };

        if picture && !origin {
            commands.entity(entity).insert((ShadowLodOrigin, Given));
        } else if !picture && given {
            commands.entity(entity).remove::<(ShadowLodOrigin, Given)>();
        }
    }
}

/// Gives each mesh of a visibility range `NotShadowCaster` while the shadow origin stands outside
/// the span from the middle of the margin it fades in over to the middle of the one it fades out
/// over, and takes the one given here off as the origin comes back into it.
fn cast_one_level(
    mut commands: Commands,
    mut cameras: Query<(Entity, &RenderTarget), With<Camera>>,
    mut origins: Query<Entity, With<ShadowLodOrigin>>,
    mut lamps: Query<Entity, Or<(With<PointLight>, With<SpotLight>)>>,
    places: Query<&GlobalTransform>,
    meshes: Query<(
        Entity,
        &VisibilityRange,
        &GlobalTransform,
        Option<&Aabb>,
        Has<NotShadowCaster>,
        Has<OutOfItsHalf>,
    )>,
) {
    let origin = get_shadow_lod_origin(
        cameras.transmute_lens_filtered(),
        origins.transmute_lens_filtered(),
        lamps.transmute_lens_filtered(),
    );
    let Some(eye) = origin
        .and_then(|origin| places.get(origin).ok())
        .map(GlobalTransform::translation)
    else {
        return;
    };

    let middle = |margin: &core::ops::Range<f32>| (margin.start + margin.end) * 0.5;
    for (entity, range, place, aabb, off, given) in &meshes {
        // Where Bevy measures the mesh from, its place, or the middle of its box where it asks.
        let at = match aabb {
            Some(aabb) if range.use_aabb => place.transform_point(aabb.center.into()),
            _ => place.translation(),
        };
        let distance = at.distance(eye);
        let casts = distance >= middle(&range.start_margin) && distance < middle(&range.end_margin);

        if !casts && !off {
            commands.entity(entity).insert((NotShadowCaster, OutOfItsHalf));
        } else if casts && given {
            commands.entity(entity).remove::<(NotShadowCaster, OutOfItsHalf)>();
        }
    }
}

#[cfg(test)]
mod tests {
    use super::{cast_one_level, OutOfItsHalf};
    use bevy::camera::visibility::VisibilityRange;
    use bevy::camera::{Camera, ShadowLodOrigin};
    use bevy::ecs::entity::Entity;
    use bevy::ecs::system::RunSystemOnce;
    use bevy::ecs::world::World;
    use bevy::light::NotShadowCaster;
    use bevy::math::Vec3;
    use bevy::transform::components::GlobalTransform;

    /// A mesh casts from the middle of the margin it fades in over to the middle of the one it
    /// fades out over, measured from the shadow origin, and a game's own `NotShadowCaster` stays as
    /// the mesh comes into that span and leaves it.
    #[test]
    fn a_mesh_casts_over_the_half_of_each_margin_nearer_its_own_distance() {
        let mut world = World::new();
        world.spawn((Camera::default(), ShadowLodOrigin));
        let range = VisibilityRange {
            start_margin: 10.0..20.0,
            end_margin: 30.0..40.0,
            use_aabb: false,
        };
        let at = |x: f32| GlobalTransform::from_translation(Vec3::new(x, 0.0, 0.0));
        let mut spawn = |x: f32| world.spawn((range.clone(), at(x))).id();
        let (near, inside, far) = (spawn(14.0), spawn(16.0), spawn(36.0));
        let games = world.spawn((range.clone(), at(16.0), NotShadowCaster)).id();
        let off = |world: &World, entity: Entity| {
            let given = world.get::<OutOfItsHalf>(entity).is_some();
            (world.get::<NotShadowCaster>(entity).is_some(), given)
        };

        world.run_system_once(cast_one_level).unwrap();
        assert_eq!(off(&world, near), (true, true), "short of its first margin's middle");
        assert_eq!(off(&world, inside), (false, false), "past it");
        assert_eq!(off(&world, far), (true, true), "past its last margin's middle");
        assert_eq!(off(&world, games), (true, false), "the game's own, in its span");

        // The far mesh comes into its span, and the game's leaves it.
        *world.get_mut::<GlobalTransform>(far).unwrap() = at(34.0);
        *world.get_mut::<GlobalTransform>(games).unwrap() = at(36.0);
        world.run_system_once(cast_one_level).unwrap();
        assert_eq!(off(&world, far), (false, false), "back short of its last margin's middle");
        assert_eq!(off(&world, games), (true, false), "the game's own, out of its span");

        // And the game's comes back.
        *world.get_mut::<GlobalTransform>(games).unwrap() = at(16.0);
        world.run_system_once(cast_one_level).unwrap();
        assert_eq!(off(&world, games), (true, false), "the game's own, back in its span");
    }
}

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

use bevy::app::App;
use bevy::camera::{Camera3d, RenderTarget, ShadowLodOrigin};
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::ecs::query::{Has, With, Without};
use bevy::ecs::system::{Commands, Query, Res};
use bevy::window::WindowRef;

/// Marks a `ShadowLodOrigin` given here rather than by the game.
#[derive(Component)]
struct Given;

pub(crate) fn install(app: &mut App) {
    app.add_systems(bevy::app::PostUpdate, keep_origins);
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

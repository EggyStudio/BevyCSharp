//! Drawing: the assets a picture is made of, the entities that carry them, and what a camera
//! does with the result.
//!
//! Everything here needs the `render` feature. A headless build keeps the entry points so the
//! managed side links either way, and they report [`status::UNSUPPORTED`].
//!
//! Split three ways, along the seams the managed surface already has:
//!
//! - [`assets`] builds the meshes and materials a picture is made of, and attaches them. Those
//!   two cannot go through the generic component path: each is a Rust value that has to be
//!   constructed rather than described by a layout, and the components carrying them hold a
//!   typed `Handle<T>`, which raw bytes cannot represent.
//! - [`scene`] spawns what the picture contains: cameras, lights and sprites.
//! - [`post`] is what a camera does to the picture once the scene has been drawn.
//!
//! What the three share sits here: resolving an asset key, and refusing an entity that is not a
//! camera.

pub mod assets;
pub mod post;
pub mod scene;

#[cfg(feature = "render")]
use crate::interop::status;

/// Resolves an asset key to the image it names.
///
/// A negative key is the caller saying "no image", which every image on a config is allowed to
/// be. A key that names nothing is a mistake rather than a default: that is what a released or
/// fabricated handle looks like from this side, and quietly drawing without the texture that was
/// asked for is a wrong picture nothing reports.
#[cfg(feature = "render")]
pub(crate) fn image_handle(
    world: &mut bevy::ecs::world::World,
    key: i32,
) -> Result<Option<bevy::asset::Handle<bevy::image::Image>>, i32> {
    if key < 0 {
        return Ok(None);
    }

    match crate::assets::clone_handle(world, key) {
        Some(handle) => Ok(Some(handle.typed::<bevy::image::Image>())),
        None => Err(status::NO_COMPONENT),
    }
}

/// Reports why `entity` cannot be given what a camera is given, or `None` if it can.
///
/// Everything these calls attach is read by the render graph the camera drives, so on any other
/// entity it would sit there doing nothing. Answered through a shared reference, so a call can be
/// refused before it has built anything it would have to throw away.
#[cfg(feature = "render")]
pub(crate) fn refuse_unless_camera(
    world: &bevy::ecs::world::World,
    entity: bevy::ecs::entity::Entity,
) -> Option<i32> {
    let Ok(entity_ref) = world.get_entity(entity) else {
        return Some(status::NO_ENTITY);
    };

    if !entity_ref.contains::<bevy::camera::Camera>() {
        return Some(status::NOT_PRESENT);
    }

    None
}

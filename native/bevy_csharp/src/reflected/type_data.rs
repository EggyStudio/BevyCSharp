//! Reflection data Bevy leaves off types a C# program reads or writes, registered as the app is
//! built.
//!
//! Each is a type Bevy reflects whose registration lacks a piece the rest of this module reads by,
//! without which the type is described and cannot be written, or is not found at all. The data is
//! Bevy's own, from the impls the type has, and only its registration is added.

use bevy::prelude::App;

/// Registers what each type below lacks.
pub(crate) fn register(app: &mut App) {
    // A range of numbers read and written as JSON, `{"start":..,"end":..}`, which Bevy's
    // reflection registers without its serde data, so a component holding one, as
    // `VisibilityRange` holds two, could be read from C# and not written. Serde has the impls and
    // only the registration is missing.
    {
        use bevy::reflect::{ReflectDeserialize, ReflectSerialize};
        app.register_type::<core::ops::Range<f32>>();
        app.register_type_data::<core::ops::Range<f32>, ReflectSerialize>();
        app.register_type_data::<core::ops::Range<f32>, ReflectDeserialize>();
    }

    // Bevy's switch for skipping the transforms of trees that have not moved, which it reflects
    // without the resource's data, so neither the schema nor a C# program would find it, and the
    // stress tests that turn it off could not. A resource here is a component on an entity of its own,
    // found and written through the component's data, which registering the resource's data alone
    // leaves out, so it is given both, as `#[reflect(Resource)]` gives a resource Bevy reflects.
    {
        use bevy::ecs::reflect::{ReflectComponent, ReflectResource};
        use bevy::transform::StaticTransformOptimizations;
        app.register_type::<StaticTransformOptimizations>();
        app.register_type_data::<StaticTransformOptimizations, ReflectResource>();
        app.register_type_data::<StaticTransformOptimizations, ReflectComponent>();
    }
}

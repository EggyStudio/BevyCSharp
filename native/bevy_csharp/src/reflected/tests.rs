use super::*;
use super::describe::*;
use super::values::*;
use std::ffi::CString;

use bevy::app::App;
use bevy::ecs::component::Component;
use bevy::ecs::entity::Entity;
use bevy::math::Vec3;
use bevy::reflect::Reflect;

use crate::state::loan_world;

#[derive(Reflect, Default, Clone, Copy, PartialEq, Debug)]
#[reflect(Default)]
struct Inner {
    depth: i32,
}

#[derive(Reflect, Default, Clone, Copy, PartialEq, Debug)]
#[reflect(Default)]
enum Mode {
    #[default]
    Off,
    Fixed(f32),
    Ranged { low: f32, high: f32 },
}

#[derive(Component, Reflect, Default)]
#[reflect(Component, Default)]
struct Probe {
    speeds: Vec<f32>,
    speed: f32,
    at: Vec3,
    inner: Inner,
    mode: Mode,
}

#[derive(Component, Reflect, Default)]
#[component(immutable)]
#[reflect(Component, Default)]
struct Frozen(f32);

#[derive(Component, Reflect, Default)]
#[reflect(Component, Default)]
struct Tinted {
    tint: Color,
    glow: LinearRgba,
}

#[derive(Component, Reflect, Default)]
#[reflect(Component, Default)]
struct Textured {
    image: bevy::asset::Handle<bevy::image::Image>,
}

const PROBE: &str = "bevy_csharp::reflected::tests::Probe";
const TEXTURED: &str = "bevy_csharp::reflected::tests::Textured";
const TINTED: &str = "bevy_csharp::reflected::tests::Tinted";
const FROZEN: &str = "bevy_csharp::reflected::tests::Frozen";

fn probe_app() -> (App, Entity) {
    let mut app = App::new();
    app.register_type::<Probe>().register_type::<Frozen>();
    let entity = app
        .world_mut()
        .spawn((Probe { speed: 2.0, ..Default::default() }, Frozen(1.0)))
        .id();
    (app, entity)
}

fn c(text: &str) -> CString {
    CString::new(text).unwrap()
}

/// Calls a text-returning export the way C# does, probing for the length and asking again.
fn text(call: impl Fn(*mut u8, i32) -> i32) -> Result<String, i32> {
    let needed = call(core::ptr::null_mut(), 0);
    if needed < 0 {
        return Err(needed);
    }
    let mut buffer = vec![0u8; needed as usize];
    assert_eq!(needed, call(buffer.as_mut_ptr(), needed));
    Ok(String::from_utf8(buffer).unwrap())
}

fn get(entity: Entity, type_path: &str, path: &str) -> Result<String, i32> {
    let (type_path, path) = (c(type_path), c(path));
    text(|out, capacity| unsafe {
        bcs_reflect_get(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), out, capacity)
    })
}

fn set(entity: Entity, type_path: &str, path: &str, json: &str) -> i32 {
    let (type_path, path) = (c(type_path), c(path));
    unsafe {
        let len = json.len() as i32;
        bcs_reflect_set(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), json.as_ptr(), len)
    }
}

fn last_error() -> String {
    text(|out, capacity| unsafe { bcs_reflect_error(out, capacity) }).unwrap()
}

#[test]
fn the_registry_describes_a_component_and_what_its_fields_hold() {
    let (mut app, _) = probe_app();
    let dump = loan_world(app.world_mut(), || {
        text(|out, capacity| unsafe { bcs_reflect_types(out, capacity) })
    });
    let dump: Value = serde_json::from_str(&dump.unwrap()).unwrap();
    let component = |path: &str| {
        dump["components"].as_array().unwrap().iter().find(|c| c["path"] == path).cloned()
    };

    let probe = component(PROBE).expect("the probe is listed");
    assert_eq!(true, probe["default"]);
    assert_eq!(true, probe["mutable"]);

    let frozen = component(FROZEN).expect("the immutable one is listed");
    assert_eq!(false, frozen["mutable"]);

    // The nested struct and the enum are described too, so the managed side can flatten them
    // without asking again.
    let types = &dump["types"];
    assert_eq!("struct", types[PROBE]["kind"]);
    let speeds = &types["alloc::vec::Vec<f32>"];
    assert_eq!("list", speeds["kind"]);
    assert_eq!("f32", speeds["item"]);

    let mode = &types["bevy_csharp::reflected::tests::Mode"];
    assert_eq!("enum", mode["kind"]);
    assert_eq!(3, mode["variants"].as_array().unwrap().len());
    assert_eq!("struct", types["bevy_csharp::reflected::tests::Inner"]["kind"]);
}

#[test]
fn a_field_is_read_and_written_by_its_path() {
    let (mut app, entity) = probe_app();
    loan_world(app.world_mut(), || {
        assert_eq!(Ok("2.0".to_owned()), get(entity, PROBE, "speed"));
        assert_eq!(status::OK, set(entity, PROBE, "speed", "4.5"));
        assert_eq!(status::OK, set(entity, PROBE, "at", "[1.0, 2.0, 3.0]"));
        assert_eq!(status::OK, set(entity, PROBE, "inner.depth", "7"));
    });

    let probe = app.world().get::<Probe>(entity).unwrap();
    assert_eq!(4.5, probe.speed);
    assert_eq!(Vec3::new(1.0, 2.0, 3.0), probe.at);
    assert_eq!(7, probe.inner.depth);
}

#[test]
fn a_variant_is_chosen_by_name_with_its_fields_at_their_defaults() {
    let (mut app, entity) = probe_app();
    loan_world(app.world_mut(), || {
        let (type_path, path, variant) = (c(PROBE), c("mode"), c("Ranged"));
        let bits = entity.to_bits();
        let code = unsafe {
            bcs_reflect_set_variant(bits, type_path.as_ptr(), path.as_ptr(), variant.as_ptr())
        };
        assert_eq!(status::OK, code);

        let named = text(|out, capacity| unsafe {
            bcs_reflect_variant(bits, type_path.as_ptr(), path.as_ptr(), out, capacity)
        });
        assert_eq!(Ok("Ranged".to_owned()), named);

        // A field of the active variant is reached by name, without naming the variant.
        assert_eq!(status::OK, set(entity, PROBE, "mode.high", "9.0"));
    });

    let mode = app.world().get::<Probe>(entity).unwrap().mode;
    assert_eq!(Mode::Ranged { low: 0.0, high: 9.0 }, mode);
}

#[test]
fn a_component_is_inserted_at_its_default_and_removed() {
    let (mut app, _) = probe_app();
    let entity = app.world_mut().spawn_empty().id();
    loan_world(app.world_mut(), || {
        let type_path = c(PROBE);
        assert_eq!(status::OK, unsafe {
            bcs_reflect_insert(entity.to_bits(), type_path.as_ptr(), core::ptr::null(), 0)
        });
        assert_eq!(Ok("0.0".to_owned()), get(entity, PROBE, "speed"));

        let remove = || unsafe { bcs_reflect_remove(entity.to_bits(), type_path.as_ptr()) };
        assert_eq!(status::OK, remove());
        assert_eq!(status::NOT_PRESENT, remove(), "a second removal finds nothing");
    });
    assert!(app.world().get::<Probe>(entity).is_none());
}

#[test]
fn a_bad_path_or_value_is_refused_with_a_reason() {
    let (mut app, entity) = probe_app();
    loan_world(app.world_mut(), || {
        assert_eq!(status::INVALID_STATE, set(entity, PROBE, "nowhere", "1.0"));
        assert!(last_error().contains("nowhere"), "{}", last_error());

        assert_eq!(status::INVALID_STATE, set(entity, PROBE, "speed", "\"fast\""));
        assert!(last_error().contains("fast"), "{}", last_error());

        assert_eq!(Err(status::NO_COMPONENT), get(entity, "no::such::Type", ""));
    });
    assert_eq!(2.0, app.world().get::<Probe>(entity).unwrap().speed);
}

#[test]
fn a_handle_crosses_as_the_key_the_asset_table_gives_it() {
    use bevy::asset::{AssetApp, Assets};
    use bevy::image::Image;
    use bevy::mesh::{Mesh, MeshBuilder, Meshable};

    let mut app = App::new();
    app.add_plugins(bevy::app::TaskPoolPlugin::default());
    app.add_plugins(bevy::asset::AssetPlugin::default());
    app.init_asset::<Image>().init_asset::<Mesh>();
    app.register_type::<Textured>();

    let first = app.world_mut().resource_mut::<Assets<Image>>().add(Image::default());
    let second = app.world_mut().resource_mut::<Assets<Image>>().add(Image::default());
    let mesh = bevy::math::primitives::Cuboid::new(1.0, 1.0, 1.0).mesh().build();
    let mesh = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh);
    let entity = app.world_mut().spawn(Textured { image: first.clone() }).id();

    loan_world(app.world_mut(), || {
        let (type_path, path) = (c(TEXTURED), c(".image"));
        let bits = entity.to_bits();
        let read = || unsafe { bcs_reflect_get_asset(bits, type_path.as_ptr(), path.as_ptr()) };
        let write = |key| unsafe {
            bcs_reflect_set_asset(bits, type_path.as_ptr(), path.as_ptr(), key)
        };

        // Read every frame by an inspector, so the second read has to find the first's slot.
        let key = read();
        assert!(key >= 0, "the read failed with {key}");
        assert_eq!(key, read(), "a second read took a slot of its own");

        let key_of = |handle: bevy::asset::UntypedHandle| {
            crate::state::with_world(|world| crate::assets::key_for(world, handle))
        };
        assert_eq!(status::OK, write(key_of(second.clone().untyped())));

        // A mesh offered to an image field is refused rather than retyped into nonsense.
        assert_eq!(status::INVALID_STATE, write(key_of(mesh.clone().untyped())));
        assert!(last_error().contains("another kind"), "{}", last_error());
    });

    assert_eq!(second.id(), app.world().get::<Textured>(entity).unwrap().image.id());
}

#[test]
fn a_color_crosses_as_linear_and_keeps_the_space_it_was_held_in() {
    let mut app = App::new();
    app.register_type::<Tinted>();
    let tinted = Tinted { tint: Color::srgb(1.0, 0.5, 0.0), glow: LinearRgba::BLACK };
    let entity = app.world_mut().spawn(tinted).id();

    loan_world(app.world_mut(), || {
        let (type_path, tint, glow) = (c(TINTED), c(".tint"), c(".glow"));
        let bits = entity.to_bits();

        let mut read = [0f32; 4];
        let (owner, into) = (type_path.as_ptr(), read.as_mut_ptr());
        let code = unsafe { bcs_reflect_get_color(bits, owner, tint.as_ptr(), into) };
        assert_eq!(status::OK, code);

        // sRGB one half is about a fifth in linear, which says the conversion ran.
        assert!((read[1] - 0.214).abs() < 0.01, "green read as {}", read[1]);
        assert_eq!(1.0, read[0]);

        let write = |path: &CString| unsafe {
            bcs_reflect_set_color(bits, type_path.as_ptr(), path.as_ptr(), 0.0, 1.0, 0.0, 1.0)
        };
        assert_eq!(status::OK, write(&tint));
        assert_eq!(status::OK, write(&glow));
    });

    let tinted = app.world().get::<Tinted>(entity).unwrap();
    assert!(matches!(tinted.tint, Color::Srgba(_)), "the color changed space");
    assert_eq!(LinearRgba::GREEN, tinted.glow);
    assert_eq!(LinearRgba::GREEN, tinted.tint.to_linear());
}

#[test]
fn an_immutable_component_is_written_by_inserting_a_written_copy() {
    // `reflect_mut` panics on an immutable component, so the export never asks for it there,
    // and writes a copy it inserts over the component instead, as Bevy changes one.
    let (mut app, entity) = probe_app();
    loan_world(app.world_mut(), || {
        assert_eq!(status::OK, set(entity, FROZEN, "0", "3.0"));
    });
    assert_eq!(3.0, app.world().get::<Frozen>(entity).unwrap().0);
}

use super::*;
use super::describe::*;
use super::lists::*;
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

#[derive(Reflect, Clone, Copy, PartialEq, Debug)]
struct Plain {
    depth: i32,
}

#[derive(Reflect, Default, Clone, Copy, PartialEq, Debug)]
#[reflect(Default)]
enum Slot {
    #[default]
    Empty,
    Holding(Plain),
}

#[derive(Component, Reflect, Default)]
#[reflect(Component, Default)]
struct Held {
    slot: Slot,
}

#[derive(Reflect, Clone, PartialEq, Debug)]
enum Filled {
    One(i32),
    Two { depth: i32 },
    Many(Vec<Plain>),
}

#[derive(Component, Reflect, Default)]
#[reflect(Component, Default)]
struct Listed {
    items: Vec<Plain>,
    fills: Vec<Filled>,
}

#[derive(Component, Reflect)]
#[reflect(Component)]
struct Spanned {
    margin: core::ops::Range<f32>,
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

#[test]
fn a_struct_holding_a_range_with_no_default_is_inserted_with_an_empty_one() {
    // A range registers no default, so the struct is made from its fields' defaults, the range's
    // the empty one at zero.
    let mut app = App::new();
    app.register_type::<Spanned>();
    let entity = app.world_mut().spawn_empty().id();
    let type_path = c("bevy_csharp::reflected::tests::Spanned");
    loan_world(app.world_mut(), || {
        let code = unsafe { bcs_reflect_insert(entity.to_bits(), type_path.as_ptr(), core::ptr::null(), 0) };
        assert_eq!(status::OK, code, "{}", last_error());
    });
    assert_eq!(0.0..0.0, app.world().get::<Spanned>(entity).unwrap().margin);
}

#[test]
fn a_variant_holding_a_struct_with_no_default_is_made_from_its_fields() {
    // A sprite's slicer registers no default, so choosing the sliced variant makes one from its
    // fields' defaults, as inserting a component with none does.
    let mut app = App::new();
    app.register_type::<Held>();
    let entity = app.world_mut().spawn(Held::default()).id();
    let (type_path, path, variant) = (c("bevy_csharp::reflected::tests::Held"), c("slot"), c("Holding"));
    loan_world(app.world_mut(), || {
        let code = unsafe { bcs_reflect_set_variant(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), variant.as_ptr()) };
        assert_eq!(status::OK, code, "{}", last_error());
    });
    assert_eq!(Slot::Holding(Plain { depth: 0 }), app.world().get::<Held>(entity).unwrap().slot);
}

#[test]
fn a_list_is_counted_and_resized_with_items_made_from_their_fields() {
    // Items the list has no default for are made from their fields', and a resize down drops the
    // items past the new end, which applying a shorter list would not.
    let mut app = App::new();
    app.register_type::<Listed>();
    let entity = app.world_mut().spawn(Listed { items: vec![Plain { depth: 7 }], fills: vec![] }).id();
    let (type_path, path) = (c("bevy_csharp::reflected::tests::Listed"), c(".items"));
    loan_world(app.world_mut(), || {
        let len = || unsafe { bcs_reflect_list_len(entity.to_bits(), type_path.as_ptr(), path.as_ptr()) };
        let resize = |to| unsafe { bcs_reflect_list_resize(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), to) };
        assert_eq!(1, len());
        assert_eq!(status::OK, resize(3), "{}", last_error());
        assert_eq!(3, len());
        assert_eq!(status::OK, resize(2), "{}", last_error());
        assert_eq!(status::INVALID_STATE, resize(-1));
    });
    assert_eq!(vec![Plain { depth: 7 }, Plain { depth: 0 }], app.world().get::<Listed>(entity).unwrap().items);
}

#[test]
fn an_enum_whose_every_variant_holds_a_value_defaults_to_its_first() {
    // A gradient has no default and no variant holding nothing, so a list of them grows by its first
    // variant made from its values' defaults.
    let mut app = App::new();
    app.register_type::<Listed>();
    let entity = app.world_mut().spawn(Listed::default()).id();
    let (type_path, path) = (c("bevy_csharp::reflected::tests::Listed"), c(".fills"));
    loan_world(app.world_mut(), || {
        let code = unsafe { bcs_reflect_list_resize(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), 1) };
        assert_eq!(status::OK, code, "{}", last_error());
    });
    assert_eq!(vec![Filled::One(0)], app.world().get::<Listed>(entity).unwrap().fills);
}

#[test]
fn a_variant_holding_a_list_is_chosen_with_the_list_empty() {
    // A Vec registers no default, so a variant holding one, as a gradient holds its stops, is made
    // with it empty.
    let mut app = App::new();
    app.register_type::<Listed>();
    let entity = app.world_mut().spawn(Listed { items: vec![], fills: vec![Filled::One(3)] }).id();
    let (type_path, path, variant) = (c("bevy_csharp::reflected::tests::Listed"), c(".fills[0]"), c("Many"));
    loan_world(app.world_mut(), || {
        let code = unsafe { bcs_reflect_set_variant(entity.to_bits(), type_path.as_ptr(), path.as_ptr(), variant.as_ptr()) };
        assert_eq!(status::OK, code, "{}", last_error());
    });
    assert_eq!(vec![Filled::Many(vec![])], app.world().get::<Listed>(entity).unwrap().fills);
}

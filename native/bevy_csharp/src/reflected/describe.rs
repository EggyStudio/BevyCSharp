//! The registry's description of Bevy's types, closed over what their fields reach, which the
//! managed side builds its inspector rows and the generator's description from.

use super::*;
use super::values::is_color;

// -- The registry

/// A field's or type's documentation, which only an editor build carries.
///
/// `docs()` exists only when `bevy_reflect` keeps doc comments, which the editor profile turns on
/// and a game's profile leaves off, since a player has no tooltip to read them in and the comments
/// would make every shipped library larger.
#[cfg(feature = "editor")]
macro_rules! docs {
    ($described:expr) => {
        $described.docs()
    };
}

#[cfg(not(feature = "editor"))]
macro_rules! docs {
    ($described:expr) => {{
        let _ = &$described;
        None::<&'static str>
    }};
}

/// Describes a type and everything its fields hold, once each, into `types`.
///
/// The description is closed over what is reachable, so the managed side can flatten a nested
/// struct into rows without asking again. A type is entered before its fields are walked, which
/// ends the walk at a type that contains itself.
pub(super) fn describe(registry: &TypeRegistry, info: &'static TypeInfo, types: &mut Map<String, Value>) {
    let path = info.type_path();
    if types.contains_key(path) {
        return;
    }
    types.insert(path.to_owned(), Value::Null);

    let registration = registry.get(info.type_id());
    let mut entry = json!({
        "short": info.type_path_table().short_path(),
        // An unregistered type cannot be serialized at all, since the serializer looks every
        // struct up, so the managed side shows such a field without offering to edit it.
        "registered": registration.is_some(),
        "serialize": registration.is_some_and(|r| r.data::<ReflectSerialize>().is_some()),
        "default": registration.is_some_and(|r| r.data::<ReflectDefault>().is_some()),
    });

    if let Some(text) = docs!(info) {
        entry["docs"] = Value::from(text);
    }

    // A color is one value to the managed side, a swatch, whatever space it is held in, so the
    // three types Bevy keeps colors in are marked rather than walked into their fields.
    if is_color(info.type_id()) {
        entry["color"] = Value::Bool(true);
    }

    // A handle names the kind of asset it holds, so the managed side can offer files of that kind.
    // An asset type the bridge does not load gets an empty kind, which still says it is a handle.
    if let Some(handle) = registration.and_then(|r| r.data::<ReflectHandle>()) {
        entry["asset"] = Value::from(crate::events::kind_of(handle.asset_type_id()));
    }

    let mut field = |name: String,
                     ty: &'static str,
                     nested: Option<&'static TypeInfo>,
                     text: Option<&'static str>| {
        if let Some(nested) = nested {
            describe(registry, nested, types);
        }
        match text {
            Some(text) => json!({ "name": name, "type": ty, "docs": text }),
            None => json!({ "name": name, "type": ty }),
        }
    };

    let kind = match info {
        TypeInfo::Struct(info) => {
            let fields: Vec<Value> = info
                .iter()
                .map(|f| field(f.name().to_owned(), f.type_path(), f.type_info(), docs!(f)))
                .collect();
            entry["fields"] = Value::Array(fields);
            "struct"
        }
        TypeInfo::TupleStruct(info) => {
            let fields: Vec<Value> = info
                .iter()
                .map(|f| field(f.index().to_string(), f.type_path(), f.type_info(), docs!(f)))
                .collect();
            entry["fields"] = Value::Array(fields);
            "tuple_struct"
        }
        TypeInfo::Enum(info) => {
            let variants: Vec<Value> = info
                .iter()
                .map(|variant| match variant {
                    VariantInfo::Unit(unit) => json!({ "name": unit.name(), "kind": "unit" }),
                    VariantInfo::Tuple(tuple) => json!({
                        "name": tuple.name(),
                        "kind": "tuple",
                        "fields": tuple.iter()
                            .map(|f| {
                                let name = f.index().to_string();
                                field(name, f.type_path(), f.type_info(), docs!(f))
                            })
                            .collect::<Vec<_>>(),
                    }),
                    VariantInfo::Struct(named) => json!({
                        "name": named.name(),
                        "kind": "struct",
                        "fields": named.iter()
                            .map(|f| {
                                let name = f.name().to_owned();
                                field(name, f.type_path(), f.type_info(), docs!(f))
                            })
                            .collect::<Vec<_>>(),
                    }),
                })
                .collect();
            entry["variants"] = Value::Array(variants);
            "enum"
        }
        TypeInfo::Tuple(_) => "tuple",
        // A list or an array says what it holds, so one of numbers or vectors can be edited as a
        // list rather than shown as JSON. An array's length is fixed, which the managed side keeps.
        TypeInfo::List(info) => {
            entry["item"] = Value::from(info.item_ty().path());
            if let Some(item) = info.item_info() {
                describe(registry, item, types);
            }
            "list"
        }
        TypeInfo::Array(info) => {
            entry["item"] = Value::from(info.item_ty().path());
            entry["capacity"] = Value::from(info.capacity());
            if let Some(item) = info.item_info() {
                describe(registry, item, types);
            }
            "array"
        }
        TypeInfo::Map(_) => "map",
        TypeInfo::Set(_) => "set",
        TypeInfo::Opaque(_) => "opaque",
    };
    entry["kind"] = Value::from(kind);

    types.insert(path.to_owned(), entry);
}

/// Builds the description [`bcs_reflect_types`] returns.
///
/// Every reflected component is registered with the world here, deliberately. Its id is then
/// fixed for the run, so the managed side keeps it as a constant rather than asking for it again,
/// and a component no plugin has touched yet can still be inserted by the editor.
fn registry_json(world: &mut World) -> String {
    let registry = world.resource::<AppTypeRegistry>().clone();
    let registry = registry.read();

    let mut components = Vec::new();
    let mut types = Map::new();

    for registration in registry.iter() {
        let Some(reflect) = registration.data::<ReflectComponent>() else {
            continue;
        };

        let id = reflect.register_component(world);
        let mutable = world.components().get_info(id).is_none_or(|info| info.mutable());
        let info = registration.type_info();
        let table = info.type_path_table();

        components.push(json!({
            "path": table.path(),
            "short": table.short_path(),
            "id": id.index(),
            "default": registration.data::<ReflectDefault>().is_some()
                || registration.data::<ReflectFromWorld>().is_some(),
            "mutable": mutable,
        }));
        describe(&registry, info, &mut types);
    }

    components.sort_by(|a, b| a["path"].as_str().cmp(&b["path"].as_str()));

    json!({ "components": components, "types": types }).to_string()
}

/// Describes every reflected component and every type its fields reach, as JSON.
///
/// Taken after startup, because a type a plugin registers exists only once that plugin has been
/// added. The text is built again on each call, so a caller probing for its length pays twice,
/// which is acceptable for something asked once a run.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_reflect_types(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            let text = registry_json(world);
            unsafe { write_text(&text, out, capacity) }
        })
    })
}

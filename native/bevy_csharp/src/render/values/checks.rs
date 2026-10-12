//! What a value is checked against before it is kept, the name and the type its shader declares,
//! and the listing of what a shader takes that a refusal shows.

use super::*;

/// Says whether `value` can go where `name` is, and why not when it cannot.
///
/// What the entry points ask before storing a value, so a wrong name or a wrong type is refused
/// where the call was made rather than found later on the render side.
pub fn check(layout: &Layout, name: &str, value: &Value) -> Result<(), String> {
    let (base, index) = match value {
        Value::Image(..) | Value::Sampler(_) | Value::Buffer(_) => split_index(name),
        _ => (name, 0),
    };

    let Some(target) = layout.find(base) else {
        return Err(format!(
            "the shader declares nothing called {base}. It declares {}.",
            listing(layout)
        ));
    };

    match (target, value) {
        (Target::Constant(_), Value::Numbers { components: 1, data, .. }) if data.len() == 4 => Ok(()),
        (Target::Constant(constant), value) => Err(format!(
            "{base} is a pipeline constant, a {}, which takes one number, and {} cannot go there",
            constant.scalar.describe(),
            value.describe()
        )),
        (Target::Uniform { ty, .. }, Value::Numbers { scalar, components, data }) => {
            fits(ty, *scalar, *components, data.len() / 4)
        }
        (Target::Uniform { .. }, Value::Bytes(_)) => Ok(()),
        (Target::Resource { info, .. }, Value::Bytes(_))
            if matches!(info.kind, BindingKind::Uniform { .. }) =>
        {
            Ok(())
        }
        (Target::Resource { info, .. }, value) => {
            let wanted = match (&info.kind, value) {
                (BindingKind::Texture { .. } | BindingKind::StorageTexture { .. }, Value::Image(..))
                | (BindingKind::Storage { .. }, Value::Buffer(_))
                | (BindingKind::Sampler { .. }, Value::Sampler(_))
                | (BindingKind::AccelerationStructure, Value::RayScene(_)) => true,
                _ => false,
            };

            if !wanted {
                return Err(format!(
                    "{base} is a {}, and {} cannot go there",
                    info.describe(),
                    value.describe()
                ));
            }

            if index >= info.count.unwrap_or(1) {
                return Err(format!(
                    "{base} holds {}, so there is no element {index}",
                    info.count.unwrap_or(1)
                ));
            }

            Ok(())
        }
        (Target::Uniform { ty, .. }, value) => Err(format!(
            "{name} is a {}, and {} cannot go there",
            ty.describe(),
            value.describe()
        )),
    }
}

/// Every name in a layout, for a message.
pub fn listing(layout: &Layout) -> String {
    let names = layout.names();

    if names.is_empty() {
        "nothing".into()
    } else {
        names.join(", ")
    }
}

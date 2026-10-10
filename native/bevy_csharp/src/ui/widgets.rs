//! Bevy's widgets, made to keep their own state.

use super::*;

/// Makes one of Bevy's widgets keep its own state: `0` a slider its value, `1` a checkbox whether
/// it is checked, `2` a radio group which of its buttons is, `3` a tab list which of its tabs is
/// selected.
///
/// A widget only reports a change, as an event, and Bevy's examples attach Bevy's own observer
/// that writes it back, which a C# game cannot hold. This attaches it, so a slider follows a drag
/// and C# reads its value from the widget's components.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_widget_self_update(entity: u64, kind: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, kind);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ui_widgets::{
                checkbox_self_update, radio_self_update, slider_self_update, tablist_self_update,
            };

            with_world(|world| {
                let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };

                match kind {
                    0 => {
                        entity_mut.observe(slider_self_update);
                    }
                    1 => {
                        entity_mut.observe(checkbox_self_update);
                    }
                    2 => {
                        entity_mut.observe(radio_self_update);
                    }
                    3 => {
                        entity_mut.observe(tablist_self_update);
                    }
                    _ => return status::NULL_ARG,
                }
                status::OK
            })
        }
    })
}

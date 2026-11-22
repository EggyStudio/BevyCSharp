//! The input held from Bevy's own camera controllers while a panel of the Diagnostics module's that
//! takes input is open, the console or the settings panel.
//!
//! The managed side holds what a program reads, `Input` in C#, and says here as the hold begins and
//! ends (`bcs_input_hold`). Bevy's free and pan cameras read Bevy's input resources in the fixed
//! main loop's first set, so for that set alone the resources are swapped for copies a held hand
//! leaves, and put back after it, and everything else in the frame reads them as they came, the
//! interface and the managed side among them. While the hold lasts every key and mouse button comes
//! up in its first frame, so a camera ends what it was doing and lets the cursor go, and after it
//! nothing is down, the mouse is still and no touch is down. As it ends, what is still down stays
//! hidden from them until it is let go, so a key held as the panel closes is no fresh press of
//! theirs. The pan-orbit cameras' pointer, which this bridge reads in
//! `camera_controllers::pointer`, ends the motion under way as the hold begins and starts none
//! while it lasts. No controller of Bevy's reads a pad, so the pads are left as they are.

#[cfg(feature = "render")]
use std::collections::HashSet;

#[cfg(feature = "render")]
use bevy::input::{keyboard::KeyCode, mouse::MouseButton};
use bevy::prelude::Resource;

/// Whether the program's input is held, with what was down as the hold ended, which stays hidden
/// from the controllers until it comes up.
#[derive(Resource, Default)]
pub struct InputHold {
    held: bool,
    began: bool,
    ended: bool,
    // A run with no renderer has no controllers to hide them from.
    #[cfg(feature = "render")]
    keys_over: HashSet<KeyCode>,
    #[cfg(feature = "render")]
    buttons_over: HashSet<MouseButton>,
}

impl InputHold {
    /// Whether a panel holds the program's input.
    pub fn held(&self) -> bool {
        self.held
    }
}

/// Holds the input from Bevy's camera controllers while `held` is not zero, and lets it go when
/// it is, what is down then staying hidden from them until it comes up. Called as the hold changes.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_input_hold(held: i32) -> i32 {
    crate::interop::guard(|| {
        crate::state::with_world(|world| {
            let held = held != 0;
            let mut hold = world.get_resource_or_insert_with(InputHold::default);
            if hold.held != held {
                hold.held = held;
                hold.began = held;
                hold.ended = !held;
            }
            crate::interop::status::OK
        })
    })
}

#[cfg(feature = "render")]
pub use systems::install;

#[cfg(feature = "render")]
mod systems {
    use std::hash::Hash;

    use bevy::app::{App, RunFixedMainLoop, RunFixedMainLoopSystems};
    use bevy::input::ButtonInput;
    use bevy::input::keyboard::KeyCode;
    use bevy::input::mouse::{AccumulatedMouseMotion, AccumulatedMouseScroll, MouseButton};
    use bevy::input::touch::Touches;
    use bevy::prelude::*;

    use super::InputHold;

    /// The real input while the controllers read the held copies.
    #[derive(Resource, Default)]
    struct Stashed {
        keys: Option<ButtonInput<KeyCode>>,
        buttons: Option<ButtonInput<MouseButton>>,
        motion: Option<AccumulatedMouseMotion>,
        scroll: Option<AccumulatedMouseScroll>,
        touches: Option<Touches>,
    }

    #[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
    enum HoldSystems {
        Hide,
        Show,
    }

    /// Swaps the input for the held copies around the set Bevy's free and pan cameras run in.
    pub fn install(app: &mut App) {
        if app.world().contains_resource::<Stashed>() {
            return;
        }

        app.init_resource::<InputHold>()
            .init_resource::<Stashed>()
            .configure_sets(
                RunFixedMainLoop,
                (
                    HoldSystems::Hide.before(RunFixedMainLoopSystems::BeforeFixedMainLoop),
                    HoldSystems::Show
                        .after(RunFixedMainLoopSystems::BeforeFixedMainLoop)
                        .before(RunFixedMainLoopSystems::FixedMainLoop),
                ),
            )
            .add_systems(RunFixedMainLoop, (hide.in_set(HoldSystems::Hide), show.in_set(HoldSystems::Show)));
    }

    fn hide(
        mut hold: ResMut<InputHold>,
        mut stashed: ResMut<Stashed>,
        mut keys: ResMut<ButtonInput<KeyCode>>,
        mut buttons: ResMut<ButtonInput<MouseButton>>,
        mut motion: ResMut<AccumulatedMouseMotion>,
        mut scroll: ResMut<AccumulatedMouseScroll>,
        mut touches: ResMut<Touches>,
    ) {
        let hold = &mut *hold;
        if std::mem::take(&mut hold.ended) {
            hold.keys_over.extend(keys.get_pressed().copied());
            hold.buttons_over.extend(buttons.get_pressed().copied());
        }
        if !hold.held && hold.keys_over.is_empty() && hold.buttons_over.is_empty() {
            return;
        }

        let began = std::mem::take(&mut hold.began);
        let held_keys = seen(&keys, hold.held, began, &mut hold.keys_over);
        stashed.keys = Some(std::mem::replace(&mut *keys, held_keys));
        let held_buttons = seen(&buttons, hold.held, began, &mut hold.buttons_over);
        stashed.buttons = Some(std::mem::replace(&mut *buttons, held_buttons));

        if hold.held {
            stashed.motion = Some(std::mem::take(&mut *motion));
            stashed.scroll = Some(std::mem::take(&mut *scroll));
            stashed.touches = Some(std::mem::take(&mut *touches));
        }
    }

    fn show(
        mut stashed: ResMut<Stashed>,
        mut keys: ResMut<ButtonInput<KeyCode>>,
        mut buttons: ResMut<ButtonInput<MouseButton>>,
        mut motion: ResMut<AccumulatedMouseMotion>,
        mut scroll: ResMut<AccumulatedMouseScroll>,
        mut touches: ResMut<Touches>,
    ) {
        if let Some(real) = stashed.keys.take() {
            *keys = real;
        }
        if let Some(real) = stashed.buttons.take() {
            *buttons = real;
        }
        if let Some(real) = stashed.motion.take() {
            *motion = real;
        }
        if let Some(real) = stashed.scroll.take() {
            *scroll = real;
        }
        if let Some(real) = stashed.touches.take() {
            *touches = real;
        }
    }

    /// The keys or buttons as a held hand leaves them, nothing down while the hold lasts, what was
    /// down coming up in its first frame, and after it what was down as it ended hidden until it
    /// comes up, its release with it.
    fn seen<T: Copy + Eq + Hash + Send + Sync + 'static>(
        real: &ButtonInput<T>,
        held: bool,
        began: bool,
        over: &mut std::collections::HashSet<T>,
    ) -> ButtonInput<T> {
        if held {
            let mut copy = ButtonInput::default();
            if began {
                for &input in real.get_pressed() {
                    copy.press(input);
                    copy.release(input);
                    copy.clear_just_pressed(input);
                }
            }
            return copy;
        }

        let mut copy = real.clone();
        for &input in over.iter() {
            copy.reset(input);
        }
        over.retain(|input| real.pressed(*input));
        copy
    }
}

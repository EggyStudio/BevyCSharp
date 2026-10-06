//! A node laid out as a grid, its tracks and where a child sits in them.

use super::*;

/// Turns one described track into Bevy's, repeated as many times as it asked for.
#[cfg(feature = "render")]
fn grid_track(track: &crate::interop::BcsGridTrack) -> bevy::ui::RepeatedGridTrack {
    use bevy::ui::{GridTrackRepetition, RepeatedGridTrack};

    // A count of at least one, because a track repeated no times is a track that is not there and
    // a caller asking for that would leave it out of the list.
    let count = track.repeat.clamp(1, u16::MAX as i32) as u16;

    // Filling the grid is only offered where a track has a size to divide the room by, which is
    // pixels and percent. Asked for on any other track it reads as once, because the alternative
    // is refusing a whole layout over one number.
    let repetition = match track.repeat {
        -1 => GridTrackRepetition::AutoFill,
        -2 => GridTrackRepetition::AutoFit,
        _ => GridTrackRepetition::Count(count),
    };

    match track.kind {
        1 => RepeatedGridTrack::px(repetition, track.value),
        2 => RepeatedGridTrack::percent(repetition, track.value),
        3 => RepeatedGridTrack::fr(count, track.value),
        6 => RepeatedGridTrack::flex(count, track.value),
        4 => RepeatedGridTrack::min_content(count),
        5 => RepeatedGridTrack::max_content(count),
        _ => RepeatedGridTrack::auto(count),
    }
}

/// Turns one described track into Bevy's, ignoring how many times it said it repeats.
///
/// What a grid makes on demand for an item placed past the tracks it was given. Bevy takes these
/// as plain tracks rather than repeated ones, because the list is cycled through as often as it is
/// needed and a repeat inside it would say the same thing twice.
#[cfg(feature = "render")]
fn auto_track(track: &crate::interop::BcsGridTrack) -> bevy::ui::GridTrack {
    use bevy::ui::GridTrack;

    match track.kind {
        1 => GridTrack::px(track.value),
        2 => GridTrack::percent(track.value),
        3 => GridTrack::fr(track.value),
        6 => GridTrack::flex(track.value),
        4 => GridTrack::min_content(),
        5 => GridTrack::max_content(),
        _ => GridTrack::auto(),
    }
}

/// Reads one of the config's lists as tracks made on demand, or nothing where it named none.
///
/// # Safety
/// `tracks` must point at `count` tracks, or be null.
#[cfg(feature = "render")]
unsafe fn auto_tracks(
    tracks: *const crate::interop::BcsGridTrack,
    count: i32,
) -> Option<Vec<bevy::ui::GridTrack>> {
    if tracks.is_null() || count <= 0 {
        return None;
    }

    let described = unsafe { core::slice::from_raw_parts(tracks, count as usize) };

    Some(described.iter().map(auto_track).collect())
}

/// Reads one of the config's lists, or nothing where it named none.
///
/// # Safety
/// `tracks` must point at `count` tracks, or be null.
#[cfg(feature = "render")]
unsafe fn grid_tracks(
    tracks: *const crate::interop::BcsGridTrack,
    count: i32,
) -> Option<Vec<bevy::ui::RepeatedGridTrack>> {
    if tracks.is_null() || count <= 0 {
        return None;
    }

    let described = unsafe { core::slice::from_raw_parts(tracks, count as usize) };

    Some(described.iter().map(grid_track).collect())
}

/// How an item sits across the cell it was placed in.
#[cfg(feature = "render")]
fn justify_items(value: i32) -> bevy::ui::JustifyItems {
    use bevy::ui::JustifyItems;

    match value {
        1 => JustifyItems::Start,
        2 => JustifyItems::End,
        3 => JustifyItems::Center,
        4 => JustifyItems::Baseline,
        5 => JustifyItems::Stretch,
        _ => JustifyItems::Default,
    }
}

/// Lays a node's children out on a grid.
///
/// Applied to a node that already exists rather than passed with one, because the tracks are lists
/// and a list has no room in the flat config every other field arrives in. The node is also told to
/// lay out as a grid here, since a grid with no `Display::Grid` is a set of numbers nothing reads.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or carries no node.
///
/// # Safety
/// `config` must point at one [`BcsUiGridConfig`], whose lists must each point at as many tracks as
/// their count says, or be null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ui_set_grid(
    entity: u64,
    config: *const crate::interop::BcsUiGridConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            let rows = unsafe { grid_tracks(config.rows, config.row_count) };
            let columns = unsafe { grid_tracks(config.columns, config.column_count) };
            let auto_rows = unsafe { auto_tracks(config.auto_rows, config.auto_row_count) };
            let auto_columns =
                unsafe { auto_tracks(config.auto_columns, config.auto_column_count) };

            crate::state::with_world(|world| {
                use bevy::ui::{Display, GridAutoFlow, Node};

                let entity = bevy::ecs::entity::Entity::from_bits(entity);
                let Some(mut node) = world.get_mut::<Node>(entity) else {
                    return status::NO_COMPONENT;
                };

                node.display = Display::Grid;
                node.grid_auto_flow = match config.auto_flow {
                    1 => GridAutoFlow::Column,
                    2 => GridAutoFlow::RowDense,
                    3 => GridAutoFlow::ColumnDense,
                    _ => GridAutoFlow::Row,
                };
                node.justify_items = justify_items(config.justify_items);

                // A list left out keeps what the node had, so a caller setting the columns alone
                // does not silently clear the rows it set a moment ago.
                if let Some(rows) = rows {
                    node.grid_template_rows = rows;
                }
                if let Some(columns) = columns {
                    node.grid_template_columns = columns;
                }
                if let Some(auto_rows) = auto_rows {
                    node.grid_auto_rows = auto_rows;
                }
                if let Some(auto_columns) = auto_columns {
                    node.grid_auto_columns = auto_columns;
                }

                status::OK
            })
        }
    })
}

/// Places one child on its parent's grid.
///
/// `row` and `column` are grid lines, counted from one, where a negative counts back from the far
/// edge and `0` leaves the item where the flow would have put it. `row_span` and `column_span` are
/// how many tracks it covers, and `0` is one track. `justify_self` overrides how this one item sits
/// across its cell, in the order `JustifySelf` declares.
///
/// Returns [`status::NO_COMPONENT`] where the entity is gone or carries no node.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_ui_set_grid_placement(
    entity: u64,
    row: i32,
    row_span: i32,
    column: i32,
    column_span: i32,
    justify_self: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, row, row_span, column, column_span, justify_self);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                use bevy::ui::{GridPlacement, JustifySelf, Node};

                let entity = bevy::ecs::entity::Entity::from_bits(entity);
                let Some(mut node) = world.get_mut::<Node>(entity) else {
                    return status::NO_COMPONENT;
                };

                let place = |line: i32, span: i32| {
                    let mut placement = GridPlacement::default();

                    if line != 0 {
                        placement = placement.set_start(line.clamp(
                            i16::MIN as i32 + 1,
                            i16::MAX as i32,
                        ) as i16);
                    }

                    if span > 0 {
                        placement = placement.set_span(span.min(u16::MAX as i32) as u16);
                    }

                    placement
                };

                node.grid_row = place(row, row_span);
                node.grid_column = place(column, column_span);
                node.justify_self = match justify_self {
                    1 => JustifySelf::Start,
                    2 => JustifySelf::End,
                    3 => JustifySelf::Center,
                    4 => JustifySelf::Baseline,
                    5 => JustifySelf::Stretch,
                    _ => JustifySelf::Auto,
                };

                status::OK
            })
        }
    })
}

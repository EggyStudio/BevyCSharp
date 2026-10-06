//! What callers ask of the programs.

use super::*;

// -- What callers ask

/// The program at a number, where there is one.
fn program(world: &World, id: i32) -> Result<(&ShaderPrograms, &Program), i32> {
    let programs = world
        .get_resource::<ShaderPrograms>()
        .ok_or(status::UNSUPPORTED)?;

    let program = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
        .ok_or(status::NO_COMPONENT)?;

    Ok((programs, program))
}

/// Whether a program can run yet: `0` still compiling, `1` ready, `2` failed.
///
/// A failed program may still run, with the last version that compiled or with a fallback, and
/// failed is still the answer, because what is on disk is not what is on screen.
pub fn state(world: &World, id: i32) -> i32 {
    let (programs, program) = match program(world, id) {
        Ok(found) => found,
        Err(refusal) => return refusal,
    };

    let mut compiling = false;
    let mut failed = !program.problem.is_empty();

    for unit in program.stages.iter().flatten() {
        match programs.units[*unit].state {
            UnitState::Compiling => compiling = true,
            UnitState::Failed => failed = true,
            UnitState::Ready => {}
        }
    }

    // A compute stage whose pipeline has not been built counts as compiling, however the shader
    // itself stands, because a dispatch sees it that way.
    if program.stages[Role::Compute as usize].is_some()
        && !compute_ready(id as usize, program.generation)
    {
        compiling = true;
    }

    if failed {
        2
    } else if compiling {
        0
    } else {
        1
    }
}

/// What went wrong with a program, or what its compiler warned about, one stage per paragraph.
pub fn diagnostics(world: &World, id: i32) -> Option<String> {
    let (programs, program) = program(world, id).ok()?;
    let mut text = Vec::new();

    for unit in program.stages.iter().flatten() {
        let unit = &programs.units[*unit];

        if !unit.diagnostics.is_empty() {
            text.push(format!(
                "{} ({}):\n{}",
                unit.path,
                unit.role.describe(),
                unit.diagnostics
            ));
        }
    }

    if !program.problem.is_empty() {
        text.push(program.problem.clone());
    }

    Some(text.join("\n\n"))
}

/// Which files a program is made of.
pub fn describe(world: &World, id: i32) -> Option<String> {
    Some(program(world, id).ok()?.1.description.clone())
}

/// What a program's shaders declare, a line each: its binding, name and kind.
pub fn describe_layout(id: i32) -> Option<String> {
    let entry = lookup(u32::try_from(id).ok()?)?;
    let mut lines = Vec::new();

    for (title, layout) in [
        ("material", &entry.material),
        ("pass", &entry.pass),
        ("compute", &entry.compute),
    ] {
        let Some(layout) = layout else {
            continue;
        };

        lines.push(format!("{title}, group {}:", layout.group));

        for (number, binding) in &layout.bindings {
            match &binding.kind {
                super::super::reflect::BindingKind::Uniform { fields, size } => {
                    lines.push(format!("  {number}: numbers, {size} bytes"));
                    let prefix = if binding.name == super::super::reflect::LOOSE {
                        String::new()
                    } else {
                        format!("{}.", binding.name)
                    };
                    for field in fields {
                        lines.push(format!(
                            "     {prefix}{} {} at {}",
                            field.name,
                            field.ty.describe(),
                            field.offset
                        ));
                    }
                }
                _ => lines.push(format!("  {number}: {} {}", binding.name, binding.describe())),
            }
        }
    }

    Some(lines.join("\n"))
}

/// How many programs there are.
pub fn count(world: &World) -> i32 {
    world
        .get_resource::<ShaderPrograms>()
        .map(|programs| programs.programs.len() as i32)
        .unwrap_or(0)
}

/// How many times the program's shaders have been replaced, counting the first time.
///
/// Only ever grows, so a caller that needs to know when an edit has reached the pipelines reads it
/// before the edit and waits for it to move.
pub fn generation(world: &World, id: i32) -> i32 {
    match program(world, id) {
        Ok((_, program)) => program.generation.min(i32::MAX as u32) as i32,
        Err(refusal) => refusal,
    }
}

/// Compiles every stage of a program again now, whether or not anything changed.
pub fn reload(world: &mut World, id: i32) -> i32 {
    let Some(mut programs) = world.get_resource_mut::<ShaderPrograms>() else {
        return status::UNSUPPORTED;
    };

    let Some(program) = usize::try_from(id)
        .ok()
        .and_then(|id| programs.programs.get(id))
    else {
        return status::NO_COMPONENT;
    };

    let units: Vec<usize> = program.stages.iter().flatten().copied().collect();

    for unit in units {
        if programs.units[unit].busy {
            programs.units[unit].stale = true;
        } else {
            start(&mut programs, unit);
        }
    }

    status::OK
}

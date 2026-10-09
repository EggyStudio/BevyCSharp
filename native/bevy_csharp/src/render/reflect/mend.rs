//! What slangc writes, made what naga 30 and WESL read.
//!
//! Three forms slangc writes that Bevy 0.20's readers refuse, each mended here before anything
//! reads the shader. Naga 30 asks for `@interpolate(flat)` written on every integer passed between
//! stages, where Slang leaves it off the instance index a material passes on, and for
//! `enable wgpu_binding_array;` ahead of a binding array. WESL's parser, which reads a shader the
//! bridge puts Bevy's imports in front of, refuses a parenthesized left-hand side, which WGSL
//! allows and slangc writes for `GetDimensions`. None changes what the shader does.

/// Every mend, in turn.
pub(super) fn mend(wgsl: &str) -> String {
    plain_left_sides(&enable_binding_arrays(&flat_integer_locations(wgsl)))
}

/// Marks `@interpolate(flat)` on every integer `@location` that has no interpolation of its own.
///
/// An integer is never interpolated, so flat is what it was all along, and Bevy's own vertex
/// outputs mark it so, which a fragment shader reading them has to match.
fn flat_integer_locations(wgsl: &str) -> String {
    let mut out = String::with_capacity(wgsl.len() + 64);
    let mut rest = wgsl;

    while let Some(at) = rest.find("@location(") {
        let (before, after) = rest.split_at(at);
        out.push_str(before);

        let Some(close) = after.find(')') else {
            out.push_str(after);
            return out;
        };
        let (attribute, tail) = after.split_at(close + 1);
        out.push_str(attribute);

        // The declaration's other attributes, before this one back to where the declaration
        // began and after it up to its name, and its type, after the colon up to where it ends.
        let begun = out[..out.len() - attribute.len()]
            .rfind([',', '{', ';'])
            .map_or(0, |found| found + 1);
        let earlier = &out[begun..out.len() - attribute.len()];
        let ends = tail.find([',', '}', ')', ';']).unwrap_or(tail.len());
        let declared = &tail[..ends];

        let interpolated = earlier.contains("@interpolate") || declared.contains("@interpolate");
        let integer = declared
            .split_once(':')
            .is_some_and(|(_, kind)| is_integer(kind.trim()));

        if integer && !interpolated {
            out.push_str(" @interpolate(flat)");
        }

        rest = tail;
    }

    out.push_str(rest);
    out
}

/// Whether a WGSL type is an integer or a vector of them.
fn is_integer(kind: &str) -> bool {
    matches!(kind, "u32" | "i32")
        || kind.ends_with("<u32>")
        || kind.ends_with("<i32>")
        || matches!(kind, "vec2u" | "vec3u" | "vec4u" | "vec2i" | "vec3i" | "vec4i")
}

/// Puts `enable wgpu_binding_array;` first where the shader declares a binding array.
fn enable_binding_arrays(wgsl: &str) -> String {
    const ENABLE: &str = "enable wgpu_binding_array;";

    if !wgsl.contains("binding_array<") || wgsl.contains(ENABLE) {
        return wgsl.to_string();
    }

    format!("{ENABLE}\n{wgsl}")
}

/// Writes `((name)) = value;`, where a statement begins, as `name = value;`.
fn plain_left_sides(wgsl: &str) -> String {
    if !wgsl.contains("((") {
        return wgsl.to_string();
    }

    let mut out = String::with_capacity(wgsl.len());
    let mut rest = wgsl;

    while let Some(at) = rest.find("((") {
        let (before, after) = rest.split_at(at);
        out.push_str(before);

        let starts = matches!(out.trim_end().chars().last(), None | Some(';' | '{' | '}'));
        let name_ends = after[2..]
            .find(|c: char| !(c.is_ascii_alphanumeric() || c == '_'))
            .map_or(after.len(), |found| found + 2);
        let name = &after[2..name_ends];
        let assigned = after[name_ends..]
            .strip_prefix("))")
            .map(str::trim_start)
            .is_some_and(|value| value.starts_with('=') && !value.starts_with("=="));

        if starts && !name.is_empty() && assigned {
            out.push_str(name);
            rest = &after[name_ends + 2..];
        } else {
            out.push_str("((");
            rest = &after[2..];
        }
    }

    out.push_str(rest);
    out
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn an_integer_location_is_made_flat_and_no_other() {
        let wgsl = "struct S {\n    @location(0) uv : vec2<f32>,\n    @location(6) instance_index_0 : u32,\n};";
        let mended = flat_integer_locations(wgsl);
        assert!(mended.contains("@location(6) @interpolate(flat) instance_index_0 : u32"));
        assert!(mended.contains("@location(0) uv : vec2<f32>"));
    }

    #[test]
    fn an_interpolation_written_before_or_after_is_kept() {
        for wgsl in [
            "struct S { @interpolate(flat) @location(0) id : u32, };",
            "struct S { @location(0) @interpolate(flat) id : u32, };",
            "fn f(@location(1) @interpolate(flat) id: vec2<u32>) {}",
        ] {
            assert_eq!(flat_integer_locations(wgsl), wgsl);
        }
    }

    #[test]
    fn a_parameter_is_mended_as_a_member_is() {
        let mended = flat_integer_locations("fn f(@builtin(position) p: vec4<f32>, @location(2) id: vec4i) {}");
        assert!(mended.contains("@location(2) @interpolate(flat) id: vec4i"));
    }

    #[test]
    fn a_binding_array_is_enabled_once() {
        let wgsl = "@group(3) @binding(0) var t: binding_array<texture_2d<f32>, 4>;";
        let mended = enable_binding_arrays(wgsl);
        assert!(mended.starts_with("enable wgpu_binding_array;\n"));
        assert_eq!(enable_binding_arrays(&mended), mended);
        assert_eq!(enable_binding_arrays("var x: f32;"), "var x: f32;");
    }

    #[test]
    fn a_parenthesized_left_side_is_written_plain_and_nothing_else_is() {
        let wgsl = "{var dim = textureDimensions((t_0));((width_0)) = dim.x;((height_0)) = dim.y;};";
        assert_eq!(
            plain_left_sides(wgsl),
            "{var dim = textureDimensions((t_0));width_0 = dim.x;height_0 = dim.y;};"
        );

        let untouched = "let a = ((b)) + 1; if (((c)) == d) {} x = f((y));";
        assert_eq!(plain_left_sides(untouched), untouched);
    }
}

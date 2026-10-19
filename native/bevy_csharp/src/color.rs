//! A color between the linear red, green, blue and alpha C# holds a `Color` in and each of Bevy's
//! color spaces, by Bevy's own conversions, so a value in HSL or Okhsl reads as Bevy reads it.

use bevy::color::{
    ColorToComponents, Hsla, Hsva, Hwba, Laba, Lcha, LinearRgba, Okhsla, Okhsva, Okhwba, Oklaba, Oklcha, Srgba, Xyza,
};

use crate::interop::status;

/// Runs `$then` with `$space`'s type as `$color`, by the place C#'s `ColorSpace` gives it.
macro_rules! by_space {
    ($space:expr, $color:ident => $then:expr) => {
        match $space {
            0 => { type $color = Srgba; $then }
            1 => { type $color = LinearRgba; $then }
            2 => { type $color = Hsla; $then }
            3 => { type $color = Hsva; $then }
            4 => { type $color = Hwba; $then }
            5 => { type $color = Laba; $then }
            6 => { type $color = Lcha; $then }
            7 => { type $color = Oklaba; $then }
            8 => { type $color = Oklcha; $then }
            9 => { type $color = Okhsla; $then }
            10 => { type $color = Okhsva; $then }
            11 => { type $color = Okhwba; $then }
            12 => { type $color = Xyza; $then }
            _ => return status::NULL_ARG,
        }
    };
}

/// Writes into `out` the linear red, green, blue and alpha of the color whose four components in
/// the space `space` are `components`, in the order Bevy's type for the space holds them.
///
/// # Safety
/// `components` must be valid for reads of four `f32`s and `out` for writes of four.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_color_from(space: i32, components: *const f32, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        if components.is_null() || out.is_null() {
            return status::NULL_ARG;
        }
        let read = unsafe { [*components, *components.add(1), *components.add(2), *components.add(3)] };
        let linear: LinearRgba = by_space!(space, C => C::from_f32_array(read).into());
        unsafe { core::ptr::copy_nonoverlapping(linear.to_f32_array().as_ptr(), out, 4) };
        status::OK
    })
}

/// Writes into `out` the four components in the space `space` of the color whose linear red,
/// green, blue and alpha are `linear`, in the order Bevy's type for the space holds them.
///
/// # Safety
/// `linear` must be valid for reads of four `f32`s and `out` for writes of four.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_color_to(space: i32, linear: *const f32, out: *mut f32) -> i32 {
    crate::interop::guard(|| {
        if linear.is_null() || out.is_null() {
            return status::NULL_ARG;
        }
        let read = LinearRgba::from_f32_array(unsafe { [*linear, *linear.add(1), *linear.add(2), *linear.add(3)] });
        let components: [f32; 4] = by_space!(space, C => C::from(read).to_f32_array());
        unsafe { core::ptr::copy_nonoverlapping(components.as_ptr(), out, 4) };
        status::OK
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_color_comes_back_through_each_space() {
        let linear = [0.25f32, 0.5, 0.75, 0.6];
        for space in 0..13 {
            let mut there = [0f32; 4];
            let mut back = [0f32; 4];
            unsafe {
                assert_eq!(bcs_color_to(space, linear.as_ptr(), there.as_mut_ptr()), status::OK);
                assert_eq!(bcs_color_from(space, there.as_ptr(), back.as_mut_ptr()), status::OK);
            }
            for (a, b) in linear.iter().zip(back) {
                assert!((a - b).abs() < 1e-3, "space {space} gave {back:?} for {linear:?}");
            }
        }
    }
}

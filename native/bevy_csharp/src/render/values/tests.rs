use super::super::reflect::{Family, reflect};
use super::*;

fn layout() -> Layout {
    reflect(
        include_str!("../fixtures/material_fragment.wgsl"),
        include_str!("../fixtures/material_fragment.json"),
        Family::Material,
    )
    .expect("the fixture reflects")
    .layout
}

fn floats(values: &[f32]) -> Vec<u8> {
    values.iter().flat_map(|value| value.to_ne_bytes()).collect()
}

fn numbers(components: u32, values: &[f32]) -> Value {
    Value::Numbers {
        scalar: Scalar::F32,
        components,
        data: floats(values),
    }
}

#[test]
fn a_value_of_the_right_shape_is_accepted() {
    let layout = layout();

    assert_eq!(check(&layout, "roughness", &numbers(1, &[0.5])), Ok(()));
    assert_eq!(check(&layout, "tint", &numbers(4, &[1.0; 4])), Ok(()));
    assert_eq!(check(&layout, "weights", &numbers(1, &[1.0; 1000])), Ok(()));
    assert_eq!(check(&layout, "twist", &numbers(16, &[0.0; 16])), Ok(()));
    assert_eq!(check(&layout, "lights[2].color", &numbers(3, &[1.0; 3])), Ok(()));
}

#[test]
fn a_wrong_name_says_what_there_is() {
    let error = check(&layout(), "rougness", &numbers(1, &[0.5])).unwrap_err();
    assert!(error.contains("rougness") && error.contains("roughness"), "{error}");
}

#[test]
fn a_wrong_shape_is_refused() {
    let layout = layout();

    assert!(check(&layout, "tint", &numbers(3, &[1.0; 3])).is_err());
    assert!(check(&layout, "weights", &numbers(1, &[1.0; 1001])).is_err());
    assert!(
        check(
            &layout,
            "mode",
            &Value::Numbers {
                scalar: Scalar::F32,
                components: 1,
                data: floats(&[1.0])
            }
        )
        .is_err()
    );
    assert!(check(&layout, "albedo", &numbers(1, &[1.0])).is_err());
}

#[test]
fn an_array_is_written_at_its_stride() {
    let layout = layout();
    let Some(Target::Uniform { offset, ty, .. }) = layout.find("weights") else {
        panic!("weights was not found");
    };

    let mut bytes = vec![0u8; 16200];
    write_numbers(&mut bytes, offset, ty, 1, &floats(&[1.0, 2.0, 3.0]));

    let at = |index: usize| {
        let start = offset as usize + index * 16;
        f32::from_ne_bytes(bytes[start..start + 4].try_into().unwrap())
    };

    assert_eq!((at(0), at(1), at(2)), (1.0, 2.0, 3.0));
}

#[test]
fn a_matrix_is_written_a_padded_row_at_a_time() {
    let ty = FieldType::Matrix(Scalar::F32, 3, 3);
    let mut bytes = vec![0u8; 48];

    write_numbers(&mut bytes, 0, &ty, 9, &floats(&[1., 2., 3., 4., 5., 6., 7., 8., 9.]));

    let at = |byte: usize| f32::from_ne_bytes(bytes[byte..byte + 4].try_into().unwrap());
    assert_eq!((at(0), at(8), at(16), at(32), at(40)), (1.0, 3.0, 4.0, 7.0, 9.0));
}

#[test]
fn an_element_of_an_array_of_textures_is_named_by_index() {
    assert_eq!(element_name("layers", 0), "layers");
    assert_eq!(element_name("layers", 3), "layers[3]");
    assert_eq!(split_index("layers[3]"), ("layers", 3));
    assert_eq!(split_index("albedo"), ("albedo", 0));
}

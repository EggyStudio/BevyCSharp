/// SPIR-V of one decoration naming set `set` and one naming binding `binding`, behind a header.
fn decorated(set: u32, binding: u32) -> Vec<u8> {
    let words: [u32; 13] = [
        0x0723_0203, 0x0001_0500, 0, 16, 0,
        (4 << 16) | 71, 7, 34, set,
        (4 << 16) | 71, 7, 33, binding,
    ];
    words.iter().flat_map(|word| word.to_le_bytes()).collect()
}

fn word(bytes: &[u8], index: usize) -> u32 {
    u32::from_le_bytes(bytes[index * 4..index * 4 + 4].try_into().unwrap())
}

/// The bridge's inputs move from space 101 to group one, and a binding number stays as it was.
#[test]
fn a_descriptor_set_moves_where_the_family_puts_it() {
    let moved = super::remap_descriptor_sets(&decorated(101, 2), super::Family::Compute).unwrap();
    assert_eq!(word(&moved, 8), 1);
    assert_eq!(word(&moved, 12), 2);

    let own = super::remap_descriptor_sets(&decorated(0, 5), super::Family::Material).unwrap();
    assert_eq!(word(&own, 8), 3);
}

#[test]
fn something_that_is_not_spirv_is_refused() {
    assert!(super::remap_descriptor_sets(&[0u8; 20], super::Family::Compute).is_err());
    assert!(super::remap_descriptor_sets(&[1, 2, 3], super::Family::Compute).is_err());
}

/// Everything a SPIR-V compute shader's own group holds is read from the reflection alone,
/// with loose numbers in a uniform buffer, the entry point's unused bindings left out, and the
/// bridge's inputs and Solari's scene noted rather than laid out.
#[test]
fn a_spirv_layout_comes_from_the_reflection() {
    use super::{BindingKind, LOOSE};
    use bevy::render::render_resource::{StorageTextureAccess, TextureFormat, TextureSampleType, TextureViewDimension};

    let reflection = r#"{
        "parameters": [
            {"name": "height", "binding": {"kind": "uniform", "offset": 0, "size": 4},
             "type": {"kind": "scalar", "scalarType": "float32"}},
            {"name": "tint", "binding": {"kind": "uniform", "offset": 16, "size": 16},
             "type": {"kind": "vector", "elementCount": 4, "elementType": {"kind": "scalar", "scalarType": "float32"}}},
            {"name": "hits", "binding": {"kind": "descriptorTableSlot", "index": 1},
             "type": {"kind": "resource", "baseShape": "structuredBuffer", "access": "readWrite"}},
            {"name": "pic", "binding": {"kind": "descriptorTableSlot", "index": 2},
             "type": {"kind": "resource", "baseShape": "texture2D",
                      "resultType": {"kind": "vector", "elementCount": 4, "elementType": {"kind": "scalar", "scalarType": "float32"}}}},
            {"name": "counts", "binding": {"kind": "descriptorTableSlot", "index": 3}, "format": "r32ui",
             "type": {"kind": "resource", "baseShape": "texture2D", "access": "readWrite",
                      "resultType": {"kind": "scalar", "scalarType": "uint32"}}},
            {"name": "many", "binding": {"kind": "descriptorTableSlot", "index": 4},
             "type": {"kind": "array", "elementCount": 8, "elementType": {"kind": "samplerState"}}},
            {"name": "unused", "binding": {"kind": "descriptorTableSlot", "index": 5},
             "type": {"kind": "resource", "baseShape": "structuredBuffer"}},
            {"name": "tlas", "binding": {"kind": "descriptorTableSlot", "space": 2, "index": 5},
             "type": {"kind": "resource", "baseShape": "accelerationStructure"}},
            {"name": "globals", "binding": {"kind": "descriptorTableSlot", "space": 101, "index": 2},
             "type": {"kind": "constantBuffer"}}
        ],
        "globalScope": {"kind": "constantBuffer", "binding": {"kind": "descriptorTableSlot", "index": 0}},
        "entryPoints": [{"name": "main", "bindings": [
            {"name": "hits", "binding": {"used": 1}},
            {"name": "pic", "binding": {"used": 1}},
            {"name": "counts", "binding": {"used": 1}},
            {"name": "many", "binding": {"used": 1}},
            {"name": "unused", "binding": {"used": 0}},
            {"name": "tlas", "binding": {"used": 1}},
            {"name": "globals", "binding": {"used": 1}}
        ]}]
    }"#;

    let reflected =
        super::reflect_spirv(&decorated(101, 2), reflection, super::Family::Compute).unwrap();
    let layout = reflected.layout;

    assert!(layout.spirv);
    assert!(layout.traces_scene);
    assert!(!layout.reads_view, "time alone is not a camera's inputs");
    assert_eq!(word(&reflected.spirv, 8), 1);

    let loose = &layout.bindings[&0];
    assert_eq!(loose.name, LOOSE);
    assert!(matches!(&loose.kind, BindingKind::Uniform { size: 32, fields } if fields.len() == 2));

    assert_eq!(layout.bindings[&1].kind, BindingKind::Storage { read_only: false });
    assert_eq!(
        layout.bindings[&2].kind,
        BindingKind::Texture {
            dimension: TextureViewDimension::D2,
            sample: TextureSampleType::Float { filterable: false },
            multisampled: false,
        }
    );
    assert_eq!(
        layout.bindings[&3].kind,
        BindingKind::StorageTexture {
            dimension: TextureViewDimension::D2,
            format: TextureFormat::R32Uint,
            access: StorageTextureAccess::ReadWrite,
        }
    );
    assert_eq!(layout.bindings[&4].count, Some(8));
    assert!(!layout.bindings.contains_key(&5), "a binding the entry point never uses is left out");
    assert!(layout.find("tint").is_some());
}

/// An acceleration structure of the shader's own is a binding a ray scene is handed to.
#[test]
fn an_acceleration_structure_of_its_own_is_a_binding() {
    let reflection = r#"{"parameters": [
        {"name": "mine", "binding": {"kind": "descriptorTableSlot", "index": 0},
         "type": {"kind": "resource", "baseShape": "accelerationStructure"}}
    ]}"#;

    let layout = super::reflect_spirv(&decorated(0, 0), reflection, super::Family::Compute)
        .unwrap()
        .layout;
    assert_eq!(layout.bindings[&0].kind, super::BindingKind::AccelerationStructure);
    assert!(!layout.traces_scene, "a scene of its own is not Solari's");
}

#[test]
fn a_shuffle_lane_written_signed_is_made_unsigned() {
    let wgsl = "var a : u32 = subgroupShuffle(f(x, y), i32(0));\nvar b : u32 = subgroupShuffleXor(z, u32(1));";
    let fixed = unsigned_shuffle_lanes(wgsl);

    assert!(fixed.contains("subgroupShuffle(f(x, y), u32(0))"));
    assert!(fixed.contains("subgroupShuffleXor(z, u32(1))"));
}

#[test]
fn the_subgroups_enable_is_taken_out_and_nothing_else() {
    let fixed = drop_subgroup_enable("enable subgroups;\nfn main() {}");
    assert_eq!(fixed, "fn main() {}");
}

use super::*;

const FRAGMENT_WGSL: &str = include_str!("../fixtures/material_fragment.wgsl");
const FRAGMENT_JSON: &str = include_str!("../fixtures/material_fragment.json");
const VERTEX_WGSL: &str = include_str!("../fixtures/material_vertex.wgsl");
const VERTEX_JSON: &str = include_str!("../fixtures/material_vertex.json");

fn fragment() -> Reflected {
    reflect(FRAGMENT_WGSL, FRAGMENT_JSON, Family::Material).expect("the fixture reflects")
}

#[test]
fn the_groups_move_to_where_they_belong() {
    let wgsl = fragment().wgsl;

    assert!(wgsl.contains("@binding(1) @group(3) var albedo_0"));
    assert!(wgsl.contains("var layers_0 : binding_array<texture_2d<f32>, 64>"), "{wgsl}");
    assert!(wgsl.contains("@binding(11) @group(0) var<uniform> globals_0"));
    assert!(!wgsl.contains("@group(100)"));
}

#[test]
fn slangs_format_names_become_wgsls() {
    assert_eq!(storage_format_name("r16f").as_deref(), Some("r16float"));
    assert_eq!(storage_format_name("rgba8").as_deref(), Some("rgba8unorm"));
    assert_eq!(storage_format_name("rg32ui").as_deref(), Some("rg32uint"));
    assert_eq!(storage_format_name("r32i").as_deref(), Some("r32sint"));
    assert_eq!(storage_format_name("rgba8_snorm").as_deref(), Some("rgba8snorm"));
    assert_eq!(storage_format_name("r11f_g11f_b10f").as_deref(), Some("rg11b10ufloat"));
    assert_eq!(storage_format_name("unknown"), None);
}

/// An image declared in a format core WGSL lacks gets that format back, and one the shader
/// only writes is bound write-only.
#[test]
fn a_storage_image_is_declared_as_the_shader_meant_it() {
    let wgsl = "@binding(0) @group(0) var picture_0 : texture_storage_2d<rgba32float, read_write>;\n\
                @compute @workgroup_size(1) fn main() { textureStore(picture_0, vec2<i32>(0), vec4<f32>(1.0)); }\n";
    let json = r#"{"parameters":[{"name":"picture","format":"r16f","binding":{"kind":"descriptorTableSlot","index":0},"type":{"kind":"resource","baseShape":"texture2D","access":"readWrite"}}]}"#;

    let reflected = reflect(wgsl, json, Family::Compute).expect("it reflects");

    assert!(
        reflected.wgsl.contains("texture_storage_2d<r16float, write>"),
        "{}",
        reflected.wgsl
    );

    let Some(Binding {
        kind: BindingKind::StorageTexture { format, access, .. },
        ..
    }) = reflected.layout.bindings.get(&0)
    else {
        panic!("the image is not a storage binding");
    };

    assert_eq!(*format, TextureFormat::R16Float);
    assert_eq!(*access, StorageTextureAccess::WriteOnly);
}

/// Bevy's own uniforms stay uniforms, and the material's become storage, which lets them share
/// a bind group with its array of textures.
#[test]
fn the_materials_numbers_are_read_from_storage() {
    let wgsl = fragment().wgsl;

    assert!(wgsl.contains("@group(3) var<storage, read> globalParams_0"), "{wgsl}");
    assert!(wgsl.contains("@group(3) var<storage, read> sun_0"));
    assert!(!wgsl.contains("@group(3) var<uniform>"));
}

#[test]
fn every_global_the_shader_reads_is_bound_by_name() {
    let layout = fragment().layout;

    let names: Vec<_> = layout.bindings.values().map(|b| b.name.as_str()).collect();
    assert_eq!(
        names,
        [LOOSE, "albedo", "albedo_sampler", "layers", "skies", "fog", "points", "raw", "sun"]
    );

    assert_eq!(layout.bindings[&3].count, Some(64));
    assert_eq!(layout.bindings[&4].count, Some(16));
    assert!(matches!(
        layout.bindings[&4].kind,
        BindingKind::Texture {
            dimension: TextureViewDimension::Cube,
            ..
        }
    ));
    assert!(matches!(
        layout.bindings[&5].kind,
        BindingKind::Texture {
            dimension: TextureViewDimension::D3,
            ..
        }
    ));
    assert!(matches!(
        layout.bindings[&6].kind,
        BindingKind::Storage { read_only: true }
    ));
}

#[test]
fn a_loose_number_is_found_at_its_offset() {
    let layout = fragment().layout;

    let Some(Target::Uniform { binding, offset, ty }) = layout.find("tint") else {
        panic!("tint was not found");
    };
    assert_eq!((binding, offset), (0, 16));
    assert_eq!(ty, &FieldType::Vector(Scalar::F32, 4));

    let Some(Target::Uniform { offset, .. }) = layout.find("weights[999]") else {
        panic!("weights[999] was not found");
    };
    assert_eq!(offset, 32 + 999 * 16);

    let Some(Target::Uniform { offset, ty, .. }) = layout.find("lights[3].power") else {
        panic!("lights[3].power was not found");
    };
    assert_eq!(offset, 16032 + 3 * 16 + 12);
    assert_eq!(ty, &FieldType::Scalar(Scalar::F32));

    assert!(matches!(
        layout.find("twist"),
        Some(Target::Uniform {
            ty: FieldType::Matrix(Scalar::F32, 4, 4),
            ..
        })
    ));
    assert!(layout.find("weights[1000]").is_none());
    assert!(layout.find("nothing").is_none());
}

#[test]
fn a_constant_buffer_field_is_found_under_the_buffer() {
    let layout = fragment().layout;

    assert!(matches!(
        layout.find("sun.power"),
        Some(Target::Uniform {
            binding: 8,
            offset: 12,
            ..
        })
    ));
    assert!(matches!(
        layout.find("albedo"),
        Some(Target::Resource { binding: 1, .. })
    ));
}

#[test]
fn the_stages_merge_into_one_layout() {
    let mut layout = reflect(VERTEX_WGSL, VERTEX_JSON, Family::Material)
        .expect("the vertex fixture reflects")
        .layout;

    layout.merge(&fragment().layout).expect("the stages agree");

    assert_eq!(layout.bindings.len(), 9);
    assert_eq!(
        layout.entries(ShaderStages::VERTEX_FRAGMENT).len(),
        layout.bindings.len()
    );
}

#[test]
fn a_binding_to_a_group_nothing_binds_is_refused() {
    let wgsl = "@group(7) @binding(0) var<uniform> stray: vec4<f32>;\n@fragment fn fragment() -> @location(0) vec4<f32> { return stray; }";
    let error = reflect(wgsl, "{}", Family::Material).unwrap_err();
    assert!(error.contains("group 7"), "{error}");
}

#[test]
fn the_rewritten_wgsl_is_valid() {
    use naga::valid::{Capabilities, ValidationFlags, Validator};

    let module = naga::front::wgsl::parse_str(&fragment().wgsl).expect("it parses");

    Validator::new(ValidationFlags::all(), Capabilities::all())
        .validate(&module)
        .expect("naga accepts it");
}

#[test]
fn a_name_loses_the_number_slang_adds() {
    assert_eq!(source_name("albedo_0"), "albedo");
    assert_eq!(source_name("albedo_sampler_12"), "albedo_sampler");
    assert_eq!(source_name("plain"), "plain");
}

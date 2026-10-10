use super::*;

#[test]
fn a_false_define_is_left_out() {
    assert_eq!(Define::Bool("A".into(), false).to_slang(), None);
    assert_eq!(
        Define::Bool("A".into(), true).to_slang(),
        Some(("A".into(), "1".into()))
    );
    assert_eq!(
        Define::Int("B".into(), -3).to_slang(),
        Some(("B".into(), "-3".into()))
    );
}

#[test]
fn every_fallback_has_the_entry_point_it_was_asked_for() {
    for role in Role::ALL {
        assert!(fallback_source(role, "custom_entry").text().contains("fn custom_entry("));
    }
}

#[test]
fn each_role_puts_its_globals_where_its_pipeline_binds_them() {
    assert_eq!(Role::Fragment.family().own_group(), 3);
    assert_eq!(Role::PrepassVertex.family().own_group(), 3);
    assert_eq!(Role::Pass.family().own_group(), 0);
    assert_eq!(Role::Compute.family().own_group(), 0);
}

#[test]
fn a_fragment_shader_is_given_the_preludes_it_calls_and_no_others() {
    let decals = "fn fragment() { let n = bcs_decal_count(a, b); }".to_string();
    let both = "fn fragment() { bcs_decal_tag(a, b, 0u); bcs_pbr_light(); }".to_string();

    let given = with_bevy(Role::Fragment, decals.clone());
    assert!(given.text().contains("fn bcs_decal_seek(") && !given.text().contains("fn bcs_pbr_light("));
    assert!(with_bevy(Role::Fragment, both.clone()).text().contains("fn bcs_pbr_light("));
    assert_eq!(with_bevy(Role::Vertex, decals.clone()).text(), decals);
    assert_eq!(with_bevy(Role::Fragment, "fn fragment() {}".into()).text(), "fn fragment() {}");

    let stand_ins = super::stand_ins(&both).expect("a shader calling both is read with stand-ins");
    assert!(stand_ins.contains("fn bcs_decal_tag(") && stand_ins.contains("fn bcs_pbr_light("));
    assert!(super::stand_ins("fn fragment() {}").is_none());
}

#[test]
fn the_mesh_stand_ins_are_mesh_shaders_naga_reads() {
    for role in [Role::DrawTask, Role::DrawMesh] {
        let text = fallback_source(role, "entry").text().to_string();
        naga::front::wgsl::parse_str(&text).unwrap_or_else(|error| panic!("{role:?}: {error}"));
    }
}

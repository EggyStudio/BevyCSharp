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
        assert!(fallback_source(role, "custom_entry").contains("fn custom_entry("));
    }
}

#[test]
fn each_role_puts_its_globals_where_its_pipeline_binds_them() {
    assert_eq!(Role::Fragment.family().own_group(), 3);
    assert_eq!(Role::PrepassVertex.family().own_group(), 3);
    assert_eq!(Role::Pass.family().own_group(), 0);
    assert_eq!(Role::Compute.family().own_group(), 0);
}

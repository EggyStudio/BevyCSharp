use bevy::prelude::*;

fn main() {
    App::new()
        .add_plugins(DefaultPlugins)
        .add_systems(Startup, setup)
        .add_systems(Update, snap)
        .run();
}

fn setup(
    mut c: Commands,
    mut meshes: ResMut<Assets<Mesh>>,
    mut mats: ResMut<Assets<StandardMaterial>>,
) {
    c.spawn((
        Mesh3d(meshes.add(Cuboid::new(1.6, 1.6, 1.6))),
        MeshMaterial3d(mats.add(Color::srgb(0.25, 0.55, 0.85))),
        Transform::from_xyz(0.0, 0.0, 0.0),
    ));
    c.spawn((
        Mesh3d(meshes.add(Plane3d::default().mesh().size(24.0, 24.0))),
        MeshMaterial3d(mats.add(Color::srgb(0.8, 0.1, 0.1))),
        Transform::from_xyz(0.0, -1.2, 0.0),
    ));
    c.spawn((
        DirectionalLight { shadow_maps_enabled: true, ..default() },
        Transform::from_xyz(4.0, 8.0, 5.0).looking_at(Vec3::ZERO, Vec3::Y),
    ));
    c.spawn((
        Camera3d::default(),
        Transform::from_xyz(3.5, 3.0, 6.0).looking_at(Vec3::ZERO, Vec3::Y),
    ));
}

fn snap(mut c: Commands, mut n: Local<u32>) {
    *n += 1;
    if *n == 45 {
        c.spawn(bevy::render::view::screenshot::Screenshot::primary_window())
            .observe(bevy::render::view::screenshot::save_to_disk(
                std::env::var("BCS_SHOT").unwrap_or_else(|_| "/tmp/ref.png".into())));
    }
    if *n == 90 { c.write_message(AppExit::Success); }
}

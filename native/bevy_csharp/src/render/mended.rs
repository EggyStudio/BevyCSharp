//! Bevy's shaders mended where they draw wrong, by exact replacements in the text Bevy ships.
//!
//! Each mend is a few replacements made in the shader once it has loaded, each of which has to
//! find its text exactly once, so a release of Bevy whose text has moved refuses the mend, says so
//! and leaves Bevy's shader as it is, rather than drawing a shader mended from text it was not
//! written for. `bcs_render_shader_mends` answers whether each took, which a test holds, so a bump
//! of Bevy that refuses one fails there and the mend follows the release. No copy of Bevy's shader
//! is kept here, only the lines a mend changes.

use bevy::app::App;
use bevy::asset::{AssetServer, Assets, Handle};
use bevy::ecs::resource::Resource;
use bevy::ecs::system::{Res, ResMut};
use bevy::shader::{Shader, Source};

use crate::interop::status;
use crate::state::with_world;

/// Where `bevy_pbr` loads the fragment shader of its standard material from.
const PBR_FRAGMENT: &str = "embedded://bevy_pbr/render/pbr_fragment.wesl";

/// The mend of the standard material's anisotropy under a normal prepass, in Bevy 0.20's
/// `render/pbr_fragment.wesl`.
///
/// A camera with a normal prepass draws a forward pixel once, and Bevy's shader loads the normal
/// the prepass wrote and skips the block that builds the tangent frame, which holds anisotropy's
/// setup as well, so the stretch's tangent and bitangent stay at zero and the highlight's
/// distribution rises without bound toward the sphere's edge, blown white. The mend closes that
/// block before anisotropy's setup and gives the setup a tangent frame of its own, so it runs
/// whether the normal is the prepass's or the mesh's, and the closing brace the setup was inside
/// goes.
const ANISOTROPY: [(&str, &str); 3] = [
    (
        "\n        // Take anisotropy into account.\n",
        "\n}\n\n        // Take anisotropy into account.\n",
    ),
    (
        "        let anisotropy_T = normalize(TBN * vec3(anisotropy_direction, 0.0));\n",
        "        let anisotropy_TBN = pbr_functions::calculate_tbn_mikktspace(pbr_input.world_normal, in.world_tangent);\n        let anisotropy_T = normalize(anisotropy_TBN * vec3(anisotropy_direction, 0.0));\n",
    ),
    (
        "        pbr_input.anisotropy_B = anisotropy_B;\n\n}\n\n}\n\n// TODO: Meshlet support\n",
        "        pbr_input.anisotropy_B = anisotropy_B;\n\n}\n\n// TODO: Meshlet support\n",
    ),
];

/// Whether each mend has been made, refused, or waits for its shader to load.
#[derive(Resource, Default)]
pub struct ShaderMends {
    pbr_fragment: Option<Handle<Shader>>,
    anisotropy: Option<bool>,
}

pub(crate) fn install(app: &mut App) {
    app.init_resource::<ShaderMends>();
    app.add_systems(bevy::app::PreUpdate, mend);
}

/// The text with every replacement made, or nothing where one does not find its text exactly once.
pub(crate) fn mended(original: &str, replacements: &[(&str, &str)]) -> Option<String> {
    let mut text = original.to_owned();
    for (from, to) in replacements {
        if text.matches(from).count() != 1 {
            return None;
        }
        text = text.replacen(from, to, 1);
    }
    Some(text)
}

/// Mends the fragment shader once it has loaded, the first frame it is there.
///
/// A pipeline compiled from it before is compiled again, as for a shader written while the game
/// runs, which the shader's change says to the render world.
fn mend(server: Res<AssetServer>, mut shaders: ResMut<Assets<Shader>>, mut mends: ResMut<ShaderMends>) {
    if mends.anisotropy.is_some() {
        return;
    }
    if mends.pbr_fragment.is_none() {
        mends.pbr_fragment = server.get_handle::<Shader>(PBR_FRAGMENT);
    }
    let Some(handle) = mends.pbr_fragment.clone() else {
        return;
    };
    let Some(shader) = shaders.get(&handle) else {
        return;
    };

    match mended(shader.source.as_str(), &ANISOTROPY) {
        Some(text) => {
            if let Some(mut shader) = shaders.get_mut(&handle) {
                shader.source = Source::Wesl(text.into());
            }
            mends.anisotropy = Some(true);
        }
        None => {
            bevy::log::warn!(
                "Bevy's {PBR_FRAGMENT} is not the text the bridge's mend of anisotropy under a normal \
                 prepass was written for, so it is left as Bevy ships it, and an anisotropic material \
                 drawn forward under a normal prepass comes out blown white. The mend in \
                 render/mended.rs follows the new text."
            );
            mends.anisotropy = Some(false);
        }
    }
}

/// Writes whether each of Bevy's shaders the bridge mends was mended, as a bit a mend, the first
/// the standard material's anisotropy under a normal prepass, and a second bit set where it was
/// refused, its text not the one the mend was written for. Nothing set is a mend whose shader has
/// not loaded yet.
///
/// # Safety
/// `mends` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_shader_mends(mends: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if mends.is_null() {
            return status::NULL_ARG;
        }
        with_world(|world| {
            let bits = match world.get_resource::<ShaderMends>().and_then(|mends| mends.anisotropy) {
                Some(true) => 1,
                Some(false) => 2,
                None => 0,
            };
            unsafe { mends.write(bits) };
            status::OK
        })
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_mend_is_made_where_each_text_is_found_once_and_refused_elsewhere() {
        let original = "a\nb\nc\n";
        assert_eq!(mended(original, &[("b\n", "B\n"), ("c\n", "C\n")]).as_deref(), Some("a\nB\nC\n"));
        assert_eq!(mended(original, &[("d\n", "D\n")]), None);
        assert_eq!(mended("b\nb\n", &[("b\n", "B\n")]), None);
    }
}

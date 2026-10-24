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

/// Where `bevy_pbr` loads the functions that write and read its deferred G-buffer from.
const DEFERRED_FUNCTIONS: &str = "embedded://bevy_pbr/deferred/functions.wesl";

/// The mend of an exposed emissive drawn deferred, in Bevy 0.20's `deferred/functions.wesl`.
///
/// A material's emissive alpha says how much of the camera's exposure its light takes, all of it at
/// 1, Bevy's default, and none at 0. Drawn forward, Bevy's lighting applies that. Drawn deferred, the
/// G-buffer keeps the emissive's color alone, and the lighting pass reads it back with an alpha of
/// 0, so an exposed emissive is shown unexposed, a lamp panel of 40,000 nits at about 40,000 where
/// forward it shows about 40 at Bevy's default exposure. The mend applies the exposure as the
/// G-buffer is written, by the share of it the alpha sets, so the light read back is the light
/// forward shows, as Bevy writes a lightmap's light into the same place times the exposure.
const EMISSIVE_EXPOSURE: [(&str, &str); 1] = [(
    "    var emissive = in.material.emissive.rgb;\n",
    "    var emissive = in.material.emissive.rgb * mix(1.0, view.exposure, in.material.emissive.a);\n",
)];

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
    deferred_functions: Option<Handle<Shader>>,
    emissive_exposure: Option<bool>,
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

/// Mends each shader once it has loaded, the first frame it is there.
///
/// A pipeline compiled from it before is compiled again, as for a shader written while the game
/// runs, which the shader's change says to the render world.
fn mend(server: Res<AssetServer>, mut shaders: ResMut<Assets<Shader>>, mut mends: ResMut<ShaderMends>) {
    let mends = &mut *mends;
    mend_one(
        &server,
        &mut shaders,
        PBR_FRAGMENT,
        &ANISOTROPY,
        &mut mends.pbr_fragment,
        &mut mends.anisotropy,
        "an anisotropic material drawn forward under a normal prepass comes out blown white",
    );
    mend_one(
        &server,
        &mut shaders,
        DEFERRED_FUNCTIONS,
        &EMISSIVE_EXPOSURE,
        &mut mends.deferred_functions,
        &mut mends.emissive_exposure,
        "an exposed emissive drawn deferred is shown unexposed, as the field's shadows and global illumination draw",
    );
}

/// Makes one mend in the shader at `path` once it has loaded, or refuses it where the text is not
/// the one it was written for, saying what then draws wrong.
fn mend_one(
    server: &AssetServer,
    shaders: &mut Assets<Shader>,
    path: &'static str,
    replacements: &[(&str, &str)],
    handle: &mut Option<Handle<Shader>>,
    made: &mut Option<bool>,
    wrong: &str,
) {
    if made.is_some() {
        return;
    }
    if handle.is_none() {
        *handle = server.get_handle::<Shader>(path);
    }
    let Some(shader_handle) = handle.clone() else {
        return;
    };
    let Some(shader) = shaders.get(&shader_handle) else {
        return;
    };

    match mended(shader.source.as_str(), replacements) {
        Some(text) => {
            if let Some(mut shader) = shaders.get_mut(&shader_handle) {
                shader.source = Source::Wesl(text.into());
            }
            *made = Some(true);
        }
        None => {
            bevy::log::warn!(
                "Bevy's {path} is not the text the bridge's mend of it was written for, so it is left as \
                 Bevy ships it, and {wrong}. The mend in render/mended.rs follows the new text."
            );
            *made = Some(false);
        }
    }
}

/// Writes whether each of Bevy's shaders the bridge mends was mended, two bits a mend, the first
/// set where it was made and the second where it was refused, its text not the one the mend was
/// written for, the standard material's anisotropy under a normal prepass in bits 0 and 1 and an
/// exposed emissive drawn deferred in bits 2 and 3. Neither set is a mend whose shader has not
/// loaded yet.
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
            let bits_of = |made: Option<bool>| match made {
                Some(true) => 1u32,
                Some(false) => 2,
                None => 0,
            };
            let bits = world
                .get_resource::<ShaderMends>()
                .map_or(0, |mends| bits_of(mends.anisotropy) | bits_of(mends.emissive_exposure) << 2);
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

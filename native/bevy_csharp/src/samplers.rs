//! The samplers images are drawn with, the one Bevy's default and a glTF file's textures take and
//! the one an image's settings ask for, which share the rule that a texture whose three filters are
//! linear is filtered at sixteen samples at a slant.

/// The most samples a texture seen at a slant takes, which every texture is filtered with where its
/// filters are linear, at every quality and with nothing to turn it down, since the cost is small
/// and a floor or a wall seen at a grazing angle reads sharp to its far end where one sample a level
/// smears it. It is the most wgpu takes, and a device that offers fewer takes its own most.
pub const TEXTURE_ANISOTROPY: u16 = 16;

/// The sampler an image left at Bevy's default is drawn with, and a glTF file's textures, Bevy's
/// linear sampler filtered at `TEXTURE_ANISOTROPY` samples. With anisotropy above one Bevy's glTF
/// loader keeps the three filters linear in place of the file's, as wgpu asks, so a model's
/// textures take it too, and its wrapping as the file says.
#[cfg(feature = "render")]
pub fn textured_sampler() -> bevy::image::ImageSamplerDescriptor {
    bevy::image::ImageSamplerDescriptor {
        anisotropy_clamp: TEXTURE_ANISOTROPY,
        ..bevy::image::ImageSamplerDescriptor::linear()
    }
}

/// The sampler an image's settings ask for, the same whether the image is loaded with them or
/// given them once it exists.
pub(crate) fn sampler_from(config: &crate::interop::BcsImageConfig) -> bevy::image::ImageSamplerDescriptor {
    use bevy::image::{ImageAddressMode, ImageFilterMode, ImageSamplerDescriptor};

    let address = |mode: i32| match mode {
        1 => ImageAddressMode::Repeat,
        2 => ImageAddressMode::MirrorRepeat,
        _ => ImageAddressMode::ClampToEdge,
    };
    let filter = |mode: i32| match mode {
        1 => ImageFilterMode::Linear,
        _ => ImageFilterMode::Nearest,
    };

    let mag_filter = filter(config.mag_filter);
    let min_filter = filter(config.min_filter);
    let mipmap_filter = filter(config.mipmap_filter);

    // wgpu rejects anisotropy above one unless all three filters are linear, and rejects it as a
    // validation failure rather than by ignoring it. Asking for both is a mistake worth absorbing
    // here rather than turning into a crash at draw time.
    let anisotropy = if mag_filter == ImageFilterMode::Linear
        && min_filter == ImageFilterMode::Linear
        && mipmap_filter == ImageFilterMode::Linear
    {
        config.anisotropy.max(1)
    } else {
        1
    };

    ImageSamplerDescriptor {
        address_mode_u: address(config.address_u),
        address_mode_v: address(config.address_v),
        mag_filter,
        min_filter,
        mipmap_filter,
        anisotropy_clamp: anisotropy as u16,
        ..Default::default()
    }
}

#[cfg(test)]
mod tests {
    #[cfg(feature = "render")]
    #[test]
    fn a_texture_left_at_the_default_takes_sixteen_samples_with_its_filters_linear() {
        use bevy::image::ImageFilterMode;

        // wgpu refuses anisotropy above one unless all three filters are linear.
        let sampler = super::textured_sampler();
        assert_eq!(16, sampler.anisotropy_clamp);
        assert_eq!(ImageFilterMode::Linear, sampler.mag_filter);
        assert_eq!(ImageFilterMode::Linear, sampler.min_filter);
        assert_eq!(ImageFilterMode::Linear, sampler.mipmap_filter);
    }
}

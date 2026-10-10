using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Everything a physically based material is made of.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeMaterialConfig
{
    /// <summary>Base color red, linear.</summary>
    public float BaseR;

    /// <summary>Base color green, linear.</summary>
    public float BaseG;

    /// <summary>Base color blue, linear.</summary>
    public float BaseB;

    /// <summary>Opacity.</summary>
    public float BaseA;

    /// <summary>Zero for a dielectric, one for a metal.</summary>
    public float Metallic;

    /// <summary>Near zero for a mirror, one for matte.</summary>
    public float Roughness;

    /// <summary>Emissive red, linear.</summary>
    public float EmissiveR;

    /// <summary>Emissive green, linear.</summary>
    public float EmissiveG;

    /// <summary>Emissive blue, linear.</summary>
    public float EmissiveB;

    /// <summary>1 to scale the emission by the camera's exposure, 0 to leave it alone.</summary>
    public float EmissiveA;

    /// <summary>0 opaque, 1 masked, 2 blended, 3 additive.</summary>
    public int AlphaMode;

    /// <summary>Cut-off for a masked material.</summary>
    public float AlphaCutoff;

    /// <summary>Non-zero to draw back faces too.</summary>
    public int DoubleSided;

    /// <summary>Non-zero to skip lighting.</summary>
    public int Unlit;

    /// <summary>Asset key of the base color map, or -1.</summary>
    public int BaseColorTexture;

    /// <summary>Asset key of the normal map, or -1.</summary>
    public int NormalMap;

    /// <summary>Asset key of the metallic-roughness map, or -1.</summary>
    public int MetallicRoughnessTexture;

    /// <summary>Asset key of the emissive map, or -1.</summary>
    public int EmissiveTexture;

    /// <summary>Asset key of the ambient occlusion map, or -1.</summary>
    public int OcclusionTexture;

    /// <summary>Texture repeats across the surface, in U.</summary>
    public float UvScaleX;

    /// <summary>Texture repeats across the surface, in V.</summary>
    public float UvScaleY;

    /// <summary>Radians the texture is turned by.</summary>
    public float UvRotation;

    /// <summary>Texture shift, in U.</summary>
    public float UvOffsetX;

    /// <summary>Texture shift, in V.</summary>
    public float UvOffsetY;

    /// <summary>How much light a dielectric reflects head on.</summary>
    public float Reflectance;

    /// <summary>How strong a clear varnish over the surface is.</summary>
    public float Clearcoat;

    /// <summary>How rough that varnish is.</summary>
    public float ClearcoatRoughness;

    /// <summary>How much light passes straight through.</summary>
    public float SpecularTransmission;

    /// <summary>How much light passes through and scatters.</summary>
    public float DiffuseTransmission;

    /// <summary>How thick the material is where light passes through.</summary>
    public float Thickness;

    /// <summary>How much light bends passing in.</summary>
    public float Ior;

    /// <summary>How far light travels inside before it takes on the attenuation color.</summary>
    public float AttenuationDistance;

    /// <summary>Attenuation color red, linear.</summary>
    public float AttenuationR;

    /// <summary>Attenuation color green, linear.</summary>
    public float AttenuationG;

    /// <summary>Attenuation color blue, linear.</summary>
    public float AttenuationB;

    /// <summary>Attenuation color alpha.</summary>
    public float AttenuationA;

    /// <summary>How much the highlight stretches.</summary>
    public float AnisotropyStrength;

    /// <summary>Radians the stretch is turned by.</summary>
    public float AnisotropyRotation;

    /// <summary>Asset key of the clearcoat map, or -1.</summary>
    public int ClearcoatTexture;

    /// <summary>Asset key of the clearcoat roughness map, or -1.</summary>
    public int ClearcoatRoughnessTexture;

    /// <summary>Asset key of the clearcoat normal map, or -1.</summary>
    public int ClearcoatNormalTexture;

    /// <summary>Asset key of the transmission map, or -1.</summary>
    public int SpecularTransmissionTexture;

    /// <summary>Asset key of the diffuse transmission map, or -1.</summary>
    public int DiffuseTransmissionTexture;

    /// <summary>Asset key of the thickness map, or -1.</summary>
    public int ThicknessTexture;

    /// <summary>Asset key of the anisotropy map, or -1.</summary>
    public int AnisotropyTexture;

    /// <summary>What a baked lightmap's values are multiplied by, in nits.</summary>
    public float LightmapExposure;

    /// <summary>Asset key of the height map parallax mapping reads, or -1.</summary>
    public int DepthMap;

    /// <summary>How deep the depth map's white is, as a share of the texture's width.</summary>
    public float ParallaxDepthScale;

    /// <summary>0 for parallax occlusion mapping, 1 for relief mapping.</summary>
    public int ParallaxMethod;

    /// <summary>How many steps relief mapping's search takes at most.</summary>
    public uint ReliefSteps;

    /// <summary>How many layers the depth map is cut into at most.</summary>
    public float ParallaxLayers;

    /// <summary>Specular tint red, linear.</summary>
    public float SpecularTintR;

    /// <summary>Specular tint green, linear.</summary>
    public float SpecularTintG;

    /// <summary>Specular tint blue, linear.</summary>
    public float SpecularTintB;

    /// <summary>Specular tint alpha.</summary>
    public float SpecularTintA;

    /// <summary>Asset key of the reflectance map, or -1.</summary>
    public int SpecularTexture;

    /// <summary>Asset key of the specular tint map, or -1.</summary>
    public int SpecularTintTexture;

    /// <summary>0 as every other material, 1 forward, 2 deferred.</summary>
    public int OpaqueRenderMethod;
}

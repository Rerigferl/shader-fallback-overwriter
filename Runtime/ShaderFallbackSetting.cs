using UnityEngine;
using VRC.SDKBase;

namespace Numeira
{
    [AddComponentMenu("NDMF/Shader Fallback Setting")]
    public sealed class ShaderFallbackSetting : MonoBehaviour, IEditorOnly
    {
        public InheritMode Inherit;

        public FallbackShaderType ShaderType = FallbackShaderType.Unlit;
        public FallbackRenderType RenderType = FallbackRenderType.Opaque;
        public FallbackCullType CullType = FallbackCullType.Default;

        public MaterialListMode ListMode = MaterialListMode.None;
        public Material[] Materials;
    }


    public enum InheritMode
    {
        Inherit,
        Set,
        Coalesce,
        DontSet
    }

    public enum FallbackShaderType : uint
    {
        Hidden,
        Unlit,
        Standard,
        VertexLit,
        Toon,
        Particle,
        Sprite,
        Matcap,
        MobileToon,
    }

    public enum FallbackRenderType : uint
    {
        Opaque,
        Cutout,
        Transparent,
        Fade,
    }

    public enum FallbackCullType : uint
    {
        Default,
        DoubleSided
    }

    public enum MaterialListMode
    {
        None,
        Whitelist,
        Blacklist,
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using VRC.SDKBase;

namespace Numeira
{
    [AddComponentMenu("NDMF/Shader Fallback Setting")]
    public sealed class ShaderFallbackSetting : MonoBehaviour, IEditorOnly
    {
        public InheritMode Inherit;

        public SetOrCoalesce ShaderTypeMode = SetOrCoalesce.Set;
        public ShaderType ShaderType = ShaderType.Unlit;

        public SetOrCoalesce RenderTypeMode = SetOrCoalesce.Set;
        public RenderType RenderType = RenderType.Opaque;

        public SetOrCoalesce CullTypeMode = SetOrCoalesce.Set;
        public CullType CullType = CullType.Default;

        public MaterialListMode ListMode = MaterialListMode.None;
        public Material[] Materials;

        public (ShaderType Shader, RenderType Render, CullType Cull)? GetSettings(Material material = null)
        {
            bool flag = material == null || ListMode switch
            {
                MaterialListMode.Whitelist => Materials.AsSpan().Find(material),
                MaterialListMode.Blacklist => !Materials.AsSpan().Find(material),
                _ => true,
            };

            var parent = transform.parent?.GetComponentInParent<ShaderFallbackSetting>()?.GetSettings(material);

            if (!flag || Inherit == InheritMode.Inherit || (Inherit == InheritMode.Coalesce && parent != null))
                return parent;

            if (Inherit == InheritMode.DontSet)
                return null;

            var settings = (ShaderType, RenderType, CullType);
            if (parent is { } p)
            {
                if (ShaderTypeMode != SetOrCoalesce.Set)
                    settings.ShaderType = p.Shader;

                if (RenderTypeMode != SetOrCoalesce.Set)
                    settings.RenderType = p.Render;

                if (CullTypeMode != SetOrCoalesce.Set)
                    settings.CullType = p.Cull;
            }

            return settings;
        }
    }

    public enum SetOrCoalesce
    {
        Set = 1,
        Coalesce
    }

    public enum InheritMode
    {
        Inherit,
        Set,
        Coalesce,
        DontSet
    }

    public enum ShaderType : uint
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

    public enum RenderType : uint
    {
        Opaque,
        Cutout,
        Transparent,
        Fade,
    }

    public enum CullType : uint
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
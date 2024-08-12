using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using Numeira;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(ShaderFallbackOverwriterCore))]

namespace Numeira
{

    public sealed class ShaderFallbackOverwriterCore : Plugin<ShaderFallbackOverwriterCore>
    {
        public override string DisplayName => "Shader Fallback Overwriter";
        public override string QualifiedName => "numeira.shader-fallback-overwriter";

        private static readonly List<ShaderFallbackSetting> componentsList = new();

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar").Run("Shader Fallback Setting", context =>
            {
                if (context.AvatarRootObject.GetComponentInChildren<ShaderFallbackSetting>() == null)
                    return;

                var rs = context.AvatarRootTransform.GetComponentsInChildren<Renderer>(true);
                Dictionary<(Material Mat, string Tag), Material> materialCache = new();

                var rand = new System.Random();
                foreach (var renderer in rs)
                {
                    var materials = renderer.sharedMaterials;
                    foreach(ref var material in materials.AsSpan())
                    {
                        if (material == null)
                            continue;

                        if (!ObjectRegistry.GetReference(material).TryResolve(context.ErrorReport, out var original))
                        {
                            original = material;
                        }
                        var tag = ResolveFallbackTag(renderer.gameObject, original as Material);
                        if (tag == null)
                            continue;

                        var currentTag = material.GetTag("VRCFallback", true, null);
                        if (currentTag == tag)
                            continue;

                        if (materialCache.TryGetValue((material, tag), out var cloned))
                        {
                            material = cloned;
                            continue;
                        }

                        cloned = Object.Instantiate(material);

                        AssetDatabase.AddObjectToAsset(cloned, context.AssetContainer);
                        cloned.name = $"{material.name}({tag})";
                        ObjectRegistry.RegisterReplacedObject(material, cloned);
                        materialCache.Add((material, tag), cloned);
                        material = cloned;

                        material.SetOverrideTag("VRCFallback", tag);

                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out var guid, out long localId);
                        Debug.LogError($"{material.name} {tag} {guid} {localId}");
                    }
                    renderer.sharedMaterials = materials;
                }

                context.AvatarRootObject.GetComponentsInChildren(true, componentsList);
                foreach(var component in componentsList.AsSpan() )
                {
                    Object.DestroyImmediate(component);
                }
            });
        }

        private static string ResolveFallbackTag(GameObject obj, Material material)
        {
            obj.GetComponentsInParent(true, componentsList);
            var components = componentsList.AsSpan();
            if (components.IsEmpty)
                return null;

            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];

                bool flag = component.ListMode switch
                {
                    MaterialListMode.Whitelist => component.Materials.AsSpan().Find(material),
                    MaterialListMode.Blacklist => !component.Materials.AsSpan().Find(material),
                    _ => true,
                };

                if (!flag)
                    continue;

                if (component.Inherit is InheritMode.DontSet)
                    return null;

                if (component.Inherit is InheritMode.Inherit)
                    continue;

                if (component.Inherit is InheritMode.Set)
                    return GetFallbackTagString(component.ShaderType, component.RenderType, component.CullType);

                if (component.Inherit is InheritMode.Coalesce)
                    if (!components.Skip(i).Find(InheritMode.Set))
                        return GetFallbackTagString(component.ShaderType, component.RenderType, component.CullType);
            }

            return null;
        }

        private static string GetFallbackTagString(FallbackShaderType shader, FallbackRenderType render = FallbackRenderType.Opaque, FallbackCullType cull = FallbackCullType.Default)
        {
            return string.Concat(
                EnumExt<FallbackShaderType>.Names[(int)shader],
                (shader is FallbackShaderType.Toon or FallbackShaderType.Unlit) ? EnumExt<FallbackRenderType>.Names[(int)render] : "",
                shader is FallbackShaderType.Toon or FallbackShaderType.Unlit && cull is FallbackCullType.DoubleSided ? "DoubleSided" : "");
        }
    }

    [CustomEditor(typeof(ShaderFallbackSetting))]
    public sealed class ShaderFallbackSettingEditor : Editor
    {
        private SerializedProperty Inherit;
        private SerializedProperty ShaderType;
        private SerializedProperty RenderType;
        private SerializedProperty CullType;
        private SerializedProperty ListMode;
        private SerializedProperty Materials;

        public void OnEnable()
        {
            Inherit    = serializedObject.FindProperty(nameof(ShaderFallbackSetting.Inherit));
            ShaderType = serializedObject.FindProperty(nameof(ShaderFallbackSetting.ShaderType));
            RenderType = serializedObject.FindProperty(nameof(ShaderFallbackSetting.RenderType));
            CullType   = serializedObject.FindProperty(nameof(ShaderFallbackSetting.CullType));
            ListMode   = serializedObject.FindProperty(nameof(ShaderFallbackSetting.ListMode));
            Materials  = serializedObject.FindProperty(nameof(ShaderFallbackSetting.Materials));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(Inherit, EditorGUIUtility.TrTempContent("Fallback Overwrite Mode"));
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup((InheritMode)Inherit.enumValueIndex is InheritMode.Inherit or InheritMode.DontSet);

            bool canEditRenderType = false;
            bool canEditCullType = false;

            EditorGUILayout.PropertyField(ShaderType);

            (canEditRenderType, canEditCullType) = (FallbackShaderType)ShaderType.enumValueIndex switch
            {
                FallbackShaderType.Toon or FallbackShaderType.Unlit => (true, true),
                _ => (false, false),
            };

            EditorGUI.BeginDisabledGroup(!canEditRenderType);
            EditorGUILayout.PropertyField(RenderType, EditorGUIUtility.TrTempContent("Rendering Mode"));
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canEditCullType);
            EditorGUILayout.PropertyField(CullType, EditorGUIUtility.TrTempContent("Facing"));
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(ListMode, EditorGUIUtility.TrTempContent("Material List Mode"));
            if (EditorGUI.EndChangeCheck() && ListMode.enumValueIndex != 0)
                Materials.isExpanded = true;
            EditorGUI.BeginDisabledGroup(ListMode.enumValueIndex == 0);
            EditorGUILayout.PropertyField(Materials);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
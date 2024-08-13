using System;
using System.Collections.Generic;
using nadena.dev.ndmf;
using Numeira;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(ShaderFallbackOverwriterCore))]

namespace Numeira
{

    public sealed class ShaderFallbackOverwriterCore : Plugin<ShaderFallbackOverwriterCore>
    {
        public override string DisplayName => "Shader Fallback Overwriter";
        public override string QualifiedName => "numeira.shader-fallback-overwriter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar").Run("Shader Fallback Setting", context =>
            {
                if (context.AvatarRootObject.GetComponentInChildren<ShaderFallbackSetting>() == null)
                    return;

                Dictionary<(Material Mat, string Tag), Material> materialCache = new();

                context.AvatarRootObject.GetComponentsInChildren(true, ListExt<Component>.Shared);
                foreach (var component in ListExt<Component>.Shared.AsSpan())
                {
                    if (component is 
                        (not (MonoBehaviour or Renderer)) 
                        or VRCAvatarDescriptor 
                        or VRCPhysBone 
                        or VRCPhysBoneCollider
                        or VRCContactReceiver 
                        or VRCContactSender
                        or ShaderFallbackSetting)
                        continue;

                    var so = new SerializedObject(component);

                    bool enterChildren = true;
                    var p = so.GetIterator();
                    while (p.Next(enterChildren))
                    {
                        try
                        {
                            if (p.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var obj = p.objectReferenceValue;
                                if (obj == null || obj is not Material material) continue;

                                if (!ObjectRegistry.GetReference(material).TryResolve(context.ErrorReport, out var original))
                                    original = material;

                                var tag = ResolveFallbackTag(component.gameObject, original as Material);
                                if (tag == null)
                                    continue;

                                var currentTag = material.GetTag("VRCFallback", true, null);
                                if (currentTag == tag)
                                    continue;

                                if (materialCache.TryGetValue((material, tag), out var cloned))
                                {
                                    p.objectReferenceValue = cloned;
                                    continue;
                                }

                                cloned = Object.Instantiate(material);

                                AssetDatabase.AddObjectToAsset(cloned, context.AssetContainer);
                                cloned.name = $"{material.name}({tag})";
                                ObjectRegistry.RegisterReplacedObject(material, cloned);
                                materialCache.Add((material, tag), cloned);
                                cloned.SetOverrideTag("VRCFallback", tag);

                                p.objectReferenceValue = cloned;
                            }
                        }
                        finally
                        {
                            enterChildren = p.propertyType switch
                            {
                                SerializedPropertyType.String or
                                SerializedPropertyType.Integer or
                                SerializedPropertyType.Boolean or
                                SerializedPropertyType.Float or
                                SerializedPropertyType.Color or
                                SerializedPropertyType.ObjectReference or
                                SerializedPropertyType.LayerMask or
                                SerializedPropertyType.Enum or
                                SerializedPropertyType.Vector2 or
                                SerializedPropertyType.Vector3 or
                                SerializedPropertyType.Vector4 or
                                SerializedPropertyType.Rect or
                                SerializedPropertyType.ArraySize or
                                SerializedPropertyType.Character or
                                SerializedPropertyType.AnimationCurve or
                                SerializedPropertyType.Bounds or
                                SerializedPropertyType.Gradient or
                                SerializedPropertyType.Quaternion or
                                SerializedPropertyType.FixedBufferSize or
                                SerializedPropertyType.Vector2Int or
                                SerializedPropertyType.Vector3Int or
                                SerializedPropertyType.RectInt or
                                SerializedPropertyType.BoundsInt
                                    => false,
                                _ => true,
                            };
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                context.AvatarRootObject.GetComponentsInChildren(true, ListExt<ShaderFallbackSetting>.Shared);
                foreach(var component in ListExt<ShaderFallbackSetting>.Shared.AsSpan())
                {
                    Object.DestroyImmediate(component);
                }
            });
        }

        private static string ResolveFallbackTag(GameObject obj, Material material)
        {
            obj.GetComponentsInParent(true, ListExt<ShaderFallbackSetting>.Shared);
            var components = ListExt<ShaderFallbackSetting>.Shared.AsSpan();
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
            var (shaderStr, renderStr, cullStr) = (EnumExt<FallbackShaderType>.Names[(int)shader], "", "");

            if (shader is FallbackShaderType.Toon or FallbackShaderType.Unlit)
            {
                renderStr = render is not FallbackRenderType.Opaque ? EnumExt<FallbackRenderType>.Names[(int)render] : "";
                cullStr = cull is FallbackCullType.DoubleSided ? "DoubleSided" : "";
            }

            return string.Concat(shaderStr, renderStr, cullStr);
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

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Shader Fallback Settings"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(Inherit, EditorGUIUtility.TrTempContent("Fallback Overwrite Mode"));

            DrawSplitter();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Fallback Shader"), EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup((InheritMode)Inherit.enumValueIndex is InheritMode.Inherit or InheritMode.DontSet);

            EditorGUILayout.PropertyField(ShaderType);

            var (canEditRenderType, canEditCullType) = (FallbackShaderType)ShaderType.enumValueIndex switch
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

            DrawSplitter();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Material List"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(ListMode, EditorGUIUtility.TrTempContent("Material List Mode"));
            if (EditorGUI.EndChangeCheck() && ListMode.enumValueIndex != 0)
                Materials.isExpanded = true;
            EditorGUI.BeginDisabledGroup(ListMode.enumValueIndex == 0);
            EditorGUILayout.PropertyField(Materials);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSplitter()
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float margin = 4f;

            rect.x += margin;
            rect.width -= margin;
            rect.y += rect.height / 2;
            rect.height = 1;

            EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.1f));
        }
    }
}
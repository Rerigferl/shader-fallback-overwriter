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
                if (context.AvatarRootObject.GetComponentInChildren<ShaderFallbackSettings>() == null)
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
                        or ShaderFallbackSettings)
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

                context.AvatarRootObject.GetComponentsInChildren(true, ListExt<ShaderFallbackSettings>.Shared);
                foreach(var component in ListExt<ShaderFallbackSettings>.Shared.AsSpan())
                {
                    Object.DestroyImmediate(component);
                }
            });
        }

        public static string ResolveFallbackTag(GameObject obj, Material material = null)
        {
            var settings = obj.GetComponentInParent<ShaderFallbackSettings>()?.GetSettings(material);
            if (settings is not { } s)
                return null;
            return GetFallbackTagString(s.Shader, s.Render, s.Cull);
        }

        private static string GetFallbackTagString(ShaderType shader, RenderType render = RenderType.Opaque, CullType cull = CullType.Default)
        {
            var (shaderStr, renderStr, cullStr) = (EnumExt<ShaderType>.Names[(int)shader], "", "");

            if (shader is ShaderType.Toon or ShaderType.Unlit)
            {
                renderStr = render is not RenderType.Opaque ? EnumExt<RenderType>.Names[(int)render] : "";
                cullStr = cull is CullType.DoubleSided ? "DoubleSided" : "";
            }

            return string.Concat(shaderStr, renderStr, cullStr);
        }
    }

    [CustomEditor(typeof(ShaderFallbackSettings))]
    public sealed class ShaderFallbackSettingEditor : Editor
    {
        private SerializedProperty Inherit;
        private SerializedProperty ShaderTypeProp;
        private SerializedProperty RenderTypeProp;
        private SerializedProperty CullTypeProp;
        private SerializedProperty ListMode;
        private SerializedProperty Materials;
        private SerializedProperty ShaderTypeMode;
        private SerializedProperty RenderTypeMode;
        private SerializedProperty CullTypeMode;

        private GUIContent resultStringCache;
        private readonly static GUIContent EmptyContent = new GUIContent(" ");

        public void OnEnable()
        {
            Inherit    = serializedObject.FindProperty(nameof(ShaderFallbackSettings.Inherit));
            ShaderTypeProp = serializedObject.FindProperty(nameof(ShaderFallbackSettings.ShaderType));
            RenderTypeProp = serializedObject.FindProperty(nameof(ShaderFallbackSettings.RenderType));
            CullTypeProp   = serializedObject.FindProperty(nameof(ShaderFallbackSettings.CullType));
            ListMode   = serializedObject.FindProperty(nameof(ShaderFallbackSettings.ListMode));
            Materials  = serializedObject.FindProperty(nameof(ShaderFallbackSettings.Materials));

            ShaderTypeMode = serializedObject.FindProperty(nameof(ShaderFallbackSettings.ShaderTypeMode));
            RenderTypeMode = serializedObject.FindProperty(nameof(ShaderFallbackSettings.RenderTypeMode));
            CullTypeMode   = serializedObject.FindProperty(nameof(ShaderFallbackSettings.CullTypeMode));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Shader Fallback Settings"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(Inherit, EditorGUIUtility.TrTempContent("Fallback Overwrite Mode"));

            DrawSplitter();

            EditorGUI.BeginDisabledGroup((InheritMode)Inherit.enumValueIndex is InheritMode.Inherit or InheritMode.DontSet);

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Shader Type Configuration"), EditorStyles.boldLabel);
            if (DrawPropertyWithFoldout(ShaderTypeProp, EditorGUIUtility.TrTempContent("Shader Type")))
            {
                EditorGUILayout.PropertyField(ShaderTypeMode, EditorGUIUtility.TrTempContent("Configuration Mode"));
            }

            var (canEditRenderType, canEditCullType) = (ShaderType)ShaderTypeProp.enumValueIndex switch
            {
                ShaderType.Toon or ShaderType.Unlit => (true, true),
                _ => (false, false),
            };

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!canEditRenderType);
            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Rendering Mode Configuration"), EditorStyles.boldLabel);

            if (DrawPropertyWithFoldout(RenderTypeProp, EditorGUIUtility.TrTempContent("Rendering Mode")))
            {
                EditorGUILayout.PropertyField(RenderTypeMode, EditorGUIUtility.TrTempContent("Configuration Mode"));
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!canEditCullType);
            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Facing Configuration"), EditorStyles.boldLabel);

            if (DrawPropertyWithFoldout(CullTypeProp, EditorGUIUtility.TrTempContent("Facing")))
            {
                EditorGUILayout.PropertyField(CullTypeMode, EditorGUIUtility.TrTempContent("Configuration Mode"));
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();


            if (resultStringCache == null || EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                resultStringCache = new(ShaderFallbackOverwriterCore.ResolveFallbackTag((target as Component).gameObject) ?? "None" );
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTempContent("Result"), resultStringCache, EditorStyles.boldLabel);

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

        private static bool DrawPropertyWithFoldout(SerializedProperty property, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.PropertyField(rect, property, EmptyContent);
            bool expanded = property.isExpanded;
            bool flag = EditorGUI.Foldout(rect, expanded, label, true);
            if (expanded != flag)
            {
                property.isExpanded = flag;
            }
            return flag;
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
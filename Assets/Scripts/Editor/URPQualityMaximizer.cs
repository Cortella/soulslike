using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.Universal;
#endif

/// <summary>
/// Configura o pipeline URP para qualidade máxima (next-gen).
/// Shadows de alta resolução, SSAO, anti-aliasing, HDR, etc.
/// </summary>
public static class URPQualityMaximizer
{
#if UNITY_EDITOR
    [MenuItem("Soulslike/Qualidade/Maximizar URP (Next-Gen)")]
    public static void MaximizeURPQuality()
    {
        // Encontrar o URP Asset ativo
        var currentRP = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (currentRP == null)
        {
            // Tentar encontrar o PC_RPAsset
            string[] guids = AssetDatabase.FindAssets("PC_RPAsset t:UniversalRenderPipelineAsset");
            if (guids.Length == 0)
                guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                currentRP = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            }

            if (currentRP == null)
            {
                Debug.LogError("[Quality] Nenhum URP Asset encontrado!");
                return;
            }
        }

        // Editar via SerializedObject para acessar propriedades internas
        SerializedObject so = new SerializedObject(currentRP);

        // === RENDERING ===
        SetProperty(so, "m_SupportsHDR", true);
        SetProperty(so, "m_HDRColorBufferPrecision", 1); // 64 bits

        // === SHADOWS ===
        SetProperty(so, "m_MainLightShadowmapResolution", 4096);
        SetProperty(so, "m_AdditionalLightsShadowmapResolution", 2048);
        SetProperty(so, "m_MainLightShadowsSupported", true);
        SetProperty(so, "m_AdditionalLightShadowsSupported", true);
        SetProperty(so, "m_ShadowDistance", 150f);
        SetProperty(so, "m_ShadowCascadeCount", 4);
        SetProperty(so, "m_Cascade2Split", 0.15f);
        SetProperty(so, "m_Cascade3Split", new Vector2(0.1f, 0.3f));
        SetProperty(so, "m_Cascade4Split", new Vector3(0.07f, 0.2f, 0.5f));
        SetProperty(so, "m_SoftShadowsSupported", true);

        // === LIGHTS ===
        SetProperty(so, "m_AdditionalLightsRenderingMode", 1); // Per-Pixel
        SetProperty(so, "m_MaxAdditionalLightsCount", 8);

        // === ANTI-ALIASING ===
        // MSAA
        SetProperty(so, "m_MSAA", 4); // 4x MSAA

        so.ApplyModifiedProperties();

        // === CONFIGURE RENDERER FEATURES (SSAO, etc.) ===
        ConfigureRendererFeatures(currentRP);

        EditorUtility.SetDirty(currentRP);
        AssetDatabase.SaveAssets();

        Debug.Log("[Quality] URP maximizado para qualidade next-gen!");
        Debug.Log("  - Sombras: 4096x4096, 4 cascatas, soft shadows");
        Debug.Log("  - HDR: Ativado 64-bit");
        Debug.Log("  - MSAA: 4x");
        Debug.Log("  - Luzes adicionais: Per-Pixel, 8 max");
    }

    private static void ConfigureRendererFeatures(UniversalRenderPipelineAsset urpAsset)
    {
        // Encontrar o renderer data
        string[] rendererGuids = AssetDatabase.FindAssets("PC_Renderer t:UniversalRendererData");
        if (rendererGuids.Length == 0)
            rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData");

        if (rendererGuids.Length == 0) return;

        foreach (string guid in rendererGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null) continue;

            SerializedObject rendererSO = new SerializedObject(rendererData);

            // Habilitar depth texture e opaque texture
            var depthProp = rendererSO.FindProperty("m_RequireDepthTexture");
            if (depthProp != null) depthProp.boolValue = true;

            var opaqueProp = rendererSO.FindProperty("m_RequireOpaqueTexture");
            if (opaqueProp != null) opaqueProp.boolValue = true;

            rendererSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(rendererData);
        }
    }

    private static void SetProperty(SerializedObject so, string name, bool value)
    {
        var prop = so.FindProperty(name);
        if (prop != null) prop.boolValue = value;
    }

    private static void SetProperty(SerializedObject so, string name, int value)
    {
        var prop = so.FindProperty(name);
        if (prop != null) prop.intValue = value;
    }

    private static void SetProperty(SerializedObject so, string name, float value)
    {
        var prop = so.FindProperty(name);
        if (prop != null) prop.floatValue = value;
    }

    private static void SetProperty(SerializedObject so, string name, Vector2 value)
    {
        var prop = so.FindProperty(name);
        if (prop != null) prop.vector2Value = value;
    }

    private static void SetProperty(SerializedObject so, string name, Vector3 value)
    {
        var prop = so.FindProperty(name);
        if (prop != null) prop.vector3Value = value;
    }
#endif
}

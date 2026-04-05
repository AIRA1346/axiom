#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity-Chan 기본 머티리얼(Built-in 전용 셰이더)을 URP Lit 으로 바꿔 분홍색을 제거합니다.
/// 메인 텍스처(_MainTex)만 Base Map 으로 옮깁니다. 툰·아웃라인 표현은 단순화됩니다.
/// Tools → ARCHÉ → Open World → Unity-Chan 머티리얼 → URP Lit 일괄 변환(간단)
/// </summary>
public static class UnityChanUrpMaterialBatch
{
    private const string UnityChanRoot = "Assets/unity-chan!";

    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/ARCHÉ/Open World/Unity-Chan 머티리얼 → URP Lit 일괄 변환(간단)")]
    public static void ConvertMaterials()
    {
        Shader urpLit = Shader.Find(UrpLitShaderName);
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "URP",
                "셰이더를 찾을 수 없습니다: " + UrpLitShaderName + "\nURP 패키지가 프로젝트에 있는지 확인하세요.",
                "확인");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { UnityChanRoot });
        int converted = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null)
            {
                continue;
            }

            if (mat.shader.name.Contains("Universal Render Pipeline"))
            {
                continue;
            }

            Texture mainTex = null;
            if (mat.HasProperty("_MainTex"))
            {
                mainTex = mat.GetTexture("_MainTex");
            }

            mat.shader = urpLit;
            if (mainTex != null && mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", mainTex);
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.35f);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", 0f);
            }

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Unity-Chan URP",
            $"변환한 머티리얼: {converted}개\n\n" +
            "• 툰·림라이트 등은 단순 Lit 이라 사라질 수 있습니다.\n" +
            "• 눈·치크 등 _MainTex 가 없는 머티리얼은 수동으로 텍스처를 넣어 주세요.",
            "확인");
    }
}
#endif

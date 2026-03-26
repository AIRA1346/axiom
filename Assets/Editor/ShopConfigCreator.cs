using UnityEditor;
using UnityEngine;

/// <summary>
/// ShopConfig 에셋을 생성합니다.
/// Menu: GSI > Create Shop Config
/// </summary>
public static class ShopConfigCreator
{
    private const string AssetPath = "Assets/Config/ShopConfig.asset";

    [MenuItem("GSI/Create Shop Config")]
    public static void Create()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ShopConfig>(AssetPath);
        if (existing != null)
        {
            Debug.Log($"[ShopConfig] 이미 존재합니다: {AssetPath}");
            Selection.activeObject = existing;
            return;
        }

        string dir = System.IO.Path.GetDirectoryName(AssetPath);
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var config = ScriptableObject.CreateInstance<ShopConfig>();
        config.TicketItemId = EconomyManager.TicketItemId;

        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ShopConfig] 생성 완료: {AssetPath}");
        Selection.activeObject = config;
    }
}

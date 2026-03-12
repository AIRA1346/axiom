using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ItemExporter
{
    private const string ItemsFolderPath = "Assets/Resources/Items";

    [MenuItem("Tools/Export Items to CSV")]
    private static void ExportItemsToCsv()
    {
        string savePath = EditorUtility.SaveFilePanel(
            "Export Items to CSV",
            Application.dataPath,
            "ItemDataExport",
            "csv");

        if (string.IsNullOrWhiteSpace(savePath))
        {
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("\"ItemId\",\"ItemName\",\"Description\",\"MainCategory\",\"MiddleCategory\",\"SubCategory\",\"Tier\",\"DefaultSlot\",\"GripType\",\"PurchasePrice\",\"SalePrice\"");

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsFolderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

            if (itemData == null)
            {
                continue;
            }

            builder.AppendLine(string.Join(",",
                EscapeCsv(itemData.ItemId),
                EscapeCsv(itemData.ItemName),
                EscapeCsv(itemData.Description),
                EscapeCsv(itemData.MainCategory.ToString()),
                EscapeCsv(itemData.MiddleCategory.ToString()),
                EscapeCsv(itemData.SubCategory.ToString()),
                EscapeCsv(itemData.Tier.ToString()),
                EscapeCsv(itemData.DefaultSlot.ToString()),
                EscapeCsv(itemData.GripType.ToString()),
                EscapeCsv(itemData.PurchasePrice.ToString()),
                EscapeCsv(itemData.SalePrice.ToString())));
        }

        File.WriteAllText(savePath, builder.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();

        Debug.Log($"ItemExporter: CSV 내보내기 완료 - {savePath}");
    }

    private static string EscapeCsv(string value)
    {
        string safeValue = value ?? string.Empty;
        safeValue = safeValue.Replace("\"", "\"\"");
        return $"\"{safeValue}\"";
    }
}

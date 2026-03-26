using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// 플레이어 빌드 전에 ItemMetadata.bin을 자동 생성합니다.
/// CI/CD 또는 수동 빌드 시 StreamingAssets에 최신 메타데이터가 포함되도록 보장합니다.
/// </summary>
public sealed class ItemMetadataBuildPipeline : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        AssetDatabase.SaveAssets();

        try
        {
            ItemMetadataBuilder.Build(Application.isBatchMode);
        }
        catch (System.Exception ex)
        {
            var wrapper = new System.Exception(
                $"[ItemMetadataBuildPipeline] ItemMetadata.bin 빌드 실패. 빌드를 중단합니다.", ex);
            throw new BuildFailedException(wrapper);
        }
    }
}

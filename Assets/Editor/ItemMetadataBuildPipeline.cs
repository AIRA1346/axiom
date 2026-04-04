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
            if (!ItemMetadataBuilder.Build(Application.isBatchMode))
            {
                throw new BuildFailedException(
                    "[ItemMetadataBuildPipeline] ItemMetadata 빌드 또는 검증(샤딩·Addressables 주소) 실패. 콘솔 로그를 확인하세요.");
            }
        }
        catch (System.Exception ex)
        {
            if (ex is BuildFailedException)
            {
                throw;
            }

            var wrapper = new System.Exception(
                "[ItemMetadataBuildPipeline] ItemMetadata 빌드 중 예외. 빌드를 중단합니다.", ex);
            throw new BuildFailedException(wrapper);
        }
    }
}

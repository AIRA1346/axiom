using NUnit.Framework;

/// <summary>
/// CI(game-ci unity-test-runner)에서 실행: 커밋된 메타·Addressables·Codex 본문 일관성.
/// </summary>
public class P0DataValidationTests
{
    [Test]
    public void StreamingAssets_Meta_Codex_And_Addressables_AreConsistent()
    {
        bool ok = ArchEP0ValidationGate.RunP0ValidateOnly(silent: true);
        Assert.IsTrue(ok, "P0 검증 실패. Unity 콘솔 로그를 확인하세요.");
    }
}

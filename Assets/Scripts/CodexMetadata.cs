using System;

/// <summary>
/// ARCHÉ ADDS(10단계 심화 위계) 도서관 색인 행.
/// Lv.1~Lv.9는 위계 문자열, Lv.10(Spec)은 공식 제목(Title)이며 본문 .txt에는 포함하지 않습니다.
/// 플랫 트리 UI용으로 Depth(0~8=카테고리 헤더, 9=본문 항목), HasBody를 함께 사용합니다.
/// </summary>
[Serializable]
public struct CodexMetadata
{
    /// <summary>지식 고유 ID (KNO-[Lv1]-[Lv2]-[6자리 번호])</summary>
    public string Id;

    /// <summary>Lv.10 공식 제목(Spec). 카테고리 행에서는 해당 Depth의 구간 표시용 텍스트로도 사용됩니다.</summary>
    public string Title;

    /// <summary>목차 미리보기 요약</summary>
    public string Summary;

    /// <summary>Lv.1 Root</summary>
    public string Level1Root;

    /// <summary>Lv.2 Source</summary>
    public string Level2Source;

    /// <summary>Lv.3 Field</summary>
    public string Level3Field;

    /// <summary>Lv.4 Nature</summary>
    public string Level4Nature;

    /// <summary>Lv.5 Lineage</summary>
    public string Level5Lineage;

    /// <summary>Lv.6 Role</summary>
    public string Level6Role;

    /// <summary>Lv.7 Rank</summary>
    public string Level7Rank;

    /// <summary>Lv.8 Species</summary>
    public string Level8Species;

    /// <summary>Lv.9 Identity</summary>
    public string Level9Identity;

    /// <summary>0~8: ADDS 구간 헤더, 9: 본문 항목(리프)</summary>
    public int Depth;

    /// <summary>CSV/빌드 시 정렬용 순서</summary>
    public int SortOrder;

    /// <summary>리프(Depth 9)이며 본문 파일이 있으면 true</summary>
    public bool HasBody;
}

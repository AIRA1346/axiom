using UnityEngine;

namespace ArchE.Game
{
    /// <summary>
    /// 개별 G.S.I 미니게임의 메타데이터와 시스템 링크를 정의하는 ScriptableObject 에셋 사양입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MiniGame_", menuName = "GSI/MiniGame Descriptor", order = 1)]
    public sealed class MiniGameDescriptor : ScriptableObject
    {
        [Header("핵심 식별자 (Core Identity)")]
        [Tooltip("미니게임의 고유 문자열 ID (예: Aim, BulletHell)")]
        public string GameId;

        [Tooltip("하위 호환성 및 기존 평가 로직 연동을 위한 레거시 TestMode 매핑")]
        public TestMode LegacyMode;

        [Header("표시 및 로컬라이징 (Display & Localization)")]
        [Tooltip("로비 성단 노드 및 결과창 UI에 표시될 로컬라이징 스트링 키 (UiStringKeys 참고)")]
        public string DisplayNameLocalizationKey;

        [Header("실행 방식 및 컴포넌트 매핑 (Execution & Components)")]
        [Tooltip("이 미니게임이 분리된 독립 씬을 사용할 경우 씬의 이름 (선택사항, 지정 시 additively 로드됨)")]
        public string SceneName;

        [Tooltip("동적으로 AddComponent할 컨트롤러 타입명 (어셈블리 포함 전체 명칭, 예: MemoryTestController, ArchE.Game)")]
        public string ControllerAssemblyQualifiedName;

        [Tooltip("프리팹 인스턴스화 방식을 사용할 경우 참조할 프리팹 (선택사항)")]
        public GameObject ControllerPrefab;
    }
}

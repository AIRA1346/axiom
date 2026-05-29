using System.Collections.Generic;
using UnityEngine;

namespace ArchE.Game
{
    /// <summary>
    /// 활성화된 G.S.I 미니게임들의 마스터 목록을 지닌 중앙 레지스트리 에셋 사양입니다.
    /// Resources/ 폴더에 에셋 파일로 생성되어 런타임에 싱글톤 형태로 색인됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MiniGameRegistry", menuName = "GSI/MiniGame Registry", order = 2)]
    public sealed class MiniGameRegistry : ScriptableObject
    {
        [Header("활성 미니게임 목록 (Active Mini-Games)")]
        [Tooltip("G.S.I 공식 시험 및 연습 성단에 포함될 미니게임 명세들의 리스트")]
        [SerializeField] private List<MiniGameDescriptor> _activeMiniGames = new List<MiniGameDescriptor>();

        public IReadOnlyList<MiniGameDescriptor> ActiveMiniGames => _activeMiniGames;

        private static MiniGameRegistry _instance;

        /// <summary>
        /// Resources/ 폴더에서 'MiniGameRegistry' 이름의 에셋을 동적으로 로드하여 반환합니다.
        /// </summary>
        public static MiniGameRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MiniGameRegistry>("MiniGameRegistry");
                    if (_instance == null)
                    {
                        Debug.LogError(
                            "[MiniGameRegistry] Resources/ 폴더 내에 'MiniGameRegistry' 에셋을 찾을 수 없습니다. " +
                            "Assets/Resources/ 폴더를 생성하고 해당 에셋 파일을 배치해 주십시오.");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 특정 고유 ID를 가진 미니게임 명세를 찾습니다.
        /// </summary>
        public MiniGameDescriptor FindGame(string gameId)
        {
            for (int i = 0; i < _activeMiniGames.Count; i++)
            {
                if (_activeMiniGames[i] != null && _activeMiniGames[i].GameId == gameId)
                {
                    return _activeMiniGames[i];
                }
            }
            return null;
        }

        /// <summary>
        /// 레거시 TestMode에 매핑되는 미니게임 명세를 찾습니다.
        /// </summary>
        public MiniGameDescriptor FindGameByLegacyMode(TestMode mode)
        {
            for (int i = 0; i < _activeMiniGames.Count; i++)
            {
                if (_activeMiniGames[i] != null && _activeMiniGames[i].LegacyMode == mode)
                {
                    return _activeMiniGames[i];
                }
            }
            return null;
        }
    }
}

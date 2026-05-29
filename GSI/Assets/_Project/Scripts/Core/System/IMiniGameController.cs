using System;

namespace ArchE.Game
{
    /// <summary>
    /// G.S.I 미니게임 컨트롤러가 준수해야 하는 공통 라이프사이클 인터페이스입니다.
    /// 새로운 미니게임을 추가할 때 이 인터페이스를 구현합니다.
    /// </summary>
    public interface IMiniGameController
    {
        /// <summary>미니게임의 고유 식별 키</summary>
        string GameId { get; }

        /// <summary>지정된 난이도 급수(1=최상, 9=최하)로 게임을 초기화합니다.</summary>
        void InitializeGame(int grade);

        /// <summary>게임을 시작합니다.</summary>
        void StartGame();

        /// <summary>게임을 강제로 종료합니다.</summary>
        void EndGame();

        /// <summary>게임 완료 시 호출되는 이벤트 (매개변수: 0~100점 점수, 통과 여부)</summary>
        event Action<float, bool> OnGameFinished;
    }
}

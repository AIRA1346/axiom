using System.Collections.Generic;
using UnityEngine;

namespace ArchE.Game
{
    /// <summary>
    /// G.S.I 공용 우주 천체 물리 오케스트레이션 시스템:
    /// 씬 내의 모든 별 노드(StarNodeControllerBase)를 중앙 등록 및 관리하고, 
    /// 화이트홀 특이점의 척력장 수식 연산 및 일괄 업데이트를 관장합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GsiCosmicOrrerySystem : MonoBehaviour
    {
        private static GsiCosmicOrrerySystem _instance;
        public static GsiCosmicOrrerySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GsiCosmicOrrerySystem>();
                }
                return _instance;
            }
        }

        private readonly List<StarNodeControllerBase> _registeredStars = new List<StarNodeControllerBase>();

        [Header("White Hole Singularity Settings")]
        private RectTransform _whiteHoleRect;
        private float _repulsionRange = 240f;
        private float _repulsionForce = 580f;
        private bool _hasWhiteHole = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdatePhysics();
        }

        /// <summary>
        /// 중앙에서 등록된 모든 별들의 우주 물리 시뮬레이션을 프레임 독립적으로 일괄 실행합니다.
        /// </summary>
        public void UpdatePhysics()
        {
            if (!Application.isPlaying) return;

            float deltaTime = Time.unscaledDeltaTime;
            int count = _registeredStars.Count;

            // 1. 개별 별들의 속도 댐핑, 마찰력, 화이트홀 척력장 적용 및 위치 업데이트
            for (int i = 0; i < count; i++)
            {
                var star = _registeredStars[i];
                if (star == null || !star.gameObject.activeInHierarchy) continue;

                // 물리 업데이트 (드래그하지 않는 상태일 때 마찰 및 자율 유영 처리)
                star.UpdatePhysicsTick(deltaTime);
            }

            // 2. 일괄 충돌 처리 (동일 프레임 내 상호 탄성 충돌 연산 보정)
            for (int i = 0; i < count; i++)
            {
                var starA = _registeredStars[i];
                if (starA == null || !starA.gameObject.activeInHierarchy) continue;

                for (int j = i + 1; j < count; j++)
                {
                    var starB = _registeredStars[j];
                    if (starB == null || !starB.gameObject.activeInHierarchy) continue;

                    starA.ResolveCollisionWith(starB);
                }
            }
        }

        /// <summary>
        /// 시스템에 새로운 별 노드를 등록합니다.
        /// </summary>
        public void RegisterStarNode(StarNodeControllerBase node)
        {
            if (node == null) return;
            if (!_registeredStars.Contains(node))
            {
                _registeredStars.Add(node);
            }
        }

        /// <summary>
        /// 시스템에서 별 노드 등록을 해제합니다.
        /// </summary>
        public void UnregisterStarNode(StarNodeControllerBase node)
        {
            if (node != null && _registeredStars.Contains(node))
            {
                _registeredStars.Remove(node);
            }
        }

        /// <summary>
        /// 중앙 특이점 화이트홀의 위치와 척력 계수를 설정합니다.
        /// </summary>
        public void SetWhiteHoleSingularity(RectTransform singularity, float repulsionRange, float repulsionForce)
        {
            _whiteHoleRect = singularity;
            _repulsionRange = repulsionRange;
            _repulsionForce = repulsionForce;
            _hasWhiteHole = singularity != null;
        }

        /// <summary>
        /// 지정된 위치에서 화이트홀의 척력 벡터를 계산하여 반환합니다.
        /// </summary>
        public Vector2 GetWhiteHoleRepulsion(Vector2 starPosition)
        {
            if (!_hasWhiteHole || _whiteHoleRect == null) return Vector2.zero;

            Vector2 whPos = _whiteHoleRect.anchoredPosition;
            Vector2 diff = starPosition - whPos;
            float dist = diff.magnitude;

            if (dist < _repulsionRange && dist > 0.1f)
            {
                // 밀어내는 척력 강도 (가까워질수록 포물선 궤적으로 증폭)
                float factor = 1f - dist / _repulsionRange;
                float force = factor * factor * _repulsionForce;
                return diff.normalized * force;
            }

            return Vector2.zero;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

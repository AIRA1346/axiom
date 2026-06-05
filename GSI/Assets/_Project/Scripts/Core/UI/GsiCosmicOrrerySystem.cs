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

        private readonly List<ICosmicKineticObject> _registeredObjects = new List<ICosmicKineticObject>();

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
        /// 중앙에서 등록된 모든 물리 객체들의 우주 물리 시뮬레이션을 프레임 독립적으로 일괄 실행합니다.
        /// </summary>
        public void UpdatePhysics()
        {
            if (!Application.isPlaying) return;

            float deltaTime = Time.unscaledDeltaTime;
            int count = _registeredObjects.Count;

            // 1. 개별 객체들의 물리 업데이트 (자율 유영 및 댐핑 처리)
            for (int i = 0; i < count; i++)
            {
                var obj = _registeredObjects[i];
                // Monobehaviour 컴포넌트인 경우 캐스팅 검사
                var mono = obj as MonoBehaviour;
                if (mono == null || !mono.gameObject.activeInHierarchy) continue;

                obj.UpdatePhysicsTick(deltaTime);
            }

            // 1.5. 시공간 물리 영향력(감속 중력장 등 특수 효과) 일괄 연산 적용
            for (int i = 0; i < count; i++)
            {
                var objA = _registeredObjects[i];
                var monoA = objA as MonoBehaviour;
                if (monoA == null || !monoA.gameObject.activeInHierarchy) continue;

                if (objA is GsiPlacedDeco decoA && decoA.Behavior != null)
                {
                    for (int j = 0; j < count; j++)
                    {
                        if (i == j) continue;
                        
                        var objB = _registeredObjects[j];
                        var monoB = objB as MonoBehaviour;
                        if (monoB == null || !monoB.gameObject.activeInHierarchy) continue;

                        decoA.Behavior.ApplyInfluence(objB, deltaTime);
                    }
                }
            }

            // 2. 일괄 충돌 처리 (동일 프레임 내 상호 탄성 충돌 연산 보정)
            for (int i = 0; i < count; i++)
            {
                var objA = _registeredObjects[i];
                var monoA = objA as MonoBehaviour;
                if (monoA == null || !monoA.gameObject.activeInHierarchy) continue;

                // 최적화: objA의 속도가 거의 없고 드래그 중이 아니라면 충돌 검출 주체를 생략 (Cosmic Sleep)
                // 단, 다른 움직이는 물체가 다가와 부딪힐 수도 있으므로, 상대가 충돌 검사를 주도하도록 검사 루프를 이원화합니다.
                bool sleepA = !objA.isDragging && objA.velocity.sqrMagnitude < 0.1f;

                for (int j = i + 1; j < count; j++)
                {
                    var objB = _registeredObjects[j];
                    var monoB = objB as MonoBehaviour;
                    if (monoB == null || !monoB.gameObject.activeInHierarchy) continue;

                    bool sleepB = !objB.isDragging && objB.velocity.sqrMagnitude < 0.1f;
                    
                    // 둘 다 멈춰있고 평화로운 상태라면 상호 충돌 검사를 완벽히 스킵! (CPU 점유율 극적 절감)
                    if (sleepA && sleepB) continue;

                    objA.ResolveCollisionWith(objB);
                }
            }
        }

        /// <summary>
        /// 시스템에 새로운 물리 객체를 등록합니다.
        /// </summary>
        public void RegisterStarNode(ICosmicKineticObject node)
        {
            if (node == null) return;
            if (!_registeredObjects.Contains(node))
            {
                _registeredObjects.Add(node);
            }
        }

        /// <summary>
        /// 시스템에서 물리 객체 등록을 해제합니다.
        /// </summary>
        public void UnregisterStarNode(ICosmicKineticObject node)
        {
            if (node != null && _registeredObjects.Contains(node))
            {
                _registeredObjects.Remove(node);
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

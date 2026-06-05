using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 메인 로비 및 GSI 시설 씬의 우주 배경 스테이지와 데코 배치 영역을 부드럽게 확대/축소(Zoom) 및 화면 이동(Pan) 해주는 컨트롤러입니다.
/// </summary>
public sealed class GsiCosmicViewportController : MonoBehaviour
{
    [Header("Zoom Config")]
    public float MinZoom = 1.0f;
    public float MaxZoom = 2.5f;
    public float ZoomSpeed = 0.08f;
    public float ZoomSmoothTime = 0.12f;

    [Header("Pan Config")]
    public float PanSmoothTime = 0.12f;
    public float MaxPanRadius = 1600f; // 화면 밖 무한 이탈을 방지할 제한 반경

    private RectTransform _stageContainer;
    private RectTransform _decoContainer;

    private float _targetZoom = 1.0f;
    private float _currentZoom = 1.0f;
    private float _zoomVelocity = 0.0f;

    private Vector2 _targetOffset = Vector2.zero;
    private Vector2 _currentOffset = Vector2.zero;
    private Vector2 _panVelocity = Vector2.zero;

    private bool _isPanning = false;
    private Vector2 _lastMousePosition;
    private bool _hasInitializedValues = false;

    private void Start()
    {
        FindContainers();
    }

    private void FindContainers()
    {
        Transform parent = transform;
        
        // 1. 자식 및 형제 오브젝트에서 탐색
        if (_stageContainer == null)
        {
            _stageContainer = parent.Find("GsiCosmicStage") as RectTransform;
            if (_stageContainer == null)
            {
                _stageContainer = parent.Find("LobbyCenterStage") as RectTransform;
            }
        }
        
        // 2. 전체 씬에서 탐색 (동적 생성 연동 대응)
        if (_stageContainer == null)
        {
            var foundStage = GameObject.Find("GsiCosmicStage");
            if (foundStage == null) foundStage = GameObject.Find("LobbyCenterStage");
            if (foundStage != null) _stageContainer = foundStage.GetComponent<RectTransform>();
        }

        if (_decoContainer == null)
        {
            _decoContainer = parent.Find("DecoPlacementContainer") as RectTransform;
            if (_decoContainer == null)
            {
                var foundDeco = GameObject.Find("DecoPlacementContainer");
                if (foundDeco != null) _decoContainer = foundDeco.GetComponent<RectTransform>();
            }
        }

        if (_stageContainer != null && !_hasInitializedValues)
        {
            _currentZoom = _stageContainer.localScale.x;
            _targetZoom = _currentZoom;
            _currentOffset = _stageContainer.anchoredPosition;
            _targetOffset = _currentOffset;
            _hasInitializedValues = true;
        }
    }

    private void Update()
    {
        // 동적 생성으로 인해 로딩이 늦어지는 경우 실시간 탐색 시도 (stageContainer만 필수 요소)
        if (_stageContainer == null || !_hasInitializedValues)
        {
            FindContainers();
        }

        HandleZoomInput();
        HandlePanInput();

        // 부드러운 감쇠 연산 (SmoothDamp)
        _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, ZoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        // 줌 아웃 시 화면이 경계 밖으로 이탈하지 않도록 실시간 타겟 오프셋 제한 보정
        var myRt = GetComponent<RectTransform>();
        float w = myRt != null ? myRt.rect.width : Screen.width;
        float h = myRt != null ? myRt.rect.height : Screen.height;

        float limitX = Mathf.Max(0f, w * (_currentZoom - 1f) * 0.5f);
        float limitY = Mathf.Max(0f, h * (_currentZoom - 1f) * 0.5f);

        _targetOffset.x = Mathf.Clamp(_targetOffset.x, -limitX, limitX);
        _targetOffset.y = Mathf.Clamp(_targetOffset.y, -limitY, limitY);

        _currentOffset = Vector2.SmoothDamp(_currentOffset, _targetOffset, ref _panVelocity, PanSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        // 1. 우주 배경 스테이지 적용
        if (_stageContainer != null)
        {
            _stageContainer.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            _stageContainer.anchoredPosition = _currentOffset;
        }

        // 2. 데코레이션 컨테이너 적용
        if (_decoContainer != null)
        {
            _decoContainer.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
            _decoContainer.anchoredPosition = _currentOffset;
        }
    }

    private void HandleZoomInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) > 0.01f)
        {
            // 인벤토리 스크롤 등 UI 조작 중일 때는 줌 작동 배제
            if (IsPointerOverBlockingUI())
            {
                return;
            }

            float zoomDelta = (scrollY > 0) ? ZoomSpeed : -ZoomSpeed;
            if (Keyboard.current != null && Keyboard.current.shiftKey.isPressed)
            {
                zoomDelta *= 2f; // Shift 키 유지 시 2배속 줌
            }

            _targetZoom = Mathf.Clamp(_targetZoom + zoomDelta * _targetZoom, MinZoom, MaxZoom);
        }
    }

    private void HandlePanInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 currentMousePos = mouse.position.ReadValue();

        // 마우스 휠(가운데) 클릭으로만 팬 이동 (우클릭 이동 기능 차단)
        bool panPressed = mouse.middleButton.isPressed;

        if (panPressed)
        {
            if (!_isPanning)
            {
                if (IsPointerOverBlockingUI() || IsPointerOverDraggableObject())
                {
                    return;
                }
                _isPanning = true;
                _lastMousePosition = currentMousePos;
            }
            else
            {
                Vector2 mouseDelta = currentMousePos - _lastMousePosition;
                _lastMousePosition = currentMousePos;

                Canvas canvas = GetComponentInParent<Canvas>();
                float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;

                // 캔버스 크기 비율에 맞춰 드래그 델타값 보정
                Vector2 panDelta = mouseDelta / scaleFactor;

                // 뷰포트 방향 드래그 오프셋 누적
                _targetOffset += panDelta;

                // 최대 반경 제한 (100% 비율 화면 경계에 고정)
                var myRt = GetComponent<RectTransform>();
                float w = myRt != null ? myRt.rect.width : Screen.width;
                float h = myRt != null ? myRt.rect.height : Screen.height;

                float limitX = Mathf.Max(0f, w * (_currentZoom - 1f) * 0.5f);
                float limitY = Mathf.Max(0f, h * (_currentZoom - 1f) * 0.5f);

                _targetOffset.x = Mathf.Clamp(_targetOffset.x, -limitX, limitX);
                _targetOffset.y = Mathf.Clamp(_targetOffset.y, -limitY, limitY);
            }
        }
        else
        {
            _isPanning = false;
        }
    }

    /// <summary>
    /// 포인터가 로비의 헤더, 인벤토리 카드 리스트 등 우주 영역 외의 활성 UI 컴포넌트 위에 있는지 여부를 체크합니다.
    /// </summary>
    private bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;
        
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            if (result.gameObject == null) continue;

            GameObject go = result.gameObject;

            // 1. 별 노드(피직스 노드) 계열은 줌/팬을 절대 차단하지 않음
            if (go.GetComponentInParent<StarNodeControllerBase>() != null)
            {
                continue;
            }

            // 2. 가구 데코레이션 아이템도 줌/팬을 절대 차단하지 않음
            if (go.GetComponentInParent<GsiPlacedDeco>() != null)
            {
                continue;
            }

            // 3. 화이트홀도 줌/팬을 차단하지 않음
            if (go.name == "UnifiedExamWhiteHole")
            {
                continue;
            }

            // 4. 실제로 마우스 입력을 소비하고 조작을 수행하는 인터랙티브 UI 컴포넌트가 존재할 경우 차단
            if (go.GetComponent<Button>() != null || go.GetComponentInParent<Button>() != null ||
                go.GetComponent<ScrollRect>() != null || go.GetComponentInParent<ScrollRect>() != null ||
                go.GetComponent<Slider>() != null || go.GetComponentInParent<Slider>() != null ||
                go.GetComponent<InputField>() != null || go.GetComponentInParent<InputField>() != null ||
                go.GetComponent<TMPro.TMP_InputField>() != null || go.GetComponentInParent<TMPro.TMP_InputField>() != null ||
                go.GetComponent<Toggle>() != null || go.GetComponentInParent<Toggle>() != null ||
                go.GetComponent<Scrollbar>() != null || go.GetComponentInParent<Scrollbar>() != null)
            {
                return true;
            }

            // 5. 컴포넌트는 없지만 명시적으로 알려진 차단 영역 이름 검사 (딤배경, 오버레이 팝업 등)
            string goName = go.name;
            if (goName == "Dim" || 
                goName == "GsiDecoPanel" || 
                goName == "LobbyEconomyStrip" || 
                goName == "LobbyTopLeftBar" ||
                goName == "GsiHubHeader" || 
                goName == "GsiHubSubBar" ||
                goName.Contains("Settings") ||
                goName.Contains("Popup") ||
                goName.Contains("Dialog") ||
                goName.Contains("Overlay"))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// 포인터가 드래그 가능한 오브젝트(별 노드 혹은 가구 등) 위에 있는지 체크합니다.
    /// </summary>
    private bool IsPointerOverDraggableObject()
    {
        if (EventSystem.current == null) return false;
        
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (var result in results)
        {
            if (result.gameObject == null) continue;
            
            // 1. 별 노드가 마우스 아래에 있는지 체크
            if (result.gameObject.GetComponentInParent<StarNodeControllerBase>() != null)
            {
                return true;
            }

            // 2. 가구 데코 아이템이 마우스 아래에 있는지 체크
            if (result.gameObject.GetComponentInParent<GsiPlacedDeco>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Button 기반 카테고리 선택 컴포넌트.
/// TMP_Dropdown 대신 사용하며, Blocker/레이캐스트 문제 없이 클릭이 확실히 동작합니다.
/// TMP_Dropdown과 동일한 API(AddOptions, ClearOptions, value, onValueChanged)를 제공합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class SimpleCategoryDropdown : MonoBehaviour
{
    [SerializeField] private RectTransform _triggerButton;
    [SerializeField] private TextMeshProUGUI _captionText;
    [SerializeField] private RectTransform _listPanel;
    [SerializeField] private RectTransform _listContent;
    [SerializeField] private GameObject _itemTemplate;

    [SerializeField] private int _value;

    [System.Serializable]
    public class DropdownEvent : UnityEvent<int> { }
    public DropdownEvent onValueChanged = new DropdownEvent();

    public int value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = Mathf.Clamp(value, 0, _options.Count > 0 ? _options.Count - 1 : 0);
            onValueChanged?.Invoke(_value);
        }
    }

    private readonly List<string> _options = new List<string>();
    private readonly List<GameObject> _itemInstances = new List<GameObject>();

    private void Awake()
    {
        if (_triggerButton != null)
        {
            var btn = _triggerButton.GetComponent<Button>();
            if (btn == null) btn = _triggerButton.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(ToggleList);
        }

        if (_listPanel != null)
            _listPanel.gameObject.SetActive(false);

        if (_itemTemplate != null)
            _itemTemplate.SetActive(false);
    }

    public void ClearOptions()
    {
        _options.Clear();
        RebuildItems();
    }

    public void AddOptions(List<string> options)
    {
        _options.AddRange(options);
        RebuildItems();
    }

    public void SetValueWithoutNotify(int index)
    {
        _value = Mathf.Clamp(index, 0, _options.Count > 0 ? _options.Count - 1 : 0);
        UpdateCaption();
    }

    private void RebuildItems()
    {
        foreach (var go in _itemInstances)
        {
            if (go != null) Destroy(go);
        }
        _itemInstances.Clear();

        if (_listContent == null || _itemTemplate == null) return;

        for (int i = 0; i < _options.Count; i++)
        {
            int idx = i;
            var item = Instantiate(_itemTemplate, _listContent);
            item.SetActive(true);
            var label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = _options[i];

            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectIndex(idx));
            _itemInstances.Add(item);
        }
    }

    private void SelectIndex(int index)
    {
        value = index;
        UpdateCaption();
        HideList();
    }

    private void UpdateCaption()
    {
        if (_captionText != null && _value >= 0 && _value < _options.Count)
            _captionText.text = _options[_value];
    }

    private Canvas _listOverlayCanvas;
    private GraphicRaycaster _listOverlayRaycaster;

    private void ToggleList()
    {
        if (_listPanel != null)
        {
            bool next = !_listPanel.gameObject.activeSelf;
            _listPanel.gameObject.SetActive(next);
            if (next)
            {
                transform.parent?.parent?.SetAsLastSibling();
                EnsureListOverlayCanvas(); // 최상위 렌더링으로 다른 UI에 가려지는 문제 해결
            }
        }
    }

    private void EnsureListOverlayCanvas()
    {
        if (_listPanel == null) return;
        var go = _listPanel.gameObject;
        if (_listOverlayCanvas == null)
        {
            _listOverlayCanvas = go.GetComponent<Canvas>();
            if (_listOverlayCanvas == null)
            {
                _listOverlayCanvas = go.AddComponent<Canvas>();
                _listOverlayCanvas.overrideSorting = true;
                _listOverlayCanvas.sortingOrder = 32767;
                _listOverlayRaycaster = go.GetComponent<GraphicRaycaster>();
                if (_listOverlayRaycaster == null)
                    _listOverlayRaycaster = go.AddComponent<GraphicRaycaster>();
            }
        }
        _listOverlayCanvas.overrideSorting = true;
        _listOverlayCanvas.sortingOrder = 32767;
    }

    private void HideList()
    {
        if (_listPanel != null)
            _listPanel.gameObject.SetActive(false);
    }

    private Canvas _canvas;
    private Camera _canvasCamera;

    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasCamera = _canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
    }

    private void LateUpdate()
    {
        if (_listPanel == null || !_listPanel.gameObject.activeSelf)
            return;

        if (!TryGetPointerDown(out Vector2 pointerPos))
            return;

        // 레이아웃 갱신 (ContentSizeFitter/VerticalLayoutGroup)
        if (_listContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_listContent);

        var cam = _canvasCamera;

        // 1) 리스트 항목 클릭 처리
        int hitIndex = FindClickedItemIndex(pointerPos, cam);
        if (hitIndex >= 0)
        {
            SelectIndex(hitIndex);
            return;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(_listPanel, pointerPos, cam))
        {
            hitIndex = FindClickedItemByRaycast(pointerPos);
            if (hitIndex >= 0)
            {
                SelectIndex(hitIndex);
                return;
            }
        }

        // 2) 리스트·트리거 안쪽이면 닫지 않음
        if (RectTransformUtility.RectangleContainsScreenPoint(_listPanel, pointerPos, cam))
            return;
        if (_triggerButton != null && RectTransformUtility.RectangleContainsScreenPoint(_triggerButton, pointerPos, cam))
            return;

        // 3) 바깥 클릭 시 닫기
        HideList();
    }

    private int FindClickedItemIndex(Vector2 screenPos, Camera cam)
    {
        for (int i = 0; i < _itemInstances.Count; i++)
        {
            var item = _itemInstances[i];
            if (item == null || !item.activeInHierarchy) continue;
            var rect = item.GetComponent<RectTransform>();
            if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
                return i;
        }
        return -1;
    }

    private int FindClickedItemByRaycast(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return -1;

        var eventData = new PointerEventData(es) { position = screenPos };
        var results = new List<RaycastResult>();
        es.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            var go = r.gameObject;
            for (int i = 0; i < _itemInstances.Count; i++)
            {
                if (_itemInstances[i] == null) continue;
                if (go == _itemInstances[i] || go.transform.IsChildOf(_itemInstances[i].transform))
                    return i;
            }
        }
        return -1;
    }

    private static bool TryGetPointerDown(out Vector2 pos)
    {
        pos = default;
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            return true;
        }
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
        {
            pos = touch.primaryTouch.position.ReadValue();
            return true;
        }
#endif
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return true;
        }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
        return false;
    }
}

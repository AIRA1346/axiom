using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tab: 무기 꺼내기/집어넣기 토글. 표시할 오브젝트는 인스펙터에 넣거나 자식 이름 WeaponHolder 를 찾습니다.
/// </summary>
public sealed class OpenWorldWeaponEquip : MonoBehaviour
{
    [SerializeField] private GameObject[] _weaponVisualRoots;

    [Tooltip("비어 있으면 시작 시 이름이 WeaponHolder 인 자식을 찾습니다.")]
    [SerializeField] private bool _findWeaponHolderByName = true;

    private bool _drawn;

    public bool IsWeaponDrawn => _drawn;

    private void Start()
    {
        if ((_weaponVisualRoots == null || _weaponVisualRoots.Length == 0) && _findWeaponHolderByName)
        {
            Transform holder = transform.Find("WeaponHolder");
            if (holder != null)
            {
                _weaponVisualRoots = new[] { holder.gameObject };
            }
        }

        ApplyVisuals();
    }

    private void Update()
    {
        if (!Application.isPlaying || !enabled)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.tabKey.wasPressedThisFrame)
        {
            _drawn = !_drawn;
            ApplyVisuals();
        }
    }

    private void ApplyVisuals()
    {
        if (_weaponVisualRoots == null)
        {
            return;
        }

        foreach (GameObject go in _weaponVisualRoots)
        {
            if (go != null)
            {
                go.SetActive(_drawn);
            }
        }
    }
}

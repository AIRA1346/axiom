using UnityEngine;

/// <summary>
/// 타깃(플레이어) 뒤·위에서 따라가는 간단한 3인칭 카메라. 추후 Cinemachine 등으로 교체 가능.
/// </summary>
public sealed class OpenWorldFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;

    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.2f, -4.5f);

    [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1.2f, 0f);

    private void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 worldOffset = _target.TransformDirection(new Vector3(_offset.x, 0f, _offset.z));
        transform.position = _target.position + worldOffset + Vector3.up * _offset.y;
        transform.LookAt(_target.position + _lookAtOffset, Vector3.up);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }
}

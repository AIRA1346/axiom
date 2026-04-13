using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임 생성 UI 목록에서 행 루트(Image+레이아웃 셸)를 재사용해 갱신 시 할당·파괴 비용을 줄입니다.
/// 행 내부 TMP·버튼은 <see cref="PopOrCreate"/> 시 비우고 다시 채웁니다.
/// </summary>
public sealed class GsiRuntimeUiRowPool
{
    private readonly Transform _poolRoot;
    private readonly Stack<GameObject> _stack = new Stack<GameObject>();

    public GsiRuntimeUiRowPool(Transform poolRoot)
    {
        _poolRoot = poolRoot != null
            ? poolRoot
            : throw new ArgumentNullException(nameof(poolRoot));
    }

    /// <summary>
    /// 이름이 <see cref="GsiShopLikeListScrollUi.IsRowPoolHolderName"/> 인 풀 홀더 자식은 건너뛰고 나머지 행만 풀로 옮깁니다.
    /// </summary>
    public void RecycleAllListRows(Transform list)
    {
        for (int i = list.childCount - 1; i >= 0; i--)
        {
            GameObject go = list.GetChild(i).gameObject;
            if (GsiShopLikeListScrollUi.IsRowPoolHolderName(go.name))
            {
                continue;
            }

            Push(go);
        }
    }

    public void Push(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        go.transform.SetParent(_poolRoot, false);
        go.SetActive(false);
        _stack.Push(go);
    }

    public GameObject PopOrCreate(Transform listParent, Func<Transform, GameObject> createNew)
    {
        if (createNew == null)
        {
            throw new ArgumentNullException(nameof(createNew));
        }

        if (_stack.Count > 0)
        {
            GameObject go = _stack.Pop();
            ClearRowContent(go.transform);
            go.transform.SetParent(listParent, false);
            go.SetActive(true);
            return go;
        }

        return createNew(listParent);
    }

    private static void ClearRowContent(Transform row)
    {
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            GsiRuntimeUiBootstrap.DestroyObjectForRuntimeUi(row.GetChild(i).gameObject);
        }
    }
}

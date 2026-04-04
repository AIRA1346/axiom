using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감 아이템 슬롯 셀. 가상화 스크롤에서 재사용됩니다.
/// 미해금: Grayscale 쉐이더 또는 어두운 실루엣. 해금: 컬러 표시.
/// Bind 시 메타데이터로 즉시 표시하고, 아이콘은 LoadAsync로 비동기 로드합니다.
/// 아이템 이름 앞에 공식 10단계 한자 접두사([凡][奇][珍]...)를 적용합니다.
/// </summary>
public sealed class EncyclopediaItemCell : MonoBehaviour
{
    private const string GrayscaleShaderName = "UI/Grayscale";
    private const string DefaultShaderName = "UI/Default";

    /// <summary>
    /// 티어 숫자(1~10) → 공식 한자 접두사. 범위 초과/비어있으면 [凡] 반환.
    /// </summary>
    private static readonly string[] TierPrefixChars = { "凡", "奇", "珍", "傑", "古", "遺", "聖", "傳", "神", "極" };

    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _lockedOverlay;
    [SerializeField] private Image _backgroundImage;

    private Material _grayscaleMaterial;
    private Material _defaultMaterial;
    private bool _isUnlocked;
    private string _currentItemId;
    private int _dataIndex;
    private Coroutine _loadCoroutine;

    public string ItemId => _currentItemId;
    public int DataIndex => _dataIndex;
    public bool IsUnlocked => _isUnlocked;
    public RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        if (_iconImage == null)
        {
            _iconImage = GetComponentInChildren<Image>();
        }

        if (_nameText == null)
        {
            _nameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        EnsureMaterials();
    }

    private void EnsureMaterials()
    {
        if (_grayscaleMaterial != null)
        {
            return;
        }

        Shader grayscaleShader = Shader.Find(GrayscaleShaderName);
        if (grayscaleShader != null)
        {
            _grayscaleMaterial = new Material(grayscaleShader);
            _grayscaleMaterial.SetFloat("_GrayscaleAmount", 1f);
            _grayscaleMaterial.SetFloat("_Invert", 0f);
        }

        Shader defaultShader = Shader.Find(DefaultShaderName);
        if (defaultShader != null)
        {
            _defaultMaterial = new Material(defaultShader);
        }
    }

    /// <summary>
    /// 셀에 표시할 데이터를 바인딩합니다.
    /// metadata로 즉시 표시하고, ItemData(아이콘)는 LoadAsync로 비동기 로드합니다.
    /// </summary>
    public void Bind(string itemId, ItemMetadata? metadata, ItemData fullData, bool isUnlocked, int dataIndex = 0)
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
            _loadCoroutine = null;
        }

        _currentItemId = itemId;
        _isUnlocked = isUnlocked;
        _dataIndex = dataIndex;

        string baseName = metadata.HasValue ? metadata.Value.ItemName : (fullData?.ItemName ?? itemId ?? "");
        ItemTier tier = metadata.HasValue ? metadata.Value.Tier : (fullData?.Tier ?? ItemTier.Tier1);
        string displayName = FormatDisplayNameWithTierPrefix(baseName, tier);

        if (_nameText == null)
        {
            _nameText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (_nameText != null)
        {
            _nameText.text = displayName;
        }

        if (fullData != null && fullData.ItemIcon != null)
        {
            ApplyIcon(fullData.ItemIcon);
        }
        else
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

            var db = ItemDatabase.Instance;
            if (db != null && db.UseMetadataOnly && !string.IsNullOrWhiteSpace(itemId))
            {
                _loadCoroutine = StartCoroutine(LoadIconAsync(itemId));
            }
        }

        if (_lockedOverlay != null)
        {
            _lockedOverlay.SetActive(!isUnlocked);
        }

        if (_backgroundImage != null)
        {
            _backgroundImage.color = isUnlocked ? new Color(1f, 1f, 1f) : new Color(0.4f, 0.4f, 0.45f);
        }
    }

    /// <summary>
    /// 기존 API 호환: ItemData가 있으면 즉시 표시.
    /// </summary>
    public void Bind(string itemId, ItemData data, bool isUnlocked, int dataIndex = 0)
    {
        var meta = ItemDatabase.Instance?.GetMetadata(itemId);
        Bind(itemId, meta, data, isUnlocked, dataIndex);
    }

    private IEnumerator LoadIconAsync(string itemId)
    {
        bool completed = false;
        ItemData loaded = null;

        ItemDatabase.Instance?.LoadItemDataAsync(itemId, data =>
        {
            loaded = data;
            completed = true;
        });

        while (!completed)
        {
            yield return null;
        }

        _loadCoroutine = null;

        if (loaded != null && loaded.ItemId == _currentItemId && _iconImage != null)
        {
            ApplyIcon(loaded.ItemIcon);
        }
    }

    private void ApplyIcon(Sprite sprite)
    {
        if (_iconImage == null)
        {
            return;
        }

        _iconImage.sprite = sprite;
        _iconImage.enabled = sprite != null;
        ApplyUnlockState(_iconImage, _isUnlocked);
    }

    private void ApplyUnlockState(Image targetImage, bool unlocked)
    {
        if (targetImage == null)
        {
            return;
        }

        EnsureMaterials();

        if (unlocked)
        {
            targetImage.material = _defaultMaterial;
            targetImage.color = Color.white;
        }
        else
        {
            if (_grayscaleMaterial != null)
            {
                targetImage.material = _grayscaleMaterial;
                targetImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else
            {
                targetImage.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
            }
        }
    }

    /// <summary>
    /// 티어를 1~10 숫자로 변환 후 한자 접두사를 반환. 비정상 값은 [凡].
    /// </summary>
    private static string GetTierPrefixChar(ItemTier tier)
    {
        int idx = (int)tier;
        if (idx < 0 || idx >= TierPrefixChars.Length)
        {
            return TierPrefixChars[0]; // [凡]
        }
        return TierPrefixChars[idx];
    }

    /// <summary>
    /// 아이템 이름 앞에 등급 접두사 추가. 예: "[凡] 거친 나뭇가지"
    /// </summary>
    private static string FormatDisplayNameWithTierPrefix(string baseName, ItemTier tier)
    {
        string prefixChar = GetTierPrefixChar(tier);
        return $"[{prefixChar}] {baseName}";
    }

    private void OnDestroy()
    {
        if (_loadCoroutine != null)
        {
            StopCoroutine(_loadCoroutine);
        }

        if (_grayscaleMaterial != null)
        {
            Destroy(_grayscaleMaterial);
        }
    }
}

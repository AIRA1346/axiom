using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[System.Serializable]
public struct SlotUIElement
{
    [FormerlySerializedAs("slot")]
    public EquipSlot Slot;

    [FormerlySerializedAs("slot_button")]
    public Button SlotButton;

    [FormerlySerializedAs("item_name_text")]
    public TextMeshProUGUI ItemNameText;
}

/// <summary>
/// 장비 UI 패널을 제어합니다. 슬롯별 장착 상태, 스탯 표시, 장비 선택 패널을 관리합니다.
/// </summary>
public sealed class EquipmentUIController : MonoBehaviour
{
    [FormerlySerializedAs("slot_uis")]
    [SerializeField] private List<SlotUIElement> _slotUis;

    [FormerlySerializedAs("stats_display_text")]
    [SerializeField] private TextMeshProUGUI _statsDisplayText;

    [FormerlySerializedAs("selection_panel")]
    [SerializeField] private GameObject _selectionPanel;

    [FormerlySerializedAs("choice_container")]
    [SerializeField] private Transform _choiceContainer;

    [FormerlySerializedAs("choice_button_prefab")]
    [SerializeField] private GameObject _choiceButtonPrefab;

    [FormerlySerializedAs("close_selection_button")]
    [SerializeField] private Button _closeSelectionButton;

    [FormerlySerializedAs("exit_button")]
    [SerializeField] private Button _exitButton;

    private void Awake()
    {
        if (_closeSelectionButton != null)
        {
            _closeSelectionButton.onClick.AddListener(CloseSelectionPanel);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem("iron_sword", 1);
            InventoryManager.Instance.AddItem("long_sword", 1);
            InventoryManager.Instance.AddItem("wood_shield", 1);
        }

        if (_slotUis != null)
        {
            foreach (SlotUIElement ui in _slotUis)
            {
                if (ui.SlotButton == null)
                {
                    continue;
                }

                EquipSlot slot = ui.Slot;
                ui.SlotButton.onClick.AddListener(() => OpenSelectionPanel(slot));
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += RefreshUI;
        }

        CloseSelectionPanel();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (_closeSelectionButton != null)
        {
            _closeSelectionButton.onClick.RemoveListener(CloseSelectionPanel);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= RefreshUI;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.Equipment)
        {
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        RefreshSlotUI();
        RefreshStatsUI();
    }

    private void RefreshSlotUI()
    {
        if (_slotUis == null || EquipmentManager.Instance == null)
        {
            return;
        }

        foreach (SlotUIElement ui in _slotUis)
        {
            if (ui.ItemNameText == null)
            {
                continue;
            }

            ItemInstance equippedInstance = EquipmentManager.Instance.GetEquippedItem(ui.Slot);
            ItemData equippedItem = equippedInstance != null && ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItem(equippedInstance.ItemId)
                : null;

            if (equippedItem == null)
            {
                ui.ItemNameText.text = "[ 비어 있음 ]";
                continue;
            }

            string enhanceText = equippedInstance != null && equippedInstance.EnhanceLevel > 0
                ? $" +{equippedInstance.EnhanceLevel}"
                : string.Empty;
            ui.ItemNameText.text = $"{equippedItem.ItemName}{enhanceText}";
        }
    }

    private void RefreshStatsUI()
    {
        if (_statsDisplayText == null)
        {
            return;
        }

        StringBuilder builder = new StringBuilder();

        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
        {
            float totalValue = PlayerStats.Instance != null
                ? PlayerStats.Instance.GetTotalStat(stat)
                : 0f;
            string statName = GetStatNameKorean(stat);
            builder.AppendLine($"- {statName}: {totalValue}");
        }

        _statsDisplayText.text = builder.ToString();
    }

    private void OpenSelectionPanel(EquipSlot targetSlot)
    {
        if (_selectionPanel == null || _choiceContainer == null || _choiceButtonPrefab == null)
        {
            return;
        }

        _selectionPanel.SetActive(true);

        for (int i = _choiceContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_choiceContainer.GetChild(i).gameObject);
        }

        GameObject unequipButtonObject = Instantiate(_choiceButtonPrefab, _choiceContainer);
        Button unequipButton = unequipButtonObject.GetComponent<Button>();
        TextMeshProUGUI unequipText = unequipButtonObject.GetComponentInChildren<TextMeshProUGUI>();

        if (unequipText != null)
        {
            unequipText.text = "[ 장착 해제 ]";
        }

        if (unequipButton != null)
        {
            unequipButton.onClick.AddListener(() =>
            {
                if (EquipmentManager.Instance != null)
                {
                    EquipmentManager.Instance.UnequipItem(targetSlot);
                }

                CloseSelectionPanel();
                RefreshUI();
            });
        }

        if (InventoryManager.Instance == null
            || ItemDatabase.Instance == null
            || EquipmentManager.Instance == null)
        {
            return;
        }

        foreach (ItemInstance instance in InventoryManager.Instance.GetAllInstances())
        {
            if (instance == null || instance.Count < 1)
            {
                continue;
            }

            ItemData data = ItemDatabase.Instance.GetItem(instance.ItemId);

            if (data == null
                || data.MainCategory != ItemMainCategory.Equipment
                || !EquipmentManager.Instance.CanEquipToSlot(data, targetSlot))
            {
                continue;
            }

            string instanceId = instance.InstanceId;
            int count = instance.Count;

            GameObject choiceButtonObject = Instantiate(_choiceButtonPrefab, _choiceContainer);
            Button choiceButton = choiceButtonObject.GetComponent<Button>();
            TextMeshProUGUI choiceText = choiceButtonObject.GetComponentInChildren<TextMeshProUGUI>();

            if (choiceText != null)
            {
                string enhanceText = instance.EnhanceLevel > 0 ? $" +{instance.EnhanceLevel}" : string.Empty;
                choiceText.text = $"[{GetTierNameFromDb(data)}] {data.ItemName}{enhanceText} (보유: {count}개)";
            }

            if (choiceButton != null)
            {
                choiceButton.onClick.AddListener(() =>
                {
                    EquipmentManager.Instance.EquipFromInventory(instanceId, targetSlot);
                    CloseSelectionPanel();
                    RefreshUI();
                });
            }
        }
    }

    private void CloseSelectionPanel()
    {
        if (_selectionPanel != null)
        {
            _selectionPanel.SetActive(false);
        }
    }

    private string GetTierNameFromDb(ItemData data)
    {
        if (data == null)
        {
            return "凡";
        }

        switch (data.Tier)
        {
            case ItemTier.Tier1:
                return "凡";

            case ItemTier.Tier2:
                return "奇";

            case ItemTier.Tier3:
                return "珍";

            case ItemTier.Tier4:
                return "傑";

            case ItemTier.Tier5:
                return "古";

            case ItemTier.Tier6:
                return "遺";

            case ItemTier.Tier7:
                return "聖";

            case ItemTier.Tier8:
                return "傳";

            case ItemTier.Tier9:
                return "神";

            case ItemTier.Tier10:
                return "極";

            default:
                return "凡";
        }
    }

    private string GetStatNameKorean(StatType stat)
    {
        switch (stat)
        {
            case StatType.Hp:
                return "체력 (HP)";

            case StatType.Mp:
                return "마나 (MP)";

            case StatType.Sp:
                return "지구력 (SP)";

            case StatType.PhysAtk:
                return "물리 공격력";

            case StatType.MagAtk:
                return "마법 공격력";

            case StatType.Accuracy:
                return "명중률";

            case StatType.CritRate:
                return "치명타율";

            case StatType.StatusInflict:
                return "상태이상 부여";

            case StatType.PhysDef:
                return "물리 방어력";

            case StatType.MagDef:
                return "마법 방어력";

            case StatType.Evasion:
                return "회피력";

            case StatType.GuardRate:
                return "가드율";

            case StatType.StatusResist:
                return "상태이상 저항";

            case StatType.Speed:
                return "속도";

            case StatType.Luck:
                return "운";

            default:
                return stat.ToString();
        }
    }

    private void OnExitButtonClicked()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.SetGameState(GameState.MainMenu);
    }
}

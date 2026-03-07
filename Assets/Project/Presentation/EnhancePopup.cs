using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class EnhancePopup : MonoBehaviour
    {
        [Header("팝업 루트")]
        public GameObject popupRoot;
        public Button closeButton;

        [Header("대상 유닛 패널")]
        public Image targetPanelImage;          // 등급별 스프라이트 교체
        public Image targetUnitIconBG;          // 등급별 스프라이트 교체
        public Image targetUnitIcon;
        public TextMeshProUGUI targetNameText;
        public Image targetRarityChipImage;     // 등급별 스프라이트 교체
        public TextMeshProUGUI targetRarityText;
        public TextMeshProUGUI targetAttrText;
        public Image targetLevelBadgeImage;     // 등급별 스프라이트 교체
        public TextMeshProUGUI targetLevelText;

        [Header("등급별 스프라이트")]
        public Sprite panelCommon;
        public Sprite panelRare;
        public Sprite panelEpic;
        public Sprite panelLegendary;

        public Sprite iconBGCommon;
        public Sprite iconBGRare;
        public Sprite iconBGEpic;
        public Sprite iconBGLegendary;

        public Sprite chipCommon;
        public Sprite chipRare;
        public Sprite chipEpic;
        public Sprite chipLegendary;
        public Sprite badgeCommon;
        public Sprite badgeRare;
        public Sprite badgeEpic;
        public Sprite badgeLegendary;

        [Header("스탯 미리보기")]
        public TextMeshProUGUI hpCurrentText;
        public TextMeshProUGUI hpAfterText;
        public RectTransform hpBarFill;
        public TextMeshProUGUI atkCurrentText;
        public TextMeshProUGUI atkAfterText;
        public RectTransform atkBarFill;
        public TextMeshProUGUI spdCurrentText;
        public TextMeshProUGUI spdAfterText;
        public RectTransform spdBarFill;

        [Header("재료 패널")]
        public Transform slotRow;
        public GameObject materialSlotPrefab;
        public TextMeshProUGUI materialCountText;
        public TextMeshProUGUI goldCostText;
        public TextMeshProUGUI goldCurrentText;

        [Header("유닛 피커")]
        public GameObject pickerPanel;
        public Transform pickerGrid;
        public GameObject pickerCardPrefab;

        [Header("강화 버튼")]
        public Button enhanceButton;

        [Header("연동")]
        public UnitDetailPopup unitDetailPopup;

        // 내부 상태
        private OwnedUnitInstance targetInstance;
        private UnitDefinition targetDef;
        private List<OwnedUnitInstance> selectedMaterials = new List<OwnedUnitInstance>();
        private List<Button> slotButtons = new List<Button>();
        private List<Image> slotIconImages = new List<Image>();
        private int activeSlotIndex = -1;

        void Awake()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            if (pickerPanel != null) pickerPanel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (enhanceButton != null) enhanceButton.onClick.AddListener(OnEnhanceClicked);
        }

        // ── 열기 / 닫기 ──────────────────────────

        private System.Action onRefreshCallback;

        public void Open(OwnedUnitInstance inst, System.Action onRefresh = null)
        {
            targetInstance = inst;
            targetDef = PlayerInventory.Instance.GetDefinition(inst.unitName);
            if (targetDef == null) return;

            onRefreshCallback = onRefresh;
            selectedMaterials.Clear();
            activeSlotIndex = -1;

            if (popupRoot != null) popupRoot.SetActive(true);
            if (pickerPanel != null) pickerPanel.SetActive(false);

            SetupTargetPanel();
            SetupSlots();
            UpdateStats();
            UpdateGold();
            UpdateEnhanceButton();
        }

        public void Close()
        {
            if (popupRoot != null) popupRoot.SetActive(false);
            // DetailPopup으로 복귀
            if (unitDetailPopup != null && targetInstance != null)
                unitDetailPopup.Open(targetInstance, unitDetailPopup.LastEnhanceCallback);
        }

        // ── 대상 패널 ────────────────────────────

        void SetupTargetPanel()
        {
            int lv = targetInstance.enhanceLevel;
            UnitRarity rarity = targetDef.rarity;

            // 스프라이트 교체
            if (targetPanelImage != null) targetPanelImage.sprite = GetPanelSprite(rarity);
            if (targetUnitIconBG != null) targetUnitIconBG.sprite = GetIconBGSprite(rarity);
            if (targetLevelBadgeImage != null) targetLevelBadgeImage.sprite = GetBadgeSpriteByLevel(lv);
            if (targetRarityChipImage != null) targetRarityChipImage.sprite = GetChipSprite(rarity);
            if (targetRarityText != null) targetRarityText.color = GetRarityColor(rarity);

            // 아이콘
            if (targetUnitIcon != null)
            {
                targetUnitIcon.sprite = targetDef.GetIcon(lv);
                targetUnitIcon.color = Color.white;
            }

            if (targetNameText != null) targetNameText.text = targetDef.unitName;
            if (targetRarityText != null) targetRarityText.text = GetRarityText(rarity);
            if (targetAttrText != null) targetAttrText.text = GetAttrText(targetDef);

            // 레벨 뱃지
            if (targetLevelText != null)
            {
                targetLevelText.text = $"+{lv}";
                targetLevelText.color = GetLevelColor(lv);
            }
        }

        // ── 재료 슬롯 ────────────────────────────

        void SetupSlots()
        {
            foreach (Transform child in slotRow) Destroy(child.gameObject);
            slotButtons.Clear();
            slotIconImages.Clear();

            int need = targetDef.enhanceMaterialCount;
            selectedMaterials = new List<OwnedUnitInstance>(new OwnedUnitInstance[need]);

            for (int i = 0; i < need; i++)
            {
                int idx = i;
                GameObject slotObj = Instantiate(materialSlotPrefab, slotRow);
                Button btn = slotObj.GetComponent<Button>();
                Image icon = slotObj.transform.Find("SlotIcon")?.GetComponent<Image>();

                if (btn != null) btn.onClick.AddListener(() => OnSlotClicked(idx));

                slotButtons.Add(btn);
                slotIconImages.Add(icon);

                if (icon != null) icon.enabled = false;
            }

            UpdateMaterialCount();
        }

        void OnSlotClicked(int idx)
        {
            activeSlotIndex = idx;
            OpenPicker();
        }

        void UpdateMaterialCount()
        {
            int filled = 0;
            foreach (var m in selectedMaterials) if (m != null) filled++;
            if (materialCountText != null)
                materialCountText.text = $"{filled}/{targetDef.enhanceMaterialCount}";
        }

        // ── 피커 ─────────────────────────────────

        void OpenPicker()
        {
            if (pickerPanel == null) return;
            pickerPanel.SetActive(true);

            foreach (Transform child in pickerGrid) Destroy(child.gameObject);

            var candidates = new List<OwnedUnitInstance>();
            foreach (var inst in PlayerInventory.Instance.ownedUnitInstances)
            {
                if (inst == targetInstance) continue;
                if (inst.unitName != targetDef.unitName) continue;
                if (selectedMaterials.Contains(inst)) continue;
                candidates.Add(inst);
            }

            foreach (var cand in candidates)
            {
                OwnedUnitInstance captured = cand;
                GameObject cardObj = Instantiate(pickerCardPrefab, pickerGrid);
                PickerCard card = cardObj.GetComponent<PickerCard>();
                if (card != null)
                    card.Initialize(targetDef, cand, (_) => OnPickerCardSelected(captured));
            }

            if (candidates.Count == 0)
            {
                GameObject cardObj = Instantiate(pickerCardPrefab, pickerGrid);
                Button btn = cardObj.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }
        }

        void OnPickerCardSelected(OwnedUnitInstance inst)
        {
            if (activeSlotIndex < 0 || activeSlotIndex >= selectedMaterials.Count) return;

            selectedMaterials[activeSlotIndex] = inst;

            // 슬롯 아이콘 업데이트
            Image slotIcon = slotIconImages[activeSlotIndex];
            if (slotIcon != null)
            {
                slotIcon.sprite = targetDef.GetIcon(inst.enhanceLevel);
                slotIcon.color = Color.white;
                slotIcon.enabled = true;
            }

            if (pickerPanel != null) pickerPanel.SetActive(false);
            activeSlotIndex = -1;

            UpdateMaterialCount();
            UpdateStats();
            UpdateEnhanceButton();
        }

        // ── 스탯 ─────────────────────────────────

        void UpdateStats()
        {
            int lv = targetInstance.enhanceLevel;
            bool allFilled = IsAllSlotsFilled();

            if (hpCurrentText != null) hpCurrentText.text = $"{targetDef.GetEnhancedHP(lv)}";
            if (atkCurrentText != null) atkCurrentText.text = $"{targetDef.GetEnhancedAttack(lv)}";
            if (spdCurrentText != null) spdCurrentText.text = $"{targetDef.GetEnhancedSpeed(lv)}";

            if (allFilled && lv < 10)
            {
                if (hpAfterText != null) hpAfterText.text = $"→ {targetDef.GetEnhancedHP(lv + 1)}";
                if (atkAfterText != null) atkAfterText.text = $"→ {targetDef.GetEnhancedAttack(lv + 1)}";
                if (spdAfterText != null) spdAfterText.text = $"→ {targetDef.GetEnhancedSpeed(lv + 1)}";
            }
            else
            {
                if (hpAfterText != null) hpAfterText.text = "→ ?";
                if (atkAfterText != null) atkAfterText.text = "→ ?";
                if (spdAfterText != null) spdAfterText.text = "→ ?";
            }

            SetBar(hpBarFill, (float)targetDef.GetEnhancedHP(lv) / targetDef.GetEnhancedHP(10));
            SetBar(atkBarFill, (float)targetDef.GetEnhancedAttack(lv) / targetDef.GetEnhancedAttack(10));
            SetBar(spdBarFill, (float)targetDef.GetEnhancedSpeed(lv) / targetDef.GetEnhancedSpeed(10));
        }

        void SetBar(RectTransform fill, float ratio)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0, 0);
            fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        // ── 골드 / 버튼 ──────────────────────────

        void UpdateGold()
        {
            if (goldCostText != null) goldCostText.text = $"{targetDef.enhanceGoldCost:N0}";
            if (goldCurrentText != null) goldCurrentText.text = $"{PlayerInventory.Instance.currency:N0}";
        }

        void UpdateEnhanceButton()
        {
            if (enhanceButton == null) return;
            bool can = IsAllSlotsFilled()
                && targetInstance.enhanceLevel < 10
                && PlayerInventory.Instance.currency >= targetDef.enhanceGoldCost;
            enhanceButton.interactable = can;
        }

        void OnEnhanceClicked()
        {
            foreach (var mat in selectedMaterials)
                if (mat != null) PlayerInventory.Instance.ownedUnitInstances.Remove(mat);

            PlayerInventory.Instance.SpendCurrency(targetDef.enhanceGoldCost);
            targetInstance.enhanceLevel++;
            PlayerInventory.Instance.SaveInventory();

            selectedMaterials.Clear();
            SetupTargetPanel();
            SetupSlots();
            UpdateStats();
            UpdateGold();
            UpdateEnhanceButton();
            onRefreshCallback?.Invoke();
        }

        bool IsAllSlotsFilled()
        {
            foreach (var m in selectedMaterials) if (m == null) return false;
            return selectedMaterials.Count > 0;
        }

        // ── 유틸 ─────────────────────────────────

        Color GetLevelColor(int level)
        {
            if (level <= 3) return new Color(0.54f, 0.61f, 0.71f);
            if (level <= 6) return new Color(0.29f, 0.62f, 1.00f);
            if (level <= 8) return new Color(0.61f, 0.36f, 0.90f);
            return new Color(0.96f, 0.77f, 0.19f);
        }
        Color GetRarityColor(UnitRarity r)
        {
            switch (r)
            {
                case UnitRarity.Common: return new Color(0.54f, 0.61f, 0.71f);
                case UnitRarity.Rare: return new Color(0.29f, 0.62f, 1.00f);
                case UnitRarity.Epic: return new Color(0.61f, 0.36f, 0.90f);
                case UnitRarity.Legendary: return new Color(0.96f, 0.77f, 0.19f);
                default: return Color.white;
            }
        }
        Sprite GetChipSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return chipRare; case UnitRarity.Epic: return chipEpic; case UnitRarity.Legendary: return chipLegendary; default: return chipCommon; } }
        Sprite GetPanelSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return panelRare; case UnitRarity.Epic: return panelEpic; case UnitRarity.Legendary: return panelLegendary; default: return panelCommon; } }
        Sprite GetIconBGSprite(UnitRarity r) { switch (r) { case UnitRarity.Rare: return iconBGRare; case UnitRarity.Epic: return iconBGEpic; case UnitRarity.Legendary: return iconBGLegendary; default: return iconBGCommon; } }
        Sprite GetBadgeSpriteByLevel(int level)
        {
            if (level <= 3) return badgeCommon;
            if (level <= 6) return badgeRare;
            if (level <= 8) return badgeEpic;
            return badgeLegendary;
        }

        string GetRarityText(UnitRarity r)
        {
            switch (r) { case UnitRarity.Common: return "커먼"; case UnitRarity.Rare: return "레어"; case UnitRarity.Epic: return "에픽"; case UnitRarity.Legendary: return "레전더리"; default: return ""; }
        }

        string GetAttrText(UnitDefinition unit)
        {
            if (unit.isKing) return "킹";
            if (unit.isPawn) return "폰";
            bool hasRook = (unit.moveAttributes & MovementAttribute.Rook) != 0;
            bool hasBishop = (unit.moveAttributes & MovementAttribute.Bishop) != 0;
            bool hasKnight = (unit.moveAttributes & MovementAttribute.Knight) != 0;
            if (hasRook && hasBishop) return "퀸";
            if (hasRook) return "룩";
            if (hasBishop) return "비숍";
            if (hasKnight) return "나이트";
            return "";
        }
    }
}
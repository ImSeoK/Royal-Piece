using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;

namespace Chess.Presentation
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("HUD")]
        public Button backButton;
        public TextMeshProUGUI unitCountText;

        [Header("필터 - 등급")]
        public Button filterAll;
        public Button filterCommon;
        public Button filterRare;
        public Button filterEpic;
        public Button filterLegendary;

        [Header("필터 - 속성")]
        public Button filterAllAttr;
        public Button filterRook;
        public Button filterBishop;
        public Button filterKnight;
        public Button filterQueen;

        [Header("필터 칩 스프라이트")]
        public Sprite chipCommon;
        public Sprite chipRare;
        public Sprite chipEpic;
        public Sprite chipLegendary;

        [Header("카드 그리드")]
        public Transform cardContent;
        public GameObject inventoryCardPrefab;

        [Header("판매 버튼")]
        public Button sellButton;

        [Header("다중선택 모드 UI")]
        public GameObject sellConfirmBar;
        public TextMeshProUGUI sellCountText;
        public TextMeshProUGUI sellTotalGoldText;
        public Button sellConfirmButton;
        public Button sellCancelButton;

        [Header("팝업")]
        public UnitDetailPopup unitDetailPopup;
        public EnhancePopup enhancePopup;

        // 필터 상태
        private UnitRarity? selectedRarity = null;
        private MovementAttribute? selectedAttr = null;

        // 선택 상태
        private OwnedUnitInstance selectedInstance = null;
        private List<OwnedUnitInstance> multiSelected = new List<OwnedUnitInstance>();
        private bool isMultiSelectMode = false;

        private List<GameObject> spawnedCards = new List<GameObject>();
        private Dictionary<OwnedUnitInstance, InventoryCard> instanceToCard = new Dictionary<OwnedUnitInstance, InventoryCard>();

        void Start()
        {
            if (sellConfirmBar != null) sellConfirmBar.SetActive(false);
            RegisterButtons();
            RefreshGrid();
        }

        void RegisterButtons()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (filterAll != null) filterAll.onClick.AddListener(() => SetRarityFilter(null));
            if (filterCommon != null) filterCommon.onClick.AddListener(() => SetRarityFilter(UnitRarity.Common));
            if (filterRare != null) filterRare.onClick.AddListener(() => SetRarityFilter(UnitRarity.Rare));
            if (filterEpic != null) filterEpic.onClick.AddListener(() => SetRarityFilter(UnitRarity.Epic));
            if (filterLegendary != null) filterLegendary.onClick.AddListener(() => SetRarityFilter(UnitRarity.Legendary));
            if (filterAllAttr != null) filterAllAttr.onClick.AddListener(() => SetAttrFilter(null));
            if (filterRook != null) filterRook.onClick.AddListener(() => SetAttrFilter(MovementAttribute.Rook));
            if (filterBishop != null) filterBishop.onClick.AddListener(() => SetAttrFilter(MovementAttribute.Bishop));
            if (filterKnight != null) filterKnight.onClick.AddListener(() => SetAttrFilter(MovementAttribute.Knight));
            if (filterQueen != null) filterQueen.onClick.AddListener(() => SetAttrFilter(MovementAttribute.Queen));
            if (sellButton != null) sellButton.onClick.AddListener(EnterMultiSelectMode);
            if (sellConfirmButton != null) sellConfirmButton.onClick.AddListener(OnSellConfirm);
            if (sellCancelButton != null) sellCancelButton.onClick.AddListener(ExitMultiSelectMode);
        }

        void OnBackClicked()
        {
            if (isMultiSelectMode) { ExitMultiSelectMode(); return; }
            SceneManager.LoadScene("MainMenu");
        }

        // ── 필터 ─────────────────────────────────

        void SetRarityFilter(UnitRarity? rarity)
        {
            selectedRarity = rarity;
            RefreshGrid();
            UpdateFilterVisuals();
        }

        void SetAttrFilter(MovementAttribute? attr)
        {
            selectedAttr = attr;
            RefreshGrid();
            UpdateFilterVisuals();
        }

        // ── 카드 그리드 ──────────────────────────

        void RefreshGrid()
        {
            foreach (var card in spawnedCards)
                if (card != null) Destroy(card);
            spawnedCards.Clear();
            instanceToCard.Clear();

            if (PlayerInventory.Instance == null) return;

            var instances = PlayerInventory.Instance.ownedUnitInstances;
            var filtered = instances.Where(inst =>
            {
                if (inst == null) return false;
                UnitDefinition u = PlayerInventory.Instance.GetDefinition(inst.unitName);
                if (u == null) return false;
                if (selectedRarity.HasValue && u.rarity != selectedRarity.Value) return false;
                if (selectedAttr.HasValue)
                {
                    if (selectedAttr.Value == MovementAttribute.Queen)
                    {
                        bool hasRook = (u.moveAttributes & MovementAttribute.Rook) != 0;
                        bool hasBishop = (u.moveAttributes & MovementAttribute.Bishop) != 0;
                        if (!(hasRook && hasBishop)) return false;
                    }
                    else
                    {
                        if ((u.moveAttributes & selectedAttr.Value) == 0) return false;
                        if (selectedAttr.Value == MovementAttribute.Rook ||
                            selectedAttr.Value == MovementAttribute.Bishop)
                        {
                            bool hasRook = (u.moveAttributes & MovementAttribute.Rook) != 0;
                            bool hasBishop = (u.moveAttributes & MovementAttribute.Bishop) != 0;
                            if (hasRook && hasBishop) return false;
                        }
                    }
                }
                return true;
            }).ToList();

            var sorted = filtered.OrderByDescending(inst => {
                UnitDefinition u = PlayerInventory.Instance.GetDefinition(inst.unitName);
                if (u == null) return 0;
                switch (u.rarity)
                {
                    case UnitRarity.Legendary: return 3;
                    case UnitRarity.Epic: return 2;
                    case UnitRarity.Rare: return 1;
                    default: return 0;
                }
            }).ToList();

            foreach (var inst in sorted)
                SpawnCard(inst);

            if (unitCountText != null)
                unitCountText.text = $"{filtered.Count} / {instances.Count}";
        }

        void SpawnCard(OwnedUnitInstance inst)
        {
            if (inventoryCardPrefab == null || cardContent == null) return;
            UnitDefinition unit = PlayerInventory.Instance.GetDefinition(inst.unitName);
            if (unit == null) return;

            GameObject cardObj = Instantiate(inventoryCardPrefab, cardContent);
            InventoryCard card = cardObj.GetComponent<InventoryCard>();
            if (card != null)
            {
                card.Initialize(unit, inst, (_) => OnCardClicked(inst, card));
                instanceToCard[inst] = card;
            }
            spawnedCards.Add(cardObj);
        }

        // ── 카드 클릭 ────────────────────────────

        void OnCardClicked(OwnedUnitInstance inst, InventoryCard card)
        {
            if (isMultiSelectMode) { ToggleMultiSelect(inst, card); return; }
            selectedInstance = inst;
            if (unitDetailPopup != null)
                unitDetailPopup.Open(inst, OnEnhanceRequested);
        }

        // ── 팝업 연동 ────────────────────────────

        void OnEnhanceRequested(OwnedUnitInstance inst)
        {
            if (enhancePopup != null)
                enhancePopup.Open(inst, RefreshGrid);
        }

        // ── 다중선택 모드 ────────────────────────

        void EnterMultiSelectMode()
        {
            isMultiSelectMode = true;
            multiSelected.Clear();
            if (sellConfirmBar != null) sellConfirmBar.SetActive(true);
            UpdateSellConfirmBar();
        }

        void ExitMultiSelectMode()
        {
            isMultiSelectMode = false;
            multiSelected.Clear();
            if (sellConfirmBar != null) sellConfirmBar.SetActive(false);
            foreach (var kv in instanceToCard) kv.Value.SetHighlight(false);
        }

        void ToggleMultiSelect(OwnedUnitInstance inst, InventoryCard card)
        {
            if (multiSelected.Contains(inst)) { multiSelected.Remove(inst); card.SetHighlight(false); }
            else { multiSelected.Add(inst); card.SetHighlight(true); }
            UpdateSellConfirmBar();
        }

        void UpdateSellConfirmBar()
        {
            int totalGold = 0;
            foreach (var inst in multiSelected)
            {
                UnitDefinition def = PlayerInventory.Instance.GetDefinition(inst.unitName);
                if (def != null) totalGold += GetSellPrice(def.rarity);
            }
            if (sellCountText != null) sellCountText.text = $"{multiSelected.Count}개 선택";
            if (sellTotalGoldText != null) sellTotalGoldText.text = $"총 {totalGold:N0} 골드";
            if (sellConfirmButton != null) sellConfirmButton.interactable = multiSelected.Count > 0;
        }

        void OnSellConfirm()
        {
            foreach (var inst in new List<OwnedUnitInstance>(multiSelected))
                PlayerInventory.Instance.SellUnit(inst);
            ExitMultiSelectMode();
            RefreshGrid();
        }

        // ── 필터 시각화 ──────────────────────────

        void UpdateFilterVisuals()
        {
            SetFilterChip(filterAll, selectedRarity == null);
            SetFilterChip(filterCommon, selectedRarity == UnitRarity.Common);
            SetFilterChip(filterRare, selectedRarity == UnitRarity.Rare);
            SetFilterChip(filterEpic, selectedRarity == UnitRarity.Epic);
            SetFilterChip(filterLegendary, selectedRarity == UnitRarity.Legendary);
            SetFilterChip(filterAllAttr, selectedAttr == null);
            SetFilterChip(filterRook, selectedAttr == MovementAttribute.Rook);
            SetFilterChip(filterBishop, selectedAttr == MovementAttribute.Bishop);
            SetFilterChip(filterKnight, selectedAttr == MovementAttribute.Knight);
            SetFilterChip(filterQueen, selectedAttr == MovementAttribute.Queen);
        }

        void SetFilterChip(Button btn, bool active)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = active ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        // ── 유틸 ─────────────────────────────────

        int GetSellPrice(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Common: return 50;
                case UnitRarity.Rare: return 150;
                case UnitRarity.Epic: return 400;
                case UnitRarity.Legendary: return 1000;
                default: return 50;
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;

namespace Chess.Presentation
{
    public class DeckBuilderManager : MonoBehaviour
    {
        private const string SAVE_KEY = "PlayerDeck";

        [Header("HUD")]
        public Button backButton;
        public Button confirmButton;
        public Button saveButton;
        public TextMeshProUGUI slotCountText;

        [Header("슬롯 인디케이터")]
        public List<Image> slotIndicators;

        [Header("체스판")]
        public Transform boardGrid;
        public GameObject boardCellPrefab;

        [Header("유닛 피커")]
        public Transform pickerContent;
        public GameObject unitCardPrefab;

        [Header("이동범위 이미지")]
        public Sprite rangeSpritePawn;
        public Sprite rangeSpriteRook;
        public Sprite rangeSpriteBishop;
        public Sprite rangeSpriteKnight;
        public Sprite rangeSpriteQueen;
        public Sprite rangeSpriteKing;

        [Header("킹/폰 고정")]
        public UnitDefinition defaultKing;
        public UnitDefinition defaultPawn;

        [Header("스킬 패널")]
        public SkillSelectPopup skillSelectPopup;
        public Button skillSlot1Button;
        public Button skillSlot2Button;

        [Header("설정")]
        public int maxFreeSlots = 7;

        private readonly Color indicatorEmpty = new Color(0.16f, 0.19f, 0.31f);
        private readonly Color indicatorCommon = new Color(0.54f, 0.61f, 0.71f);
        private readonly Color indicatorRare = new Color(0.29f, 0.62f, 1.00f);
        private readonly Color indicatorEpic = new Color(0.61f, 0.36f, 0.90f);
        private readonly Color indicatorLegendary = new Color(0.96f, 0.77f, 0.19f);

        private UnitDefinition[] currentDeck;
        private BoardCell[] rank1Cells = new BoardCell[8];
        private int[] rank1ToFreeSlot = { 0, 1, 2, -1, 3, 4, 5, 6 };
        private List<GameObject> pickerCards = new List<GameObject>();
        private List<string> savedSkillNames = new List<string>();

        private UnitDefinition selectedUnit;
        private GameObject selectedPickerCard;
        private InventoryCard selectedCard;

        void Start()
        {
            currentDeck = new UnitDefinition[maxFreeSlots];
            RegisterButtons();
            BuildBoard();
            BuildUnitPicker();
            LoadDeck();
            UpdateHUD();
        }

        void RegisterButtons()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
            if (skillSlot1Button != null) skillSlot1Button.onClick.AddListener(() => OpenSkillPicker(0));
            if (skillSlot2Button != null) skillSlot2Button.onClick.AddListener(() => OpenSkillPicker(1));
        }

        void OpenSkillPicker(int slotIndex)
        {
            var skills = GetAvailableSkills();
            if (skills.Count == 0) return;
            if (skillSelectPopup != null)
                skillSelectPopup.OpenForSlot(slotIndex, skills);
        }

        void BuildBoard()
        {
            if (boardCellPrefab == null || boardGrid == null) return;

            foreach (Transform child in boardGrid) Destroy(child.gameObject);

            for (int rank = 8; rank >= 1; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    GameObject cellObj = Instantiate(boardCellPrefab, boardGrid);
                    BoardCell cell = cellObj.GetComponent<BoardCell>();
                    bool isDark = (rank + file) % 2 == 0;

                    if (rank == 1)
                    {
                        if (file == 3)
                            cell.Setup(BoardCell.CellType.KingSlot, -1, OnBoardCellClicked, isDark);
                        else
                            cell.Setup(BoardCell.CellType.FreeSlot, rank1ToFreeSlot[file], OnBoardCellClicked, isDark);
                        rank1Cells[file] = cell;
                    }
                    else if (rank == 2)
                        cell.Setup(BoardCell.CellType.PawnSlot, -1, OnBoardCellClicked, isDark);
                    else
                        cell.Setup(BoardCell.CellType.PawnSlot, -1, OnBoardCellClicked, isDark);
                }
            }
        }

        void BuildUnitPicker()
        {
            ClearPicker();
            if (PlayerInventory.Instance == null) return;

            var units = PlayerInventory.Instance.ownedUnits
                .Where(u => u != null && !u.isKing && !u.isPawn)
                .OrderByDescending(u => {
                    switch (u.rarity)
                    {
                        case UnitRarity.Legendary: return 3;
                        case UnitRarity.Epic: return 2;
                        case UnitRarity.Rare: return 1;
                        default: return 0;
                    }
                }).ToList();

            foreach (var unit in units)
                SpawnPickerCard(unit);
        }

        void SpawnPickerCard(UnitDefinition unit)
        {
            if (unitCardPrefab == null || pickerContent == null) return;
            GameObject cardObj = Instantiate(unitCardPrefab, pickerContent);
            InventoryCard card = cardObj.GetComponent<InventoryCard>();
            if (card != null) card.Initialize(unit, OnPickerCardSelected);
            pickerCards.Add(cardObj);
        }

        void ClearPicker()
        {
            foreach (var card in pickerCards) if (card != null) Destroy(card);
            pickerCards.Clear();
        }

        void OnBoardCellClicked(BoardCell cell)
        {
            if (cell.cellType != BoardCell.CellType.FreeSlot) return;

            if (selectedUnit != null)
            {
                PlaceUnitToSlot(cell, selectedUnit, selectedPickerCard);
                ClearSelection();
                return;
            }

            if (cell.placedUnit != null) RemoveUnitFromBoard(cell);
        }

        void OnPickerCardSelected(InventoryCard card)
        {
            if (card.unitDefinition == null) return;
            ClearSelection();
            selectedUnit = card.unitDefinition;
            selectedPickerCard = card.gameObject;
            selectedCard = card;
            card.SetHighlight(true);

            foreach (var cell in rank1Cells)
                if (cell != null && cell.cellType == BoardCell.CellType.FreeSlot)
                    cell.SetAvailableHighlight(true);
        }

        void ClearSelection()
        {
            selectedUnit = null;
            selectedPickerCard = null;

            foreach (var cell in rank1Cells)
                if (cell != null && cell.cellType == BoardCell.CellType.FreeSlot)
                    cell.SetAvailableHighlight(false);

            foreach (var cardObj in pickerCards)
            {
                if (cardObj == null) continue;
                InventoryCard c = cardObj.GetComponent<InventoryCard>();
                if (c != null) c.SetHighlight(false);
            }
        }

        void PlaceUnitToSlot(BoardCell cell, UnitDefinition unit, GameObject pickerCardObj)
        {
            int freeIndex = cell.slotIndex;
            if (freeIndex < 0 || freeIndex >= maxFreeSlots) return;

            if (currentDeck[freeIndex] != null) ReturnUnitToPicker(currentDeck[freeIndex]);

            currentDeck[freeIndex] = unit;
            cell.PlaceUnit(unit);

            if (pickerCardObj != null)
            {
                pickerCards.Remove(pickerCardObj);
                Destroy(pickerCardObj);
            }
            UpdateHUD();
        }

        void RemoveUnitFromBoard(BoardCell cell)
        {
            if (cell == null || cell.placedUnit == null) return;
            int freeIndex = cell.slotIndex;
            UnitDefinition unit = currentDeck[freeIndex];
            currentDeck[freeIndex] = null;
            cell.ClearUnit();
            ReturnUnitToPicker(unit);
            UpdateHUD();
        }

        void ReturnUnitToPicker(UnitDefinition unit) { if (unit != null) SpawnPickerCard(unit); }

        // ── 스킬 선택 ────────────────────────────

        List<SkillDefinition> GetAvailableSkills()
        {
            var skills = new List<SkillDefinition>();
            foreach (var unit in currentDeck)
            {
                if (unit == null || unit.activeSkill == null) continue;
                if (!skills.Contains(unit.activeSkill))
                    skills.Add(unit.activeSkill);
            }
            return skills;
        }

        // ── 저장 / 로드 ──────────────────────────

        void SaveDeck(List<string> selectedSkillNames = null)
        {
            SavedDeckData data = new SavedDeckData();

            for (int i = 0; i < currentDeck.Length; i++)
            {
                if (currentDeck[i] == null) continue;
                data.slots.Add(new SlotEntry { slotIndex = i, unitName = currentDeck[i].unitName });
                data.customUnitNames.Add(currentDeck[i].unitName);
            }

            if (defaultKing != null) data.kingName = defaultKing.unitName;
            if (defaultPawn != null) data.pawnName = defaultPawn.unitName;

            if (selectedSkillNames != null)
                foreach (var name in selectedSkillNames)
                    data.selectedSkillNames.Add(name);

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        void LoadDeck()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
            string json = PlayerPrefs.GetString(SAVE_KEY);
            SavedDeckData data = JsonUtility.FromJson<SavedDeckData>(json);
            if (data == null) return;

            savedSkillNames = data.selectedSkillNames ?? new List<string>();

            if (data.slots != null && data.slots.Count > 0)
            {
                foreach (var entry in data.slots)
                {
                    if (entry.slotIndex < 0 || entry.slotIndex >= maxFreeSlots) continue;
                    UnitDefinition unit = PlayerInventory.Instance.ownedUnits.Find(u => u.unitName == entry.unitName);
                    if (unit == null) continue;
                    BoardCell targetCell = GetCellByFreeIndex(entry.slotIndex);
                    if (targetCell != null)
                    {
                        currentDeck[entry.slotIndex] = unit;
                        targetCell.PlaceUnit(unit);
                        RemoveFromPickerByUnit(unit);
                    }
                }
            }
            else if (data.customUnitNames != null && data.customUnitNames.Count > 0)
            {
                int freeIndex = 0;
                foreach (string unitName in data.customUnitNames)
                {
                    if (freeIndex >= maxFreeSlots) break;
                    UnitDefinition unit = PlayerInventory.Instance.ownedUnits.Find(u => u.unitName == unitName);
                    if (unit == null) continue;
                    BoardCell targetCell = GetCellByFreeIndex(freeIndex);
                    if (targetCell != null)
                    {
                        currentDeck[freeIndex] = unit;
                        targetCell.PlaceUnit(unit);
                        RemoveFromPickerByUnit(unit);
                    }
                    freeIndex++;
                }
            }

            UpdateHUD();
        }

        BoardCell GetCellByFreeIndex(int freeIndex)
        {
            for (int file = 0; file < 8; file++)
                if (rank1ToFreeSlot[file] == freeIndex) return rank1Cells[file];
            return null;
        }

        void RemoveFromPickerByUnit(UnitDefinition unit)
        {
            GameObject toRemove = null;
            foreach (var cardObj in pickerCards)
            {
                if (cardObj == null) continue;
                InventoryCard card = cardObj.GetComponent<InventoryCard>();
                if (card != null && card.unitDefinition == unit) { toRemove = cardObj; break; }
            }
            if (toRemove != null) { pickerCards.Remove(toRemove); Destroy(toRemove); }
        }

        void UpdateHUD()
        {
            int count = GetCurrentDeckCount();
            if (slotCountText != null) slotCountText.text = $"{count} / {maxFreeSlots}";

            for (int i = 0; i < slotIndicators.Count; i++)
            {
                if (slotIndicators[i] == null) continue;
                slotIndicators[i].color = (i < currentDeck.Length && currentDeck[i] != null)
                    ? GetRarityIndicatorColor(currentDeck[i].rarity)
                    : indicatorEmpty;
            }

            if (confirmButton != null) confirmButton.interactable = (count == maxFreeSlots);
        }

        int GetCurrentDeckCount()
        {
            int count = 0;
            foreach (var unit in currentDeck) if (unit != null) count++;
            return count;
        }

        Color GetRarityIndicatorColor(UnitRarity rarity)
        {
            switch (rarity)
            {
                case UnitRarity.Common: return indicatorCommon;
                case UnitRarity.Rare: return indicatorRare;
                case UnitRarity.Epic: return indicatorEpic;
                case UnitRarity.Legendary: return indicatorLegendary;
                default: return indicatorCommon;
            }
        }

        // ── 버튼 ─────────────────────────────────

        void OnConfirmClicked()
        {
            if (GetCurrentDeckCount() < maxFreeSlots) return;
            var skillNames = skillSelectPopup != null ? skillSelectPopup.GetSelectedSkillNames() : new List<string>();
            SaveDeck(skillNames);
            GoToGame();
        }

        void GoToGame()
        {
            PlayerDeck playerDeck = ScriptableObject.CreateInstance<PlayerDeck>();
            playerDeck.customUnits = new List<UnitDefinition>();
            foreach (var unit in currentDeck)
                if (unit != null) playerDeck.customUnits.Add(unit);

            playerDeck.king = defaultKing;
            playerDeck.pawn = defaultPawn;

            if (DeckTransfer.Instance != null)
                DeckTransfer.Instance.Player0Deck = playerDeck;

            SceneManager.LoadScene("SampleScene");
        }

        void OnSaveClicked() => SaveDeck();
        void OnBackClicked() => SceneManager.LoadScene("MainMenu");
    }
}
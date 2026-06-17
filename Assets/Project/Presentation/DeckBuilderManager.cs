using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
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

        [Header("토스트 메시지")]
        public TextMeshProUGUI toastText;

        private readonly Color indicatorEmpty = new Color(0.16f, 0.19f, 0.31f);
        private readonly Color indicatorCommon = new Color(0.54f, 0.61f, 0.71f);
        private readonly Color indicatorRare = new Color(0.29f, 0.62f, 1.00f);
        private readonly Color indicatorEpic = new Color(0.61f, 0.36f, 0.90f);
        private readonly Color indicatorLegendary = new Color(0.96f, 0.77f, 0.19f);

        private UnitDefinition[] currentDeck;
        private OwnedUnitInstance[] currentDeckInstances;
        private BoardCell[] rank1Cells = new BoardCell[8];
        private int[] rank1ToFreeSlot = { 0, 1, 2, -1, 3, 4, 5, 6 };
        private List<GameObject> pickerCards = new List<GameObject>();
        private List<string> savedSkillNames = new List<string>();

        private UnitDefinition selectedUnit;
        private OwnedUnitInstance selectedInstance;
        private GameObject selectedPickerCard;
        private InventoryCard selectedCard;

        void Start()
        {
            currentDeck = new UnitDefinition[maxFreeSlots];
            currentDeckInstances = new OwnedUnitInstance[maxFreeSlots];
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
                        {
                            cell.Setup(BoardCell.CellType.KingSlot, -1, OnBoardCellClicked, isDark);
                            if (defaultKing?.typeSymbol != null)
                                cell.SetSymbolSprite(defaultKing.typeSymbol);
                        }
                        else
                            cell.Setup(BoardCell.CellType.FreeSlot, rank1ToFreeSlot[file], OnBoardCellClicked, isDark);
                        rank1Cells[file] = cell;
                    }
                    else if (rank == 2)
                    {
                        cell.Setup(BoardCell.CellType.PawnSlot, -1, OnBoardCellClicked, isDark);
                        if (defaultPawn?.typeSymbol != null)
                            cell.SetSymbolSprite(defaultPawn.typeSymbol);
                    }
                    else
                        cell.Setup(BoardCell.CellType.Normal, -1, OnBoardCellClicked, isDark);
                }
            }
        }

        void BuildUnitPicker()
        {
            ClearPicker();
            if (PlayerInventory.Instance == null) return;

            var entries = PlayerInventory.Instance.ownedUnitInstances
                .Select(inst => new { inst, def = PlayerInventory.Instance.GetDefinition(inst.unitName) })
                .Where(x => x.def != null && !x.def.isKing && !x.def.isPawn)
                .ToList();

            foreach (var x in entries)
                SpawnPickerCard(x.def, x.inst);

            UnitSortUtility.SortPickerCards(pickerCards);
        }

        void SpawnPickerCard(UnitDefinition unit, OwnedUnitInstance inst)
        {
            if (unitCardPrefab == null || pickerContent == null) return;
            GameObject cardObj = Instantiate(unitCardPrefab, pickerContent);

            InventoryCard card = cardObj.GetComponent<InventoryCard>();
            if (card != null) card.Initialize(unit, inst, OnPickerCardSelected);
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
                PlaceUnitToSlot(cell, selectedUnit, selectedInstance, selectedPickerCard);
                ClearSelection();
                return;
            }

            if (cell.placedUnit != null) RemoveUnitFromBoard(cell);
        }

        bool IsQueenAlreadyInDeck()
        {
            foreach (var unit in currentDeck)
                if (unit != null && unit.isQueen) return true;
            return false;
        }

        void RefreshQueenCardStates()
        {
            bool queenInDeck = IsQueenAlreadyInDeck();
            foreach (var cardObj in pickerCards)
            {
                if (cardObj == null) continue;
                InventoryCard c = cardObj.GetComponent<InventoryCard>();
                if (c != null && c.unitDefinition != null && c.unitDefinition.isQueen)
                    c.SetGrayed(queenInDeck);
            }
        }

        void ShowToast(string message)
        {
            if (toastText == null) return;
            StopCoroutine("ToastCoroutine");
            StartCoroutine(ToastCoroutine(message));
        }

        IEnumerator ToastCoroutine(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            toastText.gameObject.SetActive(false);
        }

        void OnPickerCardSelected(InventoryCard card)
        {
            if (card.unitDefinition == null) return;
            if (card.unitDefinition.isQueen && IsQueenAlreadyInDeck())
            {
                ShowToast("퀸은 최대 1명 편성 가능합니다");
                return;
            }
            ClearSelection();
            selectedUnit = card.unitDefinition;
            selectedInstance = card.ownedInstance;
            selectedPickerCard = card.gameObject;
            selectedCard = card;
            card.SetHighlight(true);
            AudioManager.Instance?.Play("sfx_unit_pick");

            foreach (var cell in rank1Cells)
                if (cell != null && cell.cellType == BoardCell.CellType.FreeSlot)
                    cell.SetAvailableHighlight(true);
        }

        void ClearSelection()
        {
            selectedUnit = null;
            selectedInstance = null;
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

        void PlaceUnitToSlot(BoardCell cell, UnitDefinition unit, OwnedUnitInstance inst, GameObject pickerCardObj)
        {
            int freeIndex = cell.slotIndex;
            if (freeIndex < 0 || freeIndex >= maxFreeSlots) return;
            if (unit.isQueen && IsQueenAlreadyInDeck()) return;

            if (currentDeck[freeIndex] != null) ReturnUnitToPicker(currentDeck[freeIndex], currentDeckInstances[freeIndex]);

            currentDeck[freeIndex] = unit;
            currentDeckInstances[freeIndex] = inst;
            cell.PlaceUnit(unit);
            AudioManager.Instance?.Play("sfx_unit_place");

            if (pickerCardObj != null)
            {
                pickerCards.Remove(pickerCardObj);
                Destroy(pickerCardObj);
            }
            RefreshQueenCardStates();
            UpdateHUD();
        }

        void RemoveUnitFromBoard(BoardCell cell)
        {
            if (cell == null || cell.placedUnit == null) return;
            int freeIndex = cell.slotIndex;
            UnitDefinition unit = currentDeck[freeIndex];
            OwnedUnitInstance inst = currentDeckInstances[freeIndex];
            currentDeck[freeIndex] = null;
            currentDeckInstances[freeIndex] = null;
            cell.ClearUnit();
            AudioManager.Instance?.Play("sfx_unit_unequip");
            ReturnUnitToPicker(unit, inst);
            RefreshQueenCardStates();
            UpdateHUD();
        }

        void ReturnUnitToPicker(UnitDefinition unit, OwnedUnitInstance inst)
        {
            if (unit == null) return;
            SpawnPickerCard(unit, inst);
            SortPickerCards();
        }

        void SortPickerCards()
        {
            UnitSortUtility.SortPickerCards(pickerCards);
        }

        // ── 스킬 선택 ────────────────────────────

        List<SkillDefinition> GetAvailableSkills()
        {
            var skills = new List<SkillDefinition>();
            foreach (var unit in currentDeck)
            {
                if (unit == null || unit.activeSkill == null) continue;

                skills.Add(unit.activeSkill);
            }
            return skills;
        }

        // 스킬 이름으로 현재 덱 내 SkillDefinition 탐색
        SkillDefinition FindSkillInDeck(string skillName)
        {
            foreach (var unit in currentDeck)
            {
                if (unit == null) continue;
                if (unit.activeSkill != null && unit.activeSkill.skillName == skillName)
                    return unit.activeSkill;
                foreach (var passive in unit.passiveSkills)
                    if (passive != null && passive.skillName == skillName)
                        return passive;
            }
            return null;
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
                    UnitDefinition unit = PlayerInventory.Instance.GetDefinition(entry.unitName);
                    if (unit == null) continue;
                    if (unit.isQueen && IsQueenAlreadyInDeck()) continue;
                    OwnedUnitInstance inst = FindFreeInstance(entry.unitName);
                    BoardCell targetCell = GetCellByFreeIndex(entry.slotIndex);
                    if (targetCell != null)
                    {
                        currentDeck[entry.slotIndex] = unit;
                        currentDeckInstances[entry.slotIndex] = inst;
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
                    UnitDefinition unit = PlayerInventory.Instance.GetDefinition(unitName);
                    if (unit == null) continue;
                    OwnedUnitInstance inst = FindFreeInstance(unitName);
                    BoardCell targetCell = GetCellByFreeIndex(freeIndex);
                    if (targetCell != null)
                    {
                        currentDeck[freeIndex] = unit;
                        currentDeckInstances[freeIndex] = inst;
                        targetCell.PlaceUnit(unit);
                        RemoveFromPickerByUnit(unit);
                    }
                    freeIndex++;
                }
            }

            RefreshQueenCardStates();
            UpdateHUD();
        }

        BoardCell GetCellByFreeIndex(int freeIndex)
        {
            for (int file = 0; file < 8; file++)
                if (rank1ToFreeSlot[file] == freeIndex) return rank1Cells[file];
            return null;
        }

        OwnedUnitInstance FindFreeInstance(string unitName)
        {
            var usedInstances = new System.Collections.Generic.HashSet<OwnedUnitInstance>();
            foreach (var inst in currentDeckInstances)
                if (inst != null) usedInstances.Add(inst);

            OwnedUnitInstance best = null;
            foreach (var inst in PlayerInventory.Instance.ownedUnitInstances)
            {
                if (inst.unitName != unitName || usedInstances.Contains(inst)) continue;
                if (best == null || inst.enhanceLevel > best.enhanceLevel) best = inst;
            }
            return best;
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
            GoToGame(skillNames);
        }

        void GoToGame(List<string> skillNames = null)
        {
            PlayerDeck playerDeck = ScriptableObject.CreateInstance<PlayerDeck>();
            playerDeck.customUnits = new List<UnitDefinition>();
            playerDeck.customUnitEnhanceLevels = new List<int>();

            for (int i = 0; i < currentDeck.Length; i++)
            {
                if (currentDeck[i] == null) continue;
                playerDeck.customUnits.Add(currentDeck[i]);
                playerDeck.customUnitEnhanceLevels.Add(currentDeckInstances[i]?.enhanceLevel ?? 0);
            }

            playerDeck.king = defaultKing;
            playerDeck.pawn = defaultPawn;
            playerDeck.kingEnhanceLevel = FindFreeInstance(defaultKing?.unitName)?.enhanceLevel ?? 0;
            playerDeck.pawnEnhanceLevel = FindFreeInstance(defaultPawn?.unitName)?.enhanceLevel ?? 0;

            // 선택된 스킬을 SkillDefinition으로 변환해서 PlayerDeck에 담기
            if (skillNames != null)
            {
                foreach (var skillName in skillNames)
                {
                    SkillDefinition skill = FindSkillInDeck(skillName);
                    if (skill != null)
                    {
                        playerDeck.selectedActiveSkills.Add(skill);
                        Debug.Log($"[DeckBuilder] 스킬 전달: {skillName}");
                    }
                }
            }

            if (DeckTransfer.Instance != null)
                DeckTransfer.Instance.Player0Deck = playerDeck;

            SceneManager.LoadScene("SampleScene");
        }

        void OnSaveClicked()
        {
            AudioManager.Instance?.Play("sfx_deck_save");
            SaveDeck();
        }
        void OnBackClicked() => SceneManager.LoadScene("MainMenu");
    }
}
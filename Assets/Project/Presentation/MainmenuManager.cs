using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI")]
        public TextMeshProUGUI currencyText;
        public UnityEngine.UI.Button deckBuilderButton;
        public UnityEngine.UI.Button gachaButton;
        public UnityEngine.UI.Button inventoryButton;
        public UnityEngine.UI.Button startGameButton;

        void Start()
        {
            if (deckBuilderButton != null)
                deckBuilderButton.onClick.AddListener(OnDeckBuilderClicked);

            if (gachaButton != null)
                gachaButton.onClick.AddListener(OnGachaClicked);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryClicked);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            UpdateUI();
        }

        void Update()
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            if (PlayerInventory.Instance != null && currencyText != null)
                currencyText.text = PlayerInventory.Instance.currency.ToString("N0");
        }

        void OnDeckBuilderClicked() => SceneManager.LoadScene("DeckBuilder");
        void OnGachaClicked() => SceneManager.LoadScene("Gacha");
        void OnInventoryClicked() => SceneManager.LoadScene("Inventory");

        void OnStartGameClicked()
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[MainMenu] PlayerInventory가 없습니다!");
                return;
            }

            if (!PlayerPrefs.HasKey("PlayerDeck"))
            {
                Debug.LogWarning("[MainMenu] 저장된 덱이 없습니다! 덱 빌더로 이동합니다.");
                SceneManager.LoadScene("DeckBuilder");
                return;
            }

            if (LoadAndTransferDeck())
                SceneManager.LoadScene("SampleScene");
            else
            {
                Debug.LogWarning("[MainMenu] 덱 로드 실패! 덱 빌더로 이동합니다.");
                SceneManager.LoadScene("DeckBuilder");
            }
        }

        bool LoadAndTransferDeck()
        {
            try
            {
                string json = PlayerPrefs.GetString("PlayerDeck");
                SavedDeckData data = JsonUtility.FromJson<SavedDeckData>(json);

                if (data == null || data.customUnitNames.Count == 0)
                {
                    Debug.LogWarning("[MainMenu] 덱 데이터가 비어있습니다!");
                    return false;
                }

                PlayerDeck playerDeck = ScriptableObject.CreateInstance<PlayerDeck>();
                playerDeck.customUnits = new List<UnitDefinition>();

                // 유닛 로드 — 스킬 탐색 범위로도 사용
                var deckUnits = new List<UnitDefinition>();
                foreach (string unitName in data.customUnitNames)
                {
                    UnitDefinition unit = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == unitName);
                    if (unit != null)
                    {
                        playerDeck.customUnits.Add(unit);
                        deckUnits.Add(unit);
                    }
                    else
                        Debug.LogWarning($"[MainMenu] 유닛을 찾을 수 없음: {unitName}");
                }

                // King / Pawn 로드
                if (!string.IsNullOrEmpty(data.kingName))
                    playerDeck.king = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.kingName);

                if (!string.IsNullOrEmpty(data.pawnName))
                    playerDeck.pawn = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.pawnName);

                // 스킬 로드 — 덱에 편성된 유닛 안에서만 탐색
                // DeckBuilderManager.FindSkillInDeck()과 동일한 탐색 범위로 통일
                if (data.selectedSkillNames != null && data.selectedSkillNames.Count > 0)
                {
                    foreach (string skillName in data.selectedSkillNames)
                    {
                        SkillDefinition found = FindSkillInUnits(skillName, deckUnits);
                        if (found != null)
                        {
                            playerDeck.selectedActiveSkills.Add(found);
                            Debug.Log($"[MainMenu] 스킬 로드: {skillName}");
                        }
                        else
                            Debug.LogWarning($"[MainMenu] 스킬을 찾을 수 없음: {skillName}");
                    }
                }

                if (DeckTransfer.Instance != null)
                {
                    DeckTransfer.Instance.Player0Deck = playerDeck;
                    Debug.Log($"[MainMenu] 덱 로드 완료 — 유닛 {playerDeck.customUnits.Count}개, 스킬 {playerDeck.selectedActiveSkills.Count}개");
                    return true;
                }
                else
                {
                    Debug.LogError("[MainMenu] DeckTransfer가 없습니다!");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenu] 덱 로드 중 에러: {e.Message}");
                return false;
            }
        }

        // 덱에 편성된 유닛 안에서만 스킬 탐색
        // DeckBuilderManager.FindSkillInDeck()과 동일한 범위로 두 경로 통일
        SkillDefinition FindSkillInUnits(string skillName, List<UnitDefinition> units)
        {
            foreach (var unit in units)
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
    }
}
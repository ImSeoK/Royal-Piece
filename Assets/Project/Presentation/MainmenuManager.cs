using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
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
            // 버튼 이벤트 연결
            if (deckBuilderButton != null)
                deckBuilderButton.onClick.AddListener(OnDeckBuilderClicked);

            if (gachaButton != null)
                gachaButton.onClick.AddListener(OnGachaClicked);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryClicked);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            // UI 업데이트
            UpdateUI();
        }

        void Update()
        {
            // 골드 실시간 업데이트
            UpdateUI();
        }

        void UpdateUI()
        {
            if (PlayerInventory.Instance != null && currencyText != null)
            {
                currencyText.text = PlayerInventory.Instance.currency.ToString("N0"); ;
            }
        }

        void OnDeckBuilderClicked()
        {
            Debug.Log("[MainMenu] 덱 빌더로 이동");
            SceneManager.LoadScene("DeckBuilder");
        }

        void OnGachaClicked()
        {
            Debug.Log("[MainMenu] 가챠로 이동");
            SceneManager.LoadScene("Gacha"); 
        }


        void OnInventoryClicked()
        {
            Debug.Log("[MainMenu] 가챠로 이동");
            SceneManager.LoadScene("Inventory");
        }

        void OnStartGameClicked()
        {
            Debug.Log("[MainMenu] 게임 시작");

            // PlayerInventory 확인
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("[MainMenu] PlayerInventory가 없습니다!");
                return;
            }

            // 저장된 덱이 있는지 확인
            if (!PlayerPrefs.HasKey("PlayerDeck"))
            {
                Debug.LogWarning("[MainMenu] 저장된 덱이 없습니다! 덱 빌더로 이동합니다.");
                SceneManager.LoadScene("DeckBuilder");
                return;
            }

            // 저장된 덱 로드 및 DeckTransfer에 할당
            if (LoadAndTransferDeck())
            {
                // 게임 시작
                SceneManager.LoadScene("SampleScene");
            }
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
                // SavedDeckData 로드
                string json = PlayerPrefs.GetString("PlayerDeck");
                SavedDeckData data = JsonUtility.FromJson<SavedDeckData>(json);

                if (data == null || data.customUnitNames.Count == 0)
                {
                    Debug.LogWarning("[MainMenu] 덱 데이터가 비어있습니다!");
                    return false;
                }

                // PlayerDeck 생성
                PlayerDeck playerDeck = ScriptableObject.CreateInstance<PlayerDeck>();
                playerDeck.customUnits = new System.Collections.Generic.List<UnitDefinition>();

                // 유닛 이름 → UnitDefinition 변환
                foreach (string unitName in data.customUnitNames)
                {
                    UnitDefinition unit = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == unitName);

                    if (unit != null)
                    {
                        playerDeck.customUnits.Add(unit);
                    }
                    else
                    {
                        Debug.LogWarning($"[MainMenu] 유닛을 찾을 수 없음: {unitName}");
                    }
                }

                // King 설정
                if (!string.IsNullOrEmpty(data.kingName))
                {
                    playerDeck.king = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.kingName);
                }

                // Pawn 설정
                if (!string.IsNullOrEmpty(data.pawnName))
                {
                    playerDeck.pawn = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.pawnName);
                }

                // DeckTransfer에 할당
                if (DeckTransfer.Instance != null)
                {
                    DeckTransfer.Instance.Player0Deck = playerDeck;
                    Debug.Log($"[MainMenu] 덱 로드 완료! {playerDeck.customUnits.Count}개 유닛");
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
    }
}
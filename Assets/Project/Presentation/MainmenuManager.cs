using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Chess.Core;

namespace Chess.Presentation
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI - 공통")]
        public TextMeshProUGUI currencyText;

        [Header("UI - 버튼")]
        public Button deckBuilderButton;
        public Button gachaButton;
        public Button inventoryButton;
        public Button startGameButton;
        public Button settingsButton;
        public MainMenuSettingsPanel settingsPanel;

        [Header("Player Card")]
        public TextMeshProUGUI playerNameText;
        public string defaultPlayerName = "플레이어";
        public TextMeshProUGUI totalBattleText;

        [Header("Win Rate Bars")]
        public Image winBarImage;
        public Image loseBarImage;
        public Image drawBarImage;
        public TextMeshProUGUI winPercentText;
        public TextMeshProUGUI losePercentText;
        public TextMeshProUGUI drawPercentText;
        public TextMeshProUGUI winRateText;

        [Header("Rank")]
        public Image rankBadgeOutline;
        public Image rankBadgeBg;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI rankPointsText;

        [Header("랭크 뱃지 스프라이트 (D/C/B/A/S)")]
        public Sprite rankBadgeSpriteD;
        public Sprite rankBadgeSpriteC;
        public Sprite rankBadgeSpriteB;
        public Sprite rankBadgeSpriteA;
        public Sprite rankBadgeSpriteS;

        // 티어별 내부 BG 색상
        static readonly Color BADGE_BG_S = new Color(0.24f, 0.17f, 0.05f, 1f);
        static readonly Color BADGE_BG_A = new Color(0.16f, 0.10f, 0.30f, 1f);
        static readonly Color BADGE_BG_B = new Color(0.05f, 0.16f, 0.25f, 1f);
        static readonly Color BADGE_BG_C = new Color(0.10f, 0.10f, 0.10f, 1f);
        static readonly Color BADGE_BG_D = new Color(0.08f, 0.08f, 0.08f, 1f);

        [Header("Daily Bonus")]
        public Button dailyBonusButton;

        [Header("Daily Mission")]
        public Image missionProgressFill;
        public TextMeshProUGUI missionProgressText;

        [Header("Deck Preview")]
        public Transform deckPreviewRoot;

        [Header("모드 선택 패널")]
        public ModeSelectPanel modeSelectPanel;

        [Header("매칭 대기 UI 참조")]
        public GameObject categoryButtonsParent;


        [Header("희귀도별 슬롯 프레임 스프라이트")]
        public Sprite frameCommon;
        public Sprite frameRare;
        public Sprite frameEpic;
        public Sprite frameLegendary;

        void Start()
        {
            // MatchmakingOverlay에 씬 참조 등록
            MatchmakingOverlay.Instance?.RegisterMainMenuRefs(startGameButton, categoryButtonsParent);

            // Firebase 로그인 연동
            if (Chess.Core.FirebaseManager.Instance != null)
            {
                if (Chess.Core.FirebaseManager.Instance.IsLoggedIn)
                    OnFirebaseLogin(Chess.Core.FirebaseManager.Instance.UID);
                else
                    Chess.Core.FirebaseManager.Instance.OnLoginSuccess += OnFirebaseLogin;

                // Firestore 데이터 로드 완료 시 랭크 + UI 갱신
                Chess.Core.FirebaseManager.Instance.SubscribeDataReady(OnFirestoreDataReady);
            }

            if (deckBuilderButton != null)
                deckBuilderButton.onClick.AddListener(OnDeckBuilderClicked);

            if (gachaButton != null)
                gachaButton.onClick.AddListener(OnGachaClicked);

            if (inventoryButton != null)
                inventoryButton.onClick.AddListener(OnInventoryClicked);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => settingsPanel?.Open());

            if (dailyBonusButton != null)
                dailyBonusButton.onClick.AddListener(OnDailyBonusClicked);


            UpdateCurrency();
            UpdatePlayerCard();
            UpdateWinRate();
            UpdateRank();
            UpdateDailyBonus();
            UpdateDailyMission();
            UpdateDeckPreview();
        }

        void Update()
        {
            UpdateCurrency();

        }

        void UpdateCurrency()
        {
            if (PlayerInventory.Instance != null && currencyText != null)
                currencyText.text = PlayerInventory.Instance.currency.ToString("N0");
        }

        void UpdatePlayerCard()
        {
            if (playerNameText != null)
                playerNameText.text = PlayerPrefs.GetString("player_name", defaultPlayerName);

            int total = PlayerPrefs.GetInt("stat_total", 0);
            if (totalBattleText != null)
                totalBattleText.text = $"전투 : {total}";
        }

        void UpdateWinRate()
        {
            int total  = PlayerPrefs.GetInt("stat_total",  0);
            int wins   = PlayerPrefs.GetInt("stat_wins",   0);
            int losses = PlayerPrefs.GetInt("stat_losses", 0);
            int draws  = total - wins - losses;

            float winRate  = total > 0 ? (float)wins   / total : 0f;
            float loseRate = total > 0 ? (float)losses  / total : 0f;
            float drawRate = total > 0 ? (float)draws   / total : 0f;

            if (winBarImage  != null) winBarImage.fillAmount  = winRate;
            if (loseBarImage != null) loseBarImage.fillAmount = loseRate;
            if (drawBarImage != null) drawBarImage.fillAmount = drawRate;

            if (winPercentText  != null) winPercentText.text  = $"{winRate  * 100:F0}%";
            if (losePercentText != null) losePercentText.text = $"{loseRate * 100:F0}%";
            if (drawPercentText != null) drawPercentText.text = $"{drawRate * 100:F0}%";
            if (winRateText     != null) winRateText.text     = $"승률 : {winRate * 100:F0}";
        }

        void UpdateDeckPreview()
        {
            if (deckPreviewRoot == null) return;
            if (!PlayerPrefs.HasKey("PlayerDeck")) return;
            if (PlayerInventory.Instance == null) return;

            try
            {
                string json = PlayerPrefs.GetString("PlayerDeck");
                SavedDeckData data = JsonUtility.FromJson<SavedDeckData>(json);
                if (data == null || data.customUnitNames == null) return;

                var slots = new List<Image>();
                foreach (Transform child in deckPreviewRoot)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null) slots.Add(img);
                }

                for (int i = 0; i < slots.Count; i++)
                {
                    if (i >= data.customUnitNames.Count) break;

                    var unit = PlayerInventory.Instance.allUnitsDatabase
                        .Find(u => u.unitName == data.customUnitNames[i]);

                    if (unit == null) continue;

                    // 슬롯 Image → 희귀도 프레임
                    slots[i].sprite = GetFrameSprite(unit.rarity);
                    slots[i].color  = Color.white;

                    // 기존 아이콘 자식 제거 후 새로 생성
                    var existingIcon = slots[i].transform.Find("UnitIcon");
                    if (existingIcon != null) Destroy(existingIcon.gameObject);

                    if (unit.icon != null)
                    {
                        var iconGO   = new GameObject("UnitIcon");
                        iconGO.transform.SetParent(slots[i].transform, false);

                        var iconRect        = iconGO.AddComponent<RectTransform>();
                        iconRect.anchorMin  = new Vector2(0.1f, 0.1f);
                        iconRect.anchorMax  = new Vector2(0.9f, 0.9f);
                        iconRect.offsetMin  = Vector2.zero;
                        iconRect.offsetMax  = Vector2.zero;

                        var iconImg   = iconGO.AddComponent<Image>();
                        iconImg.sprite = unit.icon;
                        iconImg.color  = Color.white;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] DeckPreview 로드 실패: {e.Message}");
            }
        }

        Sprite GetFrameSprite(UnitRarity rarity)
        {
            return rarity switch
            {
                UnitRarity.Rare      => frameRare,
                UnitRarity.Epic      => frameEpic,
                UnitRarity.Legendary => frameLegendary,
                _                    => frameCommon,
            };
        }

        void UpdateRank()
        {
            string rankName  = Chess.Core.RankSystem.GetRankName();
            string tier      = Chess.Core.RankSystem.GetTierLetter();
            Color  tierColor = Chess.Core.RankSystem.GetTierColor();
            Sprite sprite    = GetRankBadgeSprite(tier);
            Color  bgColor   = GetRankBadgeBgColor(tier);

            if (rankBadgeOutline != null)
            {
                rankBadgeOutline.sprite = sprite;
                rankBadgeOutline.color  = tierColor;
            }
            if (rankBadgeBg != null)
            {
                rankBadgeBg.sprite = sprite;
                rankBadgeBg.color  = bgColor;
            }
            if (rankText       != null) rankText.text       = rankName;
            if (rankPointsText != null) rankPointsText.text = Chess.Core.RankSystem.GetPoints().ToString();
        }

        Sprite GetRankBadgeSprite(string tier)
        {
            return tier switch
            {
                "S" => rankBadgeSpriteS,
                "A" => rankBadgeSpriteA,
                "B" => rankBadgeSpriteB,
                "C" => rankBadgeSpriteC,
                _   => rankBadgeSpriteD,
            };
        }

        Color GetRankBadgeBgColor(string tier)
        {
            return tier switch
            {
                "S" => BADGE_BG_S,
                "A" => BADGE_BG_A,
                "B" => BADGE_BG_B,
                "C" => BADGE_BG_C,
                _   => BADGE_BG_D,
            };
        }

        void UpdateDailyBonus()
        {
            if (DailySystem.Instance == null) return;
            if (dailyBonusButton == null) return;

            bool claimed = DailySystem.Instance.IsDailyBonusClaimed();
            dailyBonusButton.interactable = !claimed;

            // 수령 완료 시 버튼을 명확히 회색으로 표시
            var cb = dailyBonusButton.colors;
            cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
            dailyBonusButton.colors = cb;
        }

        void UpdateDailyMission()
        {
            if (DailySystem.Instance == null) return;
            int wins = DailySystem.Instance.GetMissionWins();
            int capped = Mathf.Min(wins, 3);

            if (missionProgressFill != null)
                missionProgressFill.fillAmount = capped / 3f;
            if (missionProgressText != null)
                missionProgressText.text = $"( {capped} / 3 )";
        }

        void OnDailyBonusClicked()
        {
            if (DailySystem.Instance?.ClaimDailyBonus() == true)
                UpdateDailyBonus();
        }

        void OnFirebaseLogin(string uid)
        {
            Chess.Core.FirebaseManager.Instance.OnLoginSuccess -= OnFirebaseLogin;
            Debug.Log($"[MainMenu] Firebase 로그인 완료: {uid}");
            // player_name이 없으면 UID 앞 8자리를 기본 이름으로 사용
            if (!PlayerPrefs.HasKey("player_name"))
            {
                string shortId = uid.Length >= 8 ? uid[..8] : uid;
                PlayerPrefs.SetString("player_name", shortId);
                PlayerPrefs.Save();
                UpdatePlayerCard();
            }
        }

        void OnFirestoreDataReady(Chess.Core.FirestorePlayerData data)
        {
            if (data == null) return;
            // 랭크 포인트 서버 값 반영
            Chess.Core.RankSystem.SetFromServer(data.rankPoints);
            UpdateRank();
            UpdateCurrency();
            UpdateDailyBonus();
            UpdateDailyMission();
        }

        void OnDeckBuilderClicked() => SceneManager.LoadScene("DeckBuilder");
        void OnGachaClicked()       => SceneManager.LoadScene("Gacha");
        void OnInventoryClicked()   => SceneManager.LoadScene("Inventory");

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

            if (!LoadAndTransferDeck())
            {
                Debug.LogWarning("[MainMenu] 덱 로드 실패! 덱 빌더로 이동합니다.");
                SceneManager.LoadScene("DeckBuilder");
                return;
            }

            // 덱 전달 완료 후 모드 선택
            if (modeSelectPanel != null)
                modeSelectPanel.Open(OnModeSelected);
            else
                OnModeSelected(Chess.Core.GameMode.AI);
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

                if (!string.IsNullOrEmpty(data.kingName))
                    playerDeck.king = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.kingName);

                if (!string.IsNullOrEmpty(data.pawnName))
                    playerDeck.pawn = PlayerInventory.Instance.allUnitsDatabase.Find(u => u.unitName == data.pawnName);

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

        // ─── 모드 선택 → 씬 전환 / 온라인 매칭 ───────────────────────

        void OnModeSelected(Chess.Core.GameMode mode)
        {
            if (mode == Chess.Core.GameMode.AI)
            {
                SceneManager.LoadScene("SampleScene");
                return;
            }

            // Normal / Ranked → 온라인 매칭
            StartOnlineMatchmaking(mode);
        }

        void StartOnlineMatchmaking(Chess.Core.GameMode mode)
        {
            if (Chess.Core.PhotonManager.Instance == null)
            {
                Debug.LogError("[MainMenu] PhotonManager 없음 — AI 모드로 대체");
                SceneManager.LoadScene("SampleScene");
                return;
            }

            if (MatchmakingOverlay.Instance == null)
            {
                Debug.LogError("[MainMenu] MatchmakingOverlay 없음!");
                return;
            }

            MatchmakingOverlay.Instance.StartMatchmaking(mode);
        }

    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Chess.Core;

namespace Chess.Presentation
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("패널 루트")]
        public GameObject victoryPanel;

        [Header("타이틀")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI titleGlow;

        [Header("배경")]
        public GameObject bgPanel;
        public Image bgDim;

        [Header("골드 내역")]
        public TextMeshProUGUI baseLabelText;
        public TextMeshProUGUI baseGoldText;
        public GameObject missionRow;
        public TextMeshProUGUI missionGoldText;
        public TextMeshProUGUI totalGoldText;
        public TextMeshProUGUI ownedGoldText;

        [Header("랭크 결과 (선택)")]
        public TextMeshProUGUI rankResultText;
        public RankPanelUI rankPanel;

        [Header("버튼")]
        public Button restartButton;
        public Button mainMenuButton;

        // 승리 색상 #C9913A
        static readonly Color WIN_COLOR     = new Color(0.788f, 0.569f, 0.227f, 1f);
        // 패배 색상 #7A7A9A
        static readonly Color LOSE_COLOR    = new Color(0.478f, 0.478f, 0.604f, 1f);
        // TitleGlow alpha 31/255
        const float GLOW_ALPHA = 31f / 255f;
        // BGDim alpha 128/255
        const float DIM_ALPHA  = 128f / 255f;
        // 승리 BGDim #140E00
        static readonly Color WIN_DIM_COLOR  = new Color(0.078f, 0.055f, 0f,    DIM_ALPHA);
        // 패배 BGDim #0A0A14
        static readonly Color LOSE_DIM_COLOR = new Color(0.039f, 0.039f, 0.078f, DIM_ALPHA);

        void Awake()
        {
            if (restartButton  != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            Hide();
        }

        /// <summary>결과창 표시.</summary>
        /// <param name="winnerID">0 = 플레이어 승, 1 = 플레이어 패</param>
        /// <param name="baseGold">기본 골드 보상 (40 or 15)</param>
        /// <param name="missionBonus">미션 보너스 (150 or 0)</param>
        /// <param name="rankPointDelta">랭크 포인트 변화 (+15 / -8 / 0)</param>
        /// <param name="isRanked">랭크 모드 여부</param>
        public void Show(int winnerID, int baseGold, int missionBonus, int rankPointDelta, bool isRanked)
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (bgPanel      != null) bgPanel.SetActive(true);
            if (bgDim        != null) bgDim.gameObject.SetActive(true);

            bool isWin = winnerID == 0;
            Color mainColor = isWin ? WIN_COLOR : LOSE_COLOR;

            // 타이틀
            if (titleText != null)
            {
                titleText.text  = isWin ? "VICTORY" : "DEFEAT";
                titleText.color = mainColor;
            }
            if (titleGlow != null)
            {
                titleGlow.text  = isWin ? "VICTORY" : "DEFEAT";
                Color glowColor = mainColor;
                glowColor.a     = GLOW_ALPHA;
                titleGlow.color = glowColor;
            }

            // BGDim
            if (bgDim != null)
                bgDim.color = isWin ? WIN_DIM_COLOR : LOSE_DIM_COLOR;

            // 골드 내역
            int totalGold = baseGold + missionBonus;

            if (baseLabelText != null)
                baseLabelText.text = isWin ? "승리 보상" : "패배 보상";

            if (baseGoldText != null)
                baseGoldText.text = $"+{baseGold}";

            if (missionRow != null)
                missionRow.SetActive(missionBonus > 0);
            if (missionGoldText != null)
                missionGoldText.text = $"+{missionBonus}";

            if (totalGoldText != null)
                totalGoldText.text = $"+{totalGold}";

            if (ownedGoldText != null)
            {
                int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.currency : 0;
                ownedGoldText.text = owned.ToString("N0");
            }

            // 랭크 포인트
            if (rankResultText != null)
            {
                int totalPts = Chess.Core.RankSystem.GetPoints();
                if (isRanked)
                {
                    string sign = rankPointDelta >= 0 ? $"+{rankPointDelta}" : $"{rankPointDelta}";
                    rankResultText.text  = $"{totalPts} ({sign})";
                    rankResultText.color = rankPointDelta >= 0 ? WIN_COLOR : new Color(1f, 0.35f, 0.35f, 1f);
                }
                else
                {
                    rankResultText.text  = $"{totalPts} (+0)";
                    rankResultText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                }
            }

            // 랭크 뱃지 갱신
            rankPanel?.Refresh();

            // 사운드
            AudioManager.Instance?.PlaySting(isWin ? "sting_victory" : "sting_defeat");
        }

        public void Hide()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (bgPanel      != null) bgPanel.SetActive(false);
            if (bgDim        != null) bgDim.gameObject.SetActive(false);
        }

        public void OnRestartClicked()
        {
            AudioManager.Instance?.Play("ui_button_confirm");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnMainMenuClicked()
        {
            AudioManager.Instance?.Play("ui_button_confirm");
            SceneManager.LoadScene("MainMenu");
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Chess.Core;

namespace Chess.Presentation
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("UI 참조")]
        public GameObject victoryPanel;
        public TextMeshProUGUI victoryText;
        public Button restartButton;

        void Awake()
        {
            Debug.Log("[VictoryUI] Awake 호출");

            if (restartButton != null)
            {
                Debug.Log("[VictoryUI] 재시작 버튼 찾음, 이벤트 등록");
                restartButton.onClick.RemoveAllListeners();  // 기존 리스너 모두 제거
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            else
            {
                Debug.LogError("[VictoryUI] 재시작 버튼이 null입니다!");
            }

            Hide();
        }

        public void Show(PlayerData winner)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            AudioManager.Instance?.PlaySting(winner.playerID == 0 ? "sting_victory" : "sting_defeat");

            if (victoryText != null)
            {
                victoryText.text = $"{winner.GetDisplayName()} 승리!";

                victoryText.color = winner.playerID == 0
                    ? new Color(1f, 0.86f, 0f)
                    : new Color(1f, 0.3f, 0.3f);
            }
        }

        public void Show(int winnerID)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            AudioManager.Instance?.PlaySting(winnerID == 0 ? "sting_victory" : "sting_defeat");

            if (victoryText != null)
            {
                string playerName = winnerID == 0 ? "플레이어" : "AI";
                victoryText.text = $"{playerName} 승리!";

                victoryText.color = winnerID == 0
                    ? new Color(1f, 0.86f, 0f)
                    : new Color(1f, 0.3f, 0.3f);
            }
        }

        public void Hide()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        public void OnRestartClicked()  // 꼭 public!
        {
            Debug.Log("=============== 버튼 클릭됨! ===============");

            AudioManager.Instance?.Play("ui_button_confirm");
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}

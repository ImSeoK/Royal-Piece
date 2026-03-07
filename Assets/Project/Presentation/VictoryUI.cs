using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Chess.Core;

namespace Chess.Presentation
{
    public class VictoryUI : MonoBehaviour
    {
        [Header("UI 요소")]
        public GameObject victoryPanel;
        public TextMeshProUGUI victoryText;
        public Button restartButton;

        void Awake()
        {
            Debug.Log("[VictoryUI] Awake 실행");

            if (restartButton != null)
            {
                Debug.Log("[VictoryUI] 재시작 버튼 연결됨, 이벤트 등록");
                restartButton.onClick.RemoveAllListeners();  // ← 기존 리스너 제거
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

            if (victoryText != null)
            {
                victoryText.text = $"★ {winner.GetDisplayName()} 승리! ★";

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

            if (victoryText != null)
            {
                string playerName = winnerID == 0 ? "플레이어" : "AI";
                victoryText.text = $"★ {playerName} 승리! ★";

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

        public void OnRestartClicked()  // ← public!
        {
            Debug.Log("=============== 버튼 클릭됨! ===============");

            Scene currentScene = SceneManager.GetActiveScene();
            Debug.Log($"Scene 이름: {currentScene.name}");

            SceneManager.LoadScene(currentScene.name);
        }
    }
}